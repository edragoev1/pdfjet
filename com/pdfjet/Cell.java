/**
 *  Cell.java
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package com.pdfjet;

/**
 *  Used to create table cell objects.
 *  See the Table class for more information.
 */
public class Cell {
    protected Font font;
    protected Font fallbackFont;
    protected String text;
    protected Image image;
    protected Barcode barcode;
    protected TextBox textBox;
    protected Point point;
    protected CompositeTextLine compositeTextLine;
    protected float width = 50f;
    protected float topPadding = 2f;
    protected float bottomPadding = 2f;
    protected float leftPadding = 2f;
    protected float rightPadding = 2f;
    protected float lineWidth = 0f;

    private int background = -1;
    private int pen = Color.black;
    private int brush = Color.black;
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
     *  Creates a cell object and sets the font.
     *
     *  @param font the font.
     */
    public Cell(Font font) {
        this.font = font;
    }

    /**
     *  Creates a cell object and sets the font and the cell text.
     *
     *  @param font the font.
     *  @param text the text.
     */
    public Cell(Font font, String text) {
        this.font = font;
        this.text = text;
    }

    /**
     *  Creates a cell object and sets the font, fallback font and the cell text.
     *
     *  @param font the font.
     *  @param fallbackFont the fallback font.
     *  @param text the text.
     */
    public Cell(Font font, Font fallbackFont, String text) {
        this.font = font;
        this.fallbackFont = fallbackFont;
        this.text = text;
    }

    /**
     *  Sets the font for this cell.
     *
     *  @param font the font.
     */
    public void setFont(Font font) {
        this.font = font;
    }

