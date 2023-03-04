/**
 *  Paragraph.swift
 *
Copyright 2023 Innovatics Inc.
*/
import Foundation


///
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
///
public class Paragraph {

    var list: [TextLine]?
    var alignment: UInt32 = Align.LEFT


    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
        list = [TextLine]()
    }


    public init(_ text: TextLine) {
        list = [TextLine]()
        list!.append(text)
    }


    ///
    /// Adds a text line to this paragraph.
    ///
    /// @param text the text line to add to this paragraph.
    /// @return this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        list!.append(text)
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

}   // End of Paragraph.swift
