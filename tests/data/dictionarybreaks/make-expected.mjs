// make-expected.mjs
//
// Writes expected.json: for each run of Thai, Lao, Khmer and Burmese in the
// UDHR of resources/data/udhr, the places ICU breaks it into words, as the
// dictionary break engines of PDFjet's four ports must find them. ICU is
// Node's (Intl.Segmenter), whose word boundaries in such a run come from the
// same dictionary engines as its line breaks.
//
//   node tests/data/dictionarybreaks/make-expected.mjs
import { readFileSync, writeFileSync } from "node:fs";
const scripts = [
  ["tha", "th", /[ก-ฺเ-๎]+/gu],
  ["lao", "lo", /[ກຂຄຆ-ຊຌ-ຣລວ-ຽເ-ໄໆ່-໎ໜ-ໟ]+/gu],
  ["khm", "km", /[ក-៓ៗៜ៝]+/gu],
  ["mya", "my", /[က-ဿၐ-ႏႚ-႟ꧠ-ꧯꧺ-ꧾꩠ-ꩿ]+/gu],
];
const out = { icu: process.versions.icu, unicode: process.versions.unicode, scripts: {} };
for (const [code, lang, run] of scripts) {
  const text = readFileSync(`resources/data/udhr/udhr_${code}.txt`, "utf8");
  const seg = new Intl.Segmenter(lang, { granularity: "word" });
  const runs = [];
  for (const m of text.matchAll(run)) {
    const s = m[0], ends = [];
    let at = 0;
    for (const part of seg.segment(s)) {
      at += [...part.segment].length; // In code points, as the ports count runes
      if (at < [...s].length) ends.push(at);
    }
    runs.push({ text: s, ends });
  }
  out.scripts[code] = runs;
}
writeFileSync("tests/data/dictionarybreaks/expected.json", JSON.stringify(out) + "\n");
console.log(`ICU ${out.icu}: ` + Object.entries(out.scripts).map(([k, v]) => `${k} ${v.length} runs`).join(", "));
