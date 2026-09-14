/*
 * FileAttachment.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to attach file objects.
/// </summary>
public class FileAttachment : IDrawable {
    internal EmbeddedFile embeddedFile = null;
    internal String icon = "PushPin";
    internal String title = "";
    internal String contents = "Right mouse click on the icon to save the attached file.";
    internal float x = 0f;
    internal float y = 0f;
    internal float h = 24f;

    /// <summary>Creates an attachment for the embedded file.</summary>
    public FileAttachment(EmbeddedFile file) {
        this.embeddedFile = file;
    }

    /// <summary>Sets the location of the attachment icon on the page.</summary>
    public FileAttachment SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Uses the pushpin icon.</summary>
    public FileAttachment SetIconPushpin() {
        this.icon = "PushPin";
        return this;
    }

    /// <summary>Uses the paperclip icon.</summary>
    public FileAttachment SetIconPaperclip() {
        this.icon = "Paperclip";
        return this;
    }

    /// <summary>Sets the height of the icon.</summary>
    public FileAttachment SetIconSize(float height) {
        this.h = height;
        return this;
    }

    /// <summary>Sets the title of this attachment.</summary>
    public FileAttachment SetTitle(String title) {
        this.title = title;
        return this;
    }

    /// <summary>Sets the contents of this attachment, the text a viewer shows for it.</summary>
    public FileAttachment SetContents(String contents) {
        this.contents = contents;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Adds this attachment to the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (embeddedFile.pdf != page.pdf) {
            page.pdf.Fail(new ArgumentException("The embedded file belongs to another PDF."));
        }
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
