import { RequireAuth } from "@/shared/auth/RequireAuth";
import { JobProgressPage } from "@/web/features/jobs";

export default function JobsRoute() {
  return (
    <RequireAuth>
      <JobProgressPage />
    </RequireAuth>
  );
}
