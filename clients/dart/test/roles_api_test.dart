import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';


/// tests for RolesApi
void main() {
  final instance = StackbraidClient().getRolesApi();

  group(RolesApi, () {
    // Assign a role to a user
    //
    //Future<User> assignRole(String userId, AssignRoleRequest assignRoleRequest) async
    test('test assignRole', () async {
      // TODO
    });

    // List every role
    //
    // Not paginated. Roles are an administrator-curated catalogue, not user-generated data — the count stays small by design, so this returns the full list rather than a `Page<Role>`. 
    //
    //Future<List<Role>> listRoles() async
    test('test listRoles', () async {
      // TODO
    });

    // Revoke a role from a user
    //
    //Future revokeRole(String userId, String roleId) async
    test('test revokeRole', () async {
      // TODO
    });

  });
}
