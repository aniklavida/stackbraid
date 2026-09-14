import type { ReactNode } from "react";

import { WebShell } from "@/web/layout/WebShell";

export default function WebLayout({ children }: { children: ReactNode }) {
  return <WebShell>{children}</WebShell>;
}
