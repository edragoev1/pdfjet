#!/usr/bin/env python3
"""Lists the public API of the Java, C#, Go and Swift ports and diffs them.

    ./audit-api.py            # the differences, as Markdown
    ./audit-api.py --dump     # every public member of every port, by class

The script reads the sources in com/pdfjet, net/pdfjet, src and Sources/PDFjet
and collects, per public type, its public constructors, methods, fields,
constants and enum cases. The names are then matched across the ports without
regard to case and underscores, so Java setLocation, C# SetLocation, Go
SetLocation and Swift setLocation are one member, as are UserAccess.PRINT and
UserAccess.Print. Constructors are matched as <init>: Java, C# and Swift
constructors and Go New* functions that return the type.

Go has no overloading, so a Go method whose name is a member name of the same
class in another port followed by a suffix (DrawStringUsingFontSize for
drawString, NewBoxAt for the Box constructor) counts as that member, and the
suffix is listed with it. Go constants live in packages (color.Blue,
alignment.Left); a package with at most one exported type is treated as the
class of the same name.

A public no-arg constructor of a class that has only constants and static
members is not reported: Java declares one for javadoc and C# has an implicit
one. C# properties count as members, matched by name with the getters of the
other ports.

The report has three tables: types that are not in every port, members that
are not in every port of a type that is, and members whose numbers of
parameters differ between ports. What remains after the fixes of v9.0.0 is
the set of conventions listed in the README Port differences section; keep
the report to those when changing a public signature.
"""

import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.abspath(__file__))
PORTS = ["java", "cs", "go", "swift"]
TITLES = {"java": "Java", "cs": "C#", "go": "Go", "swift": "Swift"}


class Member:
    def __init__(self, kind, static=False):
        self.kind = kind          # ctor, method, field, const, case, property
        self.static = static      # a static method or a constant
        self.names = []           # spellings in the port, in source order
        self.arities = set()      # parameter counts, methods and ctors only

    def instance(self):
        return self.kind in ("method", "field", "property") and not self.static

    def add(self, name, arity=None):
        if name not in self.names:
            self.names.append(name)
        if arity is not None:
            self.arities.add(arity)


def key_of(name):
    return name.lower().replace("_", "")


def strip_comments(text, line="//"):
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    text = re.sub(r'"(?:\\.|[^"\\\n])*"', '""', text)
    text = re.sub(re.escape(line) + r"[^\n]*", "", text)
    return text


def count_params(params, sep=","):
    """Counts the top level comma separated entries of a parameter list."""
    params = params.strip()
    if not params:
        return 0
    depth = 0
    n = 1
    for ch in params:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        elif ch == sep and depth == 0:
            n += 1
    return n


def matching_paren(text, start):
    """Returns the index of the parenthesis that closes the one at start."""
    depth = 0
    for i in range(start, len(text)):
        if text[i] == "(":
            depth += 1
        elif text[i] == ")":
            depth -= 1
            if depth == 0:
                return i
    return -1


def files(folder, ext, skip=()):
    for dirpath, dirnames, filenames in os.walk(os.path.join(ROOT, folder)):
        dirnames[:] = sorted(d for d in dirnames if d not in skip)
        for name in sorted(filenames):
            if name.endswith(ext):
                yield os.path.join(dirpath, name)


# Java and C# ---------------------------------------------------------------

JAVA_MODS = r"(?:(?:static|final|abstract|synchronized|override|virtual|new|readonly|const|sealed|unsafe)\s+)*"
TYPE = r"[\w.\[\]?]+(?:<[\w.<>\[\],? ]*>)?(?:\[\])*"


