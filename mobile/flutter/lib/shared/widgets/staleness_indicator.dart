import 'package:flutter/material.dart';

import '../http/staleness_controller.dart';
import '../i18n/app_localizations.dart';

/// A banner widget that visibly displays the staleness state of data.
///
/// Per specification: never present stale data as current.
/// If offline or if pending writes are queued, a visible indicator warns
/// the user that the displayed information is cached or un-synchronized.
class StalenessIndicator extends StatelessWidget {
  const StalenessIndicator({
    super.key,
    required this.controller,
  });

  final StalenessController controller;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        if (!controller.isStale) {
          return const SizedBox.shrink();
        }

        final theme = Theme.of(context);
        final isOffline = controller.isOffline;
        final pendingCount = controller.pendingWritesCount;
        final isReplaying = controller.isReplaying;

        String message;
        IconData icon;

        if (isReplaying) {
          message = _translate(context, 'common.syncing', 'Syncing queued changes…');
          icon = Icons.sync;
        } else if (isOffline) {
          if (pendingCount > 0) {
            final template = _translate(
              context,
              'common.offlinePending',
              'Offline — $pendingCount changes queued for sync',
            );
            message = template.replaceAll('{count}', pendingCount.toString());
            icon = Icons.cloud_queue;
          } else {
            message = _translate(
              context,
              'common.offlineCached',
              'Offline — showing cached data',
            );
            icon = Icons.cloud_off;
          }
        } else {
          message = _translate(
            context,
            'common.staleWarning',
            'Data may be out of date',
          );
          icon = Icons.update;
        }

        return Container(
          key: const Key('staleness-indicator'),
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          color: theme.colorScheme.errorContainer,
          child: Row(
            children: [
              Icon(
                icon,
                size: 18,
                color: theme.colorScheme.onErrorContainer,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  message,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onErrorContainer,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  String _translate(BuildContext context, String key, String fallback) {
    try {
      final loc = AppLocalizations.of(context);
      return loc.t(key);
    } catch (_) {
      return fallback;
    }
  }
}
