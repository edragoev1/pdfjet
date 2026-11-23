/*
 * PNGImage.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.zip.*;

/**
 * Used to embed PNG images in the PDF document.
 * <p>
 * <strong>Please note:</strong>
 * <p>
 *     Interlaced images are not supported.
 * <p>
 *     To convert interlaced image to non-interlaced image use OptiPNG:
 * <p>
 *     optipng -i0 -o7 myimage.png
 */
class PNGImage {
    int w;                      // Image width in pixels
    int h;                      // Image height in pixels

    byte[] iDAT;                // The compressed data in the IDAT chunks
    byte[] pLTE;                // The palette data
    byte[] tRNS;                // The alpha for the palette data
    // The transparent color the tRNS chunk of a grayscale or truecolor image
    // names, as the ranges of a /Mask: the minimum and the maximum of each
    // component, both the value of the chunk, in the bits of the samples.
    private int[] colorKeyMask;

    byte[] deflatedImageData;   // The deflated image data
    byte[] deflatedAlphaData;   // The deflated alpha channel data

    // The stream of the image object: the IDAT data as it is, whose PNG
    // filters the /DecodeParms of the image undo, or the deflated image data.
    byte[] stream;
    // The colors of a pixel of the IDAT data that the stream is, for the
    // /DecodeParms, or 0 when the stream is the deflated image data.
    int decodeColors;
    // The colors of the /Indexed color space of a palette image whose stream
    // is its IDAT data, one for each index the bit depth has, the ones past
    // the palette black; null for an image of RGB samples.
    byte[] palette;

    private byte bitDepth = 8;
    private byte colorType = 0;

    // The size the pHYs chunk gives the image, in points, or 0 when it gives
    // none: the chunk holds the pixels per unit of each axis, and only unit 1,
    // the metre, is a physical size. Unit 0 is the ratio of the two axes with
    // no size to it, so the image keeps the size of its pixels.
    private float physicalWidth;
    private float physicalHeight;

    // A metre is 72/0.0254 points.
    private static final double POINTS_PER_METER = 72.0/0.0254;

    /**
     * Used to embed PNG images in the PDF document.
     *
     * @param inputStream the inputStream.
     * @throws Exception  If an input or output exception occurred.
     */
    // An image to read the size of, for readSize
    private PNGImage() {
    }

