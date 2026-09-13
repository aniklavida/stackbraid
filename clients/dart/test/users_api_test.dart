import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';


/// tests for UsersApi
void main() {
  final instance = StackbraidClient().getUsersApi();

  group(UsersApi, () {
    // Deactivate a user
    //
    // Deactivation, not deletion — the account and its history are kept, but it can no longer authenticate. Idempotent: deactivating an already-inactive user returns the current state rather than an error. 
    //
    //Future<User> deactivateUser(String userId) async
    test('test deactivateUser', () async {
      // TODO
    });

    // Read a single user
    //
    //Future<User> getUser(String userId) async
    test('test getUser', () async {
      // TODO
    });

    // List users
    //
    // Paginated, filtered and sorted. Requires the `users:read` permission.
    //
    //Future<UserPage> listUsers({ int page, int pageSize, String sort, String search, UserStatus status, String roleId }) async
    test('test listUsers', () async {
      // TODO
    });

    // Update a user's profile
    //
    // Partial update. Role membership is managed through `/v1/users/{userId}/roles`, not through this endpoint.
    //
    //Future<User> updateUser(String userId, UpdateUserRequest updateUserRequest) async
    test('test updateUser', () async {
      // TODO
    });

  });
}
