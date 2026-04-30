/**
 * OptionalContentGroup.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Mark Paxton
 * Modified and adapted for use in PDFjet by Evgeni Dragoev
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * Container for drawable objects that can be drawn on a page as part of Optional Content Group.
 * Please see the PDF specification and Example_30 for more details.
 *
 * @author Mark Paxton
 */
public class OptionalContentGroup {
    internal PDF pdf;
    internal int objNumber;
    internal String name;
    internal int ocgNumber = -1;

    private bool visible;
    private bool printable;
    private bool exportable;
    private List<IDrawable> components;

    public OptionalContentGroup(PDF pdf, String name) {
        this.pdf = pdf;
        this.name = name;
        this.components = new List<IDrawable>();
    }

    public String GetName() {
        return this.name;
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

    // Added by request from Planet Associates
    public void Clear() {
        this.components.Clear();
    }

    public List<IDrawable> GetComponents() {
        return components;
    }

    public void DrawOn(Page page) {
        if (this.ocgNumber == -1) {
            pdf.NewObj();
            pdf.Append(Token.BeginDictionary);
            pdf.Append("/Type /OCG\n");

            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            if (pdf.encryption != null) {
                nameBytes = AES256.Encrypt(nameBytes, pdf.encryption.GetKey());
            }
            pdf.Append("/Name <");
            pdf.Append(Util.ToHexString(nameBytes));
            pdf.Append(">\n");

            pdf.Append("/Usage <<\n");
            if (visible) {
                pdf.Append("/View << /ViewState /ON >>\n");
            } else {
                pdf.Append("/View << /ViewState /OFF >>\n");
            }
            if (printable) {
                pdf.Append("/Print << /PrintState /ON >>\n");
            } else {
                pdf.Append("/Print << /PrintState /OFF >>\n");
            }
            if (exportable) {
                pdf.Append("/Export << /ExportState /ON >>\n");
            } else {
                pdf.Append("/Export << /ExportState /OFF >>\n");
            }
            pdf.Append(">>\n");
            pdf.Append(Token.EndDictionary);
            pdf.EndObj();

            objNumber = pdf.GetObjNumber();

            this.pdf.groups.Add(this);
            this.ocgNumber = pdf.groups.Count;
        }

        if (components.Count > 0) {
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
