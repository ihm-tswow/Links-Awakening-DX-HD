using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class Animator
    {
        public List<Animation> Animations = new List<Animation>();

        public delegate void AnimationEvent();
        public AnimationEvent OnAnimationFinished;
        public AnimationEvent OnFrameChange;

        public Animation CurrentAnimation => Animations[_currentAnimation];
        public Frame CurrentFrame => Animations[_currentAnimation].Frames[CurrentFrameIndex];

        public Rectangle CollisionRectangle;
        public Texture2D SprTexture;

        public string SpritePath;
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

        public int FrameWidth => Animations[_currentAnimation].Frames[CurrentFrameIndex].SourceRectangle.Width;
        public int FrameHeight => Animations[_currentAnimation].Frames[CurrentFrameIndex].SourceRectangle.Height;

        private double _frameCounter;

        private int _currentLoop;
        private int _currentAnimation;
        private int _currentFrameIndex;
        private int _finishFrameIndex;

        public void Update()
        {
            var stoppedPlaying = false;

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
                            stoppedPlaying = true;
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

                    // stop the animation?
                    if (CurrentFrameIndex == _finishFrameIndex)
                        Stop();
                }
            }

            // start the following animation
            if (stoppedPlaying && !string.IsNullOrEmpty(Animations[_currentAnimation].NextAnimation))
                Play(Animations[_currentAnimation].NextAnimation);

            CollisionRectangle = GetCollisionBox(CurrentFrame);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            // needed so that the objects don't wiggle around while the camera is moving
            var normX = (int)Math.Round(position.X * MapManager.Camera.Scale) / MapManager.Camera.Scale;
            var normY = (int)Math.Round(position.Y * MapManager.Camera.Scale) / MapManager.Camera.Scale;

            spriteBatch.Draw(SprTexture, new Vector2(
                    normX + (CurrentAnimation.Offset.X + CurrentFrame.Offset.X),
                    normY + (CurrentAnimation.Offset.Y + CurrentFrame.Offset.Y)), CurrentFrame.SourceRectangle,
                color, 0, Vector2.Zero, Vector2.One,
                (CurrentFrame.MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                (CurrentFrame.MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 0);

            // draw the collision rectangle
            if (Game1.DebugMode)
            {
                spriteBatch.Draw(Resources.SprWhite, new Vector2(
                    position.X + (CurrentAnimation.Offset.X + CurrentFrame.Offset.X + CurrentFrame.CollisionRectangle.X),
                    position.Y + (CurrentAnimation.Offset.Y + CurrentFrame.Offset.Y + CurrentFrame.CollisionRectangle.Y)),
                    new Rectangle(0, 0, CurrentFrame.CollisionRectangle.Width, CurrentFrame.CollisionRectangle.Height), Color.Green * 0.5f);
            }
        }

        public void DrawBasic(SpriteBatch spriteBatch, Vector2 position, Color color, float scale = 1)
        {
            spriteBatch.Draw(SprTexture, new Vector2(
                    position.X + (CurrentAnimation.Offset.X + CurrentFrame.Offset.X) * scale,
                    position.Y + (CurrentAnimation.Offset.Y + CurrentFrame.Offset.Y) * scale), CurrentFrame.SourceRectangle,
                color, 0, Vector2.Zero, new Vector2(scale),
                (CurrentFrame.MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                (CurrentFrame.MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 0);
        }

        //public void DrawShadow(SpriteBatch spriteBatch)
        //{
        //    if (!IsVisible || !ShadowVisible)
        //        return;

        //    var normX = (int)Math.Round(PosX * (int)MapManager.Camera.Scale, 0, MidpointRounding.AwayFromZero) / MapManager.Camera.Scale;
        //    var normY = (int)Math.Round(PosY * (int)MapManager.Camera.Scale, 0, MidpointRounding.AwayFromZero) / MapManager.Camera.Scale;

        //    DrawHelper.DrawShadow(SprTexture, new Vector2(
        //            normX + (CurrentAnimation.Offset.X + CurrentFrame.Offset.X),
        //            normY + (CurrentAnimation.Offset.Y + CurrentFrame.Offset.Y)), CurrentFrame.SourceRectangle,
        //          CurrentFrame.MirroredH, Color.White);

        //    //spriteBatch.Draw(SprAnimator, new Vector2(
        //    //        drawPosX + (CurrentAnimation.Offset.X + CurrentFrame.Offset.X),
        //    //        drawPosY + (CurrentAnimation.Offset.Y + CurrentFrame.Offset.Y)),
        //    //        new Rectangle(-SprAnimator.Width, 0, SprAnimator.Width * 3, SprAnimator.Height * 2), Color.Black * 0.4f);
        //}

        public void ResetFrameCounter()
        {
            _frameCounter = 0;
        }

        public void SetFrame(int frame)
        {
            if (frame < Animations[_currentAnimation].Frames.Length)
                CurrentFrameIndex = frame;
        }

        public int GetAnimationTime(int from, int to)
        {
            var time = 0;
            for (var i = from; i < to; i++)
                time += Animations[_currentAnimation].Frames[i].FrameTime;

            return time;
        }

        public int GetAnimationTime()
        {
            return GetAnimationTime(0, CurrentAnimation.Frames.Length);
        }

        public void SetTime(double time)
        {
            _frameCounter = time;
        }

        public bool HasAnimation(string animationId)
        {
            return GetAnimationIndex(animationId) != -1;
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
            _finishFrameIndex = -1;
            _currentAnimation = animationId;
            CurrentFrameIndex = 0;
            _currentLoop = 0;
            _frameCounter = 0;

            CollisionRectangle = GetCollisionBox(CurrentFrame);
        }

        public void Play(string animationId)
        {
            Play(GetAnimationIndex(animationId));
        }

        public void Play(string animationId, int frame, double time)
        {
            Play(GetAnimationIndex(animationId));
            SetFrame(frame);
            SetTime(time);
        }

        public void FinishAnimation(int finishFrameIndex)
        {
            if (_currentFrameIndex == finishFrameIndex)
                Stop();
            else
                _finishFrameIndex = finishFrameIndex;
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

        public void AddAnimation(Animation addAnimation)
        {
            Animations.Add(addAnimation);
        }

        public void AddFrame(int animationIndex, int frameIndex, Frame newFrame)
        {
            var newFrames = Animations[animationIndex].Frames.ToList();
            newFrames.Insert(frameIndex, newFrame);

            Animations[animationIndex].Frames = newFrames.ToArray();

            UpdateAnimationSize(animationIndex, newFrame);
        }

        public void SetFrameAt(string animationId, int frameIndex, Frame newFrame)
        {
            SetFrameAt(GetAnimationIndex(animationId), frameIndex, newFrame);
        }

        public void SetFrameAt(int animationIndex, int frameIndex, Frame newFrame)
        {
            Animations[animationIndex].Frames[frameIndex] = newFrame;
            UpdateAnimationSize(animationIndex, newFrame);
        }

        public void UpdateAnimationSize(int animationIndex, Frame newFrame)
        {
            if (Animations[animationIndex].AnimationLeft > newFrame.Offset.X)
                Animations[animationIndex].AnimationLeft = newFrame.Offset.X;
            if (Animations[animationIndex].AnimationTop > newFrame.Offset.Y)
                Animations[animationIndex].AnimationTop = newFrame.Offset.Y;

            if (Animations[animationIndex].AnimationRight < newFrame.Offset.X + newFrame.SourceRectangle.Width)
                Animations[animationIndex].AnimationRight = newFrame.Offset.X + newFrame.SourceRectangle.Width;
            if (Animations[animationIndex].AnimationBottom < newFrame.Offset.Y + newFrame.SourceRectangle.Height)
                Animations[animationIndex].AnimationBottom = newFrame.Offset.Y + newFrame.SourceRectangle.Height;

            Animations[animationIndex].AnimationWidth = Animations[animationIndex].AnimationRight - Animations[animationIndex].AnimationLeft;
            Animations[animationIndex].AnimationHeight = Animations[animationIndex].AnimationBottom - Animations[animationIndex].AnimationTop;
        }

        public void RecalculateAnimationSize(int animationIndex)
        {
            // update the size of the animation
            Animations[animationIndex].AnimationLeft = 0;
            Animations[animationIndex].AnimationRight = 0;
            Animations[animationIndex].AnimationTop = 0;
            Animations[animationIndex].AnimationBottom = 0;

            foreach (var frame in Animations[animationIndex].Frames)
                UpdateAnimationSize(animationIndex, frame);
        }

        public void SetAnimationFps(int animationId, int fps)
        {
            for (var j = 0; j < Animations[animationId].Frames.Length; j++)
                Animations[animationId].Frames[j].FrameTimeFps = fps;
        }

        public void SetFrameFps(int animationId, int frame, int fps)
        {
            if (0 <= frame && frame < Animations[animationId].Frames.Length)
                Animations[animationId].Frames[frame].FrameTimeFps = fps;
        }

        public Rectangle GetCollisionBox(Frame frame)
        {
            if (frame.CollisionRectangle.Width > 0 && frame.CollisionRectangle.Height > 0)
            {
                var collisionRectangle = new Rectangle(
                    CurrentAnimation.Offset.X + frame.Offset.X,
                    CurrentAnimation.Offset.Y + frame.Offset.Y,
                    frame.CollisionRectangle.Width, frame.CollisionRectangle.Height);

                if (!frame.MirroredH)
                    collisionRectangle.X += frame.CollisionRectangle.X;
                else
                    collisionRectangle.X += frame.SourceRectangle.Width - frame.CollisionRectangle.Width - frame.CollisionRectangle.X;

                if (!frame.MirroredV)
                    collisionRectangle.Y += frame.CollisionRectangle.Y;
                else
                    collisionRectangle.Y += frame.SourceRectangle.Height - frame.CollisionRectangle.Height - frame.CollisionRectangle.Y;

                return collisionRectangle;
            }

            return Rectangle.Empty;
        }
    }
}
