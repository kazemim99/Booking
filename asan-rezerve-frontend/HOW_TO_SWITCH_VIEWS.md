# How to Switch Between Grid, List, and Map Views

## Quick Guide

### Switching TO Map View
When you're in **Grid** or **List** view:
1. Look for the view toggle buttons at the top-right of the results
2. Click the **Map icon** button (labeled "نمایش نقشه")
3. The map view will load immediately

```
┌─────────────────────────────────────┐
│  Results Header                     │
│  [Grid] [List] [Map] ← Click here  │
└─────────────────────────────────────┘
```

---

### Switching FROM Map View (Back to Grid/List)
When you're in **Map** view:
1. Look at the **map header** at the top of the map view
2. You'll see three toggle buttons: Grid | List | Map (active)
3. Click the **Grid icon** to return to grid view
4. Or click the **List icon** to return to list view

```
┌─────────────────────────────────────┐
│  نمایش نقشه                         │
│  X ارائه‌دهنده                      │
│                                     │
│  [Grid] [List] [Map✓] ← Click Grid or List
└─────────────────────────────────────┘
│                                     │
│  🗺️  Map Area                       │
│                                     │
│  [Provider Cards at bottom...]      │
└─────────────────────────────────────┘
```

---

## Detailed Instructions

### From Grid View → Map View
1. **Current View**: You're seeing providers in a grid layout with cards
2. **Action**: Click the third button (map icon) in the view toggle
3. **Result**:
   - Grid disappears
   - Map appears with provider markers
   - Header shows "نمایش نقشه" and provider count
   - Floating cards appear at bottom
   - View toggle shows Map button as active (purple)

### From Map View → Grid View
1. **Current View**: You're seeing the interactive map with markers
2. **Action**: Click the first button (grid icon) in the map header
3. **Result**:
   - Map disappears
   - Grid layout returns
   - Provider cards are displayed in grid format
   - Original results header is restored

### From Map View → List View
1. **Current View**: You're seeing the interactive map with markers
2. **Action**: Click the second button (list icon) in the map header
3. **Result**:
   - Map disappears
   - List layout returns
   - Provider cards are displayed in list format (full width)
   - Original results header is restored

---

## Visual Button Reference

### Grid Icon (نمایش شبکه‌ای)
```
┌─┬─┐
├─┼─┤  ← Grid pattern icon
└─┴─┘
```
Shows providers in a responsive grid (2-3 columns on desktop, 1 column on mobile)

### List Icon (نمایش لیستی)
```
≡  ← Three horizontal lines
≡
≡
```
Shows providers in a vertical list (one per row, full width)

### Map Icon (نمایش نقشه)
```
📍  ← Location/Map icon
🗺️
```
Shows providers on an interactive Neshan Map with markers

---

## Button States

### Inactive Button
- Background: Transparent or light gray
- Icon color: Gray (#6b7280)
- Hover: White background

### Active Button
- Background: Purple (#8b5cf6)
- Icon color: White
- No hover needed (already selected)

---

## Mobile Experience

On mobile devices (< 768px width):
- The map header stacks vertically
- Toggle buttons appear in a single row below the title
- All three buttons remain accessible
- Same click behavior as desktop

---

## Keyboard Shortcuts (Future Enhancement)
Currently not implemented, but potential shortcuts could be:
- `G` - Switch to Grid view
- `L` - Switch to List view
- `M` - Switch to Map view
- `Esc` - Close map view (return to previous view)

---

## Troubleshooting

### Can't see toggle buttons in map view?
- **Check**: The buttons are in the map header, not the original results header
- **Location**: Top of the map container, right side on desktop
- **On mobile**: They appear below the "نمایش نقشه" title

### Map view button not working?
- **Check browser console** for errors
- Ensure Neshan Maps API keys are configured in `.env.development`
- Verify providers have valid coordinates (lat/lng)

### View doesn't change when clicking buttons?
- **Check**: Event handlers are connected
- **Verify**: `@view-mode-change` event is wired in ProviderSearchView
- **Console**: Look for "[ProviderSearchView] View mode change" log messages

---

## Implementation Details

The view switching works through:
1. **Pinia Store** (`provider.store.ts`) manages `viewMode` state
2. **localStorage** persists your view preference between sessions
3. **Conditional Rendering** in ProviderSearchView shows the correct component
4. **Event Bubbling** - MapViewResults emits `viewModeChange` → ProviderSearchView handles it

---

## Testing Your View Switches

Try this sequence to verify everything works:
1. ✅ Start in Grid view (default)
2. ✅ Switch to List view (click list icon)
3. ✅ Switch to Map view (click map icon)
4. ✅ Click a provider marker on the map
5. ✅ Switch back to Grid view (click grid icon in map header)
6. ✅ Refresh page - your view preference should be remembered
7. ✅ Switch to Map view again
8. ✅ Switch to List view from map header

All transitions should be smooth and instant! 🚀
