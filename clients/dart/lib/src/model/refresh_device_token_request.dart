//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'refresh_device_token_request.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class RefreshDeviceTokenRequest {
  /// Returns a new [RefreshDeviceTokenRequest] instance.
  RefreshDeviceTokenRequest({

    required  this.token,
  });

  @JsonKey(
    
    name: r'token',
    required: true,
    includeIfNull: false,
  )


  final String token;





    @override
    bool operator ==(Object other) => identical(this, other) || other is RefreshDeviceTokenRequest &&
      other.token == token;

    @override
    int get hashCode =>
        token.hashCode;

  factory RefreshDeviceTokenRequest.fromJson(Map<String, dynamic> json) => _$RefreshDeviceTokenRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RefreshDeviceTokenRequestToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

