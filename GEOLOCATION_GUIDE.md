# Geolocation Guide

How Booksy's homepage location auto-detection works, how to test it, and how to debug it when it shows the wrong city.

## How it works

The homepage uses the device's **GPS/WiFi/cell-tower location** directly (via the browser Geolocation API), not IP-based geolocation. IP geolocation was removed because it resolves to the user's ISP server location (in Iran, usually Tehran) rather than their actual location — e.g. a user in Ardabil would see "تهران" instead of "اردبیل".

Flow: `HeroSection` requests the current position → `GeolocationService` reverse-geocodes the coordinates via the Neshan API → the resolved city/state auto-fills the search location.

## How to test

### On mobile (best results)
1. Enable location in device settings (Android: Settings → Location; iOS: Settings → Privacy → Location Services)
2. Open the Booksy homepage — the GPS button (📍) is in the city field
3. Click the GPS button and allow the location permission prompt
4. Wait 10–20 seconds for the first GPS lock; a success message shows the detected city (e.g. "✅ موقعیت شما: اردبیل")

### On desktop
Same flow; desktop typically falls back to WiFi triangulation, which is faster but less precise.

**Accuracy**: GPS outdoors ~5–50m, WiFi indoors ~50–500m, cell towers only ~500m–2km. GPS needs a clear sky view and can take 10–20 seconds on first lock.

### Manual fallback
If GPS doesn't work or is too slow: use a category chip, type the city name in the dropdown, or rely on the last-selected city (remembered across visits).

## How to debug

### 1. Open the browser console
`F12` → Console tab, then reload the homepage. Look for this log sequence:
```
[HeroSection] Auto-detecting location on page load...
[GeolocationService] Requesting current position...
[GeolocationService] Position retrieved: {...}
[GeolocationService] Reverse geocoding: 35.xxxx, 51.xxxx
[GeolocationService] Reverse geocode result: {...}
[HeroSection] Location auto-detected: {...}
```

### 2. Inspect the "Reverse geocode result" log
This is the raw Neshan reverse-geocode response. It may be flat (`{ "city": "...", ... }`) or wrapped (`{ "data": { "city": "...", ... } }`) — the parser already handles both (`const responseData = data.data || data`) and falls back across `city`, `municipal_area`, `district`, `locality`, `neighbourhood` if the city name lands in a different field.

### Common issues

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Neshan API error: 401`/`403` in console | Invalid/missing API key | Check `VITE_NESHAN_SERVICE_KEY` in `booksy-frontend/.env.development`; get a fresh **Service** key (not a Map key) from the [Neshan platform dashboard](https://platform.neshan.org/dashboard/keys) |
| CORS error | Neshan API not allowing the origin | Should work on localhost by default; if blocked, test on a deployed domain |
| City shows in an unexpected JSON field | Neshan response shape varies by area | The fallback chain above should already cover it; if not, add the new field name to the parser |
| A `403` appears but location still resolves | The browser tries a Google-backed provider first, then falls back to device GPS | Expected — ignore |
| "موقعیت شما: نامشخص" (unknown) | City field genuinely missing from the geocode result, or GPS lock timed out | Confirm coordinates in the "Position retrieved" log are plausible, retry outdoors, or manually select the city |

### Manual API test
```bash
curl -X GET "https://api.neshan.org/v5/reverse?lat=35.6892&lng=51.3890" \
  -H "Api-Key: <your Neshan service key>"
```
Swap in real coordinates and your own key from `.env.development` — never paste a live key into a doc or commit.
