/**
 *  PNGImage.cs
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.IO;
using System.Collections.Generic;


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
namespace PDFjet.NET {
public class PNGImage {

    int w = 0;                  // Image width in pixels
    int h = 0;                  // Image height in pixels

    byte[] iDAT;                // The compressed data in the IDAT chunk
    byte[] pLTE;                // The palette data
    byte[] tRNS;                // The alpha for the palette data

    byte[] deflatedImageData;   // The deflated reconstructed image data
    byte[] deflatedAlphaData;   // The deflated alpha channel data

    private byte bitDepth = 8;
    private int colorType = 0;


    /**
     * Used to embed PNG images in the PDF document.
     *
     */
    public PNGImage(Stream inputStream) {
        ValidatePNG(inputStream);

        List<Chunk> chunks = ProcessPNG(inputStream);
        for (int i = 0; i < chunks.Count; i++) {
            Chunk chunk = chunks[i];
            String chunkType = System.Text.Encoding.UTF8.GetString(chunk.type);
            if (chunkType.Equals("IHDR")) {
                this.w = (int) ToUInt32(chunk.GetData(), 0);    // Width
                this.h = (int) ToUInt32(chunk.GetData(), 4);    // Height
                this.bitDepth = chunk.GetData()[8];             // Bit Depth
                this.colorType = chunk.GetData()[9];            // Color Type

                // Console.WriteLine(
                //         "Bit Depth == " + chunk.GetData()[8]);
                // Console.WriteLine(
                //         "Color Type == " + chunk.GetData()[9]);
                // Console.WriteLine(chunk.GetData()[10]);
                // Console.WriteLine(chunk.GetData()[11]);
                // Console.WriteLine(chunk.GetData()[12]);

                if (chunk.GetData()[12] == 1) {
                    Console.WriteLine("Interlaced PNG images are not supported.");
                    Console.WriteLine("Convert the image using OptiPNG:\noptipng -i0 -o7 myimage.png\n");
                }
            }
            else if (chunkType.Equals("IDAT")) {
                iDAT = AppendIdatChunk(iDAT, chunk.GetData());
            }
            else if (chunkType.Equals("PLTE")) {
                pLTE = chunk.GetData();
                if (pLTE.Length % 3 != 0) {
                    throw new Exception("Incorrect palette length.");
                }
            }
            else if (chunkType.Equals("gAMA")) {
                // TODO:
                // Console.WriteLine("gAMA chunk found!");
            }
            else if (chunkType.Equals("tRNS")) {
                // Console.WriteLine("tRNS chunk found!");
                if (colorType == 3) {
                    tRNS = chunk.GetData();
                }
            }
            else if (chunkType.Equals("cHRM")) {
                // TODO:
                // Console.WriteLine("cHRM chunk found!");
            }
            else if (chunkType.Equals("sBIT")) {
                // TODO:
                // Console.WriteLine("sBIT chunk found!");
            }
            else if (chunkType.Equals("bKGD")) {
                // TODO:
                // Console.WriteLine("bKGD chunk found!");
            }
        }

        byte[] inflatedImageData = Decompressor.inflate(iDAT);

        byte[] imageData;
        if (colorType == 0) {
            // Grayscale Image
            if (bitDepth == 16) {
                imageData = GetImageColorType0BitDepth16(inflatedImageData);
            }
            else if (bitDepth == 8) {
                imageData = GetImageColorType0BitDepth8(inflatedImageData);
            }
            else if (bitDepth == 4) {
                imageData = GetImageColorType0BitDepth4(inflatedImageData);
            }
            else if (bitDepth == 2) {
                imageData = GetImageColorType0BitDepth2(inflatedImageData);
            }
            else if (bitDepth == 1) {
                imageData = GetImageColorType0BitDepth1(inflatedImageData);
            }
            else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        }
        else if (colorType == 6) {
            if (bitDepth == 8) {
                imageData = GetImageColorType6BitDepth8(inflatedImageData);
            }
            else {
                throw new Exception("Image with unsupported bit depth == " + bitDepth);
            }
        }
        else {
            // Color Image
            if (pLTE == null) {
                // Trucolor Image
                if (bitDepth == 16) {
                    imageData = GetImageColorType2BitDepth16(inflatedImageData);
                }
                else {
                    imageData = GetImageColorType2BitDepth8(inflatedImageData);
                }
            }
            else {
                // Indexed Image
                if (bitDepth == 8) {
                    imageData = GetImageColorType3BitDepth8(inflatedImageData);
                }
                else if (bitDepth == 4) {
                    imageData = GetImageColorType3BitDepth4(inflatedImageData);
                }
                else if (bitDepth == 2) {
                    imageData = GetImageColorType3BitDepth2(inflatedImageData);
                }
                else if (bitDepth == 1) {
                    imageData = GetImageColorType3BitDepth1(inflatedImageData);
                }
                else {
                    throw new Exception("Image with unsupported bit depth == " + bitDepth);
                }
            }
        }

        deflatedImageData = Compressor.deflate(imageData);
    }


    public int GetWidth() {
        return this.w;
    }


    public int GetHeight() {
        return this.h;
    }


    public int GetColorType() {
        return this.colorType;
    }


    public int GetBitDepth() {
        return this.bitDepth;
    }


    public byte[] GetData() {
        return this.deflatedImageData;
    }


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


    private void ValidatePNG(Stream inputStream) {
        byte[] buf = new byte[8];
        if (inputStream.Read(buf, 0, buf.Length) == -1) {
            throw new Exception("File is too short!");
        }
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


    private byte[] GetNBytes(System.IO.Stream inputStream, UInt32 n) {
        byte[] buf = new byte[(int) n];
        if (inputStream.Read(buf, 0, buf.Length) == -1) {
            throw new Exception("Error reading input stream!");
        }
        return buf;
    }


    private UInt32 ToUInt32(byte[] buf, int off) {
        return ((UInt32) buf[off]) << 24 |
                ((UInt32) buf[off + 1]) << 16 |
                ((UInt32) buf[off + 2]) << 8 |
                ((UInt32) buf[off + 3]);
    }


    // Truecolor Image with Bit Depth == 16
    private byte[] GetImageColorType2BitDepth16(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 6 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            }
            else {
                image[k++] = buf[i];
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 6);
        return image;
    }


    // Truecolor Image with Bit Depth == 8
    private byte[] GetImageColorType2BitDepth8(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 3 * this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            }
            else {
                image[k++] = buf[i];
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 3);
        return image;
    }


    // Truecolor Image with Alpha Transparency
    private byte[] GetImageColorType6BitDepth8(byte[] buf) {
        byte[] idata = new byte[3 * this.w * this.h];   // Image data
        byte[] alpha = new byte[this.w * this.h];       // Alpha values

        byte[] image = new byte[4 * this.w * this.h];
        byte[] filters = new byte[this.h];
        int bytesPerLine = 4 * this.w + 1;
        int k = 0;
        int j = 0;
        int i = 0;
        for (; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            }
            else {
                image[k++] = buf[i];
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 4);

        k = 0;
        j = 0;
        i = 0;
        while (i < image.Length) {
            idata[j++] = image[i++];
            idata[j++] = image[i++];
            idata[j++] = image[i++];
            alpha[k++] = image[i++];
        }
        deflatedAlphaData = Compressor.deflate(alpha);

        return idata;
    }


    // Indexed Image with Bit Depth == 8
    private byte[] GetImageColorType3BitDepth8(byte[] buf) {
        byte[] image = new byte[3 * (this.w * this.h)];

        byte[] filters = new byte[this.h];
        byte[] alpha = null;
        if (tRNS != null) {
            alpha = new byte[this.w * this.h];
            for (int i = 0; i < alpha.Length; i++) {
                alpha[i] = (byte) 0xff;
            }
        }

        int bytesPerLine = this.w + 1;
        int m = 0;
        int n = 0;
        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[m++] = buf[i];
            }
            else {
                int k = ((int) buf[i]) & 0xff;
                if (tRNS != null && k < tRNS.Length) {
                    alpha[n] = tRNS[k];
                }
                n++;
                image[j++] = pLTE[3*k];
                image[j++] = pLTE[3*k + 1];
                image[j++] = pLTE[3*k + 2];
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 3);

        if (tRNS != null) {
            deflatedAlphaData = Compressor.deflate(alpha);
        }

        return image;
    }


    // Indexed Image with Bit Depth == 4
    private byte[] GetImageColorType3BitDepth4(byte[] buf) {
        byte[] image = new byte[6 * (buf.Length - this.h)];

        int bytesPerLine = this.w / 2 + 1;
        if (this.w % 2 > 0) {
            bytesPerLine += 1;
        }

        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                // Skip the filter byte.
                continue;
            }

            int l = (int) buf[i];

            k = 3 * ((l >> 4) & 0x0000000f);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 0) & 0x0000000f);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];
        }

        return image;
    }


    // Indexed Image with Bit Depth == 2
    private byte[] GetImageColorType3BitDepth2(byte[] buf) {
        byte[] image = new byte[12 * (buf.Length - this.h)];

        int bytesPerLine = this.w / 4 + 1;
        if (this.w % 4 > 0) {
            bytesPerLine += 1;
        }

        int j = 0;
        int k = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                // Skip the filter byte.
                continue;
            }

            int l = (int) buf[i];

            k = 3 * ((l >> 6) & 0x00000003);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 4) & 0x00000003);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 2) & 0x00000003);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 0) & 0x00000003);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];
        }

        return image;
    }


    // Indexed Image with Bit Depth == 1
    private byte[] GetImageColorType3BitDepth1(byte[] buf) {
        byte[] image = new byte[24 * (buf.Length - this.h)];

        int bytesPerLine = this.w / 8 + 1;
        if (this.w % 8 > 0) {
            bytesPerLine += 1;
        }

        int j = 0;
        int k = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                // Skip the filter byte.
                continue;
            }

            int l = (int) buf[i];

            k = 3 * ((l >> 7) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 6) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 5) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 4) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 3) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 2) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 1) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];

            if (j % (3 * this.w) == 0) continue;

            k = 3 * ((l >> 0) & 0x00000001);
            image[j++] = pLTE[k];
            image[j++] = pLTE[k + 1];
            image[j++] = pLTE[k + 2];
        }

        return image;
    }


    // Grayscale Image with Bit Depth == 16
    private byte[] GetImageColorType0BitDepth16(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];

        byte[] filters = new byte[this.h];
        int bytesPerLine = 2 * this.w + 1;
        int k = 0;
        var j = 0;
        for (int i  = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j] = buf[i];
                j += 1;
            }
            else {
                image[k] = buf[i];
                k += 1;
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 2);

        return image;
    }


    // Grayscale Image with Bit Depth == 8
    private byte[] GetImageColorType0BitDepth8(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];

        byte[] filters = new byte[this.h];
        int bytesPerLine = this.w + 1;
        int k = 0;
        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine == 0) {
                filters[j++] = buf[i];
            }
            else {
                image[k++] = buf[i];
            }
        }
        ApplyFilters(filters, image, this.w, this.h, 1);

        return image;
    }


    // Grayscale Image with Bit Depth == 4
    private byte[] GetImageColorType0BitDepth4(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];

        int bytesPerLine = this.w / 2 + 1;
        if (this.w % 2 > 0) {
            bytesPerLine += 1;
        }

        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine != 0) {
                image[j++] = buf[i];
            }
        }

        return image;
    }


    // Grayscale Image with Bit Depth == 2
    private byte[] GetImageColorType0BitDepth2(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];

        int bytesPerLine = this.w / 4 + 1;
        if (this.w % 4 > 0) {
            bytesPerLine += 1;
        }

        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine != 0) {
                image[j++] = buf[i];
            }
        }

        return image;
    }


    // Grayscale Image with Bit Depth == 1
    private byte[] GetImageColorType0BitDepth1(byte[] buf) {
        byte[] image = new byte[buf.Length - this.h];

        int bytesPerLine = this.w / 8 + 1;
        if (this.w % 8 > 0) {
            bytesPerLine += 1;
        }

        int j = 0;
        for (int i = 0; i < buf.Length; i++) {
            if (i % bytesPerLine != 0) {
                image[j++] = buf[i];
            }
        }

        return image;
    }

    private void ApplyFilters(
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
                }
                else if (filter == 0x02) {      // Up
                    image[index] += (byte) b;
                }
                else if (filter == 0x03) {      // Average
                    image[index] += (byte) Math.Floor((a + b) / 2.0);
                }
                else if (filter == 0x04) {      // Paeth
                    int p = a + b - c;
                    int pa = Math.Abs(p - a);
                    int pb = Math.Abs(p - b);
                    int pc = Math.Abs(p - c);
                    if (pa <= pb && pa <= pc) {
                        image[index] += (byte) a;
                    }
                    else if (pb <= pc) {
                        image[index] += (byte) b;
                    }
                    else {
                        image[index] += (byte) c;
                    }
                }
            }
        }
    }


    private byte[] AppendIdatChunk(byte[] array1, byte[] array2) {
        if (array1 == null) {
            return array2;
        } else if (array2 == null) {
            return array1;
        }
        byte[] joinedArray = new byte[array1.Length + array2.Length];
        Array.Copy(array1, 0, joinedArray, 0, array1.Length);
        Array.Copy(array2, 0, joinedArray, array1.Length, array2.Length);
        return joinedArray;
    }

