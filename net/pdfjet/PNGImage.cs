/*
 * PNGImage.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to embed PNG images in the PDF document.
/// </summary>
/// <remarks>
/// <para><strong>Please note:</strong> Interlaced images are not supported.</para>
/// <para>To convert interlaced image to non-interlaced image use OptiPNG:</para>
/// <code>optipng -i0 -o7 myimage.png</code>
/// </remarks>
internal class PNGImage {
    int w = 0;                  // Image width in pixels
    int h = 0;                  // Image height in pixels

    byte[] iDAT;                // The compressed data in the IDAT chunk
    byte[] pLTE;                // The palette data
    byte[] tRNS;                // The alpha for the palette data
    // The transparent color the tRNS chunk of a grayscale or truecolor image
    // names, as the ranges of a /Mask: the minimum and the maximum of each
    // component, both the value of the chunk, in the bits of the samples.
    private int[] colorKeyMask;

    byte[] deflatedImageData;   // The deflated reconstructed image data
    byte[] deflatedAlphaData;   // The deflated alpha channel data

    // The stream of the image object: the IDAT data as it is, whose PNG
    // filters the /DecodeParms of the image undo, or the deflated image data.
    internal byte[] stream;
    // The colors of a pixel of the IDAT data that the stream is, for the
    // /DecodeParms, or 0 when the stream is the deflated image data.
    internal int decodeColors;
    // The colors of the /Indexed color space of a palette image whose stream
    // is its IDAT data, one for each index the bit depth has, the ones past
    // the palette black; null for an image of RGB samples.
    internal byte[] palette;

    private byte bitDepth = 8;
    private int colorType = 0;

    // The size the pHYs chunk gives the image, in points, or 0 when it gives
    // none: the chunk holds the pixels per unit of each axis, and only unit 1,
    // the metre, is a physical size. Unit 0 is the ratio of the two axes with
    // no size to it, so the image keeps the size of its pixels.
    private float physicalWidth;
    private float physicalHeight;

    // A metre is 72/0.0254 points.
    private const double POINTS_PER_METER = 72.0/0.0254;

    /// <summary>
    /// Used to embed PNG images in the PDF document.
    /// </summary>
    // An image to read the size of, for ReadSize
    private PNGImage() {
    }

    // Reads the size of the PNG of the stream, for ImageSize: its chunks read
    // as the constructor reads them, each with its CRC, the IHDR chunk and a
    // pHYs chunk kept, the image data and the others passed over, so that a
    // chunk of any length takes no memory; its header checked as
    // GetImageDataLength checks it. The width and the height of the image
    // returned are its pixels, and its physical size the one it asks for.
    internal static PNGImage ReadSize(Stream inputStream) {
        PNGImage image = new PNGImage();
        image.ValidatePNG(inputStream);
        bool hasIDAT = false;
        int chunks = 0;
        byte[] buf = new byte[65536];
        while (true) {
            UInt32 length = image.GetUInt32(inputStream);
            if (length > int.MaxValue) {
                throw new InvalidDataException("Invalid PNG chunk length " + length + ".");
            }
            byte[] type = image.GetNBytes(inputStream, 4);
            String chunkType = System.Text.Encoding.UTF8.GetString(type);
            // IHDR first, and once, as reading the image asks: the size of a PNG
            // that NewImage refused was given (review of 12 October 2026, R28)
            if (!chunkType.Equals("IEND")) {
                if ((chunks == 0) != chunkType.Equals("IHDR")) {
                    throw new InvalidDataException("Invalid PNG: the IHDR chunk must be the first chunk, and only the first.");
                }
                chunks++;
            }
            CRC32 crc = new CRC32();
            crc.Update(type, 0, 4);
            if (chunkType.Equals("IHDR") && length != 13) {
                throw new InvalidDataException("Invalid PNG IHDR chunk.");
            }
            byte[] data = null;
            if (chunkType.Equals("IHDR") || (chunkType.Equals("pHYs") && length == 9)) {
                data = image.GetNBytes(inputStream, length);
                crc.Update(data, 0, data.Length);
            } else {
                long remaining = length;
                while (remaining > 0) {
                    int n = inputStream.Read(buf, 0, (int) Math.Min(remaining, buf.Length));
                    if (n <= 0) {
                        throw new EndOfStreamException("Unexpected end of the PNG stream.");
                    }
                    crc.Update(buf, 0, n);
                    remaining -= n;
                }
            }
            if (image.GetUInt32(inputStream) != crc.GetValue()) {
                throw new InvalidDataException("Chunk has bad CRC.");
            }
            if (chunkType.Equals("IEND")) {
                break;
            } else if (chunkType.Equals("IHDR")) {
                image.w = (int) image.ToUInt32(data, 0);
                image.h = (int) image.ToUInt32(data, 4);
                image.bitDepth = data[8];
                image.colorType = data[9];
                if (data[10] != 0) {
                    throw new InvalidDataException("Unknown PNG compression method.");
                }
                if (data[11] != 0) {
                    throw new InvalidDataException("Unknown PNG filter method.");
                }
                if (data[12] == 1) {
                    throw new NotSupportedException("Interlaced PNG images are not supported.");
                }
                if (data[12] != 0) {
                    throw new InvalidDataException("Unknown PNG interlace method.");
                }
            } else if (chunkType.Equals("IDAT")) {
                // An empty one holds none of the image data (the review of
                // 9 October 2026)
                hasIDAT = hasIDAT || length > 0;
            } else if (chunkType.Equals("PLTE")) {
                if (length % 3 != 0 || length == 0 || length > 3*256) {
                    throw new InvalidDataException("Incorrect palette length.");
                }
                image.pLTE = new byte[0];
            } else if (chunkType.Equals("pHYs") && data != null) {
                image.ReadPhysicalSize(data);
            } else if ((type[0] & 0x20) == 0) {
                // An unknown critical chunk, as reading the image refuses it
                throw new InvalidDataException("Unknown critical PNG chunk " + chunkType + ".");
            }
        }
        image.GetImageDataLength();
        if (!hasIDAT) {
            throw new InvalidDataException("The PNG image has no image data.");
        }
        return image;
    }

    public PNGImage(Stream inputStream) {
        ValidatePNG(inputStream);

        // The data of the IDAT chunks, joined in one stream: joined one array
        // at a time, a file of many chunks took time in the square of its size.
        MemoryStream idatChunks = null;
        List<Chunk> chunks = ProcessPNG(inputStream);
        for (int i = 0; i < chunks.Count; i++) {
            Chunk chunk = chunks[i];
            String chunkType = System.Text.Encoding.UTF8.GetString(chunk.type);
            // IHDR first, and once, as the PNG specification asks: a tRNS
            // before it was read for a gray image and written as the /Mask of
            // one (review of 11 October 2026, #38)
            if ((i == 0) != chunkType.Equals("IHDR")) {
                throw new InvalidDataException("Invalid PNG: the IHDR chunk must be the first chunk, and only the first.");
            }
            if (chunkType.Equals("IHDR")) {
                if (chunk.GetData().Length != 13) {
                    throw new InvalidDataException("Invalid PNG IHDR chunk.");
                }
                this.w = (int) ToUInt32(chunk.GetData(), 0);    // Width
                this.h = (int) ToUInt32(chunk.GetData(), 4);    // Height
                this.bitDepth = chunk.GetData()[8];             // Bit Depth
                this.colorType = chunk.GetData()[9];            // Color Type
                // Only the deflate compression method and the adaptive filter
                // method are defined, and libpng refuses a file of another
                // one, where the rows of this one would be read as if it were 0.
                if (chunk.GetData()[10] != 0) {
                    throw new InvalidDataException("Unknown PNG compression method.");
                }
                if (chunk.GetData()[11] != 0) {
                    throw new InvalidDataException("Unknown PNG filter method.");
                }

                if (chunk.GetData()[12] == 1) {
                    throw new NotSupportedException("Interlaced PNG images are not supported.\n" +
                            "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png");
                }
                // Only the interlace methods 0 and 1 are defined (ISO/IEC 15948,
                // 11.2.2); the rows of another one were read as if it were 0.
                if (chunk.GetData()[12] != 0) {
                    throw new InvalidDataException("Unknown PNG interlace method.");
                }
            } else if (chunkType.Equals("IDAT")) {
                if (idatChunks == null) {
                    idatChunks = new MemoryStream();
                }
                idatChunks.Write(chunk.GetData(), 0, chunk.GetData().Length);
            } else if (chunkType.Equals("PLTE")) {
                // 1 to 256 colors of 3 bytes each.
                byte[] colors = chunk.GetData();
                if (colors.Length % 3 != 0 || colors.Length == 0 || colors.Length > 3*256) {
                    throw new InvalidDataException("Incorrect palette length.");
                }
                // An index past the colors of the palette is drawn black, as
                // libpng and browsers draw it.
                pLTE = new byte[3*256];
                Array.Copy(colors, pLTE, colors.Length);
            } else if (chunkType.Equals("tRNS")) {
                if (colorType == 3) {
                    tRNS = chunk.GetData();
                } else if (colorType == 0 || colorType == 2) {
                    colorKeyMask = ColorKeyMaskOf(chunk.GetData());
                }
            } else if (chunkType.Equals("pHYs")) {
                ReadPhysicalSize(chunk.GetData());
            } else if ((chunk.type[0] & 0x20) == 0) {
                // A critical chunk, of an uppercase first letter, that is not
                // known cannot be passed over (ISO/IEC 15948, 5.4).
                throw new InvalidDataException("Unknown critical PNG chunk " + chunkType + ".");
            }
            // The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
            // ports: the samples are embedded as they are.
        }

        if (idatChunks != null) {
            iDAT = idatChunks.ToArray();
        }
        // The size and the bit depth of the color type checked, as their image
        // data must be no more than Decompressor.MAX_DECODED_LENGTH
        GetImageDataLength();
        if (iDAT == null) {
            throw new InvalidDataException("The PNG image has no image data.");
        }
        // The image data is read a row at a time as it inflates, a row and the
        // one before it in memory, never the whole of it: an image of 8000 by
        // 8000 with alpha took 1.3 GB read whole (21 November 2026). The IDAT
        // data that is the rows of the image and nothing more is embedded as
        // it is, checked as it inflates, and the reader undoes the filters of
        // the rows, as a PNG decoder does. The IDAT data of an image with
        // alpha, whose alpha goes in a soft mask of its own, and any other IDAT
        // data are decoded and compressed again, as each row is undone. Data
        // after the last row is ignored.
        if (colorType == 0 || colorType == 2 || colorType == 3) {
            int colors = (colorType == 2) ? 3 : 1;
            if (PngRows((w * colors * bitDepth + 7) / 8, 1, null)) {
                EmbedIDAT();
                return;
            }
        }
        Decode();
        stream = deflatedImageData;
    }

    // Reads the rows of the image data as it inflates, the row and the one
    // before it alone in memory: each row's filter undone and the row handed
    // to each, or, with no each, its filter type checked alone. Returns
    // whether the data is the image exactly. It fails as InflateExact does,
    // then on data shorter than the image, then on the first filter type that
    // PNG does not define, the order in which the image read whole failed.
    private bool PngRows(int bytesPerRow, int bytesPerPixel, Action<byte[]> each) {
        int rows = h;
        // The row grows with the data that inflates, and the two lines are
        // made when the first row is whole: a header of a width of millions of
        // pixels over a few bytes of data takes memory for those bytes alone
        byte[] raw = new byte[Math.Min(bytesPerRow + 1, 32 * 1024)];
        byte[][] lines = new byte[2][];
        int first = Math.Min(bytesPerPixel, bytesPerRow);
        int fill = 0;
        int row = 0;
        int badFilter = -1;
        byte[] prior = null;
        long length = (long) rows * (bytesPerRow + 1);
        int n = Decompressor.InflateExactTo(iDAT, (int) length, (p, off, len) => {
            int end = off + len;
            while (off < end && row < rows) {
                int k = Math.Min(end - off, bytesPerRow + 1 - fill);
                if (fill + k > raw.Length) {
                    Array.Resize(ref raw,
                            (int) Math.Min(bytesPerRow + 1, Math.Max(2L * raw.Length, fill + k)));
                }
                Array.Copy(p, off, raw, fill, k);
                fill += k;
                off += k;
                if (fill < bytesPerRow + 1) {
                    return;
                }
                int filter = raw[0];
                if (badFilter < 0 && filter > 4) {
                    badFilter = filter;
                } else if (badFilter < 0 && each != null) {
                    if (lines[0] == null) {
                        lines[0] = new byte[bytesPerRow];
                        lines[1] = new byte[bytesPerRow];
                    }
                    byte[] line = lines[row & 1];
                    Array.Copy(raw, 1, line, 0, bytesPerRow);
                    UnfilterLine(line, prior, bytesPerRow, filter, bytesPerPixel, first);
                    each(line);
                    prior = line;
                }
                row++;
                fill = 0;
            }
        }, out bool exact);
        if (n < length) {
            throw new InvalidDataException("The PNG image data is shorter than the image.");
        }
        if (badFilter >= 0) {
            throw new InvalidDataException("Invalid PNG filter type " + badFilter + ".");
        }
        return exact;
    }

    // Makes the IDAT data of a grayscale, truecolor or palette image the
    // stream of the image object, which is faster than decoding the samples
    // and compressing them again; PngRows checked the filter type of each row,
    // as decoding checks it. The alpha of a palette image with a tRNS chunk
    // is decoded for its soft mask, a row at a time.
    private void EmbedIDAT() {
        stream = iDAT;
        decodeColors = (colorType == 2) ? 3 : 1;
        if (colorType == 3) {
            palette = new byte[3 << bitDepth];
            Array.Copy(pLTE, palette, palette.Length);
            // A tRNS chunk of no alpha but 255 needs no decoding for its soft mask.
            if (tRNS != null && !BMPImage.AllBytesAre(tRNS, 255)) {
                var alpha = new Compressor.ImageWriter();
                byte[] indexes = null;
                byte[] a = null;
                try {
                    PngRows((w * bitDepth + 7) / 8, 1, line => {
                        if (a == null) {
                            indexes = new byte[w];
                            a = new byte[w];
                        }
                        PaletteIndexesOf(line, indexes);
                        PaletteAlphaOf(indexes, a);
                        alpha.Write(a);
                    });
                } catch {
                    alpha.Dispose();
                    throw;
                }
                SetAlpha(alpha);
            }
        }
    }

    // Keeps the deflated alpha of the pixels for the soft mask of the image,
    // unless the image has none or every pixel is opaque: an image with no
    // soft mask is drawn the same, is smaller, and is one that PDF/A-1 can
    // hold.
    private void SetAlpha(Compressor.ImageWriter alpha) {
        if (alpha.IsEmpty() || alpha.AllAre(255)) {
            alpha.Dispose();
            return;
        }
        deflatedAlphaData = alpha.ToArray();
    }

    // Decodes the samples of the rows of the image as they inflate, a row at
    // a time, and deflates them as each row is undone; and the alpha, of a
    // grayscale or truecolor image with alpha or of a palette image's tRNS
    // chunk, for its soft mask.
    private void Decode() {
        int size = (bitDepth == 16) ? 2 : 1;   // The bytes of a sample
        var color = new Compressor.ImageWriter();
        var alpha = new Compressor.ImageWriter();
        int bytesPerRow;
        int bytesPerPixel;
        Action<byte[]> each;
        if (colorType == 0) {
            // Grayscale Image
            if (bitDepth == 16) {
                bytesPerRow = 2 * w;
                bytesPerPixel = 2;
            } else if (bitDepth == 8) {
                bytesPerRow = w;
                bytesPerPixel = 1;
            } else if (bitDepth == 4 || bitDepth == 2 || bitDepth == 1) {
                bytesPerRow = (w * bitDepth + 7) / 8;
                bytesPerPixel = 1;
            } else {
                color.Dispose();
                alpha.Dispose();
                throw new NotSupportedException("Image with unsupported bit depth == " + bitDepth);
            }
            each = line => color.Write(line);
        } else if (colorType == 4) {
            // Grayscale image with alpha, the alpha's first byte in the soft mask
            bytesPerRow = 2 * size * w;
            bytesPerPixel = 2 * size;
            byte[] gray = null;
            byte[] a = null;
            each = line => {
                if (a == null) {
                    gray = new byte[size * w];
                    a = new byte[w];
                }
                for (int i = 0; i < w; i++) {
                    Array.Copy(line, 2 * size * i, gray, size * i, size);
                    a[i] = line[2 * size * i + size];
                }
                color.Write(gray);
                alpha.Write(a);
            };
        } else if (colorType == 6) {
            bytesPerRow = 4 * size * w;
            bytesPerPixel = 4 * size;
            byte[] rgb = null;
            byte[] a = null;
            each = line => {
                if (a == null) {
                    rgb = new byte[3 * size * w];
                    a = new byte[w];
                }
                for (int i = 0; i < w; i++) {
                    Array.Copy(line, 4 * size * i, rgb, 3 * size * i, 3 * size);
                    a[i] = line[4 * size * i + 3 * size];
                }
                color.Write(rgb);
                alpha.Write(a);
            };
        } else if (colorType == 2) {
            // True color image; a PLTE chunk in it is only a suggested palette
            bytesPerRow = 3 * size * w;
            bytesPerPixel = 3 * size;
            each = line => color.Write(line);
        } else {
            // Indexed image
            bytesPerRow = (w * bitDepth + 7) / 8;
            bytesPerPixel = 1;
            byte[] indexes = null;
            byte[] rgb = null;
            byte[] a = null;
            each = line => {
                if (a == null) {
                    indexes = new byte[w];
                    rgb = new byte[3 * w];
                    a = new byte[w];
                }
                PaletteIndexesOf(line, indexes);
                for (int i = 0; i < w; i++) {
                    Array.Copy(pLTE, 3 * indexes[i], rgb, 3 * i, 3);
                }
                color.Write(rgb);
                if (tRNS != null) {
                    PaletteAlphaOf(indexes, a);
                    alpha.Write(a);
                }
            };
        }
        try {
            PngRows(bytesPerRow, bytesPerPixel, each);
        } catch {
            color.Dispose();
            alpha.Dispose();
            throw;
        }
        deflatedImageData = color.ToArray();
        SetAlpha(alpha);
    }

    // Returns the color space and the bits per component of the image object:
    // RGB for a palette image, which is /Indexed on RGB when its stream is its
    // IDAT data.
    internal String GetColorSpace(out int bitsPerComponent) {
        if (colorType == 0) {
            bitsPerComponent = bitDepth;
            return "DeviceGray";
        } else if (colorType == 4) {
            bitsPerComponent = bitDepth;
            return "DeviceGray";
        } else if (colorType == 3) {
            bitsPerComponent = (palette != null) ? bitDepth : 8;
            return "DeviceRGB";
        }
        bitsPerComponent = (bitDepth == 16) ? 16 : 8;
        return "DeviceRGB";
    }

    // Reads the size the image asks to be drawn at from the pHYs chunk: the
    // pixels per unit of each axis and the unit they are in. A chunk of
    // another length, another unit or no pixels at all gives no size, and the
    // image keeps the size of its pixels; so does one whose size is too large
    // for a PDF number.
    private void ReadPhysicalSize(byte[] data) {
        if (data.Length != 9 || data[8] != 1) {
            return;
        }
        int pixelsPerMeterX = (int) ToUInt32(data, 0);
        int pixelsPerMeterY = (int) ToUInt32(data, 4);
        if (pixelsPerMeterX <= 0 || pixelsPerMeterY <= 0) {
            return;     // Also for a count above 2^31, which reads negative.
        }
        float width = (float) (this.w*POINTS_PER_METER/pixelsPerMeterX);
        float height = (float) (this.h*POINTS_PER_METER/pixelsPerMeterY);
        if (FastFloat.IsWritable(width) && FastFloat.IsWritable(height)) {
            this.physicalWidth = width;
            this.physicalHeight = height;
        }
    }

    /// <summary>
    /// Returns the width in points the pHYs chunk asks for, or 0 when the
    /// image has no pHYs chunk with a size in it.
    /// </summary>
    public float GetPhysicalWidth() {
        return this.physicalWidth;
    }

    /// <summary>
    /// Returns the height in points the pHYs chunk asks for, or 0 when the
    /// image has no pHYs chunk with a size in it.
    /// </summary>
    public float GetPhysicalHeight() {
        return this.physicalHeight;
    }

    /// <summary>Returns the image width.</summary>
    public int GetWidth() {
        return this.w;
    }

    /// <summary>Returns the image height.</summary>
    public int GetHeight() {
        return this.h;
    }

    /// <summary>Returns the PNG color type.</summary>
    public int GetColorType() {
        return this.colorType;
    }

    /// <summary>Returns the bit depth.</summary>
    public int GetBitDepth() {
        return this.bitDepth;
    }

    /// <summary>
    /// Returns the image data: the samples, deflated. For an image whose
    /// stream is its IDAT data they are decoded on the first call.
    /// </summary>
    public byte[] GetData() {
        if (this.deflatedImageData == null) {
            Decode();
        }
        return this.deflatedImageData;
    }

    // Returns the /Mask of the transparent color the tRNS chunk of a grayscale
    // or truecolor image names: a sample of 2 bytes for each component, of
    // which the bits of the image are read, as libpng reads it, so that 255
    // in an image of 1 bit is white. A chunk of another length gives none.
    private int[] ColorKeyMaskOf(byte[] data) {
        int components = (colorType == 0) ? 1 : 3;
        if (data.Length != 2 * components) {
            return null;
        }
        int max = (1 << bitDepth) - 1;
        int[] mask = new int[2 * components];
        for (int i = 0; i < components; i++) {
            int sample = ((data[2 * i] << 8) | data[2 * i + 1]) & max;
            mask[2 * i] = sample;
            mask[2 * i + 1] = sample;
        }
        return mask;
    }

    /// <summary>
    /// Returns the /Mask of the transparent color of a grayscale or truecolor
    /// image, the ranges of its components, or null when it has none.
    /// </summary>
    internal int[] GetColorKeyMask() {
        return colorKeyMask;
    }

    /// <summary>Returns the compressed alpha channel data.</summary>
    public byte[] GetAlpha() {
        return this.deflatedAlphaData;
    }

    private List<Chunk> ProcessPNG(System.IO.Stream inputStream) {
        List<Chunk> chunks = new List<Chunk>();
        while (true) {
            Chunk chunk = GetChunk(inputStream);
            String chunkType = System.Text.Encoding.UTF8.GetString(chunk.type);
            if (chunkType.Equals("IEND")) {
                break;
            }
            chunks.Add(chunk);
        }
        return chunks;
    }

    // Checks the size, the bit depth and the color type of the IHDR chunk, and
    // returns the length of the decompressed image data: each row is a filter
    // type byte and the packed samples. The size comes from the file, so it is
    // checked before any buffer is allocated for the image.
    private long GetImageDataLength() {
        if (w <= 0 || h <= 0) {
            throw new InvalidDataException("Invalid PNG image size.");
        }
        // Each row has a filter type byte, so a taller image is too large; a
        // height of at most the limit also keeps the products below in a long.
        if (h > Decompressor.MAX_DECODED_LENGTH) {
            throw new NotSupportedException("The PNG image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
        }
        int channels;
        bool validBitDepth;
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
            throw new InvalidDataException("Invalid PNG color type " + colorType + ".");
        }
        if (!validBitDepth) {
            throw new InvalidDataException("Invalid PNG bit depth " + bitDepth + " for color type " + colorType + ".");
        }
        if (colorType == 3 && pLTE == null) {
            throw new InvalidDataException("The PNG palette image has no PLTE chunk.");
        }
        long bytesPerRow = ((long) w * channels * bitDepth + 7) / 8;
        long length = h * (1 + bytesPerRow);
        // A palette image becomes 3 bytes of RGB and 1 byte of alpha per pixel.
        long decodedLength = (colorType == 3) ? 4L * w * h : length;
        if (length > Decompressor.MAX_DECODED_LENGTH || decodedLength > Decompressor.MAX_DECODED_LENGTH) {
            throw new NotSupportedException("The PNG image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
        }
        return length;
    }

    private void ValidatePNG(Stream inputStream) {
        byte[] buf = GetNBytes(inputStream, 8);
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
            throw new InvalidDataException("Wrong PNG signature.");
        }
    }

    private Chunk GetChunk(System.IO.Stream inputStream) {
        Chunk chunk = new Chunk();
        chunk.length = GetUInt32(inputStream);                  // The length of the data chunk.
        chunk.type = GetNBytes(inputStream, 4);                 // The chunk type.
        chunk.data = GetNBytes(inputStream, chunk.length);      // The chunk data.
        chunk.crc = GetUInt32(inputStream);                     // CRC of the type and data chunks.

        CRC32 crc = new CRC32();
        crc.Update(chunk.type, 0, 4);
        crc.Update(chunk.data, 0, (int) chunk.length);
        if (crc.GetValue() != chunk.crc) {
            throw new InvalidDataException("Chunk has bad CRC.");
        }

        return chunk;
    }

    private UInt32 GetUInt32(System.IO.Stream inputStream) {
        byte[] buf = GetNBytes(inputStream, 4);
        return ToUInt32(buf, 0);
    }

    // Reads the bytes in pieces, so that a chunk length that the file does not
    // have fails at the end of the stream instead of allocating the length.
    private byte[] GetNBytes(System.IO.Stream inputStream, UInt32 n) {
        if (n > int.MaxValue) {
            throw new InvalidDataException("Invalid PNG chunk length " + n + ".");
        }
        using var bos = new MemoryStream((int) Math.Min(n, 65536));
        byte[] buf = new byte[(int) Math.Min(n, 65536)];
        long remaining = n;
        while (remaining > 0) {
            // Stream.Read returns 0 at the end of the stream.
            int count = inputStream.Read(buf, 0, (int) Math.Min(buf.Length, remaining));
            if (count <= 0) {
                throw new EndOfStreamException("Unexpected end of the PNG stream.");
            }
            bos.Write(buf, 0, count);
            remaining -= count;
        }
        return bos.ToArray();
    }

    private UInt32 ToUInt32(byte[] buf, int off) {
        return ((UInt32) buf[off]) << 24 |
                ((UInt32) buf[off + 1]) << 16 |
                ((UInt32) buf[off + 2]) << 8 |
                ((UInt32) buf[off + 3]);
    }

    // Undoes the filter of a row, the row above it its prior, none for the
    // first row. A row holds bytesPerRow bytes after its filter type, and the
    // byte of the pixel on the left is bytesPerPixel bytes before, or the byte
    // before for pixels smaller than a byte. The bytes of the first pixel,
    // first of them, have no byte on the left, and the first row no row above,
    // so they are undone apart from the rest, with the bytes they do not have
    // taken as 0. It throws on a filter type that PNG does not define, as
    // libpng does.
    private static void UnfilterLine(byte[] line, byte[] prior, int bytesPerRow, int filter,
            int bytesPerPixel, int first) {
        if (filter == 0x00) {           // None
        } else if (filter == 0x01 || (filter == 0x04 && prior == null)) {
            // Sub, and Paeth of the first row, which is the byte on the left.
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                line[i] += line[i - bytesPerPixel];
            }
        } else if (filter == 0x02) {    // Up
            if (prior != null) {
                for (int i = 0; i < bytesPerRow; i++) {
                    line[i] += prior[i];
                }
            }
        } else if (filter == 0x03 && prior == null) {   // Average of the first row
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                line[i] += (byte) (line[i - bytesPerPixel] / 2);
            }
        } else if (filter == 0x03) {    // Average
            for (int i = 0; i < first; i++) {
                line[i] += (byte) (prior[i] / 2);
            }
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                int a = line[i - bytesPerPixel];    // The byte on the left
                int b = prior[i];                   // The byte above
                line[i] += (byte) ((a + b) / 2);
            }
        } else if (filter == 0x04) {    // Paeth, whose first pixel is the byte above
            for (int i = 0; i < first; i++) {
                line[i] += prior[i];
            }
            for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                int a = line[i - bytesPerPixel];    // Left, above and above on the left
                int b = prior[i];
                int c = prior[i - bytesPerPixel];
                line[i] += (byte) Paeth(a, b, c);
            }
        } else {
            throw new InvalidDataException("Invalid PNG filter type " + filter + ".");
        }
    }

    // Returns whichever of the bytes on the left, above and above on the left
    // is nearest to their sum less the one above on the left.
    private static int Paeth(int a, int b, int c) {
        int pa = b - c;     // p - a, where p = a + b - c
        int pb = a - c;     // p - b
        int pc = pa + pb;
        if (pa < 0) {
            pa = -pa;
        }
        if (pb < 0) {
            pb = -pb;
        }
        if (pc < 0) {
            pc = -pc;
        }
        if (pa <= pb && pa <= pc) {
            return a;
        } else if (pb <= pc) {
            return b;
        }
        return c;
    }

    // Reads the palette's index of each pixel of a row.
    private void PaletteIndexesOf(byte[] row, byte[] indexes) {
        int mask = (1 << this.bitDepth) - 1;
        for (int col = 0; col < this.w; col++) {
            int bit = col * this.bitDepth;
            int b = row[bit / 8];
            indexes[col] = (byte) ((b >> (8 - this.bitDepth - bit % 8)) & mask);
        }
    }

    // Gives each index of a row its alpha in the tRNS chunk, and opaque to an
    // index the chunk has none for.
    private void PaletteAlphaOf(byte[] indexes, byte[] alpha) {
        for (int i = 0; i < indexes.Length; i++) {
            alpha[i] = (indexes[i] < tRNS.Length) ? tRNS[indexes[i]] : (byte) 0xff;
        }
    }
}   // End of PNGImage.cs
}   // End of namespace PDFjet.NET
