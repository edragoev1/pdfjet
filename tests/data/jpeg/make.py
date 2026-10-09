# make.py
#
# Copyright (c) 2026 PDFjet Software
# Licensed under the MIT License. See LICENSE file in the project root.
#
# Makes the JPEGs of the tests of the four ports, once, with Pillow:
#
#   python3 tests/data/jpeg/make.py
#
# restart.jpg: 37 by 23 pixels, 4:2:0, a restart marker after every 4 MCUs,
#   for the walk of the scans of a JPEG without its end-of-image marker.
# orientation-N.jpg: with the EXIF orientation N (1, 3, 6, 8), stored turned
#   so that it is seen, upright, as 32 by 16 pixels: red on the left, blue on
#   the right, a white square at the top left. 6 and 8 are 16 by 32 as stored.

import io
import os

from PIL import Image

here = os.path.dirname(os.path.abspath(__file__))


def picture(width, height):
    image = Image.new("RGB", (width, height), (220, 30, 30))
    for y in range(height):
        for x in range(width // 2, width):
            image.putpixel((x, y), (30, 60, 220))
    for y in range(6):
        for x in range(6):
            image.putpixel((x, y), (255, 255, 255))
    return image


picture(37, 23).save(os.path.join(here, "restart.jpg"), quality=90,
                     subsampling=2, restart_marker_blocks=4)

# Stored turned the other way, so that each is seen as orientation-1.jpg is
stored = {1: None, 3: Image.Transpose.ROTATE_180,
          6: Image.Transpose.ROTATE_90, 8: Image.Transpose.ROTATE_270}
for orientation, turn in stored.items():
    exif = Image.Exif()
    exif[0x0112] = orientation
    image = picture(32, 16)
    if turn is not None:
        image = image.transpose(turn)
    image.save(os.path.join(here, "orientation-%d.jpg" % orientation),
               quality=90, exif=exif.tobytes())
