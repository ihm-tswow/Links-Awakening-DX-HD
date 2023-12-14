using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class SheetAnimation
    {
        public AFrame[] Frames = new AFrame[0];

        public string Id;
        public string NextAnimation;
        public int LoopCount;

        public SheetAnimation(string id, int loopCount, params AFrame[] frames)
        {
            Id = id;
            LoopCount = loopCount;
            Frames = frames;
        }
    }

    public class AFrame
    {
        public ASprite[] Sprites;

        public int FrameTime
        {
            get => (int)Math.Round(FrameTimeFps * 1000 / 60f);
            set => FrameTimeFps = (int)Math.Round(value * 60 / 1000f);
        }

        // 1 => shown for 1 frame (if the game runs with 60fps)
        // 2 => shown for 2 frames
        public int FrameTimeFps { get; set; }

        public AFrame(int frameTimeFps, params ASprite[] sprites)
        {
            FrameTimeFps = frameTimeFps;
            Sprites = sprites;
        }
    }

    public class ASprite
    {
        public Point Offset;

        public bool MirroredV;
        public bool MirroredH;

        public ASprite(int offsetX, int offsetY, bool mirroredV = false, bool mirroredH = false)
        {
            Offset = new Point(offsetX, offsetY);
            MirroredV = mirroredV;
            MirroredH = mirroredH;
        }
    }
}
