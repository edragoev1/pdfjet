/**
 *  StructElem.cs
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
using System;


namespace PDFjet.NET {
/**
 *  Defines the StructElem types.
 */
public class StructElem {
    public const String DOCUMENT = "Document";
    public const String DOCUMENTFRAGMENT = "DocumentFragment";
    public const String PART = "Part";
    public const String DIV = "Div";
    public const String ASIDE = "Aside";
    public const String P = "P";
    public const String H1 = "H1";
    public const String H2 = "H2";
    public const String H3 = "H3";
    public const String H4 = "H4";
    public const String H5 = "H5";
    public const String H6 = "H6";
    public const String H = "H";
    public const String TITLE = "Title";
    public const String FENOTE = "FENote";
    public const String SUB = "Sub";
    public const String LBL = "Lbl";
    public const String SPAN = "Span";
    public const String EM = "Em";
    public const String STRONG = "Strong";
    public const String LINK = "Link";
    public const String ANNOT = "Annot";
    public const String FORM = "Form";
    public const String RUBY = "Ruby";
    public const String RB = "RB";
    public const String RT = "RT";
    public const String RP = "RP";
    public const String WARUCHI = "Waruchi";
    public const String WT = "WT";
    public const String WP = "WP";
    public const String L = "L";
    public const String LI = "LI";
    public const String LBODY = "LBody";
    public const String TABLE = "Table";
    public const String TR = "TR";
    public const String TH = "TH";
    public const String TD = "TD";
    public const String THEAD = "THead";
    public const String TBODY = "TBody";
    public const String TFOOT = "TFoot";
    public const String CAPTION = "Caption";
    public const String FIGURE = "Figure";
    public const String FORMULA = "Formula";
    public const String ARTIFACT = "Artifact";

    internal int objNumber;
    internal String structure = null;
    internal int pageObjNumber;
    internal int mcid = 0;
    internal String language = null;
    internal String actualText = null;
    internal String altDescription = null;
    internal Annotation annotation = null;
}
}   // End of namespace PDFjet.NET
