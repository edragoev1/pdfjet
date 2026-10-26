/*
 * SimpleBarChart.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// A small chart of horizontal bars that compares a few values: a name at the
/// left of each bar, the value at its end, and no axis, grid or legend, on a
/// light panel, with a title and a subtitle at the top. One bar can be
/// highlighted, in navy, its name and value bold, as the best of the values.
/// Each bar is as long as its share of the largest value.
///
/// It is meant to stand beside a Table of the same numbers: the chart for the
/// picture, the table for the record and for a screen reader. In a PDF/UA
/// document the chart is one figure, described from its title and its values
/// unless an alternate description is set. For grouped, stacked or vertical
/// bars with an axis, use BarChart.
/// </summary>
public class SimpleBarChart : IDrawable {
    // The colors of a simple bar chart unless set: a light panel, gray bars,
    // the highlighted bar navy, the names dark and the values gray.
    internal const int PANEL = 0xF1F3F6;
    internal const int BAR = 0xA7AFB8;
    internal const int HIGHLIGHT = 0x1F3A5F;
    internal const int INK = 0x22262B;
    internal const int MUTED = 0x5F6873;

    private readonly Font f1;   // The title, and the bold name and value
    private readonly Font f2;   // The rest
    private float x;
    private float y;
    private float w = 300f;
    private String title = "";
    private String subtitle = "";
    private String altDescription = "";
    private readonly List<String> names = new List<String>();
    private readonly List<float> values = new List<float>();
    private int highlight = -1;     // The index of the highlighted bar, or -1
    private int minFractionDigits = 0;
    private int maxFractionDigits = 2;
    private int panelColor = PANEL;
    private int barColor = BAR;
    private int highlightColor = HIGHLIGHT;

    /// <summary>Creates a simple bar chart, 300 points wide.</summary>
    /// <param name="f1">the font of the title and of the highlighted bar's name and value, a bold one.</param>
    /// <param name="f2">the font of the subtitle and of the other names and values.</param>
    public SimpleBarChart(Font f1, Font f2) {
        this.f1 = f1;
        this.f2 = f2;
    }

    /// <summary>Sets the title, drawn at the top left of the chart.</summary>
    /// <param name="title">the title.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetTitle(String title) {
        this.title = title;
        return this;
    }

    /// <summary>
    /// Sets the subtitle, drawn small and gray under the title: the unit of the
    /// values, say.
    /// </summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of the chart in a PDF/UA document.
    /// Without one the chart is described from its title, its subtitle and its
    /// values.
    /// </summary>
    /// <param name="altDescription">the alternate description.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Adds a bar: its name and its value. A value below zero is drawn as no
    /// bar, with its value written.
    /// </summary>
    /// <param name="name">the name of the bar.</param>
    /// <param name="value">its value.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart Add(String name, float value) {
        names.Add(name);
        values.Add(value);
        return this;
    }

    /// <summary>
    /// Highlights the bar of the index, the first bar 0: it is drawn in the
    /// highlight color, its name and value in the bold font. Which bar is the
    /// best, the lowest value or the highest, is the caller's to say.
    /// </summary>
    /// <param name="index">the index of the bar.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetHighlight(int index) {
        this.highlight = index;
        return this;
    }

    /// <summary>
    /// Sets the number of digits after the decimal point of the values, all of
    /// them alike. Without it a value has as many as it needs, two at most.
    /// </summary>
    /// <param name="digits">the number of digits.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetFractionDigits(int digits) {
        this.minFractionDigits = digits;
        this.maxFractionDigits = digits;
        return this;
    }

    /// <summary>
    /// Sets the color of the panel behind the chart, or -1 for none. It is
    /// light gray, 0xF1F3F6, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetPanelColor(int color) {
        this.panelColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the bars that are not highlighted. It is gray,
    /// 0xA7AFB8, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetBarColor(int color) {
        this.barColor = color;
        return this;
    }

    /// <summary>
    /// Sets the color of the highlighted bar and of its name. It is navy,
    /// 0x1F3A5F, unless set.
    /// </summary>
    /// <param name="color">the color.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetHighlightColor(int color) {
        this.highlightColor = color;
        return this;
    }

    /// <summary>Sets the top left corner of the chart.</summary>
    /// <param name="x">the x coordinate.</param>
    /// <param name="y">the y coordinate.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the width of the chart. Its height follows from the fonts and the
    /// number of bars; see GetHeight.
    /// </summary>
    /// <param name="w">the width.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>
    /// Returns the height of the chart: its padding, its title and subtitle,
    /// and a row for each bar.
    /// </summary>
    /// <returns>the height of the chart.</returns>
    public float GetHeight() {
        return Top() + values.Count * RowHeight() + Pad();
    }

    // The space around the chart's content, a size of the text of the bars.
    private float Pad() {
        return f2.GetSize();
    }

    // The height of a row, a bar and the space under it.
    internal float RowHeight() {
        return 2.4f * f2.GetSize();
    }

    // The size of the subtitle.
    private float SubtitleSize() {
        return 0.8f * f2.GetSize();
    }

    // The height above the first row: the padding, the title and the subtitle.
    private float Top() {
        float top = Pad();
        if (title.Length > 0) {
            top += f1.GetAscent(f1.GetSize()) + f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            top += 1.4f * SubtitleSize();
        }
        if (title.Length > 0 || subtitle.Length > 0) {
            top += f2.GetSize();
        }
        return top;
    }

    // The value written at the end of a bar.
    internal String Label(float value) {
        return Chart.Format(value, minFractionDigits, maxFractionDigits);
    }

    // The font of the name and the value of the bar of the index.
    private Font FontOf(int index) {
        return index == highlight ? f1 : f2;
    }

    // The alternate description: the one set, or the title, the subtitle and
    // the values.
    internal String AltDescription() {
        if (altDescription.Length > 0) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(title);
        if (subtitle.Length > 0) {
            if (sb.Length > 0) {
                sb.Append(", ");
            }
            sb.Append(subtitle);
        }
        if (sb.Length == 0) {
            sb.Append("Bar chart");
        }
        for (int i = 0; i < names.Count; i++) {
            sb.Append(i == 0 ? ": " : ", ");
            sb.Append(names[i]).Append(' ').Append(Label(values[i]));
        }
        return sb.Append('.').ToString();
    }

    /// <summary>Draws the chart on the page.</summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the chart.</returns>
    public float[] DrawOn(Page page) {
        float h = GetHeight();
        if (page == null || values.Count == 0) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        float size = f2.GetSize();
        float pad = Pad();

        // The widest name and value, and the room left for the bars
        float nameWidth = 0f;
        float valueWidth = 0f;
        float largest = 0f;
        for (int i = 0; i < names.Count; i++) {
            Font font = FontOf(i);
            nameWidth = Math.Max(nameWidth, font.StringWidth(size, names[i]));
            valueWidth = Math.Max(valueWidth, font.StringWidth(size, Label(values[i])));
            if (BarChart.IsValue(values[i])) {
                largest = Math.Max(largest, values[i]);
            }
        }
        float barX = x + pad + nameWidth + pad;
        float barRoom = Math.Max(x + w - pad - (valueWidth + size / 2f) - barX, 0f);

        page.AddBDC(StructElem.FIGURE, null, AltDescription());
        page.SaveGraphicsState();

        if (panelColor >= 0) {
            page.SetBrushColor(panelColor);
            page.FillRect(x, y, w, h);
        }
        float baseline = y + pad;
        if (title.Length > 0) {
            baseline += f1.GetAscent(f1.GetSize());
            page.DrawString(f1, f1.GetSize(), title, x + pad, baseline, Util.ToRGB(INK), null);
            baseline += f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            baseline += 1.4f * SubtitleSize();
            page.DrawString(f2, SubtitleSize(), subtitle, x + pad, baseline, Util.ToRGB(MUTED), null);
        }

        float rowTop = y + Top();
        float barHeight = 1.6f * size;
        float ascent = f2.GetAscent(size);
        for (int i = 0; i < names.Count; i++) {
            Font font = FontOf(i);
            float value = values[i];
            float top = rowTop + i * RowHeight();
            float textBaseline = top + (barHeight + ascent - f2.GetDescent(size)) / 2f;
            bool highlighted = i == highlight;
            page.DrawString(font, size, names[i], x + pad, textBaseline,
                    Util.ToRGB(highlighted ? highlightColor : INK), null);
            float length = 0f;
            if (largest > 0f && BarChart.IsValue(value) && value > 0f) {
                length = barRoom * (value / largest);
            }
            if (length > 0f) {
                page.SetBrushColor(highlighted ? highlightColor : barColor);
                page.FillRect(barX, top, Math.Max(length, 1.5f), barHeight);
            }
            page.DrawString(font, size, Label(value), barX + length + size / 2f, textBaseline,
                    Util.ToRGB(highlighted ? INK : MUTED), null);
        }

        page.RestoreGraphicsState();
        page.SetFigureBoundingBox(x, y, w, h);
        page.AddEMC();
        return new float[] {x + w, y + h};
    }
}   // End of SimpleBarChart.cs
}   // End of namespace PDFjet.NET
