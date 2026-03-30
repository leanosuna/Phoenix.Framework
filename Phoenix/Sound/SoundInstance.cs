using Silk.NET.OpenAL;
using System.Numerics;

namespace Phoenix.Sound
{
    public sealed class SoundInstance : IDisposable
    {
        private readonly AL _al;
        internal uint SourceId { get; }
        private bool _disposed;

        internal SoundInstance(AL al, uint sourceId)
        {
            _al = al;
            SourceId = sourceId;
        }

        public void Play() => _al.SourcePlay(SourceId);
        public void Pause() => _al.SourcePause(SourceId);
        public void Stop() => _al.SourceStop(SourceId);

        public void SetVolume(float volume)
            => _al.SetSourceProperty(SourceId, SourceFloat.Gain, Math.Clamp(volume, 0f, 1f));

        public void SetPitch(float pitch)
            => _al.SetSourceProperty(SourceId, SourceFloat.Pitch, Math.Max(0.01f, pitch));

        public void SetPosition(Vector3 position)
            => _al.SetSourceProperty(SourceId, SourceVector3.Position, position.X, position.Y, position.Z);

        public void SetVelocity(Vector3 velocity)
            => _al.SetSourceProperty(SourceId, SourceVector3.Velocity, velocity.X, velocity.Y, velocity.Z);

        public bool IsPlaying
        {
            get
            {
                _al.GetSourceProperty(SourceId, GetSourceInteger.SourceState, out var state);
                
                return (SourceState)state == SourceState.Playing;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _al.SourceStop(SourceId);
            _al.DeleteSource(SourceId);
        }
    }
}
