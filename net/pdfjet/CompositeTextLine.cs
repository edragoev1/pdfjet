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
    private float x = 0f;
    private float y = 0f;

    private List<TextLine> textLines = new List<TextLine>();

    // Subscript and Superscript size factors
    private float subscriptSizeFactor   = 0.583f;
    private float superscriptSizeFactor = 0.583f;

    // Subscript and Superscript positions in relation to the base font
    private float superscriptPosition = 0.350f;
    private float subscriptPosition   = 0.141f;

    private float fontSize = 0f;

    public CompositeTextLine(float x, float y) {
        this.x = x;
        this.y = y;
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
    public void AddComponent(TextLine component) {
        textLines.Add(component);
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
        this.x = x;
        this.y = y;
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
    /// Returns the number of text lines.
    /// </summary>
    /// <returns>the number of text lines.</returns>
    public int Size() {
        return textLines.Count;
    }

    /// <summary>
    /// Returns the vertical coordinates of the top left and bottom right corners
    /// of the bounding box of this composite text line.
    /// </summary>
    /// <returns>the an array containing the vertical coordinates.</returns>
    public float[] GetMinMax() {
        float min = this.y;
        float max = this.y;
        float cur;

        foreach (TextLine textLine in textLines) {
            textLine.SetFontSize(fontSize);
            if (textLine.GetTextEffect() == Effect.SUPERSCRIPT) {
                cur = this.y - (textLine.font.GetSize() + textLine.font.GetAscent(fontSize*superscriptSizeFactor));
                if (cur < min)
                    min = cur;
                textLine.SetFontSize(fontSize*superscriptSizeFactor);
            } else if (textLine.GetTextEffect() == Effect.SUBSCRIPT) {
                cur = this.y + (textLine.font.GetDescent() + textLine.font.GetDescent(fontSize*subscriptSizeFactor));
                if (cur > max)
                    max = cur;
                textLine.SetFontSize(fontSize*subscriptSizeFactor);
            } else {
                cur = this.y - textLine.font.GetAscent();
                if (cur < min)
                    min = cur;
                cur = this.y + textLine.font.GetDescent();
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
        float[] minMax = GetMinMax();
        return minMax[1] - minMax[0];
    }

    /// <summary>
    /// Returns the width of this CompositeTextLine.
    /// </summary>
    /// <returns>the width.</returns>
    public float GetWidth() {
        float width = 0f;

        foreach (TextLine textLine in textLines) {
            if (textLine.GetTextEffect() == Effect.SUPERSCRIPT) {
                textLine.SetFontSize(fontSize*superscriptSizeFactor);
            } else if (textLine.GetTextEffect() == Effect.SUBSCRIPT) {
                textLine.SetFontSize(fontSize*subscriptSizeFactor);
            } else {
                textLine.SetFontSize(fontSize);
            }
            width += textLine.GetWidth();
        }

        return width;
    }

    /// <summary>
    /// Draws this line on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    /// <exception cref="System.Exception"/>
    public float[] DrawOn(Page page) {
        float xMax = 0f;
        float yMax = 0f;

        if (textLines == null || textLines.Count == 0) {
            return new float[] {xMax, yMax};
        }

        float textLineX = this.x;
        float textLineY = this.y;
        foreach (TextLine textLine in textLines) {
            textLine.SetFontSize(fontSize);
            if (textLine.GetTextEffect() == Effect.SUPERSCRIPT) {
                textLine.SetLocation(
                        textLineX,
                        textLineY - fontSize*superscriptPosition);
                textLine.SetFontSize(fontSize*superscriptSizeFactor);
            } else if (textLine.GetTextEffect() == Effect.SUBSCRIPT) {
                textLine.SetLocation(
                        textLineX,
                        textLineY + fontSize*subscriptPosition);
                textLine.SetFontSize(fontSize*subscriptSizeFactor);
            } else {
                textLine.SetLocation(textLineX, textLineY);
            }
            textLineX += textLine.GetWidth();

            float[] xy = textLine.DrawOn(page);
            xMax = Math.Max(xMax, xy[0]);
            yMax = Math.Max(yMax, xy[1]);
        }

        return new float[] {xMax, yMax};
    }
}   // End of CompositeTextLine.cs
}   // End of namespace PDFjet.NET
