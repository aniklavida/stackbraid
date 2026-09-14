import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/i18n/app_localizations.dart';
import '../../../shared/i18n/date_format.dart';
import '../../../shared/i18n/locale_controller.dart';
import '../../../shared/widgets/language_switcher.dart';
import '../application/use_cases.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.useCases,
    required this.session,
    required this.localeController,
  });

  final AuthUseCases useCases;
  final SessionController session;
  final LocaleController localeController;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  bool _signingOut = false;

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
