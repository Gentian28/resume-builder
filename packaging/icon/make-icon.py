"""Generate the app icon in every size Windows, macOS and Linux ask for.

Committed as a script rather than only as binaries so the icon can be changed by editing values
here instead of round-tripping through an image editor, and so the .ico/.png outputs are
reproducible from source.

    python packaging/icon/make-icon.py

Design notes: the mark has to survive 16x16 in the Start menu and the taskbar, which is where most
people actually see it. That rules out fine detail — hence a solid indigo tile with a high-contrast
white page and only three rules on it. Anything more turns to mush at small sizes.
"""

from __future__ import annotations

import pathlib

from PIL import Image, ImageDraw

OUT = pathlib.Path(__file__).parent

# The app's accent, so the icon matches the UI rather than sitting beside it.
INDIGO = (79, 70, 229, 255)
INDIGO_DARK = (67, 56, 202, 255)
PAPER = (255, 255, 255, 255)
RULE = (150, 150, 170, 255)

# Windows .ico wants these; 256 is what modern Explorer views use.
ICO_SIZES = [256, 128, 64, 48, 32, 16]


def draw(size: int) -> Image.Image:
    """Render at 8x then downsample — cheap supersampling, keeps the corners smooth."""
    s = size * 8
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # Rounded tile. Slight inset so the shape isn't clipped by the icon bounding box.
    pad = s * 0.04
    d.rounded_rectangle([pad, pad, s - pad, s - pad], radius=s * 0.22, fill=INDIGO)

    # A thin darker band along the bottom gives the tile some depth without a gradient,
    # which would band badly once downsampled to 16px.
    d.rounded_rectangle(
        [pad, s * 0.72, s - pad, s - pad], radius=s * 0.22, fill=INDIGO_DARK
    )
    d.rectangle([pad, s * 0.72, s - pad, s * 0.86], fill=INDIGO)

    # The page.
    px0, py0, px1, py1 = s * 0.28, s * 0.20, s * 0.72, s * 0.80
    d.rounded_rectangle([px0, py0, px1, py1], radius=s * 0.03, fill=PAPER)

    # Three rules: a short bold one for the name, two lighter ones for body. At 16px these
    # collapse into a suggestion of text, which is exactly what is wanted.
    line_h = s * 0.035
    left = px0 + s * 0.07
    d.rounded_rectangle(
        [left, py0 + s * 0.10, left + (px1 - px0) * 0.45, py0 + s * 0.10 + line_h * 1.4],
        radius=line_h, fill=INDIGO,
    )
    for i, y in enumerate((0.26, 0.38)):
        width = 0.72 if i == 0 else 0.55
        d.rounded_rectangle(
            [left, py0 + s * y, left + (px1 - px0) * width, py0 + s * y + line_h],
            radius=line_h, fill=RULE,
        )

    return img.resize((size, size), Image.LANCZOS)


def splash() -> Image.Image:
    """The image Velopack shows while installing.

    Without one the installer is a bare progress bar, which for an unsigned download - right after
    the user has clicked through a SmartScreen warning - looks indistinguishable from something
    they should not have run. Showing the name and mark is reassurance at the exact moment it is
    needed.
    """
    w, h = 500, 300
    img = Image.new("RGBA", (w, h), (24, 24, 29, 255))
    d = ImageDraw.Draw(img)

    mark = draw(96)
    img.paste(mark, ((w - 96) // 2, 74), mark)

    # Real text or nothing. An earlier version drew the wordmark as two rounded bars, which read
    # as a loading skeleton rather than a logo - worse than leaving it off.
    font = _load_font(30)
    if font is not None:
        d.text((w // 2, 208), "Resume Builder", font=font, fill=PAPER, anchor="mm")
    return img


def _load_font(size: int):
    """First installed sans-serif that exists, or None. Kept to system fonts so the repo does
    not carry a TTF just to render two words."""
    from PIL import ImageFont

    candidates = [
        r"C:\Windows\Fonts\segoeui.ttf",
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
    ]
    for path in candidates:
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            continue
    return None


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    frames = [draw(n) for n in ICO_SIZES]
    frames[0].save(OUT / "icon.ico", format="ICO", sizes=[(n, n) for n in ICO_SIZES])

    # Standalone PNGs: Avalonia's window icon, the Linux .desktop entry, and anywhere a
    # .ico is not accepted.
    for n in (512, 256, 128, 64, 32):
        draw(n).save(OUT / f"icon-{n}.png")

    splash().save(OUT / "splash.png")

    print(f"wrote {OUT / 'icon.ico'} ({', '.join(str(n) for n in ICO_SIZES)})")
    print("wrote icon-512/256/128/64/32.png")
    print("wrote splash.png")


if __name__ == "__main__":
    main()
