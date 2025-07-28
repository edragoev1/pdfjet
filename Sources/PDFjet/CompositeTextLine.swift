/**
 *  CompositeTextLine.swift
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

/*
 *  This class was designed and implemented by Jon T. Swanson, Ph.D.
 *
 *  Refactored and integrated into the project by Eugene Dragoev - 2nd June 2012.
 */
import Foundation

/**
 *  Used to create composite text line objects.
 *
 */
public class CompositeTextLine : Drawable {
    private let X = 0
    private let Y = 1

    private var textLines = [TextLine]()

    private var position = [Float](repeating: 0, count: 2)
    private var current  = [Float](repeating: 0, count: 2)

    // Subscript and Superscript size factors
    private var subscriptSizeFactor: Float = 0.583
    private var superscriptSizeFactor: Float  = 0.583

    // Subscript and Superscript positions in relation to the base font
    private var superscriptPosition: Float = 0.350
    private var subscriptPosition: Float = 0.141

    private var fontSize: Float = 0

    public init(_ x: Float, _ y: Float) {
        self.position[X] = x
        self.position[Y] = y
        self.current[X]  = x
        self.current[Y]  = y
    }

    /**
     *  Sets the font size.
     *
     *  @param fontSize the font size.
     */
    @discardableResult
    public func setFontSize(_ fontSize: Float) -> CompositeTextLine {
        self.fontSize = fontSize
        return self
    }

    /**
     *  Gets the font size.
     *
     *  @return fontSize the font size.
     */
    public func getFontSize()-> Float {
        return self.fontSize
    }

    /**
     *  Sets the superscript factor for this composite text line.
     *
     *  @param superscript the superscript size factor.
     */
    @discardableResult
    public func setSuperscriptFactor(_ superscriptSizeFactor: Float) -> CompositeTextLine {
        self.superscriptSizeFactor = superscriptSizeFactor
        return self
    }

    /**
     *  Gets the superscript factor for this text line.
     *
     *  @return superscript the superscript size factor.
     */
    public func getSuperscriptFactor()-> Float {
        return self.superscriptSizeFactor
    }

    /**
     *  Sets the subscript factor for this composite text line.
     *
     *  @param subscript the subscript size factor.
     */
    @discardableResult
    public func setSubscriptFactor(_ subscriptSizeFactor: Float) -> CompositeTextLine {
        self.subscriptSizeFactor = subscriptSizeFactor
        return self
    }

    /**
     *  Gets the subscript factor for this text line.
     *
     *  @return subscript the subscript size factor.
     */
    public func getSubscriptFactor()-> Float {
        return self.subscriptSizeFactor
    }

    /**
     *  Sets the superscript position for this composite text line.
     *
     *  @param superscriptPosition the superscript position.
     */
    @discardableResult
    public func setSuperscriptPosition(_ superscriptPosition: Float) -> CompositeTextLine {
        self.superscriptPosition = superscriptPosition
        return self
    }

    /**
     *  Gets the superscript position for this text line.
     *
     *  @return superscriptPosition the superscript position.
     */
    public func getSuperscriptPosition()-> Float {
        return self.superscriptPosition
    }

    /**
     *  Sets the subscript position for this composite text line.
     *
     *  @param subscriptPosition the subscript position.
     */
    @discardableResult
    public func setSubscriptPosition(_ subscriptPosition: Float) -> CompositeTextLine {
        self.subscriptPosition = subscriptPosition
        return self
    }

    /**
     *  Gets the subscript position for this text line.
     *
     *  @return subscriptPosition the subscript position.
     */
    public func getSubscriptPosition()-> Float {
        return self.subscriptPosition
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
     */
    public func addComponent(_ component: TextLine) {
        if component.getTextEffect() == Effect.SUPERSCRIPT {
            if fontSize > 0 {
                component.getFont().setSize(fontSize * superscriptSizeFactor)
            }
            component.setLocation(
                    current[X],
                    current[Y] - fontSize * superscriptPosition)
        } else if component.getTextEffect() == Effect.SUBSCRIPT {
            if fontSize > 0 {
                component.getFont().setSize(fontSize * subscriptSizeFactor)
            }
            component.setLocation(
                    current[X],
                    current[Y] + fontSize * subscriptPosition)
        } else {
            if fontSize > 0 {
                component.getFont().setSize(fontSize)
            }
            component.setLocation(current[X], current[Y])
        }
        current[X] += component.getWidth()
        textLines.append(component)
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    /**
     *  Loop through all the text lines and reset their location based on
     *  the new location set here.
     *
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> CompositeTextLine {
        self.position[X] = x
        self.position[Y] = y
        self.current[X]  = x
        self.current[Y]  = y

        if textLines.count == 0 {
            return self
        }
        for component in textLines {
            if component.getTextEffect() == Effect.SUPERSCRIPT {
                component.setLocation(
                        current[X],
                        current[Y] - fontSize * superscriptPosition)
            } else if component.getTextEffect() == Effect.SUBSCRIPT {
                component.setLocation(
                        current[X],
                        current[Y] + fontSize * subscriptPosition)
            } else {
                component.setLocation(current[X], current[Y])
            }
            current[X] += component.getWidth()
        }
        return self
    }

    /**
     *  Return the location of this composite text line.
     *
     *  @return the location of this composite text line.
     */
    public func getLocation()-> [Float] {
        return self.position
    }

    /**
     *  Return the nth entry in the TextLine array.
     *
     *  @param index the index of the nth element.
     *  @return the text line at the specified index.
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
     *  Returns the number of text lines.
     *
     *  @return the number of text lines.
     */
    public func getNumberOfTextLines()-> Int {
       return textLines.count
    }

    /**
     *  Returns the vertical coordinates of the top left and bottom right corners
     *  of the bounding box of this composite text line.
     *
     *  @return the an array containing the vertical coordinates.
     */
    public func getMinMax()-> [Float] {
        var min: Float = position[Y]
        var max: Float = position[Y]
        var cur: Float

        for component in textLines {
            if component.getTextEffect() == Effect.SUPERSCRIPT {
                cur = (position[Y] - component.font!.ascent) - fontSize * superscriptPosition
                if cur < min {
                    min = cur
                }
            } else if component.getTextEffect() == Effect.SUBSCRIPT {
                cur = (position[Y] + component.font!.descent) + fontSize * subscriptPosition
                if cur > max {
                    max = cur
                }
            } else {
                cur = position[Y] - component.font!.ascent
                if cur < min {
                    min = cur
                }
                cur = position[Y] + component.font!.descent
                if cur > max {
                    max = cur
                }
            }
        }

        return [min, max]
    }

    /**
     *  Returns the height of this CompositeTextLine.
     *
     *  @return the height.
     */
    public func getHeight()-> Float {
        let yy = getMinMax()
        return yy[1] - yy[0]
    }

    /**
     *  Returns the width of this CompositeTextLine.
     *
     *  @return the width.
     */
    public func getWidth()-> Float {
        return (current[X] - position[X])
    }

    /**
     *  Draws this line on the specified page.
     *
     *  @param page the page to draw this line on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    @discardableResult
    public func drawOn(_ page: Page?)-> [Float] {
        var xMax: Float = 0.0
        var yMax: Float = 0.0
        // Loop through all the text lines and draw them on the page
        for textLine in textLines {
            let xy: [Float] = textLine.drawOn(page)
            xMax = max(xMax, xy[0])
            yMax = max(yMax, xy[1])
        }
        return [xMax, yMax]
    }
}
