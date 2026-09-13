import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for JobProgressMessage
void main() {
  final JobProgressMessage? instance = /* JobProgressMessage(...) */ null;
  // TODO add properties to the entity

  group(JobProgressMessage, () {
    // String type
    test('to test the property `type`', () async {
      // TODO
    });

    // String jobId
    test('to test the property `jobId`', () async {
      // TODO
    });

    // String status
    test('to test the property `status`', () async {
      // TODO
    });

    // int progress
    test('to test the property `progress`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime occurredAt
    test('to test the property `occurredAt`', () async {
      // TODO
    });

  });
}
