"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";

import { AuthProvider } from "@/shared/auth/AuthProvider";
import "@/shared/http/api-client";

export function AppProviders({ children }: { children: ReactNode }) {
  // One QueryClient per browser tab, created lazily so it survives fast
  // refresh in development without losing its cache on every render.
  const [queryClient] = useState(() => new QueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  );
}
