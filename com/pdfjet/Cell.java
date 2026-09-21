/*
 * Cell.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.barcodes.*;

/**
 * Used to create table cell objects.
 * See the Table class for more information.
 */
public class Cell {
    /** The font of the cell text. */
    protected Font font;
    /** The font used for characters that the primary font does not have. */
    protected Font fallbackFont;
    /** The font size. */
    protected float fontSize;
    /** The cell text. */
    protected String text;
    /** The image, barcode, text block, text column or other drawable in this cell. */
    protected Drawable drawable;
    /** The point drawn in this cell. */
    protected Point point;
    /** The width of this cell. */
    protected float width = 75f;
    // The four paddings are the four bytes of one int, 4 bytes instead of 16
    // for every cell. A padding is kept to the nearest quarter of a point,
    // between 0 and 63.75, which is more than the text of a cell needs.
    private int padding;

    // The colors are packed 0xRRGGBB values, 4 bytes each instead of an array
    // for every cell; NO_COLOR marks a background or a border that is not set.
    static final int NO_COLOR = -1;
    /** The background color as a 0xRRGGBB value, or -1 when the cell has no background. */
    protected int backgroundColor = NO_COLOR;
    /** The text color as a 0xRRGGBB value; it is black until it is set. */
    protected int textColor = 0x000000;
    /** The width of the cell borders. */
    protected float borderWidth;
    /** The border color as a 0xRRGGBB value, or -1 when it is not set. */
    protected int borderColor = NO_COLOR;

    private int colspan = 1;
    private int rowspan = 1;
    // The rows of the table as it is drawn that the cell spans, which is its
    // row span with the rows the wrapped text of each of them needs.
    int rowsSpanned = 1;
    // The borders and the underline and strikeout of the text are the bits of
    // one int, 4 bytes instead of 6 booleans and the padding they need. The
    // four borders are the bits Border gives them; only the top and the left
    // are drawn unless setBorder says otherwise.
    private static final int UNDERLINE = 0x00100000;
    private static final int STRIKEOUT = 0x00200000;
    // A cell that a table adds below another to hold the next line of its
    // wrapped text, which is the same table cell in a PDF/UA document.
    static final int CONTINUED = 0x00400000;
    // A cell that the cell above it spans over, which draws nothing: the cell
    // that spans the rows draws its text, background and borders over it.
    static final int COVERED = 0x00800000;

    // Where the three alignments of a cell are in properties, three bits
    // each, and where the four paddings are in padding, a byte each.
    private static final int MARKER_ALIGNMENT = 0;
    private static final int TEXT_ALIGNMENT = 3;
    private static final int VALIGN = 6;
    private static final int ALIGNMENT_BITS = 0x7;

    private static final int TOP_PADDING = 0;
    private static final int BOTTOM_PADDING = 8;
    private static final int LEFT_PADDING = 16;
    private static final int RIGHT_PADDING = 24;
    private static final int PADDING_BITS = 0xFF;
    // A padding is kept in quarters of a point.
    private static final float PADDING_SCALE = 4f;

    // Alignment.values() copies its array on every call, so the alignments
    // are taken from one array that is made once.
    private static final Alignment[] ALIGNMENTS = Alignment.values();

    // The alignment at the bits of properties.
    private Alignment alignmentAt(int shift) {
        return ALIGNMENTS[(properties >> shift) & ALIGNMENT_BITS];
    }

    // Keeps the alignment in the bits of properties.
    private void setAlignmentAt(int shift, Alignment alignment) {
        properties = (properties & ~(ALIGNMENT_BITS << shift))
                | ((alignment.ordinal() & ALIGNMENT_BITS) << shift);
    }

    // The padding at the byte of padding, in points.
    private float paddingAt(int shift) {
        return ((padding >> shift) & PADDING_BITS) / PADDING_SCALE;
    }

    // Keeps the padding in the byte of padding, to the nearest quarter of a
    // point and between 0 and 63.75.
    private void setPaddingAt(int shift, float points) {
        int quarters = (int) (points * PADDING_SCALE + 0.5f);
        if (quarters < 0) {
            quarters = 0;
        } else if (quarters > PADDING_BITS) {
            quarters = PADDING_BITS;
        }
        padding = (padding & ~(PADDING_BITS << shift)) | (quarters << shift);
    }
    int properties = Border.TOP | Border.LEFT;
    private String uri;

