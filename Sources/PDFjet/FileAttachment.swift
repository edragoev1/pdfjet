/**
 *  FileAttachment.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
import Foundation


/**
 *  Used to attach file objects.
 *
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
