import 'dart:convert';
import 'dart:io';

import 'package:asan_rezerve_customer_app/config/theme/app_colors.dart';
import 'package:asan_rezerve_customer_app/core/constants/app_strings.dart';
import 'package:flutter/painting.dart';
import 'package:flutter_test/flutter_test.dart';

/// Guards the web shell: the HTML page, manifest and loader a browser gets before any Dart runs, and the delivery
/// of that bundle (CI precompression, the nginx vhost).
///
/// None of this is Dart, so no widget test can see it. A regression here (the Flutter placeholder title coming back,
/// CanvasKit loading from gstatic.com again, the vhost losing gzip) compiles and tests clean while the customer
/// stares at a blank white page for seconds. These tests read the files directly (run from the package root).
void main() {
  String read(String path) {
    final file = File(path);
    expect(file.existsSync(), isTrue, reason: 'Expected $path (run `flutter test` from asan-rezerve-customer-app)');
    return file.readAsStringSync();
  }

  String hex(Color c) => '#${(c.toARGB32() & 0xFFFFFF).toRadixString(16).padLeft(6, '0').toUpperCase()}';

  /// The content attribute of `<meta name="...">`.
  String? meta(String html, String name) => RegExp('<meta\\s+name="${RegExp.escape(name)}"\\s+content="([^"]*)"')
      .firstMatch(html)
      ?.group(1);

  group('index.html', () {
    late String html;
    setUpAll(() => html = read('web/index.html'));

    test('is a Persian, right-to-left document', () {
      expect(html, contains('<html lang="fa" dir="rtl">'));
    });

    test('names the app with its existing Persian name and tagline, not the Flutter placeholders', () {
      expect(AppStrings.appDocumentTitle, contains(AppStrings.homeTitle));
      expect(AppStrings.appDocumentTitle, contains(AppStrings.appTagline));
      expect(html, contains('<title>${AppStrings.appDocumentTitle}</title>'));
      expect(meta(html, 'description'), contains(AppStrings.appTagline));
      expect(meta(html, 'apple-mobile-web-app-title'), AppStrings.homeTitle);
      expect(html, isNot(contains('asan_rezerve_customer_app')));
      expect(html, isNot(contains('A new Flutter project')));
    });

    test('colours the browser chrome like the app bar', () {
      expect(meta(html, 'theme-color')?.toUpperCase(), hex(AppColors.appBar));
    });

    test('shows a splash until Flutter draws its first frame, then removes it', () {
      expect(html, contains('id="splash"'));
      final splash = RegExp(r'<div id="splash".*?</div>\s*<script>', dotAll: true).firstMatch(html)?.group(0) ?? '';
      expect(splash, contains(AppStrings.homeTitle));
      expect(splash, contains(AppStrings.appTagline));
      expect(html, contains("'flutter-first-frame'"));
      expect(RegExp(r'getElementById\(.splash.\)').hasMatch(html), isTrue);
      expect(html, contains('.remove()'));
    });

    test('the splash respects reduced motion and loads nothing external', () {
      expect(html, contains('prefers-reduced-motion: reduce'));
      // No web fonts, images or stylesheets from anywhere: the splash must paint before any request completes.
      final markup = html.replaceAll(RegExp(r'<!--.*?-->', dotAll: true), '');
      expect(markup, isNot(contains('http://')));
      expect(markup, isNot(contains('https://')));
      expect(markup, isNot(contains('<img')));
      expect(markup, isNot(contains('rel="stylesheet"')));
      expect(markup, isNot(contains('@font-face')));
      expect(markup, isNot(contains('url(')));
    });

    test('still starts the app through flutter_bootstrap.js', () {
      expect(html, contains('<script src="flutter_bootstrap.js" async></script>'));
      expect(html, contains(r'<base href="$FLUTTER_BASE_HREF">'));
    });
  });

  group('manifest.json', () {
    late Map<String, dynamic> manifest;
    setUpAll(() => manifest = jsonDecode(read('web/manifest.json')) as Map<String, dynamic>);

    test('carries the app Persian name and description', () {
      expect(manifest['name'], AppStrings.appDocumentTitle);
      expect(manifest['short_name'], AppStrings.homeTitle);
      expect(manifest['description'], contains(AppStrings.appTagline));
      expect(manifest['lang'], 'fa');
      expect(manifest['dir'], 'rtl');
    });

    test('uses the app colours, not Flutter blue', () {
      expect((manifest['theme_color'] as String).toUpperCase(), hex(AppColors.appBar));
      expect((manifest['background_color'] as String).toUpperCase(), hex(AppColors.background));
    });

    test('keeps the existing icon files (brand assets are not changed here)', () {
      final icons = (manifest['icons'] as List).cast<Map<String, dynamic>>();
      expect(icons.map((i) => i['src']), [
        'icons/Icon-192.png',
        'icons/Icon-512.png',
        'icons/Icon-maskable-192.png',
        'icons/Icon-maskable-512.png',
      ]);
    });
  });

  group('flutter_bootstrap.js template', () {
    late String js;
    setUpAll(() => js = read('web/flutter_bootstrap.js'));

    test('loads CanvasKit from our own origin, not www.gstatic.com', () {
      expect(js, contains('{{flutter_js}}'));
      expect(js, contains('{{flutter_build_config}}'));
      expect(js, contains('_flutter.loader.load('));
      expect(RegExp(r'''canvasKitBaseUrl:\s*["']canvaskit/["']''').hasMatch(js), isTrue);
      expect(js.replaceAll(RegExp(r'^\s*//.*$', multiLine: true), ''), isNot(contains('gstatic')));
    });

    test('keeps the service worker settings the default bootstrap had', () {
      expect(js, contains('serviceWorkerSettings'));
      expect(js, contains('{{flutter_service_worker_version}}'));
    });
  });

  group('tool/precompress_web.sh', () {
    test('gzips text and wasm assets next to the originals, skips source maps and images, and is idempotent',
        () async {
      final bash = _bash();
      if (bash == null) {
        markTestSkipped('no POSIX bash on this machine');
        return;
      }
      final script = File('tool/precompress_web.sh').absolute.path.replaceAll(r'\', '/');
      expect(File(script).existsSync(), isTrue);

      final dir = Directory.systemTemp.createTempSync('precompress_web_test');
      addTearDown(() => dir.deleteSync(recursive: true));
      const compressed = [
        'main.dart.abc.js', 'canvaskit/canvaskit.wasm', 'manifest.json', 'x.css', 'index.html',
        'assets/fonts/Vazir.ttf', 'assets/a.otf', 'assets/i.svg',
      ];
      const untouched = ['main.dart.abc.js.map', 'icons/Icon-192.png'];
      for (final name in [...compressed, ...untouched]) {
        File('${dir.path}/$name')
          ..createSync(recursive: true)
          ..writeAsStringSync('content of $name ' * 200);
      }

      Future<ProcessResult> run() =>
          // Git for Windows checks scripts out with CRLF; `-o igncr` is its bash's switch for reading them.
          Process.run(bash, [if (Platform.isWindows) ...['-o', 'igncr'], script, '.'], workingDirectory: dir.path);

      final first = await run();
      expect(first.exitCode, 0, reason: '${first.stdout}\n${first.stderr}');
      for (final name in compressed) {
        expect(File('${dir.path}/$name').existsSync(), isTrue, reason: '$name must stay (-k)');
        final gz = File('${dir.path}/$name.gz');
        expect(gz.existsSync(), isTrue, reason: '$name.gz missing');
        expect(utf8.decode(gzip.decode(gz.readAsBytesSync())), File('${dir.path}/$name').readAsStringSync());
      }
      for (final name in untouched) {
        expect(File('${dir.path}/$name.gz').existsSync(), isFalse, reason: '$name must not be compressed');
      }

      final second = await run();
      expect(second.exitCode, 0, reason: '${second.stdout}\n${second.stderr}');
      final nested = dir.listSync(recursive: true).where((e) => e.path.endsWith('.gz.gz'));
      expect(nested, isEmpty, reason: 'a second run must not compress the .gz files');
      expect('${second.stdout}', contains('gz'), reason: 'prints a size summary');
    });
  });

  group('CI build-customer-web job', () {
    test('precompresses the bundle right after the entry file is renamed, before upload', () {
      final yml = read('../.github/workflows/deploy.yml');
      final start = yml.indexOf('  build-customer-web:');
      expect(start, greaterThanOrEqualTo(0));
      final end = yml.indexOf(RegExp(r'^  [a-z][a-z0-9-]*:\s*$', multiLine: true), start + 10);
      final job = yml.substring(start, end < 0 ? yml.length : end);

      final bust = job.indexOf('bash tool/cache_bust_web.sh build/web');
      final gz = job.indexOf('bash tool/precompress_web.sh build/web');
      final upload = job.indexOf('Upload web bundle');
      expect(bust, greaterThanOrEqualTo(0));
      expect(gz, greaterThan(bust));
      expect(upload, greaterThan(gz));
    });
  });

  group('nginx vhost for customer.nahalkmi.ir', () {
    late String https;
    setUpAll(() {
      final conf = read('../deployment/nginx/asan-rezerve-customer.conf')
          .split('\n')
          .map((l) => l.replaceFirst(RegExp(r'#.*$'), ''))
          .join('\n');
      final i = conf.indexOf('listen 443');
      expect(i, greaterThanOrEqualTo(0));
      https = conf.substring(i);
    });

    test('serves the precompressed files and compresses the rest', () {
      expect(https, contains('gzip_static on;'));
      expect(https, contains('gzip on;'));
      expect(https, contains('gzip_vary on;'));
      final types = RegExp(r'gzip_types([^;]*);').firstMatch(https)?.group(1) ?? '';
      for (final t in [
        'application/javascript', 'text/javascript', 'text/css', 'application/json', 'application/manifest+json',
        'application/wasm', 'image/svg+xml', 'font/ttf', 'font/otf',
      ]) {
        expect(types.split(RegExp(r'\s+')), contains(t), reason: 'gzip_types lacks $t');
      }
    });

    test('keeps the cache headers and the SPA fallback', () {
      expect(https, contains('add_header Cache-Control "public, max-age=31536000, immutable";'));
      expect(https, contains('location = /flutter_bootstrap.js  { add_header Cache-Control "no-store, must-revalidate"; }'));
      expect(https, contains(r'try_files $uri $uri/ /index.html;'));
    });
  });
}

/// A POSIX bash, not the WSL launcher that `bash` resolves to on some Windows machines.
String? _bash() {
  if (!Platform.isWindows) return 'bash';
  for (final p in [r'C:\Program Files\Git\bin\bash.exe', r'C:\Program Files\Git\usr\bin\bash.exe']) {
    if (File(p).existsSync()) return p;
  }
  return null;
}
