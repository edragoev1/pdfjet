// make-car-png.mjs
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Writes resources/images/car.png, the picture of Example_20, from
// util/car.svg: a navy hatchback drawn for PDFjet, a car of no make or model,
// 305 by 138 pixels, an 8-bit RGBA PNG on a transparent background. The SVG
// is drawn by headless Chrome. Run it from the project root; it needs Node 22
// or later and google-chrome, and Pillow for the last step, which writes the
// PNG again without Chrome's metadata.
//
//     node util/make-car-png.mjs

import { spawn, execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

const INPUT = "util/car.svg";
const OUTPUT = "resources/images/car.png";
const PORT = 9361;

const svg = fs.readFileSync(INPUT, "utf8");
const [, width, height] = svg.match(/width="(\d+)" height="(\d+)"/).map(Number);
const profile = fs.mkdtempSync(path.join(os.tmpdir(), "make-car-"));
const chrome = spawn("google-chrome", ["--headless=new", "--disable-gpu",
    `--remote-debugging-port=${PORT}`, `--user-data-dir=${profile}`, "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
let targets;
for (let i = 0; i < 50 && !targets; i++) {
    try {
        targets = await (await fetch(`http://127.0.0.1:${PORT}/json`)).json();
    } catch {
        await sleep(200);
    }
}
const ws = new WebSocket(targets.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((resolve) => ws.addEventListener("open", resolve));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (m) => {
    const msg = JSON.parse(m.data);
    if (msg.id && pending.has(msg.id)) {
        pending.get(msg.id)(msg);
        pending.delete(msg.id);
    }
});
const send = (method, params = {}) => new Promise((resolve) => {
    const n = ++id;
    pending.set(n, resolve);
    ws.send(JSON.stringify({ id: n, method, params }));
});

await send("Emulation.setDefaultBackgroundColorOverride", { color: { r: 0, g: 0, b: 0, a: 0 } });
await send("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile: false });
await send("Page.navigate", { url: "data:text/html," +
    encodeURIComponent(`<html><body style="margin:0;background:transparent">${svg}</body></html>`) });
await sleep(500);
const shot = await send("Page.captureScreenshot", { format: "png", clip: { x: 0, y: 0, width, height, scale: 1 } });
fs.writeFileSync(OUTPUT, Buffer.from(shot.result.data, "base64"));
ws.close();
chrome.kill();

execFileSync("python3", ["-I", "-c",
    `from PIL import Image; Image.open("${OUTPUT}").convert("RGBA").save("${OUTPUT}", optimize=True)`]);
console.log(`${OUTPUT}: ${width} by ${height}`);
process.exit(0);
