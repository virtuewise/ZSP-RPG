"""Generate original ZIMBABWE: THE SHADOW PROTOCOL brand assets.

Everything here is drawn from scratch. Nothing from the Commando APK is used.
Run from the workspace root:
    python tools/make_brand_assets.py
"""

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent / "ZSP_CLEAN" / "Assets" / "Art"
UI = ROOT / "UI"
ICONS = ROOT / "Icons"
TEXTURES = ROOT / "Textures"
for directory in (UI, ICONS, TEXTURES):
    directory.mkdir(parents=True, exist_ok=True)

# Palette
INK = (11, 17, 22)
SLATE = (22, 33, 40)
MOSS = (30, 47, 38)
DEEP = (14, 21, 18)
AMBER = (224, 168, 63)
AMBER_DARK = (176, 116, 34)
AMBER_LIGHT = (246, 205, 120)
CREAM = (232, 224, 208)
RED = (166, 58, 46)

FONT_BLACK = r"C:\Windows\Fonts\ariblk.ttf"
FONT_BOLD = r"C:\Windows\Fonts\bahnschrift.ttf"
FONT_REG = r"C:\Windows\Fonts\bahnschrift.ttf"


def font(path, size):
    return ImageFont.truetype(path, size)


def vertical_gradient(size, top, bottom):
    width, height = size
    image = Image.new("RGB", size)
    draw = ImageDraw.Draw(image)
    for y in range(height):
        t = y / max(1, height - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (width, y)], fill=color)
    return image


def add_grain(image, amount=18):
    noise = Image.effect_noise(image.size, 20).convert("L")
    alpha = noise.point(lambda v: int(amount * (v / 255)))
    grain = Image.new("RGB", image.size, (255, 255, 255))
    image = Image.composite(Image.blend(image, grain, 0.25), image, alpha)
    return image


def draw_tower(draw, cx, cy, radius, fill=DEEP, chevron=(28, 40, 34)):
    """Great Zimbabwe style conical tower silhouette with stepped chevrons."""
    apex = (cx, cy - radius * 0.78)
    left = (cx - radius * 0.34, cy + radius * 0.42)
    right = (cx + radius * 0.34, cy + radius * 0.42)
    # Slightly curved sides using segmented polygon
    points = [apex]
    steps = 16
    for i in range(1, steps + 1):
        t = i / steps
        x = cx - radius * (0.34 * t ** 0.82)
        y = cy - radius * 0.78 + (radius * 1.2) * t
        points.append((x, y))
    for i in range(steps, -1, -1):
        t = i / steps
        x = cx + radius * (0.34 * t ** 0.82)
        y = cy - radius * 0.78 + (radius * 1.2) * t
        points.append((x, y))
    draw.polygon(points, fill=fill)
    # Chevron rows
    rows = 7
    for row in range(rows):
        t = (row + 1) / (rows + 1)
        half_width = radius * (0.34 * t ** 0.82) - radius * 0.035
        if half_width <= 0:
            continue
        y = cy - radius * 0.78 + radius * 1.2 * t
        inset = radius * 0.055
        draw.line(
            [
                (cx - half_width + inset, y),
                (cx, y + radius * 0.055),
                (cx + half_width - inset, y),
            ],
            fill=chevron,
            width=max(2, int(radius * 0.022)),
        )


def draw_land(draw, width, height, horizon, color=DEEP):
    points = [(0, horizon)]
    segments = 120
    random.seed(2026)
    for i in range(segments + 1):
        x = width * i / segments
        base = horizon + math.sin(i * 0.045) * height * 0.016 + math.sin(i * 0.011 + 1.4) * height * 0.034
        if random.random() < 0.06:
            base -= height * random.uniform(0.006, 0.018)
        points.append((x, base))
    points.append((width, height))
    points.append((0, height))
    draw.polygon(points, fill=color)


