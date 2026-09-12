/*
 * TextColumn.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
///  Used to create text column objects and draw them on a page.
///
///  Please see Example_10 and Example_29.
/// </summary>
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

    /// <summary>
    /// Create a text column object.
    /// </summary>
    public TextColumn() {
        this.paragraphs = new List<Paragraph>();
    }

    /// <summary>
    /// Create a text column object and set the rotation angle.
    /// </summary>
    /// <param name="rotateByDegrees">the specified rotation angle in degrees.</param>
    public TextColumn(int rotateByDegrees) {
        this.rotate = rotateByDegrees;
        if (rotate == 0 || rotate == 90 || rotate == 270) {
        } else {
            throw new Exception(
                    "Invalid rotation angle. Please use 0, 90 or 270 degrees.");
        }
        this.paragraphs = new List<Paragraph>();
    }

    /// <summary>
    /// Sets the lineBetweenParagraphs private variable value.
    /// If the value is set to true - an empty line will be inserted between the current and next paragraphs.
    /// </summary>
    /// <param name="lineBetweenParagraphs">the specified bool value.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetLineBetweenParagraphs(bool lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
        return this;
    }

    /// <summary>Sets the spacing between the lines.</summary>
    public TextColumn SetLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
        return this;
    }

    /// <summary>
    /// Sets the spacing between the lines in this text column.
    /// </summary>
    /// <param name="lineSpacing">the specified spacing value.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetLineSpacing(double lineSpacing) {
        this.lineSpacing = (float) lineSpacing;
        return this;
    }

    /// <summary>Sets the space between paragraphs.</summary>
    public TextColumn SetParagraphSpacing(float paragraphSpacing) {
        this.paragraphSpacing = paragraphSpacing;
        return this;
    }

    /// <summary>Sets the space between paragraphs.</summary>
    public TextColumn SetParagraphSpacing(double paragraphSpacing) {
        this.paragraphSpacing = (float) paragraphSpacing;
        return this;
    }

    /// <summary>
    /// Sets the position of this text column on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of this text column when drawn on the page.</param>
    /// <param name="y">the y coordinate of the top left corner of this text column when drawn on the page.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location of this text column on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner.</param>
    /// <param name="y">the y coordinate of the top left corner.</param>
    public TextColumn SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    /// Sets the desired width of this text column.
    /// </summary>
    /// <param name="w">the width of this text column.</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetWidth(float w) {
        this.w = w;
        return this;
    }

    /// <summary>Returns the width of this text column.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Sets the height of this text column.</summary>
    public TextColumn SetHeight(float h) {
        this.h = h;
        return this;
    }

    /// <summary>Returns the height of this text column.</summary>
    public float GetHeight() {
        return this.h;
    }

    /// <summary>
    /// Sets the text alignment.
    /// </summary>
    /// <param name="alignment">the specified alignment code.
    ///      Supported values: Align.LEFT, Align.RIGHT. Align.CENTER and Align.JUSTIFY</param>
    /// <returns>this TextColumn object.</returns>
    public TextColumn SetAlignment(uint alignment) {
        this.alignment = alignment;
        return this;
    }

    /// <summary>
    /// Adds a new paragraph to this text column.
    /// </summary>
    /// <param name="paragraph">the new paragraph object.</param>
    public void AddParagraph(Paragraph paragraph) {
        this.paragraphs.Add(paragraph);
    }

    /// <summary>
    /// Removes the last paragraph added to this text column.
    /// </summary>
    public void RemoveLastParagraph() {
        if (this.paragraphs.Count >= 1) {
            this.paragraphs.RemoveAt(this.paragraphs.Count - 1);
        }
    }

    /// <summary>
    /// Returns dimension object containing the width and height of this component.
    /// Please see Example_29.
    /// </summary>
    /// <returns>dimension object containing the width and height of this component.</returns>
    public Dimension GetSize() {
        float[] xy = DrawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }

    /// <summary>
    /// Draws this text column on the specified page.
    /// </summary>
    /// <param name="page">the page to draw this text column on.</param>
    /// <returns>the point with x and y coordinates of the location where to draw the next component.</returns>
    public float[] DrawOn(Page page) {
        float[] xy = new float[] {x, y};
        foreach (Paragraph paragraph in paragraphs) {
            this.alignment = paragraph.alignment;
            xy = DrawParagraphOn(page, paragraph);
        }
        // Restore the original location
        SetLocation(this.x, this.y);
        if (this.GetHeight() > xy[1]) {
            xy[1] = this.GetHeight();
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
            // The ASCII whitespace that Java's \s matches; a no-break space does not break a line.
            String[] tokens = line.text.Split(
                    new char[] {' ', '\t', '\n', '\x0B', '\f', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            TextLine text = null;
            foreach (String token in tokens) {
                text = new TextLine(line.font, token + Single.space);
                text.SetFallbackFont(line.GetFallbackFont());
                text.SetFontSize(line.GetFontSize());
                text.SetTextColor(line.GetTextColor());
                text.SetUnderline(line.GetUnderline());
                text.SetStrikeout(line.GetStrikeout());
                text.SetVerticalOffset(line.GetVerticalOffset());
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
            if (text != null) {
                text.isLastToken = true;
            }
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
        return new float[] {x1, y1};
    }

    private float[] DrawLineOfText(Page page, List<TextLine> list) {
        if (alignment == Align.JUSTIFY) {
            float sumOfWordWidths = 0f;
            foreach (TextLine textLine in list) {
                sumOfWordWidths += textLine.GetWidth();
            }

            float dx = (w - sumOfWordWidths) / (list.Count - 1);
            // Each token draws its own link annotation when the line has a URI or GoTo action.
            foreach (TextLine textLine in list) {
                textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());
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

        // Each token draws its own link annotation when the line has a URI or GoTo action.
        foreach (TextLine textLine in list) {
            textLine.SetLocation(x1, y1 + textLine.GetVerticalOffset());
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

    /// <summary>
    /// Adds a new paragraph with Chinese text to this text column.
    /// </summary>
    /// <param name="font">the font used by this paragraph.</param>
    /// <param name="chinese">the Chinese text.</param>
    public void AddChineseParagraph(Font font, String chinese) {
        Paragraph paragraph;
        StringBuilder buf = new StringBuilder();
        foreach (char ch in chinese) {
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

    /// <summary>
    /// Adds a new paragraph with Japanese text to this text column.
    /// </summary>
    /// <param name="font">the font used by this paragraph.</param>
    /// <param name="japanese">the Japanese text.</param>
    public void AddJapaneseParagraph(Font font, String japanese) {
        AddChineseParagraph(font, japanese);
    }
}   // End of TextColumn.cs
}   // End of namespace PDFjet.NET