    /**
     *  Sets the fallback font for this cell.
     *
     *  @param fallbackFont the fallback font.
     */
    public void setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
    }

    /**
     *  Returns the font used by this cell.
     *
     *  @return the font.
     */
    public Font getFont() {
        return this.font;
    }

    /**
     *  Returns the fallback font used by this cell.
     *
     *  @return the fallback font.
     */
    public Font getFallbackFont() {
        return this.fallbackFont;
    }

    /**
     *  Sets the cell text.
     *
     *  @param text the cell text.
     */
    public void setText(String text) {
        this.text = text;
    }

    /**
     *  Returns the cell text.
     *
     *  @return the cell text.
     */
    public String getText() {
        return this.text;
    }

    /**
     *  Sets the image inside this cell.
     *
     *  @param image the image.
     */
    public void setImage(Image image) {
        this.image = image;
        this.text = null;
    }

    /**
     *  Sets the barcode inside this cell.
     *
     *  @param barcode the barcode.
     */
    public void setBarcode(Barcode barcode) {
        this.barcode = barcode;
        this.text = null;
    }

    /**
     *  Returns the cell image.
     *
     *  @return the image.
     */
    public Image getImage() {
        return this.image;
    }

    /**
     *  Sets the point inside this cell.
     *  See the Point class and Example_09 for more information.
     *
     *  @param point the point.
     */
    public void setPoint(Point point) {
        this.point = point;
    }

    /**
     *  Returns the cell point.
     *
     *  @return the point.
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

    /**
     *  Sets the width of this cell.
     *
     *  @param width the specified width.
     */
    public void setWidth(float width) {
        this.width = width;
    }

    /**
     *  Returns the cell width.
     *
     *  @return the cell width.
     */
    public float getWidth() {
        return this.width;
    }

    /**
     *  Sets the top padding of this cell.
     *
     *  @param padding the top padding.
     */
    public void setTopPadding(float padding) {
        this.topPadding = padding;
    }

    /**
     *  Sets the bottom padding of this cell.
     *
     *  @param padding the bottom padding.
     */
    public void setBottomPadding(float padding) {
        this.bottomPadding = padding;
    }

    /**
     *  Sets the left padding of this cell.
     *
     *  @param padding the left padding.
     */
    public void setLeftPadding(float padding) {
        this.leftPadding = padding;
    }

    /**
     *  Sets the right padding of this cell.
     *
     *  @param padding the right padding.
     */
    public void setRightPadding(float padding) {
        this.rightPadding = padding;
    }

    /**
     *  Sets the top, bottom, left and right paddings of this cell.
     *
     *  @param padding the right padding.
     */
    public void setPadding(float padding) {
        this.topPadding = padding;
        this.bottomPadding = padding;
        this.leftPadding = padding;
        this.rightPadding = padding;
    }

    /**
     *  Returns the cell height.
     *
     *  @param width the cell width.
     *  @return the cell height.
     */
    public float getHeight(float width) {
        float cellHeight = 0f;
        if (textBox != null) {
            textBox.setWidth(width);
            cellHeight = (textBox.drawOn(null)[1] - textBox.y) + topPadding + bottomPadding;
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
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void setBgColor(int color) {
        this.background = color;
    }

    /**
     *  Returns the background color of this cell.
     *
     *  @return the background colo specified as 0xRRGGBB integer.
     */
    public int getBgColor() {
        return this.background;
    }

    /**
     *  Sets the pen color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void setPenColor(int color) {
        this.pen = color;
    }

    /**
     *  Returns the pen color.
     *
     *  @return the color specified as 0xRRGGBB integer.
     */
    public int getPenColor() {
        return pen;
    }

    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void setBrushColor(int color) {
        this.brush = color;
    }

    /**
     *  Returns the brush color.
     *
     * @return the brush color.
     */
    public int getBrushColor() {
        return brush;
    }

    /**
     *  Sets the pen and brush colors to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void setFgColor(int color) {
        this.pen = color;
        this.brush = color;
    }

    protected void setProperties(int properties) {
        this.properties = properties;
    }

    protected int getProperties() {
        return this.properties;
    }

    /**
     *  Sets the column span private variable.
     *
     *  @param colspan the specified column span value.
     */
    public void setColSpan(int colspan) {
        this.properties &= 0x00FF0000;
        this.properties |= (colspan & 0x0000FFFF);
    }

    /**
     *  Returns the column span private variable value.
     *
     *  @return the column span value.
     */
    public int getColSpan() {
        return (this.properties & 0x0000FFFF);
    }

    /**
     *  Sets the cell border object.
     *
     *  @param border the border object.
     *  @param visible the visibility of the border.
     */
    public void setBorder(int border, boolean visible) {
        if (visible) {
            this.properties |= border;
        } else {
            this.properties &= (~border & 0x00FFFFFF);
        }
    }

    /**
     *  Returns the cell border object.
     *
     *  @param border the border.
     *  @return the cell border object.
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
     *  Sets the cell text alignment.
     *
     *  @param alignment the alignment code.
     *  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public void setTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
    }

    /**
     *  Returns the text alignment.
     *
     *  @return the text horizontal alignment code.
     */
    public int getTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     *  Sets the cell text vertical alignment.
     *
     *  @param alignment the alignment code.
     *  Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     */
    public void setVerTextAlignment(int alignment) {
        this.valign = alignment;
    }

    /**
     *  Returns the cell text vertical alignment.
     *
     *  @return the vertical alignment code.
     */
    public int getVerTextAlignment() {
        return this.valign;
    }

    /**
     *  Sets the underline text parameter.
     *  If the value of the underline variable is 'true' - the text is underlined.
     *
     *  @param underline the underline text parameter.
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
        if (background != Color.transparent) {
            drawBackground(page, x, y, w, h);
        }

        if (textBox != null) {
            textBox.setLocation(x + leftPadding, y + topPadding);
            textBox.setWidth(w - (leftPadding + rightPadding));
            textBox.drawOn(page);
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
        } else if (text != null && !text.equals("")) {
            drawText(page, x, y, w, h);
        }

        drawBorders(page, x, y, w, h);
        if (point != null) {
            if (point.align == Align.LEFT) {
                point.x = x + 2*point.r;
            } else if (point.align == Align.RIGHT) {
                point.x = (x + w) - this.rightPadding/2;
            }
            point.y = y + h/2;
            page.setBrushColor(point.getColor());
            if (point.getURIAction() != null) {
                page.addAnnotation(new Annotation(
                        point.getURIAction(),
                        null,
                        point.x - point.r,
                        point.y - point.r,
                        point.x + point.r,
                        point.y + point.r,
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
        page.setBrushColor(background);
        page.fillRect(x, y + lineWidth / 2, cellW, cellH + lineWidth);
    }

    private void drawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.setPenColor(pen);
        page.setPenWidth(lineWidth);
        if (getBorder(Border.TOP) ||
                getBorder(Border.BOTTOM) ||
                getBorder(Border.LEFT) ||
                getBorder(Border.RIGHT)) {
            page.addArtifactBMC();
        }
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
        if (getBorder(Border.TOP) ||
                getBorder(Border.BOTTOM) ||
                getBorder(Border.LEFT) ||
                getBorder(Border.RIGHT)) {
            page.addEMC();
        }
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
            yText = y + font.ascent + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + font.ascent/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.setPenColor(pen);
        if (getTextAlignment() == Align.RIGHT) {
            if (compositeTextLine == null) {
                xText = (x + cellW) - (font.stringWidth(text) + this.rightPadding);
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, text, xText, yText, brush, null);
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
                page.drawString(font, fallbackFont, text, xText, yText, brush, null);
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
                page.drawString(font, fallbackFont, text, xText, yText, brush, null);
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
                    uri,
                    null,
                    xText,
                    yText - font.ascent,
                    xText + w,
                    yText + font.descent,
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
