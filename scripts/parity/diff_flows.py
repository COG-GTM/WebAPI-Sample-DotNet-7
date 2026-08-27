#!/usr/bin/env python3
"""Diff legacy vs modern golden-flow captures and swagger docs.

Usage: diff_flows.py <legacy_dir> <modern_dir> <out_json>

Compares parity-report/legacy/flows/flowNN.json against
parity-report/modern/flows/flowNN.json (status + normalized body) and
legacy/modern swagger.json, writing per-flow verdicts to <out_json>.
"""
import json
import os
import re
import sys

# Known-inherent .NET 7 -> .NET 9 framework differences. A flow whose diffs ALL
# match one of these patterns gets verdict "justified" rather than "mismatch".
JUSTIFIED_PATTERNS = [
    # ProblemDetails "type" URLs moved from RFC 7231 to RFC 9110 in .NET 8+
    (re.compile(r"body\.type: 'https://tools\.ietf\.org/html/rfc7231#section-6\.5\.1' "
                r"!= 'https://tools\.ietf\.org/html/rfc9110#section-15\.5\.1'"),
     ".NET 8+ ProblemDetails uses RFC 9110 section URLs instead of RFC 7231"),
    (re.compile(r"body\.type: 'https://tools\.ietf\.org/html/rfc7231#section-6\.6\.1' "
                r"!= 'https://tools\.ietf\.org/html/rfc9110#section-15\.6\.1'"),
     ".NET 8+ ProblemDetails uses RFC 9110 section URLs instead of RFC 7231"),
    # System.Text.Json reworded its missing-required-properties error message
    (re.compile(r"was missing required properties, including the following: \w+.*"
                r"was missing required properties including: '\w+'\."),
     "System.Text.Json reworded the missing-required-properties message in .NET 9"),
]

# Inherent OpenAPI/Swashbuckle output changes on .NET 9 (documented, not behavioral)
SWAGGER_JUSTIFICATION = (
    "Inherent .NET 9 / Swashbuckle 9 output changes: OpenAPI patch version 3.0.1->3.0.4, "
    "response descriptions 'Success'->'OK', explicit required[] emitted for C# `required` "
    "DTO members, and default parameter style 'simple' no longer serialized. "
    "Endpoints, schemas, and semantics are unchanged."
)
SWAGGER_ALLOWED = [
    re.compile(r"swagger\.openapi: '3\.0\.1' != '3\.0\.4'"),
    re.compile(r"responses\.200\.description: 'Success' != 'OK'"),
    re.compile(r"required: only in modern \(\['degree', 'fieldOfStudy', 'school'\]\)"),
    re.compile(r"parameters\[0\]\.style: only in legacy \('simple'\)"),
]


def justify(diffs):
    """Return justification text if every diff line matches a known
    inherent framework change, else None."""
    reasons = []
    for d in diffs:
        matched = None
        for pat, reason in JUSTIFIED_PATTERNS:
            if pat.search(d.replace("\n", " ")):
                matched = reason
                break
        if not matched:
            return None
        if matched not in reasons:
            reasons.append(matched)
    return "; ".join(reasons) if reasons else None


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
        entry = {
            "flow": n,
            "name": lg.get("name"),
            "legacy_status": lg["status"],
            "modern_status": md["status"],
        }
        if not diffs:
            entry["verdict"] = "match"
        else:
            justification = justify(diffs)
            if justification:
                entry["verdict"] = "justified"
                entry["justification"] = justification
            else:
                entry["verdict"] = "mismatch"
                all_match = False
        entry["diffs"] = diffs
        flows.append(entry)

    # Swagger comparison: ignore volatile info.version-independent fields only
    sw_l = load(os.path.join(legacy_root, "swagger.json"))
    sw_m = load(os.path.join(modern_root, "swagger.json"))
    sw_diffs = json_diff(sw_l, sw_m, "swagger") if sw_l and sw_m else ["swagger.json missing on one side"]
    swagger = {"diffs": sw_diffs}
    if not sw_diffs:
        swagger["verdict"] = "match"
    elif all(any(p.search(d) for p in SWAGGER_ALLOWED) for d in sw_diffs):
        swagger["verdict"] = "justified"
        swagger["justification"] = SWAGGER_JUSTIFICATION
    else:
        swagger["verdict"] = "mismatch"

    result = {
        "all_flows_match": all_match,
        "swagger": swagger,
        "flows": flows,
    }
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w") as f:
        json.dump(result, f, indent=2)
        f.write("\n")
    print(f"flows match: {all_match}; swagger: {swagger['verdict']}")
    for fl in flows:
        if fl["verdict"] not in ("match", "justified"):
            print(f"  flow {fl['flow']}: {fl['verdict']}: {fl['diffs'][:3]}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1], sys.argv[2], sys.argv[3]))
