#!/usr/bin/env python3
#
# make-ev-by-model-year.py
#
# Copyright (c) 2026 PDFjet Software
# Licensed under the MIT License. See LICENSE file in the project root.
#
# Writes resources/data/Electric_Vehicles_by_Model_Year.csv, the vehicles of
# the Washington State Electric Vehicle Population Data counted by model year
# and by kind, which Example_25 draws as a chart: the rows whose "Electric
# Vehicle Type" begins with "Battery Electric" and with "Plug-in Hybrid",
# counted by the "Model Year" column, one line for each model year in the
# file. The full data file, 124,716 rows and 30 MB, is not in the repository;
# by default it is read from the folder pdfjet-test next to the project.
# Run it from the project root; it needs nothing but Python 3.
#
#     python3 util/make-ev-by-model-year.py [path/to/Electric_Vehicle_Population_Data.csv]

import collections
import csv
import sys

INPUT = "../pdfjet-test/Electric_Vehicle_Population_Data.csv"
OUTPUT = "resources/data/Electric_Vehicles_by_Model_Year.csv"

KINDS = ["Battery Electric", "Plug-in Hybrid"]

path = sys.argv[1] if len(sys.argv) > 1 else INPUT
counts = collections.defaultdict(lambda: [0] * len(KINDS))
with open(path, newline="", encoding="utf-8") as file:
    reader = csv.reader(file)
    header = next(reader)
    year_column = header.index("Model Year")
    type_column = header.index("Electric Vehicle Type")
    for row in reader:
        for i, kind in enumerate(KINDS):
            if row[type_column].startswith(kind):
                counts[int(row[year_column])][i] += 1

with open(OUTPUT, "w", newline="", encoding="utf-8") as file:
    file.write("Model Year," + ",".join(KINDS) + "\n")
    for year in sorted(counts):
        file.write(f"{year}," + ",".join(str(n) for n in counts[year]) + "\n")
