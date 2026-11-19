#!/usr/bin/env python3
"""Replays a fixed request sequence against the Educations API and records normalized responses.

Usage: parity.py capture <base_url> <out_dir>
       parity.py diff <baseline_dir> <candidate_dir>
"""
import json, os, re, sys, urllib.request, urllib.error

SEED_ID = "c92ea179-dd5c-46ca-b7b5-b44a191b974c"
MISSING_ID = "00000000-0000-4000-8000-000000000001"
UUID_RE = re.compile(r"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
VOLATILE_KEYS = {"traceId", "instance", "timestamp"}


def call(base, method, path, body=None, content_type="application/json"):
    data = None if body is None else (body if isinstance(body, str) else json.dumps(body)).encode()
    req = urllib.request.Request(base + path, data=data, method=method)
    if data is not None or method in ("POST", "PUT"):
        req.add_header("Content-Type", content_type)
    req.add_header("Accept", "application/json, application/problem+json")
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, r.headers.get("Content-Type"), r.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("Content-Type"), e.read().decode()


def normalize(text, ids):
    if not text:
        return None
    try:
        obj = json.loads(text)
    except ValueError:
        return text

    def walk(o):
        if isinstance(o, dict):
            return {k: ("<masked>" if k in VOLATILE_KEYS else walk(v)) for k, v in sorted(o.items())}
        if isinstance(o, list):
            return [walk(v) for v in o]
        if isinstance(o, str) and o in ids:
            return ids[o]
        if isinstance(o, str) and o != SEED_ID and UUID_RE.fullmatch(o):
            return "<GENERATED_ID>"
        return o
    result = walk(obj)
    if isinstance(result, list):
        result.sort(key=lambda v: json.dumps(v, sort_keys=True))
    return result


def media_type(ct):
    return ct.split(";")[0].strip() if ct else None


def capture(base, out):
    os.makedirs(out, exist_ok=True)
    ids = {}
    valid = {"degree": "Master's degree", "fieldOfStudy": "Computer science", "school": "Parity university", "description": "Created by parity harness"}
    steps = []

    def step(name, method, path, body=None, content_type="application/json"):
        status, ct, text = call(base, method, path, body, content_type)
        rec = {"name": name, "request": f"{method} {path}", "status": status, "contentType": media_type(ct), "body": text}
        steps.append(rec)
        return rec

    step("01_get_all_seed", "GET", "/api/Educations")
    step("02_get_all_lowercase_route", "GET", "/api/educations")
    step("03_get_seed_by_id", "GET", f"/api/Educations/{SEED_ID}")
    step("04_get_missing_id", "GET", f"/api/Educations/{MISSING_ID}")
    step("05_get_non_guid_id", "GET", "/api/Educations/not-a-guid")
    created = step("06_post_valid", "POST", "/api/Educations", dict(valid, id=MISSING_ID))
    new_id = json.loads(created["body"])["id"] if created["status"] == 200 else "none"
    ids[new_id] = "<CREATED_ID>"
    step("07_get_created", "GET", f"/api/Educations/{new_id}")
    step("08_post_missing_degree", "POST", "/api/Educations", {k: v for k, v in valid.items() if k != "degree"})
    step("09_post_blank_degree", "POST", "/api/Educations", dict(valid, degree="   "))
    step("10_post_null_school", "POST", "/api/Educations", dict(valid, school=None))
    step("11_post_empty_body", "POST", "/api/Educations", "")
    step("12_post_malformed_json", "POST", "/api/Educations", "{not json")
    step("13_post_text_plain", "POST", "/api/Educations", "hello", "text/plain")
    step("14_post_degree_too_long", "POST", "/api/Educations", dict(valid, degree="x" * 51))
    updated = dict(valid, id=new_id, degree="PhD", description=None)
    step("15_put_valid", "PUT", f"/api/Educations/{new_id}", updated)
    step("16_get_after_update", "GET", f"/api/Educations/{new_id}")
    step("17_put_id_mismatch", "PUT", f"/api/Educations/{new_id}", dict(updated, id=SEED_ID))
    step("18_put_missing_body_id", "PUT", f"/api/Educations/{new_id}", {k: v for k, v in updated.items() if k != "id"})
    step("19_put_nonexistent", "PUT", f"/api/Educations/{MISSING_ID}", dict(updated, id=MISSING_ID))
    step("20_put_missing_field", "PUT", f"/api/Educations/{new_id}", {k: v for k, v in updated.items() if k != "school"})
    step("21_put_non_guid_id", "PUT", "/api/Educations/not-a-guid", updated)
    step("22_delete_created", "DELETE", f"/api/Educations/{new_id}")
    step("23_delete_again", "DELETE", f"/api/Educations/{new_id}")
    step("24_delete_non_guid_id", "DELETE", "/api/Educations/not-a-guid")
    step("25_get_all_final", "GET", "/api/Educations")

    for rec in steps:
        rec["body"] = normalize(rec["body"], ids)
        rec["request"] = rec["request"].replace(new_id, "<CREATED_ID>")
        with open(os.path.join(out, rec["name"] + ".json"), "w") as f:
            json.dump(rec, f, indent=2, sort_keys=True)
            f.write("\n")
    print(f"captured {len(steps)} requests into {out}")


def is_problem(rec):
    return rec.get("contentType") == "application/problem+json" and isinstance(rec.get("body"), dict)


def strip_errors(rec):
    body = dict(rec["body"])
    has_errors = "errors" in body
    body.pop("errors", None)
    return body, has_errors


def diff(a_dir, b_dir):
    names = sorted(n for n in os.listdir(a_dir) if n.endswith(".json"))
    mismatches = detail_only = 0
    print("| # | Request | Baseline | Candidate | Result |")
    print("|---|---|---|---|---|")
    for n in names:
        a = json.load(open(os.path.join(a_dir, n)))
        b_path = os.path.join(b_dir, n)
        b = json.load(open(b_path)) if os.path.exists(b_path) else {}
        fields = [k for k in ("status", "contentType", "body") if a.get(k) != b.get(k)]
        res = "match"
        if fields == ["body"] and is_problem(a) and is_problem(b) and strip_errors(a) == strip_errors(b):
            res = "match (problem `type`/`title`/`status` equal; `errors` wording is framework-specific)"
            detail_only += 1
        elif fields:
            mismatches += 1
            res = "MISMATCH: " + ", ".join(fields)
        print(f"| {n[:2]} | `{a['request']}` | {a['status']} {a.get('contentType') or ''} | {b.get('status')} {b.get('contentType') or ''} | {res} |")
    print(f"\n{len(names)} requests compared, {mismatches} mismatches, {detail_only} validation-message-only differences")
    return mismatches


if __name__ == "__main__":
    if sys.argv[1] == "capture":
        capture(sys.argv[2].rstrip("/"), sys.argv[3])
    else:
        sys.exit(1 if diff(sys.argv[2], sys.argv[3]) else 0)
