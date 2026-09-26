# AsanRezerve brand assets

The nine SVGs in this folder are the designer's source files (2026-09-26). Everything shipped —
Flutter launcher icons, native splash screens, web favicons, and the in-app/in-page brand marks —
is generated or hand-copied from these, never edited separately. Change a logo here, then re-run:

```
pip install pillow
python branding/render_icons.py
```

`render_icons.py` rasterises the PNG/ICO files (Android mipmaps, iOS `AppIcon`/`LaunchImage`, web
favicons and PWA icons); it needs a Chromium binary (the Playwright cache, `$CHROME`, or `--chrome`).
The Android adaptive-icon vectors, the notification-icon silhouette, and the `BrandMark` widgets/
components (Flutter, Vue) are hand-written from the same 100×100 geometry and are not touched by the
script — see the comment at each one's top for how it's kept in sync (`brand_mark_test.dart` in each
Flutter app fails if a widget and its SVG drift apart).

## The files

| File | What it is |
|---|---|
| `symbol-color.svg` | The mark alone: an "A" of two blue strokes whose crossbar is a green check. |
| `symbol-mono-black.svg`, `symbol-mono-blue.svg` | One-colour versions (print, single-colour use). |
| `symbol-reversed-blue.svg`, `symbol-reversed-dark.svg` | The mark in white, full-bleed on the brand blue or on ink — for icons/badges an OS or app draws on its own background. |
| `appicon-blue.svg`, `appicon-white.svg` | The rounded-square app icon, ready for an app store: white mark on blue, or blue-and-green mark on white. |
| `horizontal-persian-color.svg`, `horizontal-latin-color.svg` | The full lockup (mark + wordmark + tagline) in Persian and Latin. Reference only: the web/admin headers draw the mark inline and set the wordmark in the app's own font rather than `<img>`-ing these, because an `<img>` can't run the SVG's `@import` of Google Fonts. |

## Where each one is used

| Asset | Used for |
|---|---|
| `appicon-blue` | Customer app launcher icon: Android (legacy + adaptive + Android 13 themed), web PWA, Windows `.ico`. |
| `appicon-white` | Provider app launcher icon — so a salon owner who has both apps installed can tell them apart at a glance. |
| `symbol-reversed-blue` | Full-bleed square for the customer app's iOS-style and maskable-PWA icons (the OS applies its own mask, so the square must already be the brand colour edge-to-edge). |
| `symbol-color` | Native splash screens (Android/iOS), the in-app Flutter `SplashPage`, web favicons, and the web/admin header/sidebar logo marks — all on a light background. |
| `symbol-reversed-dark` | Provider app's Android splash source geometry when the window is dark (`values-night`); the provider iOS/Android splash otherwise sits on white like the customer app. |
| `symbol-mono-black` / `symbol-mono-blue` | Kept for print/one-colour use; the same geometry, drawn solid white, is also the Android status-bar notification icon (`ic_notification.xml` in each app) and the Android 13+ themed-icon monochrome layer. |
| `horizontal-persian-color` / `horizontal-latin-color` | Reference lockup for marketing/print. Not embedded as-is (see note above). |

## Brand colours

| Token | Hex | Use |
|---|---|---|
| Letter (blue) | `#3777C0` | The "A" strokes; app bars, primary actions in both Flutter apps and the web/admin theme. |
| Check (green) | `#0AC075` | The crossbar/check; "success" colour in both Flutter apps' token files. |
| Ink | `#0F1724` | Dark backgrounds for the reversed mark; headings in the web/admin theme. |

These already matched the apps' existing design tokens (`AppColors.appBar`/`success` in both Flutter
apps) when the designer delivered the set, so no palette changes were needed anywhere else.
