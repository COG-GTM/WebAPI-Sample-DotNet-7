#!/usr/bin/env python3
"""Capture golden snapshot HTTP responses for the 20 Education API flows.

Usage: capture_flows.py <base_url> <out_dir>

Writes one JSON file per flow into <out_dir>, each containing the request
(method, path, body), the response status code, and the response JSON body.
Dynamic GUIDs generated during the run are normalized to stable placeholders
so legacy vs modern captures can be diffed byte-for-byte.
"""
import json
import sys
import urllib.request
import urllib.error

SEED_ID = "c92ea179-dd5c-46ca-b7b5-b44a191b974c"  # seeded Education row id
UNKNOWN_ID = "00000000-0000-0000-0000-000000000001"  # valid guid, not in DB

# Placeholder map for normalizing server-generated ids in captured output
id_map = {}


def request(method, path, body=None, raw_body=None):
    """Send an HTTP request and return (status, parsed_json_or_text)."""
    url = BASE + path
    data = None
    headers = {}
    if raw_body is not None:
        data = raw_body.encode()
        headers["Content-Type"] = "application/json"
    elif body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req) as resp:
            status = resp.status
            text = resp.read().decode()
    except urllib.error.HTTPError as e:
        status = e.code
        text = e.read().decode()
    try:
        parsed = json.loads(text) if text else None
    except ValueError:
        parsed = text
    return status, parsed


def normalize(obj):
    """Replace dynamic GUIDs and volatile validation-error fields with stable values."""
    if isinstance(obj, dict):
        out = {}
        for k, v in sorted(obj.items()):
            if k == "traceId":  # ProblemDetails traceId differs per request
                out[k] = "<TRACE_ID>"
            else:
                out[k] = normalize(v)
        return out
    if isinstance(obj, list):
        return [normalize(x) for x in obj]
    if isinstance(obj, str):
        return id_map.get(obj, obj)
    return obj


results = []


def record(num, name, method, path, status, body, sent=None, note=None):
    """Append and persist one flow capture with normalized dynamic values."""
    entry = {
        "flow": num,
        "name": name,
        "request": {"method": method, "path": id_path(path), "body": sent},
        "status": status,
        "body": normalize(body),
    }
    if note:
        entry["note"] = note
    results.append(entry)


def id_path(path):
    """Normalize dynamic ids appearing in request paths."""
    for real, ph in id_map.items():
        path = path.replace(real, ph)
    return path


def valid_dto(**over):
    """A valid EducationDto payload; fields can be overridden per flow."""
    d = {
        "degree": "Master's degree",
        "fieldOfStudy": "Computer science",
        "school": "Parity university",
        "description": "Golden snapshot flow",
    }
    d.update(over)
    return d


