# Geolocation Debugging Guide

**Issue:** Getting "موقعیت شما: نامشخص" instead of actual city name

---

## 🔍 How to Debug

### Step 1: Open Browser Console

1. Open your homepage (`http://localhost:5173`)
2. Press `F12` to open Developer Tools
3. Go to "Console" tab
4. Reload the page

### Step 2: Check Console Logs

Look for these log messages:

```
[HeroSection] Auto-detecting location on page load...
[GeolocationService] Requesting current position...
[GeolocationService] Position retrieved: {...}
[GeolocationService] Reverse geocoding: 35.xxxx, 51.xxxx
[GeolocationService] Reverse geocode result: {...}
[HeroSection] Location auto-detected: {...}
[HeroSection] Address details: {...}
```

### Step 3: Check the API Response

The most important log is **"Reverse geocode result"**.

**Copy the entire JSON object** from this log and paste it here.

It should look something like this:

```json
{
  "city": "تهران",
  "state": "تهران",
  "district": "منطقه ۶",
  "neighbourhood": "ولیعصر",
  "formatted_address": "تهران، منطقه ۶، خیابان ولیعصر",
  "route_name": "خیابان ولیعصر"
}
```

OR it might be wrapped:

```json
{
  "data": {
    "city": "تهران",
    // ... rest
  }
}
```

---

## 🔧 Common Issues & Solutions

### Issue 1: Neshan API Key Invalid

**Error in console:** `Neshan API error: 401` or `403`

**Solution:**
1. Check [`.env.development`](booksy-frontend/.env.development)
2. Make sure `VITE_NESHAN_SERVICE_KEY` is set correctly
3. Get a new key from: https://platform.neshan.org/dashboard/keys

### Issue 2: CORS Error

**Error in console:** `CORS policy: No 'Access-Control-Allow-Origin' header`

**Solution:**
- Neshan API should allow requests from localhost
- If blocked, you may need to test on a deployed domain

### Issue 3: Wrong Field Name

**Issue:** API returns city in a different field (e.g., `municipal_area` instead of `city`)

**Solution:** I've already added fallbacks for:
- `city`
- `municipal_area`
- `district`
- `locality`
- `neighbourhood`

If your city is in a different field, tell me the field name and I'll add it.

### Issue 4: Nested Response

**Issue:** Response is wrapped in `data` or other property

**Solution:** Already handled with:
```typescript
const responseData = data.data || data
```

---

## 🧪 Manual Test

### Test the Neshan API directly:

1. Open a new browser tab
2. Paste this URL (replace with your coordinates):

```
https://api.neshan.org/v5/reverse?lat=35.6892&lng=51.3890
```

3. Add your API key in headers:
   - Open Developer Tools (F12)
   - Go to Network tab
   - Refresh the page
   - Click the request
   - Go to "Headers" section
   - Copy the response

**OR** use curl:

```bash
curl -X GET "https://api.neshan.org/v5/reverse?lat=35.6892&lng=51.3890" \
  -H "Api-Key: service.qBDJpu7hKVBEAzERghfm9JM7vqGKXoNNNTdtrGy7"
```

Replace coordinates with your location.

---

## 📝 What to Send Me

To help you fix this, please send me:

1. **Console logs** - All logs starting with `[GeolocationService]` and `[HeroSection]`
2. **API Response** - The raw JSON from "Reverse geocode result"
3. **Your coordinates** - The lat/lng values from the logs
4. **Error messages** - Any red errors in console

Example format:

```
Console Output:
[GeolocationService] Reverse geocode result: {
  "status": "Ok",
  "data": {
    "city": null,
    "municipal_area": "تهران",
    "state": "تهران"
  }
}

Coordinates: 35.6892, 51.3890
```

---

## 🔧 Quick Fix Options

### Option 1: If city is in different field

Tell me which field has the city name, and I'll update the code.

### Option 2: If API requires authentication

Check if your API key is valid:
- Login to https://platform.neshan.org/
- Go to Dashboard → Keys
- Create a new "Service" key (not "Map" key)
- Update `.env.development`

### Option 3: If API response format is different

Send me the actual response and I'll update the parsing logic.

---

## 🎯 Expected Behavior

After the fix, you should see:

1. **Console:**
   ```
   [HeroSection] Location auto-detected
   [HeroSection] City auto-filled on load: تهران
   ```

2. **UI:**
   - City dropdown shows "تهران" (or your actual city)
   - Success message: "✅ موقعیت شما: تهران"
   - No "نامشخص" message

---

## 📞 Next Steps

1. Open the homepage
2. Open browser console (F12)
3. Reload the page
4. Copy all the logs
5. Send me the logs

I'll analyze the response and fix the parsing logic immediately! 🚀
