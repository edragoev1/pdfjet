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
    internal IDrawable drawable;    // The image, barcode, text block, text column or other drawable
    internal Point point;
    internal float width = 75f;
    // The four paddings are the four bytes of one uint, 4 bytes instead of 16
    // for every cell. A padding is kept to the nearest quarter of a point,
    // between 0 and 63.75, which is more than the text of a cell needs.
    private uint padding;

    // The colors are packed 0xRRGGBB values, 4 bytes each instead of an array
    // for every cell; NO_COLOR marks a background or a border that is not set.
    internal const int NO_COLOR = -1;
    internal int backgroundColor = NO_COLOR;
    internal int textColor = 0x000000; // Black until it is set
    internal float borderWidth;
    internal int borderColor = NO_COLOR;

    private int colspan = 1;
    // The borders and the underline and strikeout of the text are the bits of
    // one uint, 4 bytes instead of 6 bools and the padding they need. The four
    // borders are the bits Border gives them; only the top and the left are
    // drawn unless SetBorder says otherwise.
    private const uint UNDERLINE = 0x00100000;
    private const uint STRIKEOUT = 0x00200000;
    // A cell that a table adds below another to hold the next line of its
    // wrapped text, which is the same table cell in a PDF/UA document.
    internal const uint CONTINUED = 0x00400000;
    internal uint properties = Border.TOP | Border.LEFT;
    private String uri;

    // Where the three alignments of a cell are in properties, three bits
    // each, and where the four paddings are in padding, a byte each.
    private const int MARKER_ALIGNMENT = 0;
    private const int TEXT_ALIGNMENT = 3;
    private const int VALIGN = 6;
    private const uint ALIGNMENT_BITS = 0x7;

    private const int TOP_PADDING = 0;
    private const int BOTTOM_PADDING = 8;
    private const int LEFT_PADDING = 16;
    private const int RIGHT_PADDING = 24;
    private const uint PADDING_BITS = 0xFF;
    // A padding is kept in quarters of a point.
    private const float PADDING_SCALE = 4f;

    // The alignment at the bits of properties.
    private Alignment AlignmentAt(int shift) {
        return (Alignment) ((properties >> shift) & ALIGNMENT_BITS);
    }

    // Keeps the alignment in the bits of properties.
    private void SetAlignmentAt(int shift, Alignment alignment) {
        properties = (properties & ~(ALIGNMENT_BITS << shift))
                | (((uint) alignment & ALIGNMENT_BITS) << shift);
    }

    // The padding at the byte of padding, in points.
    private float PaddingAt(int shift) {
        return ((padding >> shift) & PADDING_BITS) / PADDING_SCALE;
    }

    // Keeps the padding in the byte of padding, to the nearest quarter of a
    // point and between 0 and 63.75.
    private void SetPaddingAt(int shift, float points) {
        int quarters = (int) (points * PADDING_SCALE + 0.5f);
        if (quarters < 0) {
            quarters = 0;
        } else if (quarters > PADDING_BITS) {
            quarters = (int) PADDING_BITS;
        }
        padding = (padding & ~(PADDING_BITS << shift)) | ((uint) quarters << shift);
    }

    /// <summary>
    ///  Creates a cell object and sets the font.
    /// </summary>
    /// <param name="font">the font.</param>
    public Cell(Font font) : this(font, null) {
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
        SetPaddingAt(TOP_PADDING, 2f);
        SetPaddingAt(BOTTOM_PADDING, 2f);
        SetPaddingAt(LEFT_PADDING, 2f);
        SetPaddingAt(RIGHT_PADDING, 2f);
        SetAlignmentAt(MARKER_ALIGNMENT, Alignment.RIGHT);
        SetAlignmentAt(TEXT_ALIGNMENT, Alignment.LEFT);
        SetAlignmentAt(VALIGN, Alignment.TOP);
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
    /// Sets the drawable inside this cell and clears the cell text. A cell holds one drawable,
    /// so this replaces the image, barcode, text block or text column set before. The drawable
    /// is placed by its top left corner, at the padding, and aligned in the cell as the text is;
    /// it is measured with DrawOn(null). A text block gets the width of the cell.
    /// </summary>
    /// <param name="drawable">the drawable, for example a QRCode, an SVGImage or a Table.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetDrawable(IDrawable drawable) {
        this.drawable = drawable;
        this.text = null;
        return this;
    }

    /// <summary>Returns the drawable inside this cell, or null.</summary>
    public IDrawable GetDrawable() {
        return this.drawable;
    }

    /// <summary>
    /// Sets the image inside this cell and clears the cell text.
    /// </summary>
    /// <param name="image">the image.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetImage(Image image) {
        return SetDrawable(image);
    }

    /// <summary>
    /// Sets the barcode inside this cell and clears the cell text.
    /// </summary>
    /// <param name="barcode">the barcode.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBarcode(Barcode barcode) {
        return SetDrawable(barcode);
    }

    /// <summary>Returns the barcode drawn in this cell, or null when the cell holds none.</summary>
    public Barcode GetBarcode() {
        return drawable as Barcode;
    }

    /// <summary>
    /// Returns the cell image.
    /// </summary>
    /// <returns>the image, or null when the cell holds none.</returns>
    public Image GetImage() {
        return drawable as Image;
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
        SetAlignmentAt(MARKER_ALIGNMENT, alignment);
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
        // A composite text line is the content of the cell, in the drawable it
        // holds, and is drawn where the cell text would be. The cell text is
        // left as it is, and the composite is drawn instead of it.
        this.drawable = compositeTextLine;
        return this;
    }

    /// <summary>Returns the composite text line drawn in this cell.</summary>
    public CompositeTextLine GetCompositeTextLine() {
        return drawable as CompositeTextLine;
    }

    /// <summary>Sets the text block drawn in this cell and clears the cell text.</summary>
    public Cell SetTextBlock(TextBlock textBlock) {
        return SetDrawable(textBlock);
    }

    /// <summary>Sets the text column drawn in this cell, widens the cell to fit it and clears the cell text.</summary>
    public Cell SetTextColumn(TextColumn textColumn) {
        this.width = textColumn.GetWidth() + PaddingAt(LEFT_PADDING) + PaddingAt(RIGHT_PADDING);
        return SetDrawable(textColumn);
    }

    /// <summary>Returns the text column drawn in this cell, or null when the cell holds none.</summary>
    public TextColumn GetTextColumn() {
        return drawable as TextColumn;
    }

    /// <summary>Sets the background color from an array of red, green and blue values, or removes the background with null. The cell keeps each component to the nearest of 256 steps.</summary>
    public Cell SetBackgroundColor(float[] rgbColor) {
        this.backgroundColor = Util.ToPackedRGB(rgbColor);
        return this;
    }

    /// <summary>
    /// Sets the width of this cell.
    /// </summary>
    /// <param name="width">the specified width.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetWidth(float width) {
        this.width = width;
        if (drawable is TextBlock textBlock) {
            textBlock.SetWidth(this.width - (PaddingAt(LEFT_PADDING) + PaddingAt(RIGHT_PADDING)));
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
        SetPaddingAt(TOP_PADDING, padding);
        return this;
    }

    /// <summary>Returns the top padding.</summary>
    public float GetTopPadding() {
        return PaddingAt(TOP_PADDING);
    }

    /// <summary>
    /// Sets the bottom padding of this cell.
    /// </summary>
    /// <param name="padding">the bottom padding.</param>
    public Cell SetBottomPadding(float padding) {
        SetPaddingAt(BOTTOM_PADDING, padding);
        return this;
    }

    /// <summary>Returns the bottom padding.</summary>
    public float GetBottomPadding() {
        return PaddingAt(BOTTOM_PADDING);
    }

    /// <summary>
    /// Sets the left padding of this cell.
    /// </summary>
    /// <param name="padding">the left padding.</param>
    public Cell SetLeftPadding(float padding) {
        SetPaddingAt(LEFT_PADDING, padding);
        return this;
    }

    /// <summary>
    /// Sets the right padding of this cell.
    /// </summary>
    /// <param name="padding">the right padding.</param>
    public Cell SetRightPadding(float padding) {
        SetPaddingAt(RIGHT_PADDING, padding);
        return this;
    }

    /// <summary>
    /// Sets the top, bottom, left and right paddings of this cell.
    /// </summary>
    /// <param name="padding">the right padding.</param>
    public Cell SetPadding(float padding) {
        SetPaddingAt(TOP_PADDING, padding);
        SetPaddingAt(BOTTOM_PADDING, padding);
        SetPaddingAt(LEFT_PADDING, padding);
        SetPaddingAt(RIGHT_PADDING, padding);
        return this;
    }

    /// <summary>
    /// Returns the cell height.
    /// </summary>
    /// <returns>the cell height.</returns>
    public float GetHeight(float width) {
        float cellHeight = 0f;
        if (drawable is IBaselineDrawable) {
            // A line of text is drawn on the baseline of the cell text, so the
            // cell makes room for the ascent and the descent of both.
            cellHeight = Ascent() + Descent() + PaddingAt(TOP_PADDING) + PaddingAt(BOTTOM_PADDING);
        } else if ((text == null || text.Equals("")) && drawable != null) {   // The text is drawn first
            if (drawable is TextBlock textBlock) {
                textBlock.SetWidth(width);
            }
            cellHeight = Measure(drawable)[1] + PaddingAt(TOP_PADDING) + PaddingAt(BOTTOM_PADDING);
        } else if (text != null) {
            float fontHeight = font.GetBodyHeight(fontSize);
            if (fallbackFont != null && fallbackFont.GetBodyHeight(fontSize) > fontHeight) {
                fontHeight = fallbackFont.GetBodyHeight(fontSize);
            }
            cellHeight = fontHeight + PaddingAt(TOP_PADDING) + PaddingAt(BOTTOM_PADDING);
        }
        return cellHeight;
    }

    /// <summary>Sets the background color as a 0xRRGGBB value. Color.transparent removes the background.</summary>
    public Cell SetBackgroundColor(int color) {
        if (color == Color.transparent) {
            backgroundColor = NO_COLOR;
            return this;
        }
        backgroundColor = color & 0xFFFFFF;
        return this;
    }

    /// <summary>Returns the background color, or null if the cell has no background.</summary>
    public float[] GetBackgroundColor() {
        return (backgroundColor == NO_COLOR) ? null : Util.ToRGB(backgroundColor);
    }

    /// <summary>Sets the text color as a 0xRRGGBB value. Color.transparent leaves the text color unchanged.</summary>
    public Cell SetTextColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.textColor = color & 0xFFFFFF;
        return this;
    }

    /// <summary>Sets the text color from an array of red, green and blue values. The cell keeps each component to the nearest of 256 steps. A null color leaves the text color unchanged, as Color.transparent does.</summary>
    public Cell SetTextColor(float[] rgbColor) {
        if (rgbColor == null) {
            return this;
        }
        this.textColor = Util.ToPackedRGB(rgbColor);
        return this;
    }

    /// <summary>Returns the text color.</summary>
    public float[] GetTextColor() {
        return Util.ToRGB(textColor);
    }

    /// <summary>Sets the width of the cell borders.</summary>
    /// <param name="borderWidth">the width of the cell borders.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /// <summary>Returns the width of the cell borders.</summary>
    /// <returns>the width of the cell borders.</returns>
    public float GetBorderWidth() {
        return this.borderWidth;
    }

    /// <summary>Sets the border color as a 0xRRGGBB value. Color.transparent leaves the borders the color of the pen the page draws with.</summary>
    public Cell SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = NO_COLOR;
            return this;
        }
        this.borderColor = color & 0xFFFFFF;
        return this;
    }

    /// <summary>Sets the border color from an array of red, green and blue values. The cell keeps each component to the nearest of 256 steps.</summary>
    public Cell SetBorderColor(float[] rgbColor) {
        this.borderColor = Util.ToPackedRGB(rgbColor);
        return this;
    }

    /// <summary>Returns the border color, or null if it is not set.</summary>
    public float[] GetBorderColor() {
        return (borderColor == NO_COLOR) ? null : Util.ToRGB(borderColor);
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
            this.properties |= (border & Border.ALL);
        } else {
            this.properties &= ~(border & Border.ALL);
        }
        return this;
    }

    /// <summary>
    /// Returns the cell border object.
    /// </summary>
    /// <returns>the cell border object.</returns>
    public bool GetBorder(uint border) {
        return (properties & border & Border.ALL) != 0;
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
        SetAlignmentAt(TEXT_ALIGNMENT, alignment);
        return this;
    }

    /// <summary>
    /// Returns the text alignment.
    /// </summary>
    /// <returns>the horizontal alignment.</returns>
    public Alignment GetTextAlignment() {
        return AlignmentAt(TEXT_ALIGNMENT);
    }

    /// <summary>
    /// Sets the cell text vertical alignment.
    /// </summary>
    /// <param name="alignment">the alignment.
    /// Supported values: Alignment.TOP, Alignment.CENTER and Alignment.BOTTOM.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetVerticalAlignment(Alignment alignment) {
        SetAlignmentAt(VALIGN, alignment);
        return this;
    }

    /// <summary>
    /// Returns the cell text vertical alignment.
    /// </summary>
    /// <returns>the vertical alignment.</returns>
    public Alignment GetVerticalAlignment() {
        return AlignmentAt(VALIGN);
    }

    /// <summary>
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    /// </summary>
    /// <param name="underline">the underline flag.</param>
    /// <returns>this Cell object.</returns>
    public Cell SetUnderline(bool underline) {
        if (underline) {
            this.properties |= UNDERLINE;
        } else {
            this.properties &= ~UNDERLINE;
        }
        return this;
    }

    /// <summary>Returns true if the text is underlined.</summary>
    public bool GetUnderline() {
        return (this.properties & UNDERLINE) != 0;
    }

    /// <summary>Sets whether the text is struck out.</summary>
    public Cell SetStrikeout(bool strikeout) {
        if (strikeout) {
            this.properties |= STRIKEOUT;
        } else {
            this.properties &= ~STRIKEOUT;
        }
        return this;
    }

    /// <summary>Returns true if the text is struck out.</summary>
    public bool GetStrikeout() {
        return (this.properties & STRIKEOUT) != 0;
    }

    /// <summary>Sets the URI opened when this cell is clicked.</summary>
    public Cell SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>Returns the left padding.</summary>
    public float GetLeftPadding() {
        return PaddingAt(LEFT_PADDING);
    }

    /// <summary>Returns the right padding.</summary>
    public float GetRightPadding() {
        return PaddingAt(RIGHT_PADDING);
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
        if (backgroundColor != NO_COLOR) {
            DrawBackground(page, x, y, w, h);
        }

        if (drawable is IBaselineDrawable || (text != null && !text.Equals(""))) {
            // A line of text is drawn instead of the cell text, on its baseline.
            DrawText(page, x, y, w, h);
        } else if (drawable is TextBlock textBlock) {
            textBlock.SetLocation(x + PaddingAt(LEFT_PADDING), y + PaddingAt(TOP_PADDING));
            textBlock.SetWidth(w - (PaddingAt(LEFT_PADDING) + PaddingAt(RIGHT_PADDING)));
            textBlock.DrawOn(page);
        } else if (drawable != null) {
            if (GetTextAlignment() == Alignment.RIGHT) {
                float drawableWidth = Measure(drawable)[0];
                drawable.SetLocation((x + w) - (drawableWidth + PaddingAt(RIGHT_PADDING)), y + PaddingAt(TOP_PADDING));
            } else if (GetTextAlignment() == Alignment.CENTER) {
                float drawableWidth = Measure(drawable)[0];
                drawable.SetLocation((x + w/2f) - drawableWidth/2f, y + PaddingAt(TOP_PADDING));
            } else {
                drawable.SetLocation(x + PaddingAt(LEFT_PADDING), y + PaddingAt(TOP_PADDING));
            }
            drawable.DrawOn(page);
        }

        DrawBorders(page, x, y, w, h);
        if (point != null) {
            if (AlignmentAt(MARKER_ALIGNMENT) == Alignment.LEFT) {
                point.x = x + 2*point.r;
            } else if (AlignmentAt(MARKER_ALIGNMENT) == Alignment.RIGHT) {
                point.x = (x + w) - PaddingAt(RIGHT_PADDING)/2;
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
                        0f,     // Opacity
                        null,   // Title
                        null,   // Contents
                        point.GetURIAction(),
                        null,
                        null,
                        null,
                        null));
            }
            page.AddArtifactBMC();
            page.DrawPoint(point);
            page.AddEMC();
        }
    }

    // Returns the width and the height of the drawable: its corner when it is
    // placed at 0, 0 and measured without a page.
    internal static float[] Measure(IDrawable drawable) {
        drawable.SetLocation(0f, 0f);
        return drawable.DrawOn(null);
    }

    private void DrawBackground(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.AddArtifactBMC();
        page.SetBrushColor(backgroundColor);
        page.FillRect(x, y + borderWidth/2, cellW, cellH);
        page.AddEMC();
    }

    private void DrawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        if ((properties & Border.ALL) == 0) {
            return;     // Nothing to draw, so nothing to write.
        }
        page.AddArtifactBMC();
        if (borderColor != NO_COLOR) {
            page.SetPenColor(borderColor);
        }
        page.SetPenWidth(borderWidth);
        // Half the pen width, so that the corners of the borders close.
        float hWidth = borderWidth / 2;
        // The borders of a cell are the subpaths of one path, stroked once.
        if ((properties & Border.TOP) != 0) {
            page.MoveTo(x - hWidth, y);
            page.LineTo(x + cellW, y);
        }
        if ((properties & Border.BOTTOM) != 0) {
            page.MoveTo(x - hWidth, y + cellH);
            page.LineTo(x + cellW, y + cellH);
        }
        if ((properties & Border.LEFT) != 0) {
            page.MoveTo(x, y - hWidth);
            page.LineTo(x, y + cellH + hWidth);
        }
        if ((properties & Border.RIGHT) != 0) {
            page.MoveTo(x + cellW, y - hWidth);
            page.LineTo(x + cellW, y + cellH + hWidth);
        }
        page.StrokePath();
        page.AddEMC();
    }

    private void DrawText(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        float ascent = Ascent();
        float yText;
        if (AlignmentAt(VALIGN) == Alignment.TOP) {
            yText = y + ascent + PaddingAt(TOP_PADDING);
        } else if (AlignmentAt(VALIGN) == Alignment.CENTER) {
            yText = y + cellH/2 + ascent/2;
        } else if (AlignmentAt(VALIGN) == Alignment.BOTTOM) {
            yText = (y + cellH) - PaddingAt(BOTTOM_PADDING);
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        float xText;
        if (GetTextAlignment() == Alignment.RIGHT) {
            xText = (x + cellW) - (GetTextWidth() + PaddingAt(RIGHT_PADDING));
        } else if (GetTextAlignment() == Alignment.CENTER) {
            xText = x + PaddingAt(LEFT_PADDING) +
                    (((cellW - (PaddingAt(LEFT_PADDING) + PaddingAt(RIGHT_PADDING))) - GetTextWidth()) / 2);
        } else {
            // Alignment.LEFT, and Alignment.JUSTIFY, which a single line of text cannot use.
            xText = x + PaddingAt(LEFT_PADDING);
        }
        IBaselineDrawable line = drawable as IBaselineDrawable;
        if (line == null) {
            page.AddBDC(StructElem.P, text, text);
            page.DrawString(font, fallbackFont, fontSize, text, xText, yText,
                    page.PackedToRGB(textColor), null);
            page.AddEMC();
            if (GetUnderline()) {
                UnderlineText(page, xText, yText);
            }
            if (GetStrikeout()) {
                StrikeoutText(page, xText, yText);
            }
        } else {
            // A text line and a composite text line mark their own text.
            line.SetLocation(xText, yText);
            // The text lines of the composite mark their own text.
            line.DrawOn(page);
        }

        if (uri != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + GetTextWidth(),
                    yText + Descent(),
                    null,       // Vertices
                    null,       // Fill Color
                    0f,         // Opacity
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
        if (drawable is IBaselineDrawable line) {
            return line.GetWidth();
        }
        return font.StringWidth(fallbackFont, fontSize, text);
    }

    // How far above the baseline the cell draws: the ascent of its font, and
    // of the line of text it holds, whichever reaches higher.
    private float Ascent() {
        float ascent = font.GetAscent(fontSize);
        if (drawable is IBaselineDrawable line) {
            float lineAscent = line.GetAscent();
            if (lineAscent > ascent) {
                ascent = lineAscent;
            }
        }
        return ascent;
    }

    // How far below the baseline the cell draws: the descent of its font, and
    // of the line of text it holds, whichever reaches lower.
    private float Descent() {
        float descent = font.GetDescent(fontSize);
        if (drawable is IBaselineDrawable line) {
            float lineDescent = line.GetDescent();
            if (lineDescent > descent) {
                descent = lineDescent;
            }
        }
        return descent;
    }

    private void UnderlineText(Page page, float x, float y) {
        float descent = font.GetDescent(fontSize);
        // The line is decoration, and the text says what the cell holds.
        page.AddArtifactBMC();
        page.SetPenColor(textColor);
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y + descent);
        page.LineTo(x + GetTextWidth(), y + descent);
        page.StrokePath();
        page.AddEMC();
    }

    private void StrikeoutText(Page page, float x, float y) {
        float ascent = font.GetAscent(fontSize);
        page.AddArtifactBMC();
        page.SetPenColor(textColor);
        page.SetPenWidth(font.GetUnderlineThickness(fontSize));
        page.MoveTo(x, y - ascent/3f);
        page.LineTo(x + GetTextWidth(), y - ascent/3f);
        page.StrokePath();
        page.AddEMC();
    }

    /// <summary>Returns the text block drawn in this cell.</summary>
    public TextBlock GetTextBlock() {
        return drawable as TextBlock;
    }
}   // End of Cell.cs
}   // End of namespace PDFjet.NET
