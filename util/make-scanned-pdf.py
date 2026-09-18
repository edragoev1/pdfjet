#!/usr/bin/env python3
#
# make-scanned-pdf.py
#
# Copyright (c) 2026 PDFjet Software
# Licensed under the MIT License. See LICENSE file in the project root.
#
# Writes data/testPDFs/scanned-linearized.pdf, a test file with the structure
# of a scanned article that Acrobat PDFWriter 2.0 wrote in 1998: PDF 1.2 with
# lines that end in a carriage return, linearized, with five pages that are
# each a CCITT Group 4 image drawn by an array of two LZW content streams, and
# with the length of every stream in an indirect object. The pages describe
# these features. Run it from the project root; it needs Pillow with libtiff.
#
#     python3 util/make-scanned-pdf.py

import hashlib
import io
import struct
import zlib

from PIL import Image, ImageDraw, ImageFont

OUTPUT = "data/testPDFs/scanned-linearized.pdf"
SERIF = "fonts/IBMPlexSerif/IBMPlexSerif-"

WIDTH = 2560                                # Pixels across every scan
HEIGHTS = [3282, 3286, 3281, 3285, 3280]    # Scans differ by a few rows
DPI = WIDTH / (547.2 / 72.0)                # About 337 pixels per inch

PAGES = [
    ("A Synthetic Scanned Document", [
        "This document stands in for a scanned magazine article from 1998 "
        "and has the same structure: five pages, each a black and white "
        "image 2,560 pixels across, drawn on a Letter page by two content "
        "streams. It was made for the tests and examples of PDFjet by "
        "util/make-scanned-pdf.py.",
        "The pages that follow describe how the file is built, one feature "
        "on each page, so that a reader of PDF files meets what a file of "
        "this age can hold.",
    ]),
    ("1. Linearization", [
        "The file is linearized, or optimized for fast web view. It starts "
        "with a linearization dictionary that gives the length of the file, "
        "the object number of the first page and the offset where that page "
        "ends, followed by a cross-reference section for the objects of the "
        "first page and a hint stream.",
        "The cross-reference section for the other objects is at the end of "
        "the file, and the trailer at the top points to it with /Prev. The "
        "startxref at the end of the file points back to the section at the "
        "top.",
    ]),
    ("2. CCITT Group 4 Images", [
        "Every page is a scanned image with one bit per pixel, compressed "
        "with the CCITT Group 4 fax encoding, /K -1 in its decode "
        "parameters. A page of text takes about ten kilobytes this way.",
        "The image is drawn 547.2 points wide, which is about 337 pixels "
        "per inch, and the scans are a few rows taller or shorter than one "
        "another, as the pages of a real scan are.",
    ]),
    ("3. LZW Content Streams", [
        "The contents of every page are an array of two streams compressed "
        "with LZW, the filter that PDF used before Flate. The first stream "
        "saves the graphics state, scales the image to the page and draws "
        "it; the second restores the graphics state.",
        "Each page names its image in its own resources and shares one "
        "procedure set array, [/PDF /ImageB], with the other pages.",
    ]),
    ("4. Old Habits", [
        "The lines of the file end in a carriage return alone, but for the "
        "entries of the cross-reference tables, which are 20 bytes long "
        "and end in a carriage return and a line feed.",
        "The length of every stream is an indirect object written after the "
        "stream, the header says PDF 1.2, and the catalog opens the first "
        "page at the width of the window.",
    ]),
]


def px(points):
    return round(points * DPI / 72.0)


def wrap(draw, text, font, width):
    lines, line = [], ""
    for word in text.split():
        candidate = word if line == "" else line + " " + word
        if draw.textlength(candidate, font=font) <= width:
            line = candidate
        else:
            lines.append(line)
            line = word
    lines.append(line)
    return lines


