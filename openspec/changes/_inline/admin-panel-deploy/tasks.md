Status: STOPPED(needs a route to the server's SSH: from this workstation's current foreign egress IP the connection opens and is then reset upstream, the same filtering that blocks GitHub-hosted runners, #59)
Verify: FAST

User request (2026-09-19): deploy the admin panel at admin.nahalkmi.ir, with a login "kazemi.mst" and
a password the user supplied in chat (NOT recorded here or anywhere in the repo), able to manage
providers.

## Decisions
- Username: the backend signs in by email only. The user chose "type kazemi.mst, stored as email":
  the account is kazemi.mst@nahalkmi.ir and the admin login maps a value without "@" to
  <value>@nahalkmi.ir (booksy-admin/src/utils/login-identifier.ts).
- Static files from host nginx (like the provider app), not another container on the shared box.

## Tasks
- [x] 1.1 CORS: allow https://admin.nahalkmi.ir; composition test pins every deployed origin
- [x] 1.2 Admin login accepts a plain username (unit-tested); .env.production -> live API
- [x] 1.3 CI: build-admin-web job + publish step + health check (commit dc8f1343, NOT pushed:
      the publish step would fail the whole deploy until 1.5 exists)
- [x] 1.4 Account registered through the public API (kazemi.mst@nahalkmi.ir, id
      a68c3c56-75de-4572-b723-4e726c1cb06a) — status still Pending
- [-] 1.5 BLOCKED: server setup as root, one session (script prepared): activate the account and
      grant Admin (SQL, scoped to that id, idempotent); /var/www/booksy-admin owned by booksy;
      HTTP vhost -> certbot --webroot -> full vhost (deployment/nginx/booksy-admin.conf), nginx -t
      guarded
- [ ] 1.6 Push dc8f1343; deploy; log in at admin.nahalkmi.ir; probe the admin endpoints WITH the
      admin token (an unauthenticated probe is inconclusive: the host's fallback policy answers
      401 for routes that do not exist too)

## Log
- 2026-09-19 Admin build passes (vue-tsc + vite); 47 unit tests pass; admin DNS already resolves to
  the box. /Auth/login exists and rejects a non-email identifier (400 "email format").
- 2026-09-19 Found: POST /api/v1/Users returns the activation token in its own response, so anyone
  can register and self-activate without proving they own the email. Recorded as FOLLOW-UPS #61.
- 2026-09-19 SSH: TCP to :22 opens, then "Connection reset" / timeouts. Egress IP 168.222.49.236
  (foreign); every earlier successful session came from an Iranian address.