def read_c_like(api, path, port):
    text = strip_comments(open(path, encoding="utf-8").read())
    decl = re.search(
        r"public\s+(?:(?:static|final|abstract|sealed)\s+)*(class|interface|enum|struct)\s+(\w+)", text)
    if not decl:
        return
    kind, cls = decl.group(1), decl.group(2)
    if port == "cs" and kind == "interface" and re.match(r"I[A-Z]", cls):
        cls = cls[1:]
    members = api[cls]
    body_start = text.index("{", decl.end())
    body = text[body_start + 1:]
    # Members of nested private or internal types are not public.
    for nested in reversed(list(re.finditer(
            r"(?:private|internal|protected)\s+(?:static\s+)?(?:final\s+)?(?:class|struct|enum)\s+\w+[^{]*\{", body))):
        depth = 0
        for i in range(nested.end() - 1, len(body)):
            if body[i] == "{":
                depth += 1
            elif body[i] == "}":
                depth -= 1
                if depth == 0:
                    body = body[:nested.start()] + body[i + 1:]
                    break
    if kind == "enum":
        end = re.search(r"[;}]", body)
        for m in re.finditer(r"\b([A-Za-z_]\w*)\s*(?:\([^)]*\))?\s*(?:=\s*[^,}]+)?\s*(?=[,;}])", body[:end.end()]):
            members.setdefault(key_of(m.group(1)), Member("case")).add(m.group(1))
        if port == "java":
            body = body[end.end():]
        else:
            return
    if kind == "interface":
        for m in re.finditer(r"(?:public\s+)?(" + TYPE + r")\s+(\w+)\s*\(([^)]*)\)\s*(?:throws[^;]*)?;", body):
            members.setdefault(key_of(m.group(2)), Member("method")).add(m.group(2), count_params(m.group(3)))
        return
    for m in re.finditer(
            r"public\s+" + JAVA_MODS + r"(?:(" + TYPE + r")\s+)?(\w+)\s*\(([^)]*)\)\s*(?:throws[^{;]*)?"
            r"(?::\s*(?:this|base)\s*\((?:[^()]|\((?:[^()]|\([^()]*\))*\))*\)\s*)?[{;]", body):
        rtype, name, params = m.groups()
        if rtype is None or name == cls:
            members.setdefault("<init>", Member("ctor")).add(name, count_params(params))
        elif rtype not in ("new", "override", "static"):
            members.setdefault(key_of(name), Member("method", "static" in m.group(0))).add(name, count_params(params))
    for m in re.finditer(r"public\s+" + JAVA_MODS + r"(" + TYPE + r")\s+(\w+)\s*(=>|=|;|\{)", body):
        rtype, name, end = m.groups()
        if rtype in ("class", "enum", "interface", "struct"):
            continue
        if end in ("=>", "{"):
            members.setdefault(key_of(name), Member("property")).add(name)
        elif "static" in m.group(0) or "const" in m.group(0):
            members.setdefault(key_of(name), Member("const")).add(name)
        else:
            members.setdefault(key_of(name), Member("field")).add(name)
    # A class that declares no constructor has a public no-arg one.
    if kind == "class" and "<init>" not in members and not re.search(
            r"(?:abstract|static)\s+(?:\w+\s+)*class\s+" + cls + r"\b", text) and not re.search(
            r"(?:protected|private|internal)\s+" + cls + r"\s*\(", body):
        members.setdefault("<init>", Member("ctor")).add(cls, 0)


# Go -------------------------------------------------------------------------

def go_params(params):
    return count_params(params)


def read_go(api, folder, pkg):
    """Reads one Go package; folder is its path and pkg its name (or None for the root)."""
    texts = {}
    for path in sorted(os.listdir(folder)):
        if path.endswith(".go") and not path.endswith("_test.go"):
            texts[path] = strip_comments(open(os.path.join(folder, path), encoding="utf-8").read())
    types = set()
    for text in texts.values():
        types.update(re.findall(r"^type\s+([A-Z]\w*)\s", text, flags=re.M))
    one_type = pkg is not None and len(types) <= 1

    def cls_for(type_name, stem):
        if pkg is None:
            return type_name or stem
        if one_type:
            return pkg
        return type_name or pkg

    for path, text in texts.items():
        stem = path[:-3]
        # Types and their exported fields and interface methods.
        for m in re.finditer(r"^type\s+([A-Z]\w*)\s+(struct|interface)\s*\{", text, flags=re.M):
            cls = cls_for(m.group(1), stem)
            members = api[cls]
            end = text.index("\n}", m.end())
            for line in text[m.end():end].split("\n"):
                line = line.strip()
                if m.group(2) == "struct":
                    f = re.match(r"([A-Z]\w*(?:\s*,\s*[A-Z]\w*)*)\s+\S", line)
                    if f:
                        for name in re.split(r"\s*,\s*", f.group(1)):
                            members.setdefault(key_of(name), Member("field")).add(name)
                else:
                    f = re.match(r"([A-Z]\w*)\((.*)\)", line)
                    if f:
                        members.setdefault(key_of(f.group(1)), Member("method")).add(f.group(1), go_params(f.group(2)))
        for m in re.finditer(r"^type\s+([A-Z]\w*)\s+(?!struct|interface)\w", text, flags=re.M):
            api[cls_for(m.group(1), stem)]
        # Functions and methods.
        for m in re.finditer(r"^func\s+(?:\(\s*\w+\s+\*?(\w+)\s*\)\s+)?([A-Z]\w*)\s*\(", text, flags=re.M):
            recv, name = m.groups()
            close = matching_paren(text, m.end() - 1)
            params = text[m.end():close]
            rest = text[close + 1:text.index("{", close)].strip()
            if recv:
                api[cls_for(recv, stem)].setdefault(key_of(name), Member("method")).add(name, go_params(params))
                continue
            ret = re.match(r"\(?\*?(\w+)", rest)
            if name.startswith("New") and ret and ret.group(1) in types:
                api[cls_for(ret.group(1), stem)].setdefault("<init>", Member("ctor")).add(name, go_params(params))
            else:
                api[cls_for(None, stem)].setdefault(key_of(name), Member("method", True)).add(name, go_params(params))
        # Constants and variables.
        for block in re.finditer(r"^(const|var)\s+\((.*?)^\)", text, flags=re.M | re.S):
            for line in block.group(2).split("\n"):
                f = re.match(r"\s*([A-Z]\w*(?:\s*,\s*[A-Z]\w*)*)\s*(?:([A-Z]\w*)\s*)?(?:=|$)", line)
                if f:
                    type_name = f.group(2) if f.group(2) in types else None
                    for name in re.split(r"\s*,\s*", f.group(1)):
                        api[cls_for(type_name, stem)].setdefault(key_of(name), Member("const")).add(name)
        for f in re.finditer(r"^(?:const|var)\s+([A-Z]\w*)\s*(?:([A-Z]\w*)\s*)?=", text, flags=re.M):
            type_name = f.group(2) if f.group(2) in types else None
            api[cls_for(type_name, stem)].setdefault(key_of(f.group(1)), Member("const")).add(f.group(1))


