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
    protected PDF pdf;
    protected EmbeddedFile embeddedFile;
    protected String icon = "PushPin";
    protected String title = "";
    protected String contents = "Right mouse click on the icon to save the attached file.";
    protected float x = 0f;
    protected float y = 0f;
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
     * Sets the position of the file attachment on the page
     *
     * @param x the horizontal position of the attachment
     * @param y the vertical position of the attachment
     */
    public void setPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Sets the position of the file attachment on the page
     *
     * @param x the horizontal position of the attachment
     * @param y the vertical position of the attachment
     */
    public void setPosition(double x, double y) {
        setPosition((float) x, (float) y);
    }

    /**
     * Sets the location of the file attachment on the page
     *
     * @param x the horizontal location of the attachment
     * @param y the vertical location of the attachment
     * @return this drawable object.
     */
    public Drawable setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location of the file attachment on the page
     *
     * @param x the horizontal location of the attachment
     * @param y the vertical location of the attachment
     * @return this drawable object.
     */
    public Drawable setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the icon for the attachment to be "PushPin"
     */
    public void setIconPushPin() {
        this.icon = "PushPin";
    }

    /**
     * Sets the icon for the attachment to be "Paperclip"
     */
    public void setIconPaperclip() {
        this.icon = "Paperclip";
    }

    /**
     * Sets the icon size
     *
     * @param height the vertical icon size
     */
    public void setIconSize(float height) {
        this.h = height;
    }

    /**
     * Sets the title for this attachment
     *
     * @param title the attachment title
     */
    public void setTitle(String title) {
        this.title = title;
    }

    /**
     * Sets the attachment description
     *
     * @param description the description for the attachment
     */
    public void setDescription(String description) {
        this.contents = description;
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
