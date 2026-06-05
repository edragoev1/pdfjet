/**
 * TextFrame.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/**
 * Please see Example_47
 */
namespace PDFjet.NET {
public class TextFrame : IDrawable {
    private List<TextLine> paragraphs;
    private Font font;
    private Font fallbackFont;
    private float fontSize;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private float paragraphLeading;
    private List<float[]> beginParagraphPoints;
    private bool border;

    public TextFrame(List<TextLine> paragraphs) {
        this.paragraphs = new List<TextLine>(paragraphs);
        this.font = paragraphs[0].font;
        this.fallbackFont = paragraphs[0].fallbackFont;
        this.fontSize = font.size;
        this.leading = font.GetBodyHeight(fontSize);
        this.paragraphLeading = 2*leading;
        this.beginParagraphPoints = new List<float[]>();
        if (fallbackFont != null && (fallbackFont.GetBodyHeight(fontSize) > this.leading)) {
            this.leading = fallbackFont.GetBodyHeight(fontSize);
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

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetBorder(bool border) {
        this.border = border;
    }

    public void SetDrawBorder(bool border) {
        this.border = border;
    }

    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    public float[] DrawOn(Page page) {
        float xText = x;
        float yText = y + font.GetAscent(fontSize);
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
                if (yText + font.GetDescent(fontSize) >= (y + h)) {
                    // The paragraphs are reversed so we can efficiently add new first paragraph:
                    paragraphs.Add(textLine);
                    DrawBorder(page);
                    return new float[] {x + w, y + h};
                }
            }
            xText = x;
            yText += paragraphLeading;
        }
        DrawBorder(page);
        return new float[] {x + w, y + h};
    }

    private void DrawBorder(Page page) {
        if (page != null && border) {
            (new Rect(x, y, w, h)).DrawOn(page);
        }
    }

    private TextLine DrawLineOnPage(TextLine textLine, Page page) {
        StringBuilder sb1 = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();
        String[] tokens = Regex.Split(textLine.GetText(), @"\s+");
        bool testForFit = true;
        foreach (String token in tokens) {
            if (testForFit && textLine.GetStringWidth(sb1.ToString() + token) < this.w) {
                sb1.Append(token + Single.space);
            } else {
                testForFit = false;
                sb2.Append(token + Single.space);
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
