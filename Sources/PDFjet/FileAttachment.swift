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

    public init(_ pdf: PDF, _ file: EmbeddedFile) {
        self.pdf = pdf
        self.embeddedFile = file
    }

    public func setLocation(_ x: Float, _ y: Float) {
        self.x = x
        self.y = y
    }

    public func setPosition(_ x: Float, _ y: Float) {
        setLocation(x, y)
    }

    public func setIconPushPin() {
        self.icon = "PushPin"
    }

    public func setIconPaperclip() {
        self.icon = "Paperclip"
    }

    public func setIconSize(_ height: Float) {
        self.h = height
    }

    public func setTitle(_ title: String) {
        self.title = title
    }

    public func setDescription(_ description: String) {
        self.contents = description
    }

    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        let annotation = Annotation(
                nil,
                nil,
                x,
                y,
                x + h,
                y + h,
                nil,
                nil,
                nil)
        annotation.fileAttachment = self
        page!.addAnnotation(annotation)
        return [self.x + self.h, self.y + self.h]
    }
}   // End of FileAttachment.swift
