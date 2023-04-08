/**
 *  SVGImage.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/**
 * Used to embed SVG images in the PDF document.
 */
namespace PDFjet.NET {
public class SVGImage {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    String viewBox = null;
    int fill = Color.transparent;
    int stroke = Color.transparent;
    float strokeWidth = 0f;

    List<SVGPath> paths = null;
    protected String uri = null;
    protected String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param fontPath the path to the font file.
     */
    public SVGImage(String fontPath) : this(
        new BufferedStream(new FileStream(fontPath, FileMode.Open, FileAccess.Read))) {
    }

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public SVGImage(Stream stream) {
        paths = new List<SVGPath>();
        SVGPath path = null;
        StringBuilder buf = new StringBuilder();
        bool token = false;
        String param = null;
        bool header = false;
        int ch;
        while ((ch = stream.ReadByte()) != -1) {
            if (buf.ToString().EndsWith("<svg")) {
                header = true;
                buf.Length = 0;
            } else if (header && ch == '>') {
                header = false;
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" width=")) {
                token = true;
                param = "width";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" height=")) {
                token = true;
                param = "height";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" viewBox=")) {
                token = true;
                param = "viewBox";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" d=")) {
                token = true;
                if (path != null) {
                    paths.Add(path);
                }
                path = new SVGPath();
                param = "data";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" fill=")) {
                token = true;
                param = "fill";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" stroke=")) {
                token = true;
                param = "stroke";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" stroke-width=")) {
                token = true;
                param = "stroke-width";
                buf.Length = 0;
            } else if (token && ch == '\"') {
                token = false;
                if (param.Equals("width")) {
                    this.w = float.Parse(buf.ToString());
                } else if (param.Equals("height")) {
                    this.h = float.Parse(buf.ToString());
                } else if (param.Equals("viewBox")) {
                    this.viewBox = buf.ToString();
                } else if (param.Equals("data")) {
                    path.data = buf.ToString();
                } else if (param.Equals("fill")) {
                    int fillColor = getColor(buf.ToString());
                    if (header) {
                        this.fill = fillColor;
                    } else {
                        path.fill = fillColor;
                    }
                } else if (param.Equals("stroke")) {
                    int strokeColor = getColor(buf.ToString());
                    if (header) {
                        this.stroke = strokeColor;
                    } else {
                        path.stroke = strokeColor;
                    }
                } else if (param.Equals("stroke-width")) {
                    try {
                        float strokeWidth = float.Parse(buf.ToString());
                        if (header) {
                            this.strokeWidth = strokeWidth;
                        } else {
                            path.strokeWidth = strokeWidth;
                        }
                    } catch (Exception) {
                        path.strokeWidth = 0f;
                    }
                }
                buf.Length = 0;
            } else {
                buf.Append((char) ch);
            }
        }
        if (path != null) {
            paths.Add(path);
        }
        stream.Close();
        ProcessPaths(paths);
    }

    private void ProcessPaths(List<SVGPath> paths) {
        float[] box = new float[4];
        if (viewBox != null) {
            String[] list = Regex.Split(viewBox.Trim(), "\\s+");
            box[0] = float.Parse(list[0]);
            box[1] = float.Parse(list[1]);
            box[2] = float.Parse(list[2]);
            box[3] = float.Parse(list[3]);
        }
        foreach (SVGPath path in paths) {
            path.operations = SVG.GetOperations(path.data);
            path.operations = SVG.ToPDF(path.operations);
            if (viewBox != null) {
                foreach (PathOp op in path.operations) {
                    op.x = (op.x - box[0]) * w / box[2];
                    op.y = (op.y - box[1]) * h / box[3];
                    op.x1 = (op.x1 - box[0]) * w / box[2];
                    op.y1 = (op.y1 - box[1]) * h / box[3];
                    op.x2 = (op.x2 - box[0]) * w / box[2];
                    op.y2 = (op.y2 - box[1]) * h / box[3];
                }
            }
        }
    }

    private int getColor(String colorName) {
        if (colorName.StartsWith("#")) {
            if (colorName.Length == 7) {
                return Int32.Parse(colorName.Substring(1), NumberStyles.HexNumber);
            } else if (colorName.Length == 4) {
                String str = new String(new char[] {
                        colorName[1], colorName[1],
                        colorName[2], colorName[2],
                        colorName[3], colorName[3]
                });
                return Int32.Parse(str, NumberStyles.HexNumber);
            } else {
                return Color.transparent;
            }
        }
        int color = Color.transparent;
        try {
            color = (int) typeof(Color).GetField(colorName).GetValue(null);
        } catch (Exception) {
            return color;
        }
        return color;
    }

    /**
     *  Sets the location of this SVG on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @return this SVG object.
     */
    public SVGImage SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public void ScaleBy(float factor) {
        foreach (SVGPath path in paths) {
            foreach (PathOp op in path.operations) {
                op.x1 *= factor;
                op.y1 *= factor;
                op.x2 *= factor;
                op.y2 *= factor;
                op.x *= factor;
                op.y *= factor;
            }
        }
    }

    public float getWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    private void drawPath(SVGPath path, Page page) {
        int fillColor = path.fill;
        if (fillColor == Color.transparent) {
            fillColor = this.fill;
        }
        int strokeColor = path.stroke;
        if (strokeColor == Color.transparent) {
            strokeColor = this.stroke;
        }
        float strokeWidth = this.strokeWidth;
        if (path.strokeWidth > strokeWidth) {
            strokeWidth = path.strokeWidth;
        }

        if (fillColor == Color.transparent &&
                strokeColor == Color.transparent) {
            fillColor = Color.black;
        }

        page.SetBrushColor(fillColor);
        page.SetPenColor(strokeColor);
        page.SetPenWidth(strokeWidth);

        if (fillColor != Color.transparent) {
            for (int i = 0; i < path.operations.Count; i++) {
                PathOp op = path.operations[i];
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                }
            }
            page.FillPath();
        }

        if (strokeColor != Color.transparent) {
            for (int i = 0; i < path.operations.Count; i++) {
                PathOp op = path.operations[i];
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                    page.ClosePath();
                }
            }
        }
    }

    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        for (int i = 0; i < paths.Count; i++) {
            SVGPath path = paths[i];
            drawPath(path, page);
        }
        page.AddEMC();
        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    actualText,
                    altDescription));
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.cs
}   // End of PDFjet.NET namespace
