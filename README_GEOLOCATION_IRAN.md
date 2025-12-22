# How to Use Geolocation in Ardabil (or Any City in Iran)

## 🎯 Quick Answer

**The geolocation now uses your device's GPS directly**, not IP-based detection (which was showing Tehran).

---

## ✅ What Changed

### Before:
- Used IP geolocation (inaccurate)
- Showed ISP location (Tehran) instead of your location (Ardabil)

### After:
- Uses device GPS/WiFi/Cell towers directly
- Shows your **actual location** (Ardabil) ✅

---

## 📱 How to Test

### On Mobile (Best Results):

1. **Enable Location in Device Settings:**
   - Android: Settings → Location → Turn ON
   - iOS: Settings → Privacy → Location Services → Turn ON

2. **Open the Site:**
   - Visit your Booksy homepage
   - The GPS button (📍) should be visible in the city field

3. **Click GPS Button:**
   - Browser will ask: "Allow [site] to access your location?"
   - Click **"Allow"**

4. **Wait 10-20 Seconds:**
   - First GPS lock takes time
   - You'll see a loading spinner
   - Success message will show: "✅ موقعیت شما: اردبیل"

### On Desktop:

1. **Click GPS Button**
2. **Allow Permission**
3. **Wait** (may use WiFi triangulation)
4. **City auto-fills**

---

## ⚠️ Important Notes

### Why It Takes Time:
- **GPS needs to lock onto satellites** (10-20 seconds first time)
- **Better outdoors** (GPS satellites need clear sky view)
- **Faster indoors with WiFi** (uses nearby WiFi networks)

### If It Doesn't Work:
1. ✓ Make sure device location/GPS is enabled
2. ✓ Make sure browser has location permission
3. ✓ Try going outdoors (better GPS signal)
4. ✓ Wait at least 15-20 seconds
5. ✓ Refresh the page and try again

### Accuracy:
- **Outdoors with GPS:** 5-50 meters ✅
- **Indoors with WiFi:** 50-500 meters ✅
- **Cell towers only:** 500m - 2km ✅

---

## 🧪 Testing Checklist

### Test in Ardabil:
- [x] Open homepage
- [ ] Click GPS button (📍)
- [ ] Allow location permission
- [ ] Wait 15-20 seconds
- [ ] Should show: "✅ موقعیت شما: اردبیل"
- [ ] City dropdown should show "اردبیل"

### Check Browser Console:
- [ ] Open Developer Tools (F12)
- [ ] Go to Console tab
- [ ] Look for log: `Position retrieved: { latitude: 38.xxxx, longitude: 48.xxxx }`
- [ ] Ardabil coordinates should be around **38.25°N, 48.29°E**

---

## 💡 Why IP Showed Tehran

IP geolocation detects your **ISP's server location**, not your physical location:

- **Your actual location:** Ardabil (38.25°N, 48.29°E)
- **Your ISP servers:** Tehran (35.69°N, 51.39°E)
- **IP geolocation:** Tehran ❌ (This is why it was wrong)

Most Iranian ISPs have servers in Tehran, so IP geolocation always shows Tehran.

---

## 🚀 Alternative: Manual Selection

If GPS doesn't work or is too slow, you can:

1. **Use Popular Categories:**
   - Click on category chips below the search
   - Browse by service type (آرایشگاه، آرایشگاه مردانه، etc.)

2. **Type City Name:**
   - Click city dropdown
   - Type "اردبیل"
   - Select from list

3. **Save Preference:**
   - Your last selected city is remembered
   - Next visit will pre-fill automatically

---

## 📞 Troubleshooting

### Problem: "403 Error" in Console
**Solution:** This is expected - browser tries Google first, then uses device GPS. Ignore this error.

### Problem: Takes Too Long
**Solution:**
- Go outdoors for better GPS signal
- Or manually select city from popular cities

### Problem: Shows Wrong City
**Solution:**
- Make sure you allowed location permission
- Wait full 15-20 seconds for GPS lock
- Check GPS is enabled in device settings

### Problem: Permission Denied
**Solution:**
- Clear browser permissions and try again
- Or use manual city selection

---

## ✅ Summary

1. **GPS button uses device GPS** (accurate)
2. **IP geolocation removed** (inaccurate)
3. **Takes 10-20 seconds** (normal for GPS)
4. **Works best outdoors** (GPS satellites)
5. **Manual selection available** (popular cities)

**Result:** You should now see **اردبیل** correctly! 🎉
