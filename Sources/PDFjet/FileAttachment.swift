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
    var pdf: PDF?
    var embeddedFile: EmbeddedFile?
    var icon: String = "PushPin"
    var title: String = ""
    var contents: String = "Right mouse click on the icon to save the attached file."
    var x: Float = 0.0
    var y: Float = 0.0
    var h: Float = 24.0

    /// Creates an attachment for the embedded file.
    public init(_ pdf: PDF, _ file: EmbeddedFile) {
        self.pdf = pdf
        self.embeddedFile = file
    }

    /// Sets the location of the attachment icon on the page.
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    /// Uses the push pin icon.
    @discardableResult
    public func setIconPushPin() -> FileAttachment {
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

    /// Sets the description of this attachment.
    @discardableResult
    public func setDescription(_ description: String) -> FileAttachment {
        self.contents = description
        return self
    }

    /// Adds this attachment to the specified page.
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
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
