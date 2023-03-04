/**
 *  Image.cs
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


namespace PDFjet.NET {
/**
 *  Used to create image objects and draw them on a page.
 *  The image type can be one of the following: ImageType.JPG, ImageType.PNG, ImageType.BMP or ImageType.PNG_STREAM
 *
 *  Please see Example_03 and Example_24.
 */
public class Image : IDrawable {

    internal int objNumber;

    internal float x = 0f;  // Position of the image on the page
    internal float y = 0f;
    internal float w;       // Image width
    internal float h;       // Image height

    internal String uri;
    internal String key;

    private float xBox;
    private float yBox;

    private int degrees = 0;
    private bool flipUpsideDown = false;

    private String language = null;
    private String altDescription = Single.space;
    private String actualText = Single.space;


    /**
     *  The main constructor for the Image class.
     *
     *  @param pdf the page to draw this image on.
     *  @param inputStream the input stream to read the image from.
     *  @param imageType ImageType.JPG, ImageType.PNG or ImageType.BMP.
     *
     */
    public Image(PDF pdf, Stream inputStream, int imageType) {
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.GetData();
            w = jpg.GetWidth();
            h = jpg.GetHeight();
            if (jpg.GetColorComponents() == 1) {
                AddImage(pdf, data, null, imageType, "DeviceGray", 8);
            }
            else if (jpg.GetColorComponents() == 3) {
                AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
            }
            else if (jpg.GetColorComponents() == 4) {
                AddImage(pdf, data, null, imageType, "DeviceCMYK", 8);
            }
        }
        else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImage(pdf, data, null, imageType, "DeviceGray", png.GetBitDepth());
            }
            else {
                if (png.GetBitDepth() == 16) {
                    AddImage(pdf, data, null, imageType, "DeviceRGB", 16);
                }
                else {
                    AddImage(pdf, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
        }
        else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            w = bmp.GetWidth();
            h = bmp.GetHeight();
            AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
        }
        else if (imageType == ImageType.PNG_STREAM) {
            AddImage(pdf, inputStream);
        }

        inputStream.Dispose();
    }


    /**
     *  Constructor used to attach images to existing PDF.
     *
     *  @param pdf the page to draw this image on.
     *  @param inputStream the input stream to read the image from.
     *  @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
     *
     */
    public Image(List<PDFobj> objects, Stream inputStream, int imageType) {
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.GetData();
            w = jpg.GetWidth();
            h = jpg.GetHeight();
            if (jpg.GetColorComponents() == 1) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", 8);
            }
            else if (jpg.GetColorComponents() == 3) {
                AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
            }
            else if (jpg.GetColorComponents() == 4) {
                AddImageToObjects(objects, data, null, imageType, "DeviceCMYK", 8);
            }
        }
        else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", png.GetBitDepth());
            }
            else {
                if (png.GetBitDepth() == 16) {
                    AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 16);
                }
                else {
                    AddImageToObjects(objects, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
        }
        else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            w = bmp.GetWidth();
            h = bmp.GetHeight();
            AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
        }
        inputStream.Close();
    }


    // Creates new image from an existing PDF object
    public Image(PDF pdf, PDFobj obj) {
        w = float.Parse(obj.GetValue("/Width"));
        h = float.Parse(obj.GetValue("/Height"));
        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        pdf.Append("/Filter ");
        pdf.Append(obj.GetValue("/Filter"));
        pdf.Append("\n");
        pdf.Append("/Width ");
        pdf.Append(w);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append(h);
        pdf.Append('\n');
        String colorSpace = obj.GetValue("/ColorSpace");
        if (!colorSpace.Equals("")) {
            pdf.Append("/ColorSpace ");
            pdf.Append(colorSpace);
            pdf.Append("\n");
        }
        pdf.Append("/BitsPerComponent ");
        pdf.Append(obj.GetValue("/BitsPerComponent"));
        pdf.Append("\n");
        String decodeParms = obj.GetValue("/DecodeParms");
        if (!decodeParms.Equals("")) {
            pdf.Append("/DecodeParms ");
            pdf.Append(decodeParms);
            pdf.Append("\n");
        }
        String imageMask = obj.GetValue("/ImageMask");
        if (!imageMask.Equals("")) {
            pdf.Append("/ImageMask ");
            pdf.Append(imageMask);
            pdf.Append("\n");
        }
        pdf.Append("/Length ");
        pdf.Append(obj.stream.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(obj.stream, 0, obj.stream.Length);
        pdf.Append("\nendstream\n");
        pdf.Endobj();
        pdf.images.Add(this);
        objNumber = pdf.GetObjNumber();
    }


    /**
     *  Sets the position of this image on the page to (x, y).
     *
     *  @param x the x coordinate of the top left corner of the image.
     *  @param y the y coordinate of the top left corner of the image.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }


    /**
     *  Sets the position of this image on the page to (x, y).
     *
     *  @param x the x coordinate of the top left corner of the image.
     *  @param y the y coordinate of the top left corner of the image.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public Image SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }


    /**
     *  Sets the location of this image on the page to (x, y).
     *
     *  @param x the x coordinate of the top left corner of the image.
     *  @param y the y coordinate of the top left corner of the image.
     */
    public Image SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }


    /**
     *  Scales this image by the specified factor.
     *
     *  @param factor the factor used to scale the image.
     */
    public Image ScaleBy(double factor) {
        return this.ScaleBy((float) factor, (float) factor);
    }


    /**
     *  Scales this image by the specified factor.
     *
     *  @param factor the factor used to scale the image.
     */
    public Image ScaleBy(float factor) {
        return this.ScaleBy(factor, factor);
    }


    /**
     *  Scales this image by the specified width and height factor.
     *  <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
     *
     *  @param widthFactor the factor used to scale the width of the image
     *  @param heightFactor the factor used to scale the height of the image
     */
    public Image ScaleBy(float widthFactor, float heightFactor) {
        this.w *= widthFactor;
        this.h *= heightFactor;
        return this;
    }


    public Image ResizeWidth(float width) {
        float factor = width / GetWidth();
        return this.ScaleBy(factor, factor);
    }


    public Image ResizeHeight(float height) {
        float factor = height / GetHeight();
        return this.ScaleBy(factor, factor);
    }


    /**
     *  Places this image in the specified box.
     *
     *  @param box the specified box.
     */
    public void PlaceIn(Box box) {
        xBox = box.x;
        yBox = box.y;
    }


    /**
     *  Sets the URI for the "click box" action.
     *
     *  @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }


    /**
     *  Sets the destination key for the action.
     *
     *  @param key the destination name.
     */
    public void SetGoToAction(String key) {
        this.key = key;
    }


    /**
     *  Sets the rotate90 flag.
     *  When the flag is true the image is rotated 90 degrees clockwise.
     *
     *  @param rotate90 the flag.
     */
    public void SetRotateCW90(bool rotate90) {
        if (rotate90) {
            this.degrees = 90;
        }
        else {
            this.degrees = 0;
        }
    }


    /**
     *  Sets the image rotation to the specified number of degrees.
     *
     *  @param degrees the number of degrees.
     */
    public void SetRotate(int degrees) {
        this.degrees = degrees;
    }


    /**
     *  Sets the alternate description of this image.
     *
     *  @param altDescription the alternate description of the image.
     *  @return this Image.
     */
    public Image SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }


    /**
     *  Sets the actual text for this image.
     *
     *  @param actualText the actual text for the image.
     *  @return this Image.
     */
    public Image SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }


    /**
     *  Draws this image on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);

        x += xBox;
        y += yBox;
        page.Append("q\n");

        if (degrees == 0) {
            page.Append(w);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(h);
            page.Append(' ');
            page.Append(x);
            page.Append(' ');
            page.Append(page.height - (y + h));
            page.Append(" cm\n");
        }
        else if (degrees == 90) {
            page.Append(h);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(w);
            page.Append(' ');
            page.Append(x);
            page.Append(' ');
            page.Append(page.height - y);
            page.Append(" cm\n");
            page.Append("0 -1 1 0 0 0 cm\n");
        }
        else if (degrees == 180) {
            page.Append(w);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(h);
            page.Append(' ');
            page.Append(x + w);
            page.Append(' ');
            page.Append(page.height - y);
            page.Append(" cm\n");
            page.Append("-1 0 0 -1 0 0 cm\n");
        }
        else if (degrees == 270) {
            page.Append(h);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(0f);
            page.Append(' ');
            page.Append(w);
            page.Append(' ');
            page.Append(x + h);
            page.Append(' ');
            page.Append(page.height - (y + w));
            page.Append(" cm\n");
            page.Append("0 1 -1 0 0 0 cm\n");
        }

        if (flipUpsideDown) {
            page.Append("1 0 0 -1 0 0 cm\n");
        }

        page.Append("/Im");
        page.Append(objNumber);
        page.Append(" Do\n");
        page.Append("Q\n");

        page.AddEMC();

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] {x + w, y + h};
    }


    /**
     *  Returns the width of this image when drawn on the page.
     *  The scaling is take into account.
     *
     *  @return w - the width of this image.
     */
    public float GetWidth() {
        return this.w;
    }


    /**
     *  Returns the height of this image when drawn on the page.
     *  The scaling is take into account.
     *
     *  @return h - the height of this image.
     */
    public float GetHeight() {
        return this.h;
    }


    private void AddSoftMask(
            PDF pdf,
            byte[] data,
            String colorSpace,
            int bitsPerComponent) {
        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        pdf.Append("/Filter /FlateDecode\n");
        pdf.Append("/Width ");
        pdf.Append((int) w);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append((int) h);
        pdf.Append('\n');
        pdf.Append("/ColorSpace /");
        pdf.Append(colorSpace);
        pdf.Append('\n');
        pdf.Append("/BitsPerComponent ");
        pdf.Append(bitsPerComponent);
        pdf.Append('\n');
        pdf.Append("/Length ");
        pdf.Append(data.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(data, 0, data.Length);
        pdf.Append("\nendstream\n");
        pdf.Endobj();
        objNumber = pdf.GetObjNumber();
    }


    private void AddImage(
            PDF pdf,
            byte[] data,
            byte[] alpha,
            int imageType,
            String colorSpace,
            int bitsPerComponent) {
        if (alpha != null) {
            AddSoftMask(pdf, alpha, "DeviceGray", bitsPerComponent);
        }
        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        if (imageType == ImageType.JPG) {
            pdf.Append("/Filter /DCTDecode\n");
        }
        else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
            pdf.Append("/Filter /FlateDecode\n");
            if (alpha != null) {
                pdf.Append("/SMask ");
                pdf.Append(objNumber);
                pdf.Append(" 0 R\n");
            }
        }
        pdf.Append("/Width ");
        pdf.Append((int) w);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append((int) h);
        pdf.Append('\n');
        pdf.Append("/ColorSpace /");
        pdf.Append(colorSpace);
        pdf.Append('\n');
        pdf.Append("/BitsPerComponent ");
        pdf.Append(bitsPerComponent);
        pdf.Append('\n');
        if (colorSpace.Equals("DeviceCMYK")) {
            // If the image was created with Photoshop - invert the colors:
            pdf.Append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n");
        }
        pdf.Append("/Length ");
        pdf.Append(data.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(data, 0, data.Length);
        pdf.Append("\nendstream\n");
        pdf.Endobj();
        pdf.images.Add(this);
        objNumber = pdf.GetObjNumber();
    }


    private void AddImage(PDF pdf, Stream inputStream) {

        w = GetInt(inputStream);                // Width
        h = GetInt(inputStream);                // Height
        byte c = (byte) inputStream.ReadByte(); // Color Space
        byte a = (byte) inputStream.ReadByte(); // Alpha

        if (a != 0) {
            pdf.Newobj();
            pdf.Append("<<\n");
            pdf.Append("/Type /XObject\n");
            pdf.Append("/Subtype /Image\n");
            pdf.Append("/Filter /FlateDecode\n");
            pdf.Append("/Width ");
            pdf.Append(w);
            pdf.Append('\n');
            pdf.Append("/Height ");
            pdf.Append(h);
            pdf.Append('\n');
            pdf.Append("/ColorSpace /DeviceGray\n");
            pdf.Append("/BitsPerComponent 8\n");
            int length = GetInt(inputStream);
            pdf.Append("/Length ");
            pdf.Append(length);
            pdf.Append('\n');
            pdf.Append(">>\n");
            pdf.Append("stream\n");
            byte[] buf1 = new byte[length];
            inputStream.Read(buf1, 0, length);
            pdf.Append(buf1, 0, length);
            pdf.Append("\nendstream\n");
            pdf.Endobj();
            objNumber = pdf.GetObjNumber();
        }

        pdf.Newobj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        pdf.Append("/Filter /FlateDecode\n");
        if (a != 0) {
            pdf.Append("/SMask ");
            pdf.Append(objNumber);
            pdf.Append(" 0 R\n");
        }
        pdf.Append("/Width ");
        pdf.Append(w);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append(h);
        pdf.Append('\n');
        pdf.Append("/ColorSpace /");
        if (c == 1) {
            pdf.Append("DeviceGray");
        }
        else if (c == 3 || c == 6) {
            pdf.Append("DeviceRGB");
        }
        pdf.Append('\n');
        pdf.Append("/BitsPerComponent 8\n");
        pdf.Append("/Length ");
        pdf.Append(GetInt(inputStream));
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        byte[] buf2 = new byte[4096];
        int count;
        while ((count = inputStream.Read(buf2, 0, buf2.Length)) > 0) {
            pdf.Append(buf2, 0, count);
        }
        pdf.Append("\nendstream\n");
        pdf.Endobj();
        pdf.images.Add(this);
        objNumber = pdf.GetObjNumber();
    }


    private int GetInt(Stream inputStream) {
        byte[] buf = new byte[4];
        inputStream.Read(buf, 0, 4);
        int val = 0;
        val |= buf[0] & 0xff;
        val <<= 8;
        val |= buf[1] & 0xff;
        val <<= 8;
        val |= buf[2] & 0xff;
        val <<= 8;
        val |= buf[3] & 0xff;
        return val;
    }


    private void AddSoftMask(
            List<PDFobj> objects,
            byte[] data,
            String colorSpace,
            int bitsPerComponent) {
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/XObject");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Image");
        obj.dict.Add("/Filter");
        obj.dict.Add("/FlateDecode");
        obj.dict.Add("/Width");
        obj.dict.Add(((int) w).ToString());
        obj.dict.Add("/Height");
        obj.dict.Add(((int) h).ToString());
        obj.dict.Add("/ColorSpace");
        obj.dict.Add("/" + colorSpace);
        obj.dict.Add("/BitsPerComponent");
        obj.dict.Add(bitsPerComponent.ToString());
        obj.dict.Add("/Length");
        obj.dict.Add(data.Length.ToString());
        obj.dict.Add(">>");
        obj.SetStream(data);
        obj.number = objects.Count + 1;
        objects.Add(obj);
        objNumber = obj.number;
    }


    private void AddImageToObjects(
            List<PDFobj> objects,
            byte[] data,
            byte[] alpha,
            int imageType,
            String colorSpace,
            int bitsPerComponent) {
        if (alpha != null) {
            AddSoftMask(objects, alpha, "DeviceGray", bitsPerComponent);
        }
        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/XObject");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Image");
        if (imageType == ImageType.JPG) {
            obj.dict.Add("/Filter");
            obj.dict.Add("/DCTDecode");
        }
        else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
            obj.dict.Add("/Filter");
            obj.dict.Add("/FlateDecode");
            if (alpha != null) {
                obj.dict.Add("/SMask");
                obj.dict.Add(objNumber.ToString());
                obj.dict.Add("0");
                obj.dict.Add("R");
            }
        }
        obj.dict.Add("/Width");
        obj.dict.Add(((int) w).ToString());
        obj.dict.Add("/Height");
        obj.dict.Add(((int) h).ToString());
        obj.dict.Add("/ColorSpace");
        obj.dict.Add("/" + colorSpace);
        obj.dict.Add("/BitsPerComponent");
        obj.dict.Add(bitsPerComponent.ToString());
        if (colorSpace.Equals("DeviceCMYK")) {
            // If the image was created with Photoshop - invert the colors:
            obj.dict.Add("/Decode");
            obj.dict.Add("[");
            obj.dict.Add("1.0");
            obj.dict.Add("0.0");
            obj.dict.Add("1.0");
            obj.dict.Add("0.0");
            obj.dict.Add("1.0");
            obj.dict.Add("0.0");
            obj.dict.Add("1.0");
            obj.dict.Add("0.0");
            obj.dict.Add("]");
        }
        obj.dict.Add("/Length");
        obj.dict.Add(data.Length.ToString());
        obj.dict.Add(">>");
        obj.SetStream(data);
        obj.number = objects.Count + 1;
        objects.Add(obj);
        objNumber = obj.number;
    }


    public void ResizeToFit(Page page, bool keepAspectRatio) {
        if (keepAspectRatio) {
            this.ScaleBy(Math.Min((page.width - x)/w, (page.height - y)/h));
        }
        else {
            this.ScaleBy((page.width - x)/w, (page.height - y)/h);
        }
    }


    public void FlipUpsideDown(bool flipUpsideDown) {
        this.flipUpsideDown = flipUpsideDown;
    }

}   // End of Image.cs
}   // End of namespace PDFjet.NET
