/**
 * CompositeTextLine.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * This class was designed and implemented by Jon T. Swanson, Ph.D.
 * Refactored and integrated into the project by Eugene Dragoev - 2nd June 2012.
 */
import Foundation

/**
 * Used to create composite text line objects.
 */
public class CompositeTextLine : BaselineDrawable {
    private let X = 0
    private let Y = 1

    private var textLines = [TextLine]()
    // The font size each component had when it was added. It is the base of
    // the script size and offset of that component when this composite text
    // line has no font size of its own.
    private var fontSizes = [Float]()

    private var position = [Float](repeating: 0, count: 2)
    private var current = [Float](repeating: 0, count: 2)

    // Subscript and Superscript size factors
    private var subscriptFactor: Float = 0.583
    private var superscriptFactor: Float  = 0.583

    // Subscript and Superscript positions in relation to the base font
    private var superscriptPosition: Float = 0.350
    private var subscriptPosition: Float = 0.141

    private var fontSize: Float = 0

    /// Creates a composite text line at the specified location.
    public init(_ x: Float, _ y: Float) {
        self.position[X] = x
        self.position[Y] = y
        self.current[X]  = x
        self.current[Y]  = y
    }

    /**
     * Sets the font size.
     *
     * - Parameter fontSize: the font size.
     */
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> CompositeTextLine {
        self.fontSize = fontSize
        layoutComponents()
        return self
    }

    /**
     * Gets the font size.
     *
     * - Returns: fontSize the font size.
     */
    public func getFontSize()-> Float {
        return self.fontSize
    }

    /**
     * Sets the superscript factor for this composite text line.
     *
     * - Parameter superscriptFactor: the superscript size factor.
     */
    @discardableResult
    public func setSuperscriptFactor(_ superscriptFactor: Float) -> CompositeTextLine {
        self.superscriptFactor = superscriptFactor
        layoutComponents()
        return self
    }

    /**
     * Gets the superscript factor for this text line.
     *
     * - Returns: superscript the superscript size factor.
     */
    public func getSuperscriptFactor()-> Float {
        return self.superscriptFactor
    }

    /**
     * Sets the subscript factor for this composite text line.
     *
     * - Parameter subscriptFactor: the subscript size factor.
     */
    @discardableResult
    public func setSubscriptFactor(_ subscriptFactor: Float) -> CompositeTextLine {
        self.subscriptFactor = subscriptFactor
        layoutComponents()
        return self
    }

    /**
     * Gets the subscript factor for this text line.
     *
     * - Returns: subscript the subscript size factor.
     */
    public func getSubscriptFactor()-> Float {
        return self.subscriptFactor
    }

    /**
     * Sets the superscript position for this composite text line.
     *
     * - Parameter superscriptPosition: the superscript position.
     */
    @discardableResult
    public func setSuperscriptPosition(_ superscriptPosition: Float) -> CompositeTextLine {
        self.superscriptPosition = superscriptPosition
        layoutComponents()
        return self
    }

    /**
     * Gets the superscript position for this text line.
     *
     * - Returns: superscriptPosition the superscript position.
     */
    public func getSuperscriptPosition()-> Float {
        return self.superscriptPosition
    }

    /**
     * Sets the subscript position for this composite text line.
     *
     * - Parameter subscriptPosition: the subscript position.
     */
    @discardableResult
    public func setSubscriptPosition(_ subscriptPosition: Float) -> CompositeTextLine {
        self.subscriptPosition = subscriptPosition
        layoutComponents()
        return self
    }

    /**
     * Gets the subscript position for this text line.
     *
     * - Returns: subscriptPosition the subscript position.
     */
    public func getSubscriptPosition()-> Float {
        return self.subscriptPosition
    }

    /**
     * Add a new text line.
     *
     * Find the current font, current size and effects (normal, super or subscript)
     * Set the position of the component to the starting stored as current position
     * Set the size and offset based on effects
     * Set the new current position
     *
     * - Parameter component: the component.
     */
    @discardableResult
    public func addComponent(_ component: TextLine) -> CompositeTextLine {
        textLines.append(component)
        fontSizes.append(component.getFontSize())
        place(component, baseFontSize(textLines.count - 1))
        return self
    }

    ///
    /// Adds the components of a chemical formula, in the specified font.
    ///
    /// The digits that follow an element or a closing bracket are subscripts,
    /// as the 2 of H2O and the 6, 12 and 6 of C6H12O6; a run of digits and
    /// signs after a circumflex is a superscript, as the charge of Ca^2+ and
    /// SO4^2-; and everything else is drawn on the baseline, including a digit
    /// that begins the formula, as the 2 of 2H2O.
    ///
    /// - Parameter font: the font of the formula.
    /// - Parameter formula: the formula, for example "C6H12O6" or "SO4^2-".
    /// - Returns: this CompositeTextLine object.
    ///
    @discardableResult
    public func addFormula(_ font: Font, _ formula: String) -> CompositeTextLine {
        var buf = String()
        let characters = Array(formula)
        var i = 0
        while i < characters.count {
            let ch = characters[i]
            if ch == "^" {
                addRun(font, &buf, ScriptPosition.NORMAL)
                i += 1
                while i < characters.count && CompositeTextLine.isDigitOrSign(characters[i]) {
                    buf.append(characters[i])
                    i += 1
                }
                addRun(font, &buf, ScriptPosition.SUPERSCRIPT)
            } else if CompositeTextLine.isDigit(ch)
                    && CompositeTextLine.followsAnElement(characters, i) {
                addRun(font, &buf, ScriptPosition.NORMAL)
                while i < characters.count && CompositeTextLine.isDigit(characters[i]) {
                    buf.append(characters[i])
                    i += 1
                }
                addRun(font, &buf, ScriptPosition.SUBSCRIPT)
            } else {
                buf.append(ch)
                i += 1
            }
        }
        addRun(font, &buf, ScriptPosition.NORMAL)
        return self
    }

