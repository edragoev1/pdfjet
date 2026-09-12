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
    /// <summary>The prefix drawn before the title text, such as a section number.</summary>
    public TextLine prefix = null;
    /// <summary>The title text.</summary>
    public TextLine textLine = null;

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

    /// <summary>Moves the title text right by the offset, to make room for the prefix.</summary>
    public Title SetOffset(float offset) {
        textLine.SetLocation(textLine.x + offset, textLine.y);
        return this;
    }

    /// <summary>Sets the location of this title.</summary>
    public Title SetLocation(float x, float y) {
        prefix.SetLocation(x, y);
        textLine.SetLocation(x, y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Draws the prefix and the title text on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (!prefix.Equals("")) {
            prefix.DrawOn(page);
        }
        return textLine.DrawOn(page);
    }
}
}   // End of namespace PDFjet.NET