    // Reads the size of the PNG of the stream, for ImageSize: its chunks read
    // as the constructor reads them, each with its CRC, the IHDR chunk and a
    // pHYs chunk kept, the image data and the others passed over, so that a
    // chunk of any length takes no memory; its header checked as
    // getImageDataLength checks it. The width and the height of the image
    // returned are its pixels, and its physical size the one it asks for.
    static PNGImage readSize(InputStream inputStream) throws Exception {
        PNGImage image = new PNGImage();
        image.validatePNG(inputStream);
        boolean hasIDAT = false;
        int chunks = 0;
        byte[] buf = new byte[65536];
        while (true) {
            long length = image.getLong(inputStream);
            if (length > Integer.MAX_VALUE) {
                throw new Exception("Invalid PNG chunk length " + length + ".");
            }
            byte[] type = image.getNBytes(inputStream, 4);
            String chunkType = new String(type, StandardCharsets.ISO_8859_1);
            // IHDR first, and once, as reading the image asks: the size of a PNG
            // that NewImage refused was given (review of 12 October 2026, R28)
            if (!chunkType.equals("IEND")) {
                if ((chunks == 0) != chunkType.equals("IHDR")) {
                    throw new Exception("Invalid PNG: the IHDR chunk must be the first chunk, and only the first.");
                }
                chunks++;
            }
            CRC32 crc = new CRC32();
            crc.update(type, 0, 4);
            if (chunkType.equals("IHDR") && length != 13) {
                throw new Exception("Invalid PNG IHDR chunk.");
            }
            byte[] data = null;
            if (chunkType.equals("IHDR") || (chunkType.equals("pHYs") && length == 9)) {
                data = image.getNBytes(inputStream, length);
                crc.update(data, 0, data.length);
            } else {
                long remaining = length;
                while (remaining > 0) {
                    int n = inputStream.read(buf, 0, (int) Math.min(remaining, buf.length));
                    if (n < 0) {
                        throw new Exception("Unexpected end of the PNG stream.");
                    }
                    crc.update(buf, 0, n);
                    remaining -= n;
                }
            }
            if (image.getLong(inputStream) != crc.getValue()) {
                throw new Exception("Chunk has bad CRC.");
            }
            if (chunkType.equals("IEND")) {
                break;
            } else if (chunkType.equals("IHDR")) {
                image.w = image.toIntValue(data, 0);
                image.h = image.toIntValue(data, 4);
                image.bitDepth = data[8];
                image.colorType = data[9];
                if (data[10] != 0) {
                    throw new Exception("Unknown PNG compression method.");
                }
                if (data[11] != 0) {
                    throw new Exception("Unknown PNG filter method.");
                }
                if (data[12] == 1) {
                    throw new Exception("Interlaced PNG images are not supported.");
                }
                if (data[12] != 0) {
                    throw new Exception("Unknown PNG interlace method.");
                }
            } else if (chunkType.equals("IDAT")) {
                // An empty one holds none of the image data (the review of
                // 9 October 2026)
                hasIDAT = hasIDAT || length > 0;
            } else if (chunkType.equals("PLTE")) {
                if (length % 3 != 0 || length == 0 || length > 3*256) {
                    throw new Exception("Incorrect palette length.");
                }
                image.pLTE = new byte[0];
            } else if (chunkType.equals("pHYs") && data != null) {
                image.readPhysicalSize(data);
            } else if ((type[0] & 0x20) == 0) {
                // An unknown critical chunk, as reading the image refuses it
                throw new Exception("Unknown critical PNG chunk " + chunkType + ".");
            }
        }
        image.getImageDataLength();
        if (!hasIDAT) {
            throw new Exception("The PNG image has no image data.");
        }
        return image;
    }

