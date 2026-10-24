# Cafe & Studio

Next.js app for cafe table ordering and creative studio bookings. It uses Supabase Auth, PostgreSQL through Prisma 7, and Supabase Realtime broadcasts for live staff screens.

## Local setup

1. Copy `.env.example` to `.env` and provide the URL, publishable key, and PostgreSQL connection string for the **same** Supabase project.
2. Install dependencies with `npm ci`.
3. Apply the migration in `supabase/migrations/` to the matching Supabase project. Review the existing schema and active bookings first; the migration intentionally fails rather than deleting data if an artist already has duplicate active slots.
4. Generate Prisma types with `npx prisma generate`.
5. Start the site with `npm run dev`.

The application uses Prisma from server code for business records. The migration enables RLS on the public tables, grants public read access only to the menu catalog, and keeps orders, bookings, staff, and payments off the direct Data API. Never put a database password or Supabase secret key in a `NEXT_PUBLIC_` variable.

## First owner account

Public sign-up always creates a Member; an account cannot choose its own staff role. After a trusted owner account is registered and has signed in once, a database administrator must assign the `Owner` role to that account in the `User` and `Role` tables. The owner can then assign staff roles from `/admin`.

## Screens

- `/` public cafe site and menu
- `/booking` studio artist, service, and fixed-slot booking
- `/my-bookings` member booking history
- `/table/[id]?session=...` live table QR ordering
- `/table/[id]/qr?session=...` staff-only QR display and print page
- `/kds` kitchen queue and menu summary
- `/cashier` table sessions, bill requests, and payment closeout
- `/admin` manager operations and owner revenue split / staff roles
- `/artist-dashboard` personal schedule
- `/it` non-financial connection and record-count overview

## Payment setup status

Cafe closeout supports cash and cashier-verified PromptPay. Booking requests store the full service price and remain Pending until a staff member confirms payment. No payment-gateway account or webhook credentials were present in the project, so automatic online checkout and automatic Paid/Completed updates are not enabled. Add the selected provider and its server-only credentials before accepting online studio payments.
