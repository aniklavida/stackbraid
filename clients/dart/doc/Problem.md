# stackbraid_client.model.Problem

## Load the model package
```dart
import 'package:stackbraid_client/api.dart';
```

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**type** | **String** | A stable identifier for this problem type. `about:blank` when no more specific type applies. | [default to 'about:blank']
**title** | **String** | A short, human-readable summary, constant for a given `type`. | 
**status** | **int** | The HTTP status code, repeated here so it survives proxies that only pass the body along. | 
**detail** | **String** | A human-readable explanation specific to this occurrence. | [optional] 
**instance** | **String** | The request path that produced this problem. | [optional] 
**code** | **String** | A stable, machine-readable application error code, e.g. `IDENTITY.INVALID_CREDENTIALS`. Stable across locales and across both backends; `title` and `detail` are not (they are localized). | [optional] 
**traceId** | **String** | Correlation ID for this request, matching the one in structured logs. | [optional] 
**errors** | [**Map&lt;String, List&lt;String&gt;&gt;**](List.md) | Present only for validation problems (`status` 400). Maps a field name to its violation messages. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


