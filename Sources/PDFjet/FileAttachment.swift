/**
 *  FileAttachment.swift
 *
Copyright 2023 Innovatics Inc.

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
import Foundation

/**
 *  Used to attach file objects.
 */
public class FileAttachment : Drawable {
    var objNumber = -1
    var pdf: PDF?
    var embeddedFile: EmbeddedFile?
    var icon: String = "PushPin"
    var title: String = ""
    var contents: String = "Right mouse click or double click on the icon to save the attached file."
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
