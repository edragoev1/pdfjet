/**
 *  TextColumn.cs
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
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace PDFjet.NET {
/**
 *  Used to create text column objects and draw them on a page.
 *
 *  Please see Example_10 and Example_29.
 */
public class TextColumn : IDrawable {

    internal int alignment = Align.LEFT;
    internal int rotate;

    private float x;    // This variable keeps it's original value after being initialized.
    private float y;    // This variable keeps it's original value after being initialized.
    private float w;
    private float h;

    private float x1;
    private float y1;
    private float lineHeight;

    private float spaceBetweenLines = 1.0f;
    private float spaceBetweenParagraphs = 2.0f;

    private List<Paragraph> paragraphs;

    private bool lineBetweenParagraphs = false;


    /**
     *  Create a text column object.
     *
     */
    public TextColumn() {
        this.paragraphs = new List<Paragraph>();
    }


    /**
     *  Create a text column object and set the rotation angle.
     *
     *  @param rotateByDegrees the specified rotation angle in degrees.
     */
    public TextColumn(int rotateByDegrees) {
        this.rotate = rotateByDegrees;
        if (rotate == 0 || rotate == 90 || rotate == 270) {
        }
        else {
            throw new Exception(
                    "Invalid rotation angle. Please use 0, 90 or 270 degrees.");
        }
        this.paragraphs = new List<Paragraph>();
    }


    /**
     *  Sets the lineBetweenParagraphs private variable value.
     *  If the value is set to true - an empty line will be inserted between the current and next paragraphs.
     *
     *  @param lineBetweenParagraphs the specified bool value.
     */
    public void SetLineBetweenParagraphs(bool lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
    }


    public void SetSpaceBetweenLines(float spaceBetweenLines) {
        this.spaceBetweenLines = spaceBetweenLines;
    }


    public void SetSpaceBetweenParagraphs(float spaceBetweenParagraphs) {
        this.spaceBetweenParagraphs = spaceBetweenParagraphs;
    }


