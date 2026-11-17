import assert from 'node:assert/strict';
import { describe, it } from 'mocha';

const backend = (process.env.API_BASE_URL ?? 'http://localhost:5080').replace(/\/$/, '');
const web = (process.env.WEB_BASE_URL ?? 'http://localhost:5002').replace(/\/$/, '');

async function get(url) {
  const response = await fetch(url, { signal: AbortSignal.timeout(20000) });
  return { response, text: await response.text() };
}

describe('Cafe & Studio API smoke checks', function () {
  this.timeout(30000);

  it('reports the custom API and SQLite database as ready', async () => {
    const { response, text } = await get(`${backend}/health`);
    assert.equal(response.status, 200);
    assert.deepEqual(JSON.parse(text), { status: 'ok', database: 'sqlite' });
  });

  it('serves the public menu catalog', async () => {
    const { response, text } = await get(`${backend}/api/menu`);
    assert.equal(response.status, 200);
    const menu = JSON.parse(text);
    assert.ok(Array.isArray(menu));
    assert.ok(menu.length > 0);
    assert.ok(menu.every(item => item.id && item.name && item.category && item.price >= 0));
  });

  it('serves studio services and artist profiles', async () => {
    const [servicesResult, artistsResult] = await Promise.all([
      get(`${backend}/api/services`), get(`${backend}/api/artists`),
    ]);
    assert.equal(servicesResult.response.status, 200);
    assert.ok(JSON.parse(servicesResult.text).length > 0);
    assert.equal(artistsResult.response.status, 200);
    assert.ok(Array.isArray(JSON.parse(artistsResult.text)));
  });

  it('renders the MVC home page and booking page', async () => {
    const [home, booking] = await Promise.all([get(`${web}/`), get(`${web}/booking`)]);
    assert.equal(home.response.status, 200);
    assert.match(home.text, /Cafe & Studio/i);
    assert.equal(booking.response.status, 200);
    assert.match(booking.text, /จองคิวสตูดิโอ/);
  });

  it('keeps staff dashboards behind sign-in', async () => {
    const response = await fetch(`${web}/admin`, { redirect: 'manual' });
    assert.ok([302, 303].includes(response.status));
    assert.match(response.headers.get('location') ?? '', /Account\/Login/i);
  });
});
