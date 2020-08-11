/**
 *  Title.swift
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


///
/// Please see Example_51 and Example_52
///
public class Title : Drawable {

    public var prefix: TextLine?
    public var textLine: TextLine?


    public init(_ font: Font, _ title: String, _ x: Float, _ y: Float) {
        self.prefix = TextLine(font)
        self.prefix!.setLocation(x, y)
        self.textLine = TextLine(font, title)
        self.textLine!.setLocation(x, y)
    }


    public func setPrefix(_ text: String) -> Title {
        prefix!.setText(text)
        return self
    }


    public func setOffset(_ offset: Float) -> Title {
        self.textLine!.setLocation(textLine!.x + offset, textLine!.y)
        return self
    }


    public func setPosition(_ x: Float, _ y: Float) {
        self.prefix!.setLocation(x, y)
        self.textLine!.setLocation(x, y)
    }


    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        if prefix!.getText() != "" {
            self.prefix!.drawOn(page)
        }
        return self.textLine!.drawOn(page)
    }

}
