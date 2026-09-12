/*
 * TextFrame.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A frame that draws as much of its paragraphs as fits, so text flows from
/// frame to frame. Please see Example_47.
/// </summary>
public class TextFrame : IDrawable {
    private Font f1;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private bool border;
    private int borderColor = Color.blue;
    private List<List<string>> paragraphs;

    /// <summary>Creates a text frame from a list of paragraphs.</summary>
    public TextFrame(Font f1, List<string> inputList) {
        this.f1 = f1;
        this.leading = f1.GetAscent() + f1.GetDescent();
        List<string> list = new List<string>(inputList);
        list.Reverse();
        paragraphs = new List<List<string>>();
        foreach (string text in list) {
            List<string> tokens = new List<string>(Util.SplitOnWhitespace(text));
            tokens.Reverse();
            paragraphs.Add(tokens);
        }
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the location of the top left corner of this text frame.</summary>
    public TextFrame SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of the top left corner of this text frame.</summary>
    public TextFrame SetLocation(double x, double y) {
        return SetLocation((float)x, (float)y);
    }

    /// <summary>Sets the width of this text frame.</summary>
    public TextFrame SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>Sets the width of this text frame.</summary>
    public TextFrame SetWidth(double w) {
        return SetWidth((float)w);
    }

    /// <summary>Sets the height of this text frame.</summary>
    public TextFrame SetHeight(float h) {
        this.h = h;
        return this;
    }

    /// <summary>Sets the height of this text frame.</summary>
    public TextFrame SetHeight(double h) {
        return SetHeight((float)h);
    }

    /// <summary>Returns the width of this text frame.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Returns the height of this text frame.</summary>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>Sets whether a border is drawn around this text frame.</summary>
    public TextFrame SetBorder(bool border) {
        this.border = border;
        return this;
    }

    /// <summary>Sets the border color as a 0xRRGGBB value.</summary>
    public TextFrame SetBorderColor(int borderColor) {
        this.borderColor = borderColor;
        return this;
    }

    /// <summary>Returns true if some of the text has not been drawn yet.</summary>
    public bool HasMoreText() {
        return paragraphs.Count > 0;
    }

    private void DrawBorder(Page page) {
        if (border) {
            Rect rect = new Rect(x, y, w, h);
            rect.SetBorderColor(borderColor);
            rect.DrawOn(page);
        }
    }

    /// <summary>
    /// Draws as much of the text as fits in this frame on the page.
    /// Call HasMoreText to check whether text is left for another frame.
    /// The page must not be null.
    /// </summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            throw new ArgumentNullException(nameof(page), "Page cannot be null");
        }

        float yText = y + f1.GetAscent();
        while (paragraphs.Count > 0) {
            List<string> tokens = paragraphs[paragraphs.Count - 1];
            paragraphs.RemoveAt(paragraphs.Count - 1);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (tokens.Count > 0) {
                if (yText + f1.GetDescent() < (y + h)) {
                    string token = tokens[tokens.Count - 1];
                    tokens.RemoveAt(tokens.Count - 1);
                    if (f1.StringWidth(sb.ToString() + token) < this.w) {
                        sb.Append(token);
                        sb.Append(Single.space);
                    } else {
                        new TextLine(f1, Util.Trim(sb.ToString())).SetLocation(x, yText).DrawOn(page);
                        sb.Clear();
                        tokens.Add(token);
                        yText += leading;
                    }
                } else {
                    paragraphs.Add(tokens);
                    DrawBorder(page);
                    return new float[] { this.x + this.w, this.y + this.h };
                }
            }
            String line = Util.Trim(sb.ToString());
            if (line.Length > 0) {
                new TextLine(f1, line).SetLocation(x, yText).DrawOn(page);
                yText += leading;
            }
            yText += leading;
        }

        DrawBorder(page);
        return new float[] { this.x + this.w, this.y + this.h };
    }
}   // End of TextFrame.cs
}   // End of namespace PDFjet.NET
