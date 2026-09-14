import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/errors/api_error.dart';
import '../../../shared/i18n/app_localizations.dart';
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
      // Registration alone issues no session — the contract's own rule
      // (`POST /v1/auth/register` returns the created `User`, not a
      // `TokenPair`). Signing the new account in right after, with the same
      // credentials this form already collected, is this app's UX choice,
      // matching both web frontends exactly — see
      // `application/use_cases.dart`.
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
    return Scaffold(
      appBar: AppBar(title: Text(context.t('auth.registerTitle'))),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                key: const Key('register-display-name'),
                controller: _displayNameController,
                decoration: InputDecoration(
                  labelText: context.t('auth.displayNameLabel'),
                  errorText: _fieldErrors['displayName'] == null
                      ? null
                      : context.t(authFieldErrorKey(_fieldErrors['displayName']!)),
                ),
              ),
              const SizedBox(height: 16),
              TextField(
                key: const Key('register-email'),
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: InputDecoration(
                  labelText: context.t('auth.emailLabel'),
                  errorText:
                      _fieldErrors['email'] == null ? null : context.t(authFieldErrorKey(_fieldErrors['email']!)),
                ),
              ),
              const SizedBox(height: 16),
              TextField(
                key: const Key('register-password'),
                controller: _passwordController,
                obscureText: true,
                decoration: InputDecoration(
                  labelText: context.t('auth.passwordLabel'),
                  errorText: _fieldErrors['password'] == null
                      ? null
                      : context.t(authFieldErrorKey(_fieldErrors['password']!)),
                ),
              ),
              const SizedBox(height: 24),
              if (_submitError != null) ...[
                Text(_submitError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                const SizedBox(height: 12),
              ],
              FilledButton(
                key: const Key('register-submit'),
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(context.t('auth.createAccountButton')),
              ),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(context.t('auth.haveAccountPrompt')),
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
    );
  }
}
