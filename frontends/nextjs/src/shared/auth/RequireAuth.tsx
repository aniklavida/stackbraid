"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter } from "next/navigation";

import { useAuth } from "./AuthProvider";

/**
 * The route guard, entirely client-side. A Next.js proxy/middleware file
 * cannot do this job here: the refresh cookie the session actually depends
 * on is scoped by the backend to `Path=/v1/auth` on the *backend's own*
 * origin, so it is never present on a request this app's own server ever
 * sees — only on the browser's direct fetches to that one path, which is
 * exactly where `AuthProvider` reads it. This component redirects once
 * hydration has actually confirmed (or failed to confirm) a session, and
 * every admin API call is authoritatively re-checked by the backend's own
 * 403 regardless of what this component decides first.
 */
export function RequireAuth({ children, requirePermission }: { children: ReactNode; requirePermission?: string }) {
  const auth = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (auth.status !== "anonymous") return;
    router.replace("/login");
  }, [auth.status, router]);

  useEffect(() => {
    if (auth.status !== "authenticated" || !requirePermission) return;
    if (!auth.hasPermission(requirePermission)) router.replace("/");
  }, [auth, requirePermission, router]);

  if (auth.status !== "authenticated") return null;
  if (requirePermission && !auth.hasPermission(requirePermission)) return null;

  return <>{children}</>;
}
