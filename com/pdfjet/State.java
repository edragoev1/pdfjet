/*
 * State.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

class State {
    private float[] brushColor;
    private boolean brushColorWritten;
    private float[] penColor;
    private boolean penColorWritten;
    private float penWidth;
    private boolean penWidthWritten;
    private Font writtenFont;
    private float writtenFontSize;
    private CapStyle lineCapStyle;
    private JoinStyle lineJoinStyle;
    private String strokeDashPattern;
    // The height of the page, which transform divides by the vertical scale
    // and which every y coordinate is measured from.
    private float height;

    public State(
            float[] brushColor,
            boolean brushColorWritten,
            float[] penColor,
            boolean penColorWritten,
            float penWidth,
            boolean penWidthWritten,
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

    public float getHeight() {
        return height;
    }

    public float[] getBrushColor() {
        return brushColor.clone();
    }

    public boolean getBrushColorWritten() {
        return brushColorWritten;
    }

    public boolean getPenColorWritten() {
        return penColorWritten;
    }

    public float[] getPenColor() {
        return penColor.clone();
    }

    public float getPenWidth() {
        return penWidth;
    }

    public boolean getPenWidthWritten() {
        return penWidthWritten;
    }

    public Font getWrittenFont() {
        return writtenFont;
    }

    public float getWrittenFontSize() {
        return writtenFontSize;
    }

    public CapStyle getLineCapStyle() {
        return lineCapStyle;
    }

    public JoinStyle getLineJoinStyle() {
        return lineJoinStyle;
    }

    public String getStrokeDashPattern() {
        return strokeDashPattern;
    }
}   // End of State.java

