using System;
using System.IO;
using System.Threading;

namespace GbsPlayer
{
    public class GbsPlayer
    {
        public GameBoyCPU Cpu;
        public Cartridge Cartridge;
        public GeneralMemory Memory;
        public Sound SoundGenerator;

        public byte CurrentTrack;

        public bool GbsLoaded;

        private float _volume = 1;
        private float _volumeMultiplier = 1.0f;

        private readonly object _updateLock = new object();
        private bool _exitThread;

        private Thread _updateThread;

        public GbsPlayer()
        {
            SoundGenerator = new Sound();
            Cartridge = new Cartridge();
            Memory = new GeneralMemory(Cartridge, SoundGenerator);
            Cpu = new GameBoyCPU(Memory, Cartridge, SoundGenerator);
        }

        public void OnExit()
        {
            _exitThread = true;
        }

        public void LoadFile(string path)
        {
            Cartridge.ROM = File.ReadAllBytes(path);

            Cartridge.Init();
            Cpu.Init();

            GbsLoaded = true;

            Console.WriteLine("finished loading file: {0}", path);
        }

        public void ChangeTrack(int offset)
        {
            var newTrack = CurrentTrack + offset;

            while (newTrack < 0)
                newTrack += Cartridge.TrackCount;
            newTrack %= Cartridge.TrackCount;

            StartTrack((byte)newTrack);
        }

        public void StartTrack(byte trackNr)
        {
            // directly init the new song if update is not called at this time
            lock (_updateLock)
            {
                CurrentTrack = trackNr;

                // clear buffer; stop playback
                SoundGenerator.Stop();

                // init play
                GbsInit(trackNr);

                SoundGenerator.SetStopTime(0);
            }
        }

        private void GbsInit(byte trackNumber)
        {
            Cartridge.Init();
            Cpu.SkipBootROM();
            Cpu.Init();
            Cpu.SetPlaybackSpeed(1);

            // tack number
            Cpu.reg_A = trackNumber;

            Cpu.reg_PC = Cartridge.InitAddress;
            Cpu.reg_SP = Cartridge.StackPointer;

            // push the idleAddress on the stack
            Memory[--Cpu.reg_SP] = (byte)(Cpu.IdleAddress >> 0x8);
            Memory[--Cpu.reg_SP] = (byte)(Cpu.IdleAddress & 0xFF);

            Console.WriteLine("finished gbs init");
        }

        public void Play()
        {
            Cpu.IsRunning = true;
        }

        public void Pause()
        {
            SoundGenerator.Pause();
            Cpu.IsRunning = false;
        }

        public void Resume()
        {
            SoundGenerator.Resume();
            Cpu.IsRunning = true;
        }

        public void Stop()
        {
            // stop music playback
            SoundGenerator.Stop();
            Cpu.IsRunning = false;
        }

        public float GetVolume()
        {
            return _volume;
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
            SoundGenerator.SetVolume(_volume * _volumeMultiplier);
        }

        public float GetVolumeMultiplier()
        {
            return _volumeMultiplier;
        }

        public void SetVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            SoundGenerator.SetVolume(_volume * _volumeMultiplier);
        }

        public void Update(float deltaTime)
        {
            Cpu.Update();
        }

        public void StartThread()
        {
            // thread is used to run the cpu and playback
            _updateThread = new Thread(UpdateThread);
            _updateThread.Start();
        }

        public void UpdateThread()
        {
            while (true)
            {
                if (_exitThread)
                    return;

                lock (_updateLock)
                    Cpu.Update();

                Thread.Sleep(5);
            }
        }
    }
}
