import { Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatMenuModule } from "@angular/material/menu";
import { TranslocoService, TranslocoPipe } from "@jsverse/transloco";

const LOCALES = ["en", "es"] as const;
const STORAGE_KEY = "stackbraid.locale";

@Component({
  selector: "app-locale-switcher",
  standalone: true,
  imports: [MatButtonModule, MatMenuModule, TranslocoPipe],
  template: `
    <button
      mat-button
      [attr.aria-label]="'language.label' | transloco"
      [matMenuTriggerFor]="menu"
    >
      {{ "language." + activeLang() | transloco }}
    </button>
    <mat-menu #menu="matMenu">
      @for (code of locales; track code) {
        <button mat-menu-item (click)="selectLocale(code)">{{ "language." + code | transloco }}</button>
      }
    </mat-menu>
  `,
})
export class LocaleSwitcherComponent {
  private readonly transloco = inject(TranslocoService);
  protected readonly locales = LOCALES;

  protected activeLang(): string {
    return this.transloco.getActiveLang();
  }

  protected selectLocale(next: string): void {
    localStorage.setItem(STORAGE_KEY, next);
    this.transloco.setActiveLang(next);
  }
}

/** Reads the visitor's previously chosen locale — defaults to English, exactly like the other frontend's cookie default. */
export function storedLocale(): string {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored && (LOCALES as readonly string[]).includes(stored) ? stored : "en";
  } catch {
    return "en";
  }
}
