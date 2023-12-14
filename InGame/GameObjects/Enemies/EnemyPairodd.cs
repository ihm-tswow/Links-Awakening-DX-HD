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
    internal class EnemyPairodd : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly DrawShadowCSpriteComponent _shadowComponent;
        private readonly AiTriggerTimer _teleportCooldown;
        private readonly AiTriggerCountdown _shootCountdown;
        private readonly AiStunnedState _aiStunnedState;

        private readonly Rectangle _fieldRectangle;
        private readonly Vector2 _centerPosition;

        public EnemyPairodd() : base("pairodd") { }

        public EnemyPairodd(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            Tags = Values.GameObjectTag.Enemy;

            _fieldRectangle = map.GetField(posX, posY);
            _centerPosition = new Vector2(_fieldRectangle.Center.X, _fieldRectangle.Center.Y + 8);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/pairodd");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8);

            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(_teleportCooldown = new AiTriggerTimer(300));
            var stateSpawn = new AiState(UpdateSpawn);
            var statePreDespawn = new AiState();
            statePreDespawn.Trigger.Add(new AiTriggerCountdown(200, null, ToDespawn));
            var stateDespawn = new AiState(UpdateDespawn);
            var stateHidden = new AiState();
            stateHidden.Trigger.Add(new AiTriggerCountdown(600, null, ToSpawning));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(_shootCountdown = new AiTriggerCountdown(400, null, Shoot));
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("preDespawn", statePreDespawn);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.States.Add("hidden", stateHidden);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2, false) { OnBurn = () => _animator.Pause() };
            _aiStunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);

            var hittableBox = new CBox(EntityPosition, -7, -14, 14, 14, 8);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new DrawShadowCSpriteComponent(_sprite));

            ToIdle();
            // do not shoot directly after spawning
            _shootCountdown.Stop();
        }

        private void ToSpawning()
        {
            _aiComponent.ChangeState("spawn");
            _animator.Play("spawn");
            _sprite.IsVisible = true;
            _body.Velocity = Vector3.Zero;

            // set the new position to be at the opposite side of the room
            var directionToCenter = _centerPosition - EntityPosition.Position;

            // clamp the offset to not move too fare from the center
            if (directionToCenter.Length() > 48)
            {
                directionToCenter.Normalize();
                directionToCenter *= 56;
            }

            var newPosition = _centerPosition + directionToCenter;
            EntityPosition.Set(newPosition);
        }

        private void UpdateSpawn()
        {
            // finished spawn animation?
            if (!_animator.IsPlaying)
                ToIdle();
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");
            _animator.Play("idle");
            _damageState.IsActive = true;
            _shadowComponent.IsActive = true;
            _body.IsActive = true;
            _shootCountdown.OnInit();
        }

        private void UpdateIdle()
        {
            if (!_teleportCooldown.State)
                return;

            var playerDistance = _body.BodyBox.Box.Center - MapManager.ObjLink.BodyRectangle.Center;

            if (playerDistance.Length() < 46)
                _aiComponent.ChangeState("preDespawn");
        }

        private void Shoot()
        {
            if (!_fieldRectangle.Contains(MapManager.ObjLink.BodyRectangle.Center))
                return;

            var projectile = new EnemyPairoddProjectile(Map, new Vector2(EntityPosition.X, EntityPosition.Y - 8), 1.5f);
            Map.Objects.SpawnObject(projectile);
        }

        private void ToDespawn()
        {
            // do not despawn if the enemy is dead
            if (_damageState.CurrentLives <= 0 && _damageState.DamageTrigger.CurrentTime > 0)
                return;

            Game1.GameManager.PlaySoundEffect("D360-60-3C");

            _aiComponent.ChangeState("despawn");
            _animator.Play("despawn");
            _damageState.IsActive = false;
            _shadowComponent.IsActive = false;
            _body.IsActive = false;
        }

        private void UpdateDespawn()
        {
            // finished spawn animation?
            if (!_animator.IsPlaying)
                ToHidden();
        }

        private void ToHidden()
        {
            _aiComponent.ChangeState("hidden");
            _sprite.IsVisible = false;
        }

        private void OnHoleAbsorb()
        {
            _animator.Play("idle");
            _animator.SpeedMultiplier = 4f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (!_damageState.IsActive)
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_damageState.IsActive || _damageState.IsInDamageState())
                return Values.HitCollision.None;

            if (damageType == HitType.Boomerang)
            {
                _damageState.SetDamageState(false);

                _body.Velocity.X += direction.X * 2.5f;
                _body.Velocity.Y += direction.Y * 2.5f;

                _aiStunnedState.StartStun();
                _animator.Pause();

                return Values.HitCollision.Enemy;
            }

            if (damageType == HitType.Bomb)
                damage = 1;

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }
    }
}