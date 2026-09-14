import { UserDetailPage } from "@/admin/features/users";

export default async function Page({ params }: PageProps<"/admin/users/[userId]">) {
  const { userId } = await params;
  return <UserDetailPage userId={userId} />;
}
