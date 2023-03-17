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
using System.IO;
using System.Text;

/**
 * Used to embed SVG images in the PDF document.
 */
namespace PDFjet.NET {
public class SVGImage {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    List<PathOp> pdfPathOps = null;

    private int color = Color.black;
    private float penWidth = 0.3f;
    private bool fillPath = true;

    protected String uri = null;
    protected String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public SVGImage(Stream stream) {
        List<String> paths = new List<String>();
        StringBuilder buf = new StringBuilder();
        bool token = false;
        String param = null;
        int ch;
        while ((ch = stream.ReadByte()) != -1) {
            if (!token && buf.ToString().EndsWith(" width=")) {
                token = true;
                param = "width";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" height=")) {
                token = true;
                param = "height";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith("<path d=")) {
                token = true;
                param = "path";
                buf.Length = 0;
            } else if (!token && buf.ToString().EndsWith(" fill=")) {
                token = true;
                param = "fill";
                buf.Length = 0;
            } else if (token && ch == '\"') {
                token = false;
                if (param.Equals("width")) {
                    w = float.Parse(buf.ToString());
                } else if (param.Equals("height")) {
                    h = float.Parse(buf.ToString());
                } else if (param.Equals("path")) {
                    paths.Add(buf.ToString());
                } else if (param.Equals("fill")) {
                    color = mapColorNameToValue(buf.ToString());
                }
                buf.Length = 0;
            } else {
                buf.Append((char) ch);
            }
        }
        stream.Close();
        List<PathOp> svgPathOps = SVG.GetSVGPathOps(paths);
        pdfPathOps = SVG.GetPDFPathOps(svgPathOps);
    }

    private int mapColorNameToValue(String colorName) {
        int color = Color.black;
        try {
            color = (int) typeof(Color).GetField(colorName).GetValue(null);
        } catch (Exception) {
            return color;
        }
        return color;
    }

    public List<PathOp> GetPDFPathOps() {
        return pdfPathOps;
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

    /**
     *  Sets the fill path flag to true or false.
     *
     *  @param fillPath if true fills that SVG path, strokes otherwise.
     */
    public void SetFillPath(bool fillPath) {
        this.fillPath = fillPath;
    }

    /**
     *  Sets the size of this box.
     *
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public void setSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void setPenWidth(float w) {
        this.w = w;
    }

    public void setHeight(float h) {
        this.h = h;
    }

    public float getPenWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        page.SetPenWidth(penWidth);
        if (fillPath) {
            page.SetBrushColor(color);
        }
        else {
            page.SetPenColor(color);
        }
        for (int i = 0; i < pdfPathOps.Count; i++) {
            PathOp op = pdfPathOps[i];
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
                if (!fillPath) {
                    page.ClosePath();
                }
            }
        }
        if (fillPath) {
            page.FillPath();
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
