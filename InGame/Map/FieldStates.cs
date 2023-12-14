using System;

namespace ProjectZ.InGame.Map
{
    public class MapStates
    {
        [Flags]
        public enum FieldStates
        {
            None = 0,
            Init = 1,
            Grass = 2,
            Water = 4,
            DeepWater = 8,
            Lava = 16,

            UpperLevel = 32 | 64,
            Level1 = 32,
            Level2 = 64,
        }

        public static int GetLevel(FieldStates state)
        {
            if ((state & FieldStates.Level1) != 0)
                return 1;
            if ((state & FieldStates.Level2) != 0)
                return 2;

            return 0;
        }
    }
}