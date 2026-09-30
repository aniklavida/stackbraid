import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/foundation.dart';

enum QueuedWriteStatus { pending, replaying, completed, failed }

/// Represents an offline write request waiting to be synchronized with the backend.
class QueuedWrite {
  QueuedWrite({
    required this.id,
    required this.method,
    required this.path,
    this.data,
    this.headers = const {},
    this.queryParameters,
    DateTime? createdAt,
    this.maxRetries = 3,
    this.completer,
  })  : createdAt = createdAt ?? DateTime.now(),
        retryCount = 0,
        status = QueuedWriteStatus.pending;

  final String id;
  final String method;
  final String path;
  final dynamic data;
  final Map<String, dynamic> headers;
  final Map<String, dynamic>? queryParameters;
  final DateTime createdAt;
  final int maxRetries;
  final Completer<dynamic>? completer;

  int retryCount;
  QueuedWriteStatus status;
  Object? lastError;
}

/// A surfaced failure event emitted when a queued write exhausts its retries
/// or encounters an unrecoverable failure during replay.
class QueuedWriteFailure {
  QueuedWriteFailure({required this.write, required this.error});
  final QueuedWrite write;
  final Object error;

  @override
  String toString() => 'QueuedWriteFailure(${write.method} ${write.path}, retries=${write.retryCount}, error=$error)';
}

typedef WriteExecutor = Future<dynamic> Function(QueuedWrite write);
typedef BackoffCalculator = Duration Function(int retryCount);

/// Queue for storing mutating HTTP writes made while offline and replaying
/// them in order with bounded exponential backoff when connectivity returns.
class OfflineQueue extends ChangeNotifier {
  OfflineQueue({
    this.defaultMaxRetries = 3,
    this.maxBackoff = const Duration(seconds: 10),
    this.baseBackoff = const Duration(milliseconds: 200),
  });

  final int defaultMaxRetries;
  final Duration maxBackoff;
  final Duration baseBackoff;

  final List<QueuedWrite> _items = [];
  final StreamController<QueuedWriteFailure> _failureController = StreamController<QueuedWriteFailure>.broadcast();
  int _sequence = 0;
  bool _isReplaying = false;

  List<QueuedWrite> get writes => List.unmodifiable(_items);
  List<QueuedWrite> get pendingWrites => _items.where((w) => w.status == QueuedWriteStatus.pending).toList();
  List<QueuedWrite> get failedWrites => _items.where((w) => w.status == QueuedWriteStatus.failed).toList();
  bool get hasPendingWrites => _items.any((w) => w.status == QueuedWriteStatus.pending);
  int get length => _items.length;
  int get pendingCount => pendingWrites.length;
  bool get isReplaying => _isReplaying;
  Stream<QueuedWriteFailure> get failures => _failureController.stream;

  QueuedWrite enqueue({
    required String method,
    required String path,
    dynamic data,
    Map<String, dynamic>? headers,
    Map<String, dynamic>? queryParameters,
    int? maxRetries,
    Completer<dynamic>? completer,
  }) {
    _sequence++;
    final write = QueuedWrite(
      id: 'qw_${DateTime.now().microsecondsSinceEpoch}_$_sequence',
      method: method.toUpperCase(),
      path: path,
      data: data,
      headers: headers ?? const {},
      queryParameters: queryParameters,
      maxRetries: maxRetries ?? defaultMaxRetries,
      completer: completer,
    );
    _items.add(write);
    notifyListeners();
    return write;
  }

  /// Calculates a bounded exponential backoff delay based on the attempt count.
  Duration calculateBackoff(int attempt) {
    if (attempt <= 0) return Duration.zero;
    final factor = math.pow(2, attempt - 1).toInt();
    final millis = baseBackoff.inMilliseconds * factor;
    final cappedMillis = math.min(millis, maxBackoff.inMilliseconds);
    return Duration(milliseconds: cappedMillis);
  }

  /// Replays pending writes in FIFO order.
  ///
  /// Retries transient failures up to [QueuedWrite.maxRetries] using bounded backoff.
  /// If retries are exhausted or a terminal failure occurs, the failure is surfaced
  /// through [failures], the write's [completer], and its [lastError], never swallowed.
  Future<void> replay({
    required WriteExecutor executor,
    BackoffCalculator? backoffCalculator,
  }) async {
    if (_isReplaying) return;
    _isReplaying = true;
    notifyListeners();

    try {
      while (hasPendingWrites) {
        final next = _items.firstWhere((w) => w.status == QueuedWriteStatus.pending);

        while (next.retryCount < next.maxRetries) {
          next.status = QueuedWriteStatus.replaying;
          notifyListeners();

          try {
            final result = await executor(next);
            next.status = QueuedWriteStatus.completed;
            next.completer?.complete(result);
            notifyListeners();
            break;
          } catch (e) {
            next.retryCount++;
            next.lastError = e;

            if (next.retryCount >= next.maxRetries) {
              next.status = QueuedWriteStatus.failed;
              if (next.completer != null && !next.completer!.isCompleted) {
                next.completer!.completeError(e);
              }
              _failureController.add(QueuedWriteFailure(write: next, error: e));
              notifyListeners();
              break;
            } else {
              next.status = QueuedWriteStatus.pending;
              notifyListeners();
              final delay = backoffCalculator != null
                  ? backoffCalculator(next.retryCount)
                  : calculateBackoff(next.retryCount);
              if (delay > Duration.zero) {
                await Future<void>.delayed(delay);
              }
            }
          }
        }
      }
    } finally {
      _isReplaying = false;
      notifyListeners();
    }
  }

  void clear() {
    _items.clear();
    notifyListeners();
  }

  @override
  void dispose() {
    _failureController.close();
    super.dispose();
  }
}
