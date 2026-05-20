using Phoenix.Framework.Rendering.Textures;
using Phoenix.Framework.Sound;
using Silk.NET.OpenAL;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Phoenix.Framework.Video
{
    public class VideoPlayer : IDisposable
    {
        private readonly GL _gl;
        private readonly VideoData _videoData;
        private GLTexture _texture = null!;
        private SoundInstance? _audioInstance;
        private SoundClip? _audioClip;
        private bool _disposed;

        private int _currentFrame = -1;
        private float _currentTime;
        private bool _isPlaying;
        private float _volume = 1f;

        public uint TextureHandle => _texture.Handle;
        public GLTexture Texture => _texture;
        public int Width => _videoData.Width;
        public int Height => _videoData.Height;
        public float Duration => _videoData.Duration;
        public float CurrentTime => _currentTime;
        public int CurrentFrame => _currentFrame;
        public bool IsPlaying => _isPlaying;
        public bool Loop { get; set; }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                _audioInstance?.SetVolume(_volume);
            }
        }

        public bool HasAudio => _videoData.HasAudio;

        public VideoPlayer(GL gl, VideoData videoData)
        {
            _gl = gl;
            _videoData = videoData;
            InitializeTexture();

            if (_videoData.HasAudio && _videoData.Audio != null)
            {
                _audioClip = SoundManager.LoadSoundClip(_videoData.Audio);
            }
        }

        private void InitializeTexture()
        {
            var format = _videoData.CompressionFormat;
            var mipCount = _videoData.MipCount;
            var mipSizes = _videoData.GenerateMipMaps && _videoData.MipSizes != null
                ? _videoData.MipSizes[0]
                : new[] { new Vector2(_videoData.Width, _videoData.Height) };

            byte[] firstFrameData;
            if (_videoData.IsStreaming)
            {
                firstFrameData = LoadFrameData(0);
            }
            else
            {
                firstFrameData = _videoData.Frames[0];
            }

            if (_videoData.GenerateMipMaps)
            {
                var encodedBytes = new byte[mipCount][];
                int offset = 0;
                for (int m = 0; m < mipCount; m++)
                {
                    var mipLen = _videoData.MipLengths![0][m];
                    encodedBytes[m] = new byte[mipLen];
                    Array.Copy(firstFrameData, offset, encodedBytes[m], 0, mipLen);
                    offset += mipLen;
                }

                _texture = new GLTexture(_gl, "video_frame", 
                    (int)TextureWrapMode.Repeat, (int)TextureWrapMode.Repeat,
                    (int)TextureMinFilter.LinearMipmapLinear, (int)TextureMagFilter.Linear,
                    format, mipCount, mipSizes, encodedBytes);
            }
            else
            {
                _texture = new GLTexture(_gl, "video_frame",
                    (int)TextureWrapMode.ClampToEdge, (int)TextureWrapMode.ClampToEdge,
                    (int)TextureMinFilter.Linear, (int)TextureMagFilter.Linear,
                    format, 1, mipSizes, new[] { firstFrameData });
            }

            _currentFrame = 0;
        }

        private byte[] LoadFrameData(int frameIndex)
        {
            if (!_videoData.IsStreaming)
                return _videoData.Frames[frameIndex];

            using var fs = File.OpenRead(_videoData.FilePath!);
            using var br = new BinaryReader(fs);

            fs.Position = _videoData.FrameOffsets[frameIndex];
            return br.ReadBytes(_videoData.FrameLengths[frameIndex]);
        }

        public void Play()
        {
            if (_isPlaying) return;
            _isPlaying = true;

            if (_audioClip != null)
            {
                _audioInstance = SoundManager.Play2D(_audioClip, _volume, loop: Loop);
            }
        }

        public void Pause()
        {
            if (!_isPlaying) return;
            _isPlaying = false;
            _audioInstance?.Pause();
        }

        public void Stop()
        {
            _isPlaying = false;
            _currentTime = 0;
            _currentFrame = -1;
            _audioInstance?.Stop();
            _audioInstance?.Dispose();
            _audioInstance = null;

            if (_videoData.IsStreaming)
            {
                var firstFrame = LoadFrameData(0);
                UpdateTextureData(firstFrame);
            }
            else
            {
                UpdateTextureData(_videoData.Frames[0]);
            }
            _currentFrame = 0;
        }

        public void Seek(float time)
        {
            _currentTime = Math.Clamp(time, 0f, _videoData.Duration);
            var targetFrame = (int)(_currentTime * _videoData.FrameRate);
            targetFrame = Math.Clamp(targetFrame, 0, _videoData.FrameCount - 1);

            if (targetFrame != _currentFrame)
            {
                _currentFrame = targetFrame;
                byte[] frameData;

                if (_videoData.IsStreaming)
                {
                    frameData = LoadFrameData(_currentFrame);
                }
                else
                {
                    frameData = _videoData.Frames[_currentFrame];
                }

                UpdateTextureData(frameData);
            }

            if (_audioInstance != null)
            {
                _audioInstance.Stop();
                _audioInstance.Dispose();
                _audioInstance = null;

                if (_isPlaying && _audioClip != null)
                {
                    _audioInstance = SoundManager.Play2D(_audioClip, _volume, loop: Loop);
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isPlaying) return;

            _currentTime += deltaTime;

            if (_currentTime >= _videoData.Duration)
            {
                if (Loop)
                {
                    _currentTime = 0f;
                    _currentFrame = -1;
                    if (_audioInstance != null)
                    {
                        _audioInstance.Stop();
                        _audioInstance.Dispose();
                        _audioInstance = null;
                        if (_audioClip != null)
                            _audioInstance = SoundManager.Play2D(_audioClip, _volume, loop: true);
                    }
                }
                else
                {
                    Stop();
                    return;
                }
            }

            var targetFrame = (int)(_currentTime * _videoData.FrameRate);
            targetFrame = Math.Clamp(targetFrame, 0, _videoData.FrameCount - 1);

            if (targetFrame != _currentFrame)
            {
                _currentFrame = targetFrame;
                byte[] frameData;

                if (_videoData.IsStreaming)
                {
                    frameData = LoadFrameData(_currentFrame);
                }
                else
                {
                    frameData = _videoData.Frames[_currentFrame];
                }

                UpdateTextureData(frameData);
            }
        }

        private void UpdateTextureData(byte[] frameData)
        {
            _texture.Bind();

            var format = _videoData.CompressionFormat;
            int internalFormat = format switch
            {
                0 => 0x8058,
                1 => 0x83F1,
                2 => 0x83F3,
                3 => 0x8DBD,
                _ => throw new Exception("Unknown compression format")
            };

            if (_videoData.GenerateMipMaps && _videoData.MipLengths != null)
            {
                int offset = 0;
                var mipCount = _videoData.MipCount;
                for (int m = 0; m < mipCount; m++)
                {
                    var mipLen = _videoData.MipLengths[0][m];
                    var mipW = (uint)_videoData.MipSizes![0][m].X;
                    var mipH = (uint)_videoData.MipSizes[0][m].Y;

                    unsafe
                    {
                        fixed (byte* ptr = &frameData[offset])
                        {
                            _gl.CompressedTexSubImage2D(
                                TextureTarget.Texture2D,
                                m,
                                0, 0,
                                mipW, mipH,
                                (InternalFormat)internalFormat,
                                (uint)mipLen,
                                ptr
                            );
                        }
                    }
                    offset += mipLen;
                }
            }
            else
            {
                unsafe
                {
                    fixed (byte* ptr = frameData)
                    {
                        _gl.CompressedTexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            0, 0,
                            (uint)_videoData.Width,
                            (uint)_videoData.Height,
                            (InternalFormat)internalFormat,
                            (uint)frameData.Length,
                            ptr
                        );
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();
            _audioClip = null;
            _texture.Dispose();
        }
    }
}
