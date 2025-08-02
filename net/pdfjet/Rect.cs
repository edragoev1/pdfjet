/**
 *  Rect.cs
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
using System.Collections.Generic;

namespace PDFjet.NET {
public class Rect {
    private float x;
    private float y;
    private float w;
    private float h;
    private float r;
    private int color;
    private float width;
    private string pattern;
    private bool fillShape;
    private string uri;
    private string key;
    private string language;
    private string altDescription;
    private string actualText;
    private string structureType;

    public Rect() {
        this.color = Color.black;
        this.width = 0.0f;
        this.pattern = "[] 0";
        this.altDescription = "";   // TODO:
        this.actualText = "";
        this.structureType = "P"; // StructureType.P; TODO
    }

    public Rect(float x, float y, float w, float h) : this() {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    public Rect SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void SetBorderColor(int color) {
        this.color = color;
    }

    public void SetLineWidth(float width) {
        this.width = width;
    }

    public void SetCornerRadius(float r) {
        this.r = r;
    }

    public void SetURIAction(string uri) {
        this.uri = uri;
    }

    public void SetGoToAction(string key) {
        this.key = key;
    }

    public Rect SetAltDescription(string altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    public Rect SetActualText(string actualText) {
        this.actualText = actualText;
        return this;
    }

    public Rect SetStructureType(string structureType) {
        this.structureType = structureType;
        return this;
    }

    public void SetPattern(string pattern) {
        this.pattern = pattern;
    }

    public void SetFillShape(bool fillShape) {
        this.fillShape = fillShape;
    }

    public void PlaceIn(Rect rect, float xOffset, float yOffset) {
        this.x = rect.x + xOffset;
        this.y = rect.y + yOffset;
    }

    public void ScaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    public float[] DrawOn(Page page) {
        const float k = 0.5517f;

        page.AddBMC(this.structureType, this.language, this.actualText, this.altDescription);
        if (this.r == 0.0f) {
            page.MoveTo(this.x, this.y);
            page.LineTo(this.x + this.w, this.y);
            page.LineTo(this.x + this.w, this.y + this.h);
            page.LineTo(this.x, this.y + this.h);
            if (this.fillShape) {
                page.SetBrushColor(this.color);
                page.FillPath();
            } else {
                page.SetPenWidth(this.width);
                page.SetPenColor(this.color);
                page.SetLinePattern(this.pattern);
                page.ClosePath();
            }
        } else {
            page.SetPenWidth(this.width);
            page.SetPenColor(this.color);
            page.SetLinePattern(this.pattern);

            List<Point> points = new List<Point> {
                new Point((this.x + this.r), this.y, false),
                new Point((this.x + this.w) - this.r, this.y, false),
                new Point((this.x + this.w - this.r) + this.r * k, this.y, true),
                new Point((this.x + this.w), (this.y + this.r) - this.r * k, true),
                new Point((this.x + this.w), (this.y + this.r), false),
                new Point((this.x + this.w), (this.y + this.h) - this.r, false),
                new Point((this.x + this.w), ((this.y + this.h) - this.r) + this.r * k, true),
                new Point(((this.x + this.w) - this.r) + this.r * k, (this.y + this.h), true),
                new Point(((this.x + this.w) - this.r), (this.y + this.h), false),
                new Point((this.x + this.r), (this.y + this.h), false),
                new Point(((this.x + this.r) - this.r * k), (this.y + this.h), true),
                new Point(this.x, ((this.y + this.h) - this.r) + this.r * k, true),
                new Point(this.x, (this.y + this.h) - this.r, false),
                new Point(this.x, (this.y + this.r), false),
                new Point(this.x, (this.y + this.r) - this.r * k, true),
                new Point((this.x + this.r) - this.r * k, this.y, true),
                new Point((this.x + this.r), this.y, false)
            };

            page.DrawPath(points, Operation.STROKE);
        }
        page.AddEMC();

        if (this.uri != null || this.key != null) {
            page.AddAnnotation(new Annotation(
                this.uri,
                this.key,
                this.x,
                this.y,
                this.x + this.w,
                this.y + this.h,
                this.language,
                this.actualText,
                this.altDescription
            ));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
}
