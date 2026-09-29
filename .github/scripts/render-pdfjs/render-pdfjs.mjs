// Renders PDFs with pdf.js, the engine of Firefox, for check-viewers.py.
//
//     node render-pdfjs.mjs JOB.json
//
// JOB.json is {"dpi": 72, "maxPages": 10, "out": FOLDER, "files": [{"name":
// NAME, "path": PDF, "password": PASSWORD or null}, ...]}. Every page of every
// file is rendered, as Firefox draws it, with its annotations, and its text is
// extracted. The first maxPages pages are written to FOLDER/NAME/page-NNN.png,
// and FOLDER/NAME.json gets {"pages": COUNT, "text": [TEXT OF EACH PAGE],
// "errors": [...]}. An error is an exception, or a warning pdf.js prints: it
// draws what it can of a page it cannot read, and says so only in a warning.
//
// pdf.js renders on @napi-rs/canvas in Node, which it loads itself. The fonts a
// PDF does not embed come from its standard_fonts folder and the CMaps of the
// CJK fonts from its cmaps folder, as in Firefox; but in Node it does not use
// the fonts of the machine, so a CJK font that is not embedded, which Firefox
// draws with a font of the machine, is drawn as empty boxes.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import * as pdfjs from "pdfjs-dist/legacy/build/pdf.mjs";

const dist = path.join(path.dirname(fileURLToPath(import.meta.url)), "node_modules", "pdfjs-dist") + "/";

// pdf.js runs its worker in this thread in Node, so its warnings come here.
let warnings = [];
const warn = console.warn;
console.warn = (...args) => {
    const message = args.join(" ");
    if (message.startsWith("Warning:")) {
        warnings.push(message);
    } else {
        warn(...args);
    }
};

async function render(file, dpi, maxPages, out) {
    const result = { pages: 0, text: [], errors: [] };
    warnings = [];
    let task = null;
    try {
        task = pdfjs.getDocument({
            data: new Uint8Array(fs.readFileSync(file.path)),
            password: file.password ?? undefined,
            cMapUrl: dist + "cmaps/",
            standardFontDataUrl: dist + "standard_fonts/",
            wasmUrl: dist + "wasm/",
            iccUrl: dist + "iccs/",
            verbosity: pdfjs.VerbosityLevel.WARNINGS,
        });
        const doc = await task.promise;
        result.pages = doc.numPages;
        fs.mkdirSync(path.join(out, file.name), { recursive: true });
        for (let i = 1; i <= doc.numPages; i++) {
            const page = await doc.getPage(i);
            try {
                const viewport = page.getViewport({ scale: dpi / 72 });
                const canvas = doc.canvasFactory.create(Math.ceil(viewport.width), Math.ceil(viewport.height));
                // A white page, as a viewer shows it; the canvas starts transparent.
                canvas.context.fillStyle = "white";
                canvas.context.fillRect(0, 0, canvas.canvas.width, canvas.canvas.height);
                await page.render({ canvasContext: canvas.context, viewport }).promise;
                if (i <= maxPages) {
                    const name = `page-${String(i).padStart(3, "0")}.png`;
                    fs.writeFileSync(path.join(out, file.name, name), await canvas.canvas.encode("png"));
                }
                doc.canvasFactory.destroy(canvas);
                const content = await page.getTextContent();
                result.text.push(content.items.map((item) => item.str + (item.hasEOL ? "\n" : "")).join(""));
            } catch (e) {
                result.errors.push(`page ${i}: ${e.message ?? e}`);
                result.text.push("");
            }
            page.cleanup();
        }
    } catch (e) {
        result.errors.push(`cannot open: ${e.message ?? e}`);
    }
    await task?.destroy();
    result.errors.push(...[...new Set(warnings)]);
    fs.writeFileSync(path.join(out, file.name + ".json"), JSON.stringify(result));
}

const job = JSON.parse(fs.readFileSync(process.argv[2], "utf-8"));
fs.mkdirSync(job.out, { recursive: true });
for (const file of job.files) {
    await render(file, job.dpi, job.maxPages, job.out);
}
