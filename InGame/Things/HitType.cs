using System;

namespace ProjectZ.InGame.Things
{
    [Flags]
    public enum HitType
    {
        // used to hit enemies
        Sword1 = 0x01 << 0,
        Sword2 = 0x01 << 1,
        Sword = Sword1 | Sword2,
        Boomerang = 0x01 << 2,
        Bomb = 0x01 << 3,
        Bow = 0x01 << 4,
        Hookshot = 0x01 << 5,
        MagicRod = 0x01 << 6,
        MagicPowder = 0x01 << 7,
        PegasusBootsSword = 0x01 << 8,
        PegasusBootsPush = 0x01 << 9,
        ThrownObject = 0x01 << 10,
        BowWow = 0x01 << 11,
        SwordShot = 0x01 << 12,
        SwordHold = 0x01 << 13,
        SwordSpin = 0x01 << 14,

        // used to hit the player
        Spikes = 0x01 << 15,
        Object = 0x01 << 16,
        Enemy = 0x01 << 17,
        Boss = 0x01 << 18,
    }
}
