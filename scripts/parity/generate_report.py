#!/usr/bin/env python3
"""Assemble the self-contained parity report (parity-report/index.html).

Reads the machine-readable gate artifacts produced during the migration:
  parity-report/{legacy,modern}/build.log        - build results
  parity-report/{legacy,modern}/tests/summary.txt- unit test counts
  parity-report/{legacy,modern}/smoke.log        - boot + first-response smoke
  parity-report/{legacy,modern}/flows/flowNN.json- 20 golden snapshot flows
  parity-report/diff/flows.json                  - per-flow parity verdicts
  parity-report/diff/schema.txt                  - EF migration SQL diff verdict
  parity-report/fixes.json                       - migration summary + fixes applied

Usage: generate_report.py <parity_report_dir>
"""
import html
import json
import os
import re
import sys


def read(path):
    """Return file contents, or None when missing."""
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


def load_json(path):
    txt = read(path)
    return json.loads(txt) if txt else None


def build_gate(root, side):
    """Parse MSBuild output for warning/error counts."""
    log = read(os.path.join(root, side, "build.log")) or ""
    w = re.search(r"(\d+)\s+Warning\(s\)", log)
    e = re.search(r"(\d+)\s+Error\(s\)", log)
    ok = "Build succeeded" in log and (not e or e.group(1) == "0")
    warnings = int(w.group(1)) if w else None
    errors = int(e.group(1)) if e else None
    return {"pass": ok, "detail": f"{warnings} warning(s), {errors} error(s)"}


def tests_gate(root, side):
    """Parse the unit-test summary counts."""
    txt = read(os.path.join(root, side, "tests", "summary.txt")) or ""
    vals = dict(re.findall(r"(\w+)=(\d+)", txt))
    total, passed, failed = vals.get("total"), vals.get("passed"), vals.get("failed")
    ok = failed == "0" and total is not None and total == passed
    return {"pass": ok, "detail": f"{passed}/{total} passed, {failed} failed"}


def analyser_gate(root, side):
    """Static analyser: legacy has no analyser config (baseline warning output);
    modern enforces EnableNETAnalyzers + TreatWarningsAsErrors via Directory.Build.props."""
    b = build_gate(root, side)
    if side == "legacy":
        return {"pass": True,
                "detail": f"no analyser config (baseline; default output: {b['detail']})"}
    return {"pass": b["pass"],
            "detail": f"NET analysers, AnalysisLevel=latest, warnings-as-errors: {b['detail']}"}


def smoke_gate(root, side):
    """Parse smoke.log for boot + first-response status."""
    txt = read(os.path.join(root, side, "smoke.log")) or ""
    # Two smoke.log key styles exist across captures ("key=value" and "key: value")
    m = (re.search(r"first_response_status_and_curl_time=(\d+)\s+([\d.]+)", txt)
         or re.search(r"first_get_status: (\d+).*?first_get_curl_time_seconds: ([\d.]+)", txt, re.S))
    boot = re.search(r"boot_wait_ms[=:]\s*(\d+)", txt)
    if not m:
        return {"pass": False, "detail": "no smoke result recorded"}
    status = m.group(1)
    return {"pass": status == "200",
            "detail": f"GET /api/educations -> {status} in {m.group(2)}s (boot {boot.group(1) if boot else '?'} ms)"}


def flows_gate(diff):
    """Golden snapshot verdicts from diff/flows.json."""
    flows = diff.get("flows", [])
    matches = sum(1 for f in flows if f["verdict"] == "match")
    justified = sum(1 for f in flows if f["verdict"] == "justified")
    mismatches = len(flows) - matches - justified
    ok = mismatches == 0
    detail = f"{matches}/{len(flows)} exact match"
    if justified:
        detail += f", {justified} justified framework differences"
    if mismatches:
        detail += f", {mismatches} UNEXPLAINED mismatches"
    return {"pass": ok, "detail": detail}


def schema_gate(root):
    """EF SQL parity verdict from diff/schema.txt (verdict line at end)."""
    txt = read(os.path.join(root, "diff", "schema.txt")) or ""
    verdict = ""
    for line in txt.splitlines():
        if line.startswith(("MATCH", "DIFFERS")):
            verdict = line
    ok = verdict.startswith("MATCH") or "semantically equivalent" in verdict
    return {"pass": ok, "detail": verdict or "no verdict recorded"}


def badge(ok, text=None):
    cls = "pass" if ok else "fail"
    label = text or ("PASS" if ok else "FAIL")
    return f'<span class="badge {cls}">{html.escape(label)}</span>'


def pre(obj):
    txt = obj if isinstance(obj, str) else json.dumps(obj, indent=2, sort_keys=True)
    return f"<pre>{html.escape(txt)}</pre>"


