import 'package:flutter/material.dart';

import '../../../shared/auth/session_controller.dart';
import '../../../shared/errors/api_error.dart';
import '../../../shared/i18n/app_localizations.dart';
import '../application/use_cases.dart';
import '../domain/validation.dart';
import 'i18n/auth_strings.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({
    super.key,
    required this.useCases,
    required this.session,
    required this.onCreateAccount,
  });

  final AuthUseCases useCases;
  final SessionController session;
  final VoidCallback onCreateAccount;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  FieldErrors _fieldErrors = {};
  String? _submitError;
  bool _submitting = false;

  @override
  void dispose() {
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
      await widget.useCases.signIn(
        LoginFormValues(email: _emailController.text.trim(), password: _passwordController.text),
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
      appBar: AppBar(title: Text(context.t('auth.signInTitle'))),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                key: const Key('login-email'),
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: InputDecoration(
                  labelText: context.t('auth.emailLabel'),
                  errorText: _fieldErrors['email'] == null ? null : context.t(authFieldErrorKey(_fieldErrors['email']!)),
                ),
              ),
              const SizedBox(height: 16),
              TextField(
                key: const Key('login-password'),
                controller: _passwordController,
                obscureText: true,
                decoration: InputDecoration(
                  labelText: context.t('auth.passwordLabel'),
                  errorText: _fieldErrors['password'] == null ? null : context.t(authFieldErrorKey(_fieldErrors['password']!)),
                ),
              ),
              const SizedBox(height: 24),
              if (_submitError != null) ...[
                Text(_submitError!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                const SizedBox(height: 12),
              ],
              FilledButton(
                key: const Key('login-submit'),
                onPressed: _submitting ? null : _submit,
                child: _submitting
                    ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(context.t('auth.signInButton')),
              ),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(context.t('auth.noAccountPrompt')),
                  TextButton(
                    key: const Key('login-go-register'),
                    onPressed: widget.onCreateAccount,
                    child: Text(context.t('auth.createOneLink')),
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
