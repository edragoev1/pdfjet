/**
 * CompositeTextLine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * This class was designed and implemented by Jon T. Swanson, Ph.D.
 *
 * Used to create composite text line objects.
 */
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

    /**
     * Sets the font size.
     *
     * @param fontSize the font size.
     */
    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    /**
     * Gets the font size.
     *
     * @return fontSize the font size.
     */
    public float GetFontSize() {
        return fontSize;
    }

    /**
     * Sets the superscript factor for this composite text line.
     *
     * @param superscript the superscript size factor.
     */
    public void SetSuperscriptFactor(float superscript) {
        this.superscriptSizeFactor = superscript;
    }

    /**
     * Gets the superscript factor for this text line.
     *
     * @return superscript the superscript size factor.
     */
    public float GetSuperscriptFactor() {
        return superscriptSizeFactor;
    }

    /**
     * Sets the subscript factor for this composite text line.
     *
     * @param subscript the subscript size factor.
     */
    public void SetSubscriptFactor(float subscript) {
        this.subscriptSizeFactor = subscript;
    }

    /**
     * Gets the subscript factor for this text line.
     *
     * @return subscript the subscript size factor.
     */
    public float GetSubscriptFactor() {
        return subscriptSizeFactor;
    }

    /**
     * Sets the superscript position for this composite text line.
     *
     * @param superscriptPosition the superscript position.
     */
    public void SetSuperscriptPosition(float superscriptPosition) {
        this.superscriptPosition = superscriptPosition;
    }

    /**
     * Gets the superscript position for this text line.
     *
     * @return superscriptPosition the superscript position.
     */
    public float GetSuperscriptPosition() {
        return superscriptPosition;
    }

    /**
     * Sets the subscript position for this composite text line.
     *
     * @param subscriptPosition the subscript position.
     */
    public void SetSubscriptPosition(float subscriptPosition) {
        this.subscriptPosition = subscriptPosition;
    }

    /**
     * Gets the subscript position for this text line.
     *
     * @return subscriptPosition the subscript position.
     */
    public float GetSubscriptPosition() {
        return subscriptPosition;
    }

    /**
     * Add a new text line.
     *
     * Find the current font, current size and effects (normal, super or subscript)
     * Set the position of the component to the starting stored as current position
     * Set the size and offset based on effects
     * Set the new current position
     *
     * @param component the component.
     */
    public void AddComponent(TextLine component) {
        textLines.Add(component);
    }

    /**
     * Loop through all the text lines and reset their position based on
     * the new position set here.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    /**
     * Loop through all the text lines and reset their position based on
     * the new position set here.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    /**
     * Loop through all the text lines and reset their location based on
     * the new location set here.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Return the nth entry in the TextLine array.
     *
     * @param index the index of the nth element.
     * @return the text line at the specified index.
     */
    public TextLine GetTextLine(int index) {
        if (textLines == null || textLines.Count == 0) {
            return null;
        }
        if (index < 0 || index > textLines.Count - 1) {
            return null;
        }
        return textLines[index];
    }

    /**
     * Returns the number of text lines.
     *
     * @return the number of text lines.
     */
    public int Size() {
       return textLines.Count;
    }

    /**
     * Returns the vertical coordinates of the top left and bottom right corners
     * of the bounding box of this composite text line.
     *
     * @return the an array containing the vertical coordinates.
     */
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

    /**
     * Returns the height of this CompositeTextLine.
     *
     * @return the height.
     */
    public float GetHeight() {
        float[] minMax = GetMinMax();
        return minMax[1] - minMax[0];
    }

    /**
     * Returns the width of this CompositeTextLine.
     *
     * @return the width.
     */
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

    /**
     * Draws this line on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
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
