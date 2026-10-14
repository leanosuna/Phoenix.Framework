# Audio

Namespace: `Phoenix.Framework.Sound`

Phoenix provides audio playback through OpenAL with support for 2D stereo sounds, 3D spatial positioning, Doppler velocity effects, and a pluggable decoder system.

---

## SoundManager (static API)

All audio operations use the static `SoundManager` class, initialized automatically during `PhoenixGame.InternalLoad()`.

```csharp
SoundManager.Initialize();  // Called automatically by PhoenixGame
SoundManager.Shutdown();    // Releases the OpenAL device context
```

### Loading sounds

```csharp
// Load a sound clip from disk
SoundClip clip = SoundManager.LoadSound("Sounds/explosion.wav");
```

### Playing 2D and 3D sounds

```csharp
// 2D sound (background music, UI clicks)
SoundInstance music = SoundManager.Play2D(clip, volume: 0.8f, pitch: 1.0f, loop: true);

// 3D spatialized sound (position in world space)
SoundInstance sfx = SoundManager.Play3D(
    clip,
    position: new Vector3(5, 1, 0),
    volume: 0.8f,
    pitch: 1.0f,
    loop: false
);
```

### Controlling playback

```csharp
sfx.Play();
sfx.Pause();
sfx.Stop();

sfx.SetVolume(0.5f);
sfx.SetPitch(1.2f);
sfx.SetPosition(new Vector3(10, 0, 0));
sfx.SetVelocity(new Vector3(0, 0, 0));

bool isPlaying = sfx.IsPlaying;
sfx.Dispose();
```

---

## 3D listener configuration

Update the audio listener position each frame so 3D audio reflects camera movement:

```csharp
protected override void Update(double deltaTime)
{
    // Match audio listener to camera position and orientation
    SoundManager.SetListenerPosition(Camera!.Position);
    SoundManager.SetListenerOrientation(Camera.Front, Camera.Up);
}
```

---

## Custom decoders

Implement `IAudioDecoder` to support custom audio formats:

```csharp
public class CustomOggDecoder : IAudioDecoder
{
    public bool CanDecode(string path)
    {
        return path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
    }

    public SoundData Decode(string path)
    {
        // Decode to raw PCM bytes
        return new SoundData(pcmBytes, BufferFormat.Stereo16, 44100);
    }
}

// Register with the manager
SoundManager.RegisterDecoder(new CustomOggDecoder());
```

Built-in decoders include `WavDecoder` supporting 8-bit and 16-bit mono and stereo PCM audio.
