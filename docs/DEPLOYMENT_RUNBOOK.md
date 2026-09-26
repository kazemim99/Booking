# Deployment & Operations Runbook

Operational reference for running AsanRezerve in Docker Compose on the production/staging servers.
Moved verbatim out of `CLAUDE.md` on 2026-09-08 so the assistant instruction file can stay a
routing document; nothing here changed in the move. For what the system *is*, see
[openspec/project.md](../openspec/project.md); for CI, see `.github/workflows/`.

## ⚠️ Pending: server-side rename to AsanRezerve (as of 2026-09-25)

The repository was renamed **Booksy → AsanRezerve** (آسان رزرو) on 2026-09-25: solution, all
namespaces, both frontend/Flutter app directories, `docker-compose.prod.yml` (service/container
names, network), `.github/workflows/deploy.yml` (image prefix, health-check container name),
and the `deployment/` nginx configs and setup scripts all now use the new name.

**The live server at `194.1.155.230` has not been migrated yet** and still runs everything under
the old `booksy` names described in the rest of this section (deploy user `booksy`, directory
`/opt/booksy`, containers `booksy-api`/`booksy-frontend`/`booksy-postgres`/`booksy-redis`, runner
label `booksy-prod`, images `ghcr.io/kazemim99/booksy-api`). **Do not just push this branch's
`docker-compose.prod.yml`/`deploy.yml` to `master` and let CI run against the unmigrated box** —
the deploy job's `runs-on: [self-hosted, asan-rezerve-prod]` label matches no registered runner
(the job will queue forever, not fail loudly), and even after relabeling, `docker compose up -d`
under the renamed service keys would create **brand-new, empty** volumes rather than reusing the
production data — see why below.

### Transitional deploy (2026-09-25, until the migration below runs)

So the renamed app can reach the server before the migration, `deploy.yml`'s deploy job points at the
**existing** layout: `runs-on: [self-hosted, booksy-prod]`, `DEPLOY_PATH=/opt/booksy`, the
`/var/www/booksy-*` web roots and `COMPOSE_PROJECT_NAME=booksy` — so the data volumes are still
`booksy_postgres_data`, `booksy_redis_data`, `booksy_uploads_data`. What it does on the box:

- **First deploy only:** if a `booksy-api` container exists, the old stack is taken down with its own
  file (`docker-compose.prod.yml.previous`, no `-v`) before `up`. It has to: the renamed network uses
  the same fixed subnet `172.25.0.0/16`, and `up` fails with "Pool overlaps" while the old one exists.
  Rehearsed locally on 2026-09-25 (old stack + data → switch → data intact → rollback → data intact).
- Containers are now `asan-rezerve-*`, but each keeps its old name as a network alias
  (`booksy-api`, `booksy-postgres`, …), so a `.env` connection string that says `Host=booksy-postgres`
  still works.
- If the API does not become healthy, the job brings the previous compose file back up (same volumes).
- **Demo reviews are ON** (`Database__SeedDemoReviews=${SEED_DEMO_REVIEWS:-true}`): on start the API adds
  18 reviews by 12 test customers (`+98999000100x`) to «سالن نهال», with salon replies and votes, once.
  Set `SEED_DEMO_REVIEWS=false` in `/opt/booksy/.env` **before real customers arrive** and remove the
  reviews (`ModeratedBy = 'DemoSalonReviewsSeeder'`) — they would read them as genuine.

When the migration below runs, change those five values in `deploy.yml` back to the asan-rezerve names
in the same commit.

Run #110 (4bec8d0) hit a transient GitHub Actions cache-export failure in the API image build
(`error writing layer blob: not_found` while exporting to the Actions cache, after the image itself
built and pushed successfully) — infra flake, unrelated to this change. Deploy job never ran since it
depends on that build. Retried with this commit.

### The one risk that matters: Compose auto-names volumes after the directory

`docker-compose.prod.yml` declares `postgres_data`, `redis_data`, `uploads_data`, etc. with no
explicit `name:`, so Compose prefixes them with the **project name**, which defaults to the
deploy directory's basename. Today that's `/opt/booksy` → volumes are actually named
`booksy_postgres_data`, `booksy_redis_data`, `booksy_uploads_data` on disk. Renaming the directory
to `/opt/asan-rezerve` changes the project name to `asan-rezerve`, so a plain `docker compose up
-d` there would create **new, empty** `asan-rezerve_postgres_data` etc. and the app would boot
against an empty database — the real data would still exist in the old volumes, but the running
app would not see it. This must be handled explicitly, not by just renaming the directory.

### Safe migration order (run once, on the box, as root unless noted)

