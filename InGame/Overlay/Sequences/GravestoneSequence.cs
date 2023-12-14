using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class GravestoneSequence : GameSequence
    {
        public GravestoneSequence()
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
            Sprites.Add(new SeqSprite("seqGravestoneBackground", position, 0));

            // characters
            AddDrawable("graveLink", new SeqAnimation("Sequences/link grave", "look", new Vector2(position.X + 75, position.Y + 101), 1) { Shader = Resources.ColorShader, Color = Game1.GameManager.CloakColor });
            AddDrawable("graveMouse", new SeqAnimation("NPCs/photo_mouse", "stand_0", new Vector2(position.X + 173, position.Y + 102), 1));

            AddDrawable("gravePhotoFlash", new SeqColor(new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.Transparent, 2));
            AddDrawable("gravePhoto", new SeqSprite("photo_11", position, 1) { Color = Color.Transparent });

            Game1.GameManager.StartDialogPath("seq_gravestone");

            base.OnStart();
        }
    }
}
