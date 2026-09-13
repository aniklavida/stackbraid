//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'token_pair.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class TokenPair {
  /// Returns a new [TokenPair] instance.
  TokenPair({

    required  this.accessToken,

    required  this.refreshToken,

    required  this.tokenType,

    required  this.expiresAt,
  });

  @JsonKey(
    
    name: r'accessToken',
    required: true,
    includeIfNull: false,
  )


  final String accessToken;



  @JsonKey(
    
    name: r'refreshToken',
    required: true,
    includeIfNull: false,
  )


  final String refreshToken;



  @JsonKey(
    
    name: r'tokenType',
    required: true,
    includeIfNull: false,
  )


  final TokenPairTokenTypeEnum tokenType;



      /// Expiry of `accessToken` (not the refresh token).
  @JsonKey(
    
    name: r'expiresAt',
    required: true,
    includeIfNull: false,
  )


  final DateTime expiresAt;





    @override
    bool operator ==(Object other) => identical(this, other) || other is TokenPair &&
      other.accessToken == accessToken &&
      other.refreshToken == refreshToken &&
      other.tokenType == tokenType &&
      other.expiresAt == expiresAt;

    @override
    int get hashCode =>
        accessToken.hashCode +
        refreshToken.hashCode +
        tokenType.hashCode +
        expiresAt.hashCode;

  factory TokenPair.fromJson(Map<String, dynamic> json) => _$TokenPairFromJson(json);

  Map<String, dynamic> toJson() => _$TokenPairToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

enum TokenPairTokenTypeEnum {
@JsonValue(r'Bearer')
bearer(r'Bearer');

const TokenPairTokenTypeEnum(this.value);

final String value;

@override
String toString() => value;
}


