/*
 * CompositeTextLine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// This class was designed and implemented by Jon T. Swanson, Ph.D.
///
/// Used to create composite text line objects.
/// </summary>
public class CompositeTextLine : IDrawable {
    private const int X = 0;
    private const int Y = 1;

    private List<TextLine> textLines = new List<TextLine>();

    private float[] position = new float[2];
    private float[] current  = new float[2];

    // Subscript and Superscript size factors
    private float subscriptSizeFactor   = 0.583f;
    private float superscriptSizeFactor = 0.583f;

    // Subscript and Superscript positions in relation to the base font
    private float superscriptPosition = 0.350f;
    private float subscriptPosition   = 0.141f;

    private float fontSize = 0f;

    /// <summary>Creates a composite text line at the specified location.</summary>
    public CompositeTextLine(float x, float y) {
        position[X] = x;
        position[Y] = y;
        current[X]  = x;
        current[Y]  = y;
    }

    /// <summary>
    /// Sets the font size.
    /// </summary>
    /// <param name="fontSize">the font size.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>
    /// Gets the font size.
    /// </summary>
    /// <returns>fontSize the font size.</returns>
    public float GetFontSize() {
        return fontSize;
    }

    /// <summary>
    /// Sets the superscript factor for this composite text line.
    /// </summary>
    /// <param name="superscript">the superscript size factor.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSuperscriptFactor(float superscript) {
        this.superscriptSizeFactor = superscript;
        return this;
    }

    /// <summary>
    /// Gets the superscript factor for this text line.
    /// </summary>
    /// <returns>superscript the superscript size factor.</returns>
    public float GetSuperscriptFactor() {
        return superscriptSizeFactor;
    }

    /// <summary>
    /// Sets the subscript factor for this composite text line.
    /// </summary>
    /// <param name="subscript">the subscript size factor.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSubscriptFactor(float subscript) {
        this.subscriptSizeFactor = subscript;
        return this;
    }

    /// <summary>
    /// Gets the subscript factor for this text line.
    /// </summary>
    /// <returns>subscript the subscript size factor.</returns>
    public float GetSubscriptFactor() {
        return subscriptSizeFactor;
    }

    /// <summary>
    /// Sets the superscript position for this composite text line.
    /// </summary>
    /// <param name="superscriptPosition">the superscript position.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSuperscriptPosition(float superscriptPosition) {
        this.superscriptPosition = superscriptPosition;
        return this;
    }

    /// <summary>
    /// Gets the superscript position for this text line.
    /// </summary>
    /// <returns>superscriptPosition the superscript position.</returns>
    public float GetSuperscriptPosition() {
        return superscriptPosition;
    }

    /// <summary>
    /// Sets the subscript position for this composite text line.
    /// </summary>
    /// <param name="subscriptPosition">the subscript position.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSubscriptPosition(float subscriptPosition) {
        this.subscriptPosition = subscriptPosition;
        return this;
    }

    /// <summary>
    /// Gets the subscript position for this text line.
    /// </summary>
    /// <returns>subscriptPosition the subscript position.</returns>
    public float GetSubscriptPosition() {
        return subscriptPosition;
    }

    /// <summary>
    /// Add a new text line.
    ///
    /// Find the current font, current size and effects (normal, super or subscript)
    /// Set the position of the component to the starting stored as current position
    /// Set the size and offset based on effects
    /// Set the new current position
    /// </summary>
    /// <param name="component">the component.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine AddComponent(TextLine component) {
        if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
            if (fontSize > 0f) {
                // Set it on the TextLine: DrawOn uses the line's own font size,
                // so resizing the shared Font here would have no effect.
                component.SetFontSize(fontSize * superscriptSizeFactor);
            }
            component.SetLocation(
                    current[X],
                    current[Y] - fontSize * superscriptPosition);
        } else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
            if (fontSize > 0f) {
                component.SetFontSize(fontSize * subscriptSizeFactor);
            }
            component.SetLocation(
                    current[X],
                    current[Y] + fontSize * subscriptPosition);
        } else {
            if (fontSize > 0f) {
                component.SetFontSize(fontSize);
            }
            component.SetLocation(current[X], current[Y]);
        }
        current[X] += component.GetWidth();
        textLines.Add(component);
        return this;
    }

    /// <summary>
    /// Loop through all the text lines and reset their position based on
    /// the new position set here.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Loop through all the text lines and reset their location based on
    /// the new location set here.
    /// </summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetLocation(float x, float y) {
        position[X] = x;
        position[Y] = y;
        current[X]  = x;
        current[Y]  = y;

        if (textLines == null || textLines.Count == 0) {
            return this;
        }

        foreach (TextLine component in textLines) {
            if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
                component.SetLocation(
                        current[X],
                        current[Y] - fontSize * superscriptPosition);
            } else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
                component.SetLocation(
                        current[X],
                        current[Y] + fontSize * subscriptPosition);
            } else {
                component.SetLocation(current[X], current[Y]);
            }
            current[X] += component.GetWidth();
        }
        return this;
    }

    /// <summary>
    /// Return the nth entry in the TextLine array.
    /// </summary>
    /// <param name="index">the index of the nth element.</param>
    /// <returns>the text line at the specified index.</returns>
    public TextLine GetTextLine(int index) {
        if (textLines == null || textLines.Count == 0) {
            return null;
        }
        if (index < 0 || index > textLines.Count - 1) {
            return null;
        }
        return textLines[index];
    }

    /// <summary>
    /// Returns the position of this composite text line.
    /// </summary>
    /// <returns>the x and y coordinates of this composite text line.</returns>
    public float[] GetLocation() {
        return position;
    }

    /// <summary>
    /// Returns the number of text lines.
    /// </summary>
    /// <returns>the number of text lines.</returns>
    public int GetNumberOfTextLines() {
        return textLines.Count;
    }

    /// <summary>
    /// Returns the vertical coordinates of the top left and bottom right corners
    /// of the bounding box of this composite text line.
    /// </summary>
    /// <returns>the an array containing the vertical coordinates.</returns>
    public float[] GetMinMaxY() {
        float min = position[Y];
        float max = position[Y];
        float cur;

        foreach (TextLine component in textLines) {
            if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
                cur = (position[Y] - component.font.ascent) - fontSize * superscriptPosition;
                if (cur < min)
                    min = cur;
            } else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
                cur = (position[Y] + component.font.descent) + fontSize * subscriptPosition;
                if (cur > max)
                    max = cur;
            } else {
                cur = position[Y] - component.font.ascent;
                if (cur < min)
                    min = cur;
                cur = position[Y] + component.font.descent;
                if (cur > max)
                    max = cur;
            }
        }

        return new float[] {min, max};
    }

    /// <summary>
    /// Returns the height of this CompositeTextLine.
    /// </summary>
    /// <returns>the height.</returns>
    public float GetHeight() {
        float[] minMax = GetMinMaxY();
        return minMax[1] - minMax[0];
    }

    /// <summary>
    /// Returns the width of this CompositeTextLine.
    /// </summary>
    /// <returns>the width.</returns>
    public float GetWidth() {
        return (current[X] - position[X]);
    }

    /// <summary>
    /// Draws this line on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        float xMax = 0f;
        float yMax = 0f;
        // Loop through all the text lines and draw them on the page
        foreach (TextLine textLine in textLines) {
            float[] xy = textLine.DrawOn(page);
            xMax = Math.Max(xMax, xy[0]);
            yMax = Math.Max(yMax, xy[1]);
        }
        return new float[] {xMax, yMax};
    }
}   // End of CompositeTextLine.cs
}   // End of namespace PDFjet.NET
