/*
 * Destination.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// A destination on a page, made by Page.AddDestination.
/// </summary>
public class Destination {
    /// <summary>The name of this destination.</summary>
    internal String name;
    /// <summary>The object number of the page this destination points to.</summary>
    internal int pageObjNumber;
    /// <summary>The x coordinate on the page.</summary>
    internal float xPosition;
    /// <summary>The y coordinate on the page.</summary>
    internal float yPosition;

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="xPosition">the x coordinate of the top left corner.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
    internal Destination(String name, float xPosition, float yPosition) {
        this.name = name;
        this.xPosition = xPosition;
        this.yPosition = yPosition;
    }

    /// <summary>
    /// This constructor is used to create destination objects.
    /// </summary>
    /// <param name="name">the name of this destination object.</param>
    /// <param name="yPosition">the y coordinate of the top left corner.</param>
    internal Destination(String name, float yPosition) {
        this.name = name;
        this.xPosition = 0f;
        this.yPosition = yPosition;
    }

    internal void SetPageObjNumber(int pageObjNumber) {
        this.pageObjNumber = pageObjNumber;
    }
}
}   // End of namespace PDFjet.NET
