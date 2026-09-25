// Template for build/web/flutter_bootstrap.js (flutter build web fills in the double-brace tokens).
//
// The default template passes no canvasKitBaseUrl, so the engine fetched CanvasKit (~2.3 MB) from www.gstatic.com:
// a second origin to connect to, out of our cache headers and gzip, and unreachable on networks that block Google.
// `flutter build web` already copies canvaskit/ into the bundle; this points the loader at that copy.
//
// tool/cache_bust_web.sh rewrites "mainJsPath" inside the build config after the build: keep that token.
{{flutter_js}}
{{flutter_build_config}}

_flutter.loader.load({
  serviceWorkerSettings: {
    serviceWorkerVersion: {{flutter_service_worker_version}},
  },
  config: {
    canvasKitBaseUrl: "canvaskit/",
  },
});
