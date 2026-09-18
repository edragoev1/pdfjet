/*
 * BMPImage.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;

namespace PDFjet.NET {
class BMPImage {
    int w = 0;              // Image width in pixels
    int h = 0;              // Image height in pixels

    byte[] image;           // The reconstructed image data
    byte[] deflated;        // The deflated reconstructed image data

    private int bpp;
    private byte[][] palette;
    private int[] masks;    // The red, green and blue masks of a 16 or 32 bit pixel
    private bool topDown;   // If the first row is the top row

    private const int BI_RGB = 0;
    private const int BI_BITFIELDS = 3;

    private static readonly int m10000000 = 0x80;
    private static readonly int m01000000 = 0x40;
    private static readonly int m00100000 = 0x20;
    private static readonly int m00010000 = 0x10;
    private static readonly int m00001000 = 0x08;
    private static readonly int m00000100 = 0x04;
    private static readonly int m00000010 = 0x02;
    private static readonly int m00000001 = 0x01;
    private static readonly int m11110000 = 0xF0;
    private static readonly int m00001111 = 0x0F;

    /* Tested with images created from GIMP */
    public BMPImage(System.IO.Stream stream) {
        palette = null;
        byte[] bm = GetBytes(stream, 2);
        // From Wikipedia
        if((bm[0] == 'B' && bm[1] == 'M')||
           (bm[0] == 'B' && bm[1] == 'A')||
           (bm[0] == 'C' && bm[1] == 'I')||
           (bm[0] == 'C' && bm[1] == 'P')||
           (bm[0] == 'I' && bm[1] == 'C')||
           (bm[0] == 'P' && bm[1] == 'T')) {
            SkipNBytes(stream, 8);
            int offset = ReadSignedInt(stream);     // Where the pixels start
            int headerSize = ReadSignedInt(stream);
            w = ReadSignedInt(stream);
            h = ReadSignedInt(stream);
            if (h < 0) {
                // A negative height is that of a top-down bitmap.
                h = -h;
                topDown = true;
            }
            SkipNBytes(stream, 2);
            bpp = Read2BytesLE(stream);
            int compression = ReadSignedInt(stream);
            // The size and the bit depth come from the file, so they are
            // checked before any buffer is allocated for the image.
            if (w <= 0 || h <= 0) {         // -int.MinValue is negative
                throw new Exception("Invalid BMP image size.");
            }
            if (bpp != 1 && bpp != 4 && bpp != 8 && bpp != 16 && bpp != 24 && bpp != 32) {
                throw new Exception(
                        "Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images");
            }
            // RLE and the JPEG and PNG compressions are not supported, and their
            // data is not read as pixels.
            if (compression != BI_RGB && !(compression == BI_BITFIELDS && (bpp == 16 || bpp == 32))) {
                throw new Exception("Compressed BMP images are not supported.");
            }
            // The older OS/2 header is 12 bytes; the others start with the 40
            // bytes of the BITMAPINFOHEADER.
            if (headerSize < 40) {
                throw new Exception("Unsupported BMP header of " + headerSize + " bytes.");
            }
            long rowSize = 4 * ((bpp * (long) w + 31) / 32);
            // A height of at most the limit keeps the products in a long.
            if (h > Decompressor.MAX_DECODED_LENGTH ||
                    3L * w * h > Decompressor.MAX_DECODED_LENGTH ||
                    rowSize * h > Decompressor.MAX_DECODED_LENGTH) {
                throw new Exception(
                        "The BMP image is larger than " + Decompressor.MAX_DECODED_LENGTH + " bytes.");
            }
            SkipNBytes(stream, 12);
            int colorsUsed = ReadSignedInt(stream);
            SkipNBytes(stream, 4);
            long read = 54;     // The bytes read so far

            // The masks follow the first 40 bytes of the header, in the larger
            // headers or after the BITMAPINFOHEADER. Without them the pixels
            // have the masks of the format: 5 bits a color in 16 bits, and 8
            // bits a color in 32 bits.
            if (compression == BI_BITFIELDS) {
                masks = new int[] {ReadSignedInt(stream), ReadSignedInt(stream), ReadSignedInt(stream)};
                read += 12;
            } else if (bpp == 16) {
                masks = new int[] {0x7C00, 0x03E0, 0x001F};
            } else if (bpp == 32) {
                masks = new int[] {0x00FF0000, 0x0000FF00, 0x000000FF};
            }
            if (14 + headerSize > read) {
                SkipNBytes(stream, (int) (14 + headerSize - read));     // The rest of a larger header
                read = 14 + headerSize;
            }

            if (bpp <= 8) {
                int numpalcol = (colorsUsed == 0) ? (1 << bpp) : colorsUsed;
                if (numpalcol < 0 || numpalcol > 256) {
                    throw new Exception("Invalid BMP palette size " + numpalcol + ".");
                }
                ParsePalette(stream, numpalcol);
                read += 4L * numpalcol;
            }
            if (offset > read) {
                SkipNBytes(stream, (int) (offset - read));     // The pixels start at the offset
            }
            ParseData(stream);
        } else {
            throw new Exception("BMP data could not be parsed!");
        }
    }

    private void ParseData(System.IO.Stream stream) {
        // rowsize is 4 * ceil (bpp*width/32.0)
        image = new byte[w * h * 3];

        int rowsize = (int) (4 * ((bpp * (long) w + 31) / 32));   // 4 byte alignment
        byte[] row;
        int index;
        try {
            for (int i = 0; i < h; i++) {
                row = GetBytes(stream, rowsize);
                switch (bpp) {
                case  1: row = Bit1to8(row, w); break;  // opslag i palette
                case  4: row = Bit4to8(row, w); break;  // opslag i palette
                case  8: break;                         // opslag i palette
                case 16: row = MasksTo24(row, w, 2, masks); break;
                case 24: break;                         // bytes are correct
                case 32: row = MasksTo24(row, w, 4, masks); break;
                default:
                    throw new Exception(
                            "Can only parse 1 bit, 4bit, 8bit, 16bit, 24bit and 32bit images");
                }

                index = topDown ? w*i*3 : w*(h-i-1)*3;
                if (palette != null) {  // indexed
                    for (int j = 0; j < w; j++) {
                        image[index++] = palette[(row[j]<0)?row[j]+256:row[j]][2];
                        image[index++] = palette[(row[j]<0)?row[j]+256:row[j]][1];
                        image[index++] = palette[(row[j]<0)?row[j]+256:row[j]][0];
                    }
                } else {                // not indexed
                    for (int j = 0; j < w*3; j+=3) {
                        image[index++] = row[j+2];
                        image[index++] = row[j+1];
                        image[index++] = row[j];
                    }
                }
            }
        } catch (IndexOutOfRangeException) {
            throw new Exception("BMP parse error: imagedata not correct");
        }

        deflated = Compressor.Deflate(image);
    }

    // Converts a row of 16 or 32 bit little endian pixels to blue, green and
    // red bytes, with the red, green and blue masks. A color of fewer than 8
    // bits is scaled to the full range, so that 31 of 5 bits is 255.
    private static byte[] MasksTo24(byte[] row, int width, int bytesPerPixel, int[] masks) {
        byte[] ret = new byte[width * 3];
        int j = 0;
        for (int i = 0; i < width * bytesPerPixel; i += bytesPerPixel) {
            int pixel = row[i] | row[i + 1] << 8;
            if (bytesPerPixel == 4) {
                pixel |= row[i + 2] << 16 | row[i + 3] << 24;
            }
            ret[j++] = (byte) ColorOf(pixel, masks[2]);
            ret[j++] = (byte) ColorOf(pixel, masks[1]);
            ret[j++] = (byte) ColorOf(pixel, masks[0]);
        }
        return ret;
    }

    // Returns the color of the pixel under the mask, from 0 to 255.
    private static int ColorOf(int pixel, int mask) {
        if (mask == 0) {
            return 0;
        }
        int shift = System.Numerics.BitOperations.TrailingZeroCount(mask);
        long max = (uint) mask >> shift;
        long value = (uint) (pixel & mask) >> shift;
        return (int) (value * 255 / max);
    }

    private static byte[] Bit4to8(byte[] row, int width) {
        byte[] ret = new byte[width];
        for(int i = 0; i < width; i++) {
            if (i % 2 == 0) {
                ret[i] =(byte) ((row[i/2] & m11110000)>>4);
            } else {
                ret[i] =(byte) ((row[i/2] & m00001111));
            }
        }
        return ret;
    }

    private static byte[] Bit1to8(byte[] row, int width) {
        byte[] ret = new byte[width];
        for(int i = 0; i < width; i++) {
            switch (i % 8) {
            case 0: ret[i] = (byte) ((row[i/8] & m10000000)>>7); break;
            case 1: ret[i] = (byte) ((row[i/8] & m01000000)>>6); break;
            case 2: ret[i] = (byte) ((row[i/8] & m00100000)>>5); break;
            case 3: ret[i] = (byte) ((row[i/8] & m00010000)>>4); break;
            case 4: ret[i] = (byte) ((row[i/8] & m00001000)>>3); break;
            case 5: ret[i] = (byte) ((row[i/8] & m00000100)>>2); break;
            case 6: ret[i] = (byte) ((row[i/8] & m00000010)>>1); break;
            case 7: ret[i] = (byte) ((row[i/8] & m00000001)); break;
            }
        }
        return ret;
    }

    private void ParsePalette(System.IO.Stream stream, int size) {
        palette = new byte[size][];
        for (int i = 0; i < size; i++) {
            palette[i] = GetBytes(stream, 4);
        }
    }

    private void SkipNBytes(System.IO.Stream inputStream, int n) {
        // Try seeking first
        if (inputStream.CanSeek) {
            inputStream.Seek(n, SeekOrigin.Current);
            return;
        }

        // Fallback: read and discard
        long skipped = 0;
        while (skipped < n) {
            byte[] buffer = new byte[Math.Min(8192, n - (int)skipped)];
            int read = inputStream.Read(buffer, 0, buffer.Length);
            if (read <= 0) {
                throw new Exception("Failed to skip " + n + " bytes");
            }
            skipped += read;
        }
    }

    private byte[] GetBytes(System.IO.Stream inputStream, int length) {
        byte[] buf = new byte[length];
        int totalRead = 0;
        while (totalRead < length) {
            int read = inputStream.Read(buf, totalRead, length - totalRead);
            // Stream.Read returns 0 at the end of the stream, where Java's read returns -1.
            if (read <= 0) {
                throw new Exception("Unexpected end of stream: expected " + length + " bytes");
            }
            totalRead += read;
        }
        return buf;
    }

    private int Read2BytesLE(System.IO.Stream inputStream) {
        byte[] buf = GetBytes(inputStream, 2);
        int val = 0;
        val |= buf[ 1 ] & 0xff;
        val <<= 8;
        val |= buf[ 0 ] & 0xff;
        return val;
    }

    private int ReadSignedInt(System.IO.Stream inputStream) {
        byte[] buf = GetBytes(inputStream, 4);
        long val = 0L;
        val |= (uint) buf[ 3 ] & 0xff;
        val <<= 8;
        val |= (uint) buf[ 2 ] & 0xff;
        val <<= 8;
        val |= (uint) buf[ 1 ] & 0xff;
        val <<= 8;
        val |= (uint) buf[ 0 ] & 0xff;
        return (int)val;
    }

    public int GetWidth() {
        return this.w;
    }

    public int GetHeight() {
        return this.h;
    }

    public byte[] GetData() {
        return this.deflated;
    }
}
}   // End of namespace PDFjet.NET