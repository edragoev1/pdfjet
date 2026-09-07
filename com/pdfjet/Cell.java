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
    protected Font font;
    protected Font fallbackFont;
    protected float fontSize;
    protected String text;
    protected Image image;
    protected Barcode barcode;
    protected TextBox textBox;
    protected TextBlock textBlock;
    protected TextColumn textColumn;
    protected Point point;
    protected CompositeTextLine compositeTextLine;
    protected float width = 75f;     // TODO: Rename to cellWidth
    protected float topPadding = 2f;
    protected float bottomPadding = 2f;
    protected float leftPadding = 2f;
    protected float rightPadding = 2f;

    protected float lineWidth = 0f;  // TODO: Rename to borderWidth

    protected float[] backgroundColor;
    protected float[] textColor = new float[] {0f, 0f, 0f};
    protected float strokeWidth;
    protected float[] strokeColor;
    protected String strokeDashPattern = "[] 0";    // Solid

    protected int colspan = 1;

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
    private int properties = 0x00050001;    // Set only left and top borders!
    private String uri;
    private int valign = Align.TOP;

    /**
     * Creates a cell object and sets the font.
     *
     * @param font the font.
     */
    public Cell(Font font) {
        this.font = font;
        this.fontSize = font.getSize();
        this.fallbackFont = font;
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
    }

    /**
     * Sets the font for this cell.
     *
     * @param font the font.
     */
    public void setFont(Font font) {
        this.font = font;
    }

    /**
     * Sets the fallback font for this cell.
     *
     * @param fallbackFont the fallback font.
     */
    public void setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
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
     */
    public void setText(String text) {
        this.text = text;
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
     * Sets the image inside this cell.
     *
     * @param image the image.
     */
    public void setImage(Image image) {
        this.image = image;
        this.text = null;
    }

    /**
     * Sets the barcode inside this cell.
     *
     * @param barcode the barcode.
     */
    public void setBarcode(Barcode barcode) {
        this.barcode = barcode;
        this.text = null;
    }

    /**
     * Returns the cell image.
     *
     * @return the image.
     */
    public Image getImage() {
        return this.image;
    }

    /**
     * Sets the point inside this cell.
     * See the Point class and Example_09 for more information.
     *
     * @param point the point.
     */
    public void setPoint(Point point) {
        this.point = point;
    }

    /**
     * Returns the cell point.
     *
     * @return the point.
     */
    public Point getPoint() {
        return this.point;
    }

    /**
     * Sets the composite text object.
     *
     * @param compositeTextLine the composite text object.
     */
    public void setCompositeTextLine(CompositeTextLine compositeTextLine) {
        this.compositeTextLine = compositeTextLine;
    }

    /**
     * Returns the composite text object.
     *
     * @return the composite text object.
     */
    public CompositeTextLine getCompositeTextLine() {
        return this.compositeTextLine;
    }

    /**
     * Sets the text box.
     *
     * @param textBox the text box.
     */
    public void setTextBox(TextBox textBox) {
        this.textBox = textBox;
        this.text = null;
    }

    public Cell setTextBlock(TextBlock textBlock) {
        this.textBlock = textBlock;
        return this;
    }

    public Cell setTextColumn(TextColumn textColumn) {
        this.textColumn = textColumn;
        this.width = textColumn.getWidth() + this.leftPadding + this.rightPadding;
        return this;
    }

    /**
     * Sets the width of this cell.
     *
     * @param width the specified width.
     */
    public void setWidth(float width) {
        this.width = width;
        if (textBox != null) {
            textBox.setWidth(this.width - (this.leftPadding + this.rightPadding));
        } else if (textBlock != null) {
            textBlock.setWidth(this.width - (this.leftPadding + this.rightPadding));
        }
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
     */
    public void setTopPadding(float padding) {
        this.topPadding = padding;
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * @param padding the bottom padding.
     */
    public void setBottomPadding(float padding) {
        this.bottomPadding = padding;
    }

    /**
     * Sets the left padding of this cell.
     *
     * @param padding the left padding.
     */
    public void setLeftPadding(float padding) {
        this.leftPadding = padding;
    }

    /**
     * Sets the right padding of this cell.
     *
     * @param padding the right padding.
     */
    public void setRightPadding(float padding) {
        this.rightPadding = padding;
    }

    /**
     * Sets the top, bottom, left and right paddings of this cell.
     *
     * @param padding the right padding.
     */
    public void setPadding(float padding) {
        this.topPadding = padding;
        this.bottomPadding = padding;
        this.leftPadding = padding;
        this.rightPadding = padding;
    }

    public void setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
    }

    public void setStrokeColor(float[] strokeColor) {
        this.strokeColor = strokeColor;
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
        if (textBox != null) {
            textBox.setWidth(width);
            cellHeight = (textBox.drawOn(null)[1] - textBox.y) + topPadding + bottomPadding;
        } else if (textBlock != null) {
            textBlock.setWidth(width);
            cellHeight = (textBlock.drawOn(null)[1] - textBlock.y) + topPadding + bottomPadding;
        } else if (textColumn != null) {
            cellHeight = (textColumn.drawOn(null)[1] - textColumn.y) + topPadding + bottomPadding;
        } else if (image != null) {
            cellHeight = image.getHeight() + topPadding + bottomPadding;
        } else if (barcode != null) {
            cellHeight = barcode.getHeight() + topPadding + bottomPadding;
        } else if (text != null) {
            float fontHeight = font.getHeight();
            if (fallbackFont != null && fallbackFont.getHeight() > fontHeight) {
                fontHeight = fallbackFont.getHeight();
            }
            cellHeight = fontHeight + topPadding + bottomPadding;
        }
        return cellHeight;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth the border line width.
     */
    public void setLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
    }

    /**
     * Returns the border line width.
     *
     * @return the border line width.
     */
    public float getLineWidth() {
        return this.lineWidth;
    }

    /**
     * Sets the text color.
     *
     * @param textColor the text color.
     */
    public void setBrushColor(float[] textColor) {
        this.textColor = textColor;
    }

    public void setTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, b, b};
    }

    public void setTextColor(float[] textColor) {
        this.textColor = textColor;
    }

    /**
     * Returns the brush color.
     *
     * @return the brush color.
     */
    public float[] getBrushColor() {
        return textColor;
    }

    public float[] getTextColor() {
        return textColor;
    }

    public void setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.backgroundColor = new float[] {r, g, b};
    }

    public void setBackgroundColor(float[] color) {
        this.backgroundColor = backgroundColor;
    }

    protected void setProperties(int properties) {
        this.properties = properties;
    }

    protected int getProperties() {
        return this.properties;
    }

    /**
     * Sets the column span private variable.
     *
     * @param colspan the specified column span value.
     */
    public void setColSpan(int colspan) {
        this.properties &= 0x00FF0000;
        this.properties |= (colspan & 0x0000FFFF);
    }

    /**
     * Returns the column span private variable value.
     *
     * @return the column span value.
     */
    public int getColSpan() {
        return (this.properties & 0x0000FFFF);
    }

    /**
     * Sets the cell border object.
     *
     * @param border the border object.
     * @param visible the visibility of the border.
     */
    public void setBorder(int border, boolean visible) {
        if (visible) {
            this.properties |= border;
        } else {
            this.properties &= (~border & 0x00FFFFFF);
        }
    }

    /**
     * Returns the cell border object.
     *
     * @param border the border.
     * @return the cell border object.
     */
    public boolean getBorder(int border) {
        return (this.properties & border) != 0;
    }

    /**
     * Sets all cell borders.
     * @param borders true or false.
     */
    public void setBorders(boolean borders) {
        if (borders) {
            this.properties &= 0x00FFFFFF;
        } else {
            this.properties &= 0x00F0FFFF;
        }
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public void setTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
    }

    /**
     * Returns the text alignment.
     *
     * @return the text horizontal alignment code.
     */
    public int getTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     */
    public void setVerTextAlignment(int alignment) {
        this.valign = alignment;
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * @return the vertical alignment code.
     */
    public int getVerTextAlignment() {
        return this.valign;
    }

    /**
     * Sets the underline text parameter.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline text parameter.
     */
    public void setUnderline(boolean underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
    }

    /**
     * Returns the underline text parameter.
     *
     * @return the underline text parameter.
     */
    public boolean getUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /**
     * Sets the strikeout text parameter.
     *
     * @param strikeout the strikeout text parameter.
     */
    public void setStrikeout(boolean strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
    }

    /**
     * Returns the strikeout text parameter.
     *
     * @return the strikeout text parameter.
     */
    public boolean getStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    /**
     * Sets the URI action.
     *
     * @param uri the URI.
     */
    public void setURIAction(String uri) {
        this.uri = uri;
    }

    /**
     * Draws the point, text and borders of this cell.
     */
    protected void drawOn(
            Page page,
            float x,
            float y,
            float w,
            float h) throws Exception {
        if (backgroundColor != null) {
            drawBackground(page, x, y, w, h);
        }

        if (text != null && !text.equals("")) {
            drawText(page, x, y, w, h);
        } else if (textBox != null) {
            textBox.setLocation(x + leftPadding, y + topPadding);
            textBox.setWidth(w - (leftPadding + rightPadding));
            textBox.drawOn(page);
        } else if (textBlock != null) {
            textBlock.setPosition(x + leftPadding, y + topPadding);
            textBlock.setWidth(w - (leftPadding + rightPadding));
            textBlock.drawOn(page);
        } else if (textColumn != null) {
            textColumn.setPosition(x + leftPadding, y + topPadding);
            textColumn.drawOn(page);
        } else if (image != null) {
            if (getTextAlignment() == Align.LEFT) {
                image.setLocation(x + leftPadding, y + topPadding);
                image.drawOn(page);
            } else if (getTextAlignment() == Align.CENTER) {
                image.setLocation((x + w/2f) - image.getWidth()/2f, y + topPadding);
                image.drawOn(page);
            } else if (getTextAlignment() == Align.RIGHT) {
                image.setLocation((x + w) - (image.getWidth() + leftPadding), y + topPadding);
                image.drawOn(page);
            }
        } else if (barcode != null) {
            try {
                if (getTextAlignment() == Align.LEFT) {
                    barcode.drawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                } else if (getTextAlignment() == Align.CENTER) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                } else if (getTextAlignment() == Align.RIGHT) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w) - (barcodeWidth + leftPadding), y + topPadding);
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }

        drawBorders(page, x, y, w, h);
        if (point != null) {
            if (point.align == Align.LEFT) {
                point.x = x + 2*point.r;
            } else if (point.align == Align.RIGHT) {
                point.x = (x + w) - this.rightPadding/2;
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
                        0f,     // Transparency
                        null,   // Title
                        null,   // Contents
                        point.getURIAction(),
                        null,
                        null,
                        null,
                        null));
            }
            page.drawPoint(point);
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
        page.fillRect(x, y + lineWidth/2, cellW, cellH);
        page.addEMC();
    }

    private void drawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.addArtifactBMC();
        page.setPenColor(strokeColor);
        page.setPenWidth(lineWidth);
        float qWidth = lineWidth / 4;
        if (getBorder(Border.TOP)) {
            page.moveTo(x - qWidth, y);
            page.lineTo(x + cellW, y);
            page.strokePath();
        }
        if (getBorder(Border.BOTTOM)) {
            page.moveTo(x - qWidth, y + cellH);
            page.lineTo(x + cellW, y + cellH);
            page.strokePath();
        }
        if (getBorder(Border.LEFT)) {
            page.moveTo(x, y - qWidth);
            page.lineTo(x, y + cellH + qWidth);
            page.strokePath();
        }
        if (getBorder(Border.RIGHT)) {
            page.moveTo(x + cellW, y - qWidth);
            page.lineTo(x + cellW, y + cellH + qWidth);
            page.strokePath();
        }
        page.addEMC();
    }

    private void drawText(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) throws Exception {
        float xText;
        float yText;
        if (valign == Align.TOP) {
            yText = y + font.getAscent(fontSize) + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + font.getAscent(fontSize)/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.setPenColor(strokeColor);
        if (getTextAlignment() == Align.RIGHT) {
            if (compositeTextLine == null) {
                xText = (x + cellW) - (font.stringWidth(text) + this.rightPadding);
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = (x + cellW) - (compositeTextLine.getWidth() + this.rightPadding);
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else if (getTextAlignment() == Align.CENTER) {
            if (compositeTextLine == null) {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - font.stringWidth(text)) / 2);
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - compositeTextLine.getWidth()) / 2);
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else if (getTextAlignment() == Align.LEFT) {
            xText = x + this.leftPadding;
            if (compositeTextLine == null) {
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else {
            throw new Exception("Invalid Text Alignment!");
        }

        if (uri != null) {
            float w = (compositeTextLine != null) ?
                    compositeTextLine.getWidth() : font.stringWidth(text);
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    (page.height - yText) - font.getAscent(), // (page.height - yText) - font.GetAscent(fontSize),
                    xText + w,
                    (page.height - yText) + font.getDescent(),
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

    private void underlineText(
            Page page, Font font, String text, float x, float y) {
        page.addBMC(StructElem.P, "underline", "underline");
        page.setPenWidth(font.underlineThickness);
        page.moveTo(x, y + font.descent);
        page.lineTo(x + font.stringWidth(text), y + font.descent);
        page.strokePath();
        page.addEMC();
    }

    private void strikeoutText(
            Page page, Font font, String text, float x, float y) {
        page.addBMC(StructElem.P, "strike out", "strike out");
        page.setPenWidth(font.underlineThickness);
        page.moveTo(x, y - font.getAscent()/3f);
        page.lineTo(x + font.stringWidth(text), y - font.getAscent()/3f);
        page.strokePath();
        page.addEMC();
    }

    /**
     * Returns the text box.
     *
     * @return the text box.
     */
    public TextBox getTextBox() {
        return textBox;
    }
}   // End of Cell.java