    /**
     * Creates a cell object and sets the font.
     *
     * @param font the font.
     */
    public Cell(Font font) {
        this(font, null);
    }

    /**
     * Creates a cell object and sets the font and the cell text.
     *
     * @param font the font.
     * @param text the text.
     */
    public Cell(Font font, String text) {
        this.font = font;
        this.fontSize = font.getSize();
        this.fallbackFont = font;
        this.text = text;
        setPaddingAt(TOP_PADDING, 2f);
        setPaddingAt(BOTTOM_PADDING, 2f);
        setPaddingAt(LEFT_PADDING, 2f);
        setPaddingAt(RIGHT_PADDING, 2f);
        setAlignmentAt(MARKER_ALIGNMENT, Alignment.RIGHT);
        setAlignmentAt(TEXT_ALIGNMENT, Alignment.LEFT);
        setAlignmentAt(VALIGN, Alignment.TOP);
    }

    /**
     * Sets the font for this cell. The font size does not change; set it with setFontSize.
     * The fallback font changes with the font, unless a different fallback font was set.
     *
     * @param font the font.
     * @return this Cell object.
     */
    public Cell setFont(Font font) {
        if (this.fallbackFont == this.font) {
            this.fallbackFont = font;
        }
        this.font = font;
        return this;
    }

    /**
     * Sets the fallback font for this cell.
     *
     * @param fallbackFont the fallback font.
     * @return this Cell object.
     */
    public Cell setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     * Returns the font used by this cell.
     *
     * @return the font.
     */
    public Font getFont() {
        return this.font;
    }

    /**
     * Returns the fallback font used by this cell.
     *
     * @return the fallback font.
     */
    public Font getFallbackFont() {
        return this.fallbackFont;
    }

    /**
     * Sets the cell text.
     *
     * @param text the cell text.
     * @return this Cell object.
     */
    public Cell setText(String text) {
        this.text = text;
        return this;
    }

    /**
     * Returns the cell text.
     *
     * @return the cell text.
     */
    public String getText() {
        return this.text;
    }

    /**
     * Sets the font size of the cell text.
     *
     * @param fontSize the font size.
     * @return this Cell object.
     */
    public Cell setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the drawable inside this cell and clears the cell text. A cell holds
     * one drawable, so this replaces the image, barcode, text block or text
     * column set before. The drawable is placed by its top left corner, at the
     * padding, and aligned in the cell as the text is; it is measured with
     * drawOn(null). A text block gets the width of the cell.
     *
     * @param drawable the drawable, for example a QRCode, an SVGImage or a Table.
     * @return this Cell object.
     */
    public Cell setDrawable(Drawable drawable) {
        this.drawable = drawable;
        this.text = null;
        return this;
    }

    /**
     * Returns the drawable inside this cell.
     *
     * @return the drawable, or null.
     */
    public Drawable getDrawable() {
        return this.drawable;
    }

    /**
     * Sets the image inside this cell and clears the cell text.
     *
     * @param image the image.
     * @return this Cell object.
     */
    public Cell setImage(Image image) {
        return setDrawable(image);
    }

    /**
     * Sets the barcode inside this cell and clears the cell text.
     *
     * @param barcode the barcode.
     * @return this Cell object.
     */
    public Cell setBarcode(Barcode barcode) {
        return setDrawable(barcode);
    }

    /**
     * Returns the barcode drawn in this cell.
     *
     * @return the barcode, or null when the cell holds none.
     */
    public Barcode getBarcode() {
        return (drawable instanceof Barcode) ? (Barcode) drawable : null;
    }

    /**
     * Returns the cell image.
     *
     * @return the image, or null when the cell holds none.
     */
    public Image getImage() {
        return (drawable instanceof Image) ? (Image) drawable : null;
    }

    /**
     * Sets the marker drawn in this cell: a Point, placed at the left or the
     * right of the cell and centered vertically.
     * See the Point class and Example_09 for more information.
     *
     * @param point the point.
     * @param alignment Alignment.LEFT or Alignment.RIGHT.
     * @return this Cell object.
     */
    public Cell setMarker(Point point, Alignment alignment) {
        this.point = point;
        setAlignmentAt(MARKER_ALIGNMENT, alignment);
        return this;
    }

