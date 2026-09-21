/*
 * State.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
class State {
    private float[] brushColor;
    private bool brushColorWritten;
    private float[] penColor;
    private bool penColorWritten;
    private float penWidth;
    private bool penWidthWritten;
    private Font writtenFont;
    private float writtenFontSize;
    private CapStyle lineCapStyle;
    private JoinStyle lineJoinStyle;
    private String strokeDashPattern;
    // The height of the page, which Transform divides by the vertical scale
    // and which every y coordinate is measured from.
    private float height;

    public State(
            float[] brushColor,
            bool brushColorWritten,
            float[] penColor,
            bool penColorWritten,
            float penWidth,
            bool penWidthWritten,
            Font writtenFont,
            float writtenFontSize,
            CapStyle lineCapStyle,
            JoinStyle lineJoinStyle,
            String strokeDashPattern,
            float height) {
        // Copies of the colors, so the saved state does not change with the
        // arrays of the page after it is saved.
        this.brushColor = new float[] { brushColor[0], brushColor[1], brushColor[2] };
        this.brushColorWritten = brushColorWritten;
        this.penColor = new float[] { penColor[0], penColor[1], penColor[2] };
        this.penColorWritten = penColorWritten;
        this.penWidth = penWidth;
        this.penWidthWritten = penWidthWritten;
        this.writtenFont = writtenFont;
        this.writtenFontSize = writtenFontSize;
        this.lineCapStyle = lineCapStyle;
        this.lineJoinStyle = lineJoinStyle;
        this.strokeDashPattern = strokeDashPattern;
        this.height = height;
    }

    public float GetHeight() {
        return height;
    }

    public float[] GetBrushColor() {
        return (float[]) brushColor.Clone();
    }

    public float[] GetPenColor() {
        return (float[]) penColor.Clone();
    }

    public bool GetBrushColorWritten() {
        return brushColorWritten;
    }

    public bool GetPenColorWritten() {
        return penColorWritten;
    }

    public bool GetPenWidthWritten() {
        return penWidthWritten;
    }

    public Font GetWrittenFont() {
        return writtenFont;
    }

    public float GetWrittenFontSize() {
        return writtenFontSize;
    }

    public float GetPenWidth() {
        return penWidth;
    }

    public CapStyle GetLineCapStyle() {
        return lineCapStyle;
    }

    public JoinStyle GetLineJoinStyle() {
        return lineJoinStyle;
    }

    public String GetStrokeDashPattern() {
        return strokeDashPattern;
    }
}   // End of State.cs
}   // End of namespace PDFjet.NET
