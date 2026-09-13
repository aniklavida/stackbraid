# stackbraid_client.model.RealtimeMessage

## Load the model package
```dart
import 'package:stackbraid_client/api.dart';
```

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**type** | **String** |  | 
**userId** | **String** |  | 
**occurredAt** | [**DateTime**](DateTime.md) | RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught .NET emitting the offset form while Python emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent. | 
**roles** | [**List&lt;Role&gt;**](Role.md) | The user's full role set after the change, not a diff. | 
**jobId** | **String** |  | 
**status** | **String** |  | 
**progress** | **int** |  | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