    PNGImage(InputStream inputStream) throws Exception {
        validatePNG(inputStream);

        // The IDAT chunks are joined in one buffer, not copied each time one
        // more of them is added.
        ByteArrayOutputStream idat = null;
        List<Chunk> chunks = processPNG(inputStream);
        for (int i = 0; i < chunks.size(); i++) {
            Chunk chunk = chunks.get(i);
            String chunkType = new String(chunk.type, StandardCharsets.ISO_8859_1);
            // IHDR first, and once, as the PNG specification asks: a tRNS before
            // it was read for a gray image and written as the /Mask of one
            // (review of 11 October 2026, #38)
            if ((i == 0) != chunkType.equals("IHDR")) {
                throw new Exception("Invalid PNG: the IHDR chunk must be the first chunk, and only the first.");
            }
            if (chunkType.equals("IHDR")) {
                if (chunk.getData().length != 13) {
                    throw new Exception("Invalid PNG IHDR chunk.");
                }
                this.w = toIntValue(chunk.getData(), 0);    // Width
                this.h = toIntValue(chunk.getData(), 4);    // Height
                this.bitDepth = chunk.getData()[8];         // Bit Depth
                this.colorType = chunk.getData()[9];        // Color Type
                // Only the deflate compression method and the adaptive filter
                // method are defined, and libpng refuses a file of another
                // one, where the rows of this one would be read as if it were 0.
                if (chunk.getData()[10] != 0) {
                    throw new Exception("Unknown PNG compression method.");
                }
                if (chunk.getData()[11] != 0) {
                    throw new Exception("Unknown PNG filter method.");
                }
                if (chunk.getData()[12] == 1) {
                    throw new Exception("Interlaced PNG images are not supported.\n" +
                            "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png");
                }
                // Only the interlace methods 0 and 1 are defined (ISO/IEC 15948,
                // 11.2.2); the rows of another one were read as if it were 0.
                if (chunk.getData()[12] != 0) {
                    throw new Exception("Unknown PNG interlace method.");
                }
            } else if (chunkType.equals("IDAT")) {
                if (idat == null) {
                    idat = new ByteArrayOutputStream();
                }
                idat.write(chunk.getData());
            } else if (chunkType.equals("PLTE")) {
                // 1 to 256 colors of 3 bytes each.
                byte[] colors = chunk.getData();
                if (colors.length % 3 != 0 || colors.length == 0 || colors.length > 3*256) {
                    throw new Exception("Incorrect palette length.");
                }
                // An index past the colors of the palette is drawn black, as
                // libpng and browsers draw it.
                pLTE = Arrays.copyOf(colors, 3*256);
            } else if (chunkType.equals("tRNS")) {
                if (colorType == 3) {
                    tRNS = chunk.getData();
                } else if (colorType == 0 || colorType == 2) {
                    colorKeyMask = colorKeyMaskOf(chunk.getData());
                }
            } else if (chunkType.equals("pHYs")) {
                readPhysicalSize(chunk.getData());
            } else if ((chunk.type[0] & 0x20) == 0) {
                // A critical chunk, of an uppercase first letter, that is not
                // known cannot be passed over (ISO/IEC 15948, 5.4).
                throw new Exception("Unknown critical PNG chunk " + chunkType + ".");
            }
            // The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
            // ports: the samples are embedded as they are.
        }

        if (idat != null) {
            iDAT = idat.toByteArray();
        }
        // The size and the bit depth of the color type checked, as their image
        // data must be no more than Decompressor.MAX_DECODED_LENGTH
        getImageDataLength();
        if (iDAT == null) {
            throw new Exception("The PNG image has no image data.");
        }
        // The image data is read a row at a time as it inflates, a row and the
        // one before it in memory, never the whole of it: an image of 8000 by
        // 8000 with alpha took 2.4 GB read whole (21 November 2026). The IDAT
        // data that is the rows of the image and nothing more is embedded as
        // it is, checked as it inflates, and the reader undoes the filters of
        // the rows, as a PNG decoder does. The IDAT data of an image with
        // alpha, whose alpha goes in a soft mask of its own, and any other IDAT
        // data are decoded and compressed again, as each row is undone. Data
        // after the last row is ignored.
        if (colorType == 3 && tRNS != null && !BMPImage.allBytesAre(tRNS, (byte) 255)) {
            if (embedPaletteWithAlpha()) {
                return;
            }
        } else if (colorType == 0 || colorType == 2 || colorType == 3) {
            int colors = (colorType == 2) ? 3 : 1;
            if (pngRows((this.w * colors * this.bitDepth + 7) / 8, 1, null)) {
                embedIDAT();
                return;
            }
        }
        // IDAT data that is not the image exactly, bytes after its stream or
        // a wrong checksum, is inflated a second time here, to be decoded:
        // rare, and only known once the first pass has read to its end
        decode();
        stream = deflatedImageData;
    }

    // Where pngRows hands each row, its filter undone
    private interface Row {
        void take(byte[] line) throws Exception;
    }

