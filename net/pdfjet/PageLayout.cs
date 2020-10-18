/**
 *  PageLayout.cs
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
using System;


namespace PDFjet.NET {
/**
 *  Used to specify the PDF page layout.
 *
 */
public class PageLayout {
    public const String SINGLE_PAGE = "SinglePage";          // Display one page at a time
    public const String ONE_COLUMN = "OneColumn";            // Display the pages in one column
    public const String TWO_COLUMN_LEFT = "TwoColumnLeft";   // Odd-numbered pages on the left
    public const String TWO_COLUMN_RIGTH = "TwoColumnRight"; // Odd-numbered pages on the right
    public const String TWO_PAGE_LEFT = "TwoPageLeft";       // Odd-numbered pages on the left
    public const String TWO_PAGE_RIGTH = "TwoPageRight";     // Odd-numbered pages on the right
}
}   // End of namespace PDFjet.NET
