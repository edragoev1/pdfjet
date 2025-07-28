package token

/**
 * token.go
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

var BeginDictionary = []byte("<<\n")
var EndDictionary = []byte(">>\n")
var Stream = []byte("stream\n")
var Endstream = []byte("\nendstream\n")
var Newobj = []byte(" 0 obj\n")
var Endobj = []byte("endobj\n")
var ObjRef = []byte(" 0 R\n")
var beginText = []byte("BT\n")
var endText = []byte("ET\n")
var Count = []byte("/Count ")
var Length = []byte("/Length ")
var Space = []byte(" ")
var Newline = []byte("\n")
