using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class MarinCliffSequence : GameSequence
    {
        private float _birdSoundCounter;

        public MarinCliffSequence()
        {
            _sequenceWidth = 160;
            _sequenceHeight = 144;
        }

        public override void OnStart()
        {
            Sprites.Clear();
            SpriteDict.Clear();

            var position = Vector2.Zero;

            // background
            Sprites.Add(new SeqSprite("cliff_background", position, 0));

            // link and marin
            Sprites.Add(new SeqAnimation("Sequences/cliff sequence", "link", new Vector2(position.X + 99, position.Y + 40), 1) { Shader = Resources.ColorShader, Color = Game1.GameManager.CloakColor });
            Sprites.Add(new SeqAnimation("Sequences/cliff sequence", "marin", new Vector2(position.X + 81, position.Y + 56), 1));

            AddDrawable("cliffPhotoFlash", new SeqColor(new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.Transparent, 5));

            // start the sequence path
            Game1.GameManager.StartDialogPath("seq_cliff");

            base.OnStart();
        }

        public override void Update()
        {
            base.Update();

            _birdSoundCounter -= Game1.DeltaTime;
            if (_birdSoundCounter < 0)
            {
                _birdSoundCounter += Game1.RandomNumber.Next(1000, 3500);
                Game1.GameManager.PlaySoundEffect("D360-33-21");
            }
        }
    }
}
