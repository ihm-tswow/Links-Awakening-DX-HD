using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class AnimationComponent : BaseAnimationComponent
    {
        public CSprite Sprite;
        public Animator Animator;

        public Vector2 SpriteOffset;

        private bool _mirroredV;
        public bool MirroredV
        {
            get => _mirroredV;
            set
            {
                _mirroredV = value;
                UpdateSprite();
            }
        }

        private bool _mirroredH;
        public bool MirroredH
        {
            get => _mirroredH;
            set
            {
                _mirroredH = value;
                UpdateSprite();
            }
        }

        public AnimationComponent(Animator animator, CSprite sprite, Vector2 spriteOffset)
        {
            Animator = animator;
            Animator.OnFrameChange = UpdateSprite;

            SpriteOffset = spriteOffset;
            Sprite = sprite;
            Sprite.SprTexture = animator.SprTexture;

            UpdateSprite();
        }

        public void UpdateSprite()
        {
            var offsetX = MirroredH ? -1 : 1;
            Sprite.DrawOffset.X = SpriteOffset.X + (Animator.CurrentAnimation.Offset.X +
                                                    Animator.CurrentFrame.Offset.X) * offsetX;

            var offsetY = MirroredV ? -1 : 1;
            Sprite.DrawOffset.Y = SpriteOffset.Y + (Animator.CurrentAnimation.Offset.Y +
                                                    Animator.CurrentFrame.Offset.Y) * offsetY;

            if (MirroredH)
                Sprite.DrawOffset.X -= Animator.CurrentFrame.SourceRectangle.Width;
            if (MirroredV)
                Sprite.DrawOffset.Y -= Animator.CurrentFrame.SourceRectangle.Height;

            Sprite.SourceRectangle = Animator.CurrentFrame.SourceRectangle;
            Sprite.SpriteEffect =
                (Animator.CurrentFrame.MirroredV ^ MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                (Animator.CurrentFrame.MirroredH ^ MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }

        public override void UpdateAnimation()
        {
            Animator.Update();
        }
    }
}