```bash
# 0. Confirm what exists today before touching anything
docker volume ls | grep booksy
docker ps -a --filter "name=booksy-"

# 1. Stop the app containers WITHOUT removing volumes (no `down -v`, no `-v` anywhere)
cd /opt/booksy && docker compose -f docker-compose.prod.yml stop

# 2. Move the deploy directory (keeps .env, docker-compose.prod.yml, etc.)
mv /opt/booksy /opt/asan-rezerve
cd /opt/asan-rezerve
# Then copy this branch's docker-compose.prod.yml over the one here (git pull, or scp).

# 3. Migrate each named volume's data to the new auto-derived name (repeat for every
#    volume actually in use — check step 0's output; observability's seq_data/pgadmin_data
#    only apply if that profile was ever started)
for vol in postgres_data redis_data uploads_data; do
  docker volume create asan-rezerve_${vol}
  docker run --rm -v booksy_${vol}:/from -v asan-rezerve_${vol}:/to alpine \
    sh -c "cp -av /from/. /to/"
done

# 4. Create the new deploy user, matching the old one's setup (docker group, no sudo)
useradd -m -s /bin/bash asan-rezerve
usermod -aG docker asan-rezerve
chown -R asan-rezerve:asan-rezerve /opt/asan-rezerve

# 5. nginx: install the new vhost files, remove the old ones, test before reload.
#    TLS certs are per-domain (back.nahalkmi.ir etc.), not per-app-name — certbot is unaffected.
cp deployment/nginx/asan-rezerve*.conf /etc/nginx/sites-available/
ln -sf /etc/nginx/sites-available/asan-rezerve.conf /etc/nginx/sites-enabled/
ln -sf /etc/nginx/sites-available/asan-rezerve-provider.conf /etc/nginx/sites-enabled/
rm /etc/nginx/sites-enabled/booksy.conf /etc/nginx/sites-enabled/booksy-provider.conf
nginx -t && systemctl reload nginx

# 6. Bring the app up under the new names, against the migrated volumes
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
curl -s -o /dev/null -w '%{http_code}\n' https://back.nahalkmi.ir/health   # expect 200
# Spot-check a known row count against the pre-migration database to confirm the data followed.

# 7. Re-register the GitHub Actions self-hosted runner under the new user/label
cd /home/booksy/actions-runner && ./svc.sh stop
# As the new `asan-rezerve` user, in a fresh ~/actions-runner: ./config.sh --unattended \
#   --url https://github.com/kazemim99/Booking --token <TOKEN> --name asan-rezerve-prod-1 \
#   --labels asan-rezerve-prod
# As root: ./svc.sh install asan-rezerve && ./svc.sh start
# Update repo secrets: SERVER_USER=asan-rezerve, SERVER_DEPLOY_PATH=/opt/asan-rezerve

# 8. Only after the above is verified stable (a day or two is reasonable), remove the old state:
docker volume rm booksy_postgres_data booksy_redis_data booksy_uploads_data
userdel -r booksy
rm /etc/nginx/sites-available/booksy.conf /etc/nginx/sites-available/booksy-provider.conf
```

GHCR images need no migration step: `deploy.yml` now pushes to a new package
(`ghcr.io/kazemim99/asan-rezerve-api`, `-frontend`) and the first deploy simply builds and pulls
it fresh — there is no old image to carry data forward from.

If Alertmanager's Slack routing (`deployment/monitoring/alertmanager/config.yml`) is actually wired
up to real Slack channels, the channel/webhook names in that file were renamed too (`#booksy-alerts`
→ `#asan-rezerve-alerts`, etc.) — recreate or rename the actual Slack channels before relying on
alerts, since renaming the string in the config does not rename anything on Slack's side.

**Once the migration above is done and verified**, update the rest of this document (the
"Current production state" section and everything under "Architecture" / "Common Commands" /
"GitHub Actions Workflows" below) from `booksy` to `asan-rezerve` to match — it was deliberately
left describing the *current, pre-migration* reality so the commands in it keep working until then.

## Current production state (as of 2026-09-18)

**Live and verified end to end** (health 200, API 200, CORS preflight 204, sandbox OTP login issued a token):

| URL | What | Served by |
|---|---|---|
| `https://back.nahalkmi.ir` | Vue web app + `/api/*` | host nginx → `booksy-frontend` container (`127.0.0.1:8081`), whose own nginx proxies `/api/` → `booksy-api` |
| `https://provider.nahalkmi.ir` | Provider app (Flutter **web** build) | host nginx, static files from `/var/www/booksy-provider` |

- **Server:** `194.1.155.230`, Ubuntu 24.04, 2 vCPU / ~3.8 GB RAM, **shared** with unrelated services
  (see Architecture below). DNS A records for `nahalkmi.ir`, `back.`, `provider.` all point here.
- **Deploy user:** `booksy` (docker group, **no sudo, no root**). Deploy dir `/opt/booksy`
  (`.env` + `docker-compose.prod.yml`). Root SSH is used only for host-level nginx/certbot work.
- **Running containers:** `booksy-api`, `booksy-frontend`, `booksy-postgres`, `booksy-redis`.
  Seq and pgAdmin are **not** running (Compose `observability` profile, off by default).
- **Images:** `ghcr.io/kazemim99/booksy-api:latest`, `ghcr.io/kazemim99/booksy-frontend:latest`
  (repo is public, so pulls need no auth).
- **TLS:** Let's Encrypt via certbot (auto-renew), one cert per subdomain.
- **nginx vhosts:** `deployment/nginx/booksy.conf` and `deployment/nginx/booksy-provider.conf` are
  **copies of what is live** in `/etc/nginx/sites-available/`. Edit the server, then re-sync the repo
  copy (certbot rewrites these files, so the server is authoritative).

### ⚠️ Active security debt — sandbox OTP is ON in production

`/opt/booksy/.env` currently contains:

```
OTP_SANDBOX_CODE=222222
Sms__SandboxMode=true
```

That makes **`222222` a valid OTP for any phone number** on the public host, and suppresses real SMS.
It was enabled deliberately for testing the first deployment. **Remove both lines and
`docker compose -f docker-compose.prod.yml up -d` before any real user signs up.** Tracked as
FOLLOW-UPS #58. (`OtpCode.Generate()` reads `OTP_SANDBOX_CODE` straight from the process environment;
the provider app's OTP input is 6 boxes, so the code must be 6 digits.)

### CI/CD status

`.github/workflows/deploy.yml` (push to `master`): test -> keystone E2E -> build API image,
build frontend image, **build provider web app** (analyze + test + `flutter build web`) -> **deploy**.

The `deploy` job runs on a **self-hosted runner on the production box** (labels `self-hosted`,
`booksy-prod`; runs as `booksy`; installed in `/home/booksy/actions-runner` as a systemd service
`actions.runner.kazemim99-Booking.booksy-prod-1`). It copies `docker-compose.prod.yml` to
`/opt/booksy`, pulls `booksy-api` + `frontend`, `up -d`, waits for `booksy-api` to be healthy,
publishes the provider bundle to `/var/www/booksy-provider` (verified before the old one is
removed), and checks both public URLs.

