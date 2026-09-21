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
    // The PDF the image was added to, or null for an image of an existing PDF.
    private PDF pdf;
    internal float x = 0f;  // Position of the image on the page
    internal float y = 0f;
    internal float w;       // Image width
    internal float h;       // Image height
    internal String uri;
    internal String key;

    private int degrees = 0;
    private bool flipUpsideDown = false;
    // True for a CMYK JPEG that Adobe software wrote, with its inks inverted.
    private bool invertedInks = false;
    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /// <summary>
    /// Convenience constructor for the Image class.
    /// </summary>
    /// <param name="pdf">the PDF to which we add this image.</param>
    /// <param name="filePath">the file path to the image file.</param>
    public Image(PDF pdf, String filePath) : this(pdf, new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
    }

    /// <summary>
    /// The main constructor for the Image class.
    /// </summary>
    /// <param name="pdf">the page to draw this image on.</param>
    /// <param name="inputStream">the input stream to read the image from.</param>
    public Image(PDF pdf, Stream inputStream) : this(pdf, Content.GetFromStream(inputStream)) {
    }

    private Image(PDF pdf, byte[] bytes) {
        this.pdf = pdf;
        ImageType imageType = TypeOf(bytes);
        Stream inputStream = new MemoryStream(bytes);
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
                invertedInks = jpg.IsAdobe();
                AddImage(pdf, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImage(pdf, data, null, imageType, "DeviceGray", png.GetBitDepth());
            } else if (png.GetColorType() == 4) {
                AddImage(pdf, data, png.GetAlpha(), imageType, "DeviceGray", 8);
            } else {
                if (png.GetBitDepth() == 16) {
                    AddImage(pdf, data, null, imageType, "DeviceRGB", 16);
                } else {
                    AddImage(pdf, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
            SetPhysicalSize(png);
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            w = bmp.GetWidth();
            h = bmp.GetHeight();
            AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
        }

        inputStream.Dispose();
    }

    // Creates an image from the bytes of a PNG, JPEG or BMP file.
    internal static Image CreateImage(PDF pdf, byte[] imageBytes) {
        return new Image(pdf, imageBytes);
    }

    /// <summary>
    /// Constructor used to attach images to existing PDF.
    /// </summary>
    /// <param name="objects">the objects of the existing PDF.</param>
    /// <param name="inputStream">the input stream to read the image from.</param>
    public Image(List<PDFobj> objects, Stream inputStream) : this(objects, Content.GetFromStream(inputStream)) {
    }

    private Image(List<PDFobj> objects, byte[] bytes) {
        ImageType imageType = TypeOf(bytes);
        Stream inputStream = new MemoryStream(bytes);
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
                invertedInks = jpg.IsAdobe();
                AddImageToObjects(objects, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.GetData();
            w = png.GetWidth();
            h = png.GetHeight();
            if (png.GetColorType() == 0) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", png.GetBitDepth());
            } else if (png.GetColorType() == 4) {
                AddImageToObjects(objects, data, png.GetAlpha(), imageType, "DeviceGray", 8);
            } else {
                if (png.GetBitDepth() == 16) {
                    AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 16);
                } else {
                    AddImageToObjects(objects, data, png.GetAlpha(), imageType, "DeviceRGB", 8);
                }
            }
            SetPhysicalSize(png);
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
    /// <summary>Creates an image from an image object read from an existing PDF.</summary>
    public Image(PDF pdf, PDFobj obj) {
        this.pdf = pdf;
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

    // Draws the image at the size its pHYs chunk asks for, when it has one.
    // The width and the height are the pixels of the image until here, which
    // is what the image object of the PDF is written with, and are the size it
    // is drawn at from here on.
    private void SetPhysicalSize(PNGImage png) {
        if (png.GetPhysicalWidth() > 0f && png.GetPhysicalHeight() > 0f) {
            this.w = png.GetPhysicalWidth();
            this.h = png.GetPhysicalHeight();
        }
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

    /// <summary>
    /// Scales this image by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the image.</param>
    /// <returns>this Image object.</returns>
    public Image ScaleBy(float factor) {
        return this.ScaleBy(factor, factor);
    }

    /// <summary>Rotates this image clockwise by 0, 90, 180 or 270 degrees, as every rotation in PDFjet turns.</summary>
    public Image SetRotation(int degrees) {
        if (degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270) {
            throw new Exception("The rotation angle must be 0, 90, 180 or 270");
        }
        this.degrees = degrees;
        return this;
    }

    /// <summary>
    /// Scales this image by the specified width and height factor.
    /// <para><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</para>
    /// </summary>
    /// <param name="widthFactor">the factor used to scale the width of the image</param>
    /// <param name="heightFactor">the factor used to scale the height of the image</param>
    /// <returns>this Image object.</returns>
    public Image ScaleBy(float widthFactor, float heightFactor) {
        this.w *= widthFactor;
        this.h *= heightFactor;
        return this;
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
    /// Sets the language of this image.
    /// </summary>
    /// <param name="language">the language, for example "en-US".</param>
    /// <returns>this Image.</returns>
    public Image SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>
    /// Draws this image on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        if (pdf != null && page.pdf != pdf) {
            page.pdf.Fail(new ArgumentException("The image belongs to another PDF."));
        }
        if (w == 0f || h == 0f) {
            return new float[] {x + w, y + h};  // A zero size image paints nothing.
        }
        page.AddBDC(StructElem.FIGURE, language, actualText, altDescription);
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
            page.Append("1 0 0 -1 0 1 cm\n");
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
                    0f,     // Opacity
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
    /// Sets whether this image is drawn upside down.
    /// </summary>
    /// <param name="flipUpsideDown">true to draw this image upside down.</param>
    /// <returns>this Image object.</returns>
    public Image SetFlipUpsideDown(bool flipUpsideDown) {
        this.flipUpsideDown = flipUpsideDown;
        return this;
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
            ImageType imageType,
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
        if (colorSpace.Equals("DeviceCMYK") && invertedInks) {
            // Adobe software, Photoshop among them, stores the inks inverted.
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
            ImageType imageType,
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
        if (colorSpace.Equals("DeviceCMYK") && invertedInks) {
            // Adobe software, Photoshop among them, stores the inks inverted.
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

    // The type of the image, from its first bytes.
    internal static ImageType TypeOf(byte[] bytes) {
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == (byte) 'P' && bytes[2] == (byte) 'N' && bytes[3] == (byte) 'G') {
            return ImageType.PNG;
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) {
            return ImageType.JPG;
        }
        if (bytes.Length >= 2 && bytes[0] == (byte) 'B' && bytes[1] == (byte) 'M') {
            return ImageType.BMP;
        }
        throw new Exception("The image is not a PNG, JPEG or BMP file.");
    }
}   // End of Image.cs
}   // End of namespace PDFjet.NET
