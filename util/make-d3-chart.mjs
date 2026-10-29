// Makes images/svg/d3-life-expectancy-by-region.svg, the chart of Example_33,
// with D3: a chart as D3 draws it in a browser, which SVGImage draws in a PDF.
// The data is the World Bank's life expectancy at birth of 2023, the regions
// and the world as its API gave them on 29 October 2026 (World Development
// Indicators, SP.DYN.LE00.IN, licensed under CC BY 4.0).
//
// D3 and jsdom, which gives D3 a document outside a browser, are not
// dependencies of PDFjet; to run it, in a folder of its own:
//
//     npm install d3@7.9.0 jsdom@25.0.1
//     NODE_PATH=$PWD/node_modules node /path/to/pdfjet/util/make-d3-chart.mjs
//
// It writes the file into the images/svg folder next to this one.

import { createRequire } from "module";
import { fileURLToPath } from "url";
import path from "path";
import fs from "fs";

const require = createRequire(path.join(process.env.NODE_PATH || ".", "x.js"));
const d3 = await import(require.resolve("d3"));
const { JSDOM } = require("jsdom");

const data = [
    ["East Asia & Pacific", 76.7],
    ["Europe & Central Asia", 78.4],
    ["Latin America & Caribbean", 75.6],
    ["Middle East, North Africa, Afghanistan & Pakistan", 72.1],
    ["North America", 78.7],
    ["South Asia", 72.3],
    ["Sub-Saharan Africa", 62.6],
].sort((a, b) => b[1] - a[1]);
const world = 73.3;

const width = 640;
const height = 320;
const margin = {top: 60, right: 30, bottom: 30, left: 290};

const dom = new JSDOM("<!doctype html><body></body>");
const svg = d3.select(dom.window.document.body).append("svg")
    .attr("xmlns", "http://www.w3.org/2000/svg")
    .attr("width", width)
    .attr("height", height)
    .attr("viewBox", [0, 0, width, height])
    .attr("style", "font: 12px sans-serif;");

const x = d3.scaleLinear([0, 85], [margin.left, width - margin.right]);
const y = d3.scaleBand(data.map(d => d[0]), [margin.top, height - margin.bottom]).padding(0.15);

svg.append("text")
    .attr("x", 0)
    .attr("y", 18)
    .attr("font-weight", "bold")
    .attr("font-size", 15)
    .text("Life expectancy at birth by region, 2023");

// The axis at the top, its grid lines across the bars
svg.append("g")
    .attr("transform", `translate(0,${margin.top})`)
    .call(d3.axisTop(x).ticks(width / 80).tickSize(-(height - margin.top - margin.bottom)))
    .call(g => g.select(".domain").remove())
    .call(g => g.selectAll(".tick line").attr("stroke-opacity", 0.15))
    .call(g => g.append("text")
        .attr("x", width - margin.right)
        .attr("y", -26)
        .attr("fill", "currentColor")
        .attr("text-anchor", "end")
        .text("Years →"));

// The world's life expectancy, a dashed line behind the bars
svg.append("line")
    .attr("x1", x(world))
    .attr("x2", x(world))
    .attr("y1", margin.top)
    .attr("y2", height - margin.bottom + 4)
    .attr("stroke", d3.schemeTableau10[2])
    .attr("stroke-width", 1.5)
    .attr("stroke-dasharray", "5 3");
svg.append("g")
    .attr("fill", d3.schemeTableau10[0])
    .selectAll("rect")
    .data(data)
    .join("rect")
    .attr("x", x(0))
    .attr("y", d => y(d[0]))
    .attr("width", d => x(d[1]) - x(0))
    .attr("height", y.bandwidth());

// The values, inside the ends of the bars
svg.append("g")
    .attr("fill", "white")
    .attr("text-anchor", "end")
    .selectAll("text")
    .data(data)
    .join("text")
    .attr("x", d => x(d[1]) - 6)
    .attr("y", d => y(d[0]) + y.bandwidth() / 2)
    .attr("dy", "0.35em")
    .text(d => d[1].toFixed(1));

svg.append("g")
    .attr("transform", `translate(${margin.left},0)`)
    .call(d3.axisLeft(y).tickSizeOuter(0));

// The world's value, its label under the bars, in a darker red, which
// contrasts with the white page 4.5:1 or more, as WCAG asks of text
svg.append("text")
    .attr("x", x(world))
    .attr("y", height - margin.bottom + 18)
    .attr("fill", d3.color(d3.schemeTableau10[2]).darker(0.6))
    .attr("text-anchor", "middle")
    .attr("font-weight", "bold")
    .text(`World ${world.toFixed(1)}`);

const out = path.join(path.dirname(fileURLToPath(import.meta.url)), "..", "images", "svg",
    "d3-life-expectancy-by-region.svg");
fs.writeFileSync(out, dom.window.document.body.innerHTML + "\n");
console.log("Wrote " + out);
