// Hand-written — unlike everything under `lib/src/`, which
// `.openapi-generator-ignore` does not exempt and openapi-generator owns
// entirely. OpenAPI has no concept of a push channel (see
// `contract/openapi.yaml`'s `x-realtime-channels` extension), so there is
// nothing for the generator to produce for one; this file lives at the
// package root, a path regeneration never writes to, the same way
// `pubspec.yaml`/`README.md` coexist with generated code without being
// generated themselves.
//
// Connects to whichever realtime transport the target backend actually
// speaks — a SignalR hub (.NET) or a native WebSocket (Python), the same
// two shapes `contract/conformance/src/realtime.mjs` probes. Uses
// `dart:io`'s `WebSocket` — available on every platform this client is
// actually exercised against today (macOS/iOS/Android/desktop; see
// `mobile/flutter/README.md`'s own platform note), not on Flutter web,
// which has no `dart:io`. A web-targeted realtime client would need a
// different socket implementation (`package:web`'s `WebSocket`) and is out
// of scope until Flutter web itself is.
//
// Deliberately does NOT deserialize through the generated `RealtimeMessage`
// class (`lib/src/model/realtime_message.dart`): openapi-generator's
// `oneOf` + discriminator handling flattens the three variants into one
// class requiring every field from all three at once, which cannot
// actually parse a single real message (a `user.deactivated` payload has
// no `roles`/`jobId`/`status`/`progress` to satisfy it). That is a
// generator limitation, not something to patch by hand-editing generated
// output — this module reads the wire discriminator itself and constructs
// the one concrete, correctly-generated variant class it names
// (`UserDeactivatedMessage`, `UserRoleChangedMessage`, `JobProgressMessage`
// — each generated with only its own fields).

import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:stackbraid_client/src/model/job_progress_message.dart';
import 'package:stackbraid_client/src/model/user_deactivated_message.dart';
import 'package:stackbraid_client/src/model/user_role_changed_message.dart';

const _recordSeparator = '\x1e';
const _openTimeout = Duration(seconds: 4);

enum RealtimeChannel { notifications, jobs }

enum RealtimeTransport { signalr, nativeWebSocket }

/// One of [UserDeactivatedMessage], [UserRoleChangedMessage] or
/// [JobProgressMessage] — match on type (`switch (message) { UserDeactivatedMessage m => ..., ... }`)
/// rather than on the generated (and, for this union, unusable) `RealtimeMessage` class.
typedef StackBraidRealtimeMessage = Object;

class RealtimeConnection {
  RealtimeConnection._(this._socket, this.transport, this._entityId);

  final WebSocket _socket;
  final RealtimeTransport transport;
  final String? _entityId;
  String _buffer = '';

  final _controller = StreamController<StackBraidRealtimeMessage>.broadcast();

  /// Every message this connection receives, in arrival order.
  Stream<StackBraidRealtimeMessage> get messages => _controller.stream;

  void _listen() {
    _socket.listen(
      (dynamic data) {
        final text = data is String ? data : utf8.decode(data as List<int>);
        if (transport == RealtimeTransport.signalr) {
          _buffer += text;
          var idx = _buffer.indexOf(_recordSeparator);
          while (idx != -1) {
            final raw = _buffer.substring(0, idx);
            _buffer = _buffer.substring(idx + 1);
            if (raw.isNotEmpty) _handleSignalRFrame(raw);
            idx = _buffer.indexOf(_recordSeparator);
          }
        } else {
          _dispatch(jsonDecode(text) as Map<String, dynamic>);
        }
      },
      onDone: _controller.close,
      onError: _controller.addError,
    );
  }

  void _handleSignalRFrame(String raw) {
    final parsed = jsonDecode(raw) as Map<String, dynamic>;
    // type 1 = Invocation. Every send from this project's own hubs
    // (`Host/Realtime/SignalRRealtimePublisher.cs`) targets "message";
    // anything else (6 = ping, 3 = completion, ...) is ignored.
    if (parsed['type'] == 1 && parsed['target'] == 'message') {
      final arguments = parsed['arguments'] as List<dynamic>?;
      if (arguments != null && arguments.isNotEmpty) {
        _dispatch(arguments[0] as Map<String, dynamic>);
      }
    }
  }

