"""Checks the images PDFjet embeds against Pillow: PNG, JPEG and BMP files.

    tests/references/images/fetch-images.sh .images
    python3 tests/references/images/check-images.py .images [--port go] [--jobs N] [--only SUBSTRING]

The images are Pillow's test images and the BMP Suite, which fetch-images.sh
fetches, the PngSuite and the images of the repository, and a set that the
check makes with Pillow, of each mode Pillow writes to the three formats. The
port embeds each one in a one-page PDF, the way a user would. MuPDF, through
PyMuPDF, decodes the image of the PDF, and its soft mask, and Pillow decodes
the file; the two must give the same pixels. The check fails on a file when:

- the port crashes on it, with a runtime error of its own, or takes longer
  than TIMEOUT seconds;
- Pillow reads it and the port refuses it;
- Pillow refuses it and the port embeds it;
- MuPDF has to repair the PDF the port wrote, or finds other than one image
  on its page;
- the image of the PDF has a different width or height in pixels, a
  different number of color components, or other samples than Pillow's --
  or, for a JPEG, than those MuPDF decodes the file itself to;
- the image is drawn at another size than the one its file asks for.

The pixels are compared as they are stored. Pillow does not turn an image by
the orientation of its Exif data unless it is asked to, and PDFjet does not
turn it either, so neither is turned. The samples are compared in the color
space of the file: gray, RGB or CMYK, as Pillow reads them. A palette is
looked up, a gray image of 1, 2 or 4 bits is scaled to 8, as MuPDF and Pillow
both scale it, and a gray image embedded as RGB is compared with its gray
repeated in the three components. A CMYK JPEG that Adobe software wrote holds
its inks inverted; Pillow inverts them back, and so does the /Decode array
PDFjet writes for it, which MuPDF applies. The alpha is the one a viewer
draws with: that of the alpha channel, or of the tRNS chunk of the file, or
255 for an image without one; and on the PDF's side that of the /SMask. An
image of 16 bits a sample is compared at 8, the high byte, which is what
MuPDF decodes it to, and its 16 bits are compared as well with those of the
stream where Pillow keeps them, a gray image.

The samples of a PNG or a BMP are compared exactly. A JPEG, which PDFjet
embeds as it is, is decoded by MuPDF with the IDCT and the upsampling of IJG
libjpeg 9, and by Pillow with those of libjpeg-turbo, which round
differently, most of all where a color changes sharply in chroma that is
subsampled. So its samples must be the ones MuPDF decodes the file itself to,
exactly, which leaves only what PDFjet writes -- the color space, the number
of components and the /Decode array -- to differ; and be within
JPEG_MEAN_TOLERANCE of Pillow's on average, which a color space or inks
taken the wrong way are far from. The files of the corpus that the port reads
and whose difference is understood are in known-differences.txt, one per
line, as "path: reason", and are counted rather than failed. A file listed
there that passes fails the check, so that the list does not keep what was
fixed.

The size an image is drawn at is its size in pixels, as points, unless the
file gives it a density: the pHYs chunk of a PNG in pixels per metre, the
JFIF density of a JPEG in dots per inch or per centimetre, and the pixels
per metre of a BMP. It is compared with the density Pillow reads from those
fields, and not with the resolution of the Exif data, which Pillow falls back
to for a JPEG and PDFjet does not read.

A JPEG whose header Pillow reads and whose scans it cannot decode, truncated
or corrupt, is counted as lenient rather than failed: PDFjet reads only the
header of a JPEG, and embeds the scans as they are, for the viewer to decode.
"""

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
import warnings
from collections import Counter
from concurrent.futures import ProcessPoolExecutor

