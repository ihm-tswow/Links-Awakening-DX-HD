using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjAnimator : GameObject
    {
        public Animator Animator;
        public AnimationComponent AnimationComponent;
        public CSprite Sprite;

        public ObjAnimator(Map.Map map, int posX, int posY, int offsetX, int offsetY,
            int layer, string animatorName, string animationName, bool deleteOnFinish) : base(map)
        {
            SprEditorImage = Resources.SprItem;
            EditorIconSource = new Rectangle(64, 168, 16, 16);

            EntityPosition = new CPosition(posX, posY, 0);

            Animator = AnimatorSaveLoad.LoadAnimator(animatorName);
            
            // this should never happen...
            if (Animator == null)
            {
                Console.WriteLine("Error: could not load animation \"{0}\"", animatorName);
                IsDead = true;
                return;
            }

            Animator.Play(animationName);

            EntitySize = new Rectangle(
                offsetX + Animator.CurrentAnimation.Offset.X + Animator.CurrentAnimation.AnimationLeft,
                offsetY + Animator.CurrentAnimation.Offset.Y + Animator.CurrentAnimation.AnimationTop,
                Animator.CurrentAnimation.AnimationWidth, Animator.CurrentAnimation.AnimationHeight);

            Sprite = new CSprite(EntityPosition);
            AnimationComponent = new AnimationComponent(Animator, Sprite, new Vector2(offsetX, offsetY));
            if (deleteOnFinish)
                Animator.OnAnimationFinished = () => Map.Objects.DeleteObjects.Add(this);

            AddComponent(BaseAnimationComponent.Index, AnimationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, layer));
        }

        public ObjAnimator(Map.Map map, int posX, int posY, int layer, string animatorName, string animationName,
            bool deleteOnFinish) : this(map, posX, posY, 0, 0, layer, animatorName, animationName, deleteOnFinish)
        { }
    }
}