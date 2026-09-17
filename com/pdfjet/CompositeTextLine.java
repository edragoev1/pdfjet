/*
 * CompositeTextLine.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * This class was designed and implemented by Jon T. Swanson, Ph.D.
 *
 * Refactored and integrated into the project by Eugene Dragoev - 2nd June 2012.
 * Used to create composite text line objects.
 */
public class CompositeTextLine implements BaselineDrawable {
    private static final int X = 0;
    private static final int Y = 1;

    private List<TextLine> textLines = new ArrayList<TextLine>();
    // The font size each component had when it was added. It is the base of
    // the script size and offset of that component when this composite text
    // line has no font size of its own.
    private List<Float> fontSizes = new ArrayList<Float>();

    private float[] position = new float[2];
    private float[] current  = new float[2];

    // Subscript and Superscript size factors
    private float subscriptFactor   = 0.583f;
    private float superscriptFactor = 0.583f;

    // Subscript and Superscript positions in relation to the base font
    private float superscriptPosition = 0.350f;
    private float subscriptPosition   = 0.141f;

    private float fontSize = 0f;

    /**
     * Creates a composite text line at the specified location.
     *
     * @param x the x coordinate.
     * @param y the y coordinate.
     */
    public CompositeTextLine(float x, float y) {
        position[X] = x;
        position[Y] = y;
        current[X]  = x;
        current[Y]  = y;
    }

    /**
     *  Sets the font size.
     *
     *  @param fontSize the font size.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine setFontSize(float fontSize) {
        this.fontSize = fontSize;
        layoutComponents();     // the components added before this call follow it
        return this;
    }

    /**
     *  Gets the font size.
     *
     *  @return fontSize the font size.
     */
    public float getFontSize() {
        return fontSize;
    }

    /**
     *  Sets the superscript factor for this composite text line.
     *
     *  @param superscript the superscript size factor.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine setSuperscriptFactor(float superscript) {
        this.superscriptFactor = superscript;
        layoutComponents();
        return this;
    }

    /**
     *  Gets the superscript factor for this text line.
     *
     *  @return superscript the superscript size factor.
     */
    public float getSuperscriptFactor() {
        return superscriptFactor;
    }

    /**
     *  Sets the subscript factor for this composite text line.
     *
     *  @param subscript the subscript size factor.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine setSubscriptFactor(float subscript) {
        this.subscriptFactor = subscript;
        layoutComponents();
        return this;
    }

    /**
     *  Gets the subscript factor for this text line.
     *
     *  @return subscript the subscript size factor.
     */
    public float getSubscriptFactor() {
        return subscriptFactor;
    }

    /**
     *  Sets the superscript position for this composite text line.
     *
     *  @param superscriptPosition the superscript position.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine setSuperscriptPosition(float superscriptPosition) {
        this.superscriptPosition = superscriptPosition;
        layoutComponents();
        return this;
    }

    /**
     *  Gets the superscript position for this text line.
     *
     *  @return superscriptPosition the superscript position.
     */
    public float getSuperscriptPosition() {
        return superscriptPosition;
    }

    /**
     *  Sets the subscript position for this composite text line.
     *
     *  @param subscriptPosition the subscript position.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine setSubscriptPosition(float subscriptPosition) {
        this.subscriptPosition = subscriptPosition;
        layoutComponents();
        return this;
    }

    /**
     *  Gets the subscript position for this text line.
     *
     *  @return subscriptPosition the subscript position.
     */
    public float getSubscriptPosition() {
        return subscriptPosition;
    }

    /**
     *  Add a new text line.
     *
     *  Find the current font, current size and effects (normal, super or subscript)
     *  Set the position of the component to the starting stored as current position
     *  Set the size and offset based on effects
     *  Set the new current position
     *
     *  @param component the component.
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine addComponent(TextLine component) {
        textLines.add(component);
        fontSizes.add(Float.valueOf(component.getFontSize()));
        place(component, baseFontSize(textLines.size() - 1));
        return this;
    }

    /**
     *  Adds the components of a chemical formula, in the specified font.
     *
     *  The digits that follow an element or a closing bracket are subscripts,
     *  as the 2 of H2O and the 6, 12 and 6 of C6H12O6; a run of digits and
     *  signs after a circumflex is a superscript, as the charge of Ca^2+ and
     *  SO4^2-; and everything else is drawn on the baseline, including a digit
     *  that begins the formula, as the 2 of 2H2O.
     *
     *  @param font the font of the formula.
     *  @param formula the formula, for example "C6H12O6" or "SO4^2-".
     *  @return this CompositeTextLine object.
     */
    public CompositeTextLine addFormula(Font font, String formula) {
        StringBuilder buf = new StringBuilder();
        int i = 0;
        while (i < formula.length()) {
            char ch = formula.charAt(i);
            if (ch == '^') {
                addRun(font, buf, ScriptPosition.NORMAL);
                i++;
                while (i < formula.length() && isDigitOrSign(formula.charAt(i))) {
                    buf.append(formula.charAt(i));
                    i++;
                }
                addRun(font, buf, ScriptPosition.SUPERSCRIPT);
            } else if (isDigit(ch) && followsAnElement(formula, i)) {
                addRun(font, buf, ScriptPosition.NORMAL);
                while (i < formula.length() && isDigit(formula.charAt(i))) {
                    buf.append(formula.charAt(i));
                    i++;
                }
                addRun(font, buf, ScriptPosition.SUBSCRIPT);
            } else {
                buf.append(ch);
                i++;
            }
        }
        addRun(font, buf, ScriptPosition.NORMAL);
        return this;
    }

