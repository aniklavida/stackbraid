"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
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
    return typeof crypto !== "undefined" && crypto.randomUUID ? crypto.randomUUID() : "demo-job-1";
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
          <CardTitle className="text-xl">
            <h1>{t("title")}</h1>
          </CardTitle>
          <p className="mt-1 text-sm text-muted-foreground">{t("description")}</p>
        </div>
        {connectionStatus === "connected" ? (
          <Badge variant="outline" className="border-emerald-500 bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
            {t("live")}
          </Badge>
        ) : (
          <Badge variant="outline" className="border-amber-500 bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300" role="status">
            {t("paused")}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-6">
        <div className="flex flex-col gap-2">
          <label htmlFor="job-id-input" className="text-sm font-medium">
            {t("jobIdLabel")}
          </label>
          <div className="flex gap-2">
            <Input
              id="job-id-input"
              value={jobId}
              readOnly
              className="font-mono text-xs"
            />
            <Button variant="outline" size="sm" onClick={handleCopyLink}>
              {copied ? t("copied") : t("copyLink")}
            </Button>
            <Button variant="outline" size="sm" onClick={handleNewJob}>
              {t("newJob")}
            </Button>
          </div>
        </div>

        <Separator />

        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between text-sm">
            <span className="font-medium">{t("progressLabel")}</span>
            <div className="flex items-center gap-2">
              <Badge variant={status === "succeeded" ? "default" : status === "running" ? "secondary" : "outline"}>
                {t(`status_${status}`)}
              </Badge>
              <span className="font-mono font-semibold">{progress}%</span>
            </div>
          </div>
          <div
            className="relative h-4 w-full overflow-hidden rounded-full bg-secondary"
            role="progressbar"
            aria-valuenow={progress}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            <div
              className="h-full bg-primary transition-all duration-300"
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
          <span className="text-sm font-medium">{t("frames")}</span>
          {frames.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t("noFrames")}</p>
          ) : (
            <ul className="flex flex-col gap-1">
              {frames.map((frame, idx) => (
                <li
                  key={`${frame.occurredAt}-${idx}`}
                  className="flex items-center justify-between rounded border px-3 py-1.5 text-xs font-mono"
                >
                  <span className="font-medium capitalize">{t(`status_${frame.status}`)}</span>
                  <span>{frame.progress}%</span>
                  <span className="text-muted-foreground">
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
