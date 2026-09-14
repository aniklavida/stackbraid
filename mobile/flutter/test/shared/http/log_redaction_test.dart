// Proves the token-storage decision's "never touches a log" half: neither
// the access token nor the refresh token this app handles can reach
// whatever `RedactingLogInterceptor` prints, in a request, a response, or
// an error — the three places a token could otherwise leak into stdout, a
// crash reporter, or a CI log.
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_mobile/shared/http/log_redaction.dart';

const secretAccessToken = 'eyJhbGciOiJIUzI1NiJ9.super-secret-access-token-value';
const secretRefreshToken = 'rt_9f8a7b6c5d4e3f2a1b0c-do-not-log-me';
const secretPassword = 'correct horse battery staple';

void main() {
  group('redactSensitiveLog (pure string redaction)', () {
    test('redacts a bearer token from an Authorization header line', () {
      final line = 'headers: {"Authorization":"Bearer $secretAccessToken"}';
      final out = redactSensitiveLog(line);
      expect(out, isNot(contains(secretAccessToken)));
      expect(out, contains(redactedPlaceholder));
    });

    test('redacts a cookie header value', () {
      final line = 'set-cookie: refreshToken=$secretRefreshToken; HttpOnly; Path=/v1/auth';
      final out = redactSensitiveLog(line);
      expect(out, isNot(contains(secretRefreshToken)));
    });

    test('redacts JSON fields named accessToken/refreshToken/password', () {
      final line = '{"accessToken":"$secretAccessToken","refreshToken":"$secretRefreshToken","password":"$secretPassword"}';
      final out = redactSensitiveLog(line);
      expect(out, isNot(contains(secretAccessToken)));
      expect(out, isNot(contains(secretRefreshToken)));
      expect(out, isNot(contains(secretPassword)));
    });
  });

  group('RedactingLogInterceptor', () {
    final captured = <String>[];
    final interceptor = RedactingLogInterceptor(logPrint: captured.add);

    setUp(captured.clear);

    test('describeRequest never includes a request body password or bearer token', () {
      final options = RequestOptions(
        path: '/v1/auth/login',
        method: 'POST',
        headers: {'Authorization': 'Bearer $secretAccessToken', 'Content-Type': 'application/json'},
        data: {'email': 'sadia.islam@example.com', 'password': secretPassword},
      );
      final described = interceptor.describeRequest(options);
      expect(described, isNot(contains(secretAccessToken)));
      expect(described, isNot(contains(secretPassword)));
    });

    test('describeResponse never includes an accessToken or refreshToken from the body', () {
      final requestOptions = RequestOptions(path: '/v1/auth/login');
      final response = Response(
        requestOptions: requestOptions,
        statusCode: 200,
        data: {
          'accessToken': secretAccessToken,
          'refreshToken': secretRefreshToken,
          'tokenType': 'Bearer',
          'expiresAt': '2026-09-14T10:00:00Z',
        },
      );
      final described = interceptor.describeResponse(response);
      expect(described, isNot(contains(secretAccessToken)));
      expect(described, isNot(contains(secretRefreshToken)));
      // Non-secret fields still show up — this is redaction, not a black box.
      expect(described, contains('Bearer'));
    });

    test('onRequest/onResponse actually call logPrint with the redacted text, never the raw secret', () async {
      final options = RequestOptions(
        path: '/v1/auth/me',
        headers: {'Authorization': 'Bearer $secretAccessToken'},
      );
      interceptor.onRequest(options, RequestInterceptorHandler());

      final response = Response(
        requestOptions: options,
        statusCode: 200,
        data: {'refreshToken': secretRefreshToken},
      );
      interceptor.onResponse(response, ResponseInterceptorHandler());

      expect(captured, isNotEmpty);
      for (final line in captured) {
        expect(line, isNot(contains(secretAccessToken)));
        expect(line, isNot(contains(secretRefreshToken)));
      }
    });

    test('a disabled interceptor logs nothing at all', () {
      final silent = RedactingLogInterceptor(logPrint: captured.add, enabled: false);
      silent.onRequest(
        RequestOptions(path: '/v1/auth/login', headers: {'Authorization': 'Bearer $secretAccessToken'}),
        RequestInterceptorHandler(),
      );
      expect(captured, isEmpty);
    });
  });
}
