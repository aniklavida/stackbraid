import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';


/// tests for DevicesApi
void main() {
  final instance = StackbraidClient().getDevicesApi();

  group(DevicesApi, () {
    // Refresh a device push token
    //
    //Future<DeviceToken> refreshDeviceToken(String deviceId, RefreshDeviceTokenRequest refreshDeviceTokenRequest) async
    test('test refreshDeviceToken', () async {
      // TODO
    });

    // Register a device push token
    //
    //Future<DeviceToken> registerDeviceToken(RegisterDeviceTokenRequest registerDeviceTokenRequest) async
    test('test registerDeviceToken', () async {
      // TODO
    });

    // Revoke a device push token
    //
    //Future revokeDeviceToken(String deviceId) async
    test('test revokeDeviceToken', () async {
      // TODO
    });

  });
}