Why self-hosted (FOLLOW-UPS #59): GitHub-hosted runners cannot reach this server's SSH at all --
`/var/log/auth.log` shows sshd connections from Iranian address ranges only, never a runner, so
the old `appleboy/scp-action` deploy could not work whatever the secrets said. The runner makes
outbound connections only.

Runner operations (as root):

```bash
systemctl status 'actions.runner.*'                       # is it online?
journalctl -u 'actions.runner.*' -n 100                   # its log
cd /home/booksy/actions-runner && ./svc.sh stop|start     # restart it
```

Re-registering (e.g. after removing it in GitHub): get a token from repo Settings -> Actions ->
Runners -> New self-hosted runner, then as `booksy` in `~/actions-runner`:
`./config.sh --unattended --replace --url https://github.com/kazemim99/Booking --token <TOKEN> --name booksy-prod-1 --labels booksy-prod`
and as root `./svc.sh install booksy && ./svc.sh start`.

Security: the repo is public, so a fork's pull request could otherwise edit a workflow to run on
this box. Settings -> Actions -> General -> "Fork pull request workflows from outside
collaborators" must be **"Require approval for all outside collaborators"**. No workflow triggered
by `pull_request` may use `runs-on: [self-hosted, ...]`.

### Manual deploy (fallback when the runner is down)

Backend (after CI has pushed new images):

```bash
scp -i <deploy-key> docker-compose.prod.yml booksy@194.1.155.230:/opt/booksy/
ssh -i <deploy-key> booksy@194.1.155.230 \
  'cd /opt/booksy && docker compose -f docker-compose.prod.yml pull && docker compose -f docker-compose.prod.yml up -d'
curl -s -o /dev/null -w '%{http_code}\n' https://back.nahalkmi.ir/health   # expect 200
```

Provider app (Flutter web):

```bash
cd booksy-provider-app
flutter build web --release --dart-define=API_BASE_URL=https://back.nahalkmi.ir
tar --force-local -czf provider-web.tar.gz -C build/web .   # --force-local: Git Bash reads "C:" as a host
scp -i <deploy-key> provider-web.tar.gz booksy@194.1.155.230:provider-web.tar.gz   # home dir, not /tmp
ssh -i <deploy-key> booksy@194.1.155.230 'set -e
  test -s ~/provider-web.tar.gz
  rm -rf ~/provider-web.new && mkdir ~/provider-web.new
  tar -xzf ~/provider-web.tar.gz -C ~/provider-web.new
  test -f ~/provider-web.new/index.html
  rm -rf /var/www/booksy-provider/* && cp -a ~/provider-web.new/. /var/www/booksy-provider/
  rm -rf ~/provider-web.new ~/provider-web.tar.gz'
```

Never `rm` the live directory before the new bundle is verified on the server: on 2026-09-18 an
scp to `/tmp` reported success but the file was not there for the ssh session (`/tmp` is not a
reliable hand-off on this host), the old one-liner deleted the site first, and provider.nahalkmi.ir
served 403 for about a minute.

Verify the API URL was actually compiled in: `grep -c back.nahalkmi.ir build/web/main.dart.js`
should be ≥1 and `grep -c localhost:5000 build/web/main.dart.js` should be 0. Without the
`--dart-define`, the bundle silently points at `http://localhost:5000` (see
`booksy-provider-app/lib/core/api/config/api_constants.dart`).

### Rolling back the API

Production compose pins `booksy-api:latest`, so "the previous version" is not something compose
remembers. Every CI build also pushes `booksy-api:<sha_short>` (the short commit hash; see the
`build-api` job), and that tag is what a rollback points at.

```bash
# On the box, as booksy. <sha> = the short hash of the last good master commit (git log --oneline).
cd /opt/booksy
cp docker-compose.prod.yml docker-compose.prod.yml.rollback-backup
sed -i 's#booksy-api:latest#booksy-api:<sha>#' docker-compose.prod.yml
docker compose -f docker-compose.prod.yml pull booksy-api
docker compose -f docker-compose.prod.yml up -d booksy-api
docker inspect -f '{{.Config.Image}} {{.State.Health.Status}}' booksy-api   # expect :<sha> healthy
curl -s -o /dev/null -w '%{http_code}\n' https://back.nahalkmi.ir/health        # expect 200
# and from any checkout: BASE=https://back.nahalkmi.ir bash tests/e2e/review-read-smoke.sh
```

The next push to `master` copies the repository's compose file over this one and goes back to
`:latest`, so a rollback lasts until then: land the revert (or the fix) on `master` before anything
else is pushed. The deploy job also leaves the compose file it replaced as
`docker-compose.prod.yml.previous`.

Migrations run at host startup and a rollback does **not** undo them. That is only safe when the
migration is backward compatible — the older image must run on the newer schema. Say so, per
migration, in the section that introduces it (as below). `dotnet ef database update <previous>`
against production is a protected operation and a last resort: it drops what `Down` drops.

### After deploying the QA fixes of 2026-09-22: bookings that may be 3h30 early

Until commit `4363d268` the customer app sent the chosen slot as UTC, so in Tehran a "14:00" booking was stored as
10:30 (and conflict-checked there, leaving the real 14:00 open). A booking does not record which app made it, and
the web app and the salon app always sent the right time — so nothing is rewritten automatically. Instead:

1. Once the fixed customer-app build is live, list the candidates (read-only; set the deploy time):
   ```bash
   docker exec -i booksy-postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"      -v fix_deployed_at="'<deploy time, e.g. 2026-09-23 12:00:00+03:30>'" < deployment/sql/suspect-shifted-bookings.sql
   ```
   Each row is an upcoming online booking made before the fix, with the stored time and `likely_intended_start`
   (+3h30), the customer's name and phone. The query itself is covered by `SuspectShiftedBookingsQueryTests`.
2. The salon calls each customer and, where the time is wrong, reschedules it from its own app — which sends the
   customer the normal "زمان نوبت تغییر کرد" notification, so the customer hears it from the salon, in the app.

### Deploying provider-reviews-and-ratings (migration `AddReviewModerationAndVoting`)

What changes: reviews are moderated (nothing new is public until an administrator approves it),
votes are one per signed-in user, providers can reply (also moderated), and a provider's rating is
computed from published reviews only.

1. **Back up first.** `docker exec booksy-postgres pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --exclude-table-data='observability.log_events*' -Fc > ~/pre-reviews-$(date +%F).dump`
   (values from `/opt/booksy/.env`; the migration backfills existing rows, and a dump is the only undo for data).
2. **Deploy as usual** (push to `master`, or the manual steps above). The migration applies at
   startup. It is **additive only** — new columns with defaults, three new tables
   (`ReviewVotes`, `ReviewReports`, `ProviderRatingSummaries`), indexes, and an idempotent backfill
   that marks every pre-existing review and reply **Published** and keeps the old helpful counts as a
   frozen baseline. Nothing is dropped or renamed on the way up.
3. **Recompute every provider's rating, once.** Before this change nothing ever wrote a provider's
   rating, so every provider still reads 0 until this runs. It needs an administrator token
   (Admin, Administrator or SysAdmin) and is safe to repeat:
   ```bash
   curl -s -X POST https://back.nahalkmi.ir/api/v1/admin/reviews/recompute-ratings \
     -H "Authorization: Bearer <admin access token>"
   # → { "providersRecomputed": N }; a second run changes nothing
   ```
4. **Smoke.** The deploy job now runs `tests/e2e/review-read-smoke.sh` against the public API after
   the health checks: it reads one provider's public review listing and fails the deploy on anything
   but a 200 with a numeric `statistics.totalReviews`. `/health` alone cannot see a broken review read.
   By hand: `BASE=https://back.nahalkmi.ir bash tests/e2e/review-read-smoke.sh`.
5. **Check the moderation queue is reachable** in the admin panel (Reviews). New reviews wait there;
   an empty queue right after deploy is normal, because existing reviews were published by the backfill.

Rollback: the image rollback above is safe for this migration. The older image runs on the new
schema — an old-code review insert lands as `Pending` through the column default (pinned by
`ReviewModerationMigrationTests.A_row_written_by_pre_moderation_code_after_the_migration_lands_in_the_queue`),
and the new tables are simply unused. What the older image loses is the behaviour itself: its
listings show pending reviews again, and anonymous voting comes back. Roll back only for an outage,
not for a behaviour complaint, and do not run the migration's `Down` — it drops every vote, report
and moderation decision made since the deploy.

Client note: anonymous voting is gone (401). No client depends on it (audited in the change's
`tasks.md` 9.1); the web app sends a guest to login and the customer app asks before calling.

### Provider app caching (why a deploy reaches every browser)

Flutter names the whole app `main.dart.js` on every build. Until 2026-09-19 the vhost cached it for
30 days (`public, max-age=2592000`) on the false belief that Flutter hashes its file names, so
browsers kept running an old build after each deploy — a user saw last week's services screen in an
incognito window. Now:

- CI renames the entry file to `main.dart.<sha256-16>.js` and rewrites `mainJsPath` in
  `flutter_bootstrap.js` (`booksy-provider-app/tool/cache_bust_web.sh`); the deploy job refuses a
  bundle whose bootstrap does not name that file.
- nginx caches that hashed file for a year (`immutable`: a new build is a new name), revalidates
  every other app file (`no-cache`, ETag -> 304), and never caches `index.html`,
  `flutter_bootstrap.js` or the service worker.

A manual web build must run `bash tool/cache_bust_web.sh build/web` before upload, or the site
references `main.dart.js` through a bootstrap that still works but caches nothing specially.

### Customer web app delivery (customer.nahalkmi.ir)

The customer app (`booksy-customer-app`, Flutter web) is served by host nginx from
`/var/www/booksy-customer` through `deployment/nginx/booksy-customer.conf`, with the same content-hashed
entry file and cache headers as the provider app above. Measured 2026-09-23, the first visit was a blank
white page for seconds: `main.dart.<hash>.js` (3.9 MB) went out **uncompressed** (the vhost had no
`gzip_types`, and nginx compresses only `text/html` by default) and CanvasKit (2.3 MB) came from
`www.gstatic.com`. Since then:

- `web/index.html` paints a Persian splash from the page alone, removed on Flutter's first frame.
- `web/flutter_bootstrap.js` (a build template) passes `canvasKitBaseUrl: "canvaskit/"`, so CanvasKit
  loads from the site's own `canvaskit/` folder, which `flutter build web` already ships.
- CI (`build-customer-web`) runs `tool/precompress_web.sh build/web` right after the cache-bust step: a
  `gzip -9` copy (`.gz`) next to every `.js/.wasm/.json/.css/.html/.otf/.ttf/.svg` file (the entry file
  3.9 MB → 1.1 MB, `canvaskit.wasm` 7.2 MB → 2.9 MB).
- The vhost's 443 block sets `gzip_static on` (serve those `.gz` files as is) and `gzip on` with
  `gzip_types` for JS, CSS, JSON, the manifest, wasm, SVG and fonts. The cache headers and the SPA
  fallback are unchanged.

**One-time root step to install the vhost — do it BEFORE the deploy that ships this change.** The deploy user
has no root and the deploy job only copies files. The same deploy moves CanvasKit from gstatic's CDN (which
compresses it) to our own site: without this vhost the stock nginx gzips only `text/html`, so the 7.2 MB
`canvaskit.wasm` would go out raw and the first visit would get SLOWER than today. Installing the vhost first is
safe: until the new bundle lands its `gzip on` simply starts compressing today's uncompressed `main.dart.js`.

