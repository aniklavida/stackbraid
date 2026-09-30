import 'package:stackbraid_client/realtime.dart';

abstract class NotificationsSource {
  Future<RealtimeConnection> connect(String httpBaseUrl, String accessToken);
}

class DefaultNotificationsSource implements NotificationsSource {
  const DefaultNotificationsSource();

  @override
  Future<RealtimeConnection> connect(String httpBaseUrl, String accessToken) {
    return connectRealtimeChannel(httpBaseUrl, RealtimeChannel.notifications, accessToken);
  }
}