    // Adds what the buffer holds as one component, and empties the buffer.
    private void addRun(Font font, StringBuilder buf, ScriptPosition scriptPosition) {
        if (buf.length() == 0) {
            return;
        }
        TextLine component = new TextLine(font, buf.toString());
        component.setScriptPosition(scriptPosition);
        buf.setLength(0);
        addComponent(component);
    }

    private static boolean isDigit(char ch) {
        return ch >= '0' && ch <= '9';
    }

    private static boolean isDigitOrSign(char ch) {
        return isDigit(ch) || ch == '+' || ch == '-';
    }

    // A digit is a subscript when it counts the atoms of the element or the
    // group before it, and plain text when it begins the formula.
    private static boolean followsAnElement(String formula, int index) {
        if (index == 0) {
            return false;
        }
        char ch = formula.charAt(index - 1);
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == ')' || ch == ']';
    }

    // The base of the script size and offset of the component: the font size
    // of this composite text line, or the one the component came with.
    private float baseFontSize(int index) {
        return (fontSize > 0f) ? fontSize : fontSizes.get(index).floatValue();
    }

    // Places the component at the current position, at the size its script
    // position asks for, and moves the current position past it. The size goes
    // on the TextLine: drawOn uses the line's own font size, so mutating the
    // shared Font here would have no effect.
    private void place(TextLine component, float base) {
        if (component.getScriptPosition() == ScriptPosition.SUPERSCRIPT) {
            component.setFontSize(base * superscriptFactor);
            component.setLocation(current[X], current[Y] - base * superscriptPosition);
        } else if (component.getScriptPosition() == ScriptPosition.SUBSCRIPT) {
            component.setFontSize(base * subscriptFactor);
            component.setLocation(current[X], current[Y] + base * subscriptPosition);
        } else {
            component.setFontSize(base);
            component.setLocation(current[X], current[Y]);
        }
        current[X] += component.getWidth();
    }

    // Places every component again, from the location of this composite text
    // line, after the location, the font size or a script setting changed.
    private void layoutComponents() {
        current[X] = position[X];
        current[Y] = position[Y];
        for (int i = 0; i < textLines.size(); i++) {
            place(textLines.get(i), baseFontSize(i));
        }
    }

    /**
     *  Loop through all the text lines and reset their location based on
     *  the new location set here.
     *
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     *  @return the CompositeTextLine object.
     */
    public CompositeTextLine setLocation(float x, float y) {
        position[X] = x;
        position[Y] = y;
        layoutComponents();
        return this;
    }

    /**
     *  Return the position of this composite text line.
     *
     *  @return the position of this composite text line.
     */
    public float[] getLocation() {
        return new float[] {position[0], position[1]};
    }

    /**
     *  Return the nth entry in the TextLine array.
     *
     *  @param index the index of the nth element.
     *  @return the text line at the specified index.
     */
    public TextLine getTextLine(int index) {
        if (textLines == null || textLines.size() == 0) {
            return null;
        }
        if (index < 0 || index > textLines.size() - 1) {
            return null;
        }
        return textLines.get(index);
    }

    /**
     *  Returns the number of text lines.
     *
     *  @return the number of text lines.
     */
    public int getNumberOfTextLines() {
        return textLines.size();
    }

    /**
     *  Returns the vertical coordinates of the top left and bottom right corners
     *  of the bounding box of this composite text line.
     *
     *  @return the an array containing the vertical coordinates.
     */
    public float[] getMinMaxY() {
        float min = position[Y];
        float max = position[Y];
        // Each component is measured where it is drawn, with the font size it
        // is drawn at, which a script position makes smaller than the base.
        for (TextLine component : textLines) {
            float baseline = component.getLocation()[1];
            float top = baseline - component.font.getAscent(component.getFontSize());
            float bottom = baseline + component.font.getDescent(component.getFontSize());
            if (top < min) {
                min = top;
            }
            if (bottom > max) {
                max = bottom;
            }
        }
        return new float[] {min, max};
    }

    /**
     *  Returns how far above its baseline this composite text line reaches:
     *  the ascent of the component that reaches highest, a superscript
     *  included.
     *
     *  @return the ascent, in points.
     */
    public float getAscent() {
        return position[Y] - getMinMaxY()[0];
    }

    /**
     *  Returns how far below its baseline this composite text line reaches:
     *  the descent of the component that reaches lowest, a subscript included.
     *
     *  @return the descent, in points.
     */
    public float getDescent() {
        return getMinMaxY()[1] - position[Y];
    }

    /**
     *  Returns the height of this CompositeTextLine.
     *
     *  @return the height.
     */
    public float getHeight() {
        float[] yy = getMinMaxY();
        return yy[1] - yy[0];
    }

    /**
     *  Returns the width of this CompositeTextLine.
     *
     *  @return the width.
     */
    public float getWidth() {
        float width = 0f;
        for (TextLine component : textLines) {
            width += component.getWidth();
        }
        return width;
    }

    /**
     *  Draws this line on the specified page.
     *
     *  @param page the page to draw this line on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        // A composite text line with no component reaches its own location.
        float xMax = position[X];
        float yMax = position[Y];
        // Loop through all the text lines and draw them on the page
        for (TextLine textLine : textLines) {
            float[] xy = textLine.drawOn(page);
            xMax = Math.max(xMax, xy[0]);
            yMax = Math.max(yMax, xy[1]);
        }
        return new float[] {xMax, yMax};
    }
}
