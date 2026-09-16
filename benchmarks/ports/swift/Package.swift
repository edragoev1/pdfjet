// swift-tools-version: 6.2
import PackageDescription

// A package of its own, so that the benchmark is not built with the examples
// of the PDFjet package it depends on.
let package = Package(
    name: "PortBench",
    dependencies: [
        .package(path: "../../.."),
    ],
    targets: [
        .executableTarget(name: "PortBench",
            dependencies: [.product(name: "PDFjet", package: "pdfjet")]),
    ]
)