def scan(number, height):
    """Draws a page as a 1-bit image, slightly turned, as a scanner would."""
    title, paragraphs = PAGES[number - 1]
    image = Image.new("L", (WIDTH, height), 255)
    draw = ImageDraw.Draw(image)
    heading = ImageFont.truetype(SERIF + "SemiBold.otf", px(20))
    body = ImageFont.truetype(SERIF + "Regular.otf", px(11))
    small = ImageFont.truetype(SERIF + "Italic.otf", px(9))

    x, y, width = px(54), px(72), WIDTH - 2 * px(54)
    draw.text((x, y), title, font=heading, fill=0)
    y += px(48)
    for paragraph in paragraphs:
        for line in wrap(draw, paragraph, body, width):
            draw.text((x, y), line, font=body, fill=0)
            y += px(17)
        y += px(10)

    footer = "Page %d of %d" % (number, len(PAGES))
    draw.text((WIDTH - x - draw.textlength(footer, font=small),
            height - px(54)), footer, font=small, fill=0)

    image = image.rotate(0.15 * (1 if number % 2 else -1),
            resample=Image.BICUBIC, fillcolor=255)
    # CCITT data without /BlackIs1 has 0 for black, so black is stored as 1
    # in the TIFF strip, whose 1 is black.
    return image.point(lambda v: 0 if v > 150 else 255, mode="1")


def group4(image):
    """Returns the CCITT Group 4 data of a 1-bit image, as one TIFF strip."""
    buffer = io.BytesIO()
    image.save(buffer, format="TIFF", compression="group4",
            tiffinfo={278: image.height})   # RowsPerStrip: one strip
    tiff = Image.open(io.BytesIO(buffer.getvalue()))
    offset = tiff.tag_v2[273][0]            # StripOffsets
    length = tiff.tag_v2[279][0]            # StripByteCounts
    assert len(tiff.tag_v2[273]) == 1
    return buffer.getvalue()[offset:offset + length]


def lzw(data):
    """LZW with the PDF defaults: a clear code first and early change."""
    table = {bytes([i]): i for i in range(256)}
    next_code, width = 258, 9
    codes = [(256, 9)]
    w = b""
    for byte in data:
        wc = w + bytes([byte])
        if wc in table:
            w = wc
            continue
        codes.append((table[w], width))
        table[wc] = next_code
        next_code += 1
        if next_code + 1 > (1 << width) and width < 12:
            width += 1
        w = bytes([byte])
    if w:
        codes.append((table[w], width))
    codes.append((257, width))

    out, bits, count = bytearray(), 0, 0
    for code, size in codes:
        bits = (bits << size) | code
        count += size
        while count >= 8:
            count -= 8
            out.append((bits >> count) & 0xff)
    if count > 0:
        out.append((bits << (8 - count)) & 0xff)
    return bytes(out)


class Bits:
    """Writes numbers of any number of bits, most significant bit first."""
    def __init__(self):
        self.out, self.bits, self.count = bytearray(), 0, 0

    def write(self, value, size):
        for i in range(size - 1, -1, -1):
            self.bits = (self.bits << 1) | ((value >> i) & 1)
            self.count += 1
            if self.count == 8:
                self.out.append(self.bits)
                self.bits, self.count = 0, 0

    def align(self):
        while self.count != 0:
            self.write(0, 1)

    def bytes(self):
        self.align()
        return bytes(self.out)


def hint_tables(pages, first_page_objects):
    """The page offset and the shared object hint tables of Annex F.

    pages is a list of (first offset, end offset, object count, content
    offset, content length) and first_page_objects a list of the (offset,
    length) of the objects of the first page, which are its shared object
    entries; the file has no shared objects section. Every difference is
    written in 32 bits, so that no entry depends on the others.
    """
    b = Bits()
    least_objects = min(p[2] for p in pages)
    least_length = min(p[1] - p[0] for p in pages)
    least_offset = min(p[3] - p[0] for p in pages)
    least_content = min(p[4] for p in pages)
    b.write(least_objects, 32)
    b.write(pages[0][0], 32)        # The location of the first page object
    b.write(32, 16)
    b.write(least_length, 32)
    b.write(32, 16)
    b.write(least_offset, 32)
    b.write(32, 16)
    b.write(least_content, 32)
    b.write(32, 16)
    b.write(0, 16)                  # No shared object references
    b.write(0, 16)
    b.write(0, 16)
    b.write(1, 16)
    for item in (lambda p: p[2] - least_objects,
            lambda p: p[1] - p[0] - least_length):
        for page in pages:
            b.write(item(page), 32)
        b.align()
    for item in (lambda p: p[3] - p[0] - least_offset,
            lambda p: p[4] - least_content):
        for page in pages:
            b.write(item(page), 32)
        b.align()
    page_table = b.bytes()

    b = Bits()
    least_group = min(length for _, length in first_page_objects)
    b.write(0, 32)                  # No shared objects section
    b.write(0, 32)
    b.write(len(first_page_objects), 32)
    b.write(len(first_page_objects), 32)
    b.write(0, 16)                  # One object in every group
    b.write(least_group, 32)
    b.write(32, 16)
    for _, length in first_page_objects:
        b.write(length - least_group, 32)
    b.align()
    for _ in first_page_objects:
        b.write(0, 1)               # No MD5 signatures
    b.align()
    return page_table, b.bytes()


