"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { useState, type ReactNode } from "react";

import { AuthProvider } from "@/shared/auth/AuthProvider";
import "@/shared/http/api-client";

export function AppProviders({ children }: { children: ReactNode }) {
  // One QueryClient per browser tab, created lazily so it survives fast
  // refresh in development without losing its cache on every render.
  const [queryClient] = useState(() => new QueryClient());

  return (
    <ThemeProvider
      attribute="data-theme"
      defaultTheme="dark"
      enableSystem={false}
    >
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
