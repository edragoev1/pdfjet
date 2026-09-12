/*
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
    /** The object number of this group. */
    protected int objNumber;
    /** The name of this group. */
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
     * Returns the name of this group.
     *
     * @return the name of the group.
     */
    public String getName() {
        return this.name;
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
     * Removes all drawable objects from the group.
     */
    public void clear() {
        components.clear();
    }

    /**
     * Returns the drawable objects in the group.
     *
     * @return the list of drawable objects.
     */
    public List<Drawable> getComponents() {
        return components;
    }

    /**
     * Sets the visibility of this group
     *
     * @param visible flag
     * @return this OptionalContentGroup object.
     */
    public OptionalContentGroup setVisible(boolean visible) {
        this.visible = visible;
        return this;
    }

    /**
     * Sets the printability of this group
     *
     * @param printable flag
     * @return this OptionalContentGroup object.
     */
    public OptionalContentGroup setPrintable(boolean printable) {
        this.printable = printable;
        return this;
    }

    /**
     * Sets the exportability of this group
     *
     * @param exportable flag
     * @return this OptionalContentGroup object.
     */
    public OptionalContentGroup setExportable(boolean exportable) {
        this.exportable = exportable;
        return this;
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
