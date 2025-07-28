/**
 *  GraphicsState.swift
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

public class GraphicsState {
    // Default values
    private var CA: Float = 1.0
    private var ca: Float = 1.0

    public init() {

    }

    public func setAlphaStroking(_ CA: Float) {
        if CA >= 0.0 && CA <= 1.0 {
            self.CA = CA
        }
    }

    public func getAlphaStroking() -> Float {
        return self.CA
    }

    public func setAlphaNonStroking(_ ca: Float) {
        if ca >= 0.0 && ca <= 1.0 {
            self.ca = ca
        }
    }

    public func getAlphaNonStroking() -> Float {
        return self.ca
    }
}
