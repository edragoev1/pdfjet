/**
 *  TextFrame.cs
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
using System.Text.RegularExpressions;
using System.Collections.Generic;


/**
 *  Please see Example_47
 *
 */
namespace PDFjet.NET {
public class TextFrame : IDrawable {

    private List<TextLine> paragraphs;
    private Font font;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private float paragraphLeading;
    private List<float[]> beginParagraphPoints;
    private bool drawBorder;


    public TextFrame(List<TextLine> paragraphs) {
        this.paragraphs = new List<TextLine>(paragraphs);
        this.font = paragraphs[0].font;
        this.leading = font.GetBodyHeight();
        this.paragraphLeading = 2*leading;
        this.beginParagraphPoints = new List<float[]>();
        Font fallbackFont = paragraphs[0].fallbackFont;
        if (fallbackFont != null && (fallbackFont.GetBodyHeight() > this.leading)) {
            this.leading = fallbackFont.GetBodyHeight();
        }
        this.paragraphs.Reverse();
    }


    public TextFrame SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }


    public TextFrame SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }


    public TextFrame SetWidth(float w) {
        this.w = w;
        return this;
    }


    public TextFrame SetWidth(double w) {
        return SetWidth((float) w);
    }


    public TextFrame SetHeight(float h) {
        this.h = h;
        return this;
    }


    public TextFrame SetHeight(double h) {
        return SetHeight((float) h);
    }


    public TextFrame SetLeading(float leading) {
        this.leading = leading;
        return this;
    }


    public TextFrame SetLeading(double leading) {
        return SetLeading((float) leading);
    }


    public TextFrame SetParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }


    public TextFrame SetParagraphLeading(double paragraphLeading) {
        return SetParagraphLeading((float) paragraphLeading);
    }


    public void SetParagraphs(List<TextLine> paragraphs) {
        this.paragraphs = paragraphs;
    }


    public List<TextLine> GetParagraphs() {
        return this.paragraphs;
    }


    public List<float[]> GetBeginParagraphPoints() {
        return this.beginParagraphPoints;
    }


    public void SetDrawBorder(bool drawBorder) {
        this.drawBorder = drawBorder;
    }


    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public float[] DrawOn(Page page) {
        float xText = x;
        float yText = y + font.ascent;

        while (paragraphs.Count > 0) {
            // The paragraphs are reversed so we can efficiently remove the first one:
            TextLine textLine = paragraphs[paragraphs.Count - 1];
            paragraphs.RemoveAt(paragraphs.Count - 1);
            textLine.SetLocation(xText, yText);
            beginParagraphPoints.Add(new float[] {xText, yText});
            while (true) {
                textLine = DrawLineOnPage(textLine, page);
                if (textLine.GetText().Equals("")) {
                    break;
                }
                yText = textLine.Advance(leading);
                if (yText + font.descent >= (y + h)) {
                    // The paragraphs are reversed so we can efficiently add new first paragraph:
                    paragraphs.Add(textLine);

                    if (page != null && drawBorder) {
                        Box box = new Box();
                        box.SetLocation(x, y);
                        box.SetSize(w, h);
                        box.DrawOn(page);
                    }

                    return new float[] {x + w, y + h};
                }
            }
            xText = x;
            yText += paragraphLeading;
        }

        if (page != null && drawBorder) {
            Box box = new Box();
            box.SetLocation(x, y);
            box.SetSize(w, h);
            box.DrawOn(page);
        }

        return new float[] {x + w, y + h};
    }

    private TextLine DrawLineOnPage(TextLine textLine, Page page) {
        StringBuilder sb1 = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();
        String[] tokens = Regex.Split(textLine.GetText(), @"\s+");
        bool testForFit = true;
        for (int i = 0; i < tokens.Length; i++) {
            String token = tokens[i] + Single.space;
            if (testForFit && textLine.GetStringWidth((sb1.ToString() + token).Trim()) < this.w) {
                sb1.Append(token);
            }
            else {
                if (testForFit) {
                    testForFit = false;
                }
                sb2.Append(token);
            }
        }
        textLine.SetText(sb1.ToString().Trim());
        if (page != null) {
            textLine.DrawOn(page);
        }

        textLine.SetText(sb2.ToString().Trim());
        return textLine;
    }

    public bool IsNotEmpty() {
        return paragraphs.Count > 0;
    }

}   // End of TextFrame.cs
}   // End of namespace PDFjet.NET
