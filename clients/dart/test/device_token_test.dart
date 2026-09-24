import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for DeviceToken
void main() {
  final DeviceToken? instance = /* DeviceToken(...) */ null;
  // TODO add properties to the entity

  group(DeviceToken, () {
    // String id
    test('to test the property `id`', () async {
      // TODO
    });

    // DevicePlatform platform
    test('to test the property `platform`', () async {
      // TODO
    });

    // String appVersion
    test('to test the property `appVersion`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime registeredAt
    test('to test the property `registeredAt`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime updatedAt
    test('to test the property `updatedAt`', () async {
      // TODO
    });

  });
}
