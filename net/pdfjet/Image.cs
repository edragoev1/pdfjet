/*
 * Image.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Used to create image objects and draw them on a page.
/// The image type can be one of the following: ImageType.JPG, ImageType.PNG or ImageType.BMP
///
/// Please see Example_03 and Example_24.
/// </summary>
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
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /// <summary>
    /// Convenience constructor for the Image class.
    /// </summary>
    /// <param name="pdf">the PDF to which we add this image.</param>
    /// <param name="filePath">the file path to the image file.</param>
    public Image(PDF pdf, String filePath) : this(pdf, new FileStream(filePath, FileMode.Open, FileAccess.Read),
            filePath.ToLower().EndsWith(".png") ? ImageType.PNG :
            filePath.ToLower().EndsWith(".bmp") ? ImageType.BMP : ImageType.JPG) {
    }

    /// <summary>
    /// The main constructor for the Image class.
    /// </summary>
    /// <param name="pdf">the page to draw this image on.</param>
    /// <param name="inputStream">the input stream to read the image from.</param>
    /// <param name="imageType">ImageType.JPG, ImageType.PNG or ImageType.BMP.</param>
    public Image(PDF pdf, Stream inputStream, int imageType) {
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.GetData();
            w = jpg.GetWidth();
            h = jpg.GetHeight();
            if (jpg.GetColorComponents() == 1) {
                AddImage(pdf, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.GetColorComponents() == 3) {
                AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.GetColorComponents() == 4) {
                AddImage(pdf, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImage(pdf, data, null, imageType, "DeviceGray", png.GetBitDepth());
            } else {
                if (png.GetBitDepth() == 16) {
                    AddImage(pdf, data, null, imageType, "DeviceRGB", 16);
                } else {
                    AddImage(pdf, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            w = bmp.GetWidth();
            h = bmp.GetHeight();
            AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
        }

        inputStream.Dispose();
    }

    // Method for creating images from byte[] image data
    public static Image CreateImage(PDF pdf, byte[] imageBytes, int imageType) {
        MemoryStream ms = new MemoryStream(imageBytes);
        Image image = new Image(pdf, ms, imageType);
        ms.Dispose();
        return image;
    }

    // Convenience method for creating .PNG images
    public static Image CreateImage(PDF pdf, byte[] imageBytes) {
        return CreateImage(pdf, imageBytes, ImageType.PNG);
    }

    /// <summary>
    /// Constructor used to attach images to existing PDF.
    /// </summary>
    /// <param name="objects">the objects of the existing PDF.</param>
    /// <param name="inputStream">the input stream to read the image from.</param>
    /// <param name="imageType">ImageType.JPG, ImageType.PNG and ImageType.BMP.</param>
    public Image(List<PDFobj> objects, Stream inputStream, int imageType) {
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.GetData();
            w = jpg.GetWidth();
            h = jpg.GetHeight();
            if (jpg.GetColorComponents() == 1) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.GetColorComponents() == 3) {
                AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.GetColorComponents() == 4) {
                AddImageToObjects(objects, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", png.GetBitDepth());
            } else {
                if (png.GetBitDepth() == 16) {
                    AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 16);
                } else {
                    AddImageToObjects(objects, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
        } else if (imageType == ImageType.BMP) {
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
        pdf.NewObj();
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
        pdf.EndObj();
        pdf.images.Add(this);
        objNumber = pdf.GetObjNumber();
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location of this image on the page to (x, y).
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the image.</param>
    /// <param name="y">the y coordinate of the top left corner of the image.</param>
    public Image SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public Image SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Scales this image by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the image.</param>
    public Image SetScaleFactor(double factor) {
        return this.SetScaleFactor((float) factor, (float) factor);
    }

    /// <summary>
    /// Scales this image by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the image.</param>
    public Image SetScaleFactor(float factor) {
        return this.SetScaleFactor(factor, factor);
    }

    public Image ScaleBy(float factor) {
        return this.SetScaleFactor(factor, factor);
    }

    /// <summary>
    /// Sets the image rotation to the specified number of degrees.
    /// </summary>
    /// <param name="degrees">the number of degrees.</param>
    public void RotateClockwise(int degrees) {
        if (degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270) {
            throw new Exception("The rotation angle must be 0, 90, 180 or 270");
        }
        this.degrees = degrees;
    }

    /// <summary>
    /// Scales this image by the specified width and height factor.
    /// <para><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</para>
    /// </summary>
    /// <param name="widthFactor">the factor used to scale the width of the image</param>
    /// <param name="heightFactor">the factor used to scale the height of the image</param>
    public Image SetScaleFactor(float widthFactor, float heightFactor) {
        this.w *= widthFactor;
        this.h *= heightFactor;
        return this;
    }

    public Image ScaleBy(float widthFactor, float heightFactor) {
        return SetScaleFactor(widthFactor, heightFactor);
    }

    /// <summary>
    /// Resizes the image to the specified width.
    /// </summary>
    /// <param name="width">the specified width.</param>
    public Image ResizeWidth(float width) {
        float factor = width / GetWidth();
        return this.ScaleBy(factor, factor);
    }

    /// <summary>
    /// Resizes the image to the specified height.
    /// </summary>
    /// <param name="height">the specified height.</param>
    public Image ResizeHeight(float height) {
        float factor = height / GetHeight();
        return this.ScaleBy(factor, factor);
    }

    /// <summary>
    /// Places this image in the specified box.
    /// </summary>
    /// <param name="box">the specified box.</param>
    public void PlaceIn(Box box) {
        xBox = box.x;
        yBox = box.y;
    }

    /// <summary>
    /// Sets the URI for the "click box" action.
    /// </summary>
    /// <param name="uri">the URI</param>
    /// <returns>this Image object.</returns>
    public Image SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Sets the destination key for the action.
    /// </summary>
    /// <param name="key">the destination name.</param>
    /// <returns>this Image object.</returns>
    public Image SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of this image.
    /// </summary>
    /// <param name="altDescription">the alternate description of the image.</param>
    /// <returns>this Image.</returns>
    public Image SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this image.
    /// </summary>
    /// <param name="actualText">the actual text for the image.</param>
    /// <returns>this Image.</returns>
    public Image SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>
    /// Draws this image on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    /// <exception cref="System.Exception"/>
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);

        x += xBox;
        y += yBox;

        page.SaveGraphicsState();

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
        } else if (degrees == 90) {
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
        } else if (degrees == 180) {
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
        } else if (degrees == 270) {
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

        page.RestoreGraphicsState();

        page.AddEMC();

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Transparency
                    null,   // Title
                    null,   // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] {x + w, y + h};
    }

    /// <summary>
    /// Returns the width of this image when drawn on the page.
    /// The scaling is take into account.
    /// </summary>
    /// <returns>w - the width of this image.</returns>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>
    /// Returns the height of this image when drawn on the page.
    /// The scaling is take into account.
    /// </summary>
    /// <returns>h - the height of this image.</returns>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>
    /// Resizes the image to fit the page.
    /// </summary>
    /// <param name="page">the PDF page</param>
    /// <param name="keepAspectRatio">flag</param>
    public void ResizeToFit(Page page, bool keepAspectRatio) {
        if (keepAspectRatio) {
            this.ScaleBy(Math.Min((page.width - x)/w, (page.height - y)/h));
        } else {
            this.ScaleBy((page.width - x)/w, (page.height - y)/h);
        }
    }

    /// <summary>
    /// Flips this image upside down.
    /// </summary>
    /// <param name="flipUpsideDown">flag</param>
    public void FlipUpsideDown(bool flipUpsideDown) {
        this.flipUpsideDown = flipUpsideDown;
    }

    private void AddSoftMask(
            PDF pdf,
            byte[] data,
            String colorSpace,
            int bitsPerComponent) {
        pdf.NewObj();
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

        byte[] buf = data;
        if (pdf.encryption != null) {
            buf = AES256.Encrypt(data, pdf.encryption.GetKey());
        }
        pdf.Append("/Length ");
        pdf.Append(buf.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(buf);
        pdf.Append("\nendstream\n");
        pdf.EndObj();
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
        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        if (imageType == ImageType.JPG) {
            pdf.Append("/Filter /DCTDecode\n");
        } else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
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

        byte[] buf = data;
        if (pdf.encryption != null) {
            buf = AES256.Encrypt(data, pdf.encryption.GetKey());
        }
        pdf.Append("/Length ");
        pdf.Append(buf.Length);
        pdf.Append('\n');
        pdf.Append(">>\n");
        pdf.Append("stream\n");
        pdf.Append(buf);
        pdf.Append("\nendstream\n");
        pdf.EndObj();
        pdf.images.Add(this);
        objNumber = pdf.GetObjNumber();
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
        } else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
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
}   // End of Image.cs
}   // End of namespace PDFjet.NET
