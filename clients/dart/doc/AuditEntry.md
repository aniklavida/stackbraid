# stackbraid_client.model.AuditEntry

## Load the model package
```dart
import 'package:stackbraid_client/api.dart';
```

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **String** |  | 
**entityType** | **String** |  | 
**entityId** | **String** |  | 
**action** | **String** |  | 
**actorId** | **String** |  | [optional] 
**correlationId** | **String** |  | 
**occurredAt** | [**DateTime**](DateTime.md) | RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent. | 
**details** | **String** |  | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


