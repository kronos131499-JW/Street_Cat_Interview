# Workspace reliability rules

- Use the exact workspace root `D:\Street_Cat_Interview` as the working directory. Before a tool call that depends on another path, validate it with `Test-Path -LiteralPath`. If process creation reports Windows error 267, stop retrying the malformed path and return to the workspace root.
- Keep command output bounded and targeted. Exclude `Library`, `Temp`, `Logs`, `obj`, `bin`, and `node_modules` from broad searches unless the task specifically requires them. Never print binary, base64, or complete session JSONL content.
- Inspect at most two full-resolution images in one model round. Prefer normal/high detail first and request original detail only for pixel-level inspection. Summarize findings before loading another batch.
- For long changes, work in small verified milestones and preserve completed edits before starting the next batch. If a tool fails, diagnose that failure instead of repeating the same call unchanged.
- Prefer absolute Windows paths in shell commands. Preserve backslashes and the drive separator; do not construct paths such as `D:Street_Cat_Interview` or concatenate path segments without `Join-Path`.
