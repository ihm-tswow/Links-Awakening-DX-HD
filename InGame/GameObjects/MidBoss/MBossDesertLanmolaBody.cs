using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossDesertLanmolaBody : GameObject
    {
        public bool IsVisible = true;
        public readonly CSprite Sprite;

        private readonly ShadowBodyDrawComponent _shadowComponent;

        public MBossDesertLanmolaBody(Map.Map map, Vector2 position, bool isTail) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -48, 16, 48);

            var animator = AnimatorSaveLoad.LoadAnimator("MidBoss/desertLanmola");
            animator.Play(isTail ? "tail" : "body");

            Sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, Sprite, new Vector2(0, 0));
            
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new ShadowBodyDrawComponent(EntityPosition));
        }

        public void Hide()
        {
            IsVisible = false;
            Sprite.IsVisible = false;
            _shadowComponent.IsActive = false;
        }

        public void Show()
        {
            IsVisible = true;
            Sprite.IsVisible = true;
            _shadowComponent.IsActive = true;
        }
    }
}