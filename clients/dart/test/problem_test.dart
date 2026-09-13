import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for Problem
void main() {
  final Problem? instance = /* Problem(...) */ null;
  // TODO add properties to the entity

  group(Problem, () {
    // A stable identifier for this problem type. `about:blank` when no more specific type applies.
    // String type (default value: 'about:blank')
    test('to test the property `type`', () async {
      // TODO
    });

    // A short, human-readable summary, constant for a given `type`.
    // String title
    test('to test the property `title`', () async {
      // TODO
    });

    // The HTTP status code, repeated here so it survives proxies that only pass the body along.
    // int status
    test('to test the property `status`', () async {
      // TODO
    });

    // A human-readable explanation specific to this occurrence.
    // String detail
    test('to test the property `detail`', () async {
      // TODO
    });

    // The request path that produced this problem.
    // String instance
    test('to test the property `instance`', () async {
      // TODO
    });

    // A stable, machine-readable application error code, e.g. `IDENTITY.INVALID_CREDENTIALS`. Stable across locales and across both backends; `title` and `detail` are not (they are localized).
    // String code
    test('to test the property `code`', () async {
      // TODO
    });

    // Correlation ID for this request, matching the one in structured logs.
    // String traceId
    test('to test the property `traceId`', () async {
      // TODO
    });

    // Present only for validation problems (`status` 400). Maps a field name to its violation messages.
    // Map<String, List<String>> errors
    test('to test the property `errors`', () async {
      // TODO
    });

  });
}
