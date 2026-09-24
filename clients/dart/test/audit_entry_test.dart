import 'package:test/test.dart';
import 'package:stackbraid_client/stackbraid_client.dart';

// tests for AuditEntry
void main() {
  final AuditEntry? instance = /* AuditEntry(...) */ null;
  // TODO add properties to the entity

  group(AuditEntry, () {
    // String id
    test('to test the property `id`', () async {
      // TODO
    });

    // String entityType
    test('to test the property `entityType`', () async {
      // TODO
    });

    // String entityId
    test('to test the property `entityId`', () async {
      // TODO
    });

    // String action
    test('to test the property `action`', () async {
      // TODO
    });

    // String actorId
    test('to test the property `actorId`', () async {
      // TODO
    });

    // String correlationId
    test('to test the property `correlationId`', () async {
      // TODO
    });

    // RFC 3339, UTC, always suffixed `Z` — never a numeric offset such as `+00:00`. A spike caught one backend emitting the offset form while another emitted `Z` for the same instant; this pattern makes the offset form fail validation instead of merely looking inconsistent.
    // DateTime occurredAt
    test('to test the property `occurredAt`', () async {
      // TODO
    });

    // String details
    test('to test the property `details`', () async {
      // TODO
    });

  });
}
