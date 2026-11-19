# API parity harness

`parity.py` replays the same 25-request sequence (happy paths, validation errors, malformed bodies,
wrong media type, DB constraint violation, non-GUID ids, update/delete of missing rows) against a running
instance of the Educations API and stores the normalized responses (status, media type, body with
generated ids and `traceId` masked) as one JSON file per request.

`baseline/` was captured from the original ASP.NET Core / .NET 7 application (built from the repository
`Dockerfile` at the last .NET commit) against PostgreSQL 16 seeded with `ef_initialize.sql`
(the SQL equivalent of the EF Core `Initialize` migration).

```bash
# capture
python3 parity/parity.py capture http://localhost:5000 parity/java21
# compare (exit code 1 on mismatch)
python3 parity/parity.py diff parity/baseline parity/java21
```

Each capture must start from a freshly seeded database (only the seed row present).