import pymupdf
from PIL import Image, ImageChops, ImageStat

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
TIMEOUT = 30
EXTENSIONS = ('.png', '.jpg', '.jpeg', '.bmp')
# How far apart the samples MuPDF decodes a JPEG to and those Pillow decodes it
# to may be on average, of 255. Measured on the JPEGs of the corpus that are not
# broken: at most 1.4 on the photographs, 4.1 on hopper_bad_exif.jpg, whose
# contrast is so high that a rounding flips a sample from black to white, and
# 6.1 on the ones the check makes, whose blue changes from one pixel to the
# next. Inks left inverted, or RGB taken for YCbCr, are off by tens.
JPEG_MEAN_TOLERANCE = 8
# How far apart two sizes, in points, may be: the port writes 2 decimals of a
# float32, which holds 7 digits.
SIZE_TOLERANCE = 0.01
SIZE_RELATIVE_TOLERANCE = 1e-6
# The formats Pillow names for the files the port reads, by the port's type.
FORMATS = {'PNG': 'PNG', 'JPEG': 'JPEG', 'MPO': 'JPEG', 'BMP': 'BMP'}

pymupdf.TOOLS.mupdf_display_errors(False)
pymupdf.TOOLS.mupdf_display_warnings(False)
warnings.simplefilter('ignore')


def build_port(port, out):
    """Builds the port's harness and returns the command that runs it."""
    if port == 'go':
        exe = os.path.join(out, 'images-go')
        subprocess.run(['go', 'build', '-o', exe, './tests/references/images/go'], cwd=ROOT, check=True)
        return [exe]
    sys.exit(f'The {port} port has no harness yet.')


def make_images(made):
    """Makes, with Pillow, an image of each mode it writes to PNG, JPEG and BMP."""
    os.makedirs(made, exist_ok=True)
    width, height = 67, 43  # Odd, so that rows are padded and chroma is subsampled unevenly.
    rgba = Image.new('RGBA', (width, height))
    rgba.putdata([((x * 255) // (width - 1), (y * 255) // (height - 1), (x * y) % 256,
                   (x * 7 + y * 13) % 256) for y in range(height) for x in range(width)])
    rgb = rgba.convert('RGB')
    gray = rgb.convert('L')
    palette = rgb.quantize(200)
    images = {
        'gray-1.png': (gray.convert('1'), {}),
        'gray-8.png': (gray, {}),
        'gray-16.png': (gray.point(lambda v: v * 257, 'I').convert('I;16'), {}),
        'gray-alpha.png': (Image.merge('LA', (gray, rgba.getchannel('A'))), {}),
        'gray-trns.png': (gray, {'transparency': 128}),
        'palette.png': (palette, {}),
        'palette-trns.png': (palette, {'transparency': bytes(range(0, 200))}),
        'palette-4.png': (rgb.quantize(16), {'bits': 4}),
        'rgb.png': (rgb, {}),
        'rgb-trns.png': (rgb, {'transparency': rgb.getpixel((3, 3))}),
        'rgba.png': (rgba, {}),
        'rgb-dpi.png': (rgb, {'dpi': (300, 150)}),
        'gray.jpg': (gray, {'quality': 90}),
        'rgb.jpg': (rgb, {'quality': 90}),
        'rgb-444.jpg': (rgb, {'quality': 95, 'subsampling': 0}),
        'rgb-progressive.jpg': (rgb, {'quality': 90, 'progressive': True}),
        'rgb-dpi.jpg': (rgb, {'quality': 90, 'dpi': (200, 200)}),
        'cmyk.jpg': (rgb.convert('CMYK'), {'quality': 90}),
        'mono.bmp': (gray.convert('1'), {}),
        'gray.bmp': (gray, {}),
        'palette.bmp': (palette, {}),
        'rgb.bmp': (rgb, {}),
        'rgb-dpi.bmp': (rgb, {'dpi': (96, 120)}),
    }
    for name, (image, options) in images.items():
        image.save(os.path.join(made, name), **options)


