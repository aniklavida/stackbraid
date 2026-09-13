import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for RealtimeMessage
void main() {
  final RealtimeMessage? instance = /* RealtimeMessage(...) */ null;
  // TODO add properties to the entity

  group(RealtimeMessage, () {
    // String type
    test('to test the property `type`', () async {
      // TODO
    });

    // String userId
    test('to test the property `userId`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime occurredAt
    test('to test the property `occurredAt`', () async {
      // TODO
    });

    // The user's full role set after the change, not a diff.
    // List<Role> roles
    test('to test the property `roles`', () async {
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

  });
}
