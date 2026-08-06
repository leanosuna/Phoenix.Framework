---
name: phoenix-utilities
description: Math helpers, logging, and the in-game error window for games built on Phoenix.Framework. Use for vector/matrix/quaternion math, debug logging to LOG.txt, or surfacing runtime errors via ErrorListWindow.
license: MIT
compatibility: opencode, claude, agents
metadata:
  package: Phoenix.Framework
  version: "1.0.3"
---

# Phoenix.Framework — Utilities

## Math

- `MathHelper` (static) — constants `Pi`, `TwoPi`, `PiOver2`, `PiOver4`; `Lerp(start, end, amount)`, `WrapAngle(angle)`, `ToRad/ToDeg`, `RotationMxFromYawPitchRoll`, `RotationFromYawPitchRoll`, `ExtractYawPitchRoll`; matrix/vector ref extensions `Invert`, `InverseTranspose`, `Transpose`, `Normalize`; float-array converters `ToFloatArray`, `To2Df/To2Di`, `ToNum`.
- `MathEx` (static) — `Atan2`, `ComputeCosSin`, `ClosestPointOnSegment`, `PointToSegmentDistance`, `SegmentDistance`, `LineDistance`, `ClosestPointOnTriangle`.

## Logging

- `Log` (static, namespace `Phoenix`) — `Info/Warn/Error/Debug/Exception(string)`, `ClearLog()`; flags `Enabled`, `Verbose`, `ConsoleWrite`, `Date`, `Time`. Writes to `{BaseDirectory}/LOG.txt`. All methods take a single string (no format placeholders).
- `ErrorListWindow` (static) — `Add(message)` with caller-info attributes (auto-filled), `Show` (bool); deduplicates with a count; auto-expires after `showTimeSeconds`.

## Gotchas

- `Log` methods accept one plain string only.
- `ErrorListWindow.Add("...", showTimeSeconds: 5f)` uses the 5th positional/optional param; caller info is auto-filled by `[Caller*]` attributes.

## Details

See the website docs:
- Math Helpers: https://framework.nx.net.ar/math/math/
- Logging: https://framework.nx.net.ar/utilities/logging/
