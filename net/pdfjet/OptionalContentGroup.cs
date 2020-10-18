/**
 *  OptionalContentGroup.cs
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
using System.Collections.Generic;


namespace PDFjet.NET {
/**
 * Container for drawable objects that can be drawn on a page as part of Optional Content Group.
 * Please see the PDF specification and Example_30 for more details.
 *
 * @author Mark Paxton
 */
public class OptionalContentGroup {

    internal String name;
    internal int ocgNumber;
    internal int objNumber;
    internal bool visible;
    internal bool printable;
    internal bool exportable;
    private List<IDrawable> components;

    public OptionalContentGroup(String name) {
        this.name = name;
        this.components = new List<IDrawable>();
    }

    public void Add(IDrawable drawable) {
        components.Add(drawable);
    }

    public void SetVisible(bool visible) {
        this.visible = visible;
    }

    public void SetPrintable(bool printable) {
        this.printable = printable;
    }

    public void SetExportable(bool exportable) {
        this.exportable = exportable;
    }

    public void DrawOn(Page page) {
        if (components.Count > 0) {
            page.pdf.groups.Add(this);
            ocgNumber = page.pdf.groups.Count;

            page.pdf.Newobj();
            page.pdf.Append("<<\n");
            page.pdf.Append("/Type /OCG\n");
            page.pdf.Append("/Name (" + name + ")\n");
            page.pdf.Append("/Usage <<\n");
            if (visible) {
                page.pdf.Append("/View << /ViewState /ON >>\n");
            }
            else {
                page.pdf.Append("/View << /ViewState /OFF >>\n");
            }
            if (printable) {
                page.pdf.Append("/Print << /PrintState /ON >>\n");
            }
            else {
                page.pdf.Append("/Print << /PrintState /OFF >>\n");
            }
            if (exportable) {
                page.pdf.Append("/Export << /ExportState /ON >>\n");
            }
            else {
                page.pdf.Append("/Export << /ExportState /OFF >>\n");
            }
            page.pdf.Append(">>\n");
            page.pdf.Append(">>\n");
            page.pdf.Endobj();

            objNumber = page.pdf.GetObjNumber();

            page.Append("/OC /OC");
            page.Append(ocgNumber);
            page.Append(" BDC\n");
            foreach (IDrawable component in components) {
                component.DrawOn(page);
            }
            page.Append("\nEMC\n");
        }
    }

}   // End of OptionalContentGroup.cs
}   // End of namespace PDFjet.NET