def image_files(images, made):
    """Returns (name, path) of every image of the corpus, the name as known-differences.txt has it."""
    files = []
    roots = [(os.path.join(images, 'pillow', 'Tests', 'images'), 'pillow/Tests/images'),
             (os.path.join(images, 'bmpsuite'), 'bmpsuite'),
             (os.path.join(ROOT, 'PngSuite'), 'PngSuite'),
             (os.path.join(ROOT, 'images'), 'images'),
             (made, 'made')]
    for root, name in roots:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = sorted(d for d in dirnames if d != '.git')
            for filename in sorted(filenames):
                if filename.lower().endswith(EXTENSIONS):
                    path = os.path.join(dirpath, filename)
                    files.append((name + '/' + os.path.relpath(path, root), path))
    return files


def read_pillow(path):
    """Returns what Pillow reads from the file: its image and format, or an error.

    The image is None, and the error says why, when Pillow refuses the file;
    'header' is True when Pillow read its header and failed on its pixels.
    """
    result = {'image': None, 'header': False}
    try:
        image = Image.open(path)
        result['format'] = image.format
        result['size'] = image.size
        result['mode'] = image.mode
        result['header'] = True
        image.load()
    except Exception as e:
        result['error'] = f'{type(e).__name__}: {e}'
        return result
    result['image'] = image
    return result


def density(pillow):
    """The pixels per inch of each axis the file gives the image, as Pillow reads it, or None."""
    image = pillow['image']
    info = image.info
    if pillow['format'] in ('JPEG', 'MPO'):
        # Only the JFIF density: Pillow falls back to the Exif resolution, which the port does not read.
        unit, dpi = info.get('jfif_unit'), info.get('jfif_density')
        if unit == 1 and dpi:
            return dpi
        if unit == 2 and dpi:
            return dpi[0] * 2.54, dpi[1] * 2.54
        return None
    dpi = info.get('dpi')
    # A BMP that gives no pixels per metre has a dpi of 0 in Pillow.
    return dpi if dpi and dpi[0] > 0 and dpi[1] > 0 else None


def pillow_samples(image):
    """Returns the color samples Pillow decodes, as an 8-bit image of mode L, RGB or CMYK,
    its alpha as an image of mode L or None, and its 16-bit gray samples, big-endian, or None."""
    mode = image.mode
    has_alpha = mode in ('LA', 'La', 'PA', 'RGBA', 'RGBa') or 'transparency' in image.info
    sixteen = None
    if mode in ('I', 'I;16', 'I;16B', 'I;16L'):
        values = image.convert('I') if mode != 'I' else image
        sixteen = b''.join(max(0, min(65535, v)).to_bytes(2, 'big') for v in values.getdata())
        color = Image.frombytes('L', image.size, sixteen[0::2])
        alpha = None
        if 'transparency' in image.info:
            t = image.info['transparency']
            alpha = values.point(lambda v: 0 if v == t else 255).convert('L')
        return color, alpha, sixteen
    if mode == 'CMYK':
        return image, None, None
    if mode in ('1', 'L', 'LA', 'La'):
        if has_alpha:
            la = image.convert('LA')
            return la.getchannel('L'), la.getchannel('A'), None
        return image.convert('L'), None, None
    if mode in ('P', 'PA', 'RGB', 'RGBA', 'RGBa', 'RGBX'):
        if has_alpha:
            rgba = image.convert('RGBA')
            return rgba.convert('RGB'), rgba.getchannel('A'), None
        return image.convert('RGB'), None, None
    raise ValueError(f'Pillow mode {mode} is not compared')


def first_difference(a, b, names=('PDF', 'Pillow')):
    """The first pixel at which two images of the same mode and size differ, as a string."""
    box = ImageChops.difference(a, b).getbbox()
    if box is None:
        return 'none'
    y = box[1]
    for x in range(box[0], box[2]):
        if a.getpixel((x, y)) != b.getpixel((x, y)):
            return f'at ({x}, {y}) {names[0]} {a.getpixel((x, y))}, {names[1]} {b.getpixel((x, y))}'
    return f'in {box}'


