// Enforces docs/STRUCTURE.md's Clean Architecture rule for this app, the
// same way dependency-cruiser does for Angular/Next.js and NetArchTest/
// import-linter do for the two backends: "Domain knows nothing. Application
// knows Domain. Data and Delivery know Application. Nothing points inward
// from the edge," plus "`shared` never imports a feature."
//
// A plain `flutter test` file rather than a separate static-analysis tool:
// Dart has no equivalent of dependency-cruiser/import-linter with a config
// file, so this test parses `import` statements itself (regex over source
// text — no `package:analyzer` dependency needed for something this
// small) and fails the build the moment a rule is violated, exactly like
// the other four stacks' own architecture guards.
//
// `lib/app.dart`, `lib/app_dependencies.dart` and `lib/main.dart` are the
// composition root (this app's equivalent of a backend's `Host` or
// Angular's `app.config.ts`) and are deliberately NOT scanned — a
// composition root is allowed to depend on everything, the same exemption
// `docs/STRUCTURE.md` gives every stack's own `Host`/`app` layer.
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

class Violation {
  Violation(this.rule, this.file, this.detail);
  final String rule;
  final String file;
  final String detail;

  @override
  String toString() => '[$rule] $file: $detail';
}

final _importPattern = RegExp(r'''^\s*import\s+['"]([^'"]+)['"]''', multiLine: true);

/// A file's `import` targets, resolved to a lib-relative path for a
/// relative import (`../shared/foo.dart` -> `shared/foo.dart`), or left as
/// the raw `package:`/`dart:` URI otherwise.
List<String> _importsOf(File file, String libDir) {
  final text = file.readAsStringSync();
  final matches = _importPattern.allMatches(text);
  final resolved = <String>[];
  for (final m in matches) {
    final target = m.group(1)!;
    if (target.startsWith('dart:') || target.startsWith('package:')) {
      resolved.add(target);
    } else {
      // `.absolute` matters: `file` comes from `Directory('lib').listSync()`,
      // so its own `.path` (and therefore `.parent.path`) is relative
      // ("lib/shared/..."), and resolving "../../features/..." against a
      // relative base silently produces the wrong (but plausible-looking)
      // relative path instead of failing loudly.
      final joined = Uri.file('${file.absolute.parent.path}/$target').normalizePath().toFilePath();
      final rel = joined.startsWith('$libDir/') ? joined.substring(libDir.length + 1) : joined;
      resolved.add(rel);
    }
  }
  return resolved;
}

/// `features/auth/domain/validation.dart` -> `('auth', 'domain')`. `null`
/// for anything outside `features/<name>/<layer>/`.
(String, String)? _featureLayerOf(String libRelativePath) {
  final parts = libRelativePath.split('/');
  if (parts.length >= 3 && parts[0] == 'features') {
    return (parts[1], parts[2]);
  }
  return null;
}

bool _isUnderShared(String libRelativePath) => libRelativePath.startsWith('shared/');

void main() {
  final libDir = Directory('lib').absolute.path;
  final dartFiles = Directory('lib')
      .listSync(recursive: true)
      .whereType<File>()
      .where((f) => f.path.endsWith('.dart'))
      .toList();

  // The composition root — allowed to depend on shared/ and features/ at
  // once. See the file header for why.
  const compositionRoot = {'app.dart', 'app_dependencies.dart', 'main.dart'};

  List<Violation> findViolations() {
    final violations = <Violation>[];

    for (final file in dartFiles) {
      final libRelative = file.absolute.path.substring(libDir.length + 1);
      if (compositionRoot.contains(libRelative)) continue;

      final imports = _importsOf(file, libDir);
      final fromFeatureLayer = _featureLayerOf(libRelative);
      final fromIsShared = _isUnderShared(libRelative);

      for (final target in imports) {
        if (target.startsWith('dart:') || target.startsWith('package:')) continue;

        // R1 — shared never imports a feature.
        if (fromIsShared && target.startsWith('features/')) {
          violations.add(Violation('shared-never-imports-a-feature', libRelative, 'imports $target'));
        }

        final toFeatureLayer = _featureLayerOf(target);

        // R2 — domain depends on nothing (no relative import at all, in or
        // out of the feature — a domain layer that needs nothing).
        if (fromFeatureLayer != null && fromFeatureLayer.$2 == 'domain') {
          violations.add(Violation('domain-depends-on-nothing', libRelative, 'imports $target'));
        }

        if (fromFeatureLayer != null && toFeatureLayer != null && fromFeatureLayer.$1 == toFeatureLayer.$1) {
          // R3 — presentation cannot reach data (must go through application).
          if (fromFeatureLayer.$2 == 'presentation' && toFeatureLayer.$2 == 'data') {
            violations.add(Violation('presentation-cannot-reach-data', libRelative, 'imports $target'));
          }
          // R4 — application cannot reach presentation.
          if (fromFeatureLayer.$2 == 'application' && toFeatureLayer.$2 == 'presentation') {
            violations.add(Violation('application-cannot-reach-presentation', libRelative, 'imports $target'));
          }
        }
      }
    }

    // R5 — no import cycle among shared/ and features/ files (the
    // composition root is excluded from the graph, same as above).
    final graph = <String, List<String>>{};
    for (final file in dartFiles) {
      final libRelative = file.absolute.path.substring(libDir.length + 1);
      if (compositionRoot.contains(libRelative)) continue;
      graph[libRelative] = _importsOf(file, libDir)
          .where((t) => !t.startsWith('dart:') && !t.startsWith('package:'))
          .where((t) => !compositionRoot.contains(t))
          .toList();
    }
    const unvisited = 0, visiting = 1, done = 2;
    final state = <String, int>{for (final k in graph.keys) k: unvisited};
    final stack = <String>[];
    bool dfs(String node) {
      state[node] = visiting;
      stack.add(node);
      for (final next in graph[node] ?? const <String>[]) {
        if (!graph.containsKey(next)) continue;
        if (state[next] == visiting) {
          final cycleStart = stack.indexOf(next);
          final cycle = [...stack.sublist(cycleStart), next].join(' -> ');
          violations.add(Violation('no-circular', node, 'import cycle: $cycle'));
          return true;
        }
        if (state[next] == unvisited && dfs(next)) return true;
      }
      stack.removeLast();
      state[node] = done;
      return false;
    }

    for (final node in graph.keys) {
      if (state[node] == unvisited) dfs(node);
    }

    return violations;
  }

  test('shared never imports a feature', () {
    final hits = findViolations().where((v) => v.rule == 'shared-never-imports-a-feature');
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('a feature\'s domain layer depends on nothing inside this app', () {
    final hits = findViolations().where((v) => v.rule == 'domain-depends-on-nothing');
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('presentation cannot reach data directly (must go through application)', () {
    final hits = findViolations().where((v) => v.rule == 'presentation-cannot-reach-data');
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('application cannot reach presentation', () {
    final hits = findViolations().where((v) => v.rule == 'application-cannot-reach-presentation');
    expect(hits, isEmpty, reason: hits.join('\n'));
  });

  test('no import cycle among shared/ and features/ files', () {
    final hits = findViolations().where((v) => v.rule == 'no-circular');
    expect(hits, isEmpty, reason: hits.join('\n'));
  });
}
