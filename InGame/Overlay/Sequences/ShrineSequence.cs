using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class ShrineSequence : GameSequence
    {
        public ShrineSequence()
        {
            _sequenceWidth = 160;
            _sequenceHeight = 144;

            // background
            Sprites.Add(new SeqSprite("shrine", new Vector2(0, 0), 0));
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
