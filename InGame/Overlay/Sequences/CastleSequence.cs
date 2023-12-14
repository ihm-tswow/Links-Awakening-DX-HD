using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class CastleSequence : GameSequence
    {
        public CastleSequence()
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
            Sprites.Add(new SeqSprite("seqCastleBackground", position, 0));

            // characters
            AddDrawable("castleLink", new SeqAnimation("Sequences/link castle", "stand", new Vector2(position.X - 24, position.Y + 155), 1) { Shader = Resources.ColorShader, Color = Game1.GameManager.CloakColor });
            AddDrawable("castleMouse", new SeqAnimation("NPCs/photo_mouse", "stand_0", new Vector2(position.X + 91, position.Y + 119), 1));
            AddDrawable("castleBoy", new SeqAnimation("Sequences/castle frog boy", "walk", new Vector2(position.X + 176, position.Y + 145), 2));
            AddDrawable("castleFlash", new SeqColor(new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.Transparent, 3));

            // start the sequence path
            Game1.GameManager.StartDialogPath("castle_sequence");

            base.OnStart();
        }
    }
}
