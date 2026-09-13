# stackbraid_client.api.RolesApi

## Load the API package
```dart
import 'package:stackbraid_client/api.dart';
```

All URIs are relative to *http://localhost:8080*

Method | HTTP request | Description
------------- | ------------- | -------------
[**assignRole**](RolesApi.md#assignrole) | **POST** /v1/users/{userId}/roles | Assign a role to a user
[**listRoles**](RolesApi.md#listroles) | **GET** /v1/roles | List every role
[**revokeRole**](RolesApi.md#revokerole) | **DELETE** /v1/users/{userId}/roles/{roleId} | Revoke a role from a user


# **assignRole**
> User assignRole(userId, assignRoleRequest)

Assign a role to a user

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getRolesApi();
final String userId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 
final AssignRoleRequest assignRoleRequest = ; // AssignRoleRequest | 

try {
    final response = api.assignRole(userId, assignRoleRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling RolesApi->assignRole: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **userId** | **String**|  | 
 **assignRoleRequest** | [**AssignRoleRequest**](AssignRoleRequest.md)|  | 

### Return type

[**User**](User.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **listRoles**
> List<Role> listRoles()

List every role

Not paginated. Roles are an administrator-curated catalogue, not user-generated data — the count stays small by design, so this returns the full list rather than a `Page<Role>`. 

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getRolesApi();

try {
    final response = api.listRoles();
    print(response);
} on DioException catch (e) {
    print('Exception when calling RolesApi->listRoles: $e\n');
}
```

### Parameters
This endpoint does not need any parameter.

### Return type

[**List&lt;Role&gt;**](Role.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **revokeRole**
> revokeRole(userId, roleId)

Revoke a role from a user

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getRolesApi();
final String userId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 
final String roleId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 

try {
    api.revokeRole(userId, roleId);
} on DioException catch (e) {
    print('Exception when calling RolesApi->revokeRole: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **userId** | **String**|  | 
 **roleId** | **String**|  | 

### Return type

void (empty response body)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