    // Reads the rows of the image data as it inflates, the row and the one
    // before it alone in memory: each row's filter undone and the row handed
    // to each, or, with no each, its filter type checked alone. It returns
    // whether the data is the image exactly. It fails as inflateExact does,
    // then on data shorter than the image, then on the first filter type that
    // PNG does not define, the order in which the image read whole failed.
    private boolean pngRows(final int bytesPerRow, final int bytesPerPixel, final Row each)
            throws Exception {
        final int rows = this.h;
        // The row grows with the data that inflates, and the two lines are
        // made when the first row is whole: a header of a width of millions of
        // pixels over a few bytes of data takes memory for those bytes alone
        final byte[][] raw = {new byte[Math.min(bytesPerRow + 1, 32 * 1024)]};
        final byte[][] lines = new byte[2][];
        final int first = Math.min(bytesPerPixel, bytesPerRow);
        // The bytes of the row read so far, the row, the first filter type
        // that is not one, and whether the row has one above it
        final int[] state = {0, 0, -1, 0};
        long length = (long) rows * (bytesPerRow + 1);
        boolean[] exact = new boolean[1];
        int n = Decompressor.inflateExactTo(iDAT, (int) length, new Decompressor.Sink() {
            public void write(byte[] p, int off, int len) throws Exception {
                int end = off + len;
                while (off < end && state[1] < rows) {
                    int k = Math.min(end - off, bytesPerRow + 1 - state[0]);
                    if (state[0] + k > raw[0].length) {
                        raw[0] = Arrays.copyOf(raw[0],
                                (int) Math.min(bytesPerRow + 1, Math.max(2L * raw[0].length, state[0] + k)));
                    }
                    System.arraycopy(p, off, raw[0], state[0], k);
                    state[0] += k;
                    off += k;
                    if (state[0] < bytesPerRow + 1) {
                        return;
                    }
                    int filter = raw[0][0] & 0xFF;
                    if (state[2] < 0 && filter > 4) {
                        state[2] = filter;
                    } else if (state[2] < 0 && each != null) {
                        if (lines[0] == null) {
                            lines[0] = new byte[bytesPerRow];
                            lines[1] = new byte[bytesPerRow];
                        }
                        byte[] line = lines[state[1] & 1];
                        byte[] prior = (state[3] == 1) ? lines[(state[1] + 1) & 1] : null;
                        System.arraycopy(raw[0], 1, line, 0, bytesPerRow);
                        unfilterLine(line, 0, prior, 0, bytesPerRow, filter, bytesPerPixel, first);
                        each.take(line);
                        state[3] = 1;
                    }
                    state[1]++;
                    state[0] = 0;
                }
            }
        }, exact);
        if (n < length) {
            throw new Exception("The PNG image data is shorter than the image.");
        }
        if (state[2] >= 0) {
            throw new Exception("Invalid PNG filter type " + state[2] + ".");
        }
        return exact[0];
    }

    // Makes the IDAT data of a grayscale, truecolor or palette image the
    // stream of the image object, which is faster than decoding the samples
    // and compressing them again; pngRows checked the filter type of each
    // row, as decoding checks it.
    private void embedIDAT() {
        stream = iDAT;
        decodeColors = (colorType == 2) ? 3 : 1;
        if (colorType == 3) {
            palette = Arrays.copyOf(pLTE, 3 << bitDepth);
        }
    }

    // Reads the rows of a palette image with a tRNS chunk of some alpha but
    // 255 once, its alpha decoded for the soft mask as the filter type of each
    // row is checked, and embeds its IDAT data when the data is the image
    // exactly, which it tells. The rows were read twice until the code review
    // of 23 November 2026: once for the filter types, once for the alpha.
    private boolean embedPaletteWithAlpha() throws Exception {
        final Compressor.ImageWriter alpha = new Compressor.ImageWriter();
        // indexes and alpha, made with the first whole row
        final byte[][] b = new byte[2][];
        boolean exact;
        try {
            exact = pngRows((this.w * this.bitDepth + 7) / 8, 1, new Row() {
                public void take(byte[] line) {
                    if (b[0] == null) {
                        b[0] = new byte[w];
                        b[1] = new byte[w];
                    }
                    paletteIndexesOf(line, b[0]);
                    paletteAlphaOf(b[0], b[1]);
                    alpha.write(b[1]);
                }
            });
        } catch (Exception e) {
            alpha.end();
            throw e;
        }
        if (!exact) {
            alpha.end();
            return false;
        }
        embedIDAT();
        setAlpha(alpha);
        return true;
    }

    // Keeps the deflated alpha of the pixels for the soft mask of the image,
    // unless the image has none or every pixel is opaque: an image with no
    // soft mask is drawn the same, is smaller, and is one that PDF/A-1 can
    // hold.
    private void setAlpha(Compressor.ImageWriter alpha) {
        if (alpha.isEmpty() || alpha.allAre(255)) {
            alpha.end();
            return;
        }
        deflatedAlphaData = alpha.toByteArray();
    }

