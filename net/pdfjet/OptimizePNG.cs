/**
 *  OptimizePNG.java
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

namespace PDFjet.NET {
public class OptimizePNG {

    public static void Main(String[] args) {
        String fileName = args[0];
        FileStream fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        PNGImage png = new PNGImage(fis);
        byte[] image = png.GetData();
        byte[] alpha = png.GetAlpha();
        int w = png.GetWidth();
        int h = png.GetHeight();
        int c = png.GetColorType();
        fis.Dispose();

        BufferedStream bos = new BufferedStream(
                new FileStream(fileName + ".stream", FileMode.Create));
        WriteInt(w, bos);           // Width
        WriteInt(h, bos);           // Height
        bos.WriteByte((byte) c);    // Color Space
        if (alpha != null) {
            bos.WriteByte((byte) 1);
            WriteInt(alpha.Length, bos);
            bos.Write(alpha, 0, alpha.Length);
        } else {
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

}   // End of OptimizePNG.cs
}   // End of namespace PDFjet.NET
