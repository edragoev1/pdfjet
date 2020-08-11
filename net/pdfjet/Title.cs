/**
 *  Title.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;


namespace PDFjet.NET {
/**
 * Please see Example_51 and Example_52
 *
 */
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
