import 'package:dio/dio.dart';

/// Turns a `DioException` into a message a screen can show directly,
/// reading the RFC 9457 Problem envelope every StackBraid backend returns
/// (`title`/`detail`, per `contract/openapi.yaml`'s `Problem` schema) rather
/// than a raw stack trace or status code.
String describeApiError(Object error) {
  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map) {
      final detail = data['detail'];
      final title = data['title'];
      if (detail is String && detail.isNotEmpty) return detail;
      if (title is String && title.isNotEmpty) return title;
    }
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return 'The connection to the server timed out.';
      case DioExceptionType.connectionError:
        return 'Could not reach the server. Check the connection and try again.';
      default:
        return 'Something went wrong. Please try again.';
    }
  }
  return 'Something went wrong. Please try again.';
}
