using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace GbsPlayer
{
    public class CDynamicEffectInstance
    {
        struct AudioBlock
        {
            public AudioBuffer AudioBuffer;
            public byte[] ByteBuffer;
        }

        private object _voiceLock = new Object();

        private static ByteBufferPool _bufferPool = new ByteBufferPool();

        private Queue<AudioBlock> _queuedBlocks = new Queue<AudioBlock>();

        private SourceVoice _voice;
        private WaveFormat _format;

        public SoundState State = SoundState.Stopped;

        public CDynamicEffectInstance(int sampleRate)
        {
            var xaudio2 = new XAudio2();
            var masteringVoice = new MasteringVoice(xaudio2);

            _format = new WaveFormat(sampleRate, 1);
            _voice = new SourceVoice(xaudio2, _format, true);
            _voice.BufferEnd += OnBufferEnd;
        }

        public int GetPendingBufferCount()
        {
            lock (_voiceLock)
            {
                return _queuedBlocks.Count;
            }
        }

        public void Play()
        {
            lock (_voiceLock)
            {
                State = SoundState.Playing;
                _voice.Start();
            }
        }

        public void Pause()
        {
            lock (_voiceLock)
            {
                State = SoundState.Paused;
                _voice.Stop();
            }
        }

        public void Resume()
        {
            lock (_voiceLock)
            {
                State = SoundState.Playing;
                _voice.Start();
            }
        }

        public void Stop()
        {
            lock (_voiceLock)
            {
                State = SoundState.Stopped;

                _voice.Stop();
                // Dequeue all the submitted buffers
                _voice.FlushSourceBuffers();
            }
        }

        public void SetVolume(float volume)
        {
            lock (_voiceLock)
            {
                _voice.SetVolume(volume);
            }
        }

        public void SubmitBuffer(byte[] buffer, int offset, int count)
        {
            var audioBlock = new AudioBlock();

            audioBlock.ByteBuffer = _bufferPool.Get(count);

            // we need to copy so datastream does not pin the buffer that the user might modify later
            Buffer.BlockCopy(buffer, offset, audioBlock.ByteBuffer, 0, count);

            var stream = DataStream.Create(audioBlock.ByteBuffer, true, false, 0, true);
            audioBlock.AudioBuffer = new AudioBuffer(stream);
            audioBlock.AudioBuffer.AudioBytes = count;

            _queuedBlocks.Enqueue(audioBlock);

            lock (_voiceLock)
                _voice.SubmitSourceBuffer(audioBlock.AudioBuffer, null);
        }

        private void OnBufferEnd(IntPtr obj)
        {
            // Release the buffer
            if (_queuedBlocks.Count > 0)
            {
                var block = _queuedBlocks.Dequeue();
                block.AudioBuffer.Stream.Dispose();
                _bufferPool.Return(block.ByteBuffer);
            }
        }
    }
}
