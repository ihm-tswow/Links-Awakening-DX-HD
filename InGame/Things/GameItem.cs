using Microsoft.Xna.Framework;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.Things
{
    public class GameItem
    {
        public readonly DictAtlasEntry Sprite;
        public readonly Rectangle? SourceRectangle;

        // show a different sprite when drawn on the map compared to the one shown in the inventory
        public readonly DictAtlasEntry MapSprite;
        public readonly bool AnimateSprite;

        public readonly string Name;
        public readonly string PickUpDialog;

        public readonly string SoundEffectName;
        public readonly int MusicName;
        public readonly bool TurnDownMusic;

        public readonly int Level;

        public readonly int Count;
        public readonly int MaxCount;
        public readonly int DrawLength;

        public readonly bool IsRelict;
        public readonly bool Equipable;

        public readonly bool ShowEffect;
        public readonly int ShowAnimation;
        public readonly int ShowTime;

        public GameItem(
            DictAtlasEntry sprite = null,
            DictAtlasEntry mapSprite = null,
            bool animateSprite = false,
            string name = null, string pickUpDialog = null,
            string soundEffectName = null, int musicName = -1, bool turnDownMusic = false,
            int level = 0,
            int count = 0, int maxCount = 0, int drawLength = 2,
            bool isRelict = false, bool equipable = false,
            bool showEffect = false, int showAnimation = 0, int showTime = 250)
        {
            Sprite = sprite;
            MapSprite = mapSprite;

            if (sprite != null)
                SourceRectangle = sprite.SourceRectangle;

            AnimateSprite = animateSprite;

            Name = name;
            PickUpDialog = pickUpDialog;

            SoundEffectName = soundEffectName;
            MusicName = musicName;
            TurnDownMusic = turnDownMusic;

            Level = level;

            Count = count;
            MaxCount = maxCount;
            DrawLength = drawLength;

            IsRelict = isRelict;
            Equipable = equipable;

            ShowEffect = showEffect;
            ShowAnimation = showAnimation;
            ShowTime = showTime;
        }
    }

    public class GameItemCollected
    {
        public string Name;
        public string LocationBounding;
        public int Count;

        public GameItemCollected(string name)
        {
            Name = name;
        }
    }
}
