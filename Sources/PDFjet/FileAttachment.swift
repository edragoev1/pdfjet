/**
 * FileAttachment.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Used to attach file objects.
 */
public class FileAttachment : Drawable {
    var embeddedFile: EmbeddedFile?
    var icon: String = "PushPin"
    var title: String = ""
    var contents: String = "Right mouse click on the icon to save the attached file."
    var x: Float = 0.0
    var y: Float = 0.0
    var h: Float = 24.0

    /// Creates an attachment for the embedded file.
    public init(_ file: EmbeddedFile) {
        self.embeddedFile = file
    }

    /// Sets the location of the attachment icon on the page.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Uses the pushpin icon.
    @discardableResult
    public func setIconPushpin() -> FileAttachment {
        self.icon = "PushPin"
        return self
    }

    /// Uses the paperclip icon.
    @discardableResult
    public func setIconPaperclip() -> FileAttachment {
        self.icon = "Paperclip"
        return self
    }

    /// Sets the height of the icon.
    @discardableResult
    public func setIconSize(_ height: Float) -> FileAttachment {
        self.h = height
        return self
    }

    /// Sets the title of this attachment.
    @discardableResult
    public func setTitle(_ title: String) -> FileAttachment {
        self.title = title
        return self
    }

    /// Sets the contents of this attachment, the text a viewer shows for it.
    @discardableResult
    public func setContents(_ contents: String) -> FileAttachment {
        self.contents = contents
        return self
    }

    /// Adds this attachment to the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if let file = embeddedFile, file.pdfIdentity != page!.pdf.identity {
            page!.pdf.fail("The embedded file belongs to another PDF.")
            return [self.x + self.h, self.y + self.h]
        }
        let annotation = Annotation(
                Annotation.FileAttachment,
                x,
                y,
                x + h,
                y + h,
                nil,    // Vertices
                nil,    // Fill Color
                0.0,    // Transparency
                nil,    // Title
                nil,    // Contents
                nil,
                nil,
                nil,
                nil,
                nil)
        annotation.fileAttachment = self
        page!.addAnnotation(annotation)
        return [self.x + self.h, self.y + self.h]
    }
}   // End of FileAttachment.swift
