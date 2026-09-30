import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/errors/api_error.dart';
import '../../../shared/i18n/app_localizations.dart';
import '../../../shared/theme/tokens.dart';
import '../application/use_cases.dart';
import '../domain/validation.dart';
import 'i18n/auth_strings.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key, required this.useCases, required this.session});

  final AuthUseCases useCases;
  final SessionController session;

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _displayNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  FieldErrors _fieldErrors = {};
  String? _submitError;
  bool _submitting = false;

  @override
  void dispose() {
    _displayNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _submitting = true;
      _submitError = null;
      _fieldErrors = {};
    });
    try {
      await widget.useCases.registerAccount(
        RegisterFormValues(
          email: _emailController.text.trim(),
          password: _passwordController.text,
          displayName: _displayNameController.text.trim(),
        ),
        widget.session,
      );
      if (mounted) Navigator.of(context).popUntil((route) => route.isFirst);
    } on ValidationFailed catch (e) {
      setState(() => _fieldErrors = e.fieldErrors);
    } catch (e) {
      setState(() => _submitError = describeApiError(e));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final width = MediaQuery.sizeOf(context).width;
    final horizontalPadding = width >= 430 ? AppTokens.screenMarginWide : AppTokens.screenMargin;

    return Scaffold(
      appBar: AppBar(
        title: Text(context.t('auth.registerTitle')),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: SingleChildScrollView(
              padding: EdgeInsets.symmetric(horizontal: horizontalPadding, vertical: 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Product wordmark
                  Text(
                    'StackBraid',
                    style: TextStyle(
                      fontFamily: AppTokens.fontDisplay,
                      fontSize: 26,
                      fontWeight: FontWeight.w600,
                      letterSpacing: -0.5,
                      color: theme.colorScheme.onSurface,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    context.t('auth.registerTitle'),
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 32),

                  // Display name field with label above
                  Text(
                    context.t('auth.displayNameLabel'),
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    key: const Key('register-display-name'),
                    controller: _displayNameController,
                    textInputAction: TextInputAction.next,
                    style: theme.textTheme.bodyLarge,
                    decoration: InputDecoration(
                      hintText: 'Jane Doe',
                      errorText: _fieldErrors['displayName'] == null
                          ? null
                          : context.t(authFieldErrorKey(_fieldErrors['displayName']!)),
                    ),
                  ),
                  const SizedBox(height: 18),

                  // Email field with label above
                  Text(
                    context.t('auth.emailLabel'),
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    key: const Key('register-email'),
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    textInputAction: TextInputAction.next,
                    style: theme.textTheme.bodyLarge,
                    decoration: InputDecoration(
                      hintText: 'name@example.com',
                      errorText: _fieldErrors['email'] == null
                          ? null
                          : context.t(authFieldErrorKey(_fieldErrors['email']!)),
                    ),
                  ),
                  const SizedBox(height: 18),

                  // Password field with label above
                  Text(
                    context.t('auth.passwordLabel'),
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    key: const Key('register-password'),
                    controller: _passwordController,
                    obscureText: true,
                    textInputAction: TextInputAction.done,
                    onSubmitted: (_) => _submit(),
                    style: theme.textTheme.bodyLarge,
                    decoration: InputDecoration(
                      hintText: '••••••••',
                      errorText: _fieldErrors['password'] == null
                          ? null
                          : context.t(authFieldErrorKey(_fieldErrors['password']!)),
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Submit error banner
                  if (_submitError != null) ...[
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.errorContainer,
                        borderRadius: BorderRadius.circular(AppTokens.rMd),
                        border: Border.all(color: theme.colorScheme.error.withValues(alpha: 0.3)),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.error_outline, size: 18, color: theme.colorScheme.error),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              _submitError!,
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: theme.colorScheme.onErrorContainer,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 18),
                  ],

                  // Primary action button (50px height, one per screen)
                  SizedBox(
                    height: AppTokens.buttonHeight,
                    child: FilledButton(
                      key: const Key('register-submit'),
                      onPressed: _submitting ? null : _submit,
                      child: _submitting
                          ? Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                SizedBox(
                                  height: 18,
                                  width: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: theme.colorScheme.onPrimary,
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Text(
                                  context.t('common.loading'),
                                  style: TextStyle(
                                    fontFamily: AppTokens.fontUi,
                                    fontSize: 16,
                                    fontWeight: FontWeight.w600,
                                    color: theme.colorScheme.onPrimary,
                                  ),
                                ),
                              ],
                            )
                          : Text(context.t('auth.createAccountButton')),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Secondary link in thumb zone
                  Wrap(
                    alignment: WrapAlignment.center,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(
                        context.t('auth.haveAccountPrompt'),
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                      TextButton(
                        key: const Key('register-go-login'),
                        onPressed: () => Navigator.of(context).pop(),
                        child: Text(context.t('auth.signInLink')),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
