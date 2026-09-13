//
// AUTO-GENERATED FILE, DO NOT MODIFY!
//

// ignore_for_file: unused_element
import 'package:copy_with_extension/copy_with_extension.dart';
import 'package:json_annotation/json_annotation.dart';

part 'page.g.dart';


@CopyWith()
@JsonSerializable(
  checked: true,
  createToJson: true,
  disallowUnrecognizedKeys: false,
  explicitToJson: true,
)
class Page {
  /// Returns a new [Page] instance.
  Page({

    required  this.page,

    required  this.pageSize,

    required  this.totalItems,

    required  this.totalPages,
  });

          // minimum: 1
  @JsonKey(
    
    name: r'page',
    required: true,
    includeIfNull: false,
  )


  final int page;



          // minimum: 1
  @JsonKey(
    
    name: r'pageSize',
    required: true,
    includeIfNull: false,
  )


  final int pageSize;



          // minimum: 0
  @JsonKey(
    
    name: r'totalItems',
    required: true,
    includeIfNull: false,
  )


  final int totalItems;



          // minimum: 0
  @JsonKey(
    
    name: r'totalPages',
    required: true,
    includeIfNull: false,
  )


  final int totalPages;





    @override
    bool operator ==(Object other) => identical(this, other) || other is Page &&
      other.page == page &&
      other.pageSize == pageSize &&
      other.totalItems == totalItems &&
      other.totalPages == totalPages;

    @override
    int get hashCode =>
        page.hashCode +
        pageSize.hashCode +
        totalItems.hashCode +
        totalPages.hashCode;

  factory Page.fromJson(Map<String, dynamic> json) => _$PageFromJson(json);

  Map<String, dynamic> toJson() => _$PageToJson(this);

  @override
  String toString() {
    return toJson().toString();
  }

}

