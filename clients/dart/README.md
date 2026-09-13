# StackBraid Dart client

Generated from `contract/openapi.yaml`. **Never hand-edit anything under
`lib/`, `test/` or `doc/`** — regenerate instead. The drift check
(`scripts/check-client-drift.sh` at the repository root) exists to catch
exactly that, and the pre-commit hook runs it.

**Status:** compiles (`dart analyze` reports no issues). It has never called
a real server — no backend exists yet (see `docs/SPEC.md`). Generating a
client is not the same claim as a working integration.

## Which generator, and why

[`Carapacik/swagger_parser`](https://pub.dev/packages/swagger_parser)
(MIT) is the primary Dart generator, with
[`OpenAPITools/openapi-generator`](https://github.com/OpenAPITools/openapi-generator)
(Apache-2.0) as the fallback for either client target. The known risk was
that the small Dart OpenAPI ecosystem might not handle the whole contract.
It did not: `swagger_parser` 1.44.3 crashes on this contract because it
cannot parse OpenAPI's path-item-level `parameters` (used by
`/v1/users/{userId}` and `/v1/users/{userId}/roles/{roleId}` to avoid
repeating the same path parameter on every operation) — a confirmed,
still-open upstream bug:
[Carapacik/swagger_parser#374](https://github.com/Carapacik/swagger_parser/issues/374).
That is not something to hand-patch around, so this client is generated with
the fallback instead: the `dart-dio` generator, using `dio` for
transport and `json_serializable` for models.

## Toolchain

The local Dart SDK was **3.10**; `build_runner >= 2.16` and
`json_serializable >= 6.14` both require **>= 3.11**, and json_serializable's
generated null-aware-element syntax needs an SDK constraint lower bound of at
least 3.8 to parse at all. The requirement is a toolchain upgrade rather
than another pin, so the toolchain was upgraded — a standalone
Dart SDK (`brew install dart-sdk`) alongside the Flutter-bundled one, since
Flutter itself still ships 3.10 — rather than pinning `retrofit_generator` or
any other package to an older release. `pubspec.yaml`'s `environment.sdk`
floor is `>=3.11.0` to record that requirement.

## Regenerate

From the repository root, regenerate both clients with one command:

```bash
./scripts/generate-clients.sh
```

Or just this one, from this directory (Dart SDK >= 3.11 on `PATH`):

```bash
npx --yes @openapitools/openapi-generator-cli generate -c openapi-generator-config.yaml
dart pub get
dart run build_runner build
```

`pubspec.yaml`, `analysis_options.yaml` and this `README.md` are hand-written
and listed in `.openapi-generator-ignore` so regeneration does not touch
them — the same pattern the TypeScript client uses for `package.json` and
`tsconfig.json`. Everything else under `lib/`, `test/` and `doc/` is
generated.

## Verify

```bash
dart pub get
dart analyze
```

## Using it

```dart
import 'package:stackbraid_client/stackbraid_client.dart';

final client = StackbraidClient(basePathOverride: 'https://your-backend-host');
final response = await client.getUsersApi().listUsers(page: 1, pageSize: 20);
final userPage = response.data; // UserPage
```
