// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'token_pair.dart';

// **************************************************************************
// CopyWithGenerator
// **************************************************************************

abstract class _$TokenPairCWProxy {
  TokenPair accessToken(String accessToken);

  TokenPair refreshToken(String refreshToken);

  TokenPair tokenType(TokenPairTokenTypeEnum tokenType);

  TokenPair expiresAt(DateTime expiresAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `TokenPair(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// TokenPair(...).copyWith(id: 12, name: "My name")
  /// ```
  TokenPair call({
    String accessToken,
    String refreshToken,
    TokenPairTokenTypeEnum tokenType,
    DateTime expiresAt,
  });
}

/// Callable proxy for `copyWith` functionality.
/// Use as `instanceOfTokenPair.copyWith(...)` or call `instanceOfTokenPair.copyWith.fieldName(value)` for a single field.
class _$TokenPairCWProxyImpl implements _$TokenPairCWProxy {
  const _$TokenPairCWProxyImpl(this._value);

  final TokenPair _value;

  @override
  TokenPair accessToken(String accessToken) => call(accessToken: accessToken);

  @override
  TokenPair refreshToken(String refreshToken) =>
      call(refreshToken: refreshToken);

  @override
  TokenPair tokenType(TokenPairTokenTypeEnum tokenType) =>
      call(tokenType: tokenType);

  @override
  TokenPair expiresAt(DateTime expiresAt) => call(expiresAt: expiresAt);

  /// Creates a new instance with the provided field values.
  /// Passing `null` to a nullable field nullifies it, while `null` for a non-nullable field is ignored. To update a single field use `TokenPair(...).copyWith.fieldName(value)`.
  ///
  /// Example:
  /// ```dart
  /// TokenPair(...).copyWith(id: 12, name: "My name")
  /// ```
  @override
  TokenPair call({
    Object? accessToken = const $CopyWithPlaceholder(),
    Object? refreshToken = const $CopyWithPlaceholder(),
    Object? tokenType = const $CopyWithPlaceholder(),
    Object? expiresAt = const $CopyWithPlaceholder(),
  }) {
    return TokenPair(
      accessToken:
          accessToken == const $CopyWithPlaceholder() || accessToken == null
          ? _value.accessToken
          // ignore: cast_nullable_to_non_nullable
          : accessToken as String,
      refreshToken:
          refreshToken == const $CopyWithPlaceholder() || refreshToken == null
          ? _value.refreshToken
          // ignore: cast_nullable_to_non_nullable
          : refreshToken as String,
      tokenType: tokenType == const $CopyWithPlaceholder() || tokenType == null
          ? _value.tokenType
          // ignore: cast_nullable_to_non_nullable
          : tokenType as TokenPairTokenTypeEnum,
      expiresAt: expiresAt == const $CopyWithPlaceholder() || expiresAt == null
          ? _value.expiresAt
          // ignore: cast_nullable_to_non_nullable
          : expiresAt as DateTime,
    );
  }
}

extension $TokenPairCopyWith on TokenPair {
  /// Returns a callable class used to build a new instance with modified fields.
  /// Example: `instanceOfTokenPair.copyWith(...)` or `instanceOfTokenPair.copyWith.fieldName(...)`.
  // ignore: library_private_types_in_public_api
  _$TokenPairCWProxy get copyWith => _$TokenPairCWProxyImpl(this);
}

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TokenPair _$TokenPairFromJson(Map<String, dynamic> json) =>
    $checkedCreate('TokenPair', json, ($checkedConvert) {
      $checkKeys(
        json,
        requiredKeys: const [
          'accessToken',
          'refreshToken',
          'tokenType',
          'expiresAt',
        ],
      );
      final val = TokenPair(
        accessToken: $checkedConvert('accessToken', (v) => v as String),
        refreshToken: $checkedConvert('refreshToken', (v) => v as String),
        tokenType: $checkedConvert(
          'tokenType',
          (v) => $enumDecode(_$TokenPairTokenTypeEnumEnumMap, v),
        ),
        expiresAt: $checkedConvert(
          'expiresAt',
          (v) => DateTime.parse(v as String),
        ),
      );
      return val;
    });

Map<String, dynamic> _$TokenPairToJson(TokenPair instance) => <String, dynamic>{
  'accessToken': instance.accessToken,
  'refreshToken': instance.refreshToken,
  'tokenType': _$TokenPairTokenTypeEnumEnumMap[instance.tokenType]!,
  'expiresAt': instance.expiresAt.toIso8601String(),
};

const _$TokenPairTokenTypeEnumEnumMap = {
  TokenPairTokenTypeEnum.bearer: 'Bearer',
};
