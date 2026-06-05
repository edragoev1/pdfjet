/**
 * Image.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

/**
 * Used to create image objects and draw them on a page.
 * The image type can be one of the following: ImageType.JPG, ImageType.PNG or ImageType.BMP
 *
 * Please see Example_03 and Example_24.
 */
namespace PDFjet.NET {
public class Image : IDrawable {
    internal int objNumber;
    internal float x = 0f;  // Position of the image on the page
    internal float y = 0f;
    internal float w;       // Image width
    internal float h;       // Image height
    internal String uri;
    internal String key;

    private float degrees = 0;
    private String language = null;
    private String altDescription = null;
    private String actualText = null;

    /**
     * Convenience constructor for the Image class.
     *
     * @param pdf the PDF to which we add this image.
     * @param filePath the file path to the image file.
     */
    public Image(PDF pdf, String filePath) : this(pdf, new FileStream(filePath, FileMode.Open, FileAccess.Read),
            filePath.ToLower().EndsWith(".png") ? ImageType.PNG :
            filePath.ToLower().EndsWith(".bmp") ? ImageType.BMP : ImageType.JPG) {
    }

    /**
     * The main constructor for the Image class.
     *
     * @param pdf the page to draw this image on.
     * @param inputStream the input stream to read the image from.
     * @param imageType ImageType.JPG, ImageType.PNG or ImageType.BMP.
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

    /**
     * Constructor used to attach images to existing PDF.
     *
     * @param pdf the page to draw this image on.
     * @param inputStream the input stream to read the image from.
     * @param imageType ImageType.JPG, ImageType.PNG and ImageType.BMP.
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

    /**
     * Sets the position of this image on the page to (x, y).
     *
     * @param x the x coordinate of the top left corner of the image.
     * @param y the y coordinate of the top left corner of the image.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     * Sets the position of this image on the page to (x, y).
     *
     * @param x the x coordinate of the top left corner of the image.
     * @param y the y coordinate of the top left corner of the image.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public Image SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     * Sets the location of this image on the page to (x, y).
     *
     * @param x the x coordinate of the top left corner of the image.
     * @param y the y coordinate of the top left corner of the image.
     */
    public Image SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Scales this image by the specified factor.
     *
     * @param factor the factor used to scale the image.
     */
    public Image SetScaleFactor(double factor) {
        return this.SetScaleFactor((float) factor, (float) factor);
    }

    /**
     * Scales this image by the specified factor.
     *
     * @param factor the factor used to scale the image.
     */
    public Image SetScaleFactor(float factor) {
        return this.SetScaleFactor(factor, factor);
    }

    public Image ScaleBy(float factor) {
        return this.SetScaleFactor(factor, factor);
    }

    public Image SetRotateFactor(float degrees) {
        this.degrees = -degrees;
        return this;
    }

    public Image RotateBy(float degrees) {
        this.degrees = -degrees;
        return this;
    }

    /**
     * Scales this image by the specified width and height factor.
     * <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
     *
     * @param widthFactor the factor used to scale the width of the image
     * @param heightFactor the factor used to scale the height of the image
     */
    public Image SetScaleFactor(float widthFactor, float heightFactor) {
        this.w *= widthFactor;
        this.h *= heightFactor;
        return this;
    }

    public Image ScaleBy(float widthFactor, float heightFactor) {
        return SetScaleFactor(widthFactor, heightFactor);
    }

//    public Image ResizeWidth(float width) {
//        float factor = width / GetWidth();
//        return this.ScaleBy(factor, factor);
//    }
//
//    public Image ResizeHeight(float height) {
//        float factor = height / GetHeight();
//        return this.ScaleBy(factor, factor);
//    }

    /**
     * Sets the URI for the "click box" action.
     *
     * @param uri the URI
     */
    public void SetURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     */
    public void SetGoToAction(String key) {
        this.key = key;
    }

    /**
     * Sets the alternate description of this image.
     *
     * @param altDescription the alternate description of the image.
     * @return this Image.
     */
    public Image SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this image.
     *
     * @param actualText the actual text for the image.
     * @return this Image.
     */
    public Image SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Draws this image on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (!String.IsNullOrEmpty(actualText) && !String.IsNullOrEmpty(altDescription)) {
            page.AddBMC(StructElem.P, language, actualText, altDescription);
        }
        page.Append("q\n");

        page.ScaleAndRotate(x, y, w, h, degrees);
        page.Append("/Im");
        page.Append(objNumber);
        page.Append(" Do\n");

        page.Append("Q\n");
        if (!String.IsNullOrEmpty(actualText) && !String.IsNullOrEmpty(altDescription)) {
            page.AddEMC();
        }

        if (uri != null || key != null) {
            if (!String.IsNullOrEmpty(actualText) && !String.IsNullOrEmpty(altDescription)) {
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
        }

        return new float[] {x + w, y + h};
    }

    /**
     * Returns the width of this image when drawn on the page.
     * The scaling is take into account.
     *
     * @return w - the width of this image.
     */
    public float GetWidth() {
        return this.w;
    }

    /**
     * Returns the height of this image when drawn on the page.
     * The scaling is take into account.
     *
     * @return h - the height of this image.
     */
    public float GetHeight() {
        return this.h;
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
