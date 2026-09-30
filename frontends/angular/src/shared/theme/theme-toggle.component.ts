import { Component, inject } from "@angular/core";
import { ThemeService } from "./theme.service";

@Component({
  selector: "app-theme-toggle",
  standalone: true,
  template: `
    <button
      type="button"
      class="btn-ghost btn-icon-sm"
      (click)="themeService.toggleTheme()"
      [attr.aria-label]="isDark() ? 'Switch to light theme' : 'Switch to dark theme'"
      [attr.title]="isDark() ? 'Switch to light theme' : 'Switch to dark theme'"
    >
      @if (isDark()) {
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="size-4 text-[var(--mut)] hover:text-[var(--ink)]"
        >
          <circle cx="12" cy="12" r="4"/>
          <path d="M12 2v2"/>
          <path d="M12 20v2"/>
          <path d="m4.93 4.93 1.41 1.41"/>
          <path d="m17.66 17.66 1.41 1.41"/>
          <path d="M2 12h2"/>
          <path d="M20 12h2"/>
          <path d="m6.34 17.66-1.41 1.41"/>
          <path d="m19.07 4.93-1.41 1.41"/>
        </svg>
      } @else {
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="size-4 text-[var(--mut)] hover:text-[var(--ink)]"
        >
          <path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/>
        </svg>
      }
    </button>
  `,
})
export class ThemeToggleComponent {
  protected readonly themeService = inject(ThemeService);

  protected isDark(): boolean {
    return this.themeService.theme() === "dark";
  }
}
