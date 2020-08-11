/**
 *  PlainText.cs
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
using System.Text;
using System.Collections.Generic;


namespace PDFjet.NET {
public class PlainText : IDrawable {

    private Font font;
    private String[] textLines;
    private float fontSize;
    private float x;
    private float y;
    private float w = 500f;
    private float leading;
    private int backgroundColor = Color.white;
    private int borderColor = Color.white;
    private int textColor = Color.black;
    private String language = null;
    private String altDescription = null;
    private String actualText = null;


    public PlainText(Font font, String[] textLines) {
        this.font = font;
        this.fontSize = font.GetSize();
        this.textLines = textLines;
        StringBuilder buf = new StringBuilder();
        foreach (String str in textLines) {
            buf.Append(str);
            buf.Append(' ');
        }
        this.altDescription = buf.ToString();
        this.actualText = buf.ToString();
    }


    public PlainText SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }


    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public void SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    public PlainText SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }


    public PlainText SetWidth(float w) {
        this.w = w;
        return this;
    }


    public PlainText SetLeading(float leading) {
        this.leading = leading;
        return this;
    }


    public PlainText SetBackgroundColor(int backgroundColor) {
        this.backgroundColor = backgroundColor;
        return this;
    }


    public PlainText SetBorderColor(int borderColor) {
        this.borderColor = borderColor;
        return this;
    }


    public PlainText SetTextColor(int textColor) {
        this.textColor = textColor;
        return this;
    }


    /**
     *  Draws this PlainText on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        float originalSize = font.GetSize();
        font.SetSize(fontSize);
        float yText = y + font.GetAscent();

        page.AddBMC(StructElem.SPAN, language, Single.space, Single.space);
        page.SetBrushColor(backgroundColor);
        leading = font.GetBodyHeight();
        float h = font.GetBodyHeight() * textLines.Length;
        page.FillRect(x, y, w, h);
        page.SetPenColor(borderColor);
        page.SetPenWidth(0f);
        page.DrawRect(x, y, w, h);
        page.AddEMC();

        page.AddBMC(StructElem.SPAN, language, altDescription, actualText);
        page.SetTextStart();
        page.SetTextFont(font);
        page.SetBrushColor(textColor);
        page.SetTextLeading(leading);
        page.SetTextLocation(x, yText);
        foreach (String str in textLines) {
            if (font.skew15) {
                SetTextSkew(page, 0.26f, x, yText);
            }
            page.Println(str);
            yText += leading;
        }
        page.SetTextEnd();
        page.AddEMC();

        font.SetSize(originalSize);

        return new float[] { x + w, y + h };
    }


    private void SetTextSkew(
            Page page, float skew, float x, float y) {
        page.Append(1f);
        page.Append(' ');
        page.Append(0f);
        page.Append(' ');
        page.Append(skew);
        page.Append(' ');
        page.Append(1f);
        page.Append(' ');
        page.Append(x);
        page.Append(' ');
        page.Append(page.height - y);
        page.Append(" Tm\n");
    }

}   // End of PlainText.cs
}   // End of namespace PDFjet.NET
