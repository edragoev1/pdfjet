package com.pdfjet;

import java.util.*;

public class TextParameters {
    Font font;
    float fontSize;
    float x;
    float y;
    String text;

    // Constructor to initialize with default values (optional)
    public TextParameters() {
        this.fontSize = 12f;    // Default font size
        this.x = 0f;            // Default X
        this.y = 0f;            // Default Y
    }

    // Method to set the font
    public TextParameters setFont(Font font) {
        this.font = font;
        return this;
    }

    // Method to set the font size
    public TextParameters setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    // Method to set the location (X, Y)
    public TextParameters setTextLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public TextParameters setText(String text) {
        this.text = text;
        return this;
    }
}
