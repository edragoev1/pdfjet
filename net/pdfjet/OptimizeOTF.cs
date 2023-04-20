/**
 *  OptimizeOTF.java
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
using System.Text;

/**
 * This program optimizes .otf and .ttf fonts by converting them to
 * .otf.stream and .ttf.stream fonts that can be embedded much faster in PDF
 * that the original fonts.
 */
namespace PDFjet.NET {
public class OptimizeOTF {
    private static void ConvertFontFile(String fileName) {
        OTF otf = new OTF(new FileStream(fileName, FileMode.Open, FileAccess.Read));

        FileStream fos = new FileStream(fileName + ".stream", FileMode.Create);

        byte[] name = Encoding.UTF8.GetBytes(otf.fontName);
        fos.WriteByte((byte) name.Length);
        fos.Write(name, 0, name.Length);

        byte[] info = Encoding.UTF8.GetBytes(otf.fontInfo);
        WriteInt24(info.Length, fos);
        fos.Write(info, 0, info.Length);

        MemoryStream stream = new MemoryStream(32768);
        WriteInt32(otf.unitsPerEm, stream);
        WriteInt32(otf.bBoxLLx, stream);
        WriteInt32(otf.bBoxLLy, stream);
        WriteInt32(otf.bBoxURx, stream);
        WriteInt32(otf.bBoxURy, stream);
        WriteInt32(otf.ascent, stream);
        WriteInt32(otf.descent, stream);
        WriteInt32(otf.firstChar, stream);
        WriteInt32(otf.lastChar, stream);
        WriteInt32(otf.capHeight, stream);
        WriteInt32(otf.underlinePosition, stream);
        WriteInt32(otf.underlineThickness, stream);

        WriteInt32(otf.advanceWidth.Length, stream);
        for (int i = 0; i < otf.advanceWidth.Length; i++) {
            WriteInt16(otf.advanceWidth[i], stream);
        }

        WriteInt32(otf.glyphWidth.Length, stream);
        for (int i = 0; i < otf.glyphWidth.Length; i++) {
            WriteInt16(otf.glyphWidth[i], stream);
        }

        WriteInt32(otf.unicodeToGID.Length, stream);
        for (int i = 0; i < otf.unicodeToGID.Length; i++) {
            WriteInt16(otf.unicodeToGID[i], stream);
        }

        byte[] buf1 = stream.ToArray();
        MemoryStream buf2 = new MemoryStream(0xFFFF);
        DeflaterOutputStream dos1 = new DeflaterOutputStream(buf2);
        dos1.Write(buf1, 0, buf1.Length);

        WriteInt32((int) buf2.Length, fos);
        buf2.WriteTo(fos);

        byte[] buf3 = otf.buf;
        if (otf.cff == true) {
            fos.WriteByte((byte) 'Y');
            buf3 = new byte[otf.cffLen];
            for (int i = 0; i < otf.cffLen; i++) {
                buf3[i] = otf.buf[otf.cffOff + i];
            }
        }
        else {
            fos.WriteByte((byte) 'N');
        }

        MemoryStream buf4 = new MemoryStream(0xFFFF);
        DeflaterOutputStream dos2 = new DeflaterOutputStream(buf4);
        dos2.Write(buf3, 0, buf3.Length);

        WriteInt32(buf3.Length, fos);       // Uncompressed font size
        WriteInt32((int) buf4.Length, fos); // Compressed font size
        buf4.WriteTo(fos);

        fos.Close();
    }

    private static void WriteInt16(int i, Stream stream) {
        stream.WriteByte((byte) (i >>  8));
        stream.WriteByte((byte) i);
    }

    private static void WriteInt24(int i, Stream stream) {
        stream.WriteByte((byte) (i >> 16));
        stream.WriteByte((byte) (i >>  8));
        stream.WriteByte((byte) i);
    }

    private static void WriteInt32(int i, Stream stream) {
        stream.WriteByte((byte) (i >> 24));
        stream.WriteByte((byte) (i >> 16));
        stream.WriteByte((byte) (i >>  8));
        stream.WriteByte((byte) i);
    }

    public static void Main(String[] args) {
        FileAttributes attr = File.GetAttributes(args[0]);
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
        // if (attr.HasFlag(FileAttributes.Directory)) {    // v4 and higher
            String[] list = Directory.GetFiles(args[0]);
            foreach (String fileName in list) {
                if (fileName.EndsWith(".ttf") || fileName.EndsWith(".otf")) {
                    Console.WriteLine("Reading: " + fileName);
                    ConvertFontFile(fileName);
                    Console.WriteLine("Writing: " + fileName + ".stream");
                }
            }
        } else {
            Console.WriteLine("Reading: " + args[0]);
            ConvertFontFile(args[0]);
            Console.WriteLine("Writing: " + args[0] + ".stream");
        }
    }
}   // End of OptimizeOTF.cs
}   // End of namespace PDFjet.NET
