/**
 * Paragraph.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
///
public class Paragraph {
    public var xText: Float32?
    public var yText: Float32?
    public var x1: Float32?
    public var y1: Float32?
    public var x2: Float32?
    public var y2: Float32?
    var lines: [TextLine]?
    var alignment: UInt32 = Align.LEFT

    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
        lines = [TextLine]()
    }

    public init(_ text: TextLine) {
        lines = [TextLine]()
        lines!.append(text)
    }

    ///
    /// Adds a text line to this paragraph.
    ///
    /// @param text the text line to add to this paragraph.
    /// @return this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        lines!.append(text)
        return self
    }

    ///
    /// Sets the alignment of the text in this paragraph.
    ///
    /// @param alignment the alignment code.
    /// @return this paragraph.
    ///
    /// <pre>Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY.</pre>
    ///
    @discardableResult
    public func setAlignment(_ alignment: UInt32) -> Paragraph {
        self.alignment = alignment
        return self
    }

    public func getTextLines() -> [TextLine] {
        return lines!
    }

    public func startsWith(_ token: String) -> Bool {
        return lines![0].getText()!.hasPrefix(token)
    }

    public func setColor(_ color: Int32) {
        let r = Float(((color >> 16) & 0xff))/255.0
        let g = Float(((color >>  8) & 0xff))/255.0
        let b = Float(((color)       & 0xff))/255.0
        setColor([r, g, b])
    }

    public func setColor(_ color: [Float]) {
        for line in lines! {
            line.setTextColor(color)
        }
    }

    public func setColorMap(_ colorMap: [String: Int32]) {
        for line in lines! {
            line.setColorMap(colorMap)
        }
    }
}   // End of Paragraph.swift
