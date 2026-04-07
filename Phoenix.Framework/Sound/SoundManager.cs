using Phoenix.Framework.Sound.Decoders;
using Silk.NET.OpenAL;
using System.Numerics;

namespace Phoenix.Framework.Sound
{
    
    public unsafe static class SoundManager
    {
        private static AL? _al;
        private static ALContext? _alc;
        private static Device* _device;
        private static Context* _context;

        private static readonly List<IAudioDecoder> _decoders = new();
        private static readonly List<SoundInstance> _activeInstances = new();

        public static void Initialize()
        {
            _al = AL.GetApi(true);
            _alc = ALContext.GetApi();
            
            _device = _alc.OpenDevice("");
            
            if (_device == null)
                throw new Exception("Failed to open OpenAL device.");

            _context = _alc.CreateContext(_device, null);
            if (_context == null)
                throw new Exception("Failed to create OpenAL context.");

            _alc.MakeContextCurrent(_context);

            _decoders.Clear();
            _decoders.Add(new WavDecoder());

            // Listener defaults
            _al.SetListenerProperty(ListenerVector3.Position, 0, 0, 0);
            _al.SetListenerProperty(ListenerVector3.Velocity, 0, 0, 0);
        }

        public static void Shutdown()
        {
            if (_al == null || _alc == null)
                return;

            foreach (var inst in _activeInstances.ToArray())
                inst.Dispose();

            _activeInstances.Clear();

            _alc.MakeContextCurrent(nint.Zero);

            if (_context != null)
                _alc.DestroyContext(_context);

            if (_device != null)
                _alc.CloseDevice(_device);

            _context = null;
            _device = null;

            _al.Dispose();
            _alc.Dispose();

            _al = null;
            _alc = null;
        }

        public static void RegisterDecoder(IAudioDecoder decoder)
        {
            _decoders.Add(decoder);
        }

        public static SoundClip LoadSound(string path)
        {
            EnsureInitialized();

            var decoder = _decoders.FirstOrDefault(d => d.CanDecode(path))
                ?? throw new NotSupportedException($"No decoder available for file: {path}");

            var data = decoder.Decode(path);

            uint buffer = _al!.GenBuffer();

            unsafe
            {
                fixed (byte* ptr = data.PCM)
                {
                    _al.BufferData(buffer, data.Format, ptr, data.PCM.Length, data.SampleRate);
                }
            }

            return new SoundClip(buffer);
        }

        public static SoundInstance Play2D(SoundClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
        {
            var inst = CreateInstance(clip, volume, pitch, loop, is3D: false);
            inst.Play();
            return inst;
        }

        public static SoundInstance Play3D(
            SoundClip clip,
            Vector3 position,
            float volume = 1f,
            float pitch = 1f,
            bool loop = false)
        {
            var inst = CreateInstance(clip, volume, pitch, loop, is3D: true);
            inst.SetPosition(position);
            inst.Play();
            return inst;
        }

        public static void SetListenerPosition(Vector3 position)
        {
            EnsureInitialized();
            _al!.SetListenerProperty(ListenerVector3.Position, position.X, position.Y, position.Z);
        }

        public static void SetListenerVelocity(Vector3 velocity)
        {
            EnsureInitialized();
            _al!.SetListenerProperty(ListenerVector3.Velocity, velocity.X, velocity.Y, velocity.Z);
        }

        public static void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            EnsureInitialized();

            float[] ori =
            {
            forward.X, forward.Y, forward.Z,
            up.X, up.Y, up.Z
        };

            unsafe
            {
                fixed (float* ptr = ori)
                {
                    _al!.SetListenerProperty(ListenerFloatArray.Orientation, ptr);
                }
            }
        }

        public static void Update()
        {
            EnsureInitialized();

            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                if (!_activeInstances[i].IsPlaying)
                {
                    _activeInstances[i].Dispose();
                    _activeInstances.RemoveAt(i);
                }
            }
        }

        private static SoundInstance CreateInstance(SoundClip clip, float volume, float pitch, bool loop, bool is3D)
        {
            EnsureInitialized();

            uint source = _al!.GenSource();

            _al.SetSourceProperty(source, SourceInteger.Buffer, (int)clip.BufferId);
            _al.SetSourceProperty(source, SourceFloat.Gain, Math.Clamp(volume, 0f, 1f));
            _al.SetSourceProperty(source, SourceFloat.Pitch, Math.Max(0.01f, pitch));
            _al.SetSourceProperty(source, SourceBoolean.Looping, loop);

            // 2D vs 3D
            if (!is3D)
            {
                // Relative to listener = "2D-ish"
                _al.SetSourceProperty(source, SourceBoolean.SourceRelative, true);
                _al.SetSourceProperty(source, SourceVector3.Position, 0f, 0f, 0f);
                _al.SetSourceProperty(source, SourceFloat.RolloffFactor, 0f);
            }
            else
            {
                _al.SetSourceProperty(source, SourceBoolean.SourceRelative, false);
                _al.SetSourceProperty(source, SourceFloat.ReferenceDistance, 1f);
                _al.SetSourceProperty(source, SourceFloat.MaxDistance, 100f);
                _al.SetSourceProperty(source, SourceFloat.RolloffFactor, 1f);
            }

            var instance = new SoundInstance(_al, source);
            _activeInstances.Add(instance);
            return instance;
        }

        private static void EnsureInitialized()
        {
            if (_al == null || _alc == null)
                throw new InvalidOperationException("SoundManager.Initialize() must be called first.");
        }
    }
}