    /**
     *  Sets the position of this text column on the page.
     *
     *  @param x the x coordinate of the top left corner of this text column when drawn on the page.
     *  @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }


    /**
     *  Sets the position of this text column on the page.
     *
     *  @param x the x coordinate of the top left corner of this text column when drawn on the page.
     *  @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
    }


    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }


    /**
     *  Sets the location of this text column on the page.
     *
     *  @param x the x coordinate of the top left corner.
     *  @param y the y coordinate of the top left corner.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
    }


    /**
     *  Sets the size of this text column.
     *
     *  @param w the width of this text column.
     *  @param h the height of this text column.
     */
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }


    /**
     *  Sets the size of this text column.
     *
     *  @param w the width of this text column.
     *  @param h the height of this text column.
     */
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }


    /**
     *  Sets the desired width of this text column.
     *
     *  @param w the width of this text column.
     */
    public void SetWidth(float w) {
        this.w = w;
    }


    /**
     *  Sets the text alignment.
     *
     *  @param alignment the specified alignment code. Supported values: Align.LEFT, Align.RIGHT. Align.CENTER and Align.JUSTIFY
     */
    public void SetAlignment(int alignment) {
        this.alignment = alignment;
    }


    /**
     *  Sets the spacing between the lines in this text column.
     *
     *  @param spacing the specified spacing value.
     */
    public void SetLineSpacing(double spacing) {
        this.spaceBetweenLines = (float) spacing;
    }


    /**
     *  Sets the spacing between the lines in this text column.
     *
     *  @param spacing the specified spacing value.
     */
    public void SetLineSpacing(float spacing) {
        this.spaceBetweenLines = spacing;
    }


    /**
     *  Adds a new paragraph to this text column.
     *
     *  @param paragraph the new paragraph object.
     */
    public void AddParagraph(Paragraph paragraph) {
        this.paragraphs.Add(paragraph);
    }


    /**
     *  Removes the last paragraph added to this text column.
     *
     */
    public void RemoveLastParagraph() {
        if (this.paragraphs.Count >= 1) {
            this.paragraphs.RemoveAt(this.paragraphs.Count - 1);
        }
    }


    /**
     *  Returns dimension object containing the width and height of this component.
     *  Please see Example_29.
     *
     *  @Return dimension object containing the width and height of this component.
     */
    public Dimension GetSize() {
        float[] xy = DrawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }


    /**
     *  Draws this text column on the specified page.
     *
     *  @param page the page to draw this text column on.
     *  @return the point with x and y coordinates of the location where to draw the next component.
     */
    public float[] DrawOn(Page page) {
        float[] xy = null;
        for (int i = 0; i < paragraphs.Count; i++) {
            Paragraph paragraph = paragraphs[i];
            this.alignment = paragraph.alignment;
            xy = DrawParagraphOn(page, paragraph);
        }
        // Restore the original location
        SetLocation(this.x, this.y);
        return xy;
    }


    private float[] DrawParagraphOn(Page page, Paragraph paragraph) {

        List<TextLine> list = new List<TextLine>();
        float runLength = 0f;
        for (int i = 0; i < paragraph.lines.Count; i++) {
            TextLine line = paragraph.lines[i];
            if (i == 0) {
                lineHeight = line.font.bodyHeight + spaceBetweenLines;
                if (rotate == 0) {
                    y1 += line.font.ascent;
                }
                else if (rotate == 90) {
                    x1 += line.font.ascent;
                }
                else if (rotate == 270) {
                    x1 -= line.font.ascent;
                }
            }

            String[] tokens = Regex.Split(line.text, @"\s+");
            TextLine text = null;
            for (int j = 0; j < tokens.Length; j++) {
                String str = tokens[j];
                text = new TextLine(line.font, str);
                text.SetColor(line.GetColor());
                text.SetUnderline(line.GetUnderline());
                text.SetStrikeout(line.GetStrikeout());
                text.SetVerticalOffset(line.GetVerticalOffset());
                text.SetURIAction(line.GetURIAction());
                text.SetGoToAction(line.GetGoToAction());
                text.SetFallbackFont(line.GetFallbackFont());
                runLength += line.font.StringWidth(line.fallbackFont, str);
                if (runLength < w) {
                    list.Add(text);
                    runLength += line.font.StringWidth(line.fallbackFont, Single.space);
                }
                else {
                    DrawLineOfText(page, list);
                    MoveToNextLine();
                    list.Clear();
                    list.Add(text);
                    runLength = line.font.StringWidth(line.fallbackFont, str + Single.space);
                }
            }
            if (line.GetTrailingSpace() == false) {
                runLength -= line.font.StringWidth(line.fallbackFont, Single.space);
                text.SetTrailingSpace(false);
            }
        }
        DrawNonJustifiedLine(page, list);

        if (lineBetweenParagraphs) {
            MoveToNextLine();
        }

        return MoveToNextParagraph(this.spaceBetweenParagraphs);
    }


    private float[] MoveToNextLine() {
        if (rotate == 0) {
            x1 = x;
            y1 += lineHeight;
        }
        else if (rotate == 90) {
            x1 += lineHeight;
            y1 = y;
        }
        else if (rotate == 270) {
            x1 -= lineHeight;
            y1 = y;
        }
        return new float[] {x1, y1};
    }


    private float[] MoveToNextParagraph(float spaceBetweenParagraphs) {
        if (rotate == 0) {
            x1 = x;
            y1 += spaceBetweenParagraphs;
        }
        else if (rotate == 90) {
            x1 += spaceBetweenParagraphs;
            y1 = y;
        }
        else if (rotate == 270) {
            x1 -= spaceBetweenParagraphs;
            y1 = y;
        }
        return new float[] {x1, y1};
    }


    private float[] DrawLineOfText(Page page, List<TextLine> list) {
        if (alignment == Align.JUSTIFY) {
            float sumOfWordWidths = 0f;
            for (int i = 0; i < list.Count; i++) {
                TextLine textLine = list[i];
                sumOfWordWidths += textLine.font.StringWidth(textLine.fallbackFont, textLine.text);
            }
            float dx = (w - sumOfWordWidths) / (list.Count - 1);
            for (int i = 0; i < list.Count; i++) {
                TextLine textLine = list[i];
                textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());

                if (textLine.GetGoToAction() != null) {
                    page.AddAnnotation(new Annotation(
                            null,                       // The URI
                            textLine.GetGoToAction(),   // The destination name
                            x,
                            y - textLine.font.ascent,
                            x + textLine.font.StringWidth(textLine.fallbackFont, textLine.text),
                            y + textLine.font.descent,
                            null,
                            null,
                            null));
                }

                if (rotate == 0) {
                    textLine.SetTextDirection(0);
                    textLine.DrawOn(page);
                    x1 += textLine.font.StringWidth(textLine.fallbackFont, textLine.text) + dx;
                }
                else if (rotate == 90) {
                    textLine.SetTextDirection(90);
                    textLine.DrawOn(page);
                    y1 -= textLine.font.StringWidth(textLine.fallbackFont, textLine.text) + dx;
                }
                else if (rotate == 270) {
                    textLine.SetTextDirection(270);
                    textLine.DrawOn(page);
                    y1 += textLine.font.StringWidth(textLine.fallbackFont, textLine.text) + dx;
                }
            }
        }
        else {
            return DrawNonJustifiedLine(page, list);
        }

        return new float[] {x1, y1};
    }


    private float[] DrawNonJustifiedLine(Page page, List<TextLine> list) {
        float runLength = 0f;
        for (int i = 0; i < list.Count; i++) {
            TextLine textLine = list[i];
            if (i < (list.Count - 1)) {
                if (textLine.GetTrailingSpace()) {
                    textLine.text += Single.space;
                }
            }
            runLength += textLine.font.StringWidth(textLine.fallbackFont, textLine.text);
        }

        if (alignment == Align.CENTER) {
            if (rotate == 0) {
                x1 = x + ((w - runLength) / 2);
            }
            else if (rotate == 90) {
                y1 = y - ((w - runLength) / 2);
            }
            else if (rotate == 270) {
                y1 = y + ((w - runLength) / 2);
            }
        }
        else if (alignment == Align.RIGHT) {
            if (rotate == 0) {
                x1 = x + (w - runLength);
            }
            else if (rotate == 90) {
                y1 = y - (w - runLength);
            }
            else if (rotate == 270) {
                y1 = y + (w - runLength);
            }
        }

        for (int i = 0; i < list.Count; i++) {
            TextLine textLine = list[i];
            textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());

            if (textLine.GetGoToAction() != null) {
                page.AddAnnotation(new Annotation(
                        null,                       // The URI
                        textLine.GetGoToAction(),   // The destination name
                        x,
                        y - textLine.font.ascent,
                        x + textLine.font.StringWidth(textLine.fallbackFont, textLine.text),
                        y + textLine.font.descent,
                        null,
                        null,
                        null));
            }

            if (rotate == 0) {
                textLine.SetTextDirection(0);
                textLine.DrawOn(page);
                x1 += textLine.font.StringWidth(textLine.fallbackFont, textLine.text);
            }
            else if (rotate == 90) {
                textLine.SetTextDirection(90);
                textLine.DrawOn(page);
                y1 -= textLine.font.StringWidth(textLine.fallbackFont, textLine.text);
            }
            else if (rotate == 270) {
                textLine.SetTextDirection(270);
                textLine.DrawOn(page);
                y1 += textLine.font.StringWidth(textLine.fallbackFont, textLine.text);
            }
        }

        return new float[] {x1, y1};
    }


    /**
     *  Adds a new paragraph with Chinese text to this text column.
     *
     *  @param font the font used by this paragraph.
     *  @param chinese the Chinese text.
     */
    public void AddChineseParagraph(Font font, String chinese) {
        Paragraph paragraph;
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < chinese.Length; i++) {
            char ch = chinese[i];
            if (font.StringWidth(buf.ToString() + ch) > w) {
                paragraph = new Paragraph();
                paragraph.Add(new TextLine(font, buf.ToString()));
                AddParagraph(paragraph);
                buf.Length = 0;
            }
            buf.Append(ch);
        }
        paragraph = new Paragraph();
        paragraph.Add(new TextLine(font, buf.ToString()));
        AddParagraph(paragraph);
    }


    /**
     *  Adds a new paragraph with Japanese text to this text column.
     *
     *  @param font the font used by this paragraph.
     *  @param japanese the Japanese text.
     */
    public void AddJapaneseParagraph(Font font, String japanese) {
        AddChineseParagraph(font, japanese);
    }

}   // End of TextColumn.cs
}   // End of namespace PDFjet.NET