    // Decodes the samples of the rows of the image as they inflate, a row at
    // a time, and deflates them as each row is undone; and the alpha, of a
    // grayscale or truecolor image with alpha or of a palette image's tRNS
    // chunk, for its soft mask.
    private void decode() throws Exception {
        final int w = this.w;
        final int size = (bitDepth == 16) ? 2 : 1;     // The bytes of a sample
        // The bit depth checked before the writers are made, so that none is
        // left to the garbage collector with its native zlib memory (code
        // review, 23 November 2026)
        if (colorType == 0 && bitDepth != 16 && bitDepth != 8 &&
                bitDepth != 4 && bitDepth != 2 && bitDepth != 1) {
            throw new Exception("Image with unsupported bit depth == " + bitDepth);
        }
        final Compressor.ImageWriter color = new Compressor.ImageWriter();
        final Compressor.ImageWriter alpha = new Compressor.ImageWriter();
        int bytesPerRow;
        int bytesPerPixel;
        Row each;
        if (colorType == 0) {
            // Grayscale Image
            if (bitDepth == 16) {
                bytesPerRow = 2 * w;
                bytesPerPixel = 2;
            } else if (bitDepth == 8) {
                bytesPerRow = w;
                bytesPerPixel = 1;
            } else {
                // 4, 2 or 1, checked above
                bytesPerRow = (w * bitDepth + 7) / 8;
                bytesPerPixel = 1;
            }
            each = new Row() {
                public void take(byte[] line) {
                    color.write(line);
                }
            };
        } else if (colorType == 4) {
            // Grayscale image with alpha, the alpha's first byte in the soft mask
            bytesPerRow = 2 * size * w;
            bytesPerPixel = 2 * size;
            final byte[][] b = new byte[2][];       // gray and alpha
            each = new Row() {
                public void take(byte[] line) {
                    if (b[0] == null) {
                        b[0] = new byte[size * w];
                        b[1] = new byte[w];
                    }
                    for (int i = 0; i < w; i++) {
                        System.arraycopy(line, 2 * size * i, b[0], size * i, size);
                        b[1][i] = line[2 * size * i + size];
                    }
                    color.write(b[0]);
                    alpha.write(b[1]);
                }
            };
        } else if (colorType == 6) {
            bytesPerRow = 4 * size * w;
            bytesPerPixel = 4 * size;
            final byte[][] b = new byte[2][];       // rgb and alpha
            each = new Row() {
                public void take(byte[] line) {
                    if (b[0] == null) {
                        b[0] = new byte[3 * size * w];
                        b[1] = new byte[w];
                    }
                    for (int i = 0; i < w; i++) {
                        System.arraycopy(line, 4 * size * i, b[0], 3 * size * i, 3 * size);
                        b[1][i] = line[4 * size * i + 3 * size];
                    }
                    color.write(b[0]);
                    alpha.write(b[1]);
                }
            };
        } else if (colorType == 2) {
            // True color image; a PLTE chunk in it is only a suggested palette
            bytesPerRow = 3 * size * w;
            bytesPerPixel = 3 * size;
            each = new Row() {
                public void take(byte[] line) {
                    color.write(line);
                }
            };
        } else {
            // Indexed image
            bytesPerRow = (w * bitDepth + 7) / 8;
            bytesPerPixel = 1;
            final byte[][] b = new byte[3][];       // indexes, rgb and alpha
            each = new Row() {
                public void take(byte[] line) {
                    if (b[0] == null) {
                        b[0] = new byte[w];
                        b[1] = new byte[3 * w];
                        b[2] = new byte[w];
                    }
                    paletteIndexesOf(line, b[0]);
                    for (int i = 0; i < w; i++) {
                        System.arraycopy(pLTE, 3 * (b[0][i] & 0xFF), b[1], 3 * i, 3);
                    }
                    color.write(b[1]);
                    if (tRNS != null) {
                        paletteAlphaOf(b[0], b[2]);
                        alpha.write(b[2]);
                    }
                }
            };
        }
        try {
            pngRows(bytesPerRow, bytesPerPixel, each);
        } catch (Exception e) {
            color.end();
            alpha.end();
            throw e;
        }
        deflatedImageData = color.toByteArray();
        setAlpha(alpha);
    }

    // Returns the color space of the image object: DeviceRGB for a palette
    // image, which is /Indexed on DeviceRGB when its stream is its IDAT data.
    String getColorSpace() {
        return (colorType == 0 || colorType == 4) ? "DeviceGray" : "DeviceRGB";
    }

