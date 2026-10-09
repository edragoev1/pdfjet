/*
 * Image.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

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
    // The pixels of the image across and down, which the image object is
    // written with: a float holds every whole number only up to 2^24.
    private int pixelWidth;
    private int pixelHeight;
    internal String uri;
    internal String key;

    private int degrees = 0;
    private bool flipUpsideDown = false;
    // The Exif orientation of a JPEG, 2 to 8, or 0; w and h are its size as seen.
    private int orientation = 0;
    // True for a CMYK JPEG that Adobe software wrote, with its inks inverted.
    private bool invertedInks = false;
    // The /Mask of the transparent color of a grayscale or truecolor PNG, or
    // null.
    private int[] colorKeyMask;
    // The colors of a pixel of a PNG whose stream is its IDAT data, for the
    // /DecodeParms that undo the PNG filters of its rows, or 0.
    private int decodeColors;
    // The colors of the /Indexed color space of a palette PNG, or null.
    private byte[] palette;
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
            JPGImage jpg = new JPGImage(bytes);
            data = jpg.GetData();
            SetPixels(jpg.GetWidth(), jpg.GetHeight());
            if (jpg.GetColorComponents() == 1) {
                AddImage(pdf, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.GetColorComponents() == 3) {
                AddImage(pdf, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.GetColorComponents() == 4) {
                invertedInks = jpg.IsAdobe();
                AddImage(pdf, data, null, imageType, "DeviceCMYK", 8);
            }
            SetPhysicalSize(jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight());
            SetOrientation(jpg.orientation);
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            SetPixels(png.GetWidth(), png.GetHeight());
            colorKeyMask = png.GetColorKeyMask();
            decodeColors = png.decodeColors;
            palette = png.palette;
            String colorSpace = png.GetColorSpace(out int bitsPerComponent);
            AddImage(pdf, png.stream, png.GetAlpha(), imageType, colorSpace, bitsPerComponent);
            SetPhysicalSize(png.GetPhysicalWidth(), png.GetPhysicalHeight());
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            SetPixels(bmp.GetWidth(), bmp.GetHeight());
            AddImage(pdf, data, bmp.GetAlpha(), imageType, "DeviceRGB", 8);
            SetPhysicalSize(bmp.GetPhysicalWidth(), bmp.GetPhysicalHeight());
        }

        inputStream.Dispose();
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
            JPGImage jpg = new JPGImage(bytes);
            data = jpg.GetData();
            SetPixels(jpg.GetWidth(), jpg.GetHeight());
            if (jpg.GetColorComponents() == 1) {
                AddImageToObjects(objects, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.GetColorComponents() == 3) {
                AddImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.GetColorComponents() == 4) {
                invertedInks = jpg.IsAdobe();
                AddImageToObjects(objects, data, null, imageType, "DeviceCMYK", 8);
            }
            SetPhysicalSize(jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight());
            SetOrientation(jpg.orientation);
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            SetPixels(png.GetWidth(), png.GetHeight());
            colorKeyMask = png.GetColorKeyMask();
            decodeColors = png.decodeColors;
            palette = png.palette;
            String colorSpace = png.GetColorSpace(out int bitsPerComponent);
            AddImageToObjects(objects, png.stream, png.GetAlpha(), imageType, colorSpace, bitsPerComponent);
            SetPhysicalSize(png.GetPhysicalWidth(), png.GetPhysicalHeight());
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.GetData();
            SetPixels(bmp.GetWidth(), bmp.GetHeight());
            AddImageToObjects(objects, data, bmp.GetAlpha(), imageType, "DeviceRGB", 8);
            SetPhysicalSize(bmp.GetPhysicalWidth(), bmp.GetPhysicalHeight());
        }
        inputStream.Close();
    }

    // Creates new image from an existing PDF object
    /// <summary>Creates an image from an image object read from an existing PDF.</summary>
    public Image(PDF pdf, PDFobj obj) {
        this.pdf = pdf;
        SetPixels((int) double.Parse(obj.GetValue("/Width"), System.Globalization.CultureInfo.InvariantCulture),
                (int) double.Parse(obj.GetValue("/Height"), System.Globalization.CultureInfo.InvariantCulture));
        pdf.NewObj();
        pdf.Append("<<\n");
        pdf.Append("/Type /XObject\n");
        pdf.Append("/Subtype /Image\n");
        pdf.Append("/Filter ");
        pdf.Append(obj.GetValue("/Filter"));
        pdf.Append("\n");
        pdf.Append("/Width ");
        pdf.Append(pixelWidth);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append(pixelHeight);
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

    // Sets the pixels of the image across and down, which it is drawn at, a
    // pixel to a point, unless the file asks for another size.
    private void SetPixels(int width, int height) {
        pixelWidth = width;
        pixelHeight = height;
        w = width;
        h = height;
    }

    // Draws the image at the size the file asks for, when it asks for one: the
    // pHYs chunk of a PNG, the JFIF density of a JPEG or the pixels per metre
    // of a BMP.
    // The width and the height are the pixels of the image until here, which
    // is what the image object of the PDF is written with, and are the size it
    // is drawn at from here on.
    private void SetPhysicalSize(float width, float height) {
        if (width > 0f && height > 0f) {
            this.w = width;
            this.h = height;
        }
    }

    // Keeps the Exif orientation of a JPEG, which is drawn as it is meant to be
    // seen: turned a quarter of the way (5 to 8), its size as seen is its
    // height by its width. 1, upright as stored, is drawn as without one.
    private void SetOrientation(int orientation) {
        if (orientation < 2 || orientation > 8) {
            return;
        }
        this.orientation = orientation;
        if (orientation >= 5) {
            float width = this.w;
            this.w = this.h;
            this.h = width;
        }
    }

    // Turn or flip the unit square of an image as stored into the image as
    // seen, for the Exif orientations 2 to 8.
    private static readonly string[] ORIENTATION_MATRICES = {
        null,
        null,
        "-1 0 0 1 1 0 cm\n",
        "-1 0 0 -1 1 1 cm\n",
        "1 0 0 -1 0 1 cm\n",
        "0 -1 -1 0 1 1 cm\n",
        "0 -1 1 0 0 1 cm\n",
        "0 1 1 0 0 0 cm\n",
        "0 1 -1 0 1 0 cm\n",
    };

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
        // A linked image is a Figure in the Link its annotation joins
        StructElement link = null;
        if (uri != null || key != null) {
            link = page.BeginLink();
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
        if (orientation != 0) {
            page.Append(ORIENTATION_MATRICES[orientation]);
        }

        page.Append("/Im");
        page.Append(objNumber);
        page.Append(" Do\n");

        page.RestoreGraphicsState();

        // Turned a quarter of the way, the image is as wide as it is tall unturned
        if (degrees == 90 || degrees == 270) {
            page.SetFigureBoundingBox(x, y, h, w);
        } else {
            page.SetFigureBoundingBox(x, y, w, h);
        }
        page.AddEMC();

        page.EndLink(link);
        if (uri != null || key != null) {
            // The link covers the image as it is drawn, turned or not.
            float linkW = (degrees == 90 || degrees == 270) ? h : w;
            float linkH = (degrees == 90 || degrees == 270) ? w : h;
            Annotation linkAnnotation = new Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + linkW,
                    y + linkH,
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Opacity
                    null,   // Title
                    null,   // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription);
            linkAnnotation.linkElement = link;
            page.AddAnnotation(linkAnnotation);
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
        pdf.Append(pixelWidth);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append(pixelHeight);
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
        if (!IsPDFA(pdf, alpha, colorSpace, bitsPerComponent)) {
            return;
        }
        if (alpha != null) {
            AddSoftMask(pdf, alpha, "DeviceGray", 8);
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
            } else if (colorKeyMask != null) {
                pdf.Append("/Mask [");
                for (int i = 0; i < colorKeyMask.Length; i++) {
                    if (i > 0) {
                        pdf.Append(' ');
                    }
                    pdf.Append(colorKeyMask[i]);
                }
                pdf.Append("]\n");
            }
        }
        pdf.Append("/Width ");
        pdf.Append(pixelWidth);
        pdf.Append('\n');
        pdf.Append("/Height ");
        pdf.Append(pixelHeight);
        pdf.Append('\n');
        if (palette != null) {
            byte[] colors = palette;
            if (pdf.encryption != null) {
                colors = AES256.Encrypt(colors, pdf.encryption.GetKey());
            }
            pdf.Append("/ColorSpace [/Indexed /DeviceRGB ");
            pdf.Append(palette.Length / 3 - 1);
            pdf.Append(" <");
            pdf.Append(Util.ToHexString(colors));
            pdf.Append(">]\n");
        } else {
            pdf.Append("/ColorSpace /");
            pdf.Append(colorSpace);
            pdf.Append('\n');
        }
        pdf.Append("/BitsPerComponent ");
        pdf.Append(bitsPerComponent);
        pdf.Append('\n');
        if (colorSpace.Equals("DeviceCMYK") && invertedInks) {
            // Adobe software, Photoshop among them, stores the inks inverted.
            pdf.Append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n");
        }
        if (decodeColors != 0) {
            // The rows of a PNG, each with the filter type of its PNG filter.
            pdf.Append("/DecodeParms <</Predictor 15 /Colors ");
            pdf.Append(decodeColors);
            pdf.Append(" /BitsPerComponent ");
            pdf.Append(bitsPerComponent);
            pdf.Append(" /Columns ");
            pdf.Append(pixelWidth);
            pdf.Append(">>\n");
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

    // Returns whether a PDF/A document can hold the image, and fails the
    // document when it cannot. Its output intent is sRGB, an RGB profile, so
    // its images are gray or RGB and not CMYK, as ISO 19005 asks of a device
    // color space. PDF/A-1 is PDF 1.4, which has no soft masks, and 8 bits per
    // component at most. Any other document holds any image.
    private static bool IsPDFA(PDF pdf, byte[] alpha, String colorSpace, int bitsPerComponent) {
        Compliance level = pdf.compliance;
        if (level == Compliance.PDF_1_7 || level == Compliance.PDF_UA_1) {
            return true;
        }
        bool pdfA1 = level == Compliance.PDF_A_1A || level == Compliance.PDF_A_1B;
        if (colorSpace.Equals("DeviceCMYK")) {
            pdf.Fail(new InvalidOperationException("A document of " + level
                    + " cannot hold a CMYK image: its output intent is sRGB, so its images are gray or RGB."));
            return false;
        }
        if (pdfA1 && alpha != null) {
            pdf.Fail(new InvalidOperationException("A document of " + level
                    + " cannot hold an image with transparency: PDF/A-1 has no soft masks, so its images are opaque."));
            return false;
        }
        if (pdfA1 && bitsPerComponent > 8) {
            pdf.Fail(new InvalidOperationException("A document of " + level
                    + " cannot hold an image of " + bitsPerComponent
                    + " bits per component: PDF/A-1 has 8 at most."));
            return false;
        }
        return true;
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
        obj.dict.Add(pixelWidth.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/Height");
        obj.dict.Add(pixelHeight.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/ColorSpace");
        obj.dict.Add("/" + colorSpace);
        obj.dict.Add("/BitsPerComponent");
        obj.dict.Add(bitsPerComponent.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/Length");
        obj.dict.Add(data.Length.ToString(CultureInfo.InvariantCulture));
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
            AddSoftMask(objects, alpha, "DeviceGray", 8);
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
                obj.dict.Add(objNumber.ToString(CultureInfo.InvariantCulture));
                obj.dict.Add("0");
                obj.dict.Add("R");
            } else if (colorKeyMask != null) {
                obj.dict.Add("/Mask");
                obj.dict.Add("[");
                foreach (int value in colorKeyMask) {
                    obj.dict.Add(value.ToString(CultureInfo.InvariantCulture));
                }
                obj.dict.Add("]");
            }
        }
        obj.dict.Add("/Width");
        obj.dict.Add(pixelWidth.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/Height");
        obj.dict.Add(pixelHeight.ToString(CultureInfo.InvariantCulture));
        obj.dict.Add("/ColorSpace");
        if (palette != null) {
            obj.dict.Add("[");
            obj.dict.Add("/Indexed");
            obj.dict.Add("/DeviceRGB");
            obj.dict.Add((palette.Length / 3 - 1).ToString(CultureInfo.InvariantCulture));
            obj.dict.Add("<" + Util.ToHexString(palette) + ">");
            obj.dict.Add("]");
        } else {
            obj.dict.Add("/" + colorSpace);
        }
        obj.dict.Add("/BitsPerComponent");
        obj.dict.Add(bitsPerComponent.ToString(CultureInfo.InvariantCulture));
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
        if (decodeColors != 0) {
            // The rows of a PNG, each with the filter type of its PNG filter.
            obj.dict.Add("/DecodeParms");
            obj.dict.Add("<<");
            obj.dict.Add("/Predictor");
            obj.dict.Add("15");
            obj.dict.Add("/Colors");
            obj.dict.Add(decodeColors.ToString(CultureInfo.InvariantCulture));
            obj.dict.Add("/BitsPerComponent");
            obj.dict.Add(bitsPerComponent.ToString(CultureInfo.InvariantCulture));
            obj.dict.Add("/Columns");
            obj.dict.Add(pixelWidth.ToString(CultureInfo.InvariantCulture));
            obj.dict.Add(">>");
        }
        obj.dict.Add("/Length");
        obj.dict.Add(data.Length.ToString(CultureInfo.InvariantCulture));
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