def main():
    # 1 GET all with seed row
    s, b = request("GET", "/api/educations")
    record(1, "GET all with seed row", "GET", "/api/educations", s, b)

    # 2 GET valid seeded id
    s, b = request("GET", f"/api/educations/{SEED_ID}")
    record(2, "GET seeded id -> 200", "GET", f"/api/educations/{SEED_ID}", s, b)

    # 3 GET unknown id -> 204
    s, b = request("GET", f"/api/educations/{UNKNOWN_ID}")
    record(3, "GET unknown id -> 204", "GET", f"/api/educations/{UNKNOWN_ID}", s, b)

    # 4 GET malformed guid -> 404 (route constraint)
    s, b = request("GET", "/api/educations/not-a-guid")
    record(4, "GET malformed guid -> 404", "GET", "/api/educations/not-a-guid", s, b)

    # 5 POST valid -> 200 + dto
    dto = valid_dto()
    s, b = request("POST", "/api/educations", body=dto)
    created_id = b.get("id") if isinstance(b, dict) else None
    if created_id:
        id_map[created_id] = "<CREATED_ID_F5>"
    record(5, "POST valid -> 200 + dto", "POST", "/api/educations", s, b, sent=dto)

    # 6 POST null body -> 400
    s, b = request("POST", "/api/educations", raw_body="null")
    record(6, "POST null body -> 400", "POST", "/api/educations", s, b, sent=None)

    # 7-9 POST missing required fields -> 400
    for num, field in ((7, "degree"), (8, "fieldOfStudy"), (9, "school")):
        dto = valid_dto()
        del dto[field]
        s, b = request("POST", "/api/educations", body=dto)
        record(num, f"POST missing {field} -> 400", "POST", "/api/educations", s, b, sent=dto)

    # 10 POST Degree > 50 chars
    dto = valid_dto(degree="D" * 51)
    s, b = request("POST", "/api/educations", body=dto)
    record(10, "POST Degree > 50 chars", "POST", "/api/educations", s, b, sent=dto)

    # 11 POST FieldOfStudy/School > 250 chars
    dto = valid_dto(fieldOfStudy="F" * 251, school="S" * 251)
    s, b = request("POST", "/api/educations", body=dto)
    record(11, "POST FieldOfStudy/School > 250 chars", "POST", "/api/educations", s, b, sent=dto)

    # 12 POST Description > 1000 chars
    dto = valid_dto(description="X" * 1001)
    s, b = request("POST", "/api/educations", body=dto)
    record(12, "POST Description > 1000 chars", "POST", "/api/educations", s, b, sent=dto)

    # 13 POST null Description (allowed) -> 200
    dto = valid_dto(description=None)
    s, b = request("POST", "/api/educations", body=dto)
    f13_id = b.get("id") if isinstance(b, dict) else None
    if f13_id:
        id_map[f13_id] = "<CREATED_ID_F13>"
    record(13, "POST null Description -> 200", "POST", "/api/educations", s, b, sent=dto)

    # 14 read-back GET after create matches
    if created_id:
        s, b = request("GET", f"/api/educations/{created_id}")
        record(14, "GET after create matches", "GET", f"/api/educations/{created_id}", s, b)
    else:
        record(14, "GET after create matches", "GET", "/api/educations/<none>", 0, None,
               note="flow 5 did not return an id")

    # 15 PUT valid -> 200 then GET reflects change
    upd = valid_dto(degree="PhD", description="Updated by flow 15")
    upd["id"] = created_id or UNKNOWN_ID
    s, b = request("PUT", f"/api/educations/{created_id}", body=upd)
    s2, b2 = request("GET", f"/api/educations/{created_id}")
    record(15, "PUT valid -> 200; GET reflects change", "PUT",
           f"/api/educations/{created_id}", s,
           {"put_response": b, "get_after_put_status": s2, "get_after_put": normalize(b2)},
           sent={**upd, "id": id_map.get(upd["id"], upd["id"])})

    # 16 PUT unknown id -> 400
    upd = valid_dto()
    upd["id"] = UNKNOWN_ID
    s, b = request("PUT", f"/api/educations/{UNKNOWN_ID}", body=upd)
    record(16, "PUT unknown id -> 400", "PUT", f"/api/educations/{UNKNOWN_ID}", s, b, sent=upd)

    # 17 PUT null body -> 400
    s, b = request("PUT", f"/api/educations/{SEED_ID}", raw_body="null")
    record(17, "PUT null body -> 400", "PUT", f"/api/educations/{SEED_ID}", s, b, sent=None)

    # 18 DELETE valid -> 200 then GET -> 204
    s, b = request("DELETE", f"/api/educations/{created_id}")
    s2, b2 = request("GET", f"/api/educations/{created_id}")
    record(18, "DELETE valid -> 200; GET -> 204", "DELETE", f"/api/educations/{created_id}", s,
           {"delete_response": b, "get_after_delete_status": s2, "get_after_delete": normalize(b2)})

    # 19 DELETE unknown id -> 400
    s, b = request("DELETE", f"/api/educations/{UNKNOWN_ID}")
    record(19, "DELETE unknown id -> 400", "DELETE", f"/api/educations/{UNKNOWN_ID}", s, b)

    # 20 full create -> update -> delete lifecycle
    dto = valid_dto(degree="Lifecycle degree")
    s1, b1 = request("POST", "/api/educations", body=dto)
    lc_id = b1.get("id") if isinstance(b1, dict) else None
    if lc_id:
        id_map[lc_id] = "<CREATED_ID_F20>"
    upd = valid_dto(degree="Lifecycle updated")
    upd["id"] = lc_id or UNKNOWN_ID
    s2, b2 = request("PUT", f"/api/educations/{lc_id}", body=upd)
    s3, b3 = request("DELETE", f"/api/educations/{lc_id}")
    s4, b4 = request("GET", f"/api/educations/{lc_id}")
    record(20, "create -> update -> delete lifecycle", "MULTI", f"/api/educations/{lc_id}",
           s1, {
               "create_status": s1, "create": normalize(b1),
               "update_status": s2, "update": normalize(b2),
               "delete_status": s3, "delete": normalize(b3),
               "get_after_delete_status": s4, "get_after_delete": normalize(b4),
           }, sent=dto)


if __name__ == "__main__":
    BASE = sys.argv[1].rstrip("/")
    out_dir = sys.argv[2]
    import os
    os.makedirs(out_dir, exist_ok=True)
    main()
    # Write one normalized JSON file per flow for stable diffing
    for r in results:
        with open(os.path.join(out_dir, f"flow{r['flow']:02d}.json"), "w") as f:
            json.dump(r, f, indent=2, sort_keys=True)
            f.write("\n")
    print(f"Captured {len(results)} flows into {out_dir}")
