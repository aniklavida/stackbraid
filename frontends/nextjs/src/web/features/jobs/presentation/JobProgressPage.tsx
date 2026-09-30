"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { useJobProgress, type JobsSource } from "../application/use-job-progress";

export function JobProgressPage({ source }: { source?: JobsSource }) {
  const t = useTranslations("jobs");
  const locale = useLocale();
  const searchParams = useSearchParams();
  const router = useRouter();

  const queryJobId = searchParams.get("jobId");
  const [generatedJobId, setGeneratedJobId] = useState<string>(() => {
    return typeof crypto !== "undefined" && crypto.randomUUID
      ? crypto.randomUUID()
      : "a0000000-0000-4000-8000-000000000001";
  });
  const [copied, setCopied] = useState(false);

  const jobId = queryJobId || generatedJobId;

  const { status, progress, frames, connectionStatus, startJob } = useJobProgress(jobId, source);

  const dateFormatter = new Intl.DateTimeFormat(locale, {
    timeStyle: "medium",
  });

  const handleNewJob = () => {
    const newId = crypto.randomUUID();
    setGeneratedJobId(newId);
    router.replace(`/jobs?jobId=${newId}`);
  };

  const handleCopyLink = () => {
    if (typeof window !== "undefined") {
      const url = `${window.location.origin}/jobs?jobId=${jobId}`;
      navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <Card className="w-full max-w-xl">
      <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-4">
        <div>
          <CardTitle>
            <h1 className="font-display text-xl font-semibold tracking-tight text-[var(--ink)]">{t("title")}</h1>
          </CardTitle>
          <p className="mt-1 text-xs text-[var(--mut)]">{t("description")}</p>
        </div>
        {connectionStatus === "connected" ? (
          <Badge variant="success">
            {t("live")}
          </Badge>
        ) : (
          <Badge variant="warning" role="status">
            {t("paused")}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-6">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="job-id-input">
            {t("jobIdLabel")}
          </Label>
          <div className="flex flex-wrap sm:flex-nowrap gap-2">
            <Input
              id="job-id-input"
              value={jobId}
              readOnly
              className="font-mono text-xs"
            />
            <Button variant="outline" size="sm" onClick={handleCopyLink} className="shrink-0">
              {copied ? t("copied") : t("copyLink")}
            </Button>
            <Button variant="outline" size="sm" onClick={handleNewJob} className="shrink-0">
              {t("newJob")}
            </Button>
          </div>
        </div>

        <Separator />

        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between text-xs">
            <span className="font-medium text-[var(--ink)]">{t("progressLabel")}</span>
            <div className="flex items-center gap-2">
              <Badge variant={status === "succeeded" ? "success" : status === "running" ? "default" : "outline"}>
                {t(`status_${status}`)}
              </Badge>
              <span className="font-mono font-medium text-[var(--ink)]">{progress}%</span>
            </div>
          </div>
          <div
            className="relative h-2 w-full overflow-hidden rounded-full bg-[var(--subtle)]"
            role="progressbar"
            aria-valuenow={progress}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            <div
              className="h-full bg-[var(--acc)] rounded-full transition-all duration-300"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>

        <div>
          <Button
            onClick={startJob}
            disabled={status === "running" || status === "queued" || connectionStatus === "paused"}
            className="w-full"
          >
            {t("startJob")}
          </Button>
        </div>

        <Separator />

        <div className="flex flex-col gap-2">
          <span className="text-xs font-medium text-[var(--ink)]">{t("frames")}</span>
          {frames.length === 0 ? (
            <p className="text-xs text-[var(--mut)]">{t("noFrames")}</p>
          ) : (
            <ul className="flex flex-col gap-1.5 max-h-56 overflow-y-auto">
              {frames.map((frame, idx) => (
                <li
                  key={`${frame.occurredAt}-${idx}`}
                  className="flex items-center justify-between rounded-[var(--r-sm)] border border-[var(--bd)] bg-[var(--subtle)]/40 px-3 py-1.5 text-xs font-mono text-[var(--ink)]"
                >
                  <span className="font-medium capitalize">{t(`status_${frame.status}`)}</span>
                  <span>{frame.progress}%</span>
                  <span className="text-[var(--mut)]">
                    {dateFormatter.format(new Date(frame.occurredAt))}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
