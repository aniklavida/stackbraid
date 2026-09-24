//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/device_platform.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'device_token.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class DeviceToken {
  /// Returns a new [DeviceToken] instance.
  DeviceToken({

    required  this.id,

    required  this.platform,

    required  this.appVersion,

    required  this.registeredAt,

    required  this.updatedAt,
  });

  @JsonKey(
    
    name: r'id',
    required: true,
    includeIfNull: false,
  )


  final String id;



  @JsonKey(
    
    name: r'platform',
    required: true,
    includeIfNull: false,
  )


  final DevicePlatform platform;



  @JsonKey(
    
    name: r'appVersion',
    required: true,
    includeIfNull: false,
  )


  final String appVersion;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'registeredAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime registeredAt;



      /// RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
  @JsonKey(
    
    name: r'updatedAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime updatedAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is DeviceToken &&
      other.id == id &&
      other.platform == platform &&
      other.appVersion == appVersion &&
      other.registeredAt == registeredAt &&
      other.updatedAt == updatedAt;

    @override
    int get hashCode =>
        id.hashCode +
        platform.hashCode +
        appVersion.hashCode +
        registeredAt.hashCode +
        updatedAt.hashCode;

  factory DeviceToken.fromJson(Map<String, dynamic> json) => _$DeviceTokenFromJson(json);

  Map<String, dynamic> toJson() => _$DeviceTokenToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

