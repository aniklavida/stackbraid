import 'package:dio/dio.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

import '../domain/validation.dart';

/// The only place this feature touches the generated client
/// (`clients/dart`, imported unchanged as the `stackbraid_client` package —
/// never hand-edited, never copied). Everything above this layer thinks in
/// terms of [RegisterFormValues]/[LoginFormValues]/[TokenPair]/[User], not
/// `Dio` or response envelopes.
abstract class AuthRepository {
  Future<User> register(RegisterFormValues values);
  Future<TokenPair> signIn(LoginFormValues values);
  Future<TokenPair> refresh(String refreshToken);
  Future<void> signOut(String? refreshToken);
  Future<User> currentUser(String accessToken);
}

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._authApi, this._dio);

  final AuthApi _authApi;
  final Dio _dio;

  @override
  Future<User> register(RegisterFormValues values) async {
    final response = await _authApi.registerUser(
      registerRequest: RegisterRequest(
        email: values.email,
        password: values.password,
        displayName: values.displayName,
      ),
    );
    return _requireData(response.data, 'register');
  }

  @override
  Future<TokenPair> signIn(LoginFormValues values) async {
    final response = await _authApi.login(
      loginRequest: LoginRequest(email: values.email, password: values.password),
    );
    return _requireData(response.data, 'login');
  }

  @override
  Future<TokenPair> refresh(String refreshToken) async {
    final response = await _authApi.refreshToken(
      refreshRequest: RefreshRequest(refreshToken: refreshToken),
    );
    return _requireData(response.data, 'refresh');
  }

  @override
  Future<void> signOut(String? refreshToken) async {
    await _authApi.logout(
      refreshRequest: refreshToken == null ? null : RefreshRequest(refreshToken: refreshToken),
    );
  }

  @override
  Future<User> currentUser(String accessToken) async {
    // Passed explicitly as a header rather than relying on the shared
    // client's own bearer-auth store: this call is what confirms the token
    // is actually good for something, before a caller commits to it as the
    // active session (mirrors the web frontends' `adoptSession`).
    final response = await _authApi.getCurrentUser(
      headers: {'Authorization': 'Bearer $accessToken'},
    );
    return _requireData(response.data, 'getCurrentUser');
  }

  T _requireData<T>(T? data, String operation) {
    if (data == null) {
      throw StateError('$operation returned no body.');
    }
    return data;
  }

  // Exposed so `shared/http` can build one Dio/AuthApi pair and hand this
  // repository the same instance every other repository in the app shares.
  Dio get dio => _dio;
}
