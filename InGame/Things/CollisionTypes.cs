using System;

namespace ProjectZ.InGame.Things
{
    public partial class Values
    {
        [Flags]
        public enum CollisionTypes
        {
            None = 0x00,
            Normal = 0x01,
            Hole = 0x02,
            PlayerItem = 0x04,
            Player = 0x08,
            Enemy = 0x10,
            Ladder = 0x20,
            LadderTop = 0x40,
            NPCWall = 0x80,
            Item = 0x100,
            DrownExclude = 0x200,
            Hookshot = 0x400,
            DeepWater = 0x800,
            MovingPlatform = 0x1000,
            RaftExit = 0x2000,
            PushIgnore = 0x4000, // objects the player should not push (play the push animation)
            Destroyable = 0x8000,
            ThrowIgnore = 0x10000,
            ThrowWeaponIgnore = 0x20000
        }

        [Flags]
        public enum BodyCollision
        {
            None = 0,
            Floor = 1,
            Left = 2,
            Right = 4,
            Top = 8,
            Bottom = 16,
            Horizontal = 32,
            Vertical = 64
        }

        [Flags]
        public enum HitCollision
        {
            None,
            Enemy = 1,
            Blocking = 2,
            NoneBlocking = 4, // weapons like the boomerang will move through the object
            Particle = 8,
            Repelling = 16,
            RepellingParticle = 8 + 16,
            
            Repelling0 = 32, // repell much
            Repelling1 = 64, // repell not so much
        }

        [Flags]
        public enum GameObjectTag
        {
            None = 0,
            Enemy = 1,
            Trap = 2,
            Damage = 4,
            Hole = 8,
            Lamp = 16,
            Ocarina = 32
        }
    }
}
