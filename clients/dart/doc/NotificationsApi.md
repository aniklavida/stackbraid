# stackbraid_client.api.NotificationsApi

## Load the API package
```dart
import 'package:stackbraid_client/api.dart';
```

All URIs are relative to *http://localhost:8080*

Method | HTTP request | Description
------------- | ------------- | -------------
[**listNotifications**](NotificationsApi.md#listnotifications) | **GET** /v1/notifications | List the authenticated user&#39;s in-app notifications


# **listNotifications**
> NotificationPage listNotifications(page, pageSize)

List the authenticated user's in-app notifications

### Example
```dart
import 'package:stackbraid_client/api.dart';

final api = StackbraidClient().getNotificationsApi();
final int page = 56; // int | 1-based page number.
final int pageSize = 56; // int | Items per page.

try {
    final response = api.listNotifications(page, pageSize);
    print(response);
} on DioException catch (e) {
    print('Exception when calling NotificationsApi->listNotifications: $e\n');
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **page** | **int**| 1-based page number. | [optional] [default to 1]
 **pageSize** | **int**| Items per page. | [optional] [default to 20]

### Return type

[**NotificationPage**](NotificationPage.md)

### Authorization

[bearerAuth](../README.md#bearerAuth)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

