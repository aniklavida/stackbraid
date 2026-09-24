# stackbraid_client.api.DevicesApi

## Load the API package
```dart
import 'package:stackbraid_client/api.dart';
```

All URIs are relative to *http://localhost:8080*

Method | HTTP request | Description
------------- | ------------- | -------------
[**refreshDeviceToken**](DevicesApi.md#refreshdevicetoken) | **PATCH** /v1/devices/{deviceId} | Refresh a device push token
[**registerDeviceToken**](DevicesApi.md#registerdevicetoken) | **POST** /v1/devices | Register a device push token
[**revokeDeviceToken**](DevicesApi.md#revokedevicetoken) | **DELETE** /v1/devices/{deviceId} | Revoke a device push token


# **refreshDeviceToken**
> DeviceToken refreshDeviceToken(deviceId, refreshDeviceTokenRequest)

Refresh a device push token

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getDevicesApi();
final String deviceId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 
final RefreshDeviceTokenRequest refreshDeviceTokenRequest = ; // RefreshDeviceTokenRequest | 

try {
    final response = api.refreshDeviceToken(deviceId, refreshDeviceTokenRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling DevicesApi->refreshDeviceToken: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **deviceId** | **String**|  | 
 **refreshDeviceTokenRequest** | [**RefreshDeviceTokenRequest**](RefreshDeviceTokenRequest.md)|  | 

### Return type

[**DeviceToken**](DeviceToken.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **registerDeviceToken**
> DeviceToken registerDeviceToken(registerDeviceTokenRequest)

Register a device push token

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getDevicesApi();
final RegisterDeviceTokenRequest registerDeviceTokenRequest = ; // RegisterDeviceTokenRequest | 

try {
    final response = api.registerDeviceToken(registerDeviceTokenRequest);
    print(response);
} on DioException catch (e) {
    print('Exception when calling DevicesApi->registerDeviceToken: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **registerDeviceTokenRequest** | [**RegisterDeviceTokenRequest**](RegisterDeviceTokenRequest.md)|  | 

### Return type

[**DeviceToken**](DeviceToken.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **revokeDeviceToken**
> revokeDeviceToken(deviceId)

Revoke a device push token

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getDevicesApi();
final String deviceId = 38400000-8cf0-11bd-b23e-10b96e4ef00d; // String | 

try {
    api.revokeDeviceToken(deviceId);
} on DioException catch (e) {
    print('Exception when calling DevicesApi->revokeDeviceToken: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **deviceId** | **String**|  | 

### Return type

void (empty response body)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

