# mobile/flutter

The Flutter mobile shell for StackBraid's `Identity` feature — register, sign
in, a session that survives an app restart, a profile screen, sign out and
sign back in. Speaks the contract exclusively through the generated Dart
client in [`clients/dart`](../../clients/dart), used unchanged: no
hand-written HTTP calls, no copied models.

**No admin surface on mobile** (`docs/STRUCTURE.md`: "There is no web/admin
split on mobile — administration is a desktop job"). Listing users and
assigning a role is proven instead at the level this app actually operates
on the contract — the generated client directly, using the seeded
administrator — in `integration_test/identity_flow_test.dart`'s second
test, not through a mobile UI.

## Structure

Clean Architecture per feature, the same four layers and the same
dependency direction as every other stack (`docs/STRUCTURE.md`):

```
lib/
├── shared/           auth (session + token storage), http, config, i18n, theme, widgets
├── features/auth/    domain/ application/ data/ presentation/
├── app.dart          composition root — the only file allowed to depend on
├── app_dependencies.dart   both shared/ and features/ at once
└── main.dart
```

`test/architecture/boundary_test.dart` enforces the same five rule shapes
the other stacks' own architecture guards do (`shared` never imports a
feature; a feature's `domain/` depends on nothing; `presentation/` cannot
reach `data/` directly; `application/` cannot reach `presentation/`; no
import cycle) — a plain `flutter test` file, since Dart has no
dependency-cruiser/import-linter equivalent with a config file. Each rule
was seen to genuinely fail on a deliberate violation, then reverted, before
being trusted.

## Token storage

The mobile half of the decision every StackBraid client records (an access
token that never touches disk): the **access token stays in-memory only**
(`shared/auth/session_controller.dart`), and the **refresh token is
persisted through platform secure storage** — iOS/macOS Keychain, Android
Keystore-backed `EncryptedSharedPreferences` (`flutter_secure_storage`,
BSD-3-Clause) — never a plain file or `SharedPreferences`. This resolves
`docs/SPEC.md`'s open "mobile is the half the specification never
answered" question for this shell. See `shared/auth/token_store.dart` for
the full reasoning, including why the macOS build uses the traditional
per-user login Keychain rather than the newer "data protection" Keychain
(the latter requires the running binary to be signed with a real Apple
Developer Team identity even outside the App Sandbox — see
`Configs/LocalSigning.xcconfig.example` for how to opt back into it on a
machine that has one).

**Tokens never reach a log.** `shared/http/log_redaction.dart` redacts
every bearer token, cookie, and `accessToken`/`refreshToken`/`password`
JSON field before anything is printed, and only outside release builds.
`test/shared/http/log_redaction_test.dart` proves it — including feeding a
real request/response pair with known secret values through the actual
interceptor and asserting the printed output never contains them.

## Localization

English and Spanish, both genuinely translated (no lorem). Each feature
ships its own strings (`features/auth/presentation/i18n/auth_strings.dart`)
merged with `shared/i18n/common_strings.dart` at the app root
(`shared/i18n/translations.dart`) — the same "every feature ships its own
translations" rule the Angular (`@jsverse/transloco`) and Next.js
(`next-intl`) frontends follow, adapted to plain Dart maps instead of an
ARB-based code-generation toolchain (`flutter gen-l10n` only supports one
`arb-dir`, which would have forced a single central strings file). Flutter's
own built-in widgets (back-button semantics, default tooltips) are
localized too, via `flutter_localizations` — bundled with the Flutter SDK
itself, no separate licence to audit.

## Dependencies and the licence audit

Every package this app depends on — direct and transitive, `dependencies:`
and `dev_dependencies:` — is in `docs/dependency-inventory.json` under the
`dart-mobile-flutter` ecosystem, each verified against the actual `LICENSE`
file in the local pub cache on the date recorded, not carried forward from
memory. See [`docs/DEPENDENCIES.md`](../../docs/DEPENDENCIES.md) for the
narrative account. `node scripts/check-dependency-licenses.mjs` (run from
the repository root) checks `mobile/flutter/pubspec.lock` against that
inventory and fails on drift, exactly as it already does for every other
stack.