def compare(what, pdf, pillow, tolerance, names=('PDF', 'Pillow')):
    """Compares two images of the same mode and size, and returns the problem or None."""
    diff = ImageChops.difference(pdf, pillow)
    extrema = diff.getextrema()
    if pdf.mode in ('L', '1'):
        extrema = [extrema]
    largest = max(e[1] for e in extrema)
    if largest == 0:
        return None
    mean = sum(ImageStat.Stat(diff).mean) / len(extrema)
    if tolerance is not None and mean <= tolerance:
        return None
    return f'{what} differ by up to {largest}, {mean:.3f} on average, first {first_difference(pdf, pillow, names)}'


def same_size(a, b):
    return abs(a - b) <= max(SIZE_TOLERANCE, SIZE_RELATIVE_TOLERANCE * abs(b))


def pdf_samples(doc, xref=None):
    """Returns the samples MuPDF decodes an image XObject to, or the image file doc
    is the path of, as an image of mode L, RGB or CMYK."""
    pix = pymupdf.Pixmap(doc, xref) if xref is not None else pymupdf.Pixmap(doc)
    if pix.alpha:
        pix = pymupdf.Pixmap(pix, 0)
    mode = {1: 'L', 3: 'RGB', 4: 'CMYK'}.get(pix.n)
    if mode is None:
        raise ValueError(f'MuPDF decodes the image to {pix.n} components')
    return Image.frombytes(mode, (pix.width, pix.height), pix.samples)


def quiet():
    """Sends the warnings the decoders print about corrupt data, in the workers, to nowhere."""
    devnull = os.open(os.devnull, os.O_WRONLY)
    os.dup2(devnull, 2)


def check(args):
    try:
        return check_file(*args)
    except Exception as e:
        # The exception is not returned: one of PyMuPDF's cannot be sent back
        # from the worker.
        return args[0], 'fail', [f'the check failed: {type(e).__name__}: {e}']


def check_file(name, path, command, out):
    pillow = read_pillow(path)
    with tempfile.TemporaryDirectory(dir=out) as tmp:
        pdf_path = os.path.join(tmp, 'image.pdf')
        try:
            run = subprocess.run(command + [path, pdf_path], capture_output=True, timeout=TIMEOUT)
        except subprocess.TimeoutExpired:
            return name, 'fail', [f'hangs: more than {TIMEOUT} s']
        try:
            port = json.loads(run.stdout)
        except ValueError:
            stderr = run.stderr.decode(errors='replace').strip().splitlines()
            return name, 'fail', [f'crashes: {stderr[-1] if stderr else run.returncode}']
        if port.get('crash'):
            return name, 'fail', [f'crashes: {port["crash"]}']

        kind = FORMATS.get(pillow.get('format'))
        if pillow['image'] is None or kind is None:
            if port.get('error'):
                return name, 'refused', []
            if pillow['header'] and kind == 'JPEG':
                return name, 'lenient', []
            why = pillow.get('error') or f'it reads it as {pillow.get("format")}'
            return name, 'fail', [f'embedded, and Pillow refuses it: {why}']
        if port.get('error'):
            return name, 'fail', [f'refused: {port["error"].splitlines()[0]}']

        doc = pymupdf.open(pdf_path)
        problems = []
        if doc.is_repaired:
            problems.append('MuPDF repairs the PDF')
        page = doc[0]
        images = page.get_images(full=True)
        if len(images) != 1:
            return name, 'fail', problems + [f'{len(images)} images on the page']
        xref, smask, width, height, bpc = images[0][:5]
        image = pillow['image']
        if (width, height) != image.size:
            return name, 'fail', problems + [f'{width}x{height} pixels, Pillow {image.size[0]}x{image.size[1]}']

        # The size the image is drawn at.
        rects = page.get_image_rects(xref)
        dpi = density(pillow)
        expected = (width, height) if not dpi else (width * 72 / dpi[0], height * 72 / dpi[1])
        if len(rects) != 1:
            problems.append(f'drawn {len(rects)} times')
        elif not (same_size(rects[0].width, expected[0]) and same_size(rects[0].height, expected[1])):
            problems.append(f'drawn at {rects[0].width:g}x{rects[0].height:g} points, '
                            f'not {expected[0]:g}x{expected[1]:g}')

        color, alpha, sixteen = pillow_samples(image)
        samples = pdf_samples(doc, xref)
        if samples.mode == 'RGB' and color.mode == 'L':
            color = color.convert('RGB')
        tolerance = JPEG_MEAN_TOLERANCE if kind == 'JPEG' else None
        if samples.mode != color.mode:
            problems.append(f'embedded as {samples.mode}, Pillow reads {image.mode}')
        else:
            problem = compare('samples', samples, color, tolerance)
            if problem:
                problems.append(problem)
        if kind == 'JPEG':
            mupdf = pdf_samples(path)
            if mupdf.mode != samples.mode:
                problems.append(f'embedded as {samples.mode}, MuPDF reads the file as {mupdf.mode}')
            else:
                problem = compare('MuPDF\'s samples of the PDF and of the file', samples, mupdf, None,
                                  ('PDF', 'file'))
                if problem:
                    problems.append(problem)
        if sixteen is not None and bpc == 16:
            if doc.xref_stream(xref) != sixteen:
                problems.append('the 16-bit samples differ from those of the stream')

        pdf_alpha = pdf_samples(doc, smask) if smask else None
        if pdf_alpha is not None and pdf_alpha.size != image.size:
            problems.append(f'the soft mask is {pdf_alpha.size[0]}x{pdf_alpha.size[1]}')
        elif pdf_alpha is not None or alpha is not None:
            opaque = Image.new('L', image.size, 255)
            problem = compare('alpha', opaque if pdf_alpha is None else pdf_alpha,
                              opaque if alpha is None else alpha, None)
            if problem:
                problems.append(problem)
        return name, 'fail' if problems else 'pass', problems


