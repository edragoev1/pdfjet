/**
 *  ImageSize.java
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.InputStream;

/**
 * The size an image is drawn at, and its pixels, read from the header of its
 * file alone, without its image data in memory: the size of a page laid out
 * before its images are drawn, and an image refused early, for its size or
 * for a header that Image refuses, before a byte of its image data is
 * decoded.
 *
 * The size is the one Image gives the image: its pixels, or the physical size
 * the file asks for, the pHYs chunk of a PNG, the JFIF density of a JPEG or
 * the pixels per metre of a BMP; a JPEG turned a quarter of the way by its
 * Exif orientation is its height by its width. What only the image data
 * shows, a JPEG cut short or the rows of a PNG, is Image's to refuse.
 */
public final class ImageSize {
    private final float width;
    private final float height;
    private final int pixelWidth;
    private final int pixelHeight;

    private ImageSize(int pixelWidth, int pixelHeight, float physicalWidth, float physicalHeight) {
        this.pixelWidth = pixelWidth;
        this.pixelHeight = pixelHeight;
        if (physicalWidth > 0f && physicalHeight > 0f) {
            this.width = physicalWidth;
            this.height = physicalHeight;
        } else {
            this.width = pixelWidth;
            this.height = pixelHeight;
        }
    }

    private ImageSize(ImageSize size, boolean turned) {
        this.pixelWidth = size.pixelWidth;
        this.pixelHeight = size.pixelHeight;
        this.width = turned ? size.height : size.width;
        this.height = turned ? size.width : size.height;
    }

    /**
     * Reads the size of the PNG, JPEG or BMP file at the path.
     *
     * @param filePath the path of the image file.
     * @return the size of the image.
     * @throws Exception if the file is not one Image takes, as far as its header shows.
     */
    public static ImageSize read(String filePath) throws Exception {
        try (FileInputStream stream = new FileInputStream(filePath)) {
            return read(stream);
        }
    }

    /**
     * Reads the size of the PNG, JPEG or BMP image of the stream from its
     * header, or throws why Image would refuse the image, as far as its header
     * shows. A JPEG is read to its frame header, a BMP to its pixels per
     * metre, and a PNG to its end, a chunk at a time, its image data checked
     * and passed over, as a pHYs chunk may come after it. The stream is not
     * closed.
     *
     * @param inputStream the stream of the image.
     * @return the size of the image.
     * @throws Exception if the image is not one Image takes, as far as its header shows.
     */
    public static ImageSize read(InputStream inputStream) throws Exception {
        BufferedInputStream in = new BufferedInputStream(inputStream);
        in.mark(4);
        byte[] head = new byte[4];
        int n = 0;
        while (n < 4) {
            int r = in.read(head, n, 4 - n);
            if (r < 0) {
                break;
            }
            n += r;
        }
        in.reset();
        if (n >= 4 && (head[0] & 0xFF) == 0x89 && head[1] == 'P' && head[2] == 'N' && head[3] == 'G') {
            PNGImage png = PNGImage.readSize(in);
            return new ImageSize(png.getWidth(), png.getHeight(),
                    png.getPhysicalWidth(), png.getPhysicalHeight());
        }
        if (n >= 2 && (head[0] & 0xFF) == 0xFF && (head[1] & 0xFF) == 0xD8) {
            JPGImage jpg = new JPGImage(in, true);
            ImageSize size = new ImageSize(jpg.getWidth(), jpg.getHeight(),
                    jpg.getPhysicalWidth(), jpg.getPhysicalHeight());
            // Turned a quarter of the way, as Image's setOrientation turns it
            return new ImageSize(size, jpg.orientation >= 5 && jpg.orientation <= 8);
        }
        if (n >= 2 && head[0] == 'B' && head[1] == 'M') {
            BMPImage bmp = new BMPImage(in, true);
            return new ImageSize(bmp.getWidth(), bmp.getHeight(),
                    bmp.getPhysicalWidth(), bmp.getPhysicalHeight());
        }
        throw new Exception("The image is not a PNG, JPEG or BMP file.");
    }

    /**
     * Returns the width the image is drawn at, in points, as Image's getWidth
     * before it is scaled.
     *
     * @return the width in points.
     */
    public float getWidth() {
        return width;
    }

    /**
     * Returns the height the image is drawn at, in points, as Image's
     * getHeight before it is scaled.
     *
     * @return the height in points.
     */
    public float getHeight() {
        return height;
    }

    /**
     * Returns the width of the image in pixels, as stored.
     *
     * @return the width in pixels.
     */
    public int getPixelWidth() {
        return pixelWidth;
    }

    /**
     * Returns the height of the image in pixels, as stored.
     *
     * @return the height in pixels.
     */
    public int getPixelHeight() {
        return pixelHeight;
    }
}   // End of ImageSize.java
