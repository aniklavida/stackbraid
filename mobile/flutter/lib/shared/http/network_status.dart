import 'package:flutter/foundation.dart';

/// Tracks whether the device has active network connectivity.
///
/// Can be toggled manually, updated via platform network listeners,
/// or flipped when HTTP calls fail with connection-level errors.
class NetworkStatus extends ChangeNotifier {
  NetworkStatus({bool initialOnline = true}) : _isOnline = initialOnline;

  bool _isOnline;

  bool get isOnline => _isOnline;

  void setOnline(bool online) {
    if (_isOnline == online) return;
    _isOnline = online;
    notifyListeners();
  }

  void markOffline() => setOnline(false);
  void markOnline() => setOnline(true);
}
