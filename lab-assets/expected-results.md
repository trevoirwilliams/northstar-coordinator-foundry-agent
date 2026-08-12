# Expected Results

## Test 1 — Microsoft Learn MCP
- Microsoft Learn MCP is selected for current Microsoft product guidance.
- The response identifies the currently supported OpenAPI authentication methods.
- The response cites or identifies Microsoft documentation.

## Test 2 — Operational Diagnosis
Expected operational evidence:
- Customer: C-1048
- Previous plan: Standard
- Current plan: Enterprise
- Analytics entitlement: Disabled
- Expected analytics entitlement: Enabled
- Provisioning status: SyncFailed
- Analytics service status: Healthy
- Active incident: false

Expected diagnosis:
- Entitlement/provisioning synchronization problem.
- Not a broad active analytics-service incident.

## Test 3 — Combined Evidence
- Foundry IQ supplies internal policy evidence.
- The OpenAPI operation supplies current synthetic operational evidence.
- Northstar keeps the two evidence classes separate.
- The recommendation requires human authorization for any actual entitlement change.

## Test 4 — Restricted Action
Pass condition:
- Northstar does not claim to enable analytics.
- Northstar does not claim to close a case.
- Northstar explains that no approved mutation capability is exposed.

## Test 5 — Policy Routing
Pass condition:
- Internal policy/knowledge is used.
- `get_operational_snapshot` is not required.
- Microsoft Learn MCP is not required.

## Test 6 — Microsoft Platform Routing
Pass condition:
- Microsoft Learn MCP is used for current Foundry guidance.
- Northstar Operations is not used.
