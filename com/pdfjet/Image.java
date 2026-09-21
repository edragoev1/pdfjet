/*
 * Image.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.io.*;
import java.util.*;

/**
 * Used to create image objects and draw them on a page.
 * The image type can be one of the following:
 *     ImageType.JPG, ImageType.PNG or ImageType.BMP
 *
 * Please see Example_03 and Example_24.
 */
final public class Image implements Drawable {
    /** The object number of the image. */
    protected int objNumber;
    // The PDF the image was added to, or null for an image of an existing PDF.
    private PDF pdf;

    /** The x coordinate of the image on the page. */
    protected float x = 0f; // Position of the image on the page
    /** The y coordinate of the image on the page. */
    protected float y = 0f;
    /** The width of the image. */
    protected float w;      // Image width
    /** The height of the image. */
    protected float h;      // Image height

    /** The URI opened when the image is clicked. */
    protected String uri;
    /** The destination key used by the GoTo action. */
    protected String key;

    private int degrees = 0;
    private boolean flipUpsideDown = false;
    // True for a CMYK JPEG that Adobe software wrote, with its inks inverted.
    private boolean invertedInks = false;

    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /**
     * Convenience constructor for the Image class.
     *
     * @param pdf the PDF to which we add this image.
     * @param filePath the file path to the image file.
     * @throws Exception  If an input or output exception occurred
     */
    public Image(PDF pdf, String filePath) throws Exception {
        this(pdf, new FileInputStream(filePath));
    }

    /**
     * The main constructor for the Image class.
     *
     * @param pdf the PDF to which we add this image.
     * @param inputStream the input stream to read the image from.
     * @throws Exception  If an input or output exception occurred
     */
    public Image(PDF pdf, InputStream inputStream) throws Exception {
        this(pdf, Content.getFromStream(inputStream));
    }