def draw_sun(draw, cx, cy, radius):
    steps = 90
    for index in range(steps):
        t = index / (steps - 1)  # 0 = outer edge, 1 = sun surface
        r = radius * (1.0 + (1.0 - t) * 1.05)
        alpha = int(150 * t ** 2)
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(AMBER[0], AMBER[1], AMBER[2], alpha))
    draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=AMBER)
    draw.ellipse(
        [cx - radius * 0.58, cy - radius * 0.58, cx + radius * 0.58, cy + radius * 0.58],
        fill=AMBER_LIGHT,
    )


def draw_emblem(size, with_ring=True):
    width = height = size
    image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    cx, cy = width / 2, width / 2
    radius = width * 0.40

    if with_ring:
        ring_w = int(width * 0.018)
        draw.ellipse(
            [cx - radius, cy - radius, cx + radius, cy + radius],
            fill=INK,
            outline=AMBER,
            width=ring_w,
        )
    else:
        draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=INK)

    # Clip contents to the circle by drawing on a masked layer
    contents = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    cdraw = ImageDraw.Draw(contents)
    cdraw.rectangle([0, 0, width, height], fill=(22, 38, 34, 255))
    draw_sun(cdraw, cx, cy - radius * 0.18, radius * 0.52)
    draw_land(cdraw, width, height, cy + radius * 0.20, DEEP)
    draw_tower(cdraw, cx, cy + radius * 0.34, radius * 0.62, fill=DEEP, chevron=(34, 48, 41))
    mask = Image.new("L", (width, height), 0)
    mdraw = ImageDraw.Draw(mask)
    mdraw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=255)
    image.paste(contents, (0, 0), mask)
    return image


def make_icon():
    base = vertical_gradient((1024, 1024), INK, MOSS)
    base = add_grain(base, 16).convert("RGBA")
    emblem = draw_emblem(760, with_ring=False)
    base.paste(emblem, (132, 96), emblem)
    base = base.resize((512, 512), Image.LANCZOS)
    base.save(ICONS / "zsp_app_icon_512.png")


def make_emblem():
    emblem = draw_emblem(1024, with_ring=True)
    emblem.save(UI / "zsp_emblem.png")


def make_logo():
    image = Image.new("RGBA", (2048, 1024), (0, 0, 0, 0))
    emblem = draw_emblem(760, with_ring=True)
    image.paste(emblem, (40, 130), emblem)
    draw = ImageDraw.Draw(image)
    x = 900
    title = font(FONT_BLACK, 168)
    sub = font(FONT_BLACK, 92)
    small = font(FONT_REG, 34)
    draw.text((x, 210), "ZIMBABWE", font=title, fill=CREAM)
    draw.text((x, 390), "THE SHADOW", font=sub, fill=AMBER)
    draw.text((x, 492), "PROTOCOL", font=sub, fill=AMBER)
    draw.line([(x, 640), (x + 1024, 640)], fill=(232, 224, 208, 90), width=3)
    draw.text((x, 676), "A FICTIONAL INTELLIGENCE THRILLER", font=small, fill=(232, 224, 208, 210))
    image.save(UI / "zsp_logo.png")


