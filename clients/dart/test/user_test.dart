import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for User
void main() {
  final User? instance = /* User(...) */ null;
  // TODO add properties to the entity

  group(User, () {
    // String id
    test('to test the property `id`', () async {
      // TODO
    });

    // String email
    test('to test the property `email`', () async {
      // TODO
    });

    // String displayName
    test('to test the property `displayName`', () async {
      // TODO
    });

    // UserStatus status
    test('to test the property `status`', () async {
      // TODO
    });

    // List<Role> roles
    test('to test the property `roles`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime createdAt
    test('to test the property `createdAt`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime updatedAt
    test('to test the property `updatedAt`', () async {
      // TODO
    });

    // Same rule as `UtcDateTime`; `null` means the event has not happened yet.
    // DateTime lastLoginAt
    test('to test the property `lastLoginAt`', () async {
      // TODO
    });

  });
}