The box has no checkout of this repository to copy from. The runner's workspace
(`/home/booksy/actions-runner/_work/...`) is not one either: the deploy job sparse-checks out only
`docker-compose.prod.yml` and the two smoke scripts, so `deployment/nginx/` never lands there. Upload the
repo copy from your own checkout, at a commit that contains this change, to the deploy user's home (not
`/tmp`, as in the manual deploy):

```bash
scp -i <deploy-key> deployment/nginx/booksy-customer.conf booksy@194.1.155.230:booksy-customer.conf
```

Then on the box, as root:

```bash
new=/home/booksy/booksy-customer.conf
# certbot may have edited the live file: compare first, carry any server-only line into $new (and back
# into deployment/nginx/booksy-customer.conf in the repo, so the next install does not drop it)
diff /etc/nginx/sites-available/booksy-customer.conf "$new"
cp /etc/nginx/sites-available/booksy-customer.conf /root/booksy-customer.conf.bak-$(date +%F)
cp "$new" /etc/nginx/sites-available/booksy-customer.conf
nginx -t && systemctl reload nginx    # a failed -t changes nothing; the other sites stay up
```

Check it from anywhere (the entry name comes from the live `flutter_bootstrap.js`):

```bash
entry=$(curl -s https://customer.nahalkmi.ir/flutter_bootstrap.js | grep -o 'main\.dart\.[0-9a-f]*\.js' | head -1)
curl -s -o /dev/null -D - -H 'Accept-Encoding: gzip' "https://customer.nahalkmi.ir/$entry" | grep -iE 'content-encoding|content-length|cache-control'
# expect: content-encoding: gzip, content-length ~1.1 MB (not 3.9 MB), cache-control ... immutable
curl -s -o /dev/null -D - -H 'Accept-Encoding: gzip' https://customer.nahalkmi.ir/canvaskit/canvaskit.wasm | grep -i content-encoding
```

