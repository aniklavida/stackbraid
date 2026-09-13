# stackbraid_client.api.UsersApi

## Load the API package
```dart
import 'package:stackbraid_client/api.dart';
```

All URIs are relative to *http://localhost:8080*

Method | HTTP request | Description
------------- | ------------- | -------------
[**deactivateUser**](UsersApi.md#deactivateuser) | **POST** /v1/users/{userId}/deactivate | Deactivate a user
[**getUser**](UsersApi.md#getuser) | **GET** /v1/users/{userId} | Read a single user
[**listUsers**](UsersApi.md#listusers) | **GET** /v1/users | List users
[**updateUser**](UsersApi.md#updateuser) | **PATCH** /v1/users/{userId} | Update a user&#39;s profile


# **deactivateUser**
> User deactivateUser(userId)

Deactivate a user

Deactivation, not deletion — the account and its history are kept, but it can no longer authenticate. Idempotent: deactivating an already-inactive user returns the current state rather than an error. 

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getUsersApi();
final String userId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 

try {
    final response = api.deactivateUser(userId);
    print(response);
} on DioException catch (e) {
    print('Exception when calling UsersApi->deactivateUser: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **userId** | **String**|  | 

### Return type

[**User**](User.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **getUser**
> User getUser(userId)

Read a single user

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getUsersApi();
final String userId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 

try {
    final response = api.getUser(userId);
    print(response);
} on DioException catch (e) {
    print('Exception when calling UsersApi->getUser: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **userId** | **String**|  | 

### Return type

[**User**](User.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **listUsers**
> UserPage listUsers(page, pageSize, sort, search, status, roleId)

List users

Paginated, filtered and sorted. Requires the `users:read` permission.

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getUsersApi();
final int page = 56; // int | 1-based page number.
final int pageSize = 56; // int | Items per page.
final String sort = sort_example; // String | One sortable field, optionally prefixed with `-` for descending. Allowed fields: `email`, `displayName`, `createdAt`, `status`. Examples: `createdAt`, `-createdAt`. 
final String search = search_example; // String | Case-insensitive substring match against email and display name.
final UserStatus status = ; // UserStatus | Filter by account status.
final String roleId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | Filter to users holding this role.

try {
    final response = api.listUsers(page, pageSize, sort, search, status, roleId);
    print(response);
} on DioException catch (e) {
    print('Exception when calling UsersApi->listUsers: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **page** | **int**| 1-based page number. | [optional] [default to 1]
 **pageSize** | **int**| Items per page. | [optional] [default to 20]
 **sort** | **String**| One sortable field, optionally prefixed with `-` for descending. Allowed fields: `email`, `displayName`, `createdAt`, `status`. Examples: `createdAt`, `-createdAt`.  | [optional] [default to '-createdAt']
 **search** | **String**| Case-insensitive substring match against email and display name. | [optional] 
 **status** | [**UserStatus**](.md)| Filter by account status. | [optional] 
 **roleId** | **String**| Filter to users holding this role. | [optional] 

### Return type

[**UserPage**](UserPage.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **updateUser**
> User updateUser(userId, updateUserRequest)

Update a user's profile

Partial update. Role membership is managed through `/v1/users/{userId}/roles`, not through this endpoint.

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getUsersApi();
final String userId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 
final UpdateUserRequest updateUserRequest = ; // UpdateUserRequest | 

try {
    final response = api.updateUser(userId, updateUserRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling UsersApi->updateUser: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **userId** | **String**|  | 
 **updateUserRequest** | [**UpdateUserRequest**](UpdateUserRequest.md)|  | 

### Return type

[**User**](User.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

