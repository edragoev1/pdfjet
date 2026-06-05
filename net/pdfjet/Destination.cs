/**
 * Destination.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to create PDF destination objects.
 */
namespace PDFjet.NET {
public class Destination {
    public String name;
    public int pageObjNumber;
    public float xPosition;
    public float yPosition;

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
        this.name = name;
        this.xPosition = (float) xPosition;
        this.yPosition = (float) yPosition;
    }

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, float yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = yPosition;
    }

    /**
     * This constructor is used to create destination objects.
     *
     * @param name the name of this destination object.
     * @param yPosition the y coordinate of the top left corner.
     */
    public Destination(String name, double yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = (float) yPosition;
    }

    internal void SetPageObjNumber(int pageObjNumber) {
        this.pageObjNumber = pageObjNumber;
    }
}
}   // End of namespace PDFjet.NET
