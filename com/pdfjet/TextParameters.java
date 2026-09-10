package com.pdfjet;

import java.util.*;

/**
 * The font, font size, location and text for Stamp.drawText.
 */
public class TextParameters {
    Font font;
    float fontSize;
    float x;
    float y;
    String text;

    /**
     * Creates text parameters with a font size of 12, located at (0, 0).
     */
    public TextParameters() {
        this.fontSize = 12f;    // Default font size
        this.x = 0f;            // Default X
        this.y = 0f;            // Default Y
    }

    /**
     * Sets the font.
     *
     * @param font the font.
     * @return this TextParameters object.
     */
    public TextParameters setFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Sets the font size.
     *
     * @param fontSize the font size.
     * @return this TextParameters object.
     */
    public TextParameters setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the location of the text.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     * @return this TextParameters object.
     */
    public TextParameters setTextLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the text.
     *
     * @param text the text.
     * @return this TextParameters object.
     */
    public TextParameters setText(String text) {
        this.text = text;
        return this;
    }
}
