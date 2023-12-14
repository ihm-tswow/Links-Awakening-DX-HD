using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class Animation
    {
        public Frame[] Frames = new Frame[0];
        public Point Offset;

        public string Id;
        public string NextAnimation;

        public int AnimationLeft = 0;
        public int AnimationRight = 0;

        public int AnimationTop = 0;
        public int AnimationBottom = 0;

        public int AnimationWidth = 0;
        public int AnimationHeight = 0;

        public int LoopCount;

        public Animation(string id)
        {
            Id = id.ToLower();
        }
    }

    public class Frame
    {
        public Rectangle SourceRectangle;
        public Rectangle CollisionRectangle;

        public Point Offset;

        public bool MirroredV;
        public bool MirroredH;

        public int FrameTime
        {
            get => (int)Math.Round(FrameTimeFps * 1000 / 60f);
            set => FrameTimeFps = (int)Math.Round(value * 60 / 1000f);
        }

        // 1 => shown for 1 frame (if the game runs with 60fps)
        // 2 => shown for 2 frames
        public int FrameTimeFps { get; set; }
    }
}