def main(root):
    diff = load_json(os.path.join(root, "diff", "flows.json")) or {}
    fixes = load_json(os.path.join(root, "fixes.json")) or {}
    schema = schema_gate(root)
    flow_summary = flows_gate(diff)

    # Gate table: one row per gate, legacy + modern result columns
    gates = [
        ("Build", build_gate(root, "legacy"), build_gate(root, "modern")),
        ("Unit tests", tests_gate(root, "legacy"), tests_gate(root, "modern")),
        ("Static analyser", analyser_gate(root, "legacy"), analyser_gate(root, "modern")),
        ("Smoke test", smoke_gate(root, "legacy"), smoke_gate(root, "modern")),
        ("Golden snapshot diff", {"pass": True, "detail": "baseline captured (20 flows)"}, flow_summary),
        ("EF SQL parity", {"pass": True, "detail": "schema.sql baseline captured"}, schema),
    ]

    rows = []
    for name, lg, md in gates:
        rows.append(
            f"<tr><td>{html.escape(name)}</td>"
            f"<td>{badge(lg['pass'])} {html.escape(lg['detail'])}</td>"
            f"<td>{badge(md['pass'])} {html.escape(md['detail'])}</td></tr>"
        )

    # Expandable per-flow section with legacy response, modern response, verdict
    flow_rows = []
    for f in diff.get("flows", []):
        n = f["flow"]
        lg = load_json(os.path.join(root, "legacy", "flows", f"flow{n:02d}.json")) or {}
        md = load_json(os.path.join(root, "modern", "flows", f"flow{n:02d}.json")) or {}
        verdict = f["verdict"]
        ok = verdict in ("match", "justified")
        label = {"match": "MATCH", "justified": "JUSTIFIED DIFF"}.get(verdict, "MISMATCH")
        diffs_html = ""
        if f.get("diffs"):
            diffs_html = "<h4>Differences</h4>" + pre("\n".join(f["diffs"]))
        if f.get("justification"):
            diffs_html += f'<p class="just">Justification: {html.escape(f["justification"])}</p>'
        flow_rows.append(f"""
<details class="flow {'ok' if ok else 'bad'}">
<summary><span class="fnum">Flow {n:02d}</span> {html.escape(f.get('name') or '')}
 <span class="statuses">{lg.get('status')} / {md.get('status')}</span> {badge(ok, label)}</summary>
<div class="cols">
<div><h4>Legacy (net7)</h4>{pre({'request': lg.get('request'), 'status': lg.get('status'), 'body': lg.get('body')})}</div>
<div><h4>Modernized (net9)</h4>{pre({'request': md.get('request'), 'status': md.get('status'), 'body': md.get('body')})}</div>
</div>{diffs_html}
</details>""")

    sw = diff.get("swagger", {})
    swagger_html = (
        f"<details><summary>OpenAPI (swagger.json) comparison {badge(sw.get('verdict') in ('match', 'justified'), sw.get('verdict', 'n/a').upper())}</summary>"
        + pre("\n".join(sw.get("diffs", []) or ["identical"]))
        + (f'<p class="just">Justification: {html.escape(sw.get("justification", ""))}</p>' if sw.get("justification") else "")
        + "</details>"
    )

    fixes_html = "".join(
        f"<li><b>{html.escape(fx['area'])}:</b> {html.escape(fx['description'])}</li>"
        for fx in fixes.get("fixes", [])
    )
    schema_diff_html = pre(read(os.path.join(root, "diff", "schema.txt")) or "missing")

    all_green = all(lg["pass"] and md["pass"] for _, lg, md in gates)
    page = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>net7 → net9 Migration Parity Report</title>
<style>
body{{font-family:-apple-system,Segoe UI,Roboto,sans-serif;margin:0;background:#f6f8fa;color:#1f2328}}
.wrap{{max-width:1100px;margin:0 auto;padding:24px}}
header{{background:#0d1117;color:#e6edf3;padding:28px 24px;margin-bottom:8px}}
header h1{{margin:0 0 6px;font-size:22px}} header p{{margin:4px 0;color:#9ea7b3;max-width:1050px}}
table{{border-collapse:collapse;width:100%;background:#fff;box-shadow:0 1px 3px rgba(0,0,0,.08)}}
th,td{{border:1px solid #d0d7de;padding:10px 12px;text-align:left;font-size:14px}}
th{{background:#eaeef2}}
.badge{{display:inline-block;padding:2px 10px;border-radius:12px;font-size:12px;font-weight:600;margin-right:6px}}
.badge.pass{{background:#dafbe1;color:#116329}} .badge.fail{{background:#ffebe9;color:#a40e26}}
details{{background:#fff;border:1px solid #d0d7de;border-radius:6px;margin:8px 0;padding:8px 12px}}
summary{{cursor:pointer;font-size:14px}}
.fnum{{font-weight:700}} .statuses{{color:#57606a;font-size:12px;margin:0 8px}}
.cols{{display:flex;gap:12px}} .cols>div{{flex:1;min-width:0}}
pre{{background:#f6f8fa;border:1px solid #d0d7de;border-radius:6px;padding:10px;font-size:12px;overflow:auto;max-height:420px;white-space:pre-wrap}}
h2{{margin-top:32px}} .just{{color:#57606a;font-size:13px}}
ul{{background:#fff;border:1px solid #d0d7de;border-radius:6px;padding:14px 32px;font-size:14px}}
li{{margin:6px 0}}
.overall{{font-size:16px;margin:10px 0}}
</style></head><body>
<header><div class="wrap" style="padding:0">
<h1>WebAPI-Sample-DotNet-7 — .NET 7 → .NET 9 Migration Parity Report</h1>
<p>{html.escape(fixes.get('summary', ''))}</p>
<p class="overall">Overall: {badge(all_green, 'ALL GATES GREEN' if all_green else 'GATES FAILING')}</p>
</div></header>
<div class="wrap">
<h2>Gate Summary</h2>
<table><tr><th>Gate</th><th>Legacy (net7)</th><th>Modernized (net9)</th></tr>{''.join(rows)}</table>
<h2>Fixes Applied During Migration</h2>
<ul>{fixes_html}</ul>
<h2>20 Example Flows (Golden Snapshot)</h2>
{''.join(flow_rows)}
<h2>OpenAPI Comparison</h2>
{swagger_html}
<h2>EF Migration SQL Parity</h2>
{schema_diff_html}
</div></body></html>"""
    out = os.path.join(root, "index.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(page)
    print(f"wrote {out} ({os.path.getsize(out)} bytes); all gates green: {all_green}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "parity-report")
