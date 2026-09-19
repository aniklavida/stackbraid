# stackbraid_client.api.AuthApi

## Load the API package
```dart
import 'package:stackbraid_client/api.dart';
```

All URIs are relative to *http://localhost:8080*

Method | HTTP request | Description
------------- | ------------- | -------------
[**getCurrentUser**](AuthApi.md#getcurrentuser) | **GET** /v1/auth/me | Read the caller&#39;s own account
[**login**](AuthApi.md#login) | **POST** /v1/auth/login | Exchange credentials for a token pair
[**logout**](AuthApi.md#logout) | **POST** /v1/auth/logout | Revoke the current session
[**refreshToken**](AuthApi.md#refreshtoken) | **POST** /v1/auth/refresh | Exchange a refresh token for a new token pair
[**registerUser**](AuthApi.md#registeruser) | **POST** /v1/auth/register | Register a new account


# **getCurrentUser**
> User getCurrentUser()

Read the caller's own account

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getAuthApi();

try {
    final response = api.getCurrentUser();
    print(response);
} on DioException catch (e) {
    print('Exception when calling AuthApi->getCurrentUser: $e\n');
}
```

### Parameters
This endpoint does not need any parameter.

### Return type

[**User**](User.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **login**
> TokenPair login(loginRequest)

Exchange credentials for a token pair

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getAuthApi();
final LoginRequest loginRequest = ; // LoginRequest | 

try {
    final response = api.login(loginRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling AuthApi->login: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **loginRequest** | [**LoginRequest**](LoginRequest.md)|  | 

### Return type

[**TokenPair**](TokenPair.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **logout**
> logout(refreshRequest)

Revoke the current session

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getAuthApi();
final RefreshRequest refreshRequest = ; // RefreshRequest | Omit the body to revoke the session named by the httpOnly cookie.

try {
    api.logout(refreshRequest);
} on DioException catch (e) {
    print('Exception when calling AuthApi->logout: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **refreshRequest** | [**RefreshRequest**](RefreshRequest.md)| Omit the body to revoke the session named by the httpOnly cookie. | [optional] 

### Return type

void (empty response body)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **refreshToken**
> TokenPair refreshToken(refreshRequest)

Exchange a refresh token for a new token pair

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getAuthApi();
final RefreshRequest refreshRequest = ; // RefreshRequest | This endpoint accepts the refresh token via either form. Send it as `refreshToken` in this JSON body — the path for clients with no cookie jar, such as Flutter, backed by platform secure storage — or omit the body entirely to fall back to the httpOnly `refreshToken` cookie (the browser path). One endpoint serves both kinds of client.

try {
    final response = api.refreshToken(refreshRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling AuthApi->refreshToken: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **refreshRequest** | [**RefreshRequest**](RefreshRequest.md)| This endpoint accepts the refresh token via either form. Send it as `refreshToken` in this JSON body — the path for clients with no cookie jar, such as Flutter, backed by platform secure storage — or omit the body entirely to fall back to the httpOnly `refreshToken` cookie (the browser path). One endpoint serves both kinds of client. | [optional] 

### Return type

[**TokenPair**](TokenPair.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **registerUser**
> User registerUser(registerRequest)

Register a new account

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getAuthApi();
final RegisterRequest registerRequest = ; // RegisterRequest | 

try {
    final response = api.registerUser(registerRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling AuthApi->registerUser: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **registerRequest** | [**RegisterRequest**](RegisterRequest.md)|  | 

### Return type

[**User**](User.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

