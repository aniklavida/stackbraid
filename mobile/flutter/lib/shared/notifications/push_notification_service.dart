import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

import '../http/api_client.dart';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
}

class PushNotificationService {
  PushNotificationService(this._apiClient);

  final ApiClient _apiClient;
  final _messages = StreamController<RemoteMessage>.broadcast();
  String? _deviceId;
  bool _started = false;

  Stream<RemoteMessage> get messages => _messages.stream;

  Future<void> start() async {
    if (_started) return;
    _started = true;
    final platform = _platform;
    if (platform == null) {
      debugPrint('Push notifications are not available on this platform; the app remains usable.');
      return;
    }

    try {
      await Firebase.initializeApp();
      final permission = await FirebaseMessaging.instance.requestPermission(alert: true, badge: true, sound: true);
      if (permission.authorizationStatus == AuthorizationStatus.denied) {
        debugPrint('Push notifications were declined; email and in-app notifications remain available.');
        return;
      }

      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
      FirebaseMessaging.onMessage.listen(_messages.add);
      FirebaseMessaging.onMessageOpenedApp.listen(_messages.add);
      final initialMessage = await FirebaseMessaging.instance.getInitialMessage();
      if (initialMessage != null) _messages.add(initialMessage);
      FirebaseMessaging.instance.onTokenRefresh.listen((token) => _refreshToken(token, platform));

      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) await _registerToken(token, platform);
    } catch (error) {
      debugPrint('Push notifications are unavailable: $error. The app remains usable.');
    }
  }

  Future<void> stop() async {
    if (!_started) return;
    try {
      await FirebaseMessaging.instance.deleteToken();
    } catch (_) {}
  }

  Future<void> _registerToken(String token, DevicePlatform platform) async {
    try {
      final response = await _apiClient.devicesApi.registerDeviceToken(
        registerDeviceTokenRequest: RegisterDeviceTokenRequest(token: token, platform: platform),
      );
      _deviceId = response.data?.id;
    } catch (error) {
      debugPrint('Push token registration was not accepted: $error');
    }
  }

  Future<void> _refreshToken(String token, DevicePlatform platform) async {
    final deviceId = _deviceId;
    if (deviceId == null) {
      await _registerToken(token, platform);
      return;
    }
    try {
      await _apiClient.devicesApi.refreshDeviceToken(
        deviceId: deviceId,
        refreshDeviceTokenRequest: RefreshDeviceTokenRequest(token: token),
      );
    } catch (error) {
      debugPrint('Push token refresh was not accepted: $error');
    }
  }

  DevicePlatform? get _platform {
    switch (defaultTargetPlatform) {
      case TargetPlatform.iOS:
        return DevicePlatform.ios;
      case TargetPlatform.android:
        return DevicePlatform.android;
      case TargetPlatform.macOS:
      case TargetPlatform.windows:
      case TargetPlatform.linux:
      case TargetPlatform.fuchsia:
        return null;
    }
  }

  Future<void> dispose() => _messages.close();
}
