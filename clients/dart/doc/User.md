# stackbraid_client.model.User

## Load the model package
```dart
import 'package:stackbraid_client/api.dart';
```

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **String** |  | 
**email** | **String** |  | 
**displayName** | **String** |  | 
**status** | [**UserStatus**](UserStatus.md) |  | 
**roles** | [**List&lt;Role&gt;**](Role.md) |  | 
**createdAt** | [**DateTime**](DateTime.md) | RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent. | 
**updatedAt** | [**DateTime**](DateTime.md) | RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent. | 
**lastLoginAt** | [**DateTime**](DateTime.md) | Same rule as `UtcDateTime`; `null` means the event has not happened yet. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


