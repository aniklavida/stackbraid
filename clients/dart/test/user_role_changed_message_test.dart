import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for UserRoleChangedMessage
void main() {
  final UserRoleChangedMessage? instance = /* UserRoleChangedMessage(...) */ null;
  // TODO add properties to the entity

  group(UserRoleChangedMessage, () {
    // String type
    test('to test the property `type`', () async {
      // TODO
    });

    // String userId
    test('to test the property `userId`', () async {
      // TODO
    });

    // The user's full role set after the change, not a diff.
    // List<Role> roles
    test('to test the property `roles`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime occurredAt
    test('to test the property `occurredAt`', () async {
      // TODO
    });

  });
}
