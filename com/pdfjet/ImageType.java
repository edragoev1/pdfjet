/*
 * ImageType.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to specify the image type of an image.
 * Supported types: ImageType.JPG, ImageType.PNG and ImageType.BMP
 * See the Image class for more information.
 */
public class ImageType {
    /** Default Constructor */
    public ImageType() {
    }

    /** JPEG image */
    public static final int JPG = 0;

    /** PNG image */
    public static final int PNG = 1;

    /** Bitmap image */
    public static final int BMP = 2;
}
