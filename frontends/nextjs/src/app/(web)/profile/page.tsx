import { RequireAuth } from "@/shared/auth/RequireAuth";
import { ProfilePage } from "@/web/features/profile";

export default function Page() {
  return (
    <RequireAuth>
      <ProfilePage />
    </RequireAuth>
  );
}