    // Returns the bits per component of the image object.
    int getBitsPerComponent() {
        if (colorType == 0) {
            return bitDepth;
        } else if (colorType == 3 && palette != null) {
            return bitDepth;
        } else if (colorType != 3 && bitDepth == 16) {
            return 16;
        }
        return 8;
    }

    // Returns the /Mask of the transparent color the tRNS chunk of a grayscale
    // or truecolor image names: a sample of 2 bytes for each component, of
    // which the bits of the image are read, as libpng reads it, so that 255
    // in an image of 1 bit is white. A chunk of another length gives none.
    private int[] colorKeyMaskOf(byte[] data) {
        int components = (colorType == 0) ? 1 : 3;
        if (data.length != 2 * components) {
            return null;
        }
        int max = (1 << bitDepth) - 1;
        int[] mask = new int[2 * components];
        for (int i = 0; i < components; i++) {
            int sample = (((data[2 * i] & 0xFF) << 8) | (data[2 * i + 1] & 0xFF)) & max;
            mask[2 * i] = sample;
            mask[2 * i + 1] = sample;
        }
        return mask;
    }

    /**
     * Returns the /Mask of the transparent color of a grayscale or truecolor
     * image, the ranges of its components, or null when it has none.
     *
     * @return the ranges.
     */
    int[] getColorKeyMask() {
        return colorKeyMask;
    }

    // Reads the size the image asks to be drawn at from the pHYs chunk: the
    // pixels per unit of each axis and the unit they are in. A chunk of
    // another length, another unit or no pixels at all gives no size, and the
    // image keeps the size of its pixels; so does one whose size is too large
    // for a PDF number.
    private void readPhysicalSize(byte[] data) {
        if (data.length != 9 || data[8] != 1) {
            return;
        }
        int pixelsPerMeterX = toIntValue(data, 0);
        int pixelsPerMeterY = toIntValue(data, 4);
        if (pixelsPerMeterX <= 0 || pixelsPerMeterY <= 0) {
            return;     // Also for a count above 2^31, which reads negative.
        }
        float width = (float) (this.w*POINTS_PER_METER/pixelsPerMeterX);
        float height = (float) (this.h*POINTS_PER_METER/pixelsPerMeterY);
        if (FastFloat.isWritable(width) && FastFloat.isWritable(height)) {
            this.physicalWidth = width;
            this.physicalHeight = height;
        }
    }

    /**
     * Returns the width in points the pHYs chunk asks for, or 0 when the
     * image has no pHYs chunk with a size in it.
     *
     * @return the width in points, or 0.
     */
    public float getPhysicalWidth() {
        return this.physicalWidth;
    }

    /**
     * Returns the height in points the pHYs chunk asks for, or 0 when the
     * image has no pHYs chunk with a size in it.
     *
     * @return the height in points, or 0.
     */
    public float getPhysicalHeight() {
        return this.physicalHeight;
    }

    /**
     * Returns the image width.
     *
     * @return the image width.
     */
    public int getWidth() {
        return this.w;
    }

    /**
     * Returns the image height.
     *
     * @return the image height.
     */
    public int getHeight() {
        return this.h;
    }

    /**
     * Returns the image color type.
     *
     * @return the image color type.
     */
    public int getColorType() {
        return this.colorType;
    }

    /**
     * Returns the bit depth.
     *
     * @return the bit depth.
     */
    public int getBitDepth() {
        return this.bitDepth;
    }

    /**
     * Returns the image data: the samples, deflated. For an image whose stream
     * is its IDAT data they are decoded on the first call.
     *
     * @return the image data.
     * @throws Exception if the image data cannot be decoded.
     */
    public byte[] getData() throws Exception {
        if (this.deflatedImageData == null) {
            decode();
        }
        return this.deflatedImageData;
    }

    /**
     * Returns the image alpha data.
     *
     * @return the image alpha data.
     */
    public byte[] getAlpha() {
        return this.deflatedAlphaData;
    }

    private List<Chunk> processPNG(java.io.InputStream inputStream) throws Exception {
        List<Chunk> chunks = new ArrayList<Chunk>();
        while (true) {
            Chunk chunk = getChunk(inputStream);
            if ((new String(chunk.type, StandardCharsets.ISO_8859_1)).equals("IEND")) {
                break;
            }
            chunks.add(chunk);
        }
        return chunks;
    }

