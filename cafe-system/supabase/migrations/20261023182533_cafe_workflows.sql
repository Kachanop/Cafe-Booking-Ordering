-- Extend the existing Cafe & Studio schema without dropping customer data.
ALTER TABLE "User" ADD COLUMN IF NOT EXISTS auth_user_id uuid;
CREATE UNIQUE INDEX IF NOT EXISTS "User_auth_user_id_key" ON "User" (auth_user_id);

ALTER TABLE "StaffProfile" ADD COLUMN IF NOT EXISTS avatar_url text;
ALTER TABLE "StaffProfile" ADD COLUMN IF NOT EXISTS specialties text[] NOT NULL DEFAULT '{}';
ALTER TABLE "Booking" ADD COLUMN IF NOT EXISTS customer_note text;
ALTER TABLE "Booking" ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE "TableCafe" ADD COLUMN IF NOT EXISTS bill_requested_at timestamptz;
ALTER TABLE "TableCafe" ADD COLUMN IF NOT EXISTS opened_at timestamptz;
ALTER TABLE "Order" ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE "OrderItem" ADD COLUMN IF NOT EXISTS note text;
ALTER TABLE "Payment" ADD COLUMN IF NOT EXISTS method text NOT NULL DEFAULT 'UNKNOWN';

-- Only one live booking per artist and fixed slot; cancelled slots can be booked again.
CREATE UNIQUE INDEX IF NOT EXISTS "Booking_artist_slot_live_key"
  ON "Booking" ("staffId", "bookingDate", "timeSlot")
  WHERE status IN ('Pending', 'Paid', 'Completed');
CREATE INDEX IF NOT EXISTS "Order_open_created_at_idx" ON "Order" (status, created_at);
CREATE INDEX IF NOT EXISTS "TableCafe_bill_requested_idx" ON "TableCafe" (bill_requested_at) WHERE bill_requested_at IS NOT NULL;
CREATE INDEX IF NOT EXISTS "Payment_paid_at_type_idx" ON "Payment" ("paidAt", "referenceType") WHERE status = 'Success';

-- The app reads/writes operational records through its server-only Prisma connection.
-- Deny direct Data API access to customer, staff, booking, billing, table, and order records.
ALTER TABLE "Role" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "User" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "StaffProfile" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Service" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Booking" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "TableCafe" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "MenuCategory" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "MenuItem" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Order" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "OrderItem" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Payment" ENABLE ROW LEVEL SECURITY;

GRANT SELECT ON TABLE "MenuCategory", "MenuItem" TO anon, authenticated;
DROP POLICY IF EXISTS "Public can view cafe menu categories" ON "MenuCategory";
CREATE POLICY "Public can view cafe menu categories" ON "MenuCategory" FOR SELECT TO anon, authenticated USING (true);
DROP POLICY IF EXISTS "Public can view cafe menu items" ON "MenuItem";
CREATE POLICY "Public can view cafe menu items" ON "MenuItem" FOR SELECT TO anon, authenticated USING (true);
