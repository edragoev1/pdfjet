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
    internal TextBox textBox;
    internal TextBlock textBlock;
    internal TextColumn textColumn;
    internal Point point;
    internal CompositeTextLine compositeTextLine;
    internal float width = 75f;     // TODO: Rename to cellWidth
    internal float topPadding = 2f;
    internal float bottomPadding = 2f;
    internal float leftPadding = 2f;
    internal float rightPadding = 2f;

    internal float lineWidth = 0f;  // TODO: Rename to borderWidth

    internal float[] backgroundColor;
    internal float[] textColor = new float[] {0f, 0f, 0f};
    internal float strokeWidth;
    internal float[] strokeColor;
    internal String strokeDashPattern = "[] 0";    // Solid

    internal int colspan = 1;

    // Cell properties
    // Colspan:
    // bits 0 to 15
    // Border:
    // bit 16 - top
    // bit 17 - bottom
    // bit 18 - left
    // bit 19 - right
    // Text Alignment:
    // bit 20
    // bit 21
    // Text Decoration:
    // bit 22 - underline
    // bit 23 - strikeout
    // Future use:
    // bits 24 to 31
    private uint properties = 0x00050001;   // Set only left and top borders!
    private String uri;
    private uint valign = Align.TOP;

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
    /// Sets the font for this cell.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetFont(Font font) {
        this.font = font;
        this.fontSize = font.GetSize();
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
    /// Sets the image inside this cell.
    /// </summary>
    /// <param name="image">the image.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetImage(Image image) {
        this.image = image;
        this.text = null;
        return this;
    }

    /// <summary>
    /// Sets the barcode inside this cell.
    /// </summary>
    /// <param name="barcode">the barcode.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBarcode(Barcode barcode) {
        this.barcode = barcode;
        this.text = null;
        return this;
    }

    /// <summary>
    /// Returns the cell image.
    /// </summary>
    /// <returns>the image.</returns>
    public Image GetImage() {
        return this.image;
    }

    /// <summary>
    /// Sets the point inside this cell.
    /// See the Point class and Example_09 for more information.
    /// </summary>
    /// <param name="point">the point.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetPoint(Point point) {
        this.point = point;
        return this;
    }

    /// <summary>
    /// Returns the cell point.
    /// </summary>
    /// <returns>the point.</returns>
    public Point GetPoint() {
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

    /// <summary>Sets the text box drawn in this cell.</summary>
    public Cell SetTextBox(TextBox textBox) {
        this.textBox = textBox;
        return this;
    }

    /// <summary>Sets the text block drawn in this cell.</summary>
    public Cell SetTextBlock(TextBlock textBlock) {
        this.textBlock = textBlock;
        return this;
    }

    /// <summary>Sets the text column drawn in this cell and widens the cell to fit it.</summary>
    public Cell SetTextColumn(TextColumn textColumn) {
        this.textColumn = textColumn;
        this.width = textColumn.GetWidth() + this.leftPadding + this.rightPadding;
        return this;
    }

    /// <summary>Returns the text column drawn in this cell.</summary>
    public TextColumn GetTextColumn() {
        return this.textColumn;
    }

    /// <summary>Sets the background color from an array of red, green and blue values.</summary>
    public Cell SetBackgroundColor(float[] rgbColor) {
        this.backgroundColor = rgbColor;
        return this;
    }

    /// <summary>
    /// Sets the width of this cell.
    /// </summary>
    /// <param name="width">the specified width.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetWidth(double width) {
        this.width = (float) width;
        if (textBox != null) {
            textBox.SetWidth(this.width - (this.leftPadding + this.rightPadding));
        } else if (textBlock != null) {
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
        if (textBox != null) {
            textBox.SetWidth(width);
            cellHeight = (textBox.DrawOn(null)[1] - textBox.y) + topPadding + bottomPadding;
        } else if (textBlock != null) {
            textBlock.SetWidth(width);
            cellHeight = (textBlock.DrawOn(null)[1] - textBlock.y) + topPadding + bottomPadding;
        } else if (textColumn != null) {
            cellHeight = (textColumn.DrawOn(null)[1] - textColumn.y) + topPadding + bottomPadding;
        } else if (image != null) {
            cellHeight = image.GetHeight() + topPadding + bottomPadding;
        } else if (barcode != null) {
            cellHeight = barcode.GetHeight() + topPadding + bottomPadding;
        } else if (text != null) {
            float fontHeight = font.GetHeight();
            if (fallbackFont != null && fallbackFont.GetHeight() > fontHeight) {
                fontHeight = fallbackFont.GetHeight();
            }
            cellHeight = fontHeight + topPadding + bottomPadding;
        }
        return cellHeight;
    }

    /// <summary>Sets the width of the cell borders.</summary>
    public Cell SetLineWidth(Int32 width) {
        SetLineWidth((float) width);
        return this;
    }

    /// <summary>Sets the width of the cell borders.</summary>
    public Cell SetLineWidth(float width) {
        this.lineWidth = width;
        return this;
    }

    /// <summary>Returns the width of the cell borders.</summary>
    public float GetLineWidth() {
        return this.lineWidth;
    }

    /// <summary>Sets the background color as a 0xRRGGBB value. Same as SetBackgroundColor.</summary>
    public Cell SetBgColor(int color) {
        SetBackgroundColor(color);
        return this;
    }

    /// <summary>Sets the background color as a 0xRRGGBB value. Same as SetBackgroundColor.</summary>
    public Cell SetFillColor(int color) {
        SetBackgroundColor(color);
        return this;
    }

    /// <summary>Sets the background color as a 0xRRGGBB value.</summary>
    public Cell SetBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        backgroundColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Returns the background color. Same as GetBackgroundColor.</summary>
    public float[] GetFillColor() {
        return this.backgroundColor;
    }

    /// <summary>Returns the text color.</summary>
    public float[] GetBrushColor() {
        return this.textColor;
    }

    /// <summary>Returns the background color.</summary>
    public float[] GetBackgroundColor() {
        return this.backgroundColor;
    }

    /// <summary>Sets the text color as a 0xRRGGBB value.</summary>
    public Cell SetTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetTextColor(r, g, b);
        return this;
    }

    /// <summary>Sets the text color from red, green and blue values between 0.0 and 1.0.</summary>
    public Cell SetTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the text color from an array of red, green and blue values.</summary>
    public Cell SetTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    /// <summary>Returns the text color.</summary>
    public float[] GetTextColor() {
        return this.textColor;
    }

    /// <summary>Sets the stroke width.</summary>
    public Cell SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /// <summary>Returns the stroke width.</summary>
    public float GetStrokeWidth() {
        return this.strokeWidth;
    }

    /// <summary>Sets the stroke color as a 0xRRGGBB value.</summary>
    public Cell SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetStrokeColor(r, g, b);
        return this;
    }

    /// <summary>Sets the stroke color from red, green and blue values between 0.0 and 1.0.</summary>
    public Cell SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Cell SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    /// <summary>Returns the stroke color.</summary>
    public float[] GetStrokeColor() {
        return this.strokeColor;
    }

    internal void SetProperties(uint properties) {
        this.properties = properties;
    }

    internal uint GetProperties() {
        return this.properties;
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
        if (visible) {
            this.properties |= border;
        } else {
            this.properties &= (~border & 0x00FFFFFF);
        }
        return this;
    }

    /// <summary>
    /// Returns the cell border object.
    /// </summary>
    /// <returns>the cell border object.</returns>
    public bool GetBorder(uint border) {
        return (this.properties & border) != 0;
    }

    /// <summary>
    /// Sets all cell borders.
    /// </summary>
    /// <param name="borders">true or false.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBorders(bool borders) {
        if (borders) {
            this.properties &= 0x00FFFFFF;
        } else {
            this.properties &= 0x00F0FFFF;
        }
        return this;
    }

    /// <summary>
    /// Sets the cell text alignment.
    /// </summary>
    /// <param name="alignment">the alignment code.
    /// Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.</param>
    public Cell SetTextAlignment(uint alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
        return this;
    }

    /// <summary>
    /// Returns the text alignment.
    /// </summary>
    /// <returns>the horizontal alignment code.</returns>
    public uint GetTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /// <summary>
    /// Sets the cell text vertical alignment.
    /// </summary>
    /// <param name="alignment">the alignment code.
    /// Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetVerTextAlignment(uint alignment) {
        this.valign = alignment;
        return this;
    }

    /// <summary>
    /// Returns the cell text vertical alignment.
    /// </summary>
    /// <returns>the vertical alignment code.</returns>
    public uint GetVerTextAlignment() {
        return this.valign;
    }

    /// <summary>
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    /// </summary>
    /// <param name="underline">the underline flag.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetUnderline(bool underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
        return this;
    }

    /// <summary>Returns true if the text is underlined.</summary>
    public bool GetUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /// <summary>Sets whether the text is struck out.</summary>
    public Cell SetStrikeout(bool strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
        return this;
    }

    /// <summary>Returns true if the text is struck out.</summary>
    public bool GetStrikeout() {
        return (properties & 0x00800000) != 0;
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
        } else if (textBox != null) {
            textBox.SetLocation(x + leftPadding, y + topPadding);
            textBox.SetWidth(w - (leftPadding + rightPadding));
            textBox.DrawOn(page);
        } else if (textBlock != null) {
            textBlock.SetLocation(x + leftPadding, y + topPadding);
            textBlock.SetWidth(w - (leftPadding + rightPadding));
            textBlock.DrawOn(page);
        } else if (textColumn != null) {
            textColumn.SetLocation(x + leftPadding, y + topPadding);
            textColumn.DrawOn(page);
        } else if (image != null) {
            if (GetTextAlignment() == Align.LEFT) {
                image.SetLocation(x + leftPadding, y + topPadding);
                image.DrawOn(page);
            } else if (GetTextAlignment() == Align.CENTER) {
                image.SetLocation((x + w/2f) - image.GetWidth()/2f, y + topPadding);
                image.DrawOn(page);
            } else if (GetTextAlignment() == Align.RIGHT) {
                image.SetLocation((x + w) - (image.GetWidth() + leftPadding), y + topPadding);
                image.DrawOn(page);
            }
        } else if (barcode != null) {
            try {
                if (GetTextAlignment() == Align.LEFT) {
                    barcode.DrawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                } else if (GetTextAlignment() == Align.CENTER) {
                    float barcodeWidth = barcode.DrawOn(null)[0];
                    barcode.DrawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                } else if (GetTextAlignment() == Align.RIGHT) {
                    float barcodeWidth = barcode.DrawOn(null)[0];
                    barcode.DrawOnPageAtLocation(page, (x + w) - (barcodeWidth + leftPadding), y + topPadding);
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        DrawBorders(page, x, y, w, h);
        if (point != null) {
            if (point.alignment == Alignment.LEFT) {
                point.x = x + 2*point.r;
            } else if (point.alignment == Alignment.RIGHT) {
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
        page.FillRect(x, y + lineWidth/2, cellW, cellH);
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
        page.SetPenWidth(lineWidth);
        float qWidth = lineWidth / 4;
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
        float xText;
        float yText;
        if (valign == Align.TOP) {
            yText = y + font.GetAscent(fontSize) + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + font.GetAscent(fontSize)/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.SetPenColor(strokeColor);
        if (GetTextAlignment() == Align.RIGHT) {
            if (compositeTextLine == null) {
                xText = (x + cellW) - (font.StringWidth(text) + this.rightPadding);
                page.AddBMC(StructElem.P, text, text);
                page.DrawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.AddEMC();
                if (GetUnderline()) {
                    UnderlineText(page, font, text, xText, yText);
                }
                if (GetStrikeout()) {
                    StrikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = (x + cellW) - (compositeTextLine.GetWidth() + this.rightPadding);
                compositeTextLine.SetLocation(xText, yText);
                page.AddBMC(StructElem.P, text, text);
                compositeTextLine.DrawOn(page);
                page.AddEMC();
            }
        } else if (GetTextAlignment() == Align.CENTER) {
            if (compositeTextLine == null) {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - font.StringWidth(text)) / 2);
                page.AddBMC(StructElem.P, text, text);
                page.DrawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.AddEMC();
                if (GetUnderline()) {
                    UnderlineText(page, font, text, xText, yText);
                }
                if (GetStrikeout()) {
                    StrikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - compositeTextLine.GetWidth()) / 2);
                compositeTextLine.SetLocation(xText, yText);
                page.AddBMC(StructElem.P, text, text);
                compositeTextLine.DrawOn(page);
                page.AddEMC();
            }
        } else if (GetTextAlignment() == Align.LEFT) {
            xText = x + this.leftPadding;
            if (compositeTextLine == null) {
                page.AddBMC(StructElem.P, text, text);
                page.DrawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.AddEMC();
                if (GetUnderline()) {
                    UnderlineText(page, font, text, xText, yText);
                }
                if (GetStrikeout()) {
                    StrikeoutText(page, font, text, xText, yText);
                }
            } else {
                compositeTextLine.SetLocation(xText, yText);
                page.AddBMC(StructElem.P, text, text);
                compositeTextLine.DrawOn(page);
                page.AddEMC();
            }
        } else {
            throw new Exception("Invalid Text Alignment!");
        }

        if (uri != null) {
            float w = (compositeTextLine != null) ?
                    compositeTextLine.GetWidth() : font.StringWidth(text);
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    (page.height - yText) - font.GetAscent(fontSize),
                    xText + w,
                    (page.height - yText) + font.GetDescent(fontSize),
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

    private void UnderlineText(
            Page page, Font font, String text, float x, float y) {
        page.AddBMC(StructElem.P, "underline", "underline");
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y + font.GetDescent());
        page.LineTo(x + font.StringWidth(text), y + font.GetDescent(fontSize));
        page.StrokePath();
        page.AddEMC();
    }

    private void StrikeoutText(
            Page page, Font font, String text, float x, float y) {
        page.AddBMC(StructElem.P, "strike out", "strike out");
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y - font.GetAscent()/3f);
        page.LineTo(x + font.StringWidth(text), y - font.GetAscent(fontSize)/3f);
        page.StrokePath();
        page.AddEMC();
    }

    /// <summary>Returns the text box drawn in this cell.</summary>
    public TextBox GetTextBox() {
        return this.textBox;
    }

    /// <summary>Returns the text block drawn in this cell.</summary>
    public TextBlock GetTextBlock() {
        return this.textBlock;
    }
}   // End of Cell.cs
}   // End of namespace PDFjet.NET
