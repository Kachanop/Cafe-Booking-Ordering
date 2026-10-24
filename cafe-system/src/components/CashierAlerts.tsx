"use client";
import { useEffect, useRef, useState } from "react";
import { createClient } from "@/lib/supabase/client";

export function CashierAlerts() {
  const context = useRef<AudioContext | null>(null);
  const [enabled, setEnabled] = useState(false);
  useEffect(() => {
    const supabase = createClient();
    const channel = supabase.channel("cafe-live-refresh").on("broadcast", { event: "bill" }, () => {
      if (!context.current || context.current.state !== "running") return;
      const now = context.current.currentTime;
      for (const [frequency, start] of [[880, 0], [1175, 0.18], [880, 0.36]] as const) {
        const oscillator = context.current.createOscillator();
        const gain = context.current.createGain();
        oscillator.frequency.value = frequency;
        gain.gain.setValueAtTime(0.0001, now + start);
        gain.gain.exponentialRampToValueAtTime(0.18, now + start + 0.015);
        gain.gain.exponentialRampToValueAtTime(0.0001, now + start + 0.16);
        oscillator.connect(gain); gain.connect(context.current.destination);
        oscillator.start(now + start); oscillator.stop(now + start + 0.17);
      }
    }).subscribe();
    return () => { void supabase.removeChannel(channel); context.current?.close(); context.current = null; };
  }, []);
  async function enable() {
    context.current ??= new AudioContext();
    await context.current.resume();
    setEnabled(true);
  }
  return <button onClick={() => void enable()} className="rounded-xl border border-coffee-200 bg-white px-4 py-2 text-sm font-bold text-coffee-800">{enabled ? "🔔 เปิดเสียงแจ้งบิลแล้ว" : "เปิดเสียงแจ้งเตือนเรียกเก็บเงิน"}</button>;
}
