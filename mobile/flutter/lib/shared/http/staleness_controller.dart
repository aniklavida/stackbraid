import 'package:flutter/foundation.dart';

import 'network_status.dart';
import 'offline_queue.dart';

/// Coordinates visibility of stale state across the mobile application.
///
/// Per specification: never present stale data as current.
/// Data is considered stale when:
/// 1. The device is offline.
/// 2. Pending mutating writes exist in the offline queue waiting to sync.
class StalenessController extends ChangeNotifier {
  StalenessController({
    required this.networkStatus,
    required this.offlineQueue,
  }) {
    networkStatus.addListener(_onChanged);
    offlineQueue.addListener(_onChanged);
  }

  final NetworkStatus networkStatus;
  final OfflineQueue offlineQueue;

  bool get isOffline => !networkStatus.isOnline;
  bool get hasPendingWrites => offlineQueue.hasPendingWrites;
  int get pendingWritesCount => offlineQueue.pendingCount;
  bool get isReplaying => offlineQueue.isReplaying;

  /// Whether data displayed in the app should be flagged as potentially stale.
  bool get isStale => isOffline || hasPendingWrites;

  void _onChanged() {
    notifyListeners();
  }

  @override
  void dispose() {
    networkStatus.removeListener(_onChanged);
    offlineQueue.removeListener(_onChanged);
    super.dispose();
  }
}
