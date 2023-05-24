/**
 *  Title.cs
 *
Copyright 2023 Innovatics Inc.

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

/**
 * Please see Example_51 and Example_52
 *
 */
namespace PDFjet.NET {
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
        textLine.SetPosition(x, y);
        return this;
    }

    public void SetPosition(float x, float y) {
        textLine.SetPosition(x, y);
    }

    public float[] DrawOn(Page page) {
        if (!prefix.Equals("")) {
            prefix.DrawOn(page);
        }
        return textLine.DrawOn(page);
    }
}
}   // End of namespace PDFjet.NET
