import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:stackbraid_client/realtime.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

import '../data/notifications_source.dart';
import '../domain/notification.dart';

class NotificationsController extends ChangeNotifier {
  NotificationsController({NotificationsSource source = const DefaultNotificationsSource()})
      : _source = source;

  final NotificationsSource _source;
  final List<RealtimeNotification> _notifications = [];
  bool _isConnected = false;
  bool _stopped = false;
  RealtimeConnection? _connection;
  StreamSubscription<dynamic>? _messageSub;
  StreamSubscription<dynamic>? _closeSub;
  Timer? _retryTimer;
  String? _httpBaseUrl;
  String? _accessToken;

  List<RealtimeNotification> get notifications => List.unmodifiable(_notifications);
  bool get isConnected => _isConnected;

  void start({required String httpBaseUrl, required String accessToken}) {
    _stopped = false;
    _httpBaseUrl = httpBaseUrl;
    _accessToken = accessToken;
    _connect();
  }

  void stop() {
    _stopped = true;
    _retryTimer?.cancel();
    _retryTimer = null;
    _messageSub?.cancel();
    _messageSub = null;
    _closeSub?.cancel();
    _closeSub = null;
    _connection?.close();
    _connection = null;
    _isConnected = false;
    notifyListeners();
  }

  Future<void> _connect() async {
    if (_stopped) return;
    final baseUrl = _httpBaseUrl;
    final token = _accessToken;
    if (baseUrl == null || token == null) return;

    try {
      final conn = await _source.connect(baseUrl, token);
      if (_stopped) {
        conn.close();
        return;
      }
      _connection = conn;
      _isConnected = true;
      notifyListeners();

      _messageSub = conn.messages.listen(_handleMessage);
      _closeSub = conn.onClose.listen((_) => _handleDisconnect());
    } catch (_) {
      _handleDisconnect();
    }
  }

  void _handleMessage(StackBraidRealtimeMessage message) {
    if (message is UserDeactivatedMessage) {
      _notifications.insert(
        0,
        RealtimeNotification(
          id: '${message.occurredAt.toIso8601String()}-${_notifications.length}',
          type: 'user.deactivated',
          occurredAt: message.occurredAt,
          detail: message.userId,
        ),
      );
      notifyListeners();
    } else if (message is UserRoleChangedMessage) {
      final roleNames = message.roles.map((r) => r.name).join(', ');
      _notifications.insert(
        0,
        RealtimeNotification(
          id: '${message.occurredAt.toIso8601String()}-${_notifications.length}',
          type: 'user.role_changed',
          occurredAt: message.occurredAt,
          detail: roleNames,
        ),
      );
      notifyListeners();
    }
  }

  void _handleDisconnect() {
    if (_stopped) return;
    _messageSub?.cancel();
    _messageSub = null;
    _closeSub?.cancel();
    _closeSub = null;
    _connection = null;
    _isConnected = false;
    notifyListeners();

    _retryTimer?.cancel();
    _retryTimer = Timer(const Duration(seconds: 3), () {
      if (!_stopped) _connect();
    });
  }

  @override
  void dispose() {
    stop();
    super.dispose();
  }
}
