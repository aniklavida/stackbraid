class RealtimeNotification {
  const RealtimeNotification({
    required this.id,
    required this.type,
    required this.occurredAt,
    required this.detail,
  });

  final String id;
  final String type;
  final DateTime occurredAt;
  final String detail;
}
