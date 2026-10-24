"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { getActor } from "@/lib/auth";
import { createClient } from "@/lib/supabase/server";
import { prisma } from "@/lib/prisma";

export async function updateAccount(formData: FormData) {
  const actor = await getActor();
  if (!actor) redirect("/login?error=" + encodeURIComponent("กรุณาเข้าสู่ระบบก่อน"));

  const fullName = String(formData.get("fullName") ?? "").trim();
  const email = String(formData.get("email") ?? "").trim().toLowerCase();
  if (fullName.length < 1 || fullName.length > 100 || !/^\S+@\S+\.\S+$/.test(email)) {
    redirect("/account?error=" + encodeURIComponent("กรุณาตรวจสอบชื่อและอีเมลให้ถูกต้อง"));
  }

  const supabase = await createClient();
  const emailChanged = email !== (actor.authUser.email ?? "").toLowerCase();
  const { data, error } = await supabase.auth.updateUser({
    ...(emailChanged ? { email } : {}),
    data: { ...actor.authUser.user_metadata, full_name: fullName },
  });
  if (error) redirect("/account?error=" + encodeURIComponent(error.message));

  await prisma.user.update({
    where: { id: actor.profile.id },
    data: {
      fullName,
      ...(data.user.email?.toLowerCase() === email ? { email } : {}),
    },
  });

  revalidatePath("/", "layout");
  revalidatePath("/account");
  const message = emailChanged && data.user.email?.toLowerCase() !== email
    ? "ชื่อได้บันทึกแล้ว ส่งอีเมลยืนยันไปยังอีเมลใหม่ก่อนเปลี่ยนที่อยู่อีเมล"
    : "บันทึกข้อมูลบัญชีแล้ว";
  redirect("/account?message=" + encodeURIComponent(message));
}
