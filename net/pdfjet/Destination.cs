/*
 * Destination.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create PDF destination objects.
/// </summary>
public class Destination {
    public String name;
    public int pageObjNumber;
    public float xPosition;
    public float yPosition;

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="xPosition">the x coordinate of the top left corner.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
    public Destination(String name, float xPosition, float yPosition) {
        this.name = name;
        this.xPosition = xPosition;
        this.yPosition = yPosition;
    }

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="xPosition">the x coordinate of the top left corner.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
    public Destination(String name, double xPosition, double yPosition) {
        this.name = name;
        this.xPosition = (float) xPosition;
        this.yPosition = (float) yPosition;
    }

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
    public Destination(String name, float yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = yPosition;
    }

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
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
