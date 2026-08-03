/**
 * OptionalContentGroup.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Mark Paxton
 * Modified and adapted for use in PDFjet by Evgeni Dragoev
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.util.ArrayList;
import java.util.List;

/**
 * Container for drawable objects that can be drawn on a page as part of Optional Content Group.
 * Please see the PDF specification and Example_30 for more details.
 *
 * @author Mark Paxton
 */
public class OptionalContentGroup {
    protected int objNumber;
    protected String name;

    private final PDF pdf;
    private int ocgNumber = -1;
    private boolean visible;
    private boolean printable;
    private boolean exportable;
    private final List<Drawable> components;

    /**
     * Creates OptionalContentGroup object
     *
     * @param pdf the PDF.
     * @param name the name of the group.
     */
    public OptionalContentGroup(PDF pdf, String name) {
        this.pdf = pdf;
        this.name = name;
        this.components = new ArrayList<Drawable>();
    }

    /**
     * Add drawable object to the group
     *
     * @param drawable the drawable object
     */
    public void add(Drawable drawable) {
        components.add(drawable);
    }

    /**
     * Sets the visibility of this group
     *
     * @param visible flag
     */
    public void setVisible(boolean visible) {
        this.visible = visible;
    }

    /**
     * Sets the printability of this group
     *
     * @param printable flag
     */
    public void setPrintable(boolean printable) {
        this.printable = printable;
    }

    /**
     * Sets the exportability of this group
     *
     * @param exportable flag
     */
    public void setExportable(boolean exportable) {
        this.exportable = exportable;
    }

    /**
     * Draws this content group on a page
     *
     * @param page the page to draw on
     * @throws Exception if there is a problem
     */
    public void drawOn(Page page) throws Exception {
        if (this.ocgNumber == -1) {
            pdf.newobj();
            pdf.append("<<\n");
            pdf.append("/Type /OCG\n");

            byte[] nameBytes = name.getBytes(java.nio.charset.StandardCharsets.UTF_8);
            if (pdf.encryption != null) {
                nameBytes = AES256.encrypt(nameBytes, pdf.encryption.getKey());
            }
            pdf.append("/Name <");
            pdf.append(Util.toHexString(nameBytes));
            pdf.append(">\n");

            pdf.append("/Usage <<\n");
            if (visible) {
                pdf.append("/View << /ViewState /ON >>\n");
            } else {
                pdf.append("/View << /ViewState /OFF >>\n");
            }
            if (printable) {
                pdf.append("/Print << /PrintState /ON >>\n");
            } else {
                pdf.append("/Print << /PrintState /OFF >>\n");
            }
            if (exportable) {
                pdf.append("/Export << /ExportState /ON >>\n");
            } else {
                pdf.append("/Export << /ExportState /OFF >>\n");
            }
            pdf.append(">>\n");
            pdf.append(">>\n");
            pdf.endobj();

            objNumber = pdf.getObjNumber();

            pdf.groups.add(this);
            this.ocgNumber = pdf.groups.size();
        }

        if (!components.isEmpty()) {
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
