//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'problem.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class Problem {
  /// Returns a new [Problem] instance.
  Problem({

     this.type = 'about:blank',

    required  this.title,

    required  this.status,

     this.detail,

     this.instance,

     this.code,

     this.traceId,

     this.errors,
  });

      /// A stable identifier for this problem type. `about:blank` when no more specific type applies.
  @JsonKey(
    defaultValue: 'about:blank',
    name: r'type',
    required: true,
    includeIfNull: false,
  )


  final String type;



      /// A short, human-readable summary, constant for a given `type`.
  @JsonKey(
    
    name: r'title',
    required: true,
    includeIfNull: false,
  )


  final String title;



      /// The HTTP status code, repeated here so it survives proxies that only pass the body along.
  @JsonKey(
    
    name: r'status',
    required: true,
    includeIfNull: false,
  )


  final int status;



      /// A human-readable explanation specific to this occurrence.
  @JsonKey(
    
    name: r'detail',
    required: false,
    includeIfNull: false,
  )


  final String? detail;



      /// The request path that produced this problem.
  @JsonKey(
    
    name: r'instance',
    required: false,
    includeIfNull: false,
  )


  final String? instance;



      /// A stable, machine-readable application error code, e.g. `IDENTITY.INVALID_CREDENTIALS`. Stable across locales and across both backends; `title` and `detail` are not (they are localized).
  @JsonKey(
    
    name: r'code',
    required: false,
    includeIfNull: false,
  )


  final String? code;



      /// Correlation ID for this request, matching the one in structured logs.
  @JsonKey(
    
    name: r'traceId',
    required: false,
    includeIfNull: false,
  )


  final String? traceId;



      /// Present only for validation problems (`status` 400). Maps a field name to its violation messages.
  @JsonKey(
    
    name: r'errors',
    required: false,
    includeIfNull: false,
  )


  final Map<String, List<String>>? errors;





    @override
    bool operator ==(Object other) => identical(this, other) || other is Problem &&
      other.type == type &&
      other.title == title &&
      other.status == status &&
      other.detail == detail &&
      other.instance == instance &&
      other.code == code &&
      other.traceId == traceId &&
      other.errors == errors;

    @override
    int get hashCode =>
        type.hashCode +
        title.hashCode +
        status.hashCode +
        detail.hashCode +
        instance.hashCode +
        code.hashCode +
        traceId.hashCode +
        errors.hashCode;

  factory Problem.fromJson(Map<String, dynamic> json) => _$ProblemFromJson(json);

  Map<String, dynamic> toJson() => _$ProblemToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

