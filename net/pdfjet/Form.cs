/**
 *  Form.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Text;
using System.Collections.Generic;


/**
 *  Please see Example_45
 */
namespace PDFjet.NET {
public class Form : IDrawable {

    private List<Field> fields;
    private float x;
    private float y;
    private Font f1;
    private float labelFontSize = 8f;
    private Font f2;
    private float valueFontSize = 10f;
    private int numberOfRows;
    private float rowLength = 500f;
    private float rowHeight = 12f;
    private int labelColor = Color.black;
    private int valueColor = Color.blue;
    private List<float[]> endOfLinePoints;


    public Form(List<Field> fields) {
        this.fields = fields;
        this.endOfLinePoints = new List<float[]>();
    }


    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public Form SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }


    public Form SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
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


    public Form SetLabelColor(int labelColor) {
        this.labelColor = labelColor;
        return this;
    }


    public Form SetValueColor(int valueColor) {
        this.valueColor = valueColor;
        return this;
    }


    /**
     *  Draws this form on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
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

        float boxHeight = rowHeight*numberOfRows;
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
                int color = (i == 0) ? labelColor : valueColor;
                    new TextLine(font, field.values[i])
                            .SetFontSize(fontSize)
                            .SetColor(color)
                            .PlaceIn(box, field.x + font.descent, yField - font.descent)
                            .SetAltDescription((i == 0) ? field.altDescription[i] : (field.altDescription[i] + ","))
                            .SetActualText((i == 0) ? field.actualText[i] : (field.actualText[i] + ","))
                            .DrawOn(page);
                    endOfLinePoints.Add(new float[] {
                            field.x + f1.descent + font.StringWidth(field.values[i]),
                            yField + font.descent,
                    });
                    if (page != null && i == (field.values.Length - 1)) {
                        new Line(0f, 0f, rowLength, 0f)
                                .PlaceIn(box, 0f, yField)
                                .DrawOn(page);
                        if (field.x != 0f) {
                            new Line(0f, -(field.values.Length-1)*rowHeight, 0f, 0f)
                                    .PlaceIn(box, field.x, yField)
                                    .DrawOn(page);
                        }
                    }
                }
                yField += rowHeight;
            }
        }

        return new float[] { x + rowLength, y + boxHeight };
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
