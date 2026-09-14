/*
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
/// <summary>
/// Container for drawable objects that can be drawn on a page as part of Optional Content Group.
/// Please see the PDF specification and Example_30 for more details.
/// </summary>
/// <remarks>Author: Mark Paxton</remarks>
public class OptionalContentGroup {
    internal PDF pdf;
    internal int objNumber;
    internal String name;
    internal int ocgNumber = -1;

    internal bool visible;
    private bool printable;
    private bool exportable;
    private List<IDrawable> components;

    /// <summary>Creates an optional content group, also called a layer.</summary>
    public OptionalContentGroup(PDF pdf, String name) {
        this.pdf = pdf;
        this.name = name;
        this.components = new List<IDrawable>();
    }

    /// <summary>Returns the name of this group.</summary>
    public String GetName() {
        return this.name;
    }

    /// <summary>Adds a drawable to this group.</summary>
    public OptionalContentGroup Add(IDrawable drawable) {
        components.Add(drawable);
        return this;
    }

    /// <summary>Sets whether this group is visible.</summary>
    public OptionalContentGroup SetVisible(bool visible) {
        this.visible = visible;
        return this;
    }

    /// <summary>Sets whether this group is printed.</summary>
    public OptionalContentGroup SetPrintable(bool printable) {
        this.printable = printable;
        return this;
    }

    /// <summary>Sets whether this group is exported.</summary>
    public OptionalContentGroup SetExportable(bool exportable) {
        this.exportable = exportable;
        return this;
    }

    // Added by request from Planet Associates
    /// <summary>Removes all drawables from this group.</summary>
    public OptionalContentGroup Clear() {
        this.components.Clear();
        return this;
    }

    /// <summary>Returns the drawables in this group.</summary>
    public List<IDrawable> GetComponents() {
        return components;
    }

    /// <summary>Draws this group and its drawables on the specified page.</summary>
    /// <returns>the largest x and y coordinates of the bottom right corners of the drawables in this group.</returns>
    public float[] DrawOn(Page page) {
        float[] xy = new float[] {0f, 0f};
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
                float[] corner = component.DrawOn(page);
                xy[0] = Math.Max(xy[0], corner[0]);
                xy[1] = Math.Max(xy[1], corner[1]);
            }
            page.Append("\nEMC\n");
        }
        return xy;
    }
}   // End of OptionalContentGroup.cs
}   // End of namespace PDFjet.NET
