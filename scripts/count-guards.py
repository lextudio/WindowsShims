#!/usr/bin/env python3
"""Guard-budget measurement for the LeXtudio.Windows shim.

Reproduces the numbers in docs/richtextbox/code-reuse.md:

  - how many ext/wpf upstream files are actually linked (Compile Include
    net of Compile Remove),
  - how many of them compile pristine (no #if HAS_UNO guard),
  - the HAS_UNO guard count and density, and the large-guard list,
  - the same per top-level namespace.

Usage (from the repo root):

  python3 scripts/count-guards.py                 # human-readable report
  python3 scripts/count-guards.py --json          # machine-readable
  python3 scripts/count-guards.py --gate          # exit 1 if budget exceeded

Soft-gate thresholds (docs/richtextbox/code-reuse.md, proposal P1):
  - linked Documents-family guard density > 0.25%
  - any single HAS_UNO block > 4000 chars without the reason file
  - pristine share of the linked Documents family < 75%
"""

import argparse
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CSPROJ = REPO / "src" / "LeXtudio.Windows" / "LeXtudio.Windows.csproj"

GUARD_RE = re.compile(r"^#\s*(if|ifdef|ifndef|elif|else|endif)\b", re.MULTILINE)
COND_RE = re.compile(r"^#\s*(?:if|ifdef|ifndef)\s*(.*)$", re.MULTILINE)

DENSITY_LIMIT = 0.25
BLOCK_CHAR_LIMIT = 4000
PRISTINE_MIN = 75.0


def linked_files(csproj: Path):
    """Return (linked, removed) sets of ext/wpf source files, repo-root relative."""
    text = csproj.read_text(encoding="utf-8", errors="replace")
    includes = set(re.findall(r'Compile Include="([^"]*ext\\wpf[^"]*)"', text))
    removes = set(re.findall(r'Compile Remove="([^"]*ext\\wpf[^"]*)"', text))
    norm = lambda p: p.replace("\\", "/").replace("../../", "")
    return {norm(p) for p in includes} - {norm(p) for p in removes}


def pair_blocks(src: str):
    """Pair every #if/#ifdef/#ifndef..#endif span. Returns list of
    (start, end, condition) where end is the #endif line start."""
    stack = []
    blocks = []
    for m in GUARD_RE.finditer(src):
        tag = m.group(1)
        line_start = src.rfind("\n", 0, m.start()) + 1
        if tag in ("if", "ifdef", "ifndef"):
            cond = src[m.start():].splitlines()[0].strip()[len("#"):].strip()
            stack.append((line_start, cond))
        elif tag == "elif":
            if stack:
                cond = src[m.start():].splitlines()[0].strip()[len("#"):].strip()
                stack[-1] = (stack[-1][0], cond)
        elif tag == "else":
            if stack:
                stack[-1] = (stack[-1][0], "else")
        elif tag == "endif":
            if stack:
                start, cond = stack.pop()
                blocks.append((start, line_start, cond))
    return blocks


def analyze(path: Path):
    src = path.read_text(encoding="utf-8", errors="replace")
    lines = src.count("\n")
    blocks = pair_blocks(src)
    has_uno = [b for b in blocks if "HAS_UNO" in b[2]]
    has_uno_fwd = [b for b in has_uno if not b[2].startswith("!")]
    has_uno_neg = [b for b in has_uno if b[2].startswith("!")]
    big = [b for b in has_uno if b[1] - b[0] > BLOCK_CHAR_LIMIT]
    # File-level guard: everything after the license header and using block
    # sits in one guard block — such files are effectively excluded, not bridged.
    file_level = False
    if has_uno:
        body_lines = src.splitlines()
        idx = 0
        while idx < len(body_lines) and (body_lines[idx].startswith("//") or body_lines[idx].strip() == ""):
            idx += 1
        while idx < len(body_lines) and body_lines[idx].startswith("using "):
            idx += 1
        tail = "\n".join(body_lines[idx:]).strip()
        if tail.startswith("#if") and tail.splitlines()[-1].lstrip().startswith("#endif"):
            file_level = True
    return {
        "lines": lines,
        "blocks": len(blocks),
        "has_uno": len(has_uno),
        "has_uno_fwd": len(has_uno_fwd),
        "has_uno_neg": len(has_uno_neg),
        "file_level": file_level,
        "big_blocks": [(b[1] - b[0], b[2], path.name) for b in big],
    }


def family_of(rel: str) -> str:
    """Group a linked file by its WPF family directory (Documents/Controls/...)."""
    parts = rel.split("/")
    if "System/Windows/Documents" in rel:
        return "Documents"
    if "System/Windows/Controls" in rel:
        return "Controls"
    if "System/Windows/Media" in rel:
        return "Media"
    if "System/Windows/Markup" in rel:
        return "Markup"
    if "System/Windows/Input" in rel:
        return "Input"
    if "WindowsBase" in rel:
        return "WindowsBase"
    if "PresentationCore" in rel:
        return "PresentationCore"
    if len(parts) > 6:
        return parts[6]
    return "(root)"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--gate", action="store_true")
    args = ap.parse_args()

    linked = linked_files(CSPROJ)
    per_file = {}
    per_ns = defaultdict(lambda: {"files": 0, "lines": 0, "has_uno": 0, "pristine": 0, "file_level": 0})
    all_big = []
    file_level_files = []

    for rel in sorted(linked):
        path = REPO / rel
        if not path.exists():
            continue
        a = analyze(path)
        per_file[rel] = a
        ns = family_of(rel)
        per_ns[ns]["files"] += 1
        per_ns[ns]["lines"] += a["lines"]
        per_ns[ns]["has_uno"] += a["has_uno"]
        if a["has_uno"] == 0:
            per_ns[ns]["pristine"] += 1
        if a["file_level"]:
            per_ns[ns]["file_level"] += 1
            file_level_files.append((rel, a["lines"]))
        all_big.extend(a["big_blocks"])

    total_lines = sum(a["lines"] for a in per_file.values())
    total_has_uno = sum(a["has_uno"] for a in per_file.values())
    pristine = sum(1 for a in per_file.values() if a["has_uno"] == 0)

    docs_lines = per_ns.get("Documents", {}).get("lines", 0) or 1
    docs_has = per_ns.get("Documents", {}).get("has_uno", 0)
    docs_density = 100.0 * docs_has / docs_lines
    docs_pristine = per_ns.get("Documents", {}).get("pristine", 0)
    docs_files = per_ns.get("Documents", {}).get("files", 0)
    docs_pristine_pct = 100.0 * docs_pristine / docs_files if docs_files else 0

    all_big.sort(reverse=True)

    csproj_text = CSPROJ.read_text(encoding="utf-8", errors="replace")
    compile_remove_rules = len(re.findall(r'<Compile Remove="([^"]*ext\\wpf[^"]*)"', csproj_text))

    report = {
        "linked_files": len(linked),
        "total_lines": total_lines,
        "total_has_uno_blocks": total_has_uno,
        "pristine_files": pristine,
        "file_level_files": [(rel, n) for rel, n in sorted(file_level_files, key=lambda x: -x[1])],
        "by_namespace": {k: dict(v) for k, v in sorted(per_ns.items())},
        "documents_density_pct": round(docs_density, 3),
        "documents_pristine_pct": round(docs_pristine_pct, 1),
        "big_has_uno_blocks": all_big[:20],
        "compile_remove_rules": compile_remove_rules,
    }

    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print("Guard budget — linked ext/wpf upstream files")
        print("=" * 60)
        print(f"linked files:              {report['linked_files']}")
        print(f"total lines:               {report['total_lines']}")
        print(f"HAS_UNO blocks total:      {report['total_has_uno_blocks']}")
        print(f"pristine files (no guard): {report['pristine_files']}")
        print(f"Compile Remove rules:         {report['compile_remove_rules']}")
        print()
        print("by namespace:")
        for ns, v in report["by_namespace"].items():
            density = 100.0 * v["has_uno"] / v["lines"] if v["lines"] else 0
            fl = f" file-level={v['file_level']}" if v["file_level"] else ""
            print(f"  {ns:<14} files={v['files']:>4} lines={v['lines']:>8} "
                  f"HAS_UNO={v['has_uno']:>4} pristine={v['pristine']}/{v['files']}{fl} "
                  f"density={density:.3f}%")
        if file_level_files:
            print("file-level guarded (effectively excluded, not bridged):")
            for rel, n in sorted(file_level_files, key=lambda x: -x[1]):
                print(f"  {rel} ({n} lines)")
        print()
        print(f"Documents density:         {report['documents_density_pct']}% "
              f"(limit {DENSITY_LIMIT}%)")
        print(f"Documents pristine share:  {report['documents_pristine_pct']}% "
              f"(min {PRISTINE_MIN}%)")
        print(f"largest HAS_UNO blocks (> {BLOCK_CHAR_LIMIT} chars): "
              f"{len(report['big_has_uno_blocks'])}")
        for size, cond, f in report["big_has_uno_blocks"][:10]:
            print(f"  {size:>6}  {cond:30.30}  {f}")

    if args.gate:
        violations = []
        if docs_density > DENSITY_LIMIT:
            violations.append(f"Documents HAS_UNO density {docs_density:.3f}% > {DENSITY_LIMIT}%")
        if docs_pristine_pct < PRISTINE_MIN:
            violations.append(f"Documents pristine share {docs_pristine_pct:.1f}% < {PRISTINE_MIN}%")
        # Only forward HAS_UNO blocks are gated: a #if !HAS_UNO half is the
        # retained upstream WPF implementation compiled out for Uno, and is
        # intentionally left large (see docs/richtextbox/code-reuse.md).
        big_fwd = [b for b in report["big_has_uno_blocks"] if "!HAS_UNO" not in b[1]]
        for size, cond, f in big_fwd:
            if size > BLOCK_CHAR_LIMIT:
                violations.append(f"{f}: {cond} block is {size} chars (> {BLOCK_CHAR_LIMIT})")
        if violations:
            print("\nGUARD BUDGET EXCEEDED:")
            for v in violations:
                print(f"  - {v}")
            sys.exit(1)
        print("\nguard budget OK")


if __name__ == "__main__":
    main()
