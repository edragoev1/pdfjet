/**
 *  Cell.cs
 *
Copyright 2023 Innovatics Inc.

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
using System;
using System.Text;

namespace PDFjet.NET {
/**
 *  Used to create table cell objects.
 *  See the Table class for more information.
 *
 */
public class Cell {

    internal Font font;
    internal Font fallbackFont;
    internal String text;
    internal Image image;
    internal BarCode barCode;
    internal TextBlock textBlock;
    internal Point point;
    internal CompositeTextLine compositeTextLine;
    internal IDrawable drawable;

    internal float width = 50f;
    internal float topPadding = 2f;
    internal float bottomPadding = 2f;
    internal float leftPadding = 2f;
    internal float rightPadding = 2f;
    internal float lineWidth = 0.2f;

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
    private uint properties = 0x000F0001;
    private String uri;

    private uint valign = Align.TOP;


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
    public void SetFont(Font font) {
        this.font = font;
    }


    /**
     *  Sets the fallback font for this cell.
     *
     *  @param fallbackFont the fallback font.
     */
    public void SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
    }


    /**
     *  Returns the font used by this cell.
     *
     *  @return the font.
     */
    public Font GetFont() {
        return this.font;
    }


    /**
     *  Returns the fallback font used by this cell.
     *
     *  @return the fallback font.
     */
    public Font GetFallbackFont() {
        return this.fallbackFont;
    }


    /**
     *  Sets the cell text.
     *
     *  @param text the cell text.
     */
    public void SetText(String text) {
        this.text = text;
    }


    /**
     *  Returns the cell text.
     *
     *  @return the cell text.
     */
    public String GetText() {
        return this.text;
    }


    /**
     *  Sets the image inside this cell.
     *
     *  @param image the image.
     */
    public void SetImage(Image image) {
        this.image = image;
    }


    /**
     *  Sets the barcode inside this cell.
     *
     *  @param barCode the barcode.
     */
    public void SetBarcode(BarCode barCode) {
        this.barCode = barCode;
    }


    /**
     *  Returns the cell image.
     *
     *  @return the image.
     */
    public Image GetImage() {
        return this.image;
    }


    /**
     *  Sets the point inside this cell.
     *  See the Point class and Example_09 for more information.
     *
     *  @param point the point.
     */
    public void SetPoint(Point point) {
        this.point = point;
    }


    /**
     *  Returns the cell point.
     *
     *  @return the point.
     */
    public Point GetPoint() {
        return this.point;
    }


    public void SetCompositeTextLine(CompositeTextLine compositeTextLine) {
        this.compositeTextLine = compositeTextLine;
    }


    public CompositeTextLine GetCompositeTextLine() {
        return this.compositeTextLine;
    }


    public void SetDrawable(IDrawable drawable) {
        this.drawable = drawable;
    }


    public IDrawable GetDrawable() {
        return this.drawable;
    }


    public void SetTextBlock(TextBlock textBlock) {
        this.textBlock = textBlock;
        // TODO: Remove?
        // this.textBlock.SetWidth(this.width - (this.leftPadding + this.rightPadding));
    }



    /**
     *  Sets the width of this cell.
     *
     *  @param width the specified width.
     */
    public void SetWidth(double width) {
        this.width = (float) width;
        if (textBlock != null) {
            textBlock.SetWidth(this.width - (this.leftPadding + this.rightPadding));
        }
    }


    /**
     *  Returns the cell width.
     *
     *  @return the cell width.
     */
    public float GetWidth() {
        return this.width;
    }


    /**
     *  Sets the top padding of this cell.
     *
     *  @param padding the top padding.
     */
    public void SetTopPadding(float padding) {
        this.topPadding = padding;
    }


    /**
     *  Sets the bottom padding of this cell.
     *
     *  @param padding the bottom padding.
     */
    public void SetBottomPadding(float padding) {
        this.bottomPadding = padding;
    }


    /**
     *  Sets the left padding of this cell.
     *
     *  @param padding the left padding.
     */
    public void SetLeftPadding(float padding) {
        this.leftPadding = padding;
    }


    /**
     *  Sets the right padding of this cell.
     *
     *  @param padding the right padding.
     */
    public void SetRightPadding(float padding) {
        this.rightPadding = padding;
    }


    /**
     *  Sets the top, bottom, left and right paddings of this cell.
     *
     *  @param padding the right padding.
     */
    public void SetPadding(float padding) {
        this.topPadding = padding;
        this.bottomPadding = padding;
        this.leftPadding = padding;
        this.rightPadding = padding;
    }


    /**
     *  Returns the cell height.
     *
     *  @return the cell height.
     */
    public float GetHeight() {
        float cellHeight = 0f;

        if (image != null) {
            float height = image.GetHeight() + topPadding + bottomPadding;
            if (height > cellHeight) {
                cellHeight = height;
            }
        }

        if (barCode != null) {
            float height = barCode.GetHeight() + topPadding + bottomPadding;
            if (height > cellHeight) {
                cellHeight = height;
            }
        }

        if (textBlock != null) {
            try {
                float height = textBlock.DrawOn(null)[1] + topPadding + bottomPadding;
                if (height > cellHeight) {
                    cellHeight = height;
                }
            }
            catch (Exception) {
            }
        }

        if (drawable != null) {
            try {
                float height = drawable.DrawOn(null)[1] + topPadding + bottomPadding;
                if (height > cellHeight) {
                    cellHeight = height;
                }
            }
            catch (Exception) {
            }
        }

        if (text != null) {
            float fontHeight = font.GetHeight();
            if (fallbackFont != null && fallbackFont.GetHeight() > fontHeight) {
                fontHeight = fallbackFont.GetHeight();
            }
            float height = fontHeight + topPadding + bottomPadding;
            if (height > cellHeight) {
                cellHeight = height;
            }
        }

        return cellHeight;
    }


    public void SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
    }


    public float GetLineWidth() {
        return this.lineWidth;
    }


    /**
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as an integer.
     */
    public void SetBgColor(int color) {
        this.background = color;
    }


    /**
     *  Returns the background color of this cell.
     *
     */
    public int GetBgColor() {
        return this.background;
    }


    /**
     *  Sets the pen color.
     *
     *  @param color the color specified as an integer.
     */
    public void SetPenColor(int color) {
        this.pen = color;
    }


    /**
     *  Returns the pen color.
     *
     */
    public int GetPenColor() {
        return pen;
    }


    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as an integer.
     */
    public void SetBrushColor(int color) {
        this.brush = color;
    }


    /**
     *  Returns the brush color.
     *
     */
    public int GetBrushColor() {
        return brush;
    }


    /**
     *  Sets the pen and brush colors to the specified color.
     *
     *  @param color the color specified as an integer.
     */
    public void SetFgColor(int color) {
        this.pen = color;
        this.brush = color;
    }


    internal void SetProperties(uint properties) {
        this.properties = properties;
    }


    internal uint GetProperties() {
        return this.properties;
    }


    /**
     *  Sets the column span private variable.
     *
     *  @param colspan the specified column span value.
     */
    public void SetColSpan(int colspan) {
        this.properties &= 0x00FF0000;
        this.properties |= ((uint) (colspan & 0x0000FFFF));
    }


    /**
     *  Returns the column span private variable value.
     *
     *  @return the column span value.
     */
    public uint GetColSpan() {
        return (this.properties & 0x0000FFFF);
    }


    /**
     *  Sets the cell border object.
     *
     *  @param border the border object.
     */
    public void SetBorder(uint border, bool visible) {
        if (visible) {
            this.properties |= border;
        }
        else {
            this.properties &= (~border & 0x00FFFFFF);
        }
    }


    /**
     *  Returns the cell border object.
     *
     *  @return the cell border object.
     */
    public bool GetBorder(uint border) {
        return (this.properties & border) != 0;
    }


    /**
     *  Sets all border object parameters to false.
     *  This cell will have no borders when drawn on the page.
     */
    public void SetNoBorders() {
        this.properties &= 0x00F0FFFF;
    }


    /**
     *  Sets the cell text alignment.
     *
     *  @param alignment the alignment code.
     *  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public void SetTextAlignment(uint alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
    }


    /**
     *  Returns the text alignment.
     *
     *  @return the horizontal alignment code.
     */
    public uint GetTextAlignment() {
        return (this.properties & 0x00300000);
    }


    /**
     *  Sets the cell text vertical alignment.
     *
     *  @param alignment the alignment code.
     *  Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     */
    public void SetVerTextAlignment(uint alignment) {
        this.valign = alignment;
    }


    /**
     *  Returns the cell text vertical alignment.
     *
     *  @return the vertical alignment code.
     */
    public uint GetVerTextAlignment() {
        return this.valign;
    }


    /**
     *  Sets the underline variable.
     *  If the value of the underline variable is 'true' - the text is underlined.
     *
     *  @param underline the underline flag.
     */
    public void SetUnderline(bool underline) {
        if (underline) {
            this.properties |= 0x00400000;
        }
        else {
            this.properties &= 0x00BFFFFF;
        }
    }


    public bool GetUnderline() {
        return (properties & 0x00400000) != 0;
    }


    public void SetStrikeout(bool strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        }
        else {
            this.properties &= 0x007FFFFF;
        }
    }


    public bool GetStrikeout() {
        return (properties & 0x00800000) != 0;
    }


    public void SetURIAction(String uri) {
        this.uri = uri;
    }


    /**
     *  Draws the point, text and borders of this cell.
     */
    internal void Paint(
            Page page,
            float x,
            float y,
            float w,
            float h) {
        if (background != -1) {
            DrawBackground(page, x, y, w, h);
        }
        if (image != null) {
            if (GetTextAlignment() == Align.LEFT) {
                image.SetLocation(x + leftPadding, y + topPadding);
                image.DrawOn(page);
            }
            else if (GetTextAlignment() == Align.CENTER) {
                image.SetLocation((x + w/2f) - image.GetWidth()/2f, y + topPadding);
                image.DrawOn(page);
            }
            else if (GetTextAlignment() == Align.RIGHT) {
                image.SetLocation((x + w) - (image.GetWidth() + leftPadding), y + topPadding);
                image.DrawOn(page);
            }
        }
        if (barCode != null) {
            try {
                if (GetTextAlignment() == Align.LEFT) {
                    barCode.DrawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                }
                else if (GetTextAlignment() == Align.CENTER) {
                    float barcodeWidth = barCode.DrawOn(null)[0];
                    barCode.DrawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                }
                else if (GetTextAlignment() == Align.RIGHT) {
                    float barcodeWidth = barCode.DrawOn(null)[0];
                    barCode.DrawOnPageAtLocation(page, (x + w) - (barcodeWidth + leftPadding), y + topPadding);
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        if (textBlock != null) {
            textBlock.SetLocation(x + leftPadding, y + topPadding);
            textBlock.DrawOn(page);
        }
        DrawBorders(page, x, y, w, h);
        if (text != null && !text.Equals("")) {
            DrawText(page, x, y, w, h);
        }
        if (point != null) {
            if (point.align == Align.LEFT) {
                point.x = x + 2*point.r;
            }
            else if (point.align == Align.RIGHT) {
                point.x = (x + w) - this.rightPadding/2;
            }
            point.y = y + h/2;
            page.SetBrushColor(point.GetColor());
            if (point.GetURIAction() != null) {
                page.AddAnnotation(new Annotation(
                        point.GetURIAction(),
                        null,
                        point.x - point.r,
                        point.y - point.r,
                        point.x + point.r,
                        point.y + point.r,
                        null,
                        null,
                        null));
            }
            page.DrawPoint(point);
        }

        if (drawable != null) {
            drawable.SetPosition(x + leftPadding, y + topPadding);
            drawable.DrawOn(page);
        }
    }


    private void DrawBackground(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.SetBrushColor(background);
        page.FillRect(x, y + lineWidth / 2, cellW, cellH + lineWidth);
    }


    private void DrawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.SetPenColor(pen);
        page.SetPenWidth(lineWidth);
        if (GetBorder(Border.TOP) &&
                GetBorder(Border.BOTTOM) &&
                GetBorder(Border.LEFT) &&
                GetBorder(Border.RIGHT)) {
            page.AddBMC(StructElem.P, Single.space, Single.space);
            page.DrawRect(x, y, cellW, cellH);
            page.AddEMC();
        } else {
            float qWidth = lineWidth / 4;
            if (GetBorder(Border.TOP)) {
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.MoveTo(x - qWidth, y);
                page.LineTo(x + cellW, y);
                page.StrokePath();
                page.AddEMC();
            }
            if (GetBorder(Border.BOTTOM)) {
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.MoveTo(x - qWidth, y + cellH);
                page.LineTo(x + cellW, y + cellH);
                page.StrokePath();
                page.AddEMC();
            }
            if (GetBorder(Border.LEFT)) {
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.MoveTo(x, y - qWidth);
                page.LineTo(x, y + cellH + qWidth);
                page.StrokePath();
                page.AddEMC();
            }
            if (GetBorder(Border.RIGHT)) {
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.MoveTo(x + cellW, y - qWidth);
                page.LineTo(x + cellW, y + cellH + qWidth);
                page.StrokePath();
                page.AddEMC();
            }
        }
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
            yText = y + font.ascent + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + font.ascent/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.SetPenColor(pen);
        page.SetBrushColor(brush);
        if (GetTextAlignment() == Align.RIGHT) {
            if (compositeTextLine == null) {
                xText = (x + cellW) - (font.StringWidth(text) + this.rightPadding);
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.DrawString(font, fallbackFont, text, xText, yText);
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
                page.AddBMC(StructElem.P, Single.space, Single.space);
                compositeTextLine.DrawOn(page);
                page.AddEMC();
            }
        } else if (GetTextAlignment() == Align.CENTER) {
            if (compositeTextLine == null) {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - font.StringWidth(text)) / 2);
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.DrawString(font, fallbackFont, text, xText, yText);
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
                page.AddBMC(StructElem.P, Single.space, Single.space);
                compositeTextLine.DrawOn(page);
                page.AddEMC();
            }
        } else if (GetTextAlignment() == Align.LEFT) {
            xText = x + this.leftPadding;
            if (compositeTextLine == null) {
                page.AddBMC(StructElem.P, Single.space, Single.space);
                page.DrawString(font, fallbackFont, text, xText, yText);
                page.AddEMC();
                if (GetUnderline()) {
                    UnderlineText(page, font, text, xText, yText);
                }
                if (GetStrikeout()) {
                    StrikeoutText(page, font, text, xText, yText);
                }
            } else {
                compositeTextLine.SetLocation(xText, yText);
                page.AddBMC(StructElem.P, Single.space, Single.space);
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
                    uri,
                    null,
                    xText,
                    (page.height - yText) - font.ascent,
                    xText + w,
                    (page.height - yText) + font.descent,
                    null,
                    null,
                    null));
        }
    }

    private void UnderlineText(
            Page page, Font font, String text, float x, float y) {
        page.AddBMC(StructElem.P, Single.space, Single.space);
        page.SetPenWidth(font.underlineThickness);
        page.MoveTo(x, y + font.descent);
        page.LineTo(x + font.StringWidth(text), y + font.descent);
        page.StrokePath();
        page.AddEMC();
    }

    private void StrikeoutText(
            Page page, Font font, String text, float x, float y) {
        page.AddBMC(StructElem.P, Single.space, Single.space);
        page.SetPenWidth(font.underlineThickness);
        page.MoveTo(x, y - font.GetAscent()/3f);
        page.LineTo(x + font.StringWidth(text), y - font.GetAscent()/3f);
        page.StrokePath();
        page.AddEMC();
    }

    /**
     *  Use this method to find out how many vertically stacked cell are needed after call to wrapAroundCellText.
     *
     *  @return the number of vertical cells needed to wrap around the cell text.
     */
    public int GetNumVerCells() {
        int numOfVerCells = 1;
        if (this.text == null) {
            return numOfVerCells;
        }

        float effectiveWidth = this.width - (this.leftPadding + this.rightPadding);
        String[] tokens = TextUtils.SplitTextIntoTokens(this.text, this.font, this.fallbackFont, effectiveWidth);
        StringBuilder buf = new StringBuilder();
        foreach (String token in tokens) {
            if (font.StringWidth(fallbackFont, (buf.ToString() + " " + token).Trim()) > effectiveWidth) {
                numOfVerCells++;
                buf = new StringBuilder(token);
            } else {
                buf.Append(" ");
                buf.Append(token);
            }
        }

        return numOfVerCells;
    }

    public TextBlock GetTextBlock() {
        return textBlock;
    }
}   // End of Cell.cs
}   // End of namespace PDFjet.NET
