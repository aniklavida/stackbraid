//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:stackbraid_client/src/model/audit_entry.dart';
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'audit_page.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class AuditPage {
  /// Returns a new [AuditPage] instance.
  AuditPage({

    required  this.items,
  });

  @JsonKey(
    
    name: r'items',
    required: true,
    includeIfNull: false,
  )


  final List<AuditEntry> items;





    @override
    bool operator ==(Object other) => identical(this, other) || other is AuditPage &&
      other.items == items;

    @override
    int get hashCode =>
        items.hashCode;

  factory AuditPage.fromJson(Map<String, dynamic> json) => _$AuditPageFromJson(json);

  Map<String, dynamic> toJson() => _$AuditPageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

