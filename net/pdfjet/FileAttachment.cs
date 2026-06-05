/**
 * FileAttachment.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Used to attach file objects.
 */
namespace PDFjet.NET {
public class FileAttachment : IDrawable {
    internal PDF pdf = null;
    internal EmbeddedFile embeddedFile = null;
    internal String icon = "PushPin";
    internal String title = "";
    internal String contents = "Right mouse click on the icon to save the attached file.";
    internal float x = 0f;
    internal float y = 0f;
    internal float h = 24f;

    public FileAttachment(PDF pdf, EmbeddedFile file) {
        this.pdf = pdf;
        this.embeddedFile = file;
    }

    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    public void SetIconPushPin() {
        this.icon = "PushPin";
    }

    public void SetIconPaperclip() {
        this.icon = "Paperclip";
    }

    public void SetIconSize(float height) {
        this.h = height;
    }

    public void SetTitle(String title) {
        this.title = title;
    }

    public void SetDescription(String description) {
        this.contents = description;
    }

    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public float[] DrawOn(Page page) {
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
        page.AddAnnotation(annotation);
        return new float[] {this.x + this.h, this.y + this.h};
    }
}   // End of FileAttachment.cs
}   // End of namespace PDFjet.NET
