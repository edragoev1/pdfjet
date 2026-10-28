#!/usr/bin/env python3
#
# make-circles-png.py
#
# Copyright (c) 2026 PDFjet Software
# Licensed under the MIT License. See LICENSE file in the project root.
#
# Writes images/circles.png, an 8-bit RGBA PNG of three half transparent
# circles, red, green and blue, that overlap on a transparent background. Its
# edges are anti-aliased, so the alpha channel holds every value from 0 to 255.
# It was made for the tests and examples of PDFjet, which draw it, attach it
# to a PDF and copy it with its soft mask. Run it from the project root; it
# needs Pillow.
#
#     python3 util/make-circles-png.py

from PIL import Image, ImageDraw

OUTPUT = "images/circles.png"

SIZE = 400
SCALE = 4                       # Drawn 4 times larger, then reduced
RADIUS = 110
CENTERS = [(200, 140), (140, 245), (260, 245)]
COLORS = [(220, 40, 40, 160), (40, 170, 70, 160), (40, 90, 220, 160)]

image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
for (x, y), color in zip(CENTERS, COLORS):
    layer = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.ellipse(((x - RADIUS) * SCALE, (y - RADIUS) * SCALE,
                  (x + RADIUS) * SCALE, (y + RADIUS) * SCALE), fill=color)
    layer = layer.resize((SIZE, SIZE), Image.LANCZOS)
    image = Image.alpha_composite(image, layer)

image.save(OUTPUT, optimize=True)
