import 'package:stackbraid_client/src/model/assign_role_request.dart';
import 'package:stackbraid_client/src/model/job_progress_message.dart';
import 'package:stackbraid_client/src/model/login_request.dart';
import 'package:stackbraid_client/src/model/page.dart';
import 'package:stackbraid_client/src/model/problem.dart';
import 'package:stackbraid_client/src/model/realtime_message.dart';
import 'package:stackbraid_client/src/model/refresh_request.dart';
import 'package:stackbraid_client/src/model/register_request.dart';
import 'package:stackbraid_client/src/model/role.dart';
import 'package:stackbraid_client/src/model/token_pair.dart';
import 'package:stackbraid_client/src/model/update_user_request.dart';
import 'package:stackbraid_client/src/model/user.dart';
import 'package:stackbraid_client/src/model/user_deactivated_message.dart';
import 'package:stackbraid_client/src/model/user_page.dart';
import 'package:stackbraid_client/src/model/user_role_changed_message.dart';

final _regList = RegExp(r'^List<(.*)>$');
final _regSet = RegExp(r'^Set<(.*)>$');
final _regMap = RegExp(r'^Map<String,(.*)>$');

  ReturnType deserialize<ReturnType, BaseType>(dynamic value, String targetType, {bool growable= true}) {
      switch (targetType) {
        case 'String':
          return '$value' as ReturnType;
        case 'int':
          return (value is int ? value : int.parse('$value')) as ReturnType;
        case 'bool':
          if (value is bool) {
            return value as ReturnType;
          }
          final valueString = '$value'.toLowerCase();
          return (valueString == 'true' || valueString == '1') as ReturnType;
        case 'double':
          return (value is double ? value : double.parse('$value')) as ReturnType;
        case 'AssignRoleRequest':
          return AssignRoleRequest.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'JobProgressMessage':
          return JobProgressMessage.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'LoginRequest':
          return LoginRequest.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'Page':
          return Page.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'Problem':
          return Problem.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'RealtimeMessage':
          return RealtimeMessage.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'RefreshRequest':
          return RefreshRequest.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'RegisterRequest':
          return RegisterRequest.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'Role':
          return Role.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'TokenPair':
          return TokenPair.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'UpdateUserRequest':
          return UpdateUserRequest.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'User':
          return User.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'UserDeactivatedMessage':
          return UserDeactivatedMessage.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'UserPage':
          return UserPage.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'UserRoleChangedMessage':
          return UserRoleChangedMessage.fromJson(value as Map<String, dynamic>) as ReturnType;
        case 'UserStatus':
          
          
        default:
          RegExpMatch? match;

          if (value is List && (match = _regList.firstMatch(targetType)) != null) {
            targetType = match![1]!; // ignore: parameter_assignments
            return value
              .map<BaseType>((dynamic v) => deserialize<BaseType, BaseType>(v, targetType, growable: growable))
              .toList(growable: growable) as ReturnType;
          }
          if (value is Set && (match = _regSet.firstMatch(targetType)) != null) {
            targetType = match![1]!; // ignore: parameter_assignments
            return value
              .map<BaseType>((dynamic v) => deserialize<BaseType, BaseType>(v, targetType, growable: growable))
              .toSet() as ReturnType;
          }
          if (value is Map && (match = _regMap.firstMatch(targetType)) != null) {
            targetType = match![1]!.trim(); // ignore: parameter_assignments
            return Map<String, BaseType>.fromIterables(
              value.keys as Iterable<String>,
              value.values.map((dynamic v) => deserialize<BaseType, BaseType>(v, targetType, growable: growable)),
            ) as ReturnType;
          }
          break;
    }
    throw Exception('Cannot deserialize');
  }