import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_mobile/shared/http/network_status.dart';
import 'package:stackbraid_mobile/shared/http/offline_interceptor.dart';
import 'package:stackbraid_mobile/shared/http/offline_queue.dart';

void main() {
  group('OfflineQueue', () {
    test('writes queued offline', () async {
      final queue = OfflineQueue();
      final networkStatus = NetworkStatus(initialOnline: false);

      expect(queue.hasPendingWrites, isFalse);
      expect(queue.length, 0);

      final write = queue.enqueue(
        method: 'POST',
        path: '/v1/items',
        data: {'name': 'New Item'},
      );

      expect(queue.hasPendingWrites, isTrue);
      expect(queue.length, 1);
      expect(queue.pendingCount, 1);
      expect(write.method, 'POST');
      expect(write.path, '/v1/items');
      expect(write.data, {'name': 'New Item'});
      expect(write.status, QueuedWriteStatus.pending);

      // Verify OfflineInterceptor queues writes when offline and rejects with connection error
      final dio = Dio();
      dio.interceptors.add(OfflineInterceptor(networkStatus: networkStatus, offlineQueue: queue));

      await expectLater(
        dio.post<dynamic>('/v1/other', data: {'val': 42}),
        throwsA(isA<DioException>().having(
          (e) => e.type,
          'type',
          DioExceptionType.connectionError,
        )),
      );

      expect(queue.length, 2);
      expect(queue.writes.last.path, '/v1/other');
    });

    test('replayed in order on reconnect', () async {
      final queue = OfflineQueue();
      final executionOrder = <String>[];

      queue.enqueue(method: 'POST', path: '/v1/first', data: {'seq': 1});
      queue.enqueue(method: 'PUT', path: '/v1/second', data: {'seq': 2});
      queue.enqueue(method: 'DELETE', path: '/v1/third');

      expect(queue.pendingCount, 3);

      await queue.replay(
        executor: (write) async {
          executionOrder.add(write.path);
          return {'status': 'ok'};
        },
      );

      expect(executionOrder, ['/v1/first', '/v1/second', '/v1/third']);
      expect(queue.pendingCount, 0);
      expect(queue.writes.every((w) => w.status == QueuedWriteStatus.completed), isTrue);
    });

    test('bounded retries with backoff', () async {
      final queue = OfflineQueue(
        defaultMaxRetries: 3,
        baseBackoff: const Duration(milliseconds: 10),
        maxBackoff: const Duration(milliseconds: 50),
      );

      var attemptCount = 0;
      final backoffDelays = <Duration>[];

      queue.enqueue(method: 'POST', path: '/v1/retry-me', maxRetries: 3);

      await queue.replay(
        executor: (write) async {
          attemptCount++;
          if (attemptCount < 3) {
            throw DioException(
              requestOptions: RequestOptions(path: write.path),
              type: DioExceptionType.connectionError,
            );
          }
          return {'success': true};
        },
        backoffCalculator: (retryCount) {
          final delay = queue.calculateBackoff(retryCount);
          backoffDelays.add(delay);
          return Duration.zero; // Fast forward in test
        },
      );

      expect(attemptCount, 3);
      expect(queue.writes.first.status, QueuedWriteStatus.completed);
      expect(backoffDelays, [
        const Duration(milliseconds: 10),
        const Duration(milliseconds: 20),
      ]);
    });

    test('a failure surfaced not swallowed', () async {
      final queue = OfflineQueue(defaultMaxRetries: 2);
      final surfacedFailures = <QueuedWriteFailure>[];
      final subscription = queue.failures.listen((failure) {
        surfacedFailures.add(failure);
      });

      final completer = Completer<dynamic>();
      final write = queue.enqueue(
        method: 'POST',
        path: '/v1/failing',
        maxRetries: 2,
        completer: completer,
      );

      final terminalError = Exception('Backend rejected write with 400 Bad Request');
      Object? completerError;
      completer.future.catchError((e) {
        completerError = e;
      });

      await queue.replay(
        executor: (write) async {
          throw terminalError;
        },
        backoffCalculator: (_) => Duration.zero,
      );

      await Future<void>.delayed(Duration.zero);

      // Verify the write is marked failed
      expect(write.status, QueuedWriteStatus.failed);
      expect(write.lastError, equals(terminalError));

      // Verify the failure is surfaced on the failure stream
      expect(surfacedFailures.length, 1);
      expect(surfacedFailures.first.write.id, write.id);
      expect(surfacedFailures.first.error, equals(terminalError));

      // Verify the completer is completed with error and not swallowed
      expect(completer.isCompleted, isTrue);
      expect(completerError, equals(terminalError));

      // Verify the failure is tracked in failedWrites
      expect(queue.failedWrites.length, 1);
      expect(queue.failedWrites.first.id, write.id);

      await subscription.cancel();
    });
  });
}