def make_splash():
    width, height = 2560, 1440
    base = vertical_gradient((width, height), INK, MOSS)
    base = add_grain(base, 18).convert("RGBA")
    draw = ImageDraw.Draw(base)
    # Distant hills and sun glow
    draw_land(draw, width, height, height * 0.66, (18, 27, 24))
    draw_land(draw, width, height, height * 0.78, DEEP)
    # Central emblem
    emblem = draw_emblem(620, with_ring=True)
    base.paste(emblem, (width // 2 - 310, 150), emblem)
    # Wordmark
    title = font(FONT_BLACK, 128)
    sub = font(FONT_BLACK, 66)
    tag = font(FONT_REG, 36)
    disc = font(FONT_REG, 26)
    draw.text((width // 2, 830), "ZIMBABWE", font=title, fill=CREAM, anchor="mm")
    draw.text((width // 2, 950), "THE SHADOW PROTOCOL", font=sub, fill=AMBER, anchor="mm")
    draw.line([(width // 2 - 520, 1032), (width // 2 + 520, 1032)], fill=(232, 224, 208, 80), width=2)
    draw.text((width // 2, 1088), "EVERY CONTACT LEAVES A SHADOW", font=tag, fill=(232, 224, 208, 225), anchor="mm")
    draw.text(
        (width // 2, height - 64),
        "A WORK OF FICTION. ALL ORGANISATIONS AND NAMED INDIVIDUALS ARE INVENTED.",
        font=disc,
        fill=(232, 224, 208, 150),
        anchor="mm",
    )
    base.resize((2048, 1152), Image.LANCZOS).convert("RGB").save(UI / "zsp_splash.png", quality=95)


def make_crosshair():
    image = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    cx = cy = 256
    amber = (224, 168, 63, 255)
    shadow = (0, 0, 0, 190)
    for radius, width in [(112, 10)]:
        draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], outline=shadow, width=width + 6)
        draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], outline=amber, width=width)
    for angle in (0, 90, 180, 270):
        rad = math.radians(angle)
        x1, y1 = cx + math.cos(rad) * 46, cy + math.sin(rad) * 46
        x2, y2 = cx + math.cos(rad) * 132, cy + math.sin(rad) * 132
        draw.line([(x1, y1), (x2, y2)], fill=shadow, width=14)
        draw.line([(x1, y1), (x2, y2)], fill=amber, width=7)
    draw.ellipse([cx - 9, cy - 9, cx + 9, cy + 9], fill=shadow)
    draw.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=amber)
    image.save(UI / "zsp_crosshair.png")


def make_bars():
    def segmented_bar(name, fill):
        width, height = 512, 96
        image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        draw.rounded_rectangle([4, 8, width - 4, height - 8], radius=18, fill=(9, 12, 14, 220), outline=(232, 224, 208, 70), width=2)
        segments = 5
        margin, gap = 18, 10
        usable = width - margin * 2 - gap * (segments - 1)
        seg_w = usable // segments
        for index in range(segments):
            x0 = margin + index * (seg_w + gap)
            draw.rounded_rectangle([x0, 24, x0 + seg_w, height - 24], radius=8, fill=(35, 45, 40, 255))
            draw.rounded_rectangle([x0, 24, x0 + seg_w, height - 24], radius=8, outline=(232, 224, 208, 40), width=1)
        # filled portion markers (static full bar for the art set)
        image.save(UI / name)

    segmented_bar("zsp_health_bar.png", 92)
    segmented_bar("zsp_detection_bar.png", 42)
    # Simpler filled bars
    for name, color in [("zsp_health_bar.png", (86, 168, 110)), ("zsp_detection_bar.png", AMBER)]:
        width, height = 512, 96
        image = Image.open(UI / name).convert("RGBA")
        draw = ImageDraw.Draw(image)
        width, height = image.size
        margin, gap = 18, 10
        segments = 5 if name.startswith("zsp_health") else 6
        usable = width - margin * 2 - gap * (segments - 1)
        seg_w = usable // segments
        for index in range(segments):
            x0 = margin + index * (seg_w + gap)
            draw.rounded_rectangle([x0, 24, x0 + seg_w, height - 24], radius=8, fill=color)
        image.save(UI / name)


def make_texture():
    # Subtle diagonal weave used for Nightjar uniforms (placeholder, original)
    size = 512
    image = Image.new("RGB", (size, size), (26, 34, 30))
    draw = ImageDraw.Draw(image)
    random.seed(41)
    for _ in range(900):
        x = random.randint(0, size - 1)
        y = random.randint(0, size - 1)
        length = random.randint(18, 60)
        tone = random.choice([(32, 42, 37), (22, 29, 26), (39, 51, 45)])
        draw.line([(x, y), (x + length, y + length // 3)], fill=tone, width=2)
    for _ in range(300):
        x = random.randint(0, size - 1)
        y = random.randint(0, size - 1)
        draw.point((x, y), fill=(52, 66, 58))
    image.save(TEXTURES / "zsp_nightjar_weave.png")


if __name__ == "__main__":
    make_icon()
    make_emblem()
    make_logo()
    make_splash()
    make_crosshair()
    make_bars()
    make_texture()
    print("brand assets written to", ROOT)