    // Adds what the buffer holds as one component, and empties the buffer.
    private func addRun(_ font: Font, _ buf: inout String, _ scriptPosition: ScriptPosition) {
        if buf.isEmpty {
            return
        }
        let component = TextLine(font, buf)
        component.setScriptPosition(scriptPosition)
        buf = ""
        addComponent(component)
    }

    private static func isDigit(_ ch: Character) -> Bool {
        return ch >= "0" && ch <= "9"
    }

    private static func isDigitOrSign(_ ch: Character) -> Bool {
        return isDigit(ch) || ch == "+" || ch == "-"
    }

    // A digit is a subscript when it counts the atoms of the element or the
    // group before it, and plain text when it begins the formula.
    private static func followsAnElement(_ characters: [Character], _ index: Int) -> Bool {
        if index == 0 {
            return false
        }
        let ch = characters[index - 1]
        return (ch >= "A" && ch <= "Z") || (ch >= "a" && ch <= "z") || ch == ")" || ch == "]"
    }

    // The base of the script size and offset of the component: the font size
    // of this composite text line, or the one the component came with.
    private func baseFontSize(_ index: Int) -> Float {
        return (fontSize > 0.0) ? fontSize : fontSizes[index]
    }

    // Places the component at the current position, at the size its script
    // position asks for, and moves the current position past it. The size goes
    // on the TextLine: drawOn uses the line's own font size, so resizing the
    // shared Font here would have no effect.
    private func place(_ component: TextLine, _ base: Float) {
        if component.getScriptPosition() == ScriptPosition.SUPERSCRIPT {
            component.setFontSize(base * superscriptFactor)
            component.setLocation(current[X], current[Y] - base * superscriptPosition)
        } else if component.getScriptPosition() == ScriptPosition.SUBSCRIPT {
            component.setFontSize(base * subscriptFactor)
            component.setLocation(current[X], current[Y] + base * subscriptPosition)
        } else {
            component.setFontSize(base)
            component.setLocation(current[X], current[Y])
        }
        current[X] += component.getWidth()
    }

    // Places every component again, from the location of this composite text
    // line, after the location, the font size or a script setting changed.
    private func layoutComponents() {
        current[X] = position[X]
        current[Y] = position[Y]
        for (i, component) in textLines.enumerated() {
            place(component, baseFontSize(i))
        }
    }

    /**
     * Loop through all the text lines and reset their location based on
     * the new location set here.
     *
     * - Parameter x: the x coordinate.
     * - Parameter y: the y coordinate.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.position[X] = x
        self.position[Y] = y
        layoutComponents()
        return self
    }

    /**
     * Return the position of this composite text line.
     *
     * - Returns: the position of this composite text line.
     */
    public func getLocation()-> [Float] {
        return self.position
    }

    /**
     * Return the nth entry in the TextLine array.
     *
     * - Parameter index: the index of the nth element.
     * - Returns: the text line at the specified index.
     */
    public func getTextLine(_ index: Int)-> TextLine? {
        let count = self.textLines.count
        if count == 0 {
            return nil
        }
        if index < 0 || index > count - 1 {
            return nil
        }
        return textLines[index]
    }

    /**
     * Returns the number of text lines.
     *
     * - Returns: the number of text lines.
     */
    public func getNumberOfTextLines()-> Int {
        return textLines.count
    }

    /**
     * Returns the vertical coordinates of the top left and bottom right corners
     * of the bounding box of this composite text line.
     *
     * - Returns: the an array containing the vertical coordinates.
     */
    public func getMinMaxY()-> [Float] {
        var min: Float = position[Y]
        var max: Float = position[Y]

        // Each component is measured where it is drawn, with the font size it
        // is drawn at, which a script position makes smaller than the base.
        for component in textLines {
            let baseline = component.getLocation()[1]
            let top = baseline - component.font!.getAscent(component.getFontSize())
            let bottom = baseline + component.font!.getDescent(component.getFontSize())
            if top < min {
                min = top
            }
            if bottom > max {
                max = bottom
            }
        }

        return [min, max]
    }

    /**
     * Returns the height of this CompositeTextLine.
     *
     * - Returns: the height.
     */
    public func getHeight()-> Float {
        let yy = getMinMaxY()
        return yy[1] - yy[0]
    }

    ///
    /// Returns how far above its baseline this composite text line reaches:
    /// the ascent of the component that reaches highest, a superscript
    /// included.
    ///
    public func getAscent() -> Float {
        return position[Y] - getMinMaxY()[0]
    }

    ///
    /// Returns how far below its baseline this composite text line reaches:
    /// the descent of the component that reaches lowest, a subscript included.
    ///
    public func getDescent() -> Float {
        return getMinMaxY()[1] - position[Y]
    }

    /**
     * Returns the width of this CompositeTextLine.
     *
     * - Returns: the width.
     */
    public func getWidth()-> Float {
        var width: Float = 0.0
        for component in textLines {
            width += component.getWidth()
        }
        return width
    }

    /**
     * Draws this line on the specified page.
     *
     * - Parameter page: the page to draw this line on.
     * - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    public func drawOn(_ page: Page?)-> [Float] {
        // A composite text line with no component reaches its own location.
        var xMax: Float = position[X]
        var yMax: Float = position[Y]
        // Loop through all the text lines and draw them on the page
        for textLine in textLines {
            let xy: [Float] = textLine.drawOn(page)
            xMax = max(xMax, xy[0])
            yMax = max(yMax, xy[1])
        }
        return [xMax, yMax]
    }
}