    /**
     * Returns the marker drawn in this cell.
     *
     * @return the point.
     */
    public Point getMarker() {
        return this.point;
    }

    /**
     * Sets the composite text object.
     *
     * @param compositeTextLine the composite text object.
     * @return this Cell object.
     */
    public Cell setCompositeTextLine(CompositeTextLine compositeTextLine) {
        // A composite text line is the content of the cell, in the drawable it
        // holds, and is drawn where the cell text would be. The cell text is
        // left as it is, and the composite is drawn instead of it.
        this.drawable = compositeTextLine;
        return this;
    }

    /**
     * Returns the composite text object.
     *
     * @return the composite text object.
     */
    public CompositeTextLine getCompositeTextLine() {
        return (drawable instanceof CompositeTextLine) ? (CompositeTextLine) drawable : null;
    }

    /**
     * Sets the text block drawn inside this cell and clears the cell text.
     *
     * @param textBlock the text block.
     * @return this Cell object.
     */
    public Cell setTextBlock(TextBlock textBlock) {
        return setDrawable(textBlock);
    }

    /**
     * Returns the text block drawn inside this cell.
     *
     * @return the text block, or null when the cell holds none.
     */
    public TextBlock getTextBlock() {
        return (drawable instanceof TextBlock) ? (TextBlock) drawable : null;
    }

    /**
     * Sets the text column drawn inside this cell, widens the cell to fit it
     * and clears the cell text.
     *
     * @param textColumn the text column.
     * @return this Cell object.
     */
    public Cell setTextColumn(TextColumn textColumn) {
        this.width = textColumn.getWidth() + paddingAt(LEFT_PADDING) + paddingAt(RIGHT_PADDING);
        return setDrawable(textColumn);
    }

    /**
     * Returns the text column drawn inside this cell.
     *
     * @return the text column, or null when the cell holds none.
     */
    public TextColumn getTextColumn() {
        return (drawable instanceof TextColumn) ? (TextColumn) drawable : null;
    }

    /**
     * Sets the width of this cell.
     *
     * @param width the specified width.
     * @return this Cell object.
     */
    public Cell setWidth(float width) {
        this.width = width;
        if (drawable instanceof TextBlock) {
            ((TextBlock) drawable).setWidth(this.width - (paddingAt(LEFT_PADDING) + paddingAt(RIGHT_PADDING)));
        }
        return this;
    }

    /**
     * Returns the cell width.
     *
     * @return the cell width.
     */
    public float getWidth() {
        return this.width;
    }

    /**
     * Sets the top padding of this cell.
     *
     * @param padding the top padding.
     * @return this Cell object.
     */
    public Cell setTopPadding(float padding) {
        setPaddingAt(TOP_PADDING, padding);
        return this;
    }