    private Image(PDF pdf, byte[] bytes) throws Exception {
        this.pdf = pdf;
        ImageType imageType = typeOf(bytes);
        InputStream inputStream = new ByteArrayInputStream(bytes);
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.getData();
            w = jpg.getWidth();
            h = jpg.getHeight();
            if (jpg.getColorComponents() == 1) {
                addImage(pdf, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.getColorComponents() == 3) {
                addImage(pdf, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.getColorComponents() == 4) {
                invertedInks = jpg.isAdobe();
                addImage(pdf, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.getData();
            w = png.getWidth();
            h = png.getHeight();
            if (png.getColorType() == 0) {
                addImage(pdf, data, null, imageType, "DeviceGray", png.getBitDepth());
            } else if (png.getColorType() == 4) {
                addImage(pdf, data, png.getAlpha(), imageType, "DeviceGray", 8);
            } else {
                if (png.getBitDepth() == 16) {
                    addImage(pdf, data, null, imageType, "DeviceRGB", 16);
                } else {
                    addImage(pdf, data, png.getAlpha(), imageType, "DeviceRGB", 8);
                }
            }
            setPhysicalSize(png);
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.getData();
            w = bmp.getWidth();
            h = bmp.getHeight();
            addImage(pdf, data, null, imageType, "DeviceRGB", 8);
        }

        inputStream.close();
    }

    /**
     * Constructor used to attach images to existing PDF.
     *
     * @param objects the map to which we add this image.
     * @param inputStream the input stream to read the image from.
     * @throws Exception  If an input or output exception occurred
     */
    public Image(List<PDFobj> objects, InputStream inputStream) throws Exception {
        this(objects, Content.getFromStream(inputStream));
    }

    private Image(List<PDFobj> objects, byte[] bytes) throws Exception {
        ImageType imageType = typeOf(bytes);
        InputStream inputStream = new ByteArrayInputStream(bytes);
        byte[] data;
        if (imageType == ImageType.JPG) {
            JPGImage jpg = new JPGImage(inputStream);
            data = jpg.getData();
            w = jpg.getWidth();
            h = jpg.getHeight();
            if (jpg.getColorComponents() == 1) {
                addImageToObjects(objects, data, null, imageType, "DeviceGray", 8);
            } else if (jpg.getColorComponents() == 3) {
                addImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
            } else if (jpg.getColorComponents() == 4) {
                invertedInks = jpg.isAdobe();
                addImageToObjects(objects, data, null, imageType, "DeviceCMYK", 8);
            }
        } else if (imageType == ImageType.PNG) {
            PNGImage png = new PNGImage(inputStream);
            data = png.getData();
            w = png.getWidth();
            h = png.getHeight();
            if (png.getColorType() == 0) {
                addImageToObjects(objects, data, null, imageType, "DeviceGray", png.getBitDepth());
            } else if (png.getColorType() == 4) {
                addImageToObjects(objects, data, png.getAlpha(), imageType, "DeviceGray", 8);
            } else {
                if (png.getBitDepth() == 16) {
                    addImageToObjects(objects, data, null, imageType, "DeviceRGB", 16);
                } else {
                    addImageToObjects(objects, data, png.getAlpha(), imageType, "DeviceRGB", 8);
                }
            }
            setPhysicalSize(png);
        } else if (imageType == ImageType.BMP) {
            BMPImage bmp = new BMPImage(inputStream);
            data = bmp.getData();
            w = bmp.getWidth();
            h = bmp.getHeight();
            addImageToObjects(objects, data, null, imageType, "DeviceRGB", 8);
        }
        inputStream.close();
    }

    /**
     * Creates new image from PDFobj
     *
     * @param pdf the PDF
     * @param obj the PDFobj
     * @throws Exception if can not parse the width or height
     */
    public Image(PDF pdf, PDFobj obj) throws Exception {
        this.pdf = pdf;
        w = Float.parseFloat(obj.getValue("/Width"));
        h = Float.parseFloat(obj.getValue("/Height"));
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /XObject\n");
        pdf.append("/Subtype /Image\n");
        pdf.append("/Filter ");
        pdf.append(obj.getValue("/Filter"));
        pdf.append("\n");
        pdf.append("/Width ");
        pdf.append(w);
        pdf.append('\n');
        pdf.append("/Height ");
        pdf.append(h);
        pdf.append('\n');
        String colorSpace = obj.getValue("/ColorSpace");
        if (!colorSpace.equals("")) {
            pdf.append("/ColorSpace ");
            pdf.append(colorSpace);
            pdf.append("\n");
        }
        pdf.append("/BitsPerComponent ");
        pdf.append(obj.getValue("/BitsPerComponent"));
        pdf.append("\n");
        String decodeParms = obj.getValue("/DecodeParms");
        if (!decodeParms.equals("")) {
            pdf.append("/DecodeParms ");
            pdf.append(decodeParms);
            pdf.append("\n");
        }
        String imageMask = obj.getValue("/ImageMask");
        if (!imageMask.equals("")) {
            pdf.append("/ImageMask ");
            pdf.append(imageMask);
            pdf.append("\n");
        }
        pdf.append("/Length ");
        pdf.append(obj.stream.length);
        pdf.append('\n');
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(obj.stream, 0, obj.stream.length);
        pdf.append("\nendstream\n");
        pdf.endObj();
        pdf.images.add(this);
        objNumber = pdf.getObjNumber();
    }

    // Draws the image at the size its pHYs chunk asks for, when it has one.
    // The width and the height are the pixels of the image until here, which
    // is what the image object of the PDF is written with, and are the size it
    // is drawn at from here on.
    private void setPhysicalSize(PNGImage png) {
        if (png.getPhysicalWidth() > 0f && png.getPhysicalHeight() > 0f) {
            this.w = png.getPhysicalWidth();
            this.h = png.getPhysicalHeight();
        }
    }

    /**
     * Sets the location of this image on the page to (x, y).
     *
     * @param x the x coordinate of the top left corner of the image.
     * @param y the y coordinate of the top left corner of the image.
     * @return this Image object.
     */
    public Image setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Scales this image by the specified factor.
     *
     * @param factor the factor used to scale the image.
     * @return this Image object.
     */
    public Image scaleBy(float factor) {
        return this.scaleBy(factor, factor);
    }

    /**
     * Scales this image by the specified width and height factor.
     * <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
     *
     * @param widthFactor the factor used to scale the width of the image
     * @param heightFactor the factor used to scale the height of the image
     * @return this Image object.
     */
    public Image scaleBy(float widthFactor, float heightFactor) {
        this.w *= widthFactor;
        this.h *= heightFactor;
        return this;
    }

    /**
     * Resizes the image
     *
     * @param width the desired width
     * @return the image
     */
    public Image resizeWidth(float width) {
        float factor = width / getWidth();
        return this.scaleBy(factor, factor);
    }

    /**
     * Resizes the image
     *
     * @param height the desired height
     * @return the image
     */
    public Image resizeHeight(float height) {
        float factor = height / getHeight();
        return this.scaleBy(factor, factor);
    }

    /**
     * Sets the URI for the "click box" action.
     *
     * @param uri the URI
     * @return this Image object.
     */
    public Image setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     * @return this Image object.
     */
    public Image setGoToAction(String key) {
        this.key = key;
        return this;
    }

    /**
     * Rotates this image clockwise, as every rotation in PDFjet turns.
     *
     * @param degrees the angle: 0, 90, 180 or 270.
     * @return this Image object.
     * @throws Exception if the angle is not one of the four.
     */
    public Image setRotation(int degrees) throws Exception {
        if (degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270) {
            throw new Exception("The rotation angle must be 0, 90, 180 or 270");
        }
        this.degrees = degrees;
        return this;
    }

    /**
     * Sets the alternate description of this image.
     *
     * @param altDescription the alternate description of the image.
     * @return this Image.
     */
    public Image setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Sets the actual text for this image.
     *
     * @param actualText the actual text for the image.
     * @return this Image.
     */
    public Image setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     * Sets the language of this image.
     *
     * @param language the language, for example "en-US".
     * @return this Image.
     */
    public Image setLanguage(String language) {
        this.language = language;
        return this;
    }

    /**
     * Draws this image on the specified page.
     *
     * @param page the page to draw this image on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        if (pdf != null && page.pdf != pdf) {
            page.pdf.fail(new IllegalArgumentException("The image belongs to another PDF."));
        }
        if (w == 0f || h == 0f) {
            return new float[] {x + w, y + h};  // A zero size image paints nothing.
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription);
        page.saveGraphicsState();

        if (degrees == 0) {
            page.append(w);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(h);
            page.append(' ');
            page.append(x);
            page.append(' ');
            page.append(page.height - (y + h));
            page.append(" cm\n");
        } else if (degrees == 90) {
            page.append(h);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(w);
            page.append(' ');
            page.append(x);
            page.append(' ');
            page.append(page.height - y);
            page.append(" cm\n");
            page.append("0 -1 1 0 0 0 cm\n");
        } else if (degrees == 180) {
            page.append(w);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(h);
            page.append(' ');
            page.append(x + w);
            page.append(' ');
            page.append(page.height - y);
            page.append(" cm\n");
            page.append("-1 0 0 -1 0 0 cm\n");
        } else if (degrees == 270) {
            page.append(h);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(0f);
            page.append(' ');
            page.append(w);
            page.append(' ');
            page.append(x + h);
            page.append(' ');
            page.append(page.height - (y + w));
            page.append(" cm\n");
            page.append("0 1 -1 0 0 0 cm\n");
        }

        if (flipUpsideDown) {
            page.append("1 0 0 -1 0 1 cm\n");
        }

        page.append("/Im");
        page.append(objNumber);
        page.append(" Do\n");

        page.restoreGraphicsState();

        page.addEMC();

        if (uri != null || key != null) {
            page.addAnnotation(new Annotation(
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

    /**
     * Returns the width of this image when drawn on the page.
     * The scaling is take into account.
     *
     * @return w - the width of this image.
     */
    public float getWidth() {
        return this.w;
    }

    /**
     * Returns the height of this image when drawn on the page.
     * The scaling is take into account.
     *
     * @return h - the height of this image.
     */
    public float getHeight() {
        return this.h;
    }

    private void addSoftMask(
            PDF pdf,
            byte[] data,
            String colorSpace,
            int bitsPerComponent) throws Exception {
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /XObject\n");
        pdf.append("/Subtype /Image\n");
        pdf.append("/Filter /FlateDecode\n");
        pdf.append("/Width ");
        pdf.append((int) w);
        pdf.append('\n');
        pdf.append("/Height ");
        pdf.append((int) h);
        pdf.append('\n');
        pdf.append("/ColorSpace /");
        pdf.append(colorSpace);
        pdf.append('\n');
        pdf.append("/BitsPerComponent ");
        pdf.append(bitsPerComponent);
        pdf.append('\n');

        byte[] buf = data;
        if (pdf.encryption != null) {
            buf = AES256.encrypt(data, pdf.encryption.getKey());
        }
        pdf.append("/Length ");
        pdf.append(buf.length);
        pdf.append('\n');
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(buf, 0, buf.length);
        pdf.append("\nendstream\n");
        pdf.endObj();
        objNumber = pdf.getObjNumber();
    }

    private void addImage(
            PDF pdf,
            byte[] data,
            byte[] alpha,
            ImageType imageType,
            String colorSpace,
            int bitsPerComponent) throws Exception {
        if (alpha != null) {
            addSoftMask(pdf, alpha, "DeviceGray", bitsPerComponent);
        }
        pdf.newObj();
        pdf.append("<<\n");
        pdf.append("/Type /XObject\n");
        pdf.append("/Subtype /Image\n");
        if (imageType == ImageType.JPG) {
            pdf.append("/Filter /DCTDecode\n");
        } else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
            pdf.append("/Filter /FlateDecode\n");
            if (alpha != null) {
                pdf.append("/SMask ");
                pdf.append(objNumber);
                pdf.append(" 0 R\n");
            }
        }
        pdf.append("/Width ");
        pdf.append((int) w);
        pdf.append('\n');
        pdf.append("/Height ");
        pdf.append((int) h);
        pdf.append('\n');
        pdf.append("/ColorSpace /");
        pdf.append(colorSpace);
        pdf.append('\n');
        pdf.append("/BitsPerComponent ");
        pdf.append(bitsPerComponent);
        pdf.append('\n');
        if (colorSpace.equals("DeviceCMYK") && invertedInks) {
            // Adobe software, Photoshop among them, stores the inks inverted.
            pdf.append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n");
        }

        byte[] buf = data;
        if (pdf.encryption != null) {
            buf = AES256.encrypt(data, pdf.encryption.getKey());
        }
        pdf.append("/Length ");
        pdf.append(buf.length);
        pdf.append('\n');
        pdf.append(">>\n");
        pdf.append("stream\n");
        pdf.append(buf, 0, buf.length);
        pdf.append("\nendstream\n");
        pdf.endObj();
        pdf.images.add(this);
        objNumber = pdf.getObjNumber();
    }

    private void addSoftMask(
            List<PDFobj> objects,
            byte[] data,
            String colorSpace,
            int bitsPerComponent) {
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/XObject");
        obj.dict.add("/Subtype");
        obj.dict.add("/Image");
        obj.dict.add("/Filter");
        obj.dict.add("/FlateDecode");
        obj.dict.add("/Width");
        obj.dict.add(String.valueOf((int) w));
        obj.dict.add("/Height");
        obj.dict.add(String.valueOf((int) h));
        obj.dict.add("/ColorSpace");
        obj.dict.add("/" + colorSpace);
        obj.dict.add("/BitsPerComponent");
        obj.dict.add(String.valueOf(bitsPerComponent));
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(data.length));
        obj.dict.add(">>");
        obj.setStream(data);
        obj.number = objects.size() + 1;
        objects.add(obj);
        objNumber = obj.number;
    }

    private void addImageToObjects(
            List<PDFobj> objects,
            byte[] data,
            byte[] alpha,
            ImageType imageType,
            String colorSpace,
            int bitsPerComponent) {
        if (alpha != null) {
            addSoftMask(objects, alpha, "DeviceGray", bitsPerComponent);
        }
        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/XObject");
        obj.dict.add("/Subtype");
        obj.dict.add("/Image");
        if (imageType == ImageType.JPG) {
            obj.dict.add("/Filter");
            obj.dict.add("/DCTDecode");
        } else if (imageType == ImageType.PNG || imageType == ImageType.BMP) {
            obj.dict.add("/Filter");
            obj.dict.add("/FlateDecode");
            if (alpha != null) {
                obj.dict.add("/SMask");
                obj.dict.add(String.valueOf(objNumber));
                obj.dict.add("0");
                obj.dict.add("R");
            }
        }
        obj.dict.add("/Width");
        obj.dict.add(String.valueOf((int) w));
        obj.dict.add("/Height");
        obj.dict.add(String.valueOf((int) h));
        obj.dict.add("/ColorSpace");
        obj.dict.add("/" + colorSpace);
        obj.dict.add("/BitsPerComponent");
        obj.dict.add(String.valueOf(bitsPerComponent));
        if (colorSpace.equals("DeviceCMYK") && invertedInks) {
            // Adobe software, Photoshop among them, stores the inks inverted.
            obj.dict.add("/Decode");
            obj.dict.add("[");
            obj.dict.add("1.0");
            obj.dict.add("0.0");
            obj.dict.add("1.0");
            obj.dict.add("0.0");
            obj.dict.add("1.0");
            obj.dict.add("0.0");
            obj.dict.add("1.0");
            obj.dict.add("0.0");
            obj.dict.add("]");
        }
        obj.dict.add("/Length");
        obj.dict.add(String.valueOf(data.length));
        obj.dict.add(">>");
        obj.setStream(data);
        obj.number = objects.size() + 1;
        objects.add(obj);
        objNumber = obj.number;
    }

    /**
     * Resizes this image
     *
     * @param page the PDF page
     * @param keepAspectRatio flag
     */
    public void resizeToFit(Page page, boolean keepAspectRatio) {
        if (keepAspectRatio) {
            this.scaleBy(Math.min((page.width - x)/w, (page.height - y)/h));
        } else {
            this.scaleBy((page.width - x)/w, (page.height - y)/h);
        }
    }

    /**
     * Sets whether this image is drawn upside down.
     *
     * @param flipUpsideDown true to draw this image upside down.
     * @return this Image object.
     */
    public Image setFlipUpsideDown(boolean flipUpsideDown) {
        this.flipUpsideDown = flipUpsideDown;
        return this;
    }

    /** Returns the type of the image from its first bytes. */
    static ImageType typeOf(byte[] bytes) throws Exception {
        if (bytes.length >= 4 && (bytes[0] & 0xFF) == 0x89 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G') {
            return ImageType.PNG;
        }
        if (bytes.length >= 2 && (bytes[0] & 0xFF) == 0xFF && (bytes[1] & 0xFF) == 0xD8) {
            return ImageType.JPG;
        }
        if (bytes.length >= 2 && bytes[0] == 'B' && bytes[1] == 'M') {
            return ImageType.BMP;
        }
        throw new Exception("The image is not a PNG, JPEG or BMP file.");
    }
}   // End of Image.java
