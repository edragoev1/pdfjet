// swift-tools-version: 6.2
import PackageDescription

// The first PDF of a process, by phase; a package of its own, as the table
// benchmark's is.
let package = Package(
    name: "FirstPdf",
    dependencies: [
        .package(path: "../../.."),
    ],
    targets: [
        .executableTarget(name: "FirstPdf",
            dependencies: [.product(name: "PDFjet", package: "measure")]),
    ]
)
