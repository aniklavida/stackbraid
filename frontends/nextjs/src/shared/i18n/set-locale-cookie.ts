/**
 * A plain function outside any component body — the React Compiler's
 * purity analysis (`react-hooks/immutability`) otherwise flags a direct
 * `document.cookie` write inside an event handler as mutating something
 * captured from outer scope, even though nothing here is a render-phase
 * side effect.
 */
export function setLocaleCookie(locale: string): void {
  document.cookie = `NEXT_LOCALE=${locale}; path=/; max-age=31536000; samesite=lax`;
}