  void _dispatch(Map<String, dynamic> json) {
    switch (json['type']) {
      case 'user.deactivated':
        _controller.add(UserDeactivatedMessage.fromJson(json));
      case 'user.role_changed':
        _controller.add(UserRoleChangedMessage.fromJson(json));
      case 'job.progress':
        _controller.add(JobProgressMessage.fromJson(json));
    }
  }

  /// Notifications channel: unused. Jobs channel: starts the connected job
  /// (or the shared demo job — see `docs/SPEC.md` §13, "enough to prove the
  /// plumbing, not a chat product").
  void start() {
    if (transport == RealtimeTransport.signalr) {
      _socket.add(jsonEncode({
        'type': 1,
        'target': 'StartDemoJob',
        'arguments': [_entityId],
      }) + _recordSeparator);
    } else {
      _socket.add(jsonEncode({'action': 'start_demo_job'}));
    }
  }

  Future<void> close() => _socket.close();
}

Uri _toWsUri(String httpBaseUrl, String path, String accessToken) {
  final base = Uri.parse(httpBaseUrl);
  final resolved = base.resolve(path);
  return resolved.replace(
    scheme: resolved.scheme == 'https' ? 'wss' : 'ws',
    queryParameters: {...resolved.queryParameters, 'access_token': accessToken},
  );
}

Future<void> _performSignalRHandshake(WebSocket socket) async {
  socket.add('${jsonEncode({'protocol': 'json', 'version': 1})}$_recordSeparator');
  final completer = Completer<void>();
  late StreamSubscription<dynamic> subscription;
  var buffer = '';
  subscription = socket.listen(
    (dynamic data) {
      final text = data is String ? data : utf8.decode(data as List<int>);
      buffer += text;
      final idx = buffer.indexOf(_recordSeparator);
      if (idx == -1) return;
      final raw = buffer.substring(0, idx);
      final parsed = raw.isEmpty ? <String, dynamic>{} : jsonDecode(raw) as Map<String, dynamic>;
      subscription.cancel();
      if (parsed.containsKey('error')) {
        completer.completeError(StateError('SignalR handshake rejected: ${parsed['error']}'));
      } else {
        completer.complete();
      }
    },
    onError: completer.completeError,
    onDone: () {
      if (!completer.isCompleted) completer.completeError(StateError('socket closed during SignalR handshake'));
    },
  );
  return completer.future.timeout(_openTimeout);
}

/// @param httpBaseUrl The backend's own HTTP(S) base URL.
/// @param channel `notifications` (per-user) or `jobs` (per-job — pass [entityId]).
/// @param accessToken A browser/native `WebSocket` cannot set an `Authorization`
///   header, so both backends accept the bearer token as a query parameter
///   instead — see `contract/openapi.yaml`'s `x-realtime-channels`.
/// @param entityId The job id, required for [RealtimeChannel.jobs]; ignored otherwise.
Future<RealtimeConnection> connectRealtimeChannel(
  String httpBaseUrl,
  RealtimeChannel channel,
  String accessToken, {
  String? entityId,
}) async {
  final hubPath = channel == RealtimeChannel.notifications ? '/v1/hubs/notifications' : '/v1/hubs/jobs';
  final wsPath = channel == RealtimeChannel.notifications ? '/v1/ws/notifications' : '/v1/ws/jobs/$entityId';

  RealtimeTransport transport;
  WebSocket socket;
  try {
    socket = await WebSocket.connect(_toWsUri(httpBaseUrl, hubPath, accessToken).toString()).timeout(_openTimeout);
    await _performSignalRHandshake(socket);
    transport = RealtimeTransport.signalr;
  } catch (_) {
    socket = await WebSocket.connect(_toWsUri(httpBaseUrl, wsPath, accessToken).toString()).timeout(_openTimeout);
    transport = RealtimeTransport.nativeWebSocket;
  }

  if (transport == RealtimeTransport.signalr && channel == RealtimeChannel.jobs) {
    socket.add('${jsonEncode({
      'type': 1,
      'target': 'Subscribe',
      'arguments': [entityId],
    })}$_recordSeparator');
  }

  final connection = RealtimeConnection._(socket, transport, entityId);
  connection._listen();
  return connection;
}
