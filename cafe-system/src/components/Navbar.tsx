import { getActor } from "@/lib/auth";
import NavbarClient from "@/components/NavbarClient";

export default async function Navbar() {
  const actor = await getActor();
  return <NavbarClient isAuthenticated={Boolean(actor)} />;
}
