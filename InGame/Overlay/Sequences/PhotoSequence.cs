using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class PhotoSequence : GameSequence
    {
        public PhotoSequence()
        {
            _sequenceWidth = 160;
            _sequenceHeight = 144;
        }

        public override void OnStart()
        {
            Sprites.Clear();
            SpriteDict.Clear();

            var photo = Game1.GameManager.SaveManager.GetString("photoSequencePhoto");

            // background
            if (!string.IsNullOrEmpty(photo))
                Sprites.Add(new SeqSprite(photo, new Vector2(0, 0), 0));

            base.OnStart();
        }

        public override void Update()
        {
            base.Update();

            // can close the overlay if the dialog isn't running anymore
            if (!Game1.GameManager.DialogIsRunning() && ControlHandler.ButtonPressed(CButtons.B))
                Game1.GameManager.InGameOverlay.CloseOverlay();
        }
    }
}
