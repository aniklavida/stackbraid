import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/config/app_config.dart';
import '../../../shared/i18n/app_localizations.dart';
import '../../../shared/i18n/date_format.dart';
import '../../../shared/i18n/locale_controller.dart';
import '../../../shared/widgets/language_switcher.dart';
import '../application/notifications_controller.dart';
import '../application/use_cases.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.useCases,
    required this.session,
    required this.localeController,
    this.notificationsController,
  });

  final AuthUseCases useCases;
  final SessionController session;
  final LocaleController localeController;
  final NotificationsController? notificationsController;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  late final NotificationsController _notificationsController;
  late final bool _ownsController;
  bool _signingOut = false;

  @override
  void initState() {
    super.initState();
    if (widget.notificationsController != null) {
      _notificationsController = widget.notificationsController!;
      _ownsController = false;
    } else {
      _notificationsController = NotificationsController();
      _ownsController = true;
    }

    final token = widget.session.accessToken;
    if (token != null) {
      _notificationsController.start(
        httpBaseUrl: AppConfig.instance.apiBaseUrl,
        accessToken: token,
      );
    }
  }

  @override
  void dispose() {
    if (_ownsController) {
      _notificationsController.dispose();
    }
    super.dispose();
  }

  Future<void> _signOut() async {
    setState(() => _signingOut = true);
    final refreshToken = await widget.session.readPersistedRefreshToken();
    await widget.useCases.signOut(refreshToken, widget.session);
    // No further setState: SessionController.clearSession() flips status to
    // anonymous and this screen is about to be replaced by the login screen
    // via the ListenableBuilder in app.dart.
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.session.user;
    if (user == null) {
      // Defensive only: app.dart never builds this screen unless
      // status == authenticated, which always carries a user.
      return const SizedBox.shrink();
    }

    return Scaffold(
      appBar: AppBar(title: Text(context.t('auth.profileTitle'))),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            CircleAvatar(
              radius: 32,
              child: Text(
                user.displayName.isEmpty ? '?' : user.displayName.characters.first.toUpperCase(),
                style: const TextStyle(fontSize: 24),
              ),
            ),
            const SizedBox(height: 16),
            Text(user.displayName, style: Theme.of(context).textTheme.headlineSmall, textAlign: TextAlign.center),
            const SizedBox(height: 4),
            Text(user.email, textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 24),
            _InfoRow(
              label: context.t('auth.memberSince'),
              value: formatLongDate(user.createdAt, AppLocalizations.of(context).locale),
            ),
            const SizedBox(height: 8),
            Text(context.t('auth.rolesLabel'), style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            if (user.roles.isEmpty)
              Text(context.t('auth.noRoles'))
            else
              Wrap(
                spacing: 8,
                children: user.roles.map((role) => Chip(label: Text(role.name))).toList(),
              ),
            const SizedBox(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(context.t('auth.notificationsTitle'), style: Theme.of(context).textTheme.titleMedium),
                ListenableBuilder(
                  listenable: _notificationsController,
                  builder: (context, _) {
                    final isConnected = _notificationsController.isConnected;
                    return Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: isConnected
                            ? Theme.of(context).colorScheme.primaryContainer
                            : Theme.of(context).colorScheme.errorContainer,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Container(
                            width: 8,
                            height: 8,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              color: isConnected
                                  ? Theme.of(context).colorScheme.primary
                                  : Theme.of(context).colorScheme.error,
                            ),
                          ),
                          const SizedBox(width: 6),
                          Text(
                            isConnected
                                ? context.t('auth.notificationsLive')
                                : context.t('auth.notificationsPaused'),
                            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                                  color: isConnected
                                      ? Theme.of(context).colorScheme.onPrimaryContainer
                                      : Theme.of(context).colorScheme.onErrorContainer,
                                  fontWeight: FontWeight.w600,
                                ),
                          ),
                        ],
                      ),
                    );
                  },
                ),
              ],
            ),
            const SizedBox(height: 8),
            ListenableBuilder(
              listenable: _notificationsController,
              builder: (context, _) {
                final notifications = _notificationsController.notifications;
                if (notifications.isEmpty) {
                  return Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Text(
                        context.t('auth.notificationsEmpty'),
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: Theme.of(context).colorScheme.onSurfaceVariant,
                            ),
                      ),
                    ),
                  );
                }

                return Card(
                  child: ListView.separated(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: notifications.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final n = notifications[index];
                      final titleKey = n.type == 'user.deactivated'
                          ? 'auth.userDeactivated'
                          : 'auth.userRoleChanged';
                      final timeStr =
                          '${n.occurredAt.toLocal().hour.toString().padLeft(2, '0')}:${n.occurredAt.toLocal().minute.toString().padLeft(2, '0')}:${n.occurredAt.toLocal().second.toString().padLeft(2, '0')}';
                      return ListTile(
                        dense: true,
                        leading: Icon(
                          n.type == 'user.deactivated' ? Icons.person_off : Icons.badge,
                          color: Theme.of(context).colorScheme.primary,
                        ),
                        title: Text(context.t(titleKey)),
                        subtitle: Text(n.detail),
                        trailing: Text(
                          timeStr,
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: Theme.of(context).colorScheme.onSurfaceVariant,
                              ),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
            const SizedBox(height: 24),
            Text(context.t('common.language'), style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            LanguageSwitcher(controller: widget.localeController),
            const SizedBox(height: 32),
            OutlinedButton(
              key: const Key('profile-sign-out'),
              onPressed: _signingOut ? null : _signOut,
              child: _signingOut
                  ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(context.t('auth.signOutButton')),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: Theme.of(context).textTheme.bodyMedium),
        Text(value, style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
      ],
    );
  }
}
