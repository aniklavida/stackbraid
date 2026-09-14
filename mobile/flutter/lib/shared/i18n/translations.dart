/// Key -> localized string, for one locale.
typedef LocaleStrings = Map<String, String>;

/// Locale code ("en"/"es") -> its [LocaleStrings].
///
/// Every feature ships its own translations under its own namespaced keys
/// (`auth.*`) — the same rule `docs/STRUCTURE.md` names for every stack
/// ("Every feature ships its own translations. No central `strings.json`
/// that nobody updates."). `shared/i18n` only merges what each feature (and
/// `shared` itself, for genuinely cross-feature text like button labels)
/// already owns; it defines no feature-specific string itself.
typedef Translations = Map<String, LocaleStrings>;

Translations mergeTranslations(List<Translations> sources) {
  final merged = <String, LocaleStrings>{};
  for (final source in sources) {
    for (final entry in source.entries) {
      merged.putIfAbsent(entry.key, () => <String, String>{}).addAll(entry.value);
    }
  }
  return merged;
}
