# Figma handoff and bridge

The current design file is **走调长镜头 · 美术方向探索 01**.

- File key: `LOMa9NEcogELqstS7Fsol0`
- Open: <https://www.figma.com/design/LOMa9NEcogELqstS7Fsol0/>
- Local handoff snapshots: `design/d22-layout-v12`, `design/d22-layout-v16`,
  `design/d22-v11` and `design/d22-v12`

The Figma Console MCP and its desktop bridge run per developer machine. The
bridge process, browser session and access token are intentionally not stored
in Git. Install the MCP server in the local Codex/MCP configuration, start the
Figma desktop bridge, then use the file key above to reconnect. Exported
layouts, publication JSON and scripts belong in `design/` so a collaborator
can review them without a live bridge.

The v12 publication record says the bridge was unavailable at that time; the
local SVG/JSON handoff remains the reproducible fallback. Never commit a Figma
token, `.env` file or desktop session state.

Setup instructions and version-pinned config: [MCP_SETUP](../../docs/MCP_SETUP.md).
The bridge was actively probed successfully on 2026-10-03; the design file key above responded.

Figma file access is managed by its own sharing settings; cloning Git does not grant a collaborator permission to that cloud document.
