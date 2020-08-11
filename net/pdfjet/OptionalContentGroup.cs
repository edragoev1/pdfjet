/**
 *  OptionalContentGroup.cs
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
