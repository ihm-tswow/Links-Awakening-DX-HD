using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyWizzrobe : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly PushableComponent _pushComponent;
        private readonly AiTriggerTimer _hiddenTimer;
        private readonly DamageFieldComponent _damageField;
        private readonly AiStunnedState _aiStunnedState;

        private readonly Rectangle _fieldRectangle;

        private const int BlinkTime = 600;
        private int _direction;

        public EnemyWizzrobe() : base("wizzrobe") { }

        public EnemyWizzrobe(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            Tags = Values.GameObjectTag.Enemy;

            _fieldRectangle = map.GetField(posX, posY);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/wizzrobe");
            _animator.Play("head");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, 0));

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 12, 8);

            var stateHidden = new AiState(UpdateHidden) { Init = InitHidden };
            // will be hidden for at lease x time
            stateHidden.Trigger.Add(_hiddenTimer = new AiTriggerTimer(1000));
            var stateSpawn = new AiState { Init = InitSpawn };
            stateSpawn.Trigger.Add(new AiTriggerCountdown(BlinkTime, BlinkTick, () => _aiComponent.ChangeState("head")));
            var stateHead = new AiState { Init = InitHead };
            stateHead.Trigger.Add(new AiTriggerCountdown(400, null, () => _aiComponent.ChangeState("stand")));
            var stateStand = new AiState { Init = InitStand };
            stateStand.Trigger.Add(new AiTriggerCountdown(300, null, Shoot));
            stateStand.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("despawnHead")));
            var stateDespawnHead = new AiState { Init = InitHead };
            stateDespawnHead.Trigger.Add(new AiTriggerCountdown(400, null, () => _aiComponent.ChangeState("despawn")));
            var stateDespawn = new AiState();
            stateDespawn.Trigger.Add(new AiTriggerCountdown(BlinkTime, BlinkTick, () => _aiComponent.ChangeState("hidden")));

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("head", stateHead);
            _aiComponent.States.Add("stand", stateStand);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.States.Add("despawnHead", stateDespawnHead);
            _aiStunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 4, false, false);
            new AiFallState(_aiComponent, _body, null, null, 100);

            _aiComponent.ChangeState("hidden");

            var hittableBox = new CBox(EntityPosition, -7, -15, 14, 15, 8);
            var damageBox = new CBox(EntityPosition, -7, -14, 14, 14, 4);
            var pushableBox = new CBox(EntityPosition, -6, -14, 12, 14, 8);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 4) { IsActive = false });
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, _pushComponent = new PushableComponent(pushableBox, OnPush) { IsActive = false });
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));
        }

        private void InitSpawn()
        {
            _animator.Play("head");
        }

        private void BlinkTick(double timer)
        {
            var sinState = (float)((BlinkTime - timer) / BlinkTime);
            var state = MathF.Sin(sinState * 9f * MathF.PI * 2);

            // blink
            _sprite.IsVisible = state >= 0;
        }

        private void InitHead()
        {
            _animator.Play("head");
            _sprite.IsVisible = true;

            _damageField.IsActive = false;
            _pushComponent.IsActive = false;
        }

        private void InitHidden()
        {
            _sprite.IsVisible = false;
        }

        private void UpdateHidden()
        {
            // start spawning
            if (_hiddenTimer.State && _fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position))
                _aiComponent.ChangeState("spawn");
        }

        private void InitStand()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            _direction = AnimationHelper.GetDirection(playerDirection);

            _damageField.IsActive = true;
            _pushComponent.IsActive = true;

            // look towards the player
            _animator.Play("stand_" + _direction);
        }

        private void Shoot()
        {
            var projectile = new EnemyWizzrobeProjectile(Map, new Vector2(EntityPosition.X, EntityPosition.Y - 7), _direction, 2.0f);
            Map.Objects.SpawnObject(projectile);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // can not hit the enemy while he is spawning or hidden
            if (_damageState.CurrentLives <= 0 || _damageState.IsInDamageState() ||
                (_aiComponent.CurrentStateId != "stand" && !_aiStunnedState.IsStunned()))
                return Values.HitCollision.None;

            if (type == HitType.Hookshot || type == HitType.Boomerang)
            {
                _aiStunnedState.StartStun();
                _damageField.IsActive = false;
            }

            if (type == HitType.MagicPowder)
            {
                _aiStunnedState.StartStun();
                _damageState.SetDamageState(false);
                _damageField.IsActive = false;
                _body.Velocity.X = direction.X * 2.5f;
                _body.Velocity.Y = direction.Y * 2.5f;
                return Values.HitCollision.None;
            }

            // @TODO: not sure if thrown object damage is right
            if (type == HitType.Bomb || type == HitType.ThrownObject)
                damage = 4;
            else if (type == HitType.Bow || type == HitType.MagicRod)
                damage = 1;
            else
                damage = 0;

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.75f, _body.Velocity.Z);

            return true;
        }
    }
}