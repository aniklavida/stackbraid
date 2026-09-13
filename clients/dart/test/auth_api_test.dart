import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';


/// tests for AuthApi
void main() {
  final instance = StackbraidClient().getAuthApi();

  group(AuthApi, () {
    // Read the caller's own account
    //
    //Future<User> getCurrentUser() async
    test('test getCurrentUser', () async {
      // TODO
    });

    // Exchange credentials for a token pair
    //
    //Future<TokenPair> login(LoginRequest loginRequest) async
    test('test login', () async {
      // TODO
    });

    // Revoke the current session
    //
    //Future logout({ RefreshRequest refreshRequest }) async
    test('test logout', () async {
      // TODO
    });

    // Exchange a refresh token for a new token pair
    //
    //Future<TokenPair> refreshToken({ RefreshRequest refreshRequest }) async
    test('test refreshToken', () async {
      // TODO
    });

    // Register a new account
    //
    //Future<User> registerUser(RegisterRequest registerRequest) async
    test('test registerUser', () async {
      // TODO
    });

  });
}
