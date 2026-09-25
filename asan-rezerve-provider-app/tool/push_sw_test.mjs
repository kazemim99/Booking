// Behaviour of web/push/firebase-messaging-sw.js, run in a fake service-worker global.
//
//   node --test tool/push_sw_test.mjs
//
// The worker is JavaScript no Dart test can reach, and it decides where every tapped browser notification goes.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';

const source = readFileSync(new URL('../web/push/firebase-messaging-sw.js', import.meta.url), 'utf8');
const dart = readFileSync(new URL('../lib/core/push/push_click_message.dart', import.meta.url), 'utf8');

const SITE = 'https://app.example/';
const CONFIGURED = `${SITE}push/firebase-messaging-sw.js?apiKey=k&appId=1%3A1%3Aweb%3A1&messagingSenderId=1&projectId=p`;

/** Loads the worker into a fresh fake global and records what it does. */
function load({ url = CONFIGURED, tabs = [] } = {}) {
  const log = { listeners: [], imported: [], initializedWith: null, opened: [], posted: [], focused: 0 };
  const self = {
    location: { href: url },
    addEventListener: (type, fn) => log.listeners.push({ type, fn, owner: log.initializedWith ? 'firebase' : 'ours' }),
    clients: {
      matchAll: async () => tabs.map((tabUrl) => ({
        url: tabUrl,
        focus: async () => { log.focused++; },
        postMessage: (message) => log.posted.push({ tabUrl, message }),
      })),
      openWindow: async (target) => { log.opened.push(target); },
    },
  };
  const firebase = {
    initializeApp: (config) => { log.initializedWith = config; },
    // What firebase-messaging-compat does in a worker: it adds its own click handler.
    messaging: () => self.addEventListener('notificationclick', () => log.firebaseHandlerRan = true),
  };
  const context = { self, URL, URLSearchParams, firebase, importScripts: (...urls) => log.imported.push(...urls) };
  vm.runInNewContext(source, context);
  return log;
}

/** Dispatches a click the way the browser does: listeners in registration order until one stops propagation. */
async function tap(log, data) {
  let stopped = false;
  let pending;
  let closed = false;
  const event = {
    notification: { data: { FCM_MSG: { data } }, close: () => { closed = true; } },
    stopImmediatePropagation: () => { stopped = true; },
    waitUntil: (promise) => { pending = promise; },
  };
  for (const { type, fn } of log.listeners) {
    if (type !== 'notificationclick') continue;
    fn(event);
    if (stopped) break;
  }
  await pending;
  return { closed };
}

test('with no tab open, a tap opens the app on the push through the router', async () => {
  const log = load();
  const { closed } = await tap(log, { bookingId: 'b1', notificationId: 'n1' });

  assert.equal(closed, true);
  assert.deepEqual(log.opened, [`${SITE}#/push-open?bookingId=b1&notificationId=n1`]);
});

test('with a tab open, the tap focuses it and hands it the push data instead of opening another', async () => {
  const log = load({ tabs: [`${SITE}#/home`] });
  await tap(log, { bookingId: 'b1' });

  assert.equal(log.focused, 1);
  assert.deepEqual(log.opened, []);
  // Through JSON: objects made inside the worker's context have that context's prototypes.
  assert.deepEqual(JSON.parse(JSON.stringify(log.posted)), [
    { tabUrl: `${SITE}#/home`, message: { type: 'booksy-push-open', data: { bookingId: 'b1' } } },
  ]);
});

test("another site's tab is not this app", async () => {
  const log = load({ tabs: ['https://elsewhere.example/'] });
  await tap(log, { bookingId: 'b1' });

  assert.deepEqual(log.posted, []);
  assert.equal(log.opened.length, 1);
});

test('a push without data still opens the app (the inbox decides)', async () => {
  const log = load();
  await tap(log, {});

  assert.deepEqual(log.opened, [`${SITE}#/push-open`]);
});

test("our click handler runs before Firebase's, which would open nothing", async () => {
  const log = load();
  const clicks = log.listeners.filter((l) => l.type === 'notificationclick');

  assert.equal(clicks.length, 2);
  assert.equal(clicks[0].owner, 'ours');
  await tap(log, { bookingId: 'b1' });
  assert.equal(log.firebaseHandlerRan, undefined);
});

test('Firebase starts with the config from the registration address', () => {
  const log = load();

  assert.deepEqual(JSON.parse(JSON.stringify(log.initializedWith)), { apiKey: 'k', appId: '1:1:web:1', messagingSenderId: '1', projectId: 'p' });
  assert.equal(log.imported.length, 2);
  assert.ok(log.imported.every((u) => u.startsWith('https://www.gstatic.com/firebasejs/')));
});

test('without the config the worker loads nothing', () => {
  const log = load({ url: `${SITE}push/firebase-messaging-sw.js` });

  assert.deepEqual(log.imported, []);
  assert.equal(log.initializedWith, null);
});

test('the tap message type is the one the Dart side listens for', () => {
  const dartType = /pushClickMessageType\s*=\s*'([^']+)'/.exec(dart)[1];
  const jsType = /TAP_MESSAGE\s*=\s*'([^']+)'/.exec(source)[1];

  assert.equal(jsType, dartType);
});