## What was verified, and on what

**Verified: the macOS desktop target (`flutter test integration_test -d
macos`).** `integration_test/identity_flow_test.dart` runs the full
identity flow — register, sign in, the session surviving a fresh app
instance reading the same persisted refresh token back out of the real
macOS Keychain, sign out, sign back in — against a real running backend on
a real throwaway local Postgres cluster (no Docker), changing only
`--dart-define=API_BASE_URL` between the .NET and Python runs. A second
test proves the seeded administrator can list users and assign a role
through the generated client directly. Screenshots from both runs are
saved outside this repository, never committed — see "Running the identity
flow against a real backend" below for the exact `SCREENSHOT_DIR`.

**Not verified: any iOS or Android simulator or emulator.** None is
installed on this machine, and none was downloaded to build this shell —
an explicit constraint for this work, not an oversight. The Flutter and
Dart code itself is platform-agnostic (the same `flutter_secure_storage`
API backs iOS Keychain and Android Keystore too), but "the same code
compiles for iOS/Android" is not "verified running on iOS/Android," and
this document does not claim the second thing.

**Verified: Chrome was ruled out for the integration test specifically** —
`flutter test integration_test` has no web-device support yet
("Web devices are not supported for integration tests yet"), and the
`flutter drive` web fallback needs a matching `chromedriver`, which
Homebrew currently disables for this macOS version's Gatekeeper policy.
`flutter analyze` and the plain `flutter test` unit/architecture suite
above are platform-independent and were run normally.

## Building and testing locally

```bash
cd mobile/flutter
flutter pub get
flutter analyze
flutter test                        # unit + architecture tests
```

### Running the identity flow against a real backend

```bash
# .NET — from backends/dotnet
export STACKBRAID_TEST_POSTGRES_CONNECTION_STRING="$(./scripts/start-local-postgres.sh)"
ASPNETCORE_ENVIRONMENT=Production \
  ConnectionStrings__Postgres="$STACKBRAID_TEST_POSTGRES_CONNECTION_STRING" \
  Jwt__SigningKey="<any base64 32+ byte value for a local run>" \
  dotnet run --no-launch-profile --project src/Host --urls http://127.0.0.1:8080 &
# (--no-launch-profile and an explicit Production environment sidestep the
#  already-flagged .NET DI-lifetime bug in the default Development profile)

cd ../../mobile/flutter
flutter test integration_test/identity_flow_test.dart -d macos \
  --dart-define=API_BASE_URL=http://127.0.0.1:8080

cd ../../backends/dotnet && ./scripts/stop-local-postgres.sh
```

```bash
# Python — from backends/python
export STACKBRAID_POSTGRES_DSN="$(./scripts/start-local-postgres.sh)"
PYTHONPATH=src .venv/bin/uvicorn app.host.main:app --port 8090 &

cd ../../mobile/flutter
flutter test integration_test/identity_flow_test.dart -d macos \
  --dart-define=API_BASE_URL=http://127.0.0.1:8090

cd ../../backends/python && ./scripts/stop-local-postgres.sh
```

Add `--dart-define=SCREENSHOT_DIR=<absolute path outside this repo>
--dart-define=BACKEND_LABEL=dotnet` (or `python`) to save a screenshot at
every step of the flow above.

### A note on the macOS Keychain and code signing

`flutter_secure_storage`'s macOS backend needs the `keychain-access-groups`
entitlement — and that entitlement needs the app to be signed with a real
Apple Development identity, which needs Xcode itself signed into an Apple
ID (`Add a new account in Accounts settings`), which this environment does
not have and this session did not set up (entering Apple ID credentials is
outside what an agent may do here). `shared/auth/token_store.dart` instead
opts into the older, unentitled per-user login Keychain
(`useDataProtectionKeyChain: false`), which works with plain ad hoc local
signing — see that file for the full account. A contributor who wants the
newer Keychain (and has a real Team ID) can drop the override and copy
`macos/Runner/Configs/LocalSigning.xcconfig.example` to
`LocalSigning.xcconfig` (gitignored) with their own team ID.
