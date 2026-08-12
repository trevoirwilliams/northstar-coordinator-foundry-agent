# Northstar Operations Lab Assets

This project contains the static operational endpoint and Foundry lab assets for
**Lab: Connect Approved MCP and Operational Tools to Northstar**.

## Project layout

```text
northstar-ops-lab/
├── docs/
│   ├── .nojekyll
│   ├── index.html
│   └── operational-snapshot.json
└── lab-assets/
    ├── northstar-operations.openapi.json
    ├── northstar-tool-routing.txt
    ├── lab-test-prompts.txt
    ├── expected-results.md
    └── publish-checklist.txt
```

## Publish the static endpoint

Create the repository `northstar-ops-lab` under the GitHub account `trevoirwilliams`,
commit this project, then enable GitHub Pages from the `main` branch and `/docs`
folder.

Expected site:

`https://trevoirwilliams.github.io/northstar-ops-lab/`

Expected operational endpoint:

`https://trevoirwilliams.github.io/northstar-ops-lab/operational-snapshot.json`

## Foundry OpenAPI tool

Use:

`lab-assets/northstar-operations.openapi.json`

The specification points to the GitHub Pages endpoint above and exposes one
read-only operation:

`get_operational_snapshot`

## Microsoft Learn MCP

Configure separately in Foundry:

- Name: `microsoft-learn`
- Endpoint: `https://learn.microsoft.com/api/mcp`
- Authentication: `Unauthenticated`

## Important

All data in `operational-snapshot.json` is synthetic training data.
The API exposes no write, update, delete, or case-management operations.
