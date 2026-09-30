import 'dart:async';

import 'package:dio/dio.dart';

import 'network_status.dart';
import 'offline_queue.dart';

/// Dio interceptor that intercepts mutating HTTP requests made while offline,
/// adds them to the [OfflineQueue], and updates [NetworkStatus] on connection errors.
class OfflineInterceptor extends Interceptor {
  OfflineInterceptor({
    required this.networkStatus,
    required this.offlineQueue,
  });

  final NetworkStatus networkStatus;
  final OfflineQueue offlineQueue;

  static const _mutatingMethods = {'POST', 'PUT', 'PATCH', 'DELETE'};

  bool _isWrite(String method) => _mutatingMethods.contains(method.toUpperCase());

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final skipQueue = options.extra['skipOfflineQueue'] == true;

    if (!networkStatus.isOnline && _isWrite(options.method) && !skipQueue) {
      final completer = Completer<dynamic>();
      offlineQueue.enqueue(
        method: options.method,
        path: options.path,
        data: options.data,
        headers: options.headers,
        queryParameters: options.queryParameters,
        completer: completer,
      );

      handler.reject(
        DioException(
          requestOptions: options,
          error: 'Offline: write request queued for replay on reconnect',
          type: DioExceptionType.connectionError,
        ),
      );
      return;
    }

    super.onRequest(options, handler);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.type == DioExceptionType.connectionError ||
        err.type == DioExceptionType.connectionTimeout ||
        err.type == DioExceptionType.sendTimeout ||
        err.type == DioExceptionType.receiveTimeout) {
      networkStatus.markOffline();
    }
    super.onError(err, handler);
  }
}
