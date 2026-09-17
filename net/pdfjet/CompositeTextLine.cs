/*
 * CompositeTextLine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

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
    // The font size each component had when it was added. It is the base of
    // the script size and offset of that component when this composite text
    // line has no font size of its own.
    private List<float> fontSizes = new List<float>();

    private float[] position = new float[2];
    private float[] current  = new float[2];

    // Subscript and Superscript size factors
    private float subscriptFactor   = 0.583f;
    private float superscriptFactor = 0.583f;

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
        LayoutComponents();
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
        this.superscriptFactor = superscript;
        LayoutComponents();
        return this;
    }

    /// <summary>
    /// Gets the superscript factor for this text line.
    /// </summary>
    /// <returns>superscript the superscript size factor.</returns>
    public float GetSuperscriptFactor() {
        return superscriptFactor;
    }

    /// <summary>
    /// Sets the subscript factor for this composite text line.
    /// </summary>
    /// <param name="subscript">the subscript size factor.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSubscriptFactor(float subscript) {
        this.subscriptFactor = subscript;
        LayoutComponents();
        return this;
    }

    /// <summary>
    /// Gets the subscript factor for this text line.
    /// </summary>
    /// <returns>subscript the subscript size factor.</returns>
    public float GetSubscriptFactor() {
        return subscriptFactor;
    }

    /// <summary>
    /// Sets the superscript position for this composite text line.
    /// </summary>
    /// <param name="superscriptPosition">the superscript position.</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine SetSuperscriptPosition(float superscriptPosition) {
        this.superscriptPosition = superscriptPosition;
        LayoutComponents();
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
        LayoutComponents();
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
        textLines.Add(component);
        fontSizes.Add(component.GetFontSize());
        Place(component, BaseFontSize(textLines.Count - 1));
        return this;
    }

    /// <summary>
    /// Adds the components of a chemical formula, in the specified font.
    ///
    /// The digits that follow an element or a closing bracket are subscripts,
    /// as the 2 of H2O and the 6, 12 and 6 of C6H12O6; a run of digits and
    /// signs after a circumflex is a superscript, as the charge of Ca^2+ and
    /// SO4^2-; and everything else is drawn on the baseline, including a digit
    /// that begins the formula, as the 2 of 2H2O.
    /// </summary>
    /// <param name="font">the font of the formula.</param>
    /// <param name="formula">the formula, for example "C6H12O6" or "SO4^2-".</param>
    /// <returns>this CompositeTextLine object.</returns>
    public CompositeTextLine AddFormula(Font font, string formula) {
        StringBuilder buf = new StringBuilder();
        int i = 0;
        while (i < formula.Length) {
            char ch = formula[i];
            if (ch == '^') {
                AddRun(font, buf, ScriptPosition.NORMAL);
                i++;
                while (i < formula.Length && IsDigitOrSign(formula[i])) {
                    buf.Append(formula[i]);
                    i++;
                }
                AddRun(font, buf, ScriptPosition.SUPERSCRIPT);
            } else if (IsDigit(ch) && FollowsAnElement(formula, i)) {
                AddRun(font, buf, ScriptPosition.NORMAL);
                while (i < formula.Length && IsDigit(formula[i])) {
                    buf.Append(formula[i]);
                    i++;
                }
                AddRun(font, buf, ScriptPosition.SUBSCRIPT);
            } else {
                buf.Append(ch);
                i++;
            }
        }
        AddRun(font, buf, ScriptPosition.NORMAL);
        return this;
    }

    // Adds what the buffer holds as one component, and empties the buffer.
    private void AddRun(Font font, StringBuilder buf, ScriptPosition scriptPosition) {
        if (buf.Length == 0) {
            return;
        }
        TextLine component = new TextLine(font, buf.ToString());
        component.SetScriptPosition(scriptPosition);
        buf.Length = 0;
        AddComponent(component);
    }

    private static bool IsDigit(char ch) {
        return ch >= '0' && ch <= '9';
    }

    private static bool IsDigitOrSign(char ch) {
        return IsDigit(ch) || ch == '+' || ch == '-';
    }

    // A digit is a subscript when it counts the atoms of the element or the
    // group before it, and plain text when it begins the formula.
    private static bool FollowsAnElement(string formula, int index) {
        if (index == 0) {
            return false;
        }
        char ch = formula[index - 1];
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == ')' || ch == ']';
    }

    // The base of the script size and offset of the component: the font size
    // of this composite text line, or the one the component came with.
    private float BaseFontSize(int index) {
        return (fontSize > 0f) ? fontSize : fontSizes[index];
    }

    // Places the component at the current position, at the size its script
    // position asks for, and moves the current position past it. The size goes
    // on the TextLine: DrawOn uses the line's own font size, so resizing the
    // shared Font here would have no effect.
    private void Place(TextLine component, float baseSize) {
        if (component.GetScriptPosition() == ScriptPosition.SUPERSCRIPT) {
            component.SetFontSize(baseSize * superscriptFactor);
            component.SetLocation(current[X], current[Y] - baseSize * superscriptPosition);
        } else if (component.GetScriptPosition() == ScriptPosition.SUBSCRIPT) {
            component.SetFontSize(baseSize * subscriptFactor);
            component.SetLocation(current[X], current[Y] + baseSize * subscriptPosition);
        } else {
            component.SetFontSize(baseSize);
            component.SetLocation(current[X], current[Y]);
        }
        current[X] += component.GetWidth();
    }

    // Places every component again, from the location of this composite text
    // line, after the location, the font size or a script setting changed.
    private void LayoutComponents() {
        current[X] = position[X];
        current[Y] = position[Y];
        for (int i = 0; i < textLines.Count; i++) {
            Place(textLines[i], BaseFontSize(i));
        }
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
        LayoutComponents();
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
        return new float[] {position[0], position[1]};
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
        // Each component is measured where it is drawn, with the font size it
        // is drawn at, which a script position makes smaller than the base.
        foreach (TextLine component in textLines) {
            float baseline = component.GetLocation()[1];
            float top = baseline - component.font.GetAscent(component.GetFontSize());
            float bottom = baseline + component.font.GetDescent(component.GetFontSize());
            if (top < min) {
                min = top;
            }
            if (bottom > max) {
                max = bottom;
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
        float width = 0f;
        foreach (TextLine component in textLines) {
            width += component.GetWidth();
        }
        return width;
    }

    /// <summary>
    /// Draws this line on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        // A composite text line with no component reaches its own location.
        float xMax = position[X];
        float yMax = position[Y];
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
