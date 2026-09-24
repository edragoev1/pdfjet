#!/usr/bin/env python3
"""Compares the public API of the four ports at a tag and in this checkout,
the lists check-api.sh writes, and prints what is gone (-), changed (~) and
added (+) in each. A line marked deprecated, [Obsolete] or [deprecated], or a
Swift initializer made a convenience one of the same signature, is changed
rather than gone and added, and does not fail the check.

    python3 util/compare-api.py build/check-api v9.0.1
"""
import re
import sys

work, base = sys.argv[1], sys.argv[2]


def key(line):
    """The line without the marks that change no signature."""
    return re.sub(r"^\[(Obsolete|deprecated)\] |\bconvenience ", "", line)


gone = False
for port in ["java", "go", "dotnet", "swift"]:
    with open(f"{work}/base-api/{port}.txt") as f:
        old = set(f.read().splitlines()) - {""}
    with open(f"{work}/head-api/{port}.txt") as f:
        new = set(f.read().splitlines()) - {""}
    old_keys = {key(line): line for line in old - new}
    new_keys = {key(line): line for line in new - old}
    changed = sorted(k for k in old_keys if k in new_keys)
    removed = sorted(old_keys[k] for k in old_keys if k not in new_keys)
    added = sorted(new_keys[k] for k in new_keys if k not in old_keys)
    print(f"\n=== {port}: {len(removed)} gone (-), {len(changed)} changed (~), {len(added)} added (+) since {base}")
    for line in removed:
        print("- " + line)
    for k in changed:
        print("~ " + old_keys[k])
        print("    now " + new_keys[k])
    for line in added:
        print("+ " + line)
    gone = gone or bool(removed)

print()
if gone:
    print(f"Some of the API of {base} is gone (-): goal 6 allows none.")
    sys.exit(1)
print(f"Nothing of {base} is gone. Hold what is added (+) and changed (~) against the list of goal 6 in TODO.md.")