To roll back, copy the `.bak` file back and `nginx -t && systemctl reload nginx`. The app itself needs no
rollback: without the vhost change the same bundle is served uncompressed, as before.

### Outbound services the API depends on

- **Nominatim** (`nominatim.openstreetmap.org`) — place search and reverse geocoding for the map
  picker, via `GET /api/v1/Geocoding/search|reverse`. The SERVER makes this call (2026-09-19): from
  a browser it fails on networks that cannot reach that host, and the usage policy does not allow a
  crowd of unidentified clients. Answers are cached 24 h in memory; a restart empties that cache.
  `Geocoding:BaseUrl` overrides the host. Check it from the box with
  `curl -s -o /dev/null -w '%{http_code}' -A 'BooksyProvider/1.0' 'https://nominatim.openstreetmap.org/search?q=tehran&format=jsonv2&limit=1'`.
  A 503 from our endpoint means the upstream failed; the app then keeps whatever the user typed.
- **tile.openstreetmap.org** — map tiles, fetched by the browser directly (no server involvement).
- **Firebase Cloud Messaging** (`fcm.googleapis.com`) — push notifications, sent by the server. Configured
  by `Notifications:Firebase:CredentialsPath` (a mounted service-account JSON) **or**
  `Notifications:Firebase:CredentialsJson` (its contents, for a secret env var); the JSON form wins if both
  are set. **Both blank is a supported state:** the host starts, logs
  `Firebase is not configured; push notifications will be skipped`, and every push is recorded as *skipped*.
  It is never recorded as delivered — that distinction matters, because this service previously returned a
  fabricated success and the delivery log claimed messages that were never sent. A misconfigured credential
  degrades the same way rather than failing startup.
  To check which state a running host is in: `docker logs booksy-api 2>&1 | grep -i firebase`. Expect either
  `Firebase messaging initialised` or the "not configured" warning.
  Note that push also needs registered devices — `POST /api/v1/DeviceTokens` from the apps. With no
  registered device a push is skipped with "The recipient has no registered device", which is not a failure
  and does not consume the notification's retry budget.
  Every message also carries a `webpush` block (Persian, right-to-left, app icon, `tag` = notification id,
  `Urgency: high`) that FCM applies to browser tokens only — see *Web push* below.

### Web push (notifications on the phone, for the web apps)

Both apps run in production as Flutter **web** on Android Chrome, so phone notifications there are browser push
(FCM for web). The code is in place (`openspec/changes/_inline/web-push/tasks.md`); it stays **off** until the
values below exist, and a build without them behaves exactly as before — Firebase is never loaded.

**What you have to create, once** (nothing here is in the repository):

1. **A Web app in the existing Firebase project** — the project whose service-account JSON the API already uses
   (`Notifications:Firebase:CredentialsJson` / `CredentialsPath`, above). Firebase console → Project settings →
   General → *Your apps* → *Add app* → Web (`</>`), nickname e.g. `Booksy web`, no Hosting. One Web app serves
   both sites. From its config snippet copy `apiKey`, `appId`, `messagingSenderId`, `projectId`
   (`authDomain`, `storageBucket`, `measurementId` are not used).
2. **A Web Push (VAPID) key pair** — Project settings → Cloud Messaging → *Web configuration* → *Web Push
   certificates* → *Generate key pair*. Copy the **public** key (the long string shown). The private half stays in
   Firebase: never download it, never put it in GitHub or the repo.
3. **Five GitHub repository variables** — repo → Settings → Secrets and variables → Actions → **Variables** →
   *New repository variable* (secrets with the same names also work, but these are public identifiers — they are
   compiled into every web bundle):

   | Variable | Value |
   |---|---|
   | `FIREBASE_WEB_API_KEY` | `apiKey` |
   | `FIREBASE_WEB_APP_ID` | `appId` (`1:…:web:…`) |
   | `FIREBASE_WEB_MESSAGING_SENDER_ID` | `messagingSenderId` |
   | `FIREBASE_WEB_PROJECT_ID` | `projectId` |
   | `FIREBASE_WEB_VAPID_KEY` | the VAPID **public** key |

