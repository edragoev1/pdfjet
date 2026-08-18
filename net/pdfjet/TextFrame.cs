/**
 * TextFrame.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PDFjet.NET {
/**
 * Please see Example_47
 */
public class TextFrame : IDrawable {
    private Font f1 = null;
    private List<string> list;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private bool border;
    private int borderColor = Color.blue;
    private List<List<string>> paragraphs;

    public TextFrame(Font f1, List<string> list) {
        this.f1 = f1;
        this.list = new List<string>(list);
        this.leading = f1.GetAscent() + f1.GetDescent();
        this.list.Reverse();
        paragraphs = new List<List<string>>();
        foreach (string text in this.list) {
            String[] split = Regex.Split(text.Trim(), "\\s+");
            List<string> tokens = new List<string>();
            foreach (string token in split) {
                if (!string.IsNullOrEmpty(token)) { // Filter empty tokens
                    tokens.Add(token);
                }
            }
            tokens.Reverse();
            paragraphs.Add(tokens);
        }
    }

    public TextFrame SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public TextFrame SetLocation(double x, double y) {
        return SetLocation((float)x, (float)y);
    }

    public TextFrame SetWidth(float w) {
        this.w = w;
        return this;
    }

    public TextFrame SetWidth(double w) {
        return SetWidth((float)w);
    }

    public TextFrame SetHeight(float h) {
        this.h = h;
        return this;
    }

    public TextFrame SetHeight(double h) {
        return SetHeight((float)h);
    }

    public float GetHeight() {
        return this.h;
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetBorder(bool border) {
        this.border = border;
    }

    public void SetBorderColor(int borderColor) {
        this.borderColor = borderColor;
    }

    public float[] DrawOn(Page page) {
        if (page == null) {
            throw new NullReferenceException("Page cannot be null");
        }

        float yText = y + f1.GetAscent();
        while (paragraphs.Count > 0) {
            List<string> tokens = paragraphs[paragraphs.Count - 1];
            paragraphs.RemoveAt(paragraphs.Count - 1);
            TextLine textLine = null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string token = null;
            while (tokens.Count > 0) {
                if (yText + f1.GetDescent() < (y + h)) {
                    token = tokens[tokens.Count - 1];
                    tokens.RemoveAt(tokens.Count - 1);
                    if (f1.StringWidth(sb.ToString() + token) < this.w) {
                        sb.Append(token);
                        sb.Append(Single.space);
                    } else {
                        textLine = new TextLine(f1, sb.ToString().Trim());
                        textLine.SetLocation(x, yText);
                        textLine.DrawOn(page);
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
            if (!string.IsNullOrWhiteSpace(sb.ToString())) {
                textLine = new TextLine(f1, sb.ToString().Trim());
                textLine.SetLocation(x, yText);
                textLine.DrawOn(page);
                yText += leading;
            }
            yText += leading;
        }

        DrawBorder(page);
        return new float[] { this.x + this.w, this.y + this.h };
    }

    private void DrawBorder(Page page) {
        if (border) {
            Rect rect = new Rect(x, y, w, h);
            rect.SetBorderColor(borderColor);
            rect.DrawOn(page);
        }
    }

    public bool HasMoreText() {
        return paragraphs.Count > 0;
    }
}   // End of TextFrame.cs
}   // End of namespace PDFjet.NET