def read_go_all(api):
    src = os.path.join(ROOT, "src")
    read_go(api, src, None)
    for name in sorted(os.listdir(src)):
        folder = os.path.join(src, name)
        if os.path.isdir(folder) and name != "examples" and any(f.endswith(".go") for f in os.listdir(folder)):
            read_go(api, folder, name)


# Swift ----------------------------------------------------------------------

def swift_arities(params):
    n = count_params(params)
    defaults = params.count("=")
    return set(range(n - defaults, n + 1))


def read_swift(api, path):
    text = strip_comments(open(path, encoding="utf-8").read())
    stem = os.path.basename(path)[:-6]
    cls = None            # the type whose body we are in
    public_type = False
    kind = None
    depth = 0             # brace depth; 1 inside a top level type
    mods = r"(?:(?:static|class|override|final|convenience|mutating|required)\s+)*"
    lines = text.split("\n")
    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        if depth == 0:
            top = re.match(r"(public|open|internal|fileprivate|private)?\s*(?:final\s+)?(class|struct|enum|protocol|extension)\s+(\w+)", stripped)
            if top:
                kind = top.group(2)
                cls = top.group(3)
                public_type = top.group(1) in ("public", "open") or (kind == "extension" and cls in api)
                if public_type:
                    api[cls]
        m = re.match(mods + r"(public|open)\s+" + mods + r"(func|init|let|var)\s*(\w*)", stripped)
        if kind == "protocol" and public_type and depth == 1:
            m = re.match(r"()(?:static\s+)?(func|init|let|var)\s*(\w*)", stripped)
        if m and (depth == 0 or (depth == 1 and public_type)):
            target = cls if depth == 1 else stem
            members = api[target]
            what, name = m.group(2), m.group(3)
            if what in ("func", "init"):
                start = text.index("(", sum(len(l) + 1 for l in lines[:i]) + len(line) - len(line.lstrip()) + m.end() - len(m.group(0)) + len(m.group(0)) - 1)
                close = matching_paren(text, start)
                params = text[start + 1:close]
                mem = members.setdefault("<init>" if what == "init" else key_of(name),
                                         Member("ctor" if what == "init" else "method", "static" in stripped))
                mem.add("init" if what == "init" else name)
                mem.arities.update(swift_arities(params))
                skipped = text[start:close].count("\n")
                depth += sum(l.count("{") - l.count("}") for l in lines[i:i + skipped])
                i += skipped
                line = lines[i]
            elif "static" in stripped or what == "let":
                members.setdefault(key_of(name), Member("const")).add(name)
            else:
                members.setdefault(key_of(name), Member("field")).add(name)
        elif kind == "enum" and public_type and depth == 1:
            c = re.match(r"case\s+(.*)", stripped)
            if c:
                for name in re.findall(r"(\w+)\s*(?:=\s*[^,]+)?(?:,|$)", c.group(1)):
                    api[cls].setdefault(key_of(name), Member("case")).add(name)
        depth += line.count("{") - line.count("}")
        i += 1


# Collect ---------------------------------------------------------------------

