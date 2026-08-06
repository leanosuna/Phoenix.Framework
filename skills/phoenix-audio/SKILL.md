---
name: phoenix-audio
description: Sound playback, decoding, and 3D audio for games built on Phoenix.Framework. Use for loading/playing sounds, managing instances, listener setup, or registering custom decoders.
license: MIT
compatibility: opencode, claude, agents
metadata:
  package: Phoenix.Framework
  version: "1.0.3"
---

# Phoenix.Framework — Audio

OpenAL-based audio via the static `SoundManager` (auto-initialized by `PhoenixGame`).

## Key types

- `SoundManager` (static) — `LoadSound(path)` → `SoundClip`; `Play2D(clip, volume=1, pitch=1, loop=false)` → `SoundInstance`; `Play3D(clip, position, ...)`; `SetListenerPosition/Velocity/Orientation`; `RegisterDecoder(IAudioDecoder)`; `Initialize()/Shutdown()`.
- `SoundInstance` — `Play/Pause/Stop`, `SetVolume/SetPitch/SetPosition/SetVelocity`, `IsPlaying`, `Dispose`.
- `SoundClip` — opaque handle from `LoadSound`.
- `SoundData` — `PCM`, `Format` (`BufferFormat`), `SampleRate`.
- `IAudioDecoder` / `WavDecoder` — only WAV (8/16-bit PCM, mono/stereo) is built in; OGG/MP3 need a custom decoder.

## Gotchas

- `Play2D(..., loop: false)` is the default and does not loop — no special handling needed.
- Playing before `Initialize()` throws `InvalidOperationException` (safe).
- `WavDecoder` only supports PCM WAV; other formats must be decoded by a registered decoder first.

## Details

See the website docs: https://framework.nx.net.ar/audio/sound/
