//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/device_platform.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'register_device_token_request.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class RegisterDeviceTokenRequest {
  /// Returns a new [RegisterDeviceTokenRequest] instance.
  RegisterDeviceTokenRequest({

    required  this.token,

    required  this.platform,

     this.appVersion,
  });

  @JsonKey(
    
    name: r'token',
    required: true,
    includeIfNull: false,
  )


  final String token;



  @JsonKey(
    
    name: r'platform',
    required: true,
    includeIfNull: false,
  )


  final DevicePlatform platform;



  @JsonKey(
    
    name: r'appVersion',
    required: false,
    includeIfNull: false,
  )


  final String? appVersion;





    @override
    bool operator ==(Object other) => identical(this, other) || other is RegisterDeviceTokenRequest &&
      other.token == token &&
      other.platform == platform &&
      other.appVersion == appVersion;

    @override
    int get hashCode =>
        token.hashCode +
        platform.hashCode +
        appVersion.hashCode;

  factory RegisterDeviceTokenRequest.fromJson(Map<String, dynamic> json) => _$RegisterDeviceTokenRequestFromJson(json);

  Map<String, dynamic> toJson() => _$RegisterDeviceTokenRequestToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

