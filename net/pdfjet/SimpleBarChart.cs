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
/// highlighted, in navy, its name and value bold, as the best of the values:
/// the bar of an index, or the lowest or the highest value, which the chart
/// finds. Each bar is as long as its share of the largest value. A dashed line
/// can mark a value across the bars, a target or an average, and the bars can
/// have rounded ends.
///
/// It is meant to stand beside a Table of the same numbers: the chart for the
/// picture, the table for the record and for a screen reader. In a PDF/UA
/// document the chart is one figure, described from its title and its values
/// unless an alternate description is set. For grouped, stacked or vertical
/// bars with an axis, use BarChart. Please see Example_20.
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
    // The bar the chart highlights itself: 0 none, -1 the lowest value, 1 the
    // highest
    private int highlightValue = 0;
    private float reference = float.NaN;    // The value of the reference line, or NaN
    private String referenceLabel = "";
    private bool roundedBars = false;
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
        this.title = title ?? "";
        return this;
    }

    /// <summary>
    /// Sets the subtitle, drawn small and gray under the title: the unit of the
    /// values, say.
    /// </summary>
    /// <param name="subtitle">the subtitle.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetSubtitle(String subtitle) {
        this.subtitle = subtitle ?? "";
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
        this.altDescription = altDescription ?? "";
        return this;
    }

    /// <summary>
    /// Adds a bar: its name and its value. A value below zero is drawn as no
    /// bar, with its value written. A value that is NaN or infinite has no
    /// bar and no value, as in the other charts, and the description leaves
    /// it out.
    /// </summary>
    /// <param name="name">the name of the bar.</param>
    /// <param name="value">its value.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart Add(String name, float value) {
        names.Add(name ?? "");
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
        this.highlightValue = 0;
        return this;
    }

    /// <summary>
    /// Highlights the bar of the lowest value, the best of times or prices;
    /// the first of them when two are lowest. Bars added later count too, as
    /// the chart finds the bar when it is drawn.
    /// </summary>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetHighlightLowest() {
        this.highlightValue = -1;
        return this;
    }

    /// <summary>
    /// Highlights the bar of the highest value, the best of sales or scores;
    /// the first of them when two are highest. Bars added later count too, as
    /// the chart finds the bar when it is drawn.
    /// </summary>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetHighlightHighest() {
        this.highlightValue = 1;
        return this;
    }

    /// <summary>
    /// Draws a thin dashed line across the bars at a value, a target or an
    /// average, with its label and value under the bars: "Target 50". The
    /// bars are scaled so that the line fits, should it be past the largest
    /// value. A value that is NaN or infinite draws no line.
    /// </summary>
    /// <param name="value">the value of the line.</param>
    /// <param name="label">the label written before the value, or "".</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetReferenceLine(float value, String label) {
        this.reference = value;
        this.referenceLabel = label ?? "";
        return this;
    }

    /// <summary>
    /// Draws the bars with slightly rounded ends. They are square unless set.
    /// </summary>
    /// <param name="roundedBars">true for rounded ends.</param>
    /// <returns>this chart.</returns>
    public SimpleBarChart SetRoundedBars(bool roundedBars) {
        this.roundedBars = roundedBars;
        return this;
    }

    // The index of the highlighted bar: the one set, or the one of the lowest
    // or highest value, or -1.
    internal int Highlighted() {
        if (highlightValue == 0) {
            return highlight;
        }
        int best = -1;
        for (int i = 0; i < values.Count; i++) {
            float value = values[i];
            if (!ChartScale.IsValue(value)) {
                continue;
            }
            if (best < 0 || (highlightValue < 0 ? value < values[best] : value > values[best])) {
                best = i;
            }
        }
        return best;
    }

    // True when a reference line is drawn.
    private bool HasReference() {
        return ChartScale.IsValue(reference);
    }

    // The text under the reference line: its label and its value, "Target 50",
    // or its value alone.
    internal String ReferenceText() {
        return referenceLabel.Length == 0 ? Label(reference) : referenceLabel + " " + Label(reference);
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
    /// Sets the color of the panel behind the chart. It is light gray,
    /// 0xF1F3F6, unless set. Color.transparent, -1, draws no panel: the one
    /// color setter where it does not leave the color as it was.
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
        if (f1 == null || f2 == null) {
            return 0f;      // Without its fonts the chart draws nothing
        }
        float h = Top() + values.Count * RowHeight() + Pad();
        if (HasReference()) {
            h += 1.4f * SubtitleSize();     // The line's label under the bars
        }
        return h;
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
        if (!ChartScale.IsValue(value)) {
            return "";      // No value, as in the other charts: it was written NaN or In,fin,ity
        }
        // Its digits grouped in thousands, 23,507, as in the other charts
        return ChartScale.GroupDigits(ChartScale.Format(value, minFractionDigits, maxFractionDigits));
    }

    // The font of the name and the value of the bar of the index.
    private Font FontOf(int index) {
        return index == Highlighted() ? f1 : f2;
    }

    // The alternate description: the one set, or the title, the subtitle and
    // the values.
    internal String AltDescription() {
        if (altDescription.Length > 0) {
            return altDescription;
        }
        StringBuilder sb = new StringBuilder(ChartScale.Join(title, subtitle));
        if (sb.Length == 0) {
            sb.Append("Bar chart");
        }
        String separator = PunctuationAfter(sb, ":") + " ";
        for (int i = 0; i < names.Count; i++) {
            if (!ChartScale.IsValue(values[i])) {
                continue;
            }
            sb.Append(separator).Append(names[i]).Append(' ').Append(Label(values[i]));
            separator = ", ";
        }
        if (HasReference()) {
            sb.Append("; ").Append(referenceLabel.Length == 0 ? "reference line at " + Label(reference) : ReferenceText());
        }
        return sb.Append(PunctuationAfter(sb, ".")).ToString();
    }

    // The mark to write after the text of a description, or nothing when the
    // text ends a sentence already, in ".", "?" or "!": a title "Sales." was
    // described as "Sales.: a 5", and in a line chart as "Sales., 2019 to
    // 2024:" (the review of 28 October 2026).
    internal static String PunctuationAfter(StringBuilder text, String mark) {
        if (text.Length > 0) {
            char end = text[text.Length - 1];
            if (end == '.' || end == '?' || end == '!') {
                return "";
            }
        }
        return mark;
    }

    /// <summary>Draws the chart on the page.</summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>the x and y coordinates of the bottom right corner of the chart.</returns>
    public float[] DrawOn(Page page) {
        float h = GetHeight();
        if (page == null || values.Count == 0 || f1 == null || f2 == null) {    // Measured, or nothing to draw
            return new float[] {x + w, y + h};
        }
        if (maxFractionDigits > ChartScale.MAX_DIGITS) {
            page.pdf.Fail(new ArgumentException(ChartScale.TOO_MANY_DIGITS));
        }
        float size = f2.GetSize();
        float pad = Pad();

        // The widest name and value, and the room left for the bars
        // A name wider than two fifths of the chart is shortened with an ellipsis
        List<String> shown = new List<String>();
        float nameWidth = 0f;
        float valueWidth = 0f;
        float largest = 0f;
        for (int i = 0; i < names.Count; i++) {
            valueWidth = Math.Max(valueWidth, FontOf(i).StringWidth(size, Label(values[i])));
            if (ChartScale.IsValue(values[i])) {
                largest = Math.Max(largest, values[i]);
            }
        }
        if (HasReference()) {
            largest = Math.Max(largest, reference);     // The line within the bars' room
        }
        // The names in what the values leave of the width, two fifths of it at
        // most: in a chart narrower than its values they were written past it
        float nameRoom = Math.Max(Math.Min(0.4f * (w - 2f * pad), w - 3f * pad - valueWidth - size / 2f), 0f);
        for (int i = 0; i < names.Count; i++) {
            Font font = FontOf(i);
            // No name where not even an ellipsis fits; the description has it
            shown.Add(nameRoom < font.StringWidth(size, "\u2026")
                    ? "" : ChartScale.Shorten(font, size, names[i], nameRoom));
            nameWidth = Math.Max(nameWidth, font.StringWidth(size, shown[i]));
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
            page.DrawString(f1, f1.GetSize(), ChartScale.Shorten(f1, f1.GetSize(), title, w - 2f * pad), x + pad, baseline, Util.ToRGB(INK), null);
            baseline += f1.GetDescent(f1.GetSize());
        }
        if (subtitle.Length > 0) {
            baseline += 1.4f * SubtitleSize();
            page.DrawString(f2, SubtitleSize(), ChartScale.Shorten(f2, SubtitleSize(), subtitle, w - 2f * pad),
                    x + pad, baseline, Util.ToRGB(MUTED), null);
        }

        float rowTop = y + Top();
        float barHeight = 1.6f * size;
        float ascent = f2.GetAscent(size);
        int highlight = Highlighted();
        for (int i = 0; i < names.Count; i++) {
            Font font = FontOf(i);
            float value = values[i];
            float top = rowTop + i * RowHeight();
            float textBaseline = top + (barHeight + ascent - f2.GetDescent(size)) / 2f;
            bool highlighted = i == highlight;
            page.DrawString(font, size, shown[i], x + pad, textBaseline,
                    Util.ToRGB(highlighted ? highlightColor : INK), null);
            float length = 0f;
            if (largest > 0f && ChartScale.IsValue(value) && value > 0f) {
                length = barRoom * (value / largest);
            }
            if (length > 0f) {
                page.SetBrushColor(highlighted ? highlightColor : barColor);
                float barW = Math.Max(length, 1.5f);
                // The radius a fifth of the bar's height, or half a short bar
                float r = Math.Min(barHeight / 5f, barW / 2f);
                if (roundedBars && r > 0.25f) {
                    page.FillRoundedRect(barX, top, barW, barHeight, r, r);
                } else {
                    page.FillRect(barX, top, barW, barHeight);
                }
            }
            page.DrawString(font, size, Label(value), barX + length + size / 2f, textBaseline,
                    Util.ToRGB(highlighted ? INK : MUTED), null);
        }

        // The reference line, dashed, over the bars, from a little above the
        // first to a little below the last, and its label under it. It leaves
        // out the rows where it would cross a bar's value.
        if (HasReference() && largest > 0f && reference >= 0f) {
            float lineX = barX + barRoom * (reference / largest);
            float lineBottom = rowTop + (names.Count - 1) * RowHeight() + barHeight + size / 4f;
            page.SetPenColor(INK);
            page.SetPenWidth(0.75f);
            page.SetStrokeDashPattern("[2 2] 0");
            float from = rowTop - size / 4f;
            for (int i = 0; i < names.Count; i++) {
                float value = values[i];
                float length = (ChartScale.IsValue(value) && value > 0f) ? barRoom * (value / largest) : 0f;
                float labelX = barX + length + size / 2f;
                float labelW = FontOf(i).StringWidth(size, Label(value));
                if (labelW > 0f && lineX > labelX - size / 2f && lineX < labelX + labelW + size / 2f) {
                    float top = rowTop + i * RowHeight();
                    if (top > from) {
                        page.DrawLine(lineX, from, lineX, top);
                    }
                    from = top + barHeight;
                }
            }
            if (lineBottom > from) {
                page.DrawLine(lineX, from, lineX, lineBottom);
            }
            page.SetDefaultStrokeDashPattern();
            String text = ReferenceText();
            float width = f2.StringWidth(SubtitleSize(), text);
            // Centered under the line, within the chart
            float textX = Math.Max(x + pad, Math.Min(lineX - width / 2f, x + w - pad - width));
            page.DrawString(f2, SubtitleSize(), text, textX, lineBottom + 1.2f * SubtitleSize(),
                    Util.ToRGB(INK), null);
        }

        page.RestoreGraphicsState();
        page.SetFigureBoundingBox(x, y, w, h);
        page.AddEMC();
        return new float[] {x + w, y + h};
    }
}   // End of SimpleBarChart.cs
}   // End of namespace PDFjet.NET
