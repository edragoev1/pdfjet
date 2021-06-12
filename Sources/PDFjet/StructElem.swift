/**
 *  StructElem.swift
 *
Copyright 2020 Innovatics Inc.

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


/**
 *  Defines the StructElem types.
 */
public class StructElem {
    public static let DOCUMENT = "Document"
    public static let DOCUMENTFRAGMENT = "DocumentFragment"
    public static let PART = "Part"
    public static let DIV = "Div"
    public static let ASIDE = "Aside"
    public static let P = "P"
    public static let H1 = "H1"
    public static let H2 = "H2"
    public static let H3 = "H3"
    public static let H4 = "H4"
    public static let H5 = "H5"
    public static let H6 = "H6"
    public static let H = "H"
    public static let TITLE = "Title"
    public static let FENOTE = "FENote"
    public static let SUB = "Sub"
    public static let LBL = "Lbl"
    public static let SPAN = "Span"
    public static let EM = "Em"
    public static let STRONG = "Strong"
    public static let LINK = "Link"
    public static let ANNOT = "Annot"
    public static let FORM = "Form"
    public static let RUBY = "Ruby"
    public static let RB = "RB"
    public static let RT = "RT"
    public static let RP = "RP"
    public static let WARUCHI = "Waruchi"
    public static let WT = "WT"
    public static let WP = "WP"
    public static let L = "L"
    public static let LI = "LI"
    public static let LBODY = "LBody"
    public static let TABLE = "Table"
    public static let TR = "TR"
    public static let TH = "TH"
    public static let TD = "TD"
    public static let THEAD = "THead"
    public static let TBODY = "TBody"
    public static let TFOOT = "TFoot"
    public static let CAPTION = "Caption"
    public static let FIGURE = "Figure"
    public static let FORMULA = "Formula"
    public static let ARTIFACT = "Artifact"

    var objNumber: Int?
    var structure: String?
    var pageObjNumber: Int?
    var mcid = 0
    var language: String?
    var altDescription: String?
    var actualText: String?
    var annotation: Annotation?
}
