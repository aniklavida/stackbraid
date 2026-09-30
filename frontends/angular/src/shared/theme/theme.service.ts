import { Injectable, signal } from "@angular/core";

export type Theme = "dark" | "light";
const STORAGE_KEY = "stackbraid.theme";

@Injectable({
  providedIn: "root",
})
export class ThemeService {
  readonly theme = signal<Theme>(this.readInitialTheme());

  constructor() {
    this.applyTheme(this.theme());
  }

  private readInitialTheme(): Theme {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === "light" || stored === "dark") {
        return stored;
      }
    } catch {
      // Fallback
    }
    return "dark";
  }

  toggleTheme(): void {
    const next: Theme = this.theme() === "dark" ? "light" : "dark";
    this.setTheme(next);
  }

  setTheme(theme: Theme): void {
    this.theme.set(theme);
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // Fallback
    }
    this.applyTheme(theme);
  }

  private applyTheme(theme: Theme): void {
    if (typeof document !== "undefined") {
      document.documentElement.setAttribute("data-theme", theme);
    }
  }
}
