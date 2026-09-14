/*
 * Cell.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create table cell objects.
/// See the Table class for more information.
/// </summary>
public class Cell {
    internal Font font;
    internal Font fallbackFont;
    internal float fontSize;
    internal String text;
    internal Image image;
    internal Barcode barcode;
    internal TextBlock textBlock;
    internal TextColumn textColumn;
    internal Point point;
    private Alignment markerAlignment = Alignment.RIGHT;
    internal CompositeTextLine compositeTextLine;
    internal float width = 75f;
    internal float topPadding = 2f;
    internal float bottomPadding = 2f;
    internal float leftPadding = 2f;
    internal float rightPadding = 2f;

    internal float[] backgroundColor;
    internal float[] textColor = new float[] {0f, 0f, 0f};
    internal float strokeWidth;
    internal float[] strokeColor;

    private int colspan = 1;
    // Only the top and left borders are drawn unless SetBorder says otherwise.
    private bool topBorder = true;
    private bool bottomBorder = false;
    private bool leftBorder = true;
    private bool rightBorder = false;
    private bool underline = false;
    private bool strikeout = false;
    private String uri;
    private Alignment textAlignment = Alignment.LEFT;
    private Alignment valign = Alignment.TOP;

    /// <summary>
    ///  Creates a cell object and sets the font.
    /// </summary>
    /// <param name="font">the font.</param>
    public Cell(Font font) {
        this.font = font;
        this.fontSize = font.GetSize();
        this.fallbackFont = font;
    }

    /// <summary>
    /// Creates a cell object and sets the font and the cell text.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <param name="text">the text.</param>
    public Cell(Font font, String text) {
        this.font = font;
        this.fontSize = font.GetSize();
        this.fallbackFont = font;
        this.text = text;
    }

    /// <summary>
    /// Sets the font for this cell. The font size does not change; set it with SetFontSize.
    /// The fallback font changes with the font, unless a different fallback font was set.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetFont(Font font) {
        if (this.fallbackFont == this.font) {
            this.fallbackFont = font;
        }
        this.font = font;
        return this;
    }

    /// <summary>
    /// Sets the fallback font for this cell.
    /// </summary>
    /// <param name="fallbackFont">the fallback font.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /// <summary>
    /// Returns the font used by this cell.
    /// </summary>
    /// <returns>the font.</returns>
    public Font GetFont() {
        return this.font;
    }

    /// <summary>
    /// Returns the fallback font used by this cell.
    /// </summary>
    /// <returns>the fallback font.</returns>
    public Font GetFallbackFont() {
        return this.fallbackFont;
    }

    /// <summary>
    /// Sets the cell text.
    /// </summary>
    /// <param name="text">the cell text.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetText(String text) {
        this.text = text;
        return this;
    }

    /// <summary>
    /// Returns the cell text.
    /// </summary>
    /// <returns>the cell text.</returns>
    public String GetText() {
        return this.text;
    }

    /// <summary>Sets the font size.</summary>
    public Cell SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>
    /// Sets the image inside this cell and clears the cell text.
    /// </summary>
    /// <param name="image">the image.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetImage(Image image) {
        this.image = image;
        this.text = null;
        return this;
    }

    /// <summary>
    /// Sets the barcode inside this cell and clears the cell text.
    /// </summary>
    /// <param name="barcode">the barcode.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBarcode(Barcode barcode) {
        this.barcode = barcode;
        this.text = null;
        return this;
    }

    /// <summary>Returns the barcode drawn in this cell.</summary>
    public Barcode GetBarcode() {
        return this.barcode;
    }

    /// <summary>
    /// Returns the cell image.
    /// </summary>
    /// <returns>the image.</returns>
    public Image GetImage() {
        return this.image;
    }

    /// <summary>
    /// Sets the marker drawn in this cell: a Point, placed at the left or the
    /// right of the cell and centered vertically.
    /// See the Point class and Example_09 for more information.
    /// </summary>
    /// <param name="point">the point.</param>
    /// <param name="alignment">Alignment.LEFT or Alignment.RIGHT.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetMarker(Point point, Alignment alignment) {
        this.point = point;
        this.markerAlignment = alignment;
        return this;
    }

    /// <summary>
    /// Returns the marker drawn in this cell.
    /// </summary>
    /// <returns>the point.</returns>
    public Point GetMarker() {
        return this.point;
    }

    /// <summary>Sets the composite text line drawn in this cell.</summary>
    public Cell SetCompositeTextLine(CompositeTextLine compositeTextLine) {
        this.compositeTextLine = compositeTextLine;
        return this;
    }

    /// <summary>Returns the composite text line drawn in this cell.</summary>
    public CompositeTextLine GetCompositeTextLine() {
        return this.compositeTextLine;
    }

    /// <summary>Sets the text block drawn in this cell and clears the cell text.</summary>
    public Cell SetTextBlock(TextBlock textBlock) {
        this.textBlock = textBlock;
        this.text = null;
        return this;
    }

    /// <summary>Sets the text column drawn in this cell, widens the cell to fit it and clears the cell text.</summary>
    public Cell SetTextColumn(TextColumn textColumn) {
        this.textColumn = textColumn;
        this.width = textColumn.GetWidth() + this.leftPadding + this.rightPadding;
        this.text = null;
        return this;
    }

    /// <summary>Returns the text column drawn in this cell.</summary>
    public TextColumn GetTextColumn() {
        return this.textColumn;
    }

    /// <summary>Sets the background color from an array of red, green and blue values, or removes the background with null.</summary>
    public Cell SetBackgroundColor(float[] rgbColor) {
        this.backgroundColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>
    /// Sets the width of this cell.
    /// </summary>
    /// <param name="width">the specified width.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetWidth(float width) {
        this.width = width;
        if (textBlock != null) {
            textBlock.SetWidth(this.width - (this.leftPadding + this.rightPadding));
        }
        return this;
    }

    /// <summary>
    /// Returns the cell width.
    /// </summary>
    /// <returns>the cell width.</returns>
    public float GetWidth() {
        return this.width;
    }

    /// <summary>
    /// Sets the top padding of this cell.
    /// </summary>
    /// <param name="padding">the top padding.</param>
    public Cell SetTopPadding(float padding) {
        this.topPadding = padding;
        return this;
    }

    /// <summary>Returns the top padding.</summary>
    public float GetTopPadding() {
        return this.topPadding;
    }

    /// <summary>
    /// Sets the bottom padding of this cell.
    /// </summary>
    /// <param name="padding">the bottom padding.</param>
    public Cell SetBottomPadding(float padding) {
        this.bottomPadding = padding;
        return this;
    }

    /// <summary>Returns the bottom padding.</summary>
    public float GetBottomPadding() {
        return this.bottomPadding;
    }

    /// <summary>
    /// Sets the left padding of this cell.
    /// </summary>
    /// <param name="padding">the left padding.</param>
    public Cell SetLeftPadding(float padding) {
        this.leftPadding = padding;
        return this;
    }

    /// <summary>
    /// Sets the right padding of this cell.
    /// </summary>
    /// <param name="padding">the right padding.</param>
    public Cell SetRightPadding(float padding) {
        this.rightPadding = padding;
        return this;
    }

    /// <summary>
    /// Sets the top, bottom, left and right paddings of this cell.
    /// </summary>
    /// <param name="padding">the right padding.</param>
    public Cell SetPadding(float padding) {
        this.topPadding = padding;
        this.bottomPadding = padding;
        this.leftPadding = padding;
        this.rightPadding = padding;
        return this;
    }

    /// <summary>
    /// Returns the cell height.
    /// </summary>
    /// <returns>the cell height.</returns>
    public float GetHeight(float width) {
        float cellHeight = 0f;
        if (textBlock != null) {
            textBlock.SetWidth(width);
            cellHeight = (textBlock.DrawOn(null)[1] - textBlock.y) + topPadding + bottomPadding;
        } else if (textColumn != null) {
            cellHeight = (textColumn.DrawOn(null)[1] - textColumn.y) + topPadding + bottomPadding;
        } else if (image != null) {
            cellHeight = image.GetHeight() + topPadding + bottomPadding;
        } else if (barcode != null) {
            cellHeight = barcode.GetHeight() + topPadding + bottomPadding;
        } else if (text != null) {
            float fontHeight = font.GetBodyHeight(fontSize);
            if (fallbackFont != null && fallbackFont.GetBodyHeight(fontSize) > fontHeight) {
                fontHeight = fallbackFont.GetBodyHeight(fontSize);
            }
            cellHeight = fontHeight + topPadding + bottomPadding;
        }
        return cellHeight;
    }

    /// <summary>Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.</summary>
    public Cell SetBackgroundColor(int color) {
        if (color == Color.transparent) {
            backgroundColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        backgroundColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Returns the background color, or null if the cell has no background.</summary>
    public float[] GetBackgroundColor() {
        return Util.CopyOf(this.backgroundColor);
    }

    /// <summary>Sets the text color as a 0xRRGGBB value. Color.transparent leaves the text color unchanged.</summary>
    public Cell SetTextColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the text color from an array of red, green and blue values.</summary>
    public Cell SetTextColor(float[] rgbColor) {
        this.textColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Returns the text color.</summary>
    public float[] GetTextColor() {
        return Util.CopyOf(this.textColor);
    }

    /// <summary>Sets the width of the cell borders.</summary>
    /// <param name="strokeWidth">the width of the cell borders.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBorderWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /// <summary>Returns the width of the cell borders.</summary>
    /// <returns>the width of the cell borders.</returns>
    public float GetBorderWidth() {
        return this.strokeWidth;
    }

    /// <summary>Sets the stroke color as a 0xRRGGBB value.</summary>
    public Cell SetBorderColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Cell SetBorderColor(float[] rgbColor) {
        this.strokeColor = Util.CopyOf(rgbColor);
        return this;
    }

    /// <summary>Returns the stroke color.</summary>
    public float[] GetBorderColor() {
        return Util.CopyOf(this.strokeColor);
    }

    /// <summary>
    /// Sets the column span private variable.
    /// </summary>
    /// <param name="colspan">the specified column span value.</param>
    public Cell SetColSpan(int colspan) {
        this.colspan = colspan;
        return this;
    }

    /// <summary>
    /// Returns the column span private variable value.
    /// </summary>
    /// <returns>the column span value.</returns>
    public int GetColSpan() {
        return this.colspan;
    }

    /// <summary>
    /// Sets the cell border object.
    /// </summary>
    /// <param name="border">the border object.</param>
    /// <param name="visible">true to show the border, false to hide it.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBorder(uint border, bool visible) {
        if ((border & Border.TOP) != 0) {
            this.topBorder = visible;
        }
        if ((border & Border.BOTTOM) != 0) {
            this.bottomBorder = visible;
        }
        if ((border & Border.LEFT) != 0) {
            this.leftBorder = visible;
        }
        if ((border & Border.RIGHT) != 0) {
            this.rightBorder = visible;
        }
        return this;
    }

    /// <summary>
    /// Returns the cell border object.
    /// </summary>
    /// <returns>the cell border object.</returns>
    public bool GetBorder(uint border) {
        return ((border & Border.TOP) != 0 && topBorder) ||
                ((border & Border.BOTTOM) != 0 && bottomBorder) ||
                ((border & Border.LEFT) != 0 && leftBorder) ||
                ((border & Border.RIGHT) != 0 && rightBorder);
    }

    /// <summary>
    /// Sets all cell borders.
    /// </summary>
    /// <param name="borders">true or false.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBorders(bool borders) {
        return SetBorder(Border.ALL, borders);
    }

    /// <summary>
    /// Sets the cell text alignment.
    /// </summary>
    /// <param name="alignment">the alignment.
    /// Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY,
    /// which draws the single line of cell text left aligned.</param>
    public Cell SetTextAlignment(Alignment alignment) {
        this.textAlignment = alignment;
        return this;
    }

    /// <summary>
    /// Returns the text alignment.
    /// </summary>
    /// <returns>the horizontal alignment.</returns>
    public Alignment GetTextAlignment() {
        return this.textAlignment;
    }

    /// <summary>
    /// Sets the cell text vertical alignment.
    /// </summary>
    /// <param name="alignment">the alignment.
    /// Supported values: Alignment.TOP, Alignment.CENTER and Alignment.BOTTOM.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetVerticalAlignment(Alignment alignment) {
        this.valign = alignment;
        return this;
    }

    /// <summary>
    /// Returns the cell text vertical alignment.
    /// </summary>
    /// <returns>the vertical alignment.</returns>
    public Alignment GetVerticalAlignment() {
        return this.valign;
    }

    /// <summary>
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    /// </summary>
    /// <param name="underline">the underline flag.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetUnderline(bool underline) {
        this.underline = underline;
        return this;
    }

    /// <summary>Returns true if the text is underlined.</summary>
    public bool GetUnderline() {
        return this.underline;
    }

    /// <summary>Sets whether the text is struck out.</summary>
    public Cell SetStrikeout(bool strikeout) {
        this.strikeout = strikeout;
        return this;
    }

    /// <summary>Returns true if the text is struck out.</summary>
    public bool GetStrikeout() {
        return this.strikeout;
    }

    /// <summary>Sets the URI opened when this cell is clicked.</summary>
    public Cell SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>Returns the left padding.</summary>
    public float GetLeftPadding() {
        return this.leftPadding;
    }

    /// <summary>Returns the right padding.</summary>
    public float GetRightPadding() {
        return this.rightPadding;
    }

    /// <summary>
    /// Draws the point, text and borders of this cell.
    /// </summary>
    internal void DrawOn(
            Page page,
            float x,
            float y,
            float w,
            float h) {
        if (backgroundColor != null) {
            DrawBackground(page, x, y, w, h);
        }

        if (text != null && !text.Equals("")) {
            DrawText(page, x, y, w, h);
        } else if (textBlock != null) {
            textBlock.SetLocation(x + leftPadding, y + topPadding);
            textBlock.SetWidth(w - (leftPadding + rightPadding));
            textBlock.DrawOn(page);
        } else if (textColumn != null) {
            textColumn.SetLocation(x + leftPadding, y + topPadding);
            textColumn.DrawOn(page);
        } else if (image != null) {
            if (GetTextAlignment() == Alignment.RIGHT) {
                image.SetLocation((x + w) - (image.GetWidth() + rightPadding), y + topPadding);
            } else if (GetTextAlignment() == Alignment.CENTER) {
                image.SetLocation((x + w/2f) - image.GetWidth()/2f, y + topPadding);
            } else {
                image.SetLocation(x + leftPadding, y + topPadding);
            }
            image.DrawOn(page);
        } else if (barcode != null) {
            try {
                if (GetTextAlignment() == Alignment.RIGHT) {
                    float barcodeWidth = barcode.DrawOn(null)[0];
                    barcode.DrawOnPageAtLocation(page, (x + w) - (barcodeWidth + rightPadding), y + topPadding);
                } else if (GetTextAlignment() == Alignment.CENTER) {
                    float barcodeWidth = barcode.DrawOn(null)[0];
                    barcode.DrawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                } else {
                    barcode.DrawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                }
            } catch (Exception e) {
                Console.Error.WriteLine(e.ToString());
            }
        }

        DrawBorders(page, x, y, w, h);
        if (point != null) {
            if (markerAlignment == Alignment.LEFT) {
                point.x = x + 2*point.r;
            } else if (markerAlignment == Alignment.RIGHT) {
                point.x = (x + w) - this.rightPadding/2;
            }
            point.y = y + h/2;
            page.SetBrushColor(point.GetFillColor());
            if (point.GetURIAction() != null) {
                page.AddAnnotation(new Annotation(
                        Annotation.Link,
                        point.x - point.r,
                        point.y - point.r,
                        point.x + point.r,
                        point.y + point.r,
                        null,   // Vertices
                        null,   // Fill Color
                        0f,     // Transparency
                        null,   // Title
                        null,   // Contents
                        point.GetURIAction(),
                        null,
                        null,
                        null,
                        null));
            }
            page.DrawPoint(point);
        }
    }

    private void DrawBackground(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.AddArtifactBMC();
        page.SetBrushColor(backgroundColor);
        page.FillRect(x, y + strokeWidth/2, cellW, cellH);
        page.AddEMC();
    }

    private void DrawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.AddArtifactBMC();
        page.SetPenColor(strokeColor);
        page.SetPenWidth(strokeWidth);
        float qWidth = strokeWidth / 4;
        if (GetBorder(Border.TOP)) {
            page.MoveTo(x - qWidth, y);
            page.LineTo(x + cellW, y);
            page.StrokePath();
        }
        if (GetBorder(Border.BOTTOM)) {
            page.MoveTo(x - qWidth, y + cellH);
            page.LineTo(x + cellW, y + cellH);
            page.StrokePath();
        }
        if (GetBorder(Border.LEFT)) {
            page.MoveTo(x, y - qWidth);
            page.LineTo(x, y + cellH + qWidth);
            page.StrokePath();
        }
        if (GetBorder(Border.RIGHT)) {
            page.MoveTo(x + cellW, y - qWidth);
            page.LineTo(x + cellW, y + cellH + qWidth);
            page.StrokePath();
        }
        page.AddEMC();
    }

    private void DrawText(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        float ascent = font.GetAscent(fontSize);
        float yText;
        if (valign == Alignment.TOP) {
            yText = y + ascent + this.topPadding;
        } else if (valign == Alignment.CENTER) {
            yText = y + cellH/2 + ascent/2;
        } else if (valign == Alignment.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.SetPenColor(strokeColor);
        float xText;
        if (GetTextAlignment() == Alignment.RIGHT) {
            xText = (x + cellW) - (GetTextWidth() + this.rightPadding);
        } else if (GetTextAlignment() == Alignment.CENTER) {
            xText = x + this.leftPadding +
                    (((cellW - (leftPadding + rightPadding)) - GetTextWidth()) / 2);
        } else {
            // Alignment.LEFT, and Alignment.JUSTIFY, which a single line of text cannot use.
            xText = x + this.leftPadding;
        }
        if (compositeTextLine == null) {
            page.AddBDC(StructElem.P, text, text);
            page.DrawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
            page.AddEMC();
            if (GetUnderline()) {
                UnderlineText(page, xText, yText);
            }
            if (GetStrikeout()) {
                StrikeoutText(page, xText, yText);
            }
        } else {
            compositeTextLine.SetLocation(xText, yText);
            // The text lines of the composite mark their own text.
            compositeTextLine.DrawOn(page);
        }

        if (uri != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + GetTextWidth(),
                    yText + font.GetDescent(fontSize),
                    null,       // Vertices
                    null,       // Fill Color
                    0f,         // Transparency
                    null,       // Title
                    null,       // Contents
                    uri,
                    null,
                    null,
                    null,
                    null));
        }
    }

    // Returns the width of the composite text line, or of the cell text drawn
    // with the font and the fallback font at the font size of this cell.
    private float GetTextWidth() {
        if (compositeTextLine != null) {
            return compositeTextLine.GetWidth();
        }
        return font.StringWidth(fallbackFont, fontSize, text);
    }

    private void UnderlineText(Page page, float x, float y) {
        float descent = font.GetDescent(fontSize);
        page.AddBDC(StructElem.P, "underline", "underline");
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y + descent);
        page.LineTo(x + GetTextWidth(), y + descent);
        page.StrokePath();
        page.AddEMC();
    }

    private void StrikeoutText(Page page, float x, float y) {
        float ascent = font.GetAscent(fontSize);
        page.AddBDC(StructElem.P, "strike out", "strike out");
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y - ascent/3f);
        page.LineTo(x + GetTextWidth(), y - ascent/3f);
        page.StrokePath();
        page.AddEMC();
    }

    /// <summary>Returns the text block drawn in this cell.</summary>
    public TextBlock GetTextBlock() {
        return this.textBlock;
    }
}   // End of Cell.cs
}   // End of namespace PDFjet.NET
