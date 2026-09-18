#!/usr/bin/env python3
#
# make-cmyk-jpeg.py
#
# Copyright (c) 2026 PDFjet Software
# Licensed under the MIT License. See LICENSE file in the project root.
#
# Writes images/cmyk.jpg, a test file with the structure of a CMYK JPEG that
# Photoshop saves: four components, an Adobe APP14 marker, and the inks stored
# inverted, 255 for no ink, as Adobe applications store them. The picture is a
# print test chart drawn in CMYK: a ramp of each ink from 0 to 100 percent, and
# the colors two inks make, red, green and blue, and rich black. It was made
# for the tests and examples of PDFjet. Run it from the project root; it needs
# Pillow.
#
#     python3 util/make-cmyk-jpeg.py

from PIL import Image, ImageDraw, ImageFont

OUTPUT = "images/cmyk.jpg"
FONT = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.otf"

WIDTH, HEIGHT = 1200, 800       # 4 by 2.67 inches at 300 pixels per inch
MARGIN = 60
BLACK = (0, 0, 0, 255)

image = Image.new("CMYK", (WIDTH, HEIGHT), (0, 0, 0, 0))    # Paper white
draw = ImageDraw.Draw(image)
title = ImageFont.truetype(FONT, 64)
label = ImageFont.truetype(FONT, 28)

draw.text((MARGIN, 40), "CMYK test chart", font=title, fill=BLACK)

# A ramp of each ink, 0 to 100 percent in steps of 10.
inks = [("Cyan", 0), ("Magenta", 1), ("Yellow", 2), ("Black", 3)]
x0 = MARGIN + 170
step = (WIDTH - x0 - MARGIN) // 11
y = 150
for name, ink in inks:
    draw.text((MARGIN, y + 22), name, font=label, fill=BLACK)
    for i in range(11):
        color = [0, 0, 0, 0]
        color[ink] = round(255 * i / 10)
        # A light gray outline shows the edge of the 0 percent swatch too.
        draw.rectangle([x0 + i * step, y, x0 + (i + 1) * step - 8, y + 80],
                fill=tuple(color), outline=(0, 0, 0, 64), width=2)
    y += 100
for i in range(11):
    text = str(i * 10) + "%"
    width = draw.textlength(text, font=label)
    draw.text((x0 + i * step + (step - 8 - width) / 2, y), text, font=label, fill=BLACK)

# The colors of two inks, and rich black: all four.
y += 60
mixes = [
    ("Red", (0, 255, 255, 0)),
    ("Green", (255, 0, 255, 0)),
    ("Blue", (255, 255, 0, 0)),
    ("Rich black", (153, 102, 102, 255)),
]
width = (WIDTH - 2 * MARGIN - 3 * 20) // 4
for i, (name, color) in enumerate(mixes):
    x = MARGIN + i * (width + 20)
    draw.rectangle([x, y, x + width, y + 90], fill=color)
    draw.text((x, y + 100), name, font=label, fill=BLACK)

# Pillow writes a CMYK JPEG with an Adobe APP14 marker and the inks inverted,
# as Photoshop does.
image.save(OUTPUT, "JPEG", quality=90, dpi=(300, 300))
