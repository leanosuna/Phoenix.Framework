# Audio

Phoenix provides audio playback via OpenAL with a pluggable decoder system.

## SoundManager (Static API)

All audio operations use the static `SoundManager` class. It is initialized automatically by `PhoenixGame` during startup.

### Loading Sounds

```csharp
// Load a sound clip from disk
var clip = SoundManager.LoadSound("sounds/explosion.wav");

// Load with custom decoder support
var clip = SoundManager.LoadSound("sounds/music.ogg");  // Requires OGG decoder
```

### Playing Sounds

```csharp
// 2D sound (no spatial positioning)
var instance = SoundManager.Play2D(clip);

// 2D with parameters
var instance = SoundManager.Play2D(clip, volume: 0.8f, pitch: 1.0f, loop: false);

// 3D sound (spatialized in world)
var instance = SoundManager.Play3D(clip, new Vector3(5, 1, 0), volume: 0.8f);
```

### Playing 3D Sound with Full Control

```csharp
var instance = SoundManager.Play3D(
    clip,
    position: new Vector3(0, 2, -5),
    volume: 0.7f,
    pitch: 1.2f,
    loop: false
);
```

### Controlling Playback

```csharp
// Start/stop/pause
instance.Play();
instance.Pause();
instance.Stop();

// Adjust properties
instance.SetVolume(0.5f);
instance.SetPitch(1.5f);
instance.SetPosition(new Vector3(1, 2, 3));
instance.SetVelocity(new Vector3(0, 0, 0));

// Check state
bool isPlaying = instance.IsPlaying;

// Clean up when done
instance.Dispose();
```

### Listener Configuration

Position the listener (camera) for 3D audio:

```csharp
// Position
SoundManager.SetListenerPosition(Camera.Position);

// Velocity (for Doppler effect)
SoundManager.SetListenerVelocity(CameraVelocity);

// Orientation (forward + up)
SoundManager.SetListenerOrientation(Camera.Front, Camera.Up);
```

### Automatic Cleanup

`SoundManager.Update()` is called automatically by `PhoenixGame` each frame. It disposes finished instances and cleans up the active list.


## Custom Decoders

Implement `IAudioDecoder` to support additional audio formats:

```csharp
public class OggDecoder : IAudioDecoder
{
    public bool CanDecode(string path)
    {
        return path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
    }

    public SoundData Decode(string path)
    {
        // Decode OGG to PCM using a library like StbVorbis
        // Return PCM bytes, format, and sample rate
        return new SoundData(pcmBytes, BufferFormat.Stereo16, 44100);
    }
}
```

### Registering a Decoder

```csharp
SoundManager.RegisterDecoder(new OggDecoder());
```

## SoundData Structure

```csharp
public class SoundData
{
    public byte[] PCM { get; }       // Raw PCM audio data
    public BufferFormat Format { get; }  // Audio format
    public int SampleRate { get; }   // Samples per second
}

// BufferFormat values:
// Mono8, Mono16, Stereo8, Stereo16
```

## SoundClip

An internal audio buffer created from decoded PCM data. Not directly constructable — use `SoundManager.LoadSound()` or register a decoder.

```csharp
public class SoundClip
{
    internal uint BufferId { get; }  // OpenAL buffer handle
}
```

## SoundInstance

A playing audio instance tied to a `SoundClip`.

```csharp
public class SoundInstance : IDisposable
{
    void Play();
    void Pause();
    void Stop();
    void SetVolume(float volume);
    void SetPitch(float pitch);
    void SetPosition(Vector3 position);      // For 3D sounds
    void SetVelocity(Vector3 velocity);      // For 3D Doppler
    bool IsPlaying { get; }
    void Dispose();
}
```

## Built-in Decoder: WAV

The `WavDecoder` supports:
- **8-bit and 16-bit** PCM
- **Mono and Stereo**
- **Any sample rate**

Does NOT support:
- Compressed formats (MP3, OGG, etc.)
- Floating-point PCM
- Multi-channel (surround) audio

## Complete Example

```csharp
public class MyGame : PhoenixGame
{
    private SoundClip _explosionSound;
    private List<SoundInstance> _activeSounds = new();

    protected override void Initialize()
    {
        _explosionSound = SoundManager.LoadSound("sounds/explosion.wav");
    }

    protected override void Update(double dt)
    {
        // Play explosion at cursor position
        if (InputManager.KeyDownOnce(Key.Space))
        {
            var instance = SoundManager.Play3D(
                _explosionSound,
                Camera.Position + Camera.Front * 5f,
                volume: 0.8f,
                pitch: 1f
            );
            _activeSounds.Add(instance);
        }

        // Clean up finished sounds
        _activeSounds.RemoveAll(s => !s.IsPlaying);
        foreach (var s in _activeSounds)
            s.Dispose();
        _activeSounds.Clear();

        // Update listener position
        SoundManager.SetListenerPosition(Camera.Position);
        SoundManager.SetListenerOrientation(Camera.Front, Camera.Up);
    }
}
```

## See Also

- [AssetLoader](../asset-pipeline/loading.md) — loading sound files from asset manifest
- [ErrorListWindow](../utilities/logging.md) — audio errors are reported here
