"use client";
import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";

export function RealtimeRefresh() {
  const router = useRouter();
  useEffect(() => {
    const supabase = createClient();
    const refresh = () => router.refresh();
    const channel = supabase.channel("cafe-live-refresh");
    channel.on("broadcast", { event: "refresh" }, refresh);
    channel.on("broadcast", { event: "bill" }, refresh);
    channel.subscribe();
    const fallback = window.setInterval(() => router.refresh(), 8000);
    return () => { window.clearInterval(fallback); void supabase.removeChannel(channel); };
  }, [router]);
  return <span className="inline-flex items-center gap-2 rounded-full bg-emerald-50 px-3 py-1.5 text-xs font-bold text-emerald-800"><span className="h-2 w-2 animate-pulse rounded-full bg-emerald-500" />อัปเดตสด</span>;
}
