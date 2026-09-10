/*
 * FileAttachment.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Used to attach file objects.
 */
public class FileAttachment implements Drawable {
    /** The PDF this attachment belongs to. */
    protected PDF pdf;
    /** The attached file. */
    protected EmbeddedFile embeddedFile;
    /** The name of the icon: "PushPin" or "Paperclip". */
    protected String icon = "PushPin";
    /** The title of the attachment. */
    protected String title = "";
    /** The description shown for the attachment. */
    protected String contents = "Right mouse click on the icon to save the attached file.";
    /** The x coordinate of the icon. */
    protected float x = 0f;
    /** The y coordinate of the icon. */
    protected float y = 0f;
    /** The height of the icon. */
    protected float h = 24f;

    /**
     * Create file attachment object
     *
     * @param pdf the PDF that the object is attached to
     * @param file the embedded file object
     */
    public FileAttachment(PDF pdf, EmbeddedFile file) {
        this.pdf = pdf;
        this.embeddedFile = file;
    }

    /**
     * Sets the location of the file attachment on the page
     *
     * @param x the horizontal location of the attachment
     * @param y the vertical location of the attachment
     * @return this FileAttachment object.
     */
    public FileAttachment setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of the file attachment on the page
     *
     * @param x the horizontal location of the attachment
     * @param y the vertical location of the attachment
     * @return this FileAttachment object.
     */
    public FileAttachment setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the icon for the attachment to be "PushPin"
     *
     * @return this FileAttachment object.
     */
    public FileAttachment setIconPushPin() {
        this.icon = "PushPin";
        return this;
    }

    /**
     * Sets the icon for the attachment to be "Paperclip"
     *
     * @return this FileAttachment object.
     */
    public FileAttachment setIconPaperclip() {
        this.icon = "Paperclip";
        return this;
    }

    /**
     * Sets the icon size
     *
     * @param height the vertical icon size
     * @return this FileAttachment object.
     */
    public FileAttachment setIconSize(float height) {
        this.h = height;
        return this;
    }

    /**
     * Sets the title for this attachment
     *
     * @param title the attachment title
     * @return this FileAttachment object.
     */
    public FileAttachment setTitle(String title) {
        this.title = title;
        return this;
    }

    /**
     * Sets the attachment description
     *
     * @param description the description for the attachment
     * @return this FileAttachment object.
     */
    public FileAttachment setDescription(String description) {
        this.contents = description;
        return this;
    }

    /**
     * Draw the attachment on the page.
     *
     * @param page the page to draw on
     */
    public float[] drawOn(Page page) throws Exception {
        Annotation annotation = new Annotation(
                Annotation.FileAttachment,
                x,
                y,
                x + h,
                y + h,
                null,   // Vertices
                null,   // Fill Color
                0f,     // Transparency
                null,   // Title
                null,   // Contents
                null,
                null,
                null,
                null,
                null);
        annotation.fileAttachment = this;
        page.addAnnotation(annotation);
        return new float[] {this.x + this.h, this.y + this.h};
    }
}   // End of FileAttachment.java
