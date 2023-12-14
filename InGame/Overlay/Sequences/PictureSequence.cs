using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class PictureSequence : GameSequence
    {
        public PictureSequence()
        {
            _sequenceWidth = 103;
            _sequenceHeight = 128;

            // background
            Sprites.Add(new SeqSprite("trade_picture", new Vector2(0, 0), 0));
        }

        public override void Update()
        {
            base.Update();

            // can close the overlay if the dialog isn't running anymore
            if (!Game1.GameManager.DialogIsRunning() && ControlHandler.ButtonPressed(CButtons.B))
            {
                Game1.GameManager.InGameOverlay.CloseOverlay();
                Game1.GameManager.StartDialogPath("close_picture");
            }
        }
    }
}
