/**
 * State.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
class State {
    private float[] brushColor;
    private float[] penColor;
    private float penWidth;
    private CapStyle lineCapStyle;
    private JoinStyle lineJoinStyle;
    private String strokeDashPattern;

    public State(
            float[] brushColor,
            float[] penColor,
            float penWidth,
            CapStyle lineCapStyle,
            JoinStyle lineJoinStyle,
            String strokeDashPattern) {
        this.brushColor = new float[] { brushColor[0], brushColor[1], brushColor[2] }; // TODO: Is this needed?
        this.penColor = new float[] { penColor[0], penColor[1], penColor[2] };         // Creating new objects?
        this.penWidth = penWidth;
        this.lineCapStyle = lineCapStyle;
        this.lineJoinStyle = lineJoinStyle;
        this.strokeDashPattern = strokeDashPattern;
    }

    public float[] GetBrushColor() {
        return brushColor;
    }

    public float[] GetPenColor() {
        return penColor;
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