4. **The server credential** (a secret, only on the box): `Notifications__Firebase__CredentialsJson` in
   `/opt/booksy/.env` (the service-account JSON of the SAME project, on one line), then
   `docker compose -f docker-compose.prod.yml up -d booksy-api`. Check: `docker logs booksy-api 2>&1 | grep -i
   firebase` → `Firebase messaging initialised`. Initialising only parses the JSON; it does not prove the box can
   reach Google (see *Reachability*).
5. In Google Cloud console for the same project, *Firebase Cloud Messaging API (V1)* must be enabled (Project
   settings → Cloud Messaging shows it). If the browser API key is restricted, allow the referrers
   `https://customer.nahalkmi.ir/*` and `https://provider.nahalkmi.ir/*` and the APIs *Firebase Installations
   API* and *FCM Registration API*.
6. Push to `master`. Each web build job then says `::notice::… web push is configured (project …)`; without the
   variables it says `web push is OFF`.

**How it behaves.** The browser is asked for permission only from a tap: «فعال‌سازی اعلان‌ها» in the customer's
Profile and the salon's More page, and a one-time card (after a booking; on the salon's Home). Signing in never
prompts in a browser; it re-registers a browser that was allowed before. The token is registered with platform
`Web`. A tap on a notification opens the booking (customer: the appointment; salon: the calendar on it), through
sign-in if needed; a push that arrives while the app is on screen shows as a snackbar. The service worker is
`web/push/firebase-messaging-sw.js` (scope `/push/`, served `no-cache` by the existing vhost rule for `.js`).

