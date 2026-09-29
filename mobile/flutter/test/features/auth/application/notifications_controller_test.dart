import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_client/realtime.dart';
import 'package:stackbraid_client/stackbraid_client.dart';
import 'package:stackbraid_mobile/features/auth/application/notifications_controller.dart';
import 'package:stackbraid_mobile/features/auth/data/notifications_source.dart';

class _FakeRealtimeConnection implements RealtimeConnection {
  final _messageController = StreamController<StackBraidRealtimeMessage>.broadcast();
  final _closeController = StreamController<void>.broadcast();
  bool closed = false;

  @override
  RealtimeTransport get transport => RealtimeTransport.signalr;

  @override
  Stream<StackBraidRealtimeMessage> get messages => _messageController.stream;

  @override
  Stream<void> get onClose => _closeController.stream;

  @override
  void start() {}

  @override
  Future<void> close() async {
    closed = true;
    if (!_closeController.isClosed) {
      _closeController.add(null);
      await _closeController.close();
    }
    await _messageController.close();
  }

  void pushMessage(StackBraidRealtimeMessage message) {
    _messageController.add(message);
  }

  void simulateDisconnect() {
    _closeController.add(null);
  }
}

class _FakeNotificationsSource implements NotificationsSource {
  _FakeRealtimeConnection? connection;
  String? connectedBaseUrl;
  String? connectedToken;

  @override
  Future<RealtimeConnection> connect(String httpBaseUrl, String accessToken) async {
    connectedBaseUrl = httpBaseUrl;
    connectedToken = accessToken;
    final conn = _FakeRealtimeConnection();
    connection = conn;
    return conn;
  }
}

void main() {
  group('NotificationsController', () {
    test('connects and receives realtime notification messages', () async {
      final source = _FakeNotificationsSource();
      final controller = NotificationsController(source: source);

      expect(controller.isConnected, isFalse);
      expect(controller.notifications, isEmpty);

      controller.start(
        httpBaseUrl: 'http://127.0.0.1:8080',
        accessToken: 'test-token',
      );

      await Future<void>.delayed(Duration.zero);

      expect(controller.isConnected, isTrue);
      expect(source.connectedBaseUrl, 'http://127.0.0.1:8080');
      expect(source.connectedToken, 'test-token');

      // Send UserRoleChangedMessage
      source.connection?.pushMessage(
        UserRoleChangedMessage(
          type: UserRoleChangedMessageTypeEnum.userPeriodRoleChanged,
          occurredAt: DateTime.utc(2026, 1, 1, 12, 0),
          userId: 'u1',
          roles: [
            Role(
              id: 'r1',
              name: 'Admin',
              description: 'Administrator',
              permissions: const [],
            ),
          ],
        ),
      );

      await Future<void>.delayed(Duration.zero);

      expect(controller.notifications.length, 1);
      expect(controller.notifications.first.type, 'user.role_changed');
      expect(controller.notifications.first.detail, 'Admin');

      // Send UserDeactivatedMessage
      source.connection?.pushMessage(
        UserDeactivatedMessage(
          type: UserDeactivatedMessageTypeEnum.userPeriodDeactivated,
          occurredAt: DateTime.utc(2026, 1, 1, 12, 1),
          userId: 'u1',
        ),
      );

      await Future<void>.delayed(Duration.zero);

      expect(controller.notifications.length, 2);
      expect(controller.notifications.first.type, 'user.deactivated');
      expect(controller.notifications.first.detail, 'u1');

      controller.stop();
      expect(controller.isConnected, isFalse);
      expect(source.connection?.closed, isTrue);
    });

    test('updates connection status to paused on disconnect while keeping notifications', () async {
      final source = _FakeNotificationsSource();
      final controller = NotificationsController(source: source);

      controller.start(
        httpBaseUrl: 'http://127.0.0.1:8080',
        accessToken: 'test-token',
      );
      await Future<void>.delayed(Duration.zero);
      expect(controller.isConnected, isTrue);

      source.connection?.pushMessage(
        UserDeactivatedMessage(
          type: UserDeactivatedMessageTypeEnum.userPeriodDeactivated,
          occurredAt: DateTime.utc(2026, 1, 1, 12, 0),
          userId: 'u1',
        ),
      );
      await Future<void>.delayed(Duration.zero);
      expect(controller.notifications.length, 1);

      // Simulate disconnect
      source.connection?.simulateDisconnect();
      await Future<void>.delayed(Duration.zero);

      expect(controller.isConnected, isFalse);
      expect(controller.notifications.length, 1);

      controller.stop();
    });
  });
}
