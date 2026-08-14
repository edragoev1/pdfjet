/**
 * Form.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

/**
 * Please see Example_45
 */
namespace PDFjet.NET {
public class Form : IDrawable {
    private List<Field> fields;
    private float x;
    private float y;
    private Font f1;
    private Font f2;
    private float labelFontSize = 8f;
    private float valueFontSize = 10f;
    private int numberOfRows;
    private float rowLength = 500f;
    private float rowHeight = 12f;
    private float[] labelColor = new float[] {0f, 0f, 0f};
    private float[] valueColor = new float[] {0f, 0f, 1f};

    public Form(List<Field> fields) {
        this.fields = fields;
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public Form SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public Form SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    public Form SetRowLength(float rowLength) {
        this.rowLength = rowLength;
        return this;
    }

    public Form SetRowHeight(float rowHeight) {
        this.rowHeight = rowHeight;
        return this;
    }

    public Form SetLabelFont(Font f1) {
        this.f1 = f1;
        return this;
    }

    public Form SetLabelFontSize(float labelFontSize) {
        this.labelFontSize = labelFontSize;
        return this;
    }

    public Form SetValueFont(Font f2) {
        this.f2 = f2;
        return this;
    }

    public Form SetValueFontSize(float valueFontSize) {
        this.valueFontSize = valueFontSize;
        return this;
    }

    public Form SetLabelColor(float[] labelColor) {
        this.labelColor = labelColor;
        return this;
    }

    public Form SetValueColor(float[] valueColor) {
        this.valueColor = valueColor;
        return this;
    }

    /**
     * Draws this form on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        foreach (Field field in fields) {
            if (field.format) {
                field.values = Format(field.values[0], field.values[1], this.f2, this.rowLength);
                field.altDescription = new String[field.values.Length];
                field.actualText = new String[field.values.Length];
                for (int i = 0; i < field.values.Length; i++) {
                    field.altDescription[i] = field.values[i];
                    field.actualText[i] = field.values[i];
                }
            }
            if (field.x == 0f) {
                numberOfRows += field.values.Length;
            }
        }

        if (numberOfRows == 0) {
            return new float[] { x, y };
        }

        float boxHeight = rowHeight*numberOfRows + f2.GetDescent();
        Box box = new Box();
        box.SetLocation(x, y);
        box.SetSize(rowLength, boxHeight);
        if (page != null) {
            box.DrawOn(page);
        }

        float yField = 0f;
        int rowSpan = 1;
        float yRow = 0;
        foreach (Field field in fields) {
            if (field.x == 0f) {
                yRow += rowSpan*rowHeight;
                rowSpan = field.values.Length;
            }
            yField = yRow;
            for (int i = 0; i < field.values.Length; i++) {
                if (page != null) {
                    Font font = (i == 0) ? f1 : f2;
                    float fontSize = (i == 0) ? labelFontSize : valueFontSize;
                    float[] color = (i == 0) ? labelColor : valueColor;
                    new TextLine(font, field.values[i])
                            .SetFontSize(fontSize)
                            .SetTextColor(color)
                            .SetAltDescription((i == 0) ? field.altDescription[i] : (field.altDescription[i] + ","))
                            .SetLocation(2f + x + field.x, y + yField)
                            .DrawOn(page);
                    if (page != null && i == (field.values.Length - 1)) {
                        new Line(x, y + yField + font.GetDescent(),
                                x + rowLength, y + yField + font.GetDescent()).DrawOn(page);
                        new Line(x + field.x, y + yField + font.GetDescent() - (field.values.Length-1)*rowHeight,
                                x + field.x, y + yField + font.GetDescent()).DrawOn(page);
                    }
                }
                yField += rowHeight;
            }
        }

        return new float[] { x + rowLength, y + yField };
    }

    public static String[] Format(
            String title, String text, Font font, float width) {
        String[] original = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (original[original.Length - 1].Equals("")) {
            String[] truncated = new String[original.Length - 1];
            Array.Copy(original , truncated , truncated.Length);
            original = truncated;
        }

        List<String> lines = new List<String>();
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < original.Length; i++) {
            String line = original[i];
            if (font.StringWidth(line) < width) {
                lines.Add(line);
                continue;
            }

            buf.Length = 0;
            for (int j = 0; j < line.Length; j++) {
                buf.Append(line[j]);
                if (font.StringWidth(buf.ToString()) > (width - font.StringWidth("   "))) {
                    while (j > 0 && line[j] != ' ') {
                        j -= 1;
                    }
                    String str = line.Substring(0, j).TrimEnd();
                    lines.Add(str);
                    buf.Length = 0;
                    while (j < line.Length && line[j] == ' ') {
                        j += 1;
                    }
                    line = line.Substring(j);
                    j = 0;
                }
            }
            if (!line.Equals("")) {
                lines.Add(line);
            }
        }

        int count = lines.Count;
        String[] data = new String[1 + count];
        data[0] = title;
        for (int i = 0; i < count; i++) {
            data[i + 1] = lines[i];
        }

        return data;
    }
}   // End of Form.cs
}   // End of namespace PDFjet.NET
