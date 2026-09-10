/*
 * Title.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Please see Example_51 and Example_52
/// </summary>
public class Title : IDrawable {
    public TextLine prefix = null;
    public TextLine textLine = null;

    public Title(Font font, String title, float x, float y) {
        this.prefix = new TextLine(font);
        this.prefix.SetLocation(x, y);
        this.textLine = new TextLine(font, title);
        this.textLine.SetLocation(x, y);
    }

    public Title SetPrefix(String text) {
        prefix.SetText(text);
        return this;
    }

    public Title SetOffset(float offset) {
        textLine.SetLocation(textLine.x + offset, textLine.y);
        return this;
    }

    public Title SetLocation(float x, float y) {
        prefix.SetLocation(x, y);
        textLine.SetLocation(x, y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    public float[] DrawOn(Page page) {
        if (!prefix.Equals("")) {
            prefix.DrawOn(page);
        }
        return textLine.DrawOn(page);
    }
}
}   // End of namespace PDFjet.NET
