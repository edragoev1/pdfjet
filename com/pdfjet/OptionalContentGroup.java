/**
 *  OptionalContentGroup.java
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
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * Container for drawable objects that can be drawn on a page as part of Optional Content Group.
 * Please see the PDF specification and Example_30 for more details.
 *
 * @author Mark Paxton
 */
public class OptionalContentGroup {

    protected String name;
    protected int ocgNumber;
    protected int objNumber;
    protected boolean visible;
    protected boolean printable;
    protected boolean exportable;
    private List<Drawable> components;

    public OptionalContentGroup(String name) {
        this.name = name;
        this.components = new ArrayList<Drawable>();
    }

    public void add(Drawable drawable) {
        components.add(drawable);
    }

    public void setVisible(boolean visible) {
        this.visible = visible;
    }

    public void setPrintable(boolean printable) {
        this.printable = printable;
    }

    public void setExportable(boolean exportable) {
        this.exportable = exportable;
    }

    public void drawOn(Page page) throws Exception {
        if (!components.isEmpty()) {
            page.pdf.groups.add(this);
            ocgNumber = page.pdf.groups.size();

            page.pdf.newobj();
            page.pdf.append("<<\n");
            page.pdf.append("/Type /OCG\n");
            page.pdf.append("/Name (" + name + ")\n");
            page.pdf.append("/Usage <<\n");
            if (visible) {
                page.pdf.append("/View << /ViewState /ON >>\n");
            }
            else {
                page.pdf.append("/View << /ViewState /OFF >>\n");
            }
            if (printable) {
                page.pdf.append("/Print << /PrintState /ON >>\n");
            }
            else {
                page.pdf.append("/Print << /PrintState /OFF >>\n");
            }
            if (exportable) {
                page.pdf.append("/Export << /ExportState /ON >>\n");
            }
            else {
                page.pdf.append("/Export << /ExportState /OFF >>\n");
            }
            page.pdf.append(">>\n");
            page.pdf.append(">>\n");
            page.pdf.endobj();

            objNumber = page.pdf.getObjNumber();

            page.append("/OC /OC");
            page.append(ocgNumber);
            page.append(" BDC\n");
            for (Drawable component : components) {
                component.drawOn(page);
            }
            page.append("\nEMC\n");
        }
    }

}   // End of OptionalContentGroup.java
