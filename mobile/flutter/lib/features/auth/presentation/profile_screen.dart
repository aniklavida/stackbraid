import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/config/app_config.dart';
import '../../../shared/i18n/app_localizations.dart';
import '../../../shared/i18n/date_format.dart';
import '../../../shared/i18n/locale_controller.dart';
import '../../../shared/theme/theme_controller.dart';
import '../../../shared/theme/tokens.dart';
import '../../../shared/widgets/language_switcher.dart';
import '../../../shared/widgets/theme_mode_switcher.dart';
import '../application/notifications_controller.dart';
import '../application/use_cases.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.useCases,
    required this.session,
    required this.localeController,
    this.themeController,
    this.notificationsController,
  });

  final AuthUseCases useCases;
  final SessionController session;
  final LocaleController localeController;
  final ThemeController? themeController;
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
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= 430
        ? AppTokens.screenMarginWide
        : AppTokens.screenMargin;

    final user = widget.session.user;
    if (user == null) {
      return const SizedBox.shrink();
    }

    final initial = user.displayName.isEmpty
        ? '?'
        : user.displayName.characters.first.toUpperCase();

    return Scaffold(
      appBar: AppBar(title: Text(context.t('auth.profileTitle'))),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            // A plain scroll view + column (not a lazy ListView) so every
            // action, including sign out, is always built and reachable.
            child: SingleChildScrollView(
              padding: EdgeInsets.symmetric(
                horizontal: horizontalPadding,
                vertical: 20,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Identity card with avatar and member since
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        children: [
                          CircleAvatar(
                            radius: 36,
                            backgroundColor:
                                theme.colorScheme.surfaceContainerHighest,
                            child: Text(
                              initial,
                              style: TextStyle(
                                fontFamily: AppTokens.fontDisplay,
                                fontSize: 26,
                                fontWeight: FontWeight.w600,
                                color: theme.colorScheme.onSurface,
                              ),
                            ),
                          ),
                          const SizedBox(height: 12),
                          Text(
                            user.displayName,
                            style: theme.textTheme.headlineMedium?.copyWith(
                              fontWeight: FontWeight.w600,
                            ),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 4),
                          Text(
                            user.email,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 16),
                          const Divider(height: 1),
                          const SizedBox(height: 14),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(
                                context.t('auth.memberSince'),
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
                                ),
                              ),
                              const SizedBox(width: 8),
                              Flexible(
                                child: Text(
                                  formatLongDate(
                                    user.createdAt,
                                    AppLocalizations.of(context).locale,
                                  ),
                                  style: theme.textTheme.bodySmall?.copyWith(
                                    fontWeight: FontWeight.w500,
                                    color: theme.colorScheme.onSurface,
                                  ),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Roles section
                  _SectionHeader(title: context.t('auth.rolesLabel')),
                  const SizedBox(height: 8),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: user.roles.isEmpty
                          ? Text(
                              context.t('auth.noRoles'),
                              style: theme.textTheme.bodyMedium?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant,
                              ),
                            )
                          : Wrap(
                              spacing: 8,
                              runSpacing: 8,
                              children: user.roles.map((role) {
                                return Chip(label: Text(role.name));
                              }).toList(),
                            ),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Notifications section
                  Wrap(
                    alignment: WrapAlignment.spaceBetween,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    spacing: 8,
                    runSpacing: 6,
                    children: [
                      _SectionHeader(
                        title: context.t('auth.notificationsTitle'),
                      ),
                      ListenableBuilder(
                        listenable: _notificationsController,
                        builder: (context, _) {
                          final isConnected =
                              _notificationsController.isConnected;
                          final indicatorColor = isConnected
                              ? theme.colorScheme.primary
                              : theme.colorScheme.error;
                          return Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: isConnected
                                  ? theme.colorScheme.primaryContainer
                                  : theme.colorScheme.errorContainer,
                              borderRadius: BorderRadius.circular(
                                AppTokens.rPill,
                              ),
                              border: Border.all(
                                color: indicatorColor.withValues(alpha: 0.2),
                              ),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Container(
                                  width: 7,
                                  height: 7,
                                  decoration: BoxDecoration(
                                    shape: BoxShape.circle,
                                    color: indicatorColor,
                                  ),
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  isConnected
                                      ? context.t('auth.notificationsLive')
                                      : context.t('auth.notificationsPaused'),
                                  style: TextStyle(
                                    fontFamily: AppTokens.fontUi,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                    color: isConnected
                                        ? theme.colorScheme.onPrimaryContainer
                                        : theme.colorScheme.onErrorContainer,
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
                      final notifications =
                          _notificationsController.notifications;
                      if (notifications.isEmpty) {
                        return Card(
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Text(
                              context.t('auth.notificationsEmpty'),
                              style: theme.textTheme.bodyMedium?.copyWith(
                                color: theme.colorScheme.onSurfaceVariant,
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
                                n.type == 'user.deactivated'
                                    ? Icons.person_off_outlined
                                    : Icons.badge_outlined,
                                size: 20,
                                color: theme.colorScheme.primary,
                              ),
                              title: Text(context.t(titleKey)),
                              subtitle: Text(n.detail),
                              trailing: Text(
                                timeStr,
                                style: TextStyle(
                                  fontFamily: AppTokens.fontMono,
                                  fontSize: 12,
                                  color: theme.colorScheme.onSurfaceVariant,
                                ),
                              ),
                            );
                          },
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: 20),

                  // Preferences section: Language
                  _SectionHeader(title: context.t('common.language')),
                  const SizedBox(height: 8),
                  SizedBox(
                    width: double.infinity,
                    child: LanguageSwitcher(
                      controller: widget.localeController,
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Preferences section: Theme
                  if (widget.themeController != null) ...[
                    _SectionHeader(title: context.t('common.theme')),
                    const SizedBox(height: 8),
                    SizedBox(
                      width: double.infinity,
                      child: ThemeModeSwitcher(
                        controller: widget.themeController!,
                      ),
                    ),
                    const SizedBox(height: 24),
                  ],

                  // Sign out button (secondary, full width, 50px tall)
                  SizedBox(
                    height: AppTokens.buttonHeight,
                    child: OutlinedButton(
                      key: const Key('profile-sign-out'),
                      onPressed: _signingOut ? null : _signOut,
                      child: _signingOut
                          ? SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: theme.colorScheme.onSurface,
                              ),
                            )
                          : Text(context.t('auth.signOutButton')),
                    ),
                  ),
                  const SizedBox(height: 24),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Text(
      title,
      style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
    );
  }
}
