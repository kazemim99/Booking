#!/usr/bin/env python3
"""Rasterise the brand SVGs in this folder into every app's icon and splash files.

Re-run after the designer changes an SVG:

    pip install pillow
    python branding/render_icons.py [--chrome PATH]

Rendering is done by headless Chromium, so the output follows the SVG files exactly; Pillow
only resamples and writes PNG/ICO. Chromium is found via --chrome, $CHROME, or the Playwright
browser cache. What goes where, and why, is in branding/README.md.

The Android adaptive-icon vectors (mipmap-anydpi-v26, drawable/ic_launcher_*.xml) are
hand-written from the same 100x100 geometry and are not produced here.
"""
from __future__ import annotations

import argparse
import glob
import io
import json
import os
import shutil
import subprocess
import tempfile
from pathlib import Path

from PIL import Image

BRANDING = Path(__file__).resolve().parent
ROOT = BRANDING.parent
CUSTOMER = ROOT / "asan-rezerve-customer-app"
PROVIDER = ROOT / "asan-rezerve-provider-app"
FRONTEND = ROOT / "asan-rezerve-frontend"
ADMIN = ROOT / "asan-rezerve-admin"

BLUE = "#3777C0"
CANVAS = 1024  # every composition is rendered once at this size, then resampled

# name -> (background CSS, SVG file, scale of the SVG within the canvas)
COMPOSITIONS = {
    # The designer's app icons, rounded corners and all.
    "appicon-blue": ("transparent", "appicon-blue.svg", 1.0),
    "appicon-white": ("transparent", "appicon-white.svg", 1.0),
    # Full-bleed squares for platforms that apply their own mask (iOS, apple-touch-icon).
    "fullbleed-blue": (BLUE, "symbol-reversed-blue.svg", 1.0),
    "fullbleed-white": ("#FFFFFF", "symbol-color.svg", 1.0),
    # PWA maskable icons: the mark must sit inside the central 80% circle, hence 0.7.
    "maskable-blue": (BLUE, "symbol-reversed-blue.svg", 0.7),
    "maskable-white": ("#FFFFFF", "symbol-color.svg", 0.7),
    # The bare mark for launch screens, drawn on the window's own white background.
    "mark": ("transparent", "symbol-color.svg", 1.0),
}

ANDROID_DENSITIES = {"mdpi": 1, "hdpi": 1.5, "xhdpi": 2, "xxhdpi": 3, "xxxhdpi": 4}
LAUNCHER_DP = 48
SPLASH_MARK_DP = 96  # also the iOS LaunchImage size in points


def find_chrome(explicit: str | None) -> str:
    candidates = [explicit, os.environ.get("CHROME")]
    cache = os.environ.get("PLAYWRIGHT_BROWSERS_PATH") or str(Path.home() / ".cache/ms-playwright")
    candidates += sorted(glob.glob(f"{cache}/chromium_headless_shell-*/chrome-linux/headless_shell"), reverse=True)
    candidates += sorted(glob.glob(f"{cache}/chromium-*/chrome-linux/chrome"), reverse=True)
    candidates += [shutil.which(n) for n in ("chromium", "chromium-browser", "google-chrome", "chrome")]
    for c in candidates:
        if c and Path(c).exists():
            return c
    raise SystemExit("No Chromium found: pass --chrome or set $CHROME")


def render(chrome: str, background: str, svg: str, scale: float, workdir: Path) -> Image.Image:
    size = round(CANVAS * scale)
    offset = (CANVAS - size) // 2
    page = workdir / "page.html"
    shot = workdir / "shot.png"
    page.write_text(
        f"<html><body style='margin:0;background:{background}'>"
        f"<img src='{(BRANDING / svg).as_uri()}' style='position:absolute;left:{offset}px;top:{offset}px;"
        f"width:{size}px;height:{size}px'></body></html>",
        encoding="utf-8",
    )
    subprocess.run(
        [chrome, "--headless", "--no-sandbox", "--hide-scrollbars", "--force-device-scale-factor=1",
         "--default-background-color=00000000", f"--window-size={CANVAS},{CANVAS}",
         f"--screenshot={shot}", page.as_uri()],
        check=True, capture_output=True,
    )
    return Image.open(shot).convert("RGBA").copy()


def save_png(image: Image.Image, px: int, dest: Path, opaque: bool = False) -> None:
    out = image.resize((px, px), Image.LANCZOS)
    if opaque:  # App Store icons must not carry an alpha channel.
        out = out.convert("RGB")
    dest.parent.mkdir(parents=True, exist_ok=True)
    out.save(dest, optimize=True)
    print(f"  {dest.relative_to(ROOT)} ({px}px)")


def save_ico(image: Image.Image, sizes: list[int], dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    image.save(dest, format="ICO", sizes=[(s, s) for s in sizes])
    print(f"  {dest.relative_to(ROOT)} ({', '.join(map(str, sizes))})")


def flutter_app(app: Path, img: dict[str, Image.Image], launcher: str, maskable: str, fullbleed: str) -> None:
    res = app / "android/app/src/main/res"
    for density, factor in ANDROID_DENSITIES.items():
        save_png(img[launcher], round(LAUNCHER_DP * factor), res / f"mipmap-{density}/ic_launcher.png")
        save_png(img["mark"], round(SPLASH_MARK_DP * factor), res / f"drawable-{density}/launch_mark.png")

    web = app / "web"
    save_png(img[launcher], 32, web / "favicon.png")
    for px in (192, 512):
        save_png(img[launcher], px, web / f"icons/Icon-{px}.png")
        save_png(img[maskable], px, web / f"icons/Icon-maskable-{px}.png")
    save_png(img[fullbleed], 180, web / "icons/apple-touch-icon.png", opaque=True)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--chrome", help="path to a Chromium/Chrome binary")
    chrome = find_chrome(parser.parse_args().chrome)

    with tempfile.TemporaryDirectory() as tmp:
        img = {name: render(chrome, *spec, Path(tmp)) for name, spec in COMPOSITIONS.items()}

    print("customer app (blue icon)")
    flutter_app(CUSTOMER, img, "appicon-blue", "maskable-blue", "fullbleed-blue")
    save_ico(img["appicon-blue"], [16, 24, 32, 48, 64, 128, 256], CUSTOMER / "windows/runner/resources/app_icon.ico")

    print("provider app (white icon)")
    flutter_app(PROVIDER, img, "appicon-white", "maskable-white", "fullbleed-white")
    ios = PROVIDER / "ios/Runner/Assets.xcassets"
    for entry in json.loads((ios / "AppIcon.appiconset/Contents.json").read_text())["images"]:
        px = round(float(entry["size"].split("x")[0]) * int(entry["scale"].rstrip("x")))
        save_png(img["fullbleed-white"], px, ios / "AppIcon.appiconset" / entry["filename"], opaque=True)
    for scale, suffix in ((1, ""), (2, "@2x"), (3, "@3x")):
        save_png(img["mark"], SPLASH_MARK_DP * scale, ios / f"LaunchImage.imageset/LaunchImage{suffix}.png")

    print("web app")
    save_ico(img["appicon-blue"], [16, 32, 48], FRONTEND / "public/favicon.ico")
    save_png(img["fullbleed-blue"], 180, FRONTEND / "public/apple-touch-icon.png", opaque=True)

    print("admin app")
    save_ico(img["appicon-blue"], [16, 32, 48], ADMIN / "public/favicon.ico")
    save_png(img["fullbleed-blue"], 180, ADMIN / "public/apple-touch-icon.png", opaque=True)


if __name__ == "__main__":
    main()
