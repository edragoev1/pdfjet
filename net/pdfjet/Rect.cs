/**
 * Rect.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class Rect  : IDrawable {
    internal float x;
    internal float y;
    private float w;
    private float h;
    private float r;

    private float[] fillColor;
    private float[] borderColor = new float[] {0f, 0f, 0f};
    private float borderWidth;
    private string borderPattern = "[] 0";

    private string uri;
    private string key;
    private string language = "en-US";
    private string actualText = null;
    private string altDescription = null;

    /**
     * The default constructor.
     */
    public Rect() {
    }

    public Rect(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    public Rect(double x, double y, double w, double h) {
        this.x = (float) x;
        this.y = (float) y;
        this.w = (float) w;
        this.h = (float) h;
    }

    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    public void SetSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(r, g, b);
    }

    public void SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
    }

    public void SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void SetBorderWidth(float width) {
        this.borderWidth = width;
    }

    public void SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetBorderColor(r, g, b);
    }

    public void SetBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
    }

    public void SetBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
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

    public void SetLanguage(String language) {
        this.language = language;
    }

    public Rect SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    public Rect SetAltDescription(string altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    public void SetBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
    }

    public void ScaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};
        }

        const float k = 0.55228f;
        page.Append("q\n");
        if (this.r == 0.0f) {
            if (fillColor != null) {
                page.SetBrushColor(this.fillColor);
                page.MoveTo(this.x, this.y);
                page.LineTo(this.x + this.w, this.y);
                page.LineTo(this.x + this.w, this.y + this.h);
                page.LineTo(this.x, this.y + this.h);
                page.LineTo(this.x, this.y);
                page.FillPath();
            }
            if (borderColor != null) {
                page.MoveTo(this.x, this.y);
                page.LineTo(this.x + this.w, this.y);
                page.LineTo(this.x + this.w, this.y + this.h);
                page.LineTo(this.x, this.y + this.h);
                page.SetPenColor(this.borderColor);
                page.SetPenWidth(this.borderWidth);
                page.SetStrokeDashPattern(this.borderPattern);
                page.ClosePath();
            }
        } else {
            List<Point> points = new List<Point> {
                new Point((this.x + this.r), this.y),
                new Point((this.x + this.w) - this.r, this.y),
                new Point((this.x + this.w - this.r) + this.r * k, this.y, Point.ControlPointC),
                new Point((this.x + this.w), (this.y + this.r) - this.r * k, Point.ControlPointC),
                new Point((this.x + this.w), (this.y + this.r)),
                new Point((this.x + this.w), (this.y + this.h) - this.r),
                new Point((this.x + this.w), ((this.y + this.h) - this.r) + this.r * k, Point.ControlPointC),
                new Point(((this.x + this.w) - this.r) + this.r * k, (this.y + this.h), Point.ControlPointC),
                new Point(((this.x + this.w) - this.r), (this.y + this.h)),
                new Point((this.x + this.r), (this.y + this.h)),
                new Point(((this.x + this.r) - this.r * k), (this.y + this.h), Point.ControlPointC),
                new Point(this.x, ((this.y + this.h) - this.r) + this.r * k, Point.ControlPointC),
                new Point(this.x, (this.y + this.h) - this.r),
                new Point(this.x, (this.y + this.r)),
                new Point(this.x, (this.y + this.r) - this.r * k, Point.ControlPointC),
                new Point((this.x + this.r) - this.r * k, this.y, Point.ControlPointC),
                new Point((this.x + this.r), this.y)
            };
            if (fillColor != null && borderColor == null) {
                page.DrawPath(points, PathOperator.Fill);
            } else if (fillColor == null && borderColor != null) {
                page.DrawPath(points, PathOperator.Stroke);
            } else if (fillColor != null && borderColor != null) {
                page.DrawPath(points, PathOperator.FillAndStroke);
            }

            if (borderColor != null && borderPattern != null) {
                page.SetStrokeDashPattern(borderPattern);
            }
            if (fillColor != null && borderColor != null) {
                page.SetBrushColor(fillColor);
                page.SetPenWidth(borderWidth);
                page.SetPenColor(borderColor);
                page.Append("B\n");
            } else if (fillColor != null && borderColor == null) {
                page.SetBrushColor(fillColor);
                page.Append("f\n");
            } else if (fillColor == null && borderColor != null) {
                page.SetPenWidth(borderWidth);
                page.SetPenColor(borderColor);
                page.Append("S\n");
            }
        }
        page.Append("Q\n");

        if (this.uri != null || this.key != null) {
            page.AddAnnotation(new Annotation(
                Annotation.Link,
                this.x,
                this.y,
                this.x + this.w,
                this.y + this.h,
                null,       // Vertices
                null,       // Fill Color
                0f,         // Transparency
                null,       // Title
                null,       // Contents
                this.uri,
                this.key,
                this.language,
                this.actualText,
                this.altDescription
            ));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
}
