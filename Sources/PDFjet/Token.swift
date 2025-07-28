/**
 *  Token.swift
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

///
/// Please see PDF.swift
///
public class Token {
    public static let beginDictionary = Array("<<\n".utf8);
    public static let endDictionary = Array(">>\n".utf8);
    public static let stream = Array("stream\n".utf8)
    public static let endstream = Array("\nendstream\n".utf8)
    public static let newobj = Array(" 0 obj\n".utf8)
    public static let endobj = Array("endobj\n".utf8)
    public static let objRef = " 0 R\n"
    public static let beginText = Array("BT\n".utf8)
    public static let endText = Array("ET\n".utf8)
    public static let count = Array("/Count ".utf8);
    public static let length = Array("/Length ".utf8);
    // public static let space: UInt8 = 32      // SPACE
    public static let space = " "               // SPACE
    public static let newline: UInt8 = 10   // LF
    public static let beginStructElem = Array("<<\n/Type /StructElem /S /".utf8)
    public static let endStructElem = Array(">\n>>\n".utf8)
    public static let beginAnnotation = Array("/K <</Type /OBJR /Obj ".utf8)
    public static let endAnnotation = Array(" 0 R>>".utf8)
    public static let actualText = Array(">\n/ActualText <".utf8)
    public static let altDescription = Array(")\n/Alt <".utf8)

    public static let P = Array("\n/P ".utf8)
    public static let objRefPg = Array(" 0 R /Pg ".utf8)
    public static let K = Array("/K ".utf8)
    public static let lang = Array("\n/Lang (".utf8)

    public static let BDC = Array("BDC\n".utf8)
    public static let BMC = Array("BMC\n".utf8)
    public static let EMC = Array("EMC\n".utf8)
    public static let ArtifactBMC = Array("/Artifact BMC\n".utf8)

    public static let beginHexString = Array("[<".utf8)
    public static let endHexString = Array(">] TJ\n".utf8)
}
