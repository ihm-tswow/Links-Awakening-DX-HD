using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBombiteGreen : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly AiTriggerSwitch _damageCooldown;
        private readonly AiStunnedState _aiStunnedState;
        private readonly CSprite _sprite;

        private const float WalkSpeed = 0.5f;

        private int _direction;
        private bool _startedAnimation;
        private bool _follow;

        public EnemyBombiteGreen() : base("bombiteGreen") { }

        public EnemyBombiteGreen(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/bombiteGreen");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-7, -16));

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 11, 8)
            {
                AbsorbPercentage = 0.9f,
                CollisionTypes = Values.CollisionTypes.Normal,
                AvoidTypes =
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall,
                Bounciness = 0.25f,
                Drag = 0.85f,
            };

            var stateIdle = new AiState(UpdateIdle) { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 250, 500));
            var stateFollow = new AiState(UpdateFollow);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("follow", stateFollow);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb, null);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1, false);
            _aiStunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900) { SilentStateChange = false };

            _aiComponent.Trigger.Add(_damageCooldown = new AiTriggerSwitch(250));

            _aiComponent.ChangeState("idle");
            ChangeDirection();

            var damageCollider = new CBox(EntityPosition, -6, -13, 0, 12, 12, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite) { Height = 1.0f, Rotation = 0.1f });
        }

        private void InitIdle()
        {
            _animator.Play("idle");
        }

        private void UpdateIdle()
        {
            if (_follow && !_damageState.IsInDamageState())
                _aiComponent.ChangeState("follow");
        }

        private void UpdateFollow()
        {
            // start animation when slowed down enough
            if (!_startedAnimation && _body.Velocity.Length() < 0.1f)
            {
                _startedAnimation = true;
                _animator.Play("timer");
            }

            if (_startedAnimation)
            {
                if (!_animator.IsPlaying)
                    Explode();
                else if (_animator.CurrentFrameIndex > 2)
                {
                    // blink
                    _sprite.SpriteShader = Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
                }

                // move towards the player
                var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
                var distance = direction.Length();
                if (direction != Vector2.Zero)
                    direction.Normalize();

                if (distance > 20)
                    _body.VelocityTarget = direction;
                else
                    _body.VelocityTarget = Vector2.Zero;
            }
        }

        private void ChangeDirection()
        {
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * WalkSpeed;
        }

        private void Explode()
        {
            // spawn explosion effect
            var objExplosion = new ObjBomb(Map, EntityPosition.X, EntityPosition.Y, false, false);
            objExplosion.Explode();
            Map.Objects.SpawnObject(objExplosion);

            Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            if (damageType == HitType.Bomb && !(gameObject is EnemyBombite))
            {
                // spawn a bomb
                _damageState.SpawnItem = "bomb_1";
                return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
            }

            // stun state
            if (damageType == HitType.Hookshot || damageType == HitType.Boomerang)
            {
                _body.VelocityTarget = Vector2.Zero;
                _body.Velocity.X += direction.X * 4.0f;
                _body.Velocity.Y += direction.Y * 4.0f;

                _aiStunnedState.StartStun();
                _animator.Pause();

                return Values.HitCollision.Enemy;
            }

            if (damageType != HitType.MagicPowder)
            {
                if (pieceOfPower)
                    Game1.GameManager.PlaySoundEffect("D370-17-11");

                Game1.GameManager.PlaySoundEffect("D360-03-03");

                if (pieceOfPower)
                    _damageState.HitKnockBack(gameObject, direction, damageType, pieceOfPower, false);
                else
                {
                    _body.Velocity.X += direction.X * 5.0f;
                    _body.Velocity.Y += direction.Y * 5.0f;
                    _damageState.SetDamageState(false);
                }
            }
            else
            {
                _body.Velocity.X += direction.X * 1.0f;
                _body.Velocity.Y += direction.Y * 1.0f;
                _damageState.SetDamageState(false);
            }

            if (!_aiStunnedState.IsStunned())
                _follow = true;

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
            _animator.Play("idle");
        }
    }
}