#!/usr/bin/env python3
"""Diff legacy vs modern golden-flow captures and swagger docs.

Usage: diff_flows.py <legacy_dir> <modern_dir> <out_json>

Compares parity-report/legacy/flows/flowNN.json against
parity-report/modern/flows/flowNN.json (status + normalized body) and
legacy/modern swagger.json, writing per-flow verdicts to <out_json>.
"""
import json
import os
import sys


def load(path):
    """Load a JSON file, returning None when it is missing."""
    if not os.path.exists(path):
        return None
    with open(path) as f:
        return json.load(f)


def json_diff(a, b, path=""):
    """Recursively collect human-readable differences between two JSON values."""
    diffs = []
    if type(a) is not type(b):
        diffs.append(f"{path or '$'}: type {type(a).__name__} != {type(b).__name__} ({a!r} vs {b!r})")
    elif isinstance(a, dict):
        for k in sorted(set(a) | set(b)):
            if k not in a:
                diffs.append(f"{path}.{k}: only in modern ({b[k]!r})")
            elif k not in b:
                diffs.append(f"{path}.{k}: only in legacy ({a[k]!r})")
            else:
                diffs.extend(json_diff(a[k], b[k], f"{path}.{k}"))
    elif isinstance(a, list):
        if len(a) != len(b):
            diffs.append(f"{path}: list length {len(a)} != {len(b)}")
        for i, (x, y) in enumerate(zip(a, b)):
            diffs.extend(json_diff(x, y, f"{path}[{i}]"))
    elif a != b:
        diffs.append(f"{path or '$'}: {a!r} != {b!r}")
    return diffs


def main(legacy_root, modern_root, out_path):
    flows = []
    all_match = True
    for n in range(1, 21):
        fname = f"flow{n:02d}.json"
        lg = load(os.path.join(legacy_root, "flows", fname))
        md = load(os.path.join(modern_root, "flows", fname))
        if lg is None or md is None:
            flows.append({"flow": n, "verdict": "missing",
                          "diffs": [f"missing capture: legacy={lg is not None}, modern={md is not None}"]})
            all_match = False
            continue
        diffs = []
        if lg["status"] != md["status"]:
            diffs.append(f"status: {lg['status']} != {md['status']}")
        diffs.extend(json_diff(lg.get("body"), md.get("body"), "body"))
        verdict = "match" if not diffs else "mismatch"
        if diffs:
            all_match = False
        flows.append({
            "flow": n,
            "name": lg.get("name"),
            "legacy_status": lg["status"],
            "modern_status": md["status"],
            "verdict": verdict,
            "diffs": diffs,
        })

    # Swagger comparison: ignore volatile info.version-independent fields only
    sw_l = load(os.path.join(legacy_root, "swagger.json"))
    sw_m = load(os.path.join(modern_root, "swagger.json"))
    sw_diffs = json_diff(sw_l, sw_m, "swagger") if sw_l and sw_m else ["swagger.json missing on one side"]
    swagger_verdict = "match" if not sw_diffs else "mismatch"

    result = {
        "all_flows_match": all_match,
        "swagger": {"verdict": swagger_verdict, "diffs": sw_diffs},
        "flows": flows,
    }
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w") as f:
        json.dump(result, f, indent=2)
        f.write("\n")
    print(f"flows match: {all_match}; swagger: {swagger_verdict}")
    for fl in flows:
        if fl["verdict"] != "match":
            print(f"  flow {fl['flow']}: {fl['verdict']}: {fl['diffs'][:3]}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1], sys.argv[2], sys.argv[3]))
