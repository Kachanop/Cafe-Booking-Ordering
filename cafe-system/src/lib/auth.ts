import { redirect } from "next/navigation";
import { createClient } from "@/lib/supabase/server";
import { prisma } from "@/lib/prisma";

export type StaffRole = "member" | "artist" | "waiter" | "kitchen" | "cashier" | "manager" | "owner" | "it staff";

export function normalizeRole(value: string): StaffRole | "guest" {
  const role = value.trim().toLowerCase();
  if (role === "it" || role === "it staff") return "it staff";
  if (role === "waiter/staff" || role === "staff") return "waiter";
  if (["member", "artist", "waiter", "kitchen", "cashier", "manager", "owner", "guest"].includes(role)) return role as StaffRole | "guest";
  return "guest";
}

export async function getActor() {
  const supabase = await createClient();
  const { data: { user }, error } = await supabase.auth.getUser();
  if (error || !user) return null;
  const profile = await prisma.user.findFirst({
    where: { OR: [{ authUserId: user.id }, { email: user.email ?? undefined }] },
    include: { role: true },
  });
  if (!profile) return null;
  const metadataName = typeof user.user_metadata?.full_name === "string" ? user.user_metadata.full_name.trim() : "";
  const profileOutOfSync = profile.email !== (user.email ?? null) || (metadataName && metadataName !== profile.fullName);
  const syncedProfile = profileOutOfSync
    ? await prisma.user.update({
        where: { id: profile.id },
        data: { ...(user.email ? { email: user.email } : {}), ...(metadataName ? { fullName: metadataName } : {}) },
        include: { role: true },
      })
    : profile;
  return { authUser: user, profile: syncedProfile };
}

export async function requireActor(roles?: StaffRole[]) {
  const actor = await getActor();
  if (!actor) redirect("/login?error=" + encodeURIComponent("กรุณาเข้าสู่ระบบก่อนใช้งาน"));
  const role = normalizeRole(actor.profile.role.name);
  if (roles && !roles.includes(role as StaffRole)) redirect("/unauthorized");
  return actor;
}

export async function ensureMemberProfile(user: { id: string; email?: string; user_metadata?: Record<string, unknown> }) {
  if (!user.email) throw new Error("บัญชีนี้ยังไม่มีอีเมล");
  const existing = await prisma.user.findFirst({ where: { OR: [{ authUserId: user.id }, { email: user.email }] } });
  if (existing) {
    const fullName = typeof user.user_metadata?.full_name === "string" ? user.user_metadata.full_name.trim() : "";
    return prisma.user.update({
      where: { id: existing.id },
      data: { authUserId: user.id, ...(user.email ? { email: user.email } : {}), ...(fullName ? { fullName } : {}) },
    });
  }
  const role = await prisma.role.upsert({
    where: { name: "Member" },
    update: {},
    create: { name: "Member", description: "ลูกค้าสมาชิก" },
  });
  const fullName = typeof user.user_metadata?.full_name === "string" ? user.user_metadata.full_name.trim() : "";
  return prisma.user.create({ data: { authUserId: user.id, email: user.email, fullName: fullName || user.email.split("@")[0], roleId: role.id } });
}