/*
    public static void Main(String[] args) {
        FileStream fis = new FileStream(args[0], FileMode.Open, FileAccess.Read);
        PNGImage png = new PNGImage(fis);
        byte[] image = png.GetData();
        byte[] alpha = png.GetAlpha();
        int w = png.GetWidth();
        int h = png.GetHeight();
        int c = png.GetColorType();
        fis.Dispose();

        String fileName = args[0].Substring(0, args[0].LastIndexOf("."));
        FileStream fos = new FileStream(fileName + ".jet", FileMode.Create);
        BufferedStream bos = new BufferedStream(fos);
        WriteInt(w, bos);           // Width
        WriteInt(h, bos);           // Height
        bos.WriteByte((byte) c);    // Color Space
        if (alpha != null) {
            bos.WriteByte((byte) 1);
            WriteInt(alpha.Length, bos);
            bos.Write(alpha, 0, alpha.Length);
        }
        else {
            bos.WriteByte((byte) 0);
        }
        WriteInt(image.Length, bos);
        bos.Write(image, 0, image.Length);
        bos.Flush();
        bos.Dispose();
    }


    private static void WriteInt(int i, Stream os) {
        os.WriteByte((byte) (i >> 24));
        os.WriteByte((byte) (i >> 16));
        os.WriteByte((byte) (i >>  8));
        os.WriteByte((byte) (i >>  0));
    }
*/

}   // End of PNGImage.cs
}   // End of namespace PDFjet.NET
