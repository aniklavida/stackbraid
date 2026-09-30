import { Component, OnDestroy, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { TranslocoPipe, TranslocoService } from "@jsverse/transloco";

import { JobProgressService, type JobsSource } from "../application/job-progress.service";

@Component({
  selector: "app-jobs-page",
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslocoPipe,
  ],
  providers: [JobProgressService],
  templateUrl: "./jobs-page.html",
})
export class JobsPageComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly jobService = inject(JobProgressService);

  readonly jobId = signal<string>("");
  readonly copied = signal<boolean>(false);

  constructor() {
    const initialId =
      this.route.snapshot.queryParamMap.get("jobId") ||
      (typeof crypto !== "undefined" && crypto.randomUUID
        ? crypto.randomUUID()
        : "a0000000-0000-4000-8000-000000000001");
    this.jobId.set(initialId);
    this.jobService.start(initialId);

    this.route.queryParamMap.subscribe((params) => {
      const id = params.get("jobId");
      if (id && id !== this.jobId()) {
        this.jobId.set(id);
        this.jobService.start(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.jobService.stop();
  }

  startJobWithSource(source: JobsSource): void {
    this.jobService.start(this.jobId(), source);
  }

  handleStartJob(): void {
    this.jobService.startJob();
  }

  handleNewJob(): void {
    const newId = crypto.randomUUID();
    this.jobId.set(newId);
    this.router.navigate(["/jobs"], { queryParams: { jobId: newId } });
    this.jobService.start(newId);
  }

  handleCopyLink(): void {
    if (typeof window !== "undefined") {
      const url = `${window.location.origin}/jobs?jobId=${this.jobId()}`;
      navigator.clipboard.writeText(url);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    }
  }

  formatTime(value: string): string {
    return new Intl.DateTimeFormat(this.transloco.getActiveLang(), {
      timeStyle: "medium",
    }).format(new Date(value));
  }
}
