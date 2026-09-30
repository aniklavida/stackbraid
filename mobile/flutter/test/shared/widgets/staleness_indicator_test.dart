import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:stackbraid_mobile/shared/http/network_status.dart';
import 'package:stackbraid_mobile/shared/http/offline_queue.dart';
import 'package:stackbraid_mobile/shared/http/staleness_controller.dart';
import 'package:stackbraid_mobile/shared/widgets/staleness_indicator.dart';

void main() {
  group('StalenessIndicator widget', () {
    testWidgets('hidden when online and no pending writes (fresh state)', (tester) async {
      final networkStatus = NetworkStatus(initialOnline: true);
      final offlineQueue = OfflineQueue();
      final controller = StalenessController(
        networkStatus: networkStatus,
        offlineQueue: offlineQueue,
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StalenessIndicator(controller: controller),
          ),
        ),
      );

      expect(find.byKey(const Key('staleness-indicator')), findsNothing);
    });

    testWidgets('visibly shows staleness state when offline with cached data', (tester) async {
      final networkStatus = NetworkStatus(initialOnline: false);
      final offlineQueue = OfflineQueue();
      final controller = StalenessController(
        networkStatus: networkStatus,
        offlineQueue: offlineQueue,
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StalenessIndicator(controller: controller),
          ),
        ),
      );

      expect(find.byKey(const Key('staleness-indicator')), findsOneWidget);
      expect(find.textContaining('Offline — showing cached data'), findsOneWidget);
      expect(find.byIcon(Icons.cloud_off), findsOneWidget);
    });

    testWidgets('visibly shows count of queued pending writes when offline', (tester) async {
      final networkStatus = NetworkStatus(initialOnline: false);
      final offlineQueue = OfflineQueue();
      final controller = StalenessController(
        networkStatus: networkStatus,
        offlineQueue: offlineQueue,
      );

      offlineQueue.enqueue(method: 'POST', path: '/v1/items', data: {'a': 1});
      offlineQueue.enqueue(method: 'PUT', path: '/v1/items/1', data: {'b': 2});

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StalenessIndicator(controller: controller),
          ),
        ),
      );

      expect(find.byKey(const Key('staleness-indicator')), findsOneWidget);
      expect(find.textContaining('Offline — 2 changes queued for sync'), findsOneWidget);
      expect(find.byIcon(Icons.cloud_queue), findsOneWidget);
    });

    testWidgets('transitions dynamically from stale to fresh when reconnected and cleared', (tester) async {
      final networkStatus = NetworkStatus(initialOnline: false);
      final offlineQueue = OfflineQueue();
      final controller = StalenessController(
        networkStatus: networkStatus,
        offlineQueue: offlineQueue,
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StalenessIndicator(controller: controller),
          ),
        ),
      );

      expect(find.byKey(const Key('staleness-indicator')), findsOneWidget);

      // Reconnect
      networkStatus.setOnline(true);
      await tester.pump();

      // Now online and queue empty -> hidden
      expect(find.byKey(const Key('staleness-indicator')), findsNothing);
    });
  });
}
