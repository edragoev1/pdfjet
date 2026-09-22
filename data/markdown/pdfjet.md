# PDFjet

![The PDFjet logo](pdfjet-logo.png)

PDFjet is a library for creating **PDF documents** from programs, in *Java*,
*C#*, *Go* and *Swift*, with the same API and the same output in all four. It
has no dependencies, and it writes documents that are **accessible** to screen
readers and fit for long-term archiving when you ask it to. See
[pdfjet.com](https://pdfjet.com) and the
[repository](https://github.com/edragoev1/pdfjet).

## What it draws

- Text in any script, with kerning, fallback fonts and right to left text
- Tables that break across pages and repeat their header rows
- Charts, barcodes, images and SVG
- Forms, links, bookmarks, attachments and layers
  - Links to web pages
  - Links inside the document

## Markdown

This page is a Markdown text, drawn by `Markdown`. It reads:

1. headings of `#` and of underlines
2. paragraphs, with **bold**, *italic*, `code` and links
3. bullet and numbered lists, which nest
4. quotes, code, tables, thematic breaks and images

> Markdown is a text that reads well as it is, and PDFjet draws it as a
> tagged PDF, with no browser and no other library.

```java
Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
markdown.setImageDirectory("data/markdown");
List<Page> pages = new ArrayList<Page>();
markdown.drawOn(pdf, text, pages, Letter.PORTRAIT);
pdf.addPages(pages);
```

---

## The four ports

| Port  | Language     | Package          |
|:------|:-------------|:-----------------|
| Java  | Java 8+      | PDFjet.jar       |
| C#    | .NET 8       | PDFjet.dll       |
| Go    | Go           | pdfjet/v9        |
| Swift | Swift 6.2    | PDFjet           |

Every example is built in the four languages, and the PDFs they write draw
the same text in the same fonts, sizes and positions, page by page.