    // Checks the size, the bit depth and the color type of the IHDR chunk, and
    // returns the length of the decompressed image data: each row is a filter
    // type byte and the packed samples. The size comes from the file, so it is
    // checked before any buffer is allocated for the image.
    private long getImageDataLength() throws Exception {
        if (w <= 0 || h <= 0) {
            throw new Exception("Invalid PNG image size.");
        }
        // Each row has a filter type byte, so a taller image is too large; a
        // height of at most the limit also keeps the products below in a long.
        if (h > Decompressor.MAX_DECODED_LENGTH) {
            throw new Exception("The PNG image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
        }
        int channels;
        boolean validBitDepth;
        if (colorType == 0) {
            channels = 1;
            validBitDepth = bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
        } else if (colorType == 2) {
            channels = 3;
            validBitDepth = bitDepth == 8 || bitDepth == 16;
        } else if (colorType == 3) {
            channels = 1;
            validBitDepth = bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
        } else if (colorType == 4) {
            channels = 2;
            validBitDepth = bitDepth == 8 || bitDepth == 16;
        } else if (colorType == 6) {
            channels = 4;
            validBitDepth = bitDepth == 8 || bitDepth == 16;
        } else {
            throw new Exception("Invalid PNG color type " + (colorType & 0xff) + ".");
        }
        if (!validBitDepth) {
            throw new Exception("Invalid PNG bit depth " + (bitDepth & 0xff) + " for color type " + colorType + ".");
        }
        if (colorType == 3 && pLTE == null) {
            throw new Exception("The PNG palette image has no PLTE chunk.");
        }
        long bytesPerRow = ((long) w * channels * bitDepth + 7) / 8;
        long length = h * (1 + bytesPerRow);
        // A palette image becomes 3 bytes of RGB and 1 byte of alpha per pixel.
        long decodedLength = (colorType == 3) ? 4L * w * h : length;
        if (length > Decompressor.MAX_DECODED_LENGTH || decodedLength > Decompressor.MAX_DECODED_LENGTH) {
            throw new Exception("The PNG image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
        }
        return length;
    }

    private void validatePNG(InputStream stream) throws Exception {
        byte[] buf = getNBytes(stream, 8);
        if ((buf[0] & 0xFF) == 0x89 &&
                buf[1] == 0x50 &&
                buf[2] == 0x4E &&
                buf[3] == 0x47 &&
                buf[4] == 0x0D &&
                buf[5] == 0x0A &&
                buf[6] == 0x1A &&
                buf[7] == 0x0A) {
            // The PNG signature is correct.
        } else {
            throw new Exception("Wrong PNG signature.");
        }
    }

    private Chunk getChunk(InputStream inputStream) throws Exception {
        Chunk chunk = new Chunk();
        chunk.length = getLong(inputStream);                // The length of the data chunk.
        chunk.type = getNBytes(inputStream, 4);             // The chunk type.
        chunk.data = getNBytes(inputStream, chunk.length);  // The chunk data.
        chunk.crc = getLong(inputStream);                   // CRC of the type and data chunks.

        CRC32 crc = new CRC32();
        crc.update(chunk.type, 0, 4);
        crc.update(chunk.data, 0, (int) chunk.length);
        if (crc.getValue() != chunk.crc) {
            throw new Exception("Chunk has bad CRC.");
        }
        return chunk;
    }

    private long getLong(InputStream inputStream) throws Exception {
        byte[] buf = getNBytes(inputStream, 4);
        return (toIntValue(buf, 0) & 0x00000000ffffffffL);
    }

    // Reads the bytes in pieces, so that a chunk length that the file does not
    // have fails at the end of the stream instead of allocating the length.
    private byte[] getNBytes(InputStream inputStream, long length) throws Exception {
        if (length > Integer.MAX_VALUE) {
            throw new Exception("Invalid PNG chunk length " + length + ".");
        }
        ByteArrayOutputStream bos = new ByteArrayOutputStream((int) Math.min(length, 65536));
        byte[] buf = new byte[(int) Math.min(length, 65536)];
        long remaining = length;
        while (remaining > 0) {
            int count = inputStream.read(buf, 0, (int) Math.min(buf.length, remaining));
            if (count < 0) {
                throw new Exception("Unexpected end of the PNG stream.");
            }
            bos.write(buf, 0, count);
            remaining -= count;
        }
        return bos.toByteArray();
    }

    private int toIntValue(byte[] buf, int off) {
        return (buf[off] & 0xff) << 24 |
                (buf[off + 1] & 0xff) << 16 |
                (buf[off + 2] & 0xff) << 8 |
                (buf[off + 3] & 0xff);
    }

    // Undoes the filter of the row at line of the array, the row above it at
    // prior of the array above, none for the first row. A row holds
    // bytesPerRow bytes after its filter type, and the byte of the pixel on
    // the left is bytesPerPixel bytes before, or the byte before for pixels
    // smaller than a byte. The bytes of the first pixel, first of them, have
    // no byte on the left, and the first row no row above, so they are undone
    // apart from the rest, with the bytes they do not have taken as 0. It
    // throws on a filter type that PNG does not define, as libpng does.
    private static void unfilterLine(byte[] image, int line, byte[] above, int prior,
            int bytesPerRow, int filter, int bytesPerPixel, int first) throws Exception {
        if (filter == 0x00) {               // None
        } else if (filter == 0x01 || (filter == 0x04 && above == null)) {
            // Sub, and Paeth of the first row, which is the byte on the left.
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                image[line + i] += image[line + i - bytesPerPixel];
            }
        } else if (filter == 0x02) {        // Up
            if (above != null) {
                for (int i = 0; i < bytesPerRow; i++) {
                    image[line + i] += above[prior + i];
                }
            }
        } else if (filter == 0x03 && above == null) {   // Average of the first row
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                image[line + i] += (byte) ((image[line + i - bytesPerPixel] & 0xFF) / 2);
            }
        } else if (filter == 0x03) {        // Average
            for (int i = 0; i < first; i++) {
                image[line + i] += (byte) ((above[prior + i] & 0xFF) / 2);
            }
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                int a = image[line + i - bytesPerPixel] & 0xFF;
                int b = above[prior + i] & 0xFF;
                image[line + i] += (byte) ((a + b) / 2);
            }
        } else if (filter == 0x04) {        // Paeth, whose first pixel is the byte above
            for (int i = 0; i < first; i++) {
                image[line + i] += above[prior + i];
            }
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                int a = image[line + i - bytesPerPixel] & 0xFF;
                int b = above[prior + i] & 0xFF;
                int c = above[prior + i - bytesPerPixel] & 0xFF;
                image[line + i] += (byte) paeth(a, b, c);
            }
        } else {
            throw new Exception("Invalid PNG filter type " + filter + ".");
        }
    }

    // Returns whichever of the bytes on the left, above and above on the left
    // is nearest to their sum less the one above on the left.
    private static int paeth(int a, int b, int c) {
        int pa = Math.abs(b - c);           // p - a, where p = a + b - c
        int pb = Math.abs(a - c);           // p - b
        int pc = Math.abs(a + b - 2*c);     // p - c
        if (pa <= pb && pa <= pc) {
            return a;
        } else if (pb <= pc) {
            return b;
        }
        return c;
    }

    // Reads the palette's index of each pixel of a row.
    private void paletteIndexesOf(byte[] row, byte[] indexes) {
        int mask = (1 << this.bitDepth) - 1;
        for (int col = 0; col < this.w; col++) {
            int bit = col * this.bitDepth;
            int b = row[bit / 8] & 0xff;
            indexes[col] = (byte) ((b >> (8 - this.bitDepth - bit % 8)) & mask);
        }
    }

    // Gives each index of a row its alpha in the tRNS chunk, and 255 to an
    // index past its end.
    private void paletteAlphaOf(byte[] indexes, byte[] alpha) {
        for (int i = 0; i < indexes.length; i++) {
            int k = indexes[i] & 0xFF;
            alpha[i] = (k < tRNS.length) ? tRNS[k] : (byte) 0xff;
        }
    }

}   // End of PNGImage.java
