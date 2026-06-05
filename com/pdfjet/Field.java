/**
 * Field.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Please see Example_45
 */
public class Field {
    float x;
    String[] values;
    String[] actualText;
    String[] altDescription;
    boolean format = false;

    /**
     * Creates a Field that will be used in a Form
     *
     * @param x the horizontal position within the Form
     * @param values the values contained in this field
     */
    public Field(float x, String[] values) {
        this(x, values, false);
    }

    /**
     * Creates a Field that will be used in a Form
     *
     * @param x the horizontal position within the Form
     * @param values the values contained in this field
     * @param format format the value or not ...
     */
    public Field(float x, String[] values, boolean format) {
        this.x = x;
        this.values = values;
        this.format = format;
        if (values != null) {
            this.actualText     = new String[values.length];
            this.altDescription = new String[values.length];
            for (int i = 0; i < values.length; i++) {
                this.actualText[i]     = values[i];
                this.altDescription[i] = values[i];
            }
        }
    }

    /**
     * Sets the alternative description for this field
     *
     * @param altDescription the alternative description
     * @return this field
     */
    public Field setAltDescription(String altDescription) {
        this.altDescription[0] = altDescription;
        return this;
    }

    /**
     * Sets the actual text for the field
     *
     * @param actualText the actual text in the field
     * @return this field
     */
    public Field setActualText(String actualText) {
        this.actualText[0] = actualText;
        return this;
    }
}