def read_known(path):
    known = {}
    with open(path, encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith('#'):
                name, _, reason = line.partition(': ')
                known[name] = reason
    return known


def category(problem):
    """The kind of a problem, with the file's own numbers and words left out."""
    problem = re.sub(r'differ by .*', 'differ', problem)
    problem = re.sub(r'[\d.]+x[\d.]+', 'WxH', problem)
    problem = re.sub(r'\d+', 'N', problem)
    return problem if len(problem) < 80 else problem[:77] + '...'


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('images')
    parser.add_argument('--port', default='go')
    parser.add_argument('--jobs', type=int, default=os.cpu_count())
    parser.add_argument('--only', default='')
    args = parser.parse_args()

    known = read_known(os.path.join(HERE, 'known-differences.txt'))
    with tempfile.TemporaryDirectory() as out:
        command = build_port(args.port, out)
        made = os.path.join(out, 'made')
        make_images(made)
        files = [f for f in image_files(args.images, made) if args.only in f[0]]
        tasks = [(name, path, command, out) for name, path in files]
        counts = Counter()
        kinds = Counter()
        with ProcessPoolExecutor(args.jobs, initializer=quiet) as pool:
            for name, result, problems in pool.map(check, tasks, chunksize=4):
                if name in known:
                    if result == 'fail':
                        result = 'known'
                    else:
                        problems = [f'is listed in known-differences.txt and no longer fails ({result})']
                        result = 'fail'
                counts[result] += 1
                if result == 'fail':
                    kinds.update({category(p) for p in problems})
                    print(f'{name}: {"; ".join(problems)}', flush=True)

    print()
    print(f'{len(files)} files: {counts["pass"]} pass, {counts["fail"]} fail, '
          f'{counts["known"]} known differences, {counts["refused"]} that Pillow and the port '
          f'both refuse, {counts["lenient"]} broken JPEGs the port embeds as they are.')
    for kind, count in kinds.most_common():
        print(f'  {count:5d}  {kind}')
    sys.exit(1 if counts['fail'] else 0)


if __name__ == '__main__':
    main()