**Check on a phone.** Chrome on Android → sign in → Profile/More → «فعال‌سازی اعلان‌ها» → Allow. The row must say
«اعلان‌ها روی این دستگاه فعال است» (if it says «اعلان‌ها هنوز به این دستگاه نمی‌رسد», the phone could not reach
Google's registration endpoints). On the box (values from `/opt/booksy/.env`) a `Web` row must appear:

```bash
docker exec -i booksy-postgres psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c \
  'select "Platform", "RegisteredAt" from "ServiceCatalog"."DeviceTokens" where "RevokedAt" is null order by "RegisteredAt" desc limit 5;'
```

Then book as a customer: the salon's phone shows the new-booking notice, and confirming it notifies the customer.

**Reachability — read before promising anything to users.** Every hop of FCM web push is Google:

| Hop | Who connects | Host |
|---|---|---|
| Firebase JS SDK (page and service worker) | the phone | `www.gstatic.com` |
| token: installation + push registration | the phone | `firebaseinstallations.googleapis.com`, `fcmregistrations.googleapis.com` |
| delivery to the phone | Chrome via Google Play services | `fcm.googleapis.com`, `mtalk.google.com:5228` |
| send | the API, from the box | `oauth2.googleapis.com`, `fcm.googleapis.com` |

From Iran these are often unreachable without a VPN: some are filtered, and Google does not offer Firebase / Google
Cloud in Iran (US sanctions) and refuses some of their APIs to Iranian IP addresses — likely the same reason Google
Maven 404s from the workstation. Nothing here could be measured from an Iranian address during development (the
workstation's traffic left through a foreign VPN exit).

This box is on an Iranian network (FOLLOW-UPS #59), so **the send hop itself may fail** even when everything else
is right. Check from the box before relying on it — any HTTP code means reachable, `000` or a timeout means not:

```bash
for u in https://oauth2.googleapis.com/token https://fcm.googleapis.com/; do
  curl -s -o /dev/null -m 15 -w "%{http_code} $u\n" "$u"
done
```

What each failure does: the page cannot load Firebase → push stays off for that visit (bounded, the app is unaffected); the phone
cannot get a token → the row says «…نمی‌رسد» and a tap retries; the box cannot send → the delivery log records a
push failure and the notification's other channels (in-app, and SMS for critical notices) still go out. A salon's
new-booking request has **no SMS**, so for salons push and the in-app inbox are the only channels.

Alternatives, not implemented: an Iranian push provider with its own delivery network (e.g. Pushe, Najva, Chabok)
— they sit beside or replace FCM and need their own SDK and account; standard Web Push with our own VAPID keys (the
`WebPush` protocol, no Firebase project) removes the Firebase-API hops but Chrome still delivers through
`fcm.googleapis.com`; Firefox's push service is Mozilla's. SMS remains the only channel that reliably reaches an
Iranian phone today, and it is reserved for critical notices by decision (2026-09-19).

### Gotchas learned the hard way

- **fail2ban bans the operator.** Many SSH connections in a short time got the working IP banned
  (port 22 timed out while 80/443 and ping still worked). Unban from the VPS provider's web console:
  `fail2ban-client set sshd unbanip <ip>` and `fail2ban-client set sshd addignoreip <ip>`.
- **The operator's VPN exit IP changes.** When it switched, the server became unreachable on *every*
  port from that route while an external probe still saw it healthy. Check `curl https://api.ipify.org`
  before assuming the server is down.
- **nginx here is 1.24.** It does not accept the standalone `http2 on;` directive — use
  `listen 443 ssl http2;`. A failed `nginx -t`/reload keeps the running config, so the other sites on
  the box stay up; always `nginx -t` before `systemctl reload nginx`.
- **certbot turns a placeholder into a redirect loop.** Issuing a cert against a vhost whose only
  `location /` is a `return 301` copies that redirect into the new 443 block (HTTPS → HTTPS forever).
  Write the real 443 server block after issuance.
- **Never enable `ufw`** on this box without allow-listing every service it runs (VPN, proxy panel,
  tunnel ports) — see `deployment/scripts/server-setup.sh`.

## Architecture

Booksy is a **modular monolith**: a single backend host composes multiple bounded contexts in-process.

The reference production deployment (`back.nahalkmi.ir`, 194.1.155.230) is a box **shared with other,
unrelated services** — it is not a dedicated Booksy host. Two consequences that shape everything
below: (1) all container ports bind to `127.0.0.1` only, never `0.0.0.0` — the box's existing
host-level nginx (already serving other domains) is the only thing that ever binds `80`/`443`
publicly, with a Booksy-only vhost (`deployment/nginx/booksy.conf`, one `server_name`) added
alongside the others, never replacing them; (2) there is no `ufw`/firewall automation in
`deployment/scripts/server-setup.sh` — see the caution comment there before ever enabling one on a
shared box. CI deploys over SSH as a dedicated, unprivileged `booksy` user (docker-group member,
no sudo), never root.

### Application Services
- **Booksy.Host** (`booksy-api`, `127.0.0.1:5000` → internal `80`): Single ASP.NET Core host that composes both bounded contexts (UserManagement and ServiceCatalog) in-process and serves all of their controllers under `/api/v1/...`. (A Booking context exists only as empty scaffolding and is not built.) Database migrations run at host startup. The host port binding is for operator debugging only — the frontend container reaches it over `booksy-network` by container name, never through this port.
- **Frontend** (`127.0.0.1:8081` → internal `80`): Web application frontend served via Nginx inside the container; its nginx config proxies `/api` to `booksy-api:80` over the Docker network. The host's own nginx (outside Docker) is what the public internet actually reaches, terminating TLS and reverse-proxying to this port — see `deployment/nginx/booksy.conf`.

### Infrastructure Services
- **PostgreSQL** (`127.0.0.1:5432`): Single primary database (`booksy`) with schema-per-context (schemas: `user_management`, `ServiceCatalog`, `cap`). One connection string (`DefaultConnection`).
- **Redis** (`127.0.0.1:6379`): Caching layer with LRU eviction policy (192MB limit on the shared reference box; raise it in `docker-compose.prod.yml` if you have more headroom)
- **Seq** (`127.0.0.1:5341`, `127.0.0.1:5342`) and **pgAdmin** (`127.0.0.1:5050`): OFF by default (Compose `profiles: ["observability"]`) — optional, RAM-hungry admin tools that aren't required for the app to run. Start them with `docker compose --profile observability up -d` if the box has headroom **and** set `SEQ_SERVER_URL=http://seq:5341` in `.env` (the Seq sink is off when it is empty, the default); otherwise use an SSH tunnel + a local pgAdmin/DBeaver. Logs do not need Seq: the admin panel's **Logs** page reads the API's own database log store (`observability` schema, 14 days) — see `docs/OBSERVABILITY.md`.

### Service Communication
- All containers connect via a Docker bridge network (`booksy-network`, subnet 172.25.0.0/16)
- Containers communicate using container names as DNS hostnames
- Cross-context integration events run **in-process** via CAP (DotNetCore.CAP) on its in-memory transport (`EventBus:Provider=InMemory`) — there is no message broker container
- Redis provides distributed caching and session management

## Common Commands

### Deployment
```bash
# Every push to master runs this automatically (.github/workflows/deploy.yml).
# To do the same thing by hand:
cd /opt/booksy
docker compose -f docker-compose.prod.yml pull

# Clean up orphaned containers (prevents network removal errors)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f || true

docker compose -f docker-compose.prod.yml down --remove-orphans
docker compose -f docker-compose.prod.yml up -d

# View all service status
docker compose -f docker-compose.prod.yml ps

# View logs for specific service
docker compose -f docker-compose.prod.yml logs -f [service-name]
# Example: docker compose -f docker-compose.prod.yml logs -f booksy-api

# View logs for all services
docker compose -f docker-compose.prod.yml logs -f
```

### Service Management
```bash
# Start all services
docker compose -f docker-compose.prod.yml up -d

# Stop all services
docker compose -f docker-compose.prod.yml down

# Restart a specific service
docker compose -f docker-compose.prod.yml restart [service-name]

# Scale a service (if supported)
docker compose -f docker-compose.prod.yml up -d --scale booksy-api=3
```

### Database Operations
```bash
# Access PostgreSQL shell
docker exec -it booksy-postgres psql -U booksy_admin -d booksy_user_management

# Create database backup (stored logs left out: tables kept, rows not — see docs/OBSERVABILITY.md)
docker exec booksy-postgres pg_dump -U booksy_admin --exclude-table-data='observability.log_events*' booksy_user_management > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore from backup
docker exec -i booksy-postgres psql -U booksy_admin booksy_user_management < backup.sql

# View database logs
docker logs booksy-postgres
```

### Redis Operations
```bash
# Access Redis CLI
docker exec -it booksy-redis redis-cli -a YourRedisPassword123!

# Monitor Redis commands in real-time
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! MONITOR

# Check Redis memory usage
docker exec -it booksy-redis redis-cli -a YourRedisPassword123! INFO memory
```

### Integration Events (CAP)

Cross-context integration events run in-process via CAP on its in-memory transport — there is no RabbitMQ broker. CAP persists outbox/inbox state in the `cap` schema of the PostgreSQL database. To inspect published/received messages, query the CAP tables in Postgres or use the Seq logs.

### Monitoring and Logging
```bash
# Access Seq logging UI
# Open browser to: http://server-ip:5341

# View container resource usage
docker stats

# Check health status of all services
docker compose -f docker-compose.prod.yml ps

# View specific service health
docker inspect --format='{{.State.Health.Status}}' booksy-[service-name]
```

### API Documentation (Swagger)
```bash
# Access Swagger UI on the single host:
# Booksy API: http://server-ip:5000/swagger

# Note: The service must be healthy for Swagger to be accessible
# Check service health: docker ps
```

### Cleanup and Maintenance
```bash
# Remove stopped containers and unused images
docker system prune -a

# Remove only unused images
docker image prune -f

# View disk usage by Docker
docker system df

# Clean up orphaned booksy containers (all containers with 'booksy-' prefix)
docker ps -a --filter "name=booksy-" --format "{{.Names}}" | xargs -r docker rm -f

# Clean up old backups (manual)
cd /opt/booksy/backups && ls -lt | tail -n +10 | awk '{print $9}' | xargs rm -f
```

## GitHub Actions Workflows

### Deploy to Production (`deploy.yml`)
One workflow does the whole thing on every push to `master` (or manual `workflow_dispatch`):
- `test`: unit tests against ephemeral Postgres/Redis service containers
- `e2e-keystone`: boots the real host and runs the keystone booking-flow smoke test
- `build-api` / `build-frontend`: build and push Docker images to GHCR (`ghcr.io/kazemim99/booksy-api`, `-frontend`), tagged `latest` and the commit SHA — run in parallel once tests pass
- `deploy`: SCPs `docker-compose.prod.yml` to the server, SSHes in as the dedicated `booksy` user, pulls the new images, and runs `docker compose up -d` (no `--profile observability` — Seq/pgAdmin stay off)
- A final `Health check` step curls `https://back.nahalkmi.ir/health` (through the box's own nginx, not the bare server IP — see the Architecture note above on why that matters on a shared box) and fails the run if it's not `200`

Required repo secrets (Settings → Secrets and variables → Actions): `SERVER_HOST`, `SERVER_USER`
(`booksy`), `SERVER_SSH_KEY` (the CI-only deploy key's private half — never a personal key),
`SERVER_DEPLOY_PATH` (`/opt/booksy`).

## Environment Configuration

All environment variables are stored in `/opt/booksy/.env`. Key variables include:

- **Database**: `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
- **Redis**: `REDIS_PASSWORD`
- **Seq**: `SEQ_FIRSTRUN_ADMINUSERNAME`, `SEQ_FIRSTRUN_ADMINPASSWORD`; `SEQ_SERVER_URL` (empty = no Seq sink)
- **Container Registry**: `GITHUB_REPOSITORY_OWNER` (currently: kazemim99)

**Observability and caching (from 2026-09-26, `add-observability-and-caching`).** No new variable is required. On the
first start the API creates the `observability` schema (log store, log-level overrides); if that fails the API still
runs and the admin Logs page reports the store as not ready. The cache now really uses `REDIS_CONNECTION_STRING`
(before, it pointed at localhost inside the container and never reached Redis); Redis keys move from `RateLimit_*` to
`asanrezerve:*`, so rate-limit windows restart once and old keys expire on their own. Log volume drops: SQL commands and
ASP.NET Core routing are no longer logged at Information. Stored logs live in `observability.log_events`, one
partition per UTC day, dropped whole after 14 days; database dumps should leave their rows out
(`--exclude-table-data='observability.log_events*'`, as the backup script does) — the tables and the log-level
overrides are still dumped. The raw `postgres_data` volume archive still contains them. Watch the store's size on Logs ›
Overview › Log store. Details: `docs/OBSERVABILITY.md`.

Never commit the `.env` file to version control. The `.env.backup` file should also be excluded from commits.

## Health Checks

All services have health checks configured using **curl**:

- **Backend API** (`booksy-api`): HTTP check on `/health` endpoint using `curl -f` (30s interval, 10s timeout, 3 retries, 40s start period)
- **PostgreSQL**: `pg_isready` command (10s interval, 5s timeout, 5 retries, 10s start period)
- **Redis**: `redis-cli ping` (10s interval, 5s timeout, 3 retries, 5s start period)
- **Seq**: HTTP check on `/api/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)
- **Frontend**: HTTP check on `/health` using `curl -f` (30s interval, 10s timeout, 3 retries, 20s start period)

**Important**: All Docker images must have `curl` installed for health checks to work. The Dockerfiles in the source repository include curl installation:
- .NET host: `apt-get install -y curl`
- Frontend (nginx:alpine): `apk add --no-cache curl`

## Resource Limits

Services have CPU and memory constraints:

- **Backend API** (`booksy-api`): 1 CPU / 1GB RAM (reserved: 0.25 CPU / 256MB)
- **Frontend**: 0.5 CPU / 256MB RAM (reserved: 0.1 CPU / 64MB)
- **PostgreSQL**: 2 CPU / 2GB RAM (reserved: 0.5 CPU / 512MB)
- **Redis**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **Seq**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)
- **pgAdmin**: 0.5 CPU / 512MB RAM (reserved: 0.1 CPU / 128MB)

## Service Dependencies

The startup order is enforced through Docker Compose dependencies:

1. **Infrastructure** (PostgreSQL, Redis, Seq) starts first
2. **Backend host** (`booksy-api`) waits for infrastructure health checks, then runs migrations at startup
3. **Frontend** waits for `booksy-api` to be healthy

## Security Considerations

- Database and cache ports (5432, 6379) are bound to localhost only (`127.0.0.1`)
- Passwords should be changed from defaults in `.env` before production use
- GitHub Container Registry authentication is required for pulling images
- SSH key-based authentication is used for deployment automation
- All services run in an isolated Docker network with defined subnet

## Troubleshooting

Common issues and solutions:

### Other Common Issues

1. **Service won't start**: Check logs with `docker compose logs [service]` (e.g., `booksy-api`) and verify health check status
2. **Database connection errors**: Ensure PostgreSQL is healthy and connection string in `.env` is correct
3. **Out of memory**: Check `docker stats` and adjust resource limits in docker-compose.prod.yml
4. **Image pull failures**: Verify GHCR authentication with `docker login ghcr.io`
5. **Port conflicts**: all container ports bind `127.0.0.1` only (5000, 5432, 6379, 8081, and 5341/5342/5050 if the observability profile is running) — check with `ss -tlnp` on the host for anything else already bound to those before deploying to a new box. `80`/`443` belong to the host's own nginx, shared with whatever else it serves; Booksy's vhost is one `server_name` block among others (`deployment/nginx/booksy.conf`) — never delete or overwrite the others
6. **Swagger not accessible**: Verify `booksy-api` is healthy with `docker ps`. An unhealthy host cannot serve Swagger UI.

