/**
 *  CompositeTextLine.cs
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 *  This class was designed and implemented by Jon T. Swanson, Ph.D.
 *
 *  Refactored and integrated into the project by Eugene Dragoev - 2 June 2012.
 *  Used to create composite text line objects.
 */
public class CompositeTextLine : IDrawable {

    private const int X = 0;
    private const int Y = 1;

    private List<TextLine> textLines = new List<TextLine>();

    private float[] position = new float[2];
    private float[] current  = new float[2];

    // Subscript and Superscript size factors
    private float subscriptSizeFactor    = 0.583f;
    private float superscriptSizeFactor  = 0.583f;

    // Subscript and Superscript positions in relation to the base font
    private float superscriptPosition = 0.350f;
    private float subscriptPosition   = 0.141f;

    private float fontSize = 0f;


    public CompositeTextLine(float x, float y) {
        position[X] = x;
        position[Y] = y;
        current[X]  = x;
        current[Y]  = y;
    }


    /**
     *  Sets the font size.
     *
     *  @param fontSize the font size.
     */
    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }


    /**
     *  Gets the font size.
     *
     *  @return fontSize the font size.
     */
    public float GetFontSize() {
        return fontSize;
    }


    /**
     *  Sets the superscript factor for this composite text line.
     *
     *  @param superscript the superscript size factor.
     */
    public void SetSuperscriptFactor(float superscript) {
        this.superscriptSizeFactor = superscript;
    }


    /**
     *  Gets the superscript factor for this text line.
     *
     *  @return superscript the superscript size factor.
     */
    public float GetSuperscriptFactor() {
        return superscriptSizeFactor;
    }


    /**
     *  Sets the subscript factor for this composite text line.
     *
     *  @param subscript the subscript size factor.
     */
    public void SetSubscriptFactor(float subscript) {
        this.subscriptSizeFactor = subscript;
    }


    /**
     *  Gets the subscript factor for this text line.
     *
     *  @return subscript the subscript size factor.
     */
    public float GetSubscriptFactor() {
        return subscriptSizeFactor;
    }


    /**
     *  Sets the superscript position for this composite text line.
     *
     *  @param superscriptPosition the superscript position.
     */
    public void SetSuperscriptPosition(float superscriptPosition) {
        this.superscriptPosition = superscriptPosition;
    }


    /**
     *  Gets the superscript position for this text line.
     *
     *  @return superscriptPosition the superscript position.
     */
    public float GetSuperscriptPosition() {
        return superscriptPosition;
    }


    /**
     *  Sets the subscript position for this composite text line.
     *
     *  @param subscriptPosition the subscript position.
     */
    public void SetSubscriptPosition(float subscriptPosition) {
        this.subscriptPosition = subscriptPosition;
    }


    /**
     *  Gets the subscript position for this text line.
     *
     *  @return subscriptPosition the subscript position.
     */
    public float GetSubscriptPosition() {
        return subscriptPosition;
    }


    /**
     *  Add a new text line.
     *
     *  Find the current font, current size and effects (normal, super or subscript)
     *  Set the position of the component to the starting stored as current position
     *  Set the size and offset based on effects
     *  Set the new current position
     *
     *  @param component the component.
     */
    public void AddComponent(TextLine component) {
        if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
            if (fontSize > 0f) {
                component.GetFont().SetSize(fontSize * superscriptSizeFactor);
            }
            component.SetLocation(
                    current[X],
                    current[Y] - fontSize * superscriptPosition);
        }
        else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
            if (fontSize > 0f) {
                component.GetFont().SetSize(fontSize * subscriptSizeFactor);
            }
            component.SetLocation(
                    current[X],
                    current[Y] + fontSize * subscriptPosition);
        }
        else {
            if (fontSize > 0f) {
                component.GetFont().SetSize(fontSize);
            }
            component.SetLocation(current[X], current[Y]);
        }
        current[X] += component.GetWidth();
        textLines.Add(component);
    }


    /**
     *  Loop through all the text lines and reset their position based on
     *  the new position set here.
     *
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    /**
     *  Loop through all the text lines and reset their position based on
     *  the new position set here.
     *
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }


    /**
     *  Loop through all the text lines and reset their location based on
     *  the new location set here.
     *
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    public void SetLocation(float x, float y) {
        position[X] = x;
        position[Y] = y;
        current[X]  = x;
        current[Y]  = y;

        if (textLines == null || textLines.Count == 0) {
            return;
        }

        foreach (TextLine component in textLines) {
            if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
                component.SetLocation(
                        current[X],
                        current[Y] - fontSize * superscriptPosition);
            }
            else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
                component.SetLocation(
                        current[X],
                        current[Y] + fontSize * subscriptPosition);
            }
            else {
                component.SetLocation(current[X], current[Y]);
            }
            current[X] += component.GetWidth();
        }
    }


    /**
     *  Return the position of this composite text line.
     *
     *  @return the position of this composite text line.
     */
    public float[] GetPosition() {
        return position;
    }


    /**
     *  Return the nth entry in the TextLine array.
     *
     *  @param index the index of the nth element.
     *  @return the text line at the specified index.
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
     *  Returns the number of text lines.
     *
     *  @return the number of text lines.
     */
    public int Size() {
       return textLines.Count;
    }


    /**
     *  Returns the vertical coordinates of the top left and bottom right corners
     *  of the bounding box of this composite text line.
     *
     *  @return the an array containing the vertical coordinates.
     */
    public float[] GetMinMax() {
        float min = position[Y];
        float max = position[Y];
        float cur;

        foreach (TextLine component in textLines) {
            if (component.GetTextEffect() == Effect.SUPERSCRIPT) {
                cur = (position[Y] - component.font.ascent) - fontSize * superscriptPosition;
                if (cur < min)
                    min = cur;
            }
            else if (component.GetTextEffect() == Effect.SUBSCRIPT) {
                cur = (position[Y] + component.font.descent) + fontSize * subscriptPosition;
                if (cur > max)
                    max = cur;
            }
            else {
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


    /**
     *  Returns the height of this CompositeTextLine.
     *
     *  @return the height.
     */
    public float GetHeight() {
        float[] yy = GetMinMax();
        return yy[1] - yy[0];
    }


    /**
     *  Returns the width of this CompositeTextLine.
     *
     *  @return the width.
     */
    public float GetWidth() {
        return (current[X] - position[X]);
    }


    /**
     *  Draws this line on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
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
