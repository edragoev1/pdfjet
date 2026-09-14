/*
 * PNGImage.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
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

    byte[] deflatedImageData;   // The deflated image data
    byte[] deflatedAlphaData;   // The deflated alpha channel data

    private byte bitDepth = 8;
    private byte colorType = 0;

    /**
     * Used to embed PNG images in the PDF document.
     *
     * @param inputStream the inputStream.
     * @throws Exception  If an input or output exception occurred.
     */
    PNGImage(InputStream inputStream) throws Exception {
        validatePNG(inputStream);

        List<Chunk> chunks = processPNG(inputStream);
        for (Chunk chunk : chunks) {
            String chunkType = new String(chunk.type);
            if (chunkType.equals("IHDR")) {
                if (chunk.getData().length != 13) {
                    throw new Exception("Invalid PNG IHDR chunk.");
                }
                this.w = toIntValue(chunk.getData(), 0);    // Width
                this.h = toIntValue(chunk.getData(), 4);    // Height
                this.bitDepth = chunk.getData()[8];         // Bit Depth
                this.colorType = chunk.getData()[9];        // Color Type
                if (chunk.getData()[12] == 1) {
                    throw new Exception("Interlaced PNG images are not supported.\n" +
                            "Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png");
                }
            } else if (chunkType.equals("IDAT")) {
                iDAT = appendIdatChunk(iDAT, chunk.getData());
            } else if (chunkType.equals("PLTE")) {
                pLTE = chunk.getData();
                if (pLTE.length % 3 != 0) {
                    throw new Exception("Incorrect palette length.");
                }
            } else if (chunkType.equals("tRNS")) {
                if (colorType == 3) {
                    tRNS = chunk.getData();
                }
            }
            // The gAMA, cHRM, sBIT and bKGD chunks are ignored, in all four
            // ports: the samples are embedded as they are.
        }

        long imageDataLength = getImageDataLength();
        if (iDAT == null) {
            throw new Exception("The PNG image has no image data.");
        }
        // The rows of the image; data after the last row is ignored.
        byte[] inflatedImageData = Decompressor.inflatePrefix(iDAT, (int) imageDataLength);
        if (inflatedImageData.length < imageDataLength) {
            throw new Exception("The PNG image data is shorter than the image.");
        }
        byte[] image;
        if (colorType == 0) {
            // Grayscale Image
            if (bitDepth == 16) {
                image = getImageColorType0BitDepth16(inflatedImageData);
            } else if (bitDepth == 8) {
                image = getImageColorType0BitDepth8(inflatedImageData);
            } else if (bitDepth == 4) {
                image = getImageColorType0BitDepth4(inflatedImageData);
            } else if (bitDepth == 2) {
                image = getImageColorType0BitDepth2(inflatedImageData);
            } else if (bitDepth == 1) {
                image = getImageColorType0BitDepth1(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 4) {
            // Grayscale image with alpha
            if (bitDepth == 8) {
                image = getImageColorType4BitDepth8(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 6) {
            if (bitDepth == 8) {
                image = getImageColorType6BitDepth8(inflatedImageData);
            } else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        } else if (colorType == 2) {
            // True color image; a PLTE chunk in it is only a suggested palette
            if (bitDepth == 16) {
                image = getImageColorType2BitDepth16(inflatedImageData);
            } else {
                image = getImageColorType2BitDepth8(inflatedImageData);
            }
        } else {
            // Indexed image
            image = getImageColorType3(inflatedImageData);
        }

        deflatedImageData = Compressor.deflate(image);
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
     * Returns the image data.
     *
     * @return the image data.
     */
    public byte[] getData() {
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
            if ((new String(chunk.type)).equals("IEND")) {
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

    // Truecolor Image with Bit Depth == 16
    private byte[] getImageColorType2BitDepth16(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 6 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            } else {
                image[k++] = buf[i];
            }
        }
        applyFilters(filters, image, this.w, this.h, 6);
        return image;
    }

    // Truecolor Image with Bit Depth == 8
    private byte[] getImageColorType2BitDepth8(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 3 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            } else {
                image[k++] = buf[i];
            }
        }
        applyFilters(filters, image, this.w, this.h, 3);
        return image;
    }

    // Truecolor Image with Alpha Transparency
    // The gray samples are the image and the alpha samples its soft mask.
    private byte[] getImageColorType4BitDepth8(byte[] buf) {
        byte[] gray = new byte[this.w * this.h];
        byte[] alpha = new byte[this.w * this.h];
        byte[] image = new byte[2 * this.w * this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 2 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            } else {
                image[k++] = buf[i];
            }
        }
        applyFilters(filters, image, this.w, this.h, 2);
        for (int i = 0; i < gray.length; i++) {
            gray[i] = image[2 * i];
            alpha[i] = image[2 * i + 1];
        }
        deflatedAlphaData = Compressor.deflate(alpha);
        return gray;
    }

    private byte[] getImageColorType6BitDepth8(byte[] buf) {
        byte[] idata = new byte[3 * this.w * this.h];   // Image data
        byte[] alpha = new byte[this.w * this.h];       // Alpha values
        byte[] image = new byte[4 * this.w * this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 4 * this.w + 1;
        int k = 0;
        int j = 0;
        int i = 0;
        for (; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            } else {
                image[k++] = buf[i];
            }
        }
        applyFilters(filters, image, this.w, this.h, 4);
        k = 0;
        j = 0;
        i = 0;
        while (i < image.length) {
            idata[j++] = image[i++];
            idata[j++] = image[i++];
            idata[j++] = image[i++];
            alpha[k++] = image[i++];
        }
        deflatedAlphaData = Compressor.deflate(alpha);
        return idata;
    }

    // Indexed-color image with bit depth == 1, 2, 4 or 8
    // Each value is a palette index; a PLTE chunk shall appear.
    // The filters are undone on the packed indexes, one byte per pixel whatever
    // the bit depth, before the indexes are looked up in the palette.
    private byte[] getImageColorType3(byte[] buf) {
        int bytesPerLine = (this.w * this.bitDepth + 7) / 8;
        byte[] indexes = new byte[bytesPerLine * this.h];
        byte[] filters = new byte[this.h];
        for (int row = 0; row < this.h; row++) {
            int offset = row * (bytesPerLine + 1);
            filters[row] = buf[offset];
            System.arraycopy(buf, offset + 1, indexes, row * bytesPerLine, bytesPerLine);
        }
        applyFilters(filters, indexes, bytesPerLine, this.h, 1);

        byte[] image = new byte[3 * (this.w * this.h)];
        byte[] alpha = null;
        if (tRNS != null) {
            alpha = new byte[this.w * this.h];
            Arrays.fill(alpha, (byte) 0xff);
        }
        int mask = (1 << this.bitDepth) - 1;
        int n = 0;
        int j = 0;
        for (int row = 0; row < this.h; row++) {
            for (int col = 0; col < this.w; col++) {
                int bit = col * this.bitDepth;
                int b = indexes[row * bytesPerLine + bit / 8] & 0xff;
                int k = (b >> (8 - this.bitDepth - bit % 8)) & mask;
                if (tRNS != null && k < tRNS.length) {
                    alpha[n] = tRNS[k];
                }
                n++;
                image[j++] = pLTE[3*k];
                image[j++] = pLTE[3*k + 1];
                image[j++] = pLTE[3*k + 2];
            }
        }
        if (tRNS != null) {
            deflatedAlphaData = Compressor.deflate(alpha);
        }

        return image;
    }

    // Grayscale Image with Bit Depth == 16
    private byte[] getImageColorType0BitDepth16(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 2 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j] = buf[i];
                j += 1;
            } else {
                image[k] = buf[i];
                k += 1;
            }
        }
        applyFilters(filters, image, this.w, this.h, 2);
        return image;
    }

    // Grayscale Image with Bit Depth == 8
    private byte[] getImageColorType0BitDepth8(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            } else {
                image[k++] = buf[i];
            }
        }
        applyFilters(filters, image, this.w, this.h, 1);
        return image;
    }

    // Grayscale Image with Bit Depth == 4
    private byte[] getImageColorType0BitDepth4(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = this.w / 2 + 1;
        if (this.w % 2 > 0) {
            bytesPerLine += 1;
        }
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[k++] = buf[i];
            } else {
                image[j++] = buf[i];
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(filters, image, bytesPerLine - 1, this.h, 1);
        return image;
    }

    // Grayscale Image with Bit Depth == 2
    private byte[] getImageColorType0BitDepth2(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = this.w / 4 + 1;
        if (this.w % 4 > 0) {
            bytesPerLine += 1;
        }
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[k++] = buf[i];
            } else {
                image[j++] = buf[i];
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(filters, image, bytesPerLine - 1, this.h, 1);
        return image;
    }

    // Grayscale Image with Bit Depth == 1
    private byte[] getImageColorType0BitDepth1(byte[] buf) {
        byte[] image = new byte[buf.length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = this.w / 8 + 1;
        if (this.w % 8 > 0) {
            bytesPerLine += 1;
        }
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.length; i++) {
            if (i % bytesPerLine == 0) {
                filters[k++] = buf[i];
            } else {
                image[j++] = buf[i];
            }
        }
        // The filters work on bytes, one byte per pixel whatever the bit depth.
        applyFilters(filters, image, bytesPerLine - 1, this.h, 1);
        return image;
    }

    private void applyFilters(
            byte[] filters,
            byte[] image,
            int width,
            int height,
            int bytesPerPixel) {
        int bytesPerLine = width * bytesPerPixel;
        byte filter = 0x00;
        for (int row = 0; row < height; row++) {
            for (int col = 0; col < bytesPerLine; col++) {
                if (col == 0) {
                    filter = filters[row];
                }
                if (filter == 0x00) {           // None
                    continue;
                }

                int a = 0;                      // The pixel on the left
                if (col >= bytesPerPixel) {
                    a = image[(bytesPerLine * row + col) - bytesPerPixel] & 0xff;
                }
                int b = 0;                      // The pixel above
                if (row > 0) {
                    b = image[bytesPerLine * (row - 1) + col] & 0xff;
                }
                int c = 0;                      // The pixel diagonally left above
                if (col >= bytesPerPixel && row > 0) {
                    c = image[(bytesPerLine * (row - 1) + col) - bytesPerPixel] & 0xff;
                }

                int index = bytesPerLine * row + col;
                if (filter == 0x01) {           // Sub
                    image[index] += (byte) a;
                } else if (filter == 0x02) {      // Up
                    image[index] += (byte) b;
                } else if (filter == 0x03) {      // Average
                    image[index] += (byte) Math.floor((a + b) / 2.0);
                } else if (filter == 0x04) {      // Paeth
                    int p = a + b - c;
                    int pa = Math.abs(p - a);
                    int pb = Math.abs(p - b);
                    int pc = Math.abs(p - c);
                    if (pa <= pb && pa <= pc) {
                        image[index] += (byte) a;
                    } else if (pb <= pc) {
                        image[index] += (byte) b;
                    } else {
                        image[index] += (byte) c;
                    }
                }
            }
        }
    }

    private byte[] appendIdatChunk(byte[] array1, byte[] array2) {
        if (array1 == null) {
            return array2;
        } else if (array2 == null) {
            return array1;
        }
        byte[] joinedArray = new byte[array1.length + array2.length];
        System.arraycopy(array1, 0, joinedArray, 0, array1.length);
        System.arraycopy(array2, 0, joinedArray, array1.length, array2.length);
        return joinedArray;
    }
/*
    public static void main(String[] args) throws Exception {
        FileInputStream fis = new FileInputStream(args[0]);
        PNGImage png = new PNGImage(fis);
        byte[] image = png.getData();
        byte[] alpha = png.getAlpha();
        int w = png.getWidth();
        int h = png.getHeight();
        int c = png.getColorType();
        fis.close();

        String fileName = args[0].substring(0, args[0].lastIndexOf("."));
        FileOutputStream fos = new FileOutputStream(fileName + ".jet");
        BufferedOutputStream bos = new BufferedOutputStream(fos);
        writeInt(w, bos);   // Width
        writeInt(h, bos);   // Height
        bos.write(c);       // Color Space
        if (alpha != null) {
            bos.write(1);
            writeInt(alpha.length, bos);
            bos.write(alpha);
        } else {
            bos.write(0);
        }
        writeInt(image.length, bos);
        bos.write(image);
        bos.flush();
        bos.close();
    }

    private static void writeInt(int i, OutputStream os) throws IOException {
        os.write((i >> 24) & 0xff);
        os.write((i >> 16) & 0xff);
        os.write((i >>  8) & 0xff);
        os.write((i >>  0) & 0xff);
    }
*/
}   // End of PNGImage.java
