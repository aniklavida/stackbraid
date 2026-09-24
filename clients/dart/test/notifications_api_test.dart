import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';


/// tests for NotificationsApi
void main() {
  final instance = StackbraidClient().getNotificationsApi();

  group(NotificationsApi, () {
    // List the authenticated user's in-app notifications
    //
    //Future<NotificationPage> listNotifications({ int page, int pageSize }) async
    test('test listNotifications', () async {
      // TODO
    });

  });
}
