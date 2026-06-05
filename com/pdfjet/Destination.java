/**
 * Destination.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to create PDF destination objects.
 */
public class Destination {
    String name;
    int pageObjNumber;
    float xPosition;
    float yPosition;

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param xPosition the x coordinate of the top left corner.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, float xPosition, float yPosition) {
        this.name = name;
        this.xPosition = xPosition;
        this.yPosition = yPosition;
    }

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param xPosition the x coordinate of the top left corner.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, double xPosition, double yPosition) {
        this(name, (float) xPosition, (float) yPosition);
    }

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, float yPosition) {
        this(name, 0f, yPosition);
    }

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, double yPosition) {
        this(name, 0f, (float) yPosition);
    }

    protected void setPageObjNumber(int pageObjNumber) {
        this.pageObjNumber = pageObjNumber;
    }
}