def collect():
    api = {port: defaultdict(dict) for port in PORTS}
    for path in files("com/pdfjet", ".java"):
        read_c_like(api["java"], path, "java")
    for path in files("net/pdfjet", ".cs"):
        read_c_like(api["cs"], path, "cs")
    read_go_all(api["go"])
    for path in files("Sources/PDFjet", ".swift"):
        read_swift(api["swift"], path)
    return api


def merge(api):
    """Returns {class key: {port: (display name, members)}} and merges Go variants."""
    classes = defaultdict(dict)
    for port in PORTS:
        for cls, members in api[port].items():
            if port in classes[key_of(cls)]:
                # A Go file and its type that differ in case only (barcode.go, Barcode).
                name, merged = classes[key_of(cls)][port]
                merged.update(members)
                if cls[0].isupper():
                    classes[key_of(cls)][port] = (cls, merged)
            else:
                classes[key_of(cls)][port] = (cls, members)
    for ck, ports in classes.items():
        # A class of constants and static methods needs no instances; Java declares
        # a constructor for javadoc and C# has an implicit one. Not a difference.
        for port in ports:
            members = ports[port][1]
            if "<init>" in members and members["<init>"].arities == {0} and not any(
                    m.instance() for k, m in members.items() if k != "<init>"):
                del members["<init>"]
        if "go" not in ports:
            continue
        others = set()
        for port in PORTS:
            if port != "go" and port in ports:
                others.update(ports[port][1].keys())
        go_members = ports["go"][1]
        for mk in sorted(go_members.keys(), key=len, reverse=True):
            if mk in others or mk == "<init>":
                continue
            member = go_members[mk]
            name = member.names[0]
            base = None
            for ok in sorted(others, key=len, reverse=True):
                if ok != "<init>" and mk.startswith(ok) and len(mk) > len(ok) and name[len(ok)].isupper():
                    base = ok
                    break
            if base is None:
                continue
            target = go_members.setdefault(base, Member(member.kind))
            for n in member.names:
                target.add(n)
            target.arities.update(member.arities)
            del go_members[mk]
    return classes


def arities(member):
    return ",".join(str(a) for a in sorted(member.arities)) if member.arities else ""


def cell(member):
    if member is None:
        return "-"
    text = ", ".join(member.names)
    if member.arities:
        text += " (" + arities(member) + ")"
    return text


def display(ports):
    for port in PORTS:
        if port in ports:
            return ports[port][0]


def report(classes):
    out = []
    out.append("# Public API audit\n")
    out.append("Public types per port: " + ", ".join(
        "%s %d" % (TITLES[p], sum(1 for c in classes.values() if p in c)) for p in PORTS) + "\n")
    header = "| %s | " + " | ".join(TITLES[p] for p in PORTS) + " |\n|---|" + "---|" * len(PORTS) + "\n"

    out.append("\n## Types not in every port\n\n" + header % "Type")
    for ck in sorted(classes):
        ports = classes[ck]
        if len(ports) < len(PORTS):
            out.append("| %s | %s |\n" % (display(ports), " | ".join(ports[p][0] if p in ports else "-" for p in PORTS)))

    out.append("\n## Members not in every port\n")
    for ck in sorted(classes):
        ports = classes[ck]
        if len(ports) < len(PORTS):
            continue
        keys = set()
        for port in PORTS:
            keys.update(ports[port][1].keys())
        rows = []
        for mk in sorted(keys):
            members = [ports[p][1].get(mk) for p in PORTS]
            if any(m is None for m in members):
                rows.append("| %s | %s |\n" % (mk, " | ".join(cell(m) for m in members)))
        if rows:
            out.append("\n### %s\n\n" % display(ports) + header % "Member" + "".join(rows))

    out.append("\n## Members whose parameter counts differ\n\n" + header % "Member")
    for ck in sorted(classes):
        ports = classes[ck]
        if len(ports) < len(PORTS):
            continue
        for mk in sorted(ports["java"][1].keys()):
            members = [ports[p][1].get(mk) for p in PORTS]
            if any(m is None for m in members):
                continue
            sets = [m.arities for m in members if m.arities]
            if len(sets) == len(PORTS) and any(s != sets[0] for s in sets):
                out.append("| %s.%s | %s |\n" % (display(ports), mk, " | ".join(cell(m) for m in members)))
    return "".join(out)


def dump(classes):
    for ck in sorted(classes):
        ports = classes[ck]
        print("== " + display(ports))
        keys = set()
        for port in ports:
            keys.update(ports[port][1].keys())
        for mk in sorted(keys):
            print("  %-40s %s" % (mk, " | ".join(cell(ports[p][1].get(mk)) if p in ports else "?" for p in PORTS)))


if __name__ == "__main__":
    classes = merge(collect())
    if "--dump" in sys.argv:
        dump(classes)
    else:
        sys.stdout.write(report(classes))
