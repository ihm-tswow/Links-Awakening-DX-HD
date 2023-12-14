using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class SheetAnimator
    {
        public List<SheetAnimation> Animations = new List<SheetAnimation>();

        public delegate void AnimationEvent();
        public AnimationEvent OnAnimationFinished;
        public AnimationEvent OnFrameChange;

        public SheetAnimation CurrentAnimation => Animations[_currentAnimation];
        public AFrame CurrentFrame => Animations[_currentAnimation].Frames[CurrentFrameIndex];

        public float SpeedMultiplier = 1;

        public int CurrentFrameIndex
        {
            get => _currentFrameIndex;
            private set
            {
                _currentFrameIndex = value;
                OnFrameChange?.Invoke();
            }
        }
        public bool IsPlaying;

        public double FrameCounter => _frameCounter;

        private double _frameCounter;

        private int _currentLoop;
        private int _currentAnimation;
        private int _currentFrameIndex;

        public void Update()
        {
            if (IsPlaying)
            {
                _frameCounter += Game1.DeltaTime * SpeedMultiplier;

                while (_frameCounter > Animations[_currentAnimation].Frames[CurrentFrameIndex].FrameTime)
                {
                    _frameCounter -= Animations[_currentAnimation].Frames[CurrentFrameIndex].FrameTime;

                    if (CurrentFrameIndex + 1 >= Animations[_currentAnimation].Frames.Length)
                    {
                        // stop playing
                        if (Animations[_currentAnimation].LoopCount >= 0 &&
                            Animations[_currentAnimation].LoopCount <= _currentLoop)
                        {
                            IsPlaying = false;
                            OnAnimationFinished?.Invoke();
                        }
                        else
                        {
                            // loop animation
                            CurrentFrameIndex = 0;
                            _currentLoop++;
                        }
                    }
                    else
                    {
                        CurrentFrameIndex++;
                    }
                }
            }

            // start the following animation
            if (!IsPlaying && Animations[_currentAnimation].NextAnimation != null)
                Play(Animations[_currentAnimation].NextAnimation);
        }

        public void ResetFrameCounter()
        {
            _frameCounter = 0;
        }

        public void SetFrame(int frame)
        {
            if (frame < Animations[_currentAnimation].Frames.Length)
                CurrentFrameIndex = frame;
        }

        public void SetTime(double time)
        {
            _frameCounter = time;
        }

        public int GetAnimationIndex(string animationId)
        {
            var index = -1;

            for (var i = 0; i < Animations.Count; i++)
                if (Animations[i].Id == animationId)
                {
                    index = i;
                    break;
                }

            return index;
        }

        public void Play(int animationId)
        {
            if ((_currentAnimation == animationId && IsPlaying) || animationId < 0)
                return;

            IsPlaying = true;
            _currentAnimation = animationId;
            CurrentFrameIndex = 0;
            _currentLoop = 0;
            _frameCounter = 0;
        }

        public void Play(string animationId)
        {
            Play(GetAnimationIndex(animationId));
        }

        public void Stop()
        {
            IsPlaying = false;
            CurrentFrameIndex = 0;
            _currentLoop = 0;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Continue()
        {
            IsPlaying = true;
        }
    }
}