def main():
    streams = []    # (image data, first content, second content)
    for i, height in enumerate(HEIGHTS):
        image = scan(i + 1, height)
        first = ("q \r547.2 0 0 %.2f 32.4 %.2f cm \r/im%d Do \r" % (
                547.2 * height / WIDTH, 792.0 - 18.0 - 547.2 * height / WIDTH,
                70 + i)).encode()
        streams.append((group4(image), lzw(first), lzw(b"Q \r")))

    def dictionary(*lines):
        return b"<< \r" + b"".join(line.encode() + b" \r" for line in lines) + b">> \r"

    def page_objects(n, index, image_length, image_number):
        """The objects of one page, numbered from n as PDFWriter did."""
        image, first, second = streams[index]
        name = 70 + index
        return [
            (n, dictionary("/Type /Page", "/Parent 33 0 R",
                    "/Resources << /XObject << /im%d %d 0 R >> /ProcSet 44 0 R >>" % (name, image_number),
                    "/Contents %d 0 R" % (n + 1), "/MediaBox [ 0 0 612 792 ]",
                    "/CropBox [ 0 0 612 792 ]", "/Rotate 0")),
            (n + 1, b"[ %d 0 R %d 0 R ] \r" % (n + 3, n + 5)),
            (n + 2, b"%d \r" % len(first)),
            (n + 3, dictionary("/Length %d 0 R" % (n + 2), "/Filter /LZWDecode")
                    + b"stream\r\n" + first + b"\rendstream\r"),
            (n + 4, b"%d \r" % len(second)),
            (n + 5, dictionary("/Length %d 0 R" % (n + 4), "/Filter /LZWDecode")
                    + b"stream\r\n" + second + b"\rendstream\r"),
            (image_length, b"%d \r" % len(image)),
            (image_number, dictionary("/Type /XObject", "/Subtype /Image", "/Name /im%d" % name,
                    "/Filter /CCITTFaxDecode", "/Width %d" % WIDTH,
                    "/Height %d" % HEIGHTS[index], "/BitsPerComponent 1",
                    "/ColorSpace /DeviceGray", "/Length %d 0 R" % image_length,
                    "/DecodeParms << /K -1 /Columns %d >>" % WIDTH)
                    + b"stream\r\n" + image + b"\rendstream\r"),
        ]

    # The first page is objects 38 to 46, with the procedure set that all
    # the pages share in 44, and the other pages are objects 1 to 32.
    first_part = [
        (36, dictionary("/Type /Catalog", "/Pages 33 0 R",
                "/OpenAction 37 0 R", "/PageMode /UseNone")),
        (37, dictionary("/S /GoTo", "/D [ 38 0 R /FitH null ]")),
    ]
    first_part += page_objects(38, 0, 45, 46)
    first_part.insert(8, (44, b"[ /PDF /ImageB ] \r"))
    rest = []
    for index, n in enumerate([1, 9, 17, 25]):
        rest += page_objects(n, index + 1, n + 6, n + 7)
    rest += [
        (33, dictionary("/Type /Pages",
                "/Kids [ 38 0 R 1 0 R 9 0 R 17 0 R 25 0 R ]", "/Count 5")),
        (34, dictionary("/CreationDate (D:19980207150114)",
                "/ModDate (D:19980207150115)",
                "/Title (A Synthetic Scanned Document)",
                "/Author (PDFjet Software)",
                "/Creator (PDFjet test data)",
                "/Producer (util/make-scanned-pdf.py)",
                "/Subject ()", "/Keywords ()")),
    ]

    def obj(number, body):
        return b"%d 0 obj\r" % number + body + b"endobj\r"

    def xref(first, offsets):
        text = b"xref\r%d %d \r" % (first, len(offsets))
        for offset in offsets:
            if offset is None:
                text += b"0000000000 65535 f\r\n"
            else:
                text += b"%010d 00000 n\r\n" % offset
        return text

    digest = hashlib.md5(b"A Synthetic Scanned Document").hexdigest().encode()
    file_id = b"<%s><%s>" % (digest, digest)
    header = b"%PDF-1.2\r%\xe2\xe3\xcf\xd3\r\n"
    values = {"L": 0, "E": 0, "T": 0, "prev": 0, "H0": 0, "H1": 0,
            "pages": [(0, 1, 1, 0, 1)] * 5, "first": [(0, 1)]}
    data = None
    for _ in range(10):     # The offsets settle after a few passes.
        previous = data
        linearized = obj(35, dictionary("/Linearized 1", "/O 38",
                "/H [ %d %d ]" % (values["H0"], values["H1"]),
                "/L %d" % values["L"], "/E %d" % values["E"], "/N 5",
                "/T %d" % values["T"]))
        linearized = linearized.ljust(120, b" ")
        top = header + linearized
        offsets = {35: len(header)}
        top_xref_at = len(top)
        # 14 entries for objects 35 to 48; their offsets are filled in below.
        body = b""
        position = top_xref_at + len(xref(35, [0] * 14)) + len(
                b"trailer\r<<\r/Size 49\r/Info 34 0 R \r/Root 36 0 R \r/Prev %010d \r/ID[%s]\r>>\rstartxref\r0\r%%%%EOF\r" % (0, file_id))
        # The hint stream comes right after the trailer at the top.
        page_table, shared_table = hint_tables(values["pages"], values["first"])
        hints = page_table + shared_table
        hint_objects = [
            (47, dictionary("/S %d" % len(page_table), "/Filter /FlateDecode", "/Length 48 0 R")
                    + b"stream\r\n" + zlib.compress(hints) + b"\rendstream\r"),
        ]
        hint_objects.append((48, b"%d \r" % len(zlib.compress(hints))))
        for number, content in hint_objects + first_part:
            offsets[number] = position
            chunk = obj(number, content)
            if number == 47:
                values["H0"], values["H1"] = position, len(chunk)
            body += chunk
            position += len(chunk)
        values["E"] = position
        for number, content in rest:
            offsets[number] = position
            chunk = obj(number, content)
            body += chunk
            position += len(chunk)
        main_xref_at = position
        values["prev"] = main_xref_at
        values["T"] = main_xref_at + len(b"xref\r0 35 \r")
        top_trailer = (b"trailer\r<<\r/Size 49\r/Info 34 0 R \r/Root 36 0 R \r"
                b"/Prev %010d \r/ID[%s]\r>>\rstartxref\r0\r%%%%EOF\r" % (main_xref_at, file_id))
        main = xref(0, [None] + [offsets[n] for n in range(1, 35)])
        main += b"trailer\r<<\r/Size 35\r/ID[%s]\r>>\rstartxref\r%d\r%%%%EOF\r" % (
                file_id, top_xref_at)
        data = top + xref(35, [offsets[n] for n in range(35, 49)]) + top_trailer + body + main
        values["L"] = len(data)
        ends = {}
        for number, content in hint_objects + first_part + rest:
            ends[number] = offsets[number] + len(obj(number, content))
        values["first"] = [(offsets[n], ends[n] - offsets[n])
                for n in (36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46)]
        values["pages"] = [(offsets[38], values["E"], 9, offsets[41], ends[43] - offsets[41])]
        for n in (1, 9, 17, 25):
            values["pages"].append((offsets[n], ends[n + 7], 8,
                    offsets[n + 3], ends[n + 5] - offsets[n + 3]))
        if data == previous:
            break
    assert data == previous, "the offsets did not settle"

    with open(OUTPUT, "wb") as f:
        f.write(data)
    print("%s: %d bytes" % (OUTPUT, len(data)))


if __name__ == "__main__":
    main()
