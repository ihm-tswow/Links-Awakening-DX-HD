using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossDesertLanmolaHead : GameObject
    {
        public bool IsVisible = true;
        public readonly CSprite Sprite;

        private readonly MBossDesertLanmola _owner;
        private readonly Animator _animator;
        private readonly ShadowBodyDrawComponent _shadowComponent;
        private readonly DamageFieldComponent _damageComponent;

        private int _direction;

        public MBossDesertLanmolaHead(Map.Map map, MBossDesertLanmola owner, Vector2 position) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -48, 16, 48);

            _owner = owner;
            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/desertLanmola");
            _animator.Play("head_0");

            Sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, Sprite, new Vector2(0, 0));

            var damageBox = new CBox(EntityPosition, -7, -15, 0, 14, 14, 8, true);
            AddComponent(DamageFieldComponent.Index, _damageComponent = new DamageFieldComponent(damageBox, HitType.Enemy, 4));

            var hittableBox = new CBox(EntityPosition, -7, -15, 0, 14, 14, 8, true);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new ShadowBodyDrawComponent(EntityPosition));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (!IsVisible)
                return Values.HitCollision.None;

            return _owner.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        public void Hide()
        {
            IsVisible = false;
            Sprite.IsVisible = false;
            _shadowComponent.IsActive = false;
            _damageComponent.IsActive = false;
        }

        public void Spawn(Vector2 direction)
        {
            IsVisible = true;
            Sprite.IsVisible = true;
            _shadowComponent.IsActive = true;
            _damageComponent.IsActive = true;

            if (Math.Abs(direction.Y) > Math.Abs(direction.X))
                _direction = direction.Y > 0 ? 3 : 1;
            else
                _direction = direction.X > 0 ? 2 : 0;

            _animator.Play("head_" + _direction);
        }

        public void SetDown()
        {
            _animator.Play("head_3");
        }
    }
}