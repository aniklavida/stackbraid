import 'dart:convert';

import 'package:dio/dio.dart';

const String redactedPlaceholder = '[REDACTED]';

/// Field names whose value is a bearer/refresh/opaque credential and must
/// never reach a log line, a crash report, or stdout — the mobile half of
/// the token-storage decision recorded for every StackBraid client: an
/// access token that never touches disk also never touches a log.
const List<String> _sensitiveFieldNames = [
  'accessToken',
  'refreshToken',
  'password',
  'token',
];

final RegExp _authorizationHeaderPattern = RegExp(
  r'(Authorization["\x27]?\s*[:=]\s*["\x27]?Bearer\s+)([^"\x27\s,}]+)',
  caseSensitive: false,
);

final RegExp _cookieHeaderPattern = RegExp(
  r'((?:set-)?cookie["\x27]?\s*[:=]\s*["\x27]?)([^"\x27;]+)',
  caseSensitive: false,
);

/// Redacts any bearer token, cookie value, or JSON field named in
/// [_sensitiveFieldNames] from a string that is about to be logged. Pure and
/// synchronous on purpose so it can be unit-tested without a network stack —
/// see `test/shared/http/log_redaction_test.dart`.
String redactSensitiveLog(String input) {
  var out = input.replaceAllMapped(
    _authorizationHeaderPattern,
    (m) => '${m.group(1)}$redactedPlaceholder',
  );
  out = out.replaceAllMapped(
    _cookieHeaderPattern,
    (m) => '${m.group(1)}$redactedPlaceholder',
  );
  for (final field in _sensitiveFieldNames) {
    final jsonFieldPattern = RegExp(
      '("$field"\\s*:\\s*)"([^"]*)"',
      caseSensitive: false,
    );
    out = out.replaceAllMapped(jsonFieldPattern, (m) => '${m.group(1)}"$redactedPlaceholder"');
  }
  return out;
}

/// Redacts the same sensitive fields from a decoded JSON-ish map/list
/// structure, recursively — used before a request/response body is
/// stringified for logging, so a nested `{"data": {"refreshToken": "..."}}`
/// is caught even though the flat string pattern above only sees the
/// top-level shape reliably.
Object? redactSensitiveJson(Object? value) {
  if (value is Map) {
    return value.map((key, v) {
      final keyStr = key.toString();
      final isSensitive = _sensitiveFieldNames.any((f) => f.toLowerCase() == keyStr.toLowerCase());
      return MapEntry(key, isSensitive ? redactedPlaceholder : redactSensitiveJson(v));
    });
  }
  if (value is List) {
    return value.map(redactSensitiveJson).toList();
  }
  return value;
}

/// A `LogInterceptor`-shaped Dio interceptor that only ever logs redacted
/// text, and only outside release builds. `describeRequest`/
/// `describeResponse` are exposed separately (not just wired into
/// `onRequest`/`onResponse`) so a test can call them directly without
/// standing up a real HTTP call.
class RedactingLogInterceptor extends Interceptor {
  RedactingLogInterceptor({void Function(String)? logPrint, this.enabled = true})
      : logPrint = logPrint ?? _defaultLogPrint;

  final void Function(String) logPrint;
  final bool enabled;

  static void _defaultLogPrint(String line) {
    // ignore: avoid_print
    print(line);
  }

  String describeRequest(RequestOptions options) {
    final headers = redactSensitiveJson(Map<String, dynamic>.from(options.headers));
    final body = options.data == null ? null : redactSensitiveJson(_asJsonish(options.data));
    return redactSensitiveLog(
      '--> ${options.method} ${options.uri}\nheaders: ${jsonEncode(headers)}'
      '${body == null ? '' : '\nbody: ${jsonEncode(body)}'}',
    );
  }

  String describeResponse(Response response) {
    final headers = redactSensitiveJson(response.headers.map);
    final body = redactSensitiveJson(_asJsonish(response.data));
    return redactSensitiveLog(
      '<-- ${response.statusCode} ${response.requestOptions.uri}\n'
      'headers: ${jsonEncode(headers)}\nbody: ${jsonEncode(body)}',
    );
  }

  String describeError(DioException err) {
    final status = err.response?.statusCode;
    return redactSensitiveLog(
      '<-- error ${status ?? '(no response)'} ${err.requestOptions.uri}: ${err.message}',
    );
  }

  Object? _asJsonish(Object? data) {
    if (data == null || data is Map || data is List || data is num || data is bool) return data;
    return data.toString();
  }

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    if (enabled) logPrint(describeRequest(options));
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    if (enabled) logPrint(describeResponse(response));
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (enabled) logPrint(describeError(err));
    handler.next(err);
  }
}
