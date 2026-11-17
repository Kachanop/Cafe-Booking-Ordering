import assert from 'node:assert/strict';
import { describe, it } from 'mocha';

const api = (process.env.API_BASE_URL ?? 'http://localhost:5080').replace(/\/$/, '');
const ownerPassword = process.env.SMOKE_OWNER_PASSWORD;
const isolatedDatabaseConfirmed = process.env.SMOKE_ISOLATED_DB === 'true';

async function request(path, { method = 'GET', token, body } = {}) {
  const response = await fetch(`${api}${path}`, {
    method,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  return { status: response.status, body: text ? JSON.parse(text) : null };
}

describe('Cafe & Studio workflows (isolated database only)', function () {
  this.timeout(30000);

  it('runs booking, payment, table ordering, KDS, and cashier flows', async function () {
    if (!ownerPassword || !process.env.API_BASE_URL || !isolatedDatabaseConfirmed) this.skip();

    const email = `smoke-${Date.now()}@example.test`;
    const registered = await request('/api/auth/register', {
      method: 'POST', body: { email, password: 'MemberSmokePass!2026', fullName: 'Smoke Test Member' },
    });
    assert.equal(registered.status, 200);
    const memberToken = registered.body.accessToken;
    const ownerLogin = await request('/api/auth/login', {
      method: 'POST', body: { email: 'kachanop.peace@gmail.com', password: ownerPassword },
    });
    assert.equal(ownerLogin.status, 200);
    assert.equal(ownerLogin.body.user.role, 'Owner');
    const ownerToken = ownerLogin.body.accessToken;
    const [artistResult, serviceResult, menuResult] = await Promise.all([
      request('/api/artists'), request('/api/services'), request('/api/menu'),
    ]);
    const artist = artistResult.body[0];
    const service = serviceResult.body[0];
    const menu = menuResult.body[0];
    assert.ok(artist?.id && service?.id && menu?.id);

    const parts = new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Asia/Bangkok', year: 'numeric', month: '2-digit', day: '2-digit',
    }).formatToParts(new Date());
    const date = `${parts.find(x => x.type === 'year').value}-${parts.find(x => x.type === 'month').value}-${parts.find(x => x.type === 'day').value}`;
    const bookingBody = {
      staffId: artist.id, serviceId: service.id, bookingDate: date, timeSlot: '10:00-12:00', note: 'smoke workflow',
    };
    const booking = await request('/api/bookings', { method: 'POST', token: memberToken, body: bookingBody });
    assert.equal(booking.status, 201);
    assert.equal(booking.body.status, 'Pending');
    const duplicate = await request('/api/bookings', { method: 'POST', token: memberToken, body: bookingBody });
    assert.equal(duplicate.status, 409, 'a second booking for the same artist and slot must be rejected');
    assert.equal((await request(`/api/bookings/${booking.body.id}/paid`, {
      method: 'POST', token: ownerToken, body: { method: 'CASH' },
    })).status, 204);
    assert.equal((await request(`/api/bookings/${booking.body.id}/complete`, {
      method: 'POST', token: ownerToken, body: {},
    })).status, 204);

    const opened = await request('/api/tables/1/open', { method: 'POST', token: ownerToken, body: {} });
    assert.equal(opened.status, 200);
    const session = opened.body.session;
    const ordered = await request('/api/tables/1/orders', {
      method: 'POST', body: { session, items: [{ menuItemId: menu.id, quantity: 2, note: 'less sweet' }] },
    });
    assert.equal(ordered.status, 200);
    assert.equal(ordered.body.totalAmount, menu.price * 2);
    const tableView = await request(`/api/tables/1/session?session=${encodeURIComponent(session)}`);
    assert.equal(tableView.status, 200);
    const lineId = tableView.body.orders[0].items[0].id;
    assert.equal((await request(`/api/kds/${lineId}/status`, {
      method: 'POST', token: ownerToken, body: { status: 'Cooking' },
    })).status, 204);
    assert.equal((await request(`/api/kds/${lineId}/status`, {
      method: 'POST', token: ownerToken, body: { status: 'Served' },
    })).status, 204);
    assert.equal((await request('/api/tables/1/bill', { method: 'POST', body: { session } })).status, 204);
    const bills = await request('/api/cashier', { token: ownerToken });
    assert.equal(bills.status, 200);
    assert.equal(bills.body.length, 1);
    assert.equal((await request('/api/tables/1/close', {
      method: 'POST', token: ownerToken, body: { method: 'PROMPTPAY' },
    })).status, 204);
    assert.equal((await request(`/api/tables/1/session?session=${encodeURIComponent(session)}`)).status, 409,
      'closing the bill must invalidate the previous QR session');
    const overview = await request('/api/admin/overview', { token: ownerToken });
    assert.equal(overview.status, 200);
    assert.ok(overview.body.cafeRevenue >= menu.price * 2);
    assert.ok(overview.body.studioRevenue >= service.price);
  });
});
