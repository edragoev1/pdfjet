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
    public PNGImage(Stream inputStream) {
        ValidatePNG(inputStream);

        // The data of the IDAT chunks, joined in one stream: joined one array
        // at a time, a file of many chunks took time in the square of its size.
        MemoryStream idatChunks = null;
        List<Chunk> chunks = ProcessPNG(inputStream);
        foreach (Chunk chunk in chunks) {
            String chunkType = System.Text.Encoding.UTF8.GetString(chunk.type);
            if (chunkType.Equals("IHDR")) {
                if (chunk.GetData().Length != 13) {
                    throw new Exception("Invalid PNG IHDR chunk.");
                }
                this.w = (int) ToUInt32(chunk.GetData(), 0);    // Width
                this.h = (int) ToUInt32(chunk.GetData(), 4);    // Height
                this.bitDepth = chunk.GetData()[8];             // Bit Depth
                this.colorType = chunk.GetData()[9];            // Color Type
                // Only the deflate compression method and the adaptive filter
                // method are defined, and libpng refuses a file of another
                // one, where the rows of this one would be read as if it were 0.
                if (chunk.GetData()[10] != 0) {
                    throw new Exception("Unknown PNG compression method.");
                }
                if (chunk.GetData()[11] != 0) {
                    throw new Exception("Unknown PNG filter method.");
                }

                if (chunk.GetData()[12] == 1) {
                    throw new Exception("Interlaced PNG images are not supported.\n" +
                            "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png");
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
                    throw new Exception("Incorrect palette length.");
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
            }
            // The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
            // ports: the samples are embedded as they are.
        }

        if (idatChunks != null) {
            iDAT = idatChunks.ToArray();
        }
        long imageDataLength = GetImageDataLength();
        if (iDAT == null) {
            throw new Exception("The PNG image has no image data.");
        }
        // The rows of the image; data after the last row is ignored.
        byte[] inflatedImageData = Decompressor.InflateExact(iDAT, (int) imageDataLength, out bool exact);
        if (inflatedImageData.Length < imageDataLength) {
            throw new Exception("The PNG image data is shorter than the image.");
        }

        // The IDAT data that is the rows of the image and nothing more is
        // embedded as it is, and the reader undoes the filters of the rows, as
        // a PNG decoder does. The IDAT data of an image with alpha, whose alpha
        // goes in a soft mask of its own, and any other IDAT data are decoded
        // and compressed again.
        if (exact && (colorType == 0 || colorType == 2 || colorType == 3)) {
            EmbedIDAT(inflatedImageData);
        } else {
            Decode(inflatedImageData);
            stream = deflatedImageData;
        }
    }

    // Makes the IDAT data of a grayscale, truecolor or palette image the
    // stream of the image object, which is faster than decoding the samples
    // and compressing them again. The filter type of each row is checked, as
    // decoding checks it; the alpha of a palette image with a tRNS chunk is
    // decoded for its soft mask.
    private void EmbedIDAT(byte[] buf) {
        int colors = (colorType == 2) ? 3 : 1;
        int bytesPerRow = (w * colors * bitDepth + 7) / 8;
        for (int row = 0; row < h; row++) {
            int filter = buf[row * (bytesPerRow + 1)];
            if (filter > 4) {
                throw new Exception("Invalid PNG filter type " + filter + ".");
            }
        }
        stream = iDAT;
        decodeColors = colors;
        if (colorType == 3) {
            palette = new byte[3 << bitDepth];
            Array.Copy(pLTE, palette, palette.Length);
            if (tRNS != null) {
                byte[] indexes = PaletteIndexes(Unfilter(buf, h, bytesPerRow, 1));
                deflatedAlphaData = Compressor.Deflate(PaletteAlpha(indexes));
            }
        }
    }

    // Decodes the samples of the rows of the image, and deflates them.
    private void Decode(byte[] inflatedImageData) {
        byte[] imageData;
        if (colorType == 0) {
            // Grayscale Image
            if (bitDepth == 16) {
                imageData = GetImageColorType0BitDepth16(inflatedImageData);
            } else if (bitDepth == 8) {
                imageData = GetImageColorType0BitDepth8(inflatedImageData);
            } else if (bitDepth == 4 || bitDepth == 2 || bitDepth == 1) {
                imageData = GetImageColorType0BitDepthBelow8(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 4) {
            // Grayscale image with alpha
            if (bitDepth == 8) {
                imageData = GetImageColorType4BitDepth8(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 6) {
            if (bitDepth == 8) {
                imageData = GetImageColorType6BitDepth8(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 2) {
            // True color image; a PLTE chunk in it is only a suggested palette
            if (bitDepth == 16) {
                imageData = GetImageColorType2BitDepth16(inflatedImageData);
            } else {
                imageData = GetImageColorType2BitDepth8(inflatedImageData);
            }
        } else {
            // Indexed image
            imageData = GetImageColorType3(inflatedImageData);
        }

        deflatedImageData = Compressor.Deflate(imageData);
    }

    // Returns the color space and the bits per component of the image object:
    // RGB for a palette image, which is /Indexed on RGB when its stream is its
    // IDAT data.
    internal String GetColorSpace(out int bitsPerComponent) {
        if (colorType == 0) {
            bitsPerComponent = bitDepth;
            return "DeviceGray";
        } else if (colorType == 4) {
            bitsPerComponent = 8;
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
            Decode(Decompressor.InflatePrefix(iDAT, (int) GetImageDataLength()));
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
            throw new Exception("Invalid PNG image size.");
        }
        // Each row has a filter type byte, so a taller image is too large; a
        // height of at most the limit also keeps the products below in a long.
        if (h > Decompressor.MAX_DECODED_LENGTH) {
            throw new Exception("The PNG image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
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
            throw new Exception("Invalid PNG color type " + colorType + ".");
        }
        if (!validBitDepth) {
            throw new Exception("Invalid PNG bit depth " + bitDepth + " for color type " + colorType + ".");
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
            throw new Exception("Wrong PNG signature.");
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
            throw new Exception("Chunk has bad CRC.");
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
            throw new Exception("Invalid PNG chunk length " + n + ".");
        }
        using var bos = new MemoryStream((int) Math.Min(n, 65536));
        byte[] buf = new byte[(int) Math.Min(n, 65536)];
        long remaining = n;
        while (remaining > 0) {
            // Stream.Read returns 0 at the end of the stream.
            int count = inputStream.Read(buf, 0, (int) Math.Min(buf.Length, remaining));
            if (count <= 0) {
                throw new Exception("Unexpected end of the PNG stream.");
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

    // Returns the rows of the image data without the filter type byte each
    // begins with, and with their filters undone. A row holds bytesPerRow
    // bytes after the filter type, and the byte of the pixel on the left is
    // bytesPerPixel bytes before, or the byte before for pixels smaller than a
    // byte. It throws on a filter type that PNG does not define, as libpng does.
    private static byte[] Unfilter(byte[] buf, int rows, int bytesPerRow, int bytesPerPixel) {
        byte[] image = new byte[rows * bytesPerRow];
        int prior = -1;     // Where the row above begins, none for the first row
        for (int row = 0; row < rows; row++) {
            int offset = row * (bytesPerRow + 1);
            int filter = buf[offset];
            int line = row * bytesPerRow;
            Array.Copy(buf, offset + 1, image, line, bytesPerRow);
            if (filter == 0x00) {           // None
            } else if (filter == 0x01) {    // Sub
                for (int i = bytesPerPixel; i < bytesPerRow; i++) {
                    image[line + i] += image[line + i - bytesPerPixel];
                }
            } else if (filter == 0x02) {    // Up
                if (prior >= 0) {
                    for (int i = 0; i < bytesPerRow; i++) {
                        image[line + i] += image[prior + i];
                    }
                }
            } else if (filter == 0x03) {    // Average
                for (int i = 0; i < bytesPerRow; i++) {
                    int a = (i >= bytesPerPixel) ? image[line + i - bytesPerPixel] : 0;    // The byte on the left
                    int b = (prior >= 0) ? image[prior + i] : 0;                            // The byte above
                    image[line + i] += (byte) ((a + b) / 2);
                }
            } else if (filter == 0x04) {    // Paeth
                for (int i = 0; i < bytesPerRow; i++) {
                    int a = 0;  // Left, above and above on the left
                    int b = 0;
                    int c = 0;
                    if (i >= bytesPerPixel) {
                        a = image[line + i - bytesPerPixel];
                    }
                    if (prior >= 0) {
                        b = image[prior + i];
                        if (i >= bytesPerPixel) {
                            c = image[prior + i - bytesPerPixel];
                        }
                    }
                    image[line + i] += (byte) Paeth(a, b, c);
                }
            } else {
                throw new Exception("Invalid PNG filter type " + filter + ".");
            }
            prior = line;
        }
        return image;
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

    // Truecolor Image with Bit Depth == 16
    private byte[] GetImageColorType2BitDepth16(byte[] buf) {
        return Unfilter(buf, this.h, 6 * this.w, 6);
    }

    // Truecolor Image with Bit Depth == 8
    private byte[] GetImageColorType2BitDepth8(byte[] buf) {
        return Unfilter(buf, this.h, 3 * this.w, 3);
    }

    // Truecolor Image with Alpha Transparency
    // The gray samples are the image and the alpha samples its soft mask.
    private byte[] GetImageColorType4BitDepth8(byte[] buf) {
        byte[] image = Unfilter(buf, this.h, 2 * this.w, 2);
        byte[] gray = new byte[this.w * this.h];
        byte[] alpha = new byte[this.w * this.h];
        for (int i = 0; i < gray.Length; i++) {
            gray[i] = image[2 * i];
            alpha[i] = image[2 * i + 1];
        }
        deflatedAlphaData = Compressor.Deflate(alpha);
        return gray;
    }

    private byte[] GetImageColorType6BitDepth8(byte[] buf) {
        byte[] image = Unfilter(buf, this.h, 4 * this.w, 4);
        byte[] idata = new byte[3 * this.w * this.h];   // Image data
        byte[] alpha = new byte[this.w * this.h];       // Alpha values
        for (int i = 0; i < alpha.Length; i++) {
            idata[3*i] = image[4*i];
            idata[3*i + 1] = image[4*i + 1];
            idata[3*i + 2] = image[4*i + 2];
            alpha[i] = image[4*i + 3];
        }
        deflatedAlphaData = Compressor.Deflate(alpha);

        return idata;
    }

    // Indexed-color image with bit depth == 1, 2, 4 or 8
    // Each value is a palette index; a PLTE chunk shall appear.
    // The filters are undone on the packed indexes, one byte per pixel whatever
    // the bit depth, before the indexes are looked up in the palette.
    private byte[] GetImageColorType3(byte[] buf) {
        int bytesPerLine = (this.w * this.bitDepth + 7) / 8;
        byte[] indexes = PaletteIndexes(Unfilter(buf, this.h, bytesPerLine, 1));

        byte[] image = new byte[3 * indexes.Length];
        for (int i = 0; i < indexes.Length; i++) {
            Array.Copy(pLTE, 3 * indexes[i], image, 3 * i, 3);
        }

        if (tRNS != null) {
            deflatedAlphaData = Compressor.Deflate(PaletteAlpha(indexes));
        }

        return image;
    }

    // Returns the palette index of each pixel, a byte each, of the unfiltered
    // rows of a palette image.
    private byte[] PaletteIndexes(byte[] rows) {
        int bytesPerLine = (this.w * this.bitDepth + 7) / 8;
        byte[] indexes = new byte[this.w * this.h];
        int mask = (1 << this.bitDepth) - 1;
        int n = 0;
        for (int row = 0; row < this.h; row++) {
            for (int col = 0; col < this.w; col++) {
                int bit = col * this.bitDepth;
                int b = rows[row * bytesPerLine + bit / 8];
                indexes[n++] = (byte) ((b >> (8 - this.bitDepth - bit % 8)) & mask);
            }
        }
        return indexes;
    }

    // Returns the alpha of each pixel of a palette image with a tRNS chunk:
    // the alpha of its index, or opaque for an index the chunk has none for.
    private byte[] PaletteAlpha(byte[] indexes) {
        byte[] alpha = new byte[indexes.Length];
        for (int i = 0; i < indexes.Length; i++) {
            alpha[i] = (indexes[i] < tRNS.Length) ? tRNS[indexes[i]] : (byte) 0xff;
        }
        return alpha;
    }

    // Grayscale Image with Bit Depth == 16
    private byte[] GetImageColorType0BitDepth16(byte[] buf) {
        return Unfilter(buf, this.h, 2 * this.w, 2);
    }

    // Grayscale Image with Bit Depth == 8
    private byte[] GetImageColorType0BitDepth8(byte[] buf) {
        return Unfilter(buf, this.h, this.w, 1);
    }

    // A grayscale image of 1, 2 or 4 bits per pixel. The filters work on
    // bytes, one byte per pixel whatever the bit depth.
    private byte[] GetImageColorType0BitDepthBelow8(byte[] buf) {
        return Unfilter(buf, this.h, (this.w * this.bitDepth + 7) / 8, 1);
    }
}   // End of PNGImage.cs
}   // End of namespace PDFjet.NET