    /**
     * Returns the top padding of this cell.
     *
     * @return the top padding.
     */
    public float getTopPadding() {
        return paddingAt(TOP_PADDING);
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * @param padding the bottom padding.
     * @return this Cell object.
     */
    public Cell setBottomPadding(float padding) {
        setPaddingAt(BOTTOM_PADDING, padding);
        return this;
    }

    /**
     * Returns the bottom padding of this cell.
     *
     * @return the bottom padding.
     */
    public float getBottomPadding() {
        return paddingAt(BOTTOM_PADDING);
    }

    /**
     * Sets the left padding of this cell.
     *
     * @param padding the left padding.
     * @return this Cell object.
     */
    public Cell setLeftPadding(float padding) {
        setPaddingAt(LEFT_PADDING, padding);
        return this;
    }

    /**
     * Returns the left padding of this cell.
     *
     * @return the left padding.
     */
    public float getLeftPadding() {
        return paddingAt(LEFT_PADDING);
    }

    /**
     * Sets the right padding of this cell.
     *
     * @param padding the right padding.
     * @return this Cell object.
     */
    public Cell setRightPadding(float padding) {
        setPaddingAt(RIGHT_PADDING, padding);
        return this;
    }

    /**
     * Returns the right padding of this cell.
     *
     * @return the right padding.
     */
    public float getRightPadding() {
        return paddingAt(RIGHT_PADDING);
    }

    /**
     * Sets the top, bottom, left and right paddings of this cell.
     *
     * @param padding the right padding.
     * @return this Cell object.
     */
    public Cell setPadding(float padding) {
        setPaddingAt(TOP_PADDING, padding);
        setPaddingAt(BOTTOM_PADDING, padding);
        setPaddingAt(LEFT_PADDING, padding);
        setPaddingAt(RIGHT_PADDING, padding);
        return this;
    }

    /**
     * Sets the border color of this cell. Color.transparent leaves the borders
     * the color of the pen the page draws with.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = NO_COLOR;
            return this;
        }
        this.borderColor = color & 0xFFFFFF;
        return this;
    }

    /**
     * Sets the border color of this cell. The cell keeps each component to the
     * nearest of 256 steps.
     *
     * @param borderColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Cell object.
     */
    public Cell setBorderColor(float[] borderColor) {
        this.borderColor = Util.toPackedRGB(borderColor);
        return this;
    }

    /**
     * Returns the border color.
     *
     * @return the border color, or null if it is not set.
     */
    public float[] getBorderColor() {
        return (borderColor == NO_COLOR) ? null : Util.toRGB(borderColor);
    }

    /**
     * Sets the width of the cell borders.
     *
     * @param borderWidth the width of the cell borders.
     * @return this Cell object.
     */
    public Cell setBorderWidth(float borderWidth) {
        this.borderWidth = borderWidth;
        return this;
    }

    /**
     * Returns the width of the cell borders.
     *
     * @return the width of the cell borders.
     */
    public float getBorderWidth() {
        return this.borderWidth;
    }

    /**
     * Returns the cell height.
     *
     * @param width the cell width.
     * @throws Exception is there is an error.
     * @return the cell height.
     */
    public float getHeight(float width) throws Exception {
        float cellHeight = 0f;
        if (drawable instanceof BaselineDrawable) {
            // A line of text is drawn on the baseline of the cell text, so the
            // cell makes room for the ascent and the descent of both.
            cellHeight = ascent() + descent() + paddingAt(TOP_PADDING) + paddingAt(BOTTOM_PADDING);
        } else if ((text == null || text.equals("")) && drawable != null) {    // The text is drawn first
            if (drawable instanceof TextBlock) {
                ((TextBlock) drawable).setWidth(width);
            }
            cellHeight = measure(drawable)[1] + paddingAt(TOP_PADDING) + paddingAt(BOTTOM_PADDING);
        } else if (text != null) {
            float fontHeight = font.getBodyHeight(fontSize);
            if (fallbackFont != null && fallbackFont.getBodyHeight(fontSize) > fontHeight) {
                fontHeight = fallbackFont.getBodyHeight(fontSize);
            }
            cellHeight = fontHeight + paddingAt(TOP_PADDING) + paddingAt(BOTTOM_PADDING);
        }
        return cellHeight;
    }

    /**
     * Sets the text color of this cell. Color.transparent leaves the text color unchanged.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setTextColor(int color) {
        if (color == Color.transparent) {
            return this;
        }
        this.textColor = color & 0xFFFFFF;
        return this;
    }

    /**
     * Sets the text color of this cell. The cell keeps each component to the
     * nearest of 256 steps. A null color leaves the text color unchanged, as
     * Color.transparent does.
     *
     * @param textColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Cell object.
     */
    public Cell setTextColor(float[] textColor) {
        if (textColor == null) {
            return this;
        }
        this.textColor = Util.toPackedRGB(textColor);
        return this;
    }

    /**
     * Returns the text color of this cell.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getTextColor() {
        return Util.toRGB(textColor);
    }

    /**
     * Sets the background color of this cell. Color.transparent removes the background.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setBackgroundColor(int color) {
        if (color == Color.transparent) {
            this.backgroundColor = NO_COLOR;
            return this;
        }
        this.backgroundColor = color & 0xFFFFFF;
        return this;
    }

    /**
     * Sets the background color of this cell, or removes the background. The cell
     * keeps each component to the nearest of 256 steps.
     *
     * @param color the red, green and blue components, from 0.0 to 1.0, or null.
     * @return this Cell object.
     */
    public Cell setBackgroundColor(float[] color) {
        this.backgroundColor = Util.toPackedRGB(color);
        return this;
    }

    /**
     * Returns the background color.
     *
     * @return the background color, or null if the cell has no background.
     */
    public float[] getBackgroundColor() {
        return (backgroundColor == NO_COLOR) ? null : Util.toRGB(backgroundColor);
    }

    /**
     * Sets the column span private variable.
     *
     * @param colspan the specified column span value.
     * @return this Cell object.
     */
    public Cell setColSpan(int colspan) {
        this.colspan = colspan;
        return this;
    }

    /**
     * Returns the column span private variable value.
     *
     * @return the column span value.
     */
    public int getColSpan() {
        return this.colspan;
    }

    /**
     * Sets the number of rows this cell spans, counted from this one, so that
     * a row span of 2 covers this row and the one under it. The cells the span
     * covers are not drawn: this cell draws its text, its background and its
     * borders once over all of them, and a page break moves the whole of it to
     * the next page. The table keeps its shape, so the rows under this one
     * still hold a cell at this column, which is left empty. A row span of 1,
     * the default, spans nothing. Please see Example_38.
     *
     * @param rowspan the number of rows, from 1.
     * @return this Cell object.
     */
    public Cell setRowSpan(int rowspan) {
        this.rowspan = Math.max(1, rowspan);
        return this;
    }

    /**
     * Returns the number of rows this cell spans.
     *
     * @return the row span value.
     */
    public int getRowSpan() {
        return this.rowspan;
    }

    /**
     * Sets the cell border object.
     *
     * @param border the border object.
     * @param visible the visibility of the border.
     * @return this Cell object.
     */
    public Cell setBorder(int border, boolean visible) {
        if (visible) {
            this.properties |= (border & Border.ALL);
        } else {
            this.properties &= ~(border & Border.ALL);
        }
        return this;
    }

    /**
     * Returns the cell border object.
     *
     * @param border the border.
     * @return the cell border object.
     */
    public boolean getBorder(int border) {
        return (properties & border & Border.ALL) != 0;
    }

    /**
     * Sets all cell borders.
     * @param borders true or false.
     * @return this Cell object.
     */
    public Cell setBorders(boolean borders) {
        return setBorder(Border.ALL, borders);
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment.
     * Supported values: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER and Alignment.JUSTIFY,
     * which draws the single line of cell text left aligned.
     * @return this Cell object.
     */
    public Cell setTextAlignment(Alignment alignment) {
        setAlignmentAt(TEXT_ALIGNMENT, alignment);
        return this;
    }

    /**
     * Returns the text alignment.
     *
     * @return the horizontal text alignment.
     */
    public Alignment getTextAlignment() {
        return alignmentAt(TEXT_ALIGNMENT);
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * @param alignment the alignment.
     * Supported values: Alignment.TOP, Alignment.CENTER and Alignment.BOTTOM.
     * @return this Cell object.
     */
    public Cell setVerticalAlignment(Alignment alignment) {
        setAlignmentAt(VALIGN, alignment);
        return this;
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * @return the vertical alignment.
     */
    public Alignment getVerticalAlignment() {
        return alignmentAt(VALIGN);
    }

    /**
     * Sets the underline text parameter.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline text parameter.
     * @return this Cell object.
     */
    public Cell setUnderline(boolean underline) {
        if (underline) {
            this.properties |= UNDERLINE;
        } else {
            this.properties &= ~UNDERLINE;
        }
        return this;
    }

    /**
     * Returns the underline text parameter.
     *
     * @return the underline text parameter.
     */
    public boolean getUnderline() {
        return (this.properties & UNDERLINE) != 0;
    }

    /**
     * Sets the strikeout text parameter.
     *
     * @param strikeout the strikeout text parameter.
     * @return this Cell object.
     */
    public Cell setStrikeout(boolean strikeout) {
        if (strikeout) {
            this.properties |= STRIKEOUT;
        } else {
            this.properties &= ~STRIKEOUT;
        }
        return this;
    }

    /**
     * Returns the strikeout text parameter.
     *
     * @return the strikeout text parameter.
     */
    public boolean getStrikeout() {
        return (this.properties & STRIKEOUT) != 0;
    }

    /**
     * Sets the URI action.
     *
     * @param uri the URI.
     * @return this Cell object.
     */
    public Cell setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Draws the point, text and borders of this cell.
     *
     * @param page the page to draw on.
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width of the cell.
     * @param h the height of the cell.
     * @throws Exception if an input or output exception occurred.
     */
    protected void drawOn(
            Page page,
            float x,
            float y,
            float w,
            float h) throws Exception {
        if (backgroundColor != NO_COLOR) {
            drawBackground(page, x, y, w, h);
        }

        if (drawable instanceof BaselineDrawable || (text != null && !text.equals(""))) {
            // A line of text is drawn instead of the cell text, on its baseline.
            drawText(page, x, y, w, h);
        } else if (drawable instanceof TextBlock) {
            TextBlock textBlock = (TextBlock) drawable;
            textBlock.setLocation(x + paddingAt(LEFT_PADDING), y + paddingAt(TOP_PADDING));
            textBlock.setWidth(w - (paddingAt(LEFT_PADDING) + paddingAt(RIGHT_PADDING)));
            textBlock.drawOn(page);
        } else if (drawable != null) {
            if (getTextAlignment() == Alignment.RIGHT) {
                float drawableWidth = measure(drawable)[0];
                drawable.setLocation((x + w) - (drawableWidth + paddingAt(RIGHT_PADDING)), y + paddingAt(TOP_PADDING));
            } else if (getTextAlignment() == Alignment.CENTER) {
                float drawableWidth = measure(drawable)[0];
                drawable.setLocation((x + w/2f) - drawableWidth/2f, y + paddingAt(TOP_PADDING));
            } else {
                drawable.setLocation(x + paddingAt(LEFT_PADDING), y + paddingAt(TOP_PADDING));
            }
            drawable.drawOn(page);
        }

        drawBorders(page, x, y, w, h);
        if (point != null) {
            if (alignmentAt(MARKER_ALIGNMENT) == Alignment.LEFT) {
                point.x = x + 2*point.r;
            } else if (alignmentAt(MARKER_ALIGNMENT) == Alignment.RIGHT) {
                point.x = (x + w) - paddingAt(RIGHT_PADDING)/2;
            }
            point.y = y + h/2;
            page.setBrushColor(point.getFillColor());
            if (point.getURIAction() != null) {
                page.addAnnotation(new Annotation(
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
                        point.getURIAction(),
                        null,
                        null,
                        null,
                        null));
            }
            page.addArtifactBMC();
            page.drawPoint(point);
            page.addEMC();
        }
    }

    // Returns the width and the height of the drawable: its corner when it is
    // placed at 0, 0 and measured without a page. Measuring writes nothing, so
    // an exception from it is not an input or output error.
    static float[] measure(Drawable drawable) {
        drawable.setLocation(0f, 0f);
        try {
            return drawable.drawOn(null);
        } catch (RuntimeException e) {
            throw e;
        } catch (Exception e) {
            throw new IllegalStateException(e);
        }
    }

    private void drawBackground(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.addArtifactBMC();
        page.setBrushColor(backgroundColor);
        page.fillRect(x, y + borderWidth/2, cellW, cellH);
        page.addEMC();
    }

    private void drawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        if ((properties & Border.ALL) == 0) {
            return;     // Nothing to draw, so nothing to write.
        }
        page.addArtifactBMC();
        if (borderColor != NO_COLOR) {
            page.setPenColor(borderColor);
        }
        page.setPenWidth(borderWidth);
        // Half the pen width, so that the corners of the borders close.
        float hWidth = borderWidth / 2;
        // The borders of a cell are the subpaths of one path, stroked once.
        if ((properties & Border.TOP) != 0) {
            page.moveTo(x - hWidth, y);
            page.lineTo(x + cellW, y);
        }
        if ((properties & Border.BOTTOM) != 0) {
            page.moveTo(x - hWidth, y + cellH);
            page.lineTo(x + cellW, y + cellH);
        }
        if ((properties & Border.LEFT) != 0) {
            page.moveTo(x, y - hWidth);
            page.lineTo(x, y + cellH + hWidth);
        }
        if ((properties & Border.RIGHT) != 0) {
            page.moveTo(x + cellW, y - hWidth);
            page.lineTo(x + cellW, y + cellH + hWidth);
        }
        page.strokePath();
        page.addEMC();
    }

    private void drawText(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) throws Exception {
        float ascent = ascent();
        float yText;
        if (alignmentAt(VALIGN) == Alignment.TOP) {
            yText = y + ascent + paddingAt(TOP_PADDING);
        } else if (alignmentAt(VALIGN) == Alignment.CENTER) {
            yText = y + cellH/2 + ascent/2;
        } else if (alignmentAt(VALIGN) == Alignment.BOTTOM) {
            yText = (y + cellH) - paddingAt(BOTTOM_PADDING);
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        float xText;
        if (getTextAlignment() == Alignment.RIGHT) {
            xText = (x + cellW) - (getTextWidth() + paddingAt(RIGHT_PADDING));
        } else if (getTextAlignment() == Alignment.CENTER) {
            xText = x + paddingAt(LEFT_PADDING) +
                    (((cellW - (paddingAt(LEFT_PADDING) + paddingAt(RIGHT_PADDING))) - getTextWidth()) / 2);
        } else {
            // Alignment.LEFT, and Alignment.JUSTIFY, which a single line of text cannot use.
            xText = x + paddingAt(LEFT_PADDING);
        }
        BaselineDrawable line = (drawable instanceof BaselineDrawable)
                ? (BaselineDrawable) drawable : null;
        if (line == null) {
            page.addBDC(StructElem.P, text, text);
            page.drawString(font, fallbackFont, fontSize, text, xText, yText,
                    page.packedToRGB(textColor), null);
            page.addEMC();
            if (getUnderline()) {
                underlineText(page, xText, yText);
            }
            if (getStrikeout()) {
                strikeoutText(page, xText, yText);
            }
        } else {
            // A text line and a composite text line mark their own text.
            line.setLocation(xText, yText);
            line.drawOn(page);
        }

        if (uri != null) {
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + getTextWidth(),
                    yText + descent(),
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

    // Returns the width of the line of text this cell draws: of the drawable
    // when it is one, and of the cell text with the font and the fallback font
    // at the font size of this cell otherwise.
    private float getTextWidth() throws Exception {
        if (drawable instanceof BaselineDrawable) {
            return ((BaselineDrawable) drawable).getWidth();
        }
        return font.stringWidth(fallbackFont, fontSize, text);
    }

    // How far above the baseline the cell draws: the ascent of its font, and
    // of the line of text it holds, whichever reaches higher.
    private float ascent() {
        float ascent = font.getAscent(fontSize);
        if (drawable instanceof BaselineDrawable) {
            float lineAscent = ((BaselineDrawable) drawable).getAscent();
            if (lineAscent > ascent) {
                ascent = lineAscent;
            }
        }
        return ascent;
    }

    // How far below the baseline the cell draws: the descent of its font, and
    // of the line of text it holds, whichever reaches lower.
    private float descent() {
        float descent = font.getDescent(fontSize);
        if (drawable instanceof BaselineDrawable) {
            float lineDescent = ((BaselineDrawable) drawable).getDescent();
            if (lineDescent > descent) {
                descent = lineDescent;
            }
        }
        return descent;
    }

    private void underlineText(Page page, float x, float y) throws Exception {
        float descent = font.getDescent(fontSize);
        // The line is decoration, and the text says what the cell holds.
        page.addArtifactBMC();
        page.setPenColor(textColor);
        page.setPenWidth(font.getUnderlineThickness(fontSize));
        page.moveTo(x, y + descent);
        page.lineTo(x + getTextWidth(), y + descent);
        page.strokePath();
        page.addEMC();
    }

    private void strikeoutText(Page page, float x, float y) throws Exception {
        float ascent = font.getAscent(fontSize);
        page.addArtifactBMC();
        page.setPenColor(textColor);
        page.setPenWidth(font.getUnderlineThickness(fontSize));
        page.moveTo(x, y - ascent/3f);
        page.lineTo(x + getTextWidth(), y - ascent/3f);
        page.strokePath();
        page.addEMC();
    }
}   // End of Cell.java
