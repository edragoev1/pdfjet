/**
 * TextColumn.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
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
    internal uint alignment = Align.LEFT;
    internal int rotate;
    internal float x;   // This variable keeps it's original value after being initialized.
    internal float y;   // This variable keeps it's original value after being initialized.
    internal float w;
    internal float h;
    private float x1;
    private float y1;
    private float lineSpacing = 1.0f;
    private float paragraphSpacing = 1.0f;
    private List<Paragraph> paragraphs;
    private bool lineBetweenParagraphs = false;
    private float fixedHeight;

    /**
     * Create a text column object.
     */
    public TextColumn() {
        this.paragraphs = new List<Paragraph>();
    }

    /**
     * Create a text column object and set the rotation angle.
     *
     * @param rotateByDegrees the specified rotation angle in degrees.
     */
    public TextColumn(int rotateByDegrees) {
        this.rotate = rotateByDegrees;
        if (rotate == 0 || rotate == 90 || rotate == 270) {
        } else {
            throw new Exception(
                    "Invalid rotation angle. Please use 0, 90 or 270 degrees.");
        }
        this.paragraphs = new List<Paragraph>();
    }

    /**
     * Sets the lineBetweenParagraphs private variable value.
     * If the value is set to true - an empty line will be inserted between the current and next paragraphs.
     *
     * @param lineBetweenParagraphs the specified bool value.
     */
    public void SetLineBetweenParagraphs(bool lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
    }

    public void SetLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
    }

    /**
     * Sets the spacing between the lines in this text column.
     *
     * @param spacing the specified spacing value.
     */
    public void SetLineSpacing(double lineSpacing) {
        this.lineSpacing = (float) lineSpacing;
    }

    public void SetParagraphSpacing(float paragraphSpacing) {
        this.paragraphSpacing = paragraphSpacing;
    }

    public void SetParagraphSpacing(double paragraphSpacing) {
        this.paragraphSpacing = (float) paragraphSpacing;
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void SetPosition(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
    }

    /**
     * Sets the location of this text column on the page.
     *
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
    }

    /**
     * Sets the size of this text column.
     *
     * @param w the width of this text column.
     * @param h the height of this text column.
     */
    [Obsolete]
    public void SetSize(double w, double h) {
        SetSize((float) w, (float) h);
    }

    /**
     * Sets the size of this text column.
     *
     * @param w the width of this text column.
     * @param h the height of this text column.
     */
    [Obsolete]
    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /**
     * Sets the desired width of this text column.
     *
     * @param w the width of this text column.
     */
    public void SetWidth(float w) {
        this.w = w;
    }

    public float GetWidth() {
        return this.w;
    }

    public void SetHeight(float h) {
        this.h = h;
    }

    public float GetHeight() {
        if (fixedHeight > 0f) {
            return fixedHeight;
        }
        return this.h;
    }

    public void SetFixedHeight(float fixedHeight) {
        this.fixedHeight = fixedHeight;
    }

    public float GetFixedHeight() {
        return this.fixedHeight;
    }

    /**
     * Sets the text alignment.
     *
     * @param alignment the specified alignment code.
     *      Supported values: Align.LEFT, Align.RIGHT. Align.CENTER and Align.JUSTIFY
     */
    public void SetAlignment(uint alignment) {
        this.alignment = alignment;
    }

    /**
     * Adds a new paragraph to this text column.
     *
     * @param paragraph the new paragraph object.
     */
    public void AddParagraph(Paragraph paragraph) {
        this.paragraphs.Add(paragraph);
    }

    /**
     * Removes the last paragraph added to this text column.
     */
    public void RemoveLastParagraph() {
        if (this.paragraphs.Count >= 1) {
            this.paragraphs.RemoveAt(this.paragraphs.Count - 1);
        }
    }

    /**
     * Returns dimension object containing the width and height of this component.
     * Please see Example_29.
     *
     * @Return dimension object containing the width and height of this component.
     */
    public Dimension GetSize() {
        float[] xy = DrawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }

    /**
     * Draws this text column on the specified page.
     *
     * @param page the page to draw this text column on.
     * @return the point with x and y coordinates of the location where to draw the next component.
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
        if (fixedHeight > 0f) {
            xy[1] = y + fixedHeight;
        }
        return xy;
    }

    private float[] DrawParagraphOn(Page page, Paragraph paragraph) {
        List<TextLine> list = new List<TextLine>();
        float lineHeight = 0f;
        float maxAscent = 0f;
        foreach (TextLine line in paragraph.lines) {
            if ((line.GetHeight() * lineSpacing) > lineHeight) {
                lineHeight = line.GetHeight() * lineSpacing;
            }
            if (line.font.GetAscent(line.fontSize) > maxAscent) {
                maxAscent = line.font.GetAscent(line.fontSize);
            }
        }
        if (rotate == 0) {
            y1 += maxAscent;
        } else if (rotate == 90) {
            x1 += maxAscent;
        } else if (rotate == 270) {
            x1 -= maxAscent;
        }

        float runLength = 0f;
        foreach (TextLine line in paragraph.lines) {
            String[] tokens = Regex.Split(line.text, @"\s+");
            TextLine text = null;
            foreach (String token in tokens) {
                text = new TextLine(line.font, token + Single.space);
                text.SetFallbackFont(line.GetFallbackFont());
                text.SetFontSize(line.GetFontSize());
                text.SetTextColor(line.GetTextColor());
                text.SetUnderline(line.GetUnderline());
                text.SetStrikeout(line.GetStrikeout());
                text.SetTextEffect(line.GetTextEffect());
                text.SetURIAction(line.GetURIAction());
                text.SetGoToAction(line.GetGoToAction());
                runLength += text.GetWidth();
                if (runLength < this.w) {
                    list.Add(text);
                } else {
                    DrawLineOfText(page, list);
                    MoveToNextLine(lineHeight);
                    list.Clear();
                    list.Add(text);
                    runLength = text.GetWidth();
                }
            }
            text.isLastToken = true;
        }
        DrawNonJustifiedLine(page, list);

        if (lineBetweenParagraphs) {
            MoveToNextLine(lineHeight);
        }

        return MoveToNextParagraph(lineHeight * this.paragraphSpacing);
    }

    private float[] MoveToNextLine(float lineHeight) {
        if (rotate == 0) {
            x1 = x;
            y1 += lineHeight;
        } else if (rotate == 90) {
            x1 += lineHeight;
            y1 = y;
        } else if (rotate == 270) {
            x1 -= lineHeight;
            y1 = y;
        }
        return new float[] {x1, y1};
    }

    private float[] MoveToNextParagraph(float paragraphSpacing) {
        if (rotate == 0) {
            x1 = x;
            y1 += paragraphSpacing;
        } else if (rotate == 90) {
            x1 += paragraphSpacing;
            y1 = y;
        } else if (rotate == 270) {
            x1 -= paragraphSpacing;
            y1 = y;
        }
        return new float[] {x1 + this.w, y1};
    }

    private float[] DrawLineOfText(Page page, List<TextLine> list) {
        if (alignment == Align.JUSTIFY) {
            float sumOfWordWidths = 0f;
            foreach (TextLine textLine in list) {
                sumOfWordWidths += textLine.GetWidth();
            }

            float dx = (w - sumOfWordWidths) / (list.Count - 1);
            foreach (TextLine textLine in list) {
                textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());
                if (textLine.GetGoToAction() != null) {
                    page.AddAnnotation(new Annotation(
                            Annotation.Link,
                            x,
                            y - textLine.font.GetAscent(),
                            x + textLine.GetWidth(),
                            y + textLine.font.GetDescent(),
                            null,                       // Vertices
                            null,                       // Fill Color
                            0f,                         // Transparency
                            null,                       // Title
                            null,                       // Contents
                            null,                       // The URI
                            textLine.GetGoToAction(),   // The destination name
                            null,
                            null,
                            null));
                }

                if (rotate == 0) {
                    textLine.SetTextDirection(0);
                    textLine.DrawOn(page);
                    x1 += textLine.GetWidth() + dx;
                } else if (rotate == 90) {
                    textLine.SetTextDirection(90);
                    textLine.DrawOn(page);
                    y1 -= textLine.GetWidth() + dx;
                } else if (rotate == 270) {
                    textLine.SetTextDirection(270);
                    textLine.DrawOn(page);
                    y1 += textLine.GetWidth() + dx;
                }
            }
        } else {
            return DrawNonJustifiedLine(page, list);
        }

        return new float[] {x1, y1};
    }

    private float[] DrawNonJustifiedLine(Page page, List<TextLine> list) {
        float runLength = 0f;
        foreach (TextLine textLine in list) {
            runLength += textLine.GetWidth();
        }

        if (alignment == Align.CENTER) {
            if (rotate == 0) {
                x1 = x + ((w - runLength) / 2);
            } else if (rotate == 90) {
                y1 = y - ((w - runLength) / 2);
            } else if (rotate == 270) {
                y1 = y + ((w - runLength) / 2);
            }
        } else if (alignment == Align.RIGHT) {
            if (rotate == 0) {
                x1 = x + (w - runLength);
            } else if (rotate == 90) {
                y1 = y - (w - runLength);
            } else if (rotate == 270) {
                y1 = y + (w - runLength);
            }
        }

        foreach (TextLine textLine in list) {
            textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());

            if (textLine.GetGoToAction() != null) {
                page.AddAnnotation(new Annotation(
                        Annotation.Link,
                        x,
                        y - textLine.font.GetAscent(),
                        x + textLine.GetWidth(),
                        y + textLine.font.GetDescent(),
                        null,                       // Vertices
                        null,                       // Fill Color
                        0f,                         // Transparency
                        null,                       // Title
                        null,                       // Contents
                        null,                       // The URI
                        textLine.GetGoToAction(),   // The destination name
                        null,
                        null,
                        null));
            }

            if (rotate == 0) {
                textLine.SetTextDirection(0);
                textLine.DrawOn(page);
                x1 += textLine.GetWidth();
            } else if (rotate == 90) {
                textLine.SetTextDirection(90);
                textLine.DrawOn(page);
                y1 -= textLine.GetWidth();
            } else if (rotate == 270) {
                textLine.SetTextDirection(270);
                textLine.DrawOn(page);
                y1 += textLine.GetWidth();
            }
        }

        return new float[] {x1, y1};
    }

    /**
     * Adds a new paragraph with Chinese text to this text column.
     *
     * @param font the font used by this paragraph.
     * @param chinese the Chinese text.
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
     * Adds a new paragraph with Japanese text to this text column.
     *
     * @param font the font used by this paragraph.
     * @param japanese the Japanese text.
     */
    public void AddJapaneseParagraph(Font font, String japanese) {
        AddChineseParagraph(font, japanese);
    }
}   // End of TextColumn.cs
}   // End of namespace PDFjet.NET
