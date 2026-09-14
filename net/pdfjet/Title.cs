/*
 * Title.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Please see Example_48
/// </summary>
public class Title : IDrawable {
    internal TextLine prefix = null;
    internal TextLine textLine = null;

    private float offset = 0f;

    /// <summary>Creates a title at the specified location.</summary>
    public Title(Font font, String title, float x, float y) {
        this.prefix = new TextLine(font);
        this.prefix.SetLocation(x, y);
        this.textLine = new TextLine(font, title);
        this.textLine.SetLocation(x, y);
    }

    /// <summary>Sets the prefix text.</summary>
    public Title SetPrefix(String text) {
        prefix.SetText(text);
        return this;
    }

    /// <summary>Sets the distance from the start of the prefix to the start of the title text, to make room for the prefix.</summary>
    public Title SetOffset(float offset) {
        this.offset = offset;
        textLine.SetLocation(prefix.x + offset, prefix.y);
        return this;
    }

    /// <summary>Sets the location of the prefix; the title text keeps its offset from it.</summary>
    public Title SetLocation(float x, float y) {
        prefix.SetLocation(x, y);
        textLine.SetLocation(x + offset, y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Returns the prefix drawn before the title text, such as a section number.</summary>
    public TextLine GetPrefix() {
        return prefix;
    }

    /// <summary>Returns the title text.</summary>
    public TextLine GetTextLine() {
        return textLine;
    }

    /// <summary>Draws the prefix and the title text on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (!string.IsNullOrEmpty(prefix.GetText())) {
            prefix.DrawOn(page);
        }
        return textLine.DrawOn(page);
    }
}
}   // End of namespace PDFjet.NET
