using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyAntiKirby : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageField;

        private readonly ObjAnimator _suckParticles;

        private Vector2 _walkDirection;

        private int _direction;
        private bool _hasPlayerTrapped;
        private bool _endMove;
        private bool _bounceSound;

        public EnemyAntiKirby() : base("anti kirby") { }

        public EnemyAntiKirby(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntityPosition.AddPositionListener(typeof(EnemyLikeLike), UpdatePosition);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/anti kirby");
            _animator.Play("idle_0");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy |
                                 Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Drag = 0.85f,
                Bounciness = 0.3f,
                AbsorbPercentage = 0.8f,
                Gravity = -0.125f,
                MaxJumpHeight = 3,
            };

            var stateIdle = new AiState(UpdateIdle) { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(EndIdle, 500, 850));
            var stateMove = new AiState(UpdateMoving) { Init = InitMoving };
            stateMove.Trigger.Add(new AiTriggerRandomTime(EndMove, 500, 850));
            var stateSuck = new AiState(UpdateSuck) { Init = InitSuck };
            stateSuck.Trigger.Add(new AiTriggerCountdown(4300, null, EndSuck));
            var stateTrap = new AiState(UpdateTrap) { Init = InitTrap };
            stateTrap.Trigger.Add(new AiTriggerCountdown(2000, null, EndTrap));
            var stateSpit = new AiState { Init = InitSpit };
            stateSpit.Trigger.Add(new AiTriggerCountdown(250, null, EndSpit));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("suck", stateSuck);
            _aiComponent.States.Add("trap", stateTrap);
            _aiComponent.States.Add("spit", stateSpit);
            new AiFallState(_aiComponent, _body);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 8, true, false)
            {
                HitMultiplierX = 2.5f,
                HitMultiplierY = 2.5f,
            };
            _damageState.OnDeath = OnDeath;

            var hittableBox = new CBox(EntityPosition, -7, -13, 0, 14, 13, 8, true);
            var damageBox = new CBox(EntityPosition, -6, -12, 12, 12, 2);

            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 10 });

            // suck animator
            _suckParticles = new ObjAnimator(map,
                (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerPlayer, "Enemies/anti kirby suck", "", false);
            _suckParticles.EntityPosition.SetParent(EntityPosition, Vector2.Zero);
            map.Objects.SpawnObject(_suckParticles);

            _aiComponent.ChangeState("idle");
        }

        private void InitSuck()
        {
            _damageField.IsActive = false;
            _body.IgnoresZ = true;

            // look in the direction of the player
            _direction = EntityPosition.X > MapManager.ObjLink.EntityPosition.X ? 0 : 1;

            _animator.Play("suck_" + _direction);

            // suck animation particles
            _suckParticles.AnimationComponent.SpriteOffset = new Vector2(_direction == 0 ? -15 : 15, 2);
            _suckParticles.Animator.Play("suck_" + _direction);
        }

        private void UpdateSuck()
        {
            Game1.GameManager.PlaySoundEffect("D378-59-3B", false, 0.75f, 0, false, 100);

            if (EntityPosition.Z < 12)
            {
                EntityPosition.Z += Game1.TimeMultiplier * 0.25f;
                if (EntityPosition.Z > 12)
                    EntityPosition.Z = 12;
            }

            // suck in the player
            var playerDirection = new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z + 4) - MapManager.ObjLink.EntityPosition.Position;
            var playerDir = EntityPosition.X > MapManager.ObjLink.EntityPosition.X ? 0 : 1;

            // trap the player if he is close enough
            if (playerDirection.Length() < 4)
            {
                ToTrap();
            }
            else if (playerDirection.Length() < 48 && playerDir == _direction)
            {
                playerDirection.Normalize();
                MapManager.ObjLink._body.Velocity.X = 0.5f * playerDirection.X;
                MapManager.ObjLink._body.Velocity.Y = 0.5f * playerDirection.Y;
                MapManager.ObjLink._body.DisableVelocityTargetMultiplier = true;
            }
        }

        private void EndSuck()
        {
            _damageField.IsActive = true;
            _body.IgnoresZ = false;
            _aiComponent.ChangeState("idle");

            StopSuckSound();
        }

        private void StopSuckSound()
        {
            Game1.GameManager.StopSoundEffect("D378-59-3B");
        }

        private void ToTrap()
        {
            MapManager.ObjLink.TrapPlayer(true);
            MapManager.ObjLink.SetPosition(EntityPosition.Position);

            _aiComponent.ChangeState("trap");
            _hasPlayerTrapped = true;

            StopSuckSound();
        }

        private void InitTrap()
        {
            _bounceSound = false;
            _body.IgnoresZ = false;
            _suckParticles.Animator.Play("hidden");
            _animator.Play("full_" + _direction);
        }

        private void UpdateTrap()
        {
            if (!_body.WasGrounded && _body.IsGrounded && !_bounceSound)
            {
                _bounceSound = true;
                Game1.GameManager.PlaySoundEffect("D360-09-09");
            }
        }

        private void EndTrap()
        {
            _hasPlayerTrapped = false;

            MapManager.ObjLink.SetPosition(EntityPosition.Position);
            MapManager.ObjLink.FreeTrappedPlayer();
            MapManager.ObjLink.CurrentState = ObjLink.State.Jumping;
            MapManager.ObjLink._body.Velocity = new Vector3(_direction == 0 ? -1.5f : 1.5f, 0, 1.25f);

            _aiComponent.ChangeState("spit");
            Game1.GameManager.PlaySoundEffect("D360-08-08");
            Game1.GameManager.InflictDamage(2);
        }

        private void InitSpit()
        {
            _animator.Play("suck_" + _direction);
        }

        private void EndSpit()
        {
            _aiComponent.ChangeState("idle");
        }

        private void InitIdle()
        {
            _animator.Play("idle_" + _direction);
            _suckParticles.Animator.Play("hidden");
        }

        private void UpdateIdle()
        {
            // jump on the spot
            if (_body.IsGrounded)
                _body.Velocity.Z = 0.9f;
        }

        private void EndIdle()
        {
            _aiComponent.ChangeState("move");
        }

        private void InitMoving()
        {
            _endMove = false;

            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _walkDirection.X = MathF.Sin(rotation);
            _walkDirection.Y = MathF.Cos(rotation);

            _direction = _walkDirection.X < 0 ? 0 : 1;
            _animator.Play("idle_" + _direction);
        }

        private void UpdateMoving()
        {
            // jump
            if (_body.IsGrounded)
            {
                if (_endMove)
                {
                    var playerDirection = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                    var playerDir = EntityPosition.X > MapManager.ObjLink.EntityPosition.X ? 0 : 1;

                    if (Math.Abs(playerDirection.Y) < 32 && Math.Abs(playerDirection.X) < 48 && playerDir == _direction)
                        _aiComponent.ChangeState("suck");
                    else
                        _aiComponent.ChangeState("idle");
                }
                else
                {
                    _body.Velocity = new Vector3(_walkDirection.X, _walkDirection.Y, 0.9f);
                }
            }
        }

        private void EndMove()
        {
            _damageField.IsActive = true;
            _endMove = true;
        }

        private void UpdatePosition(CPosition newPosition)
        {
            if (_hasPlayerTrapped)
                MapManager.ObjLink.SetPosition(newPosition.Position);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "move")
                _aiComponent.ChangeState("idle");

            if ((damageType & HitType.Sword) != 0 ||
                damageType == HitType.Bow ||
                damageType == HitType.Hookshot ||
                damageType == HitType.MagicPowder)
                damage = 0;

            // 4 hits
            if (damageType == HitType.Boomerang)
                damage = 2;

            if (damageType == HitType.Bomb ||
                damageType == HitType.MagicRod)
                damage = 4;

            if (damage != 0 && _hasPlayerTrapped)
                EndTrap();

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void OnDeath(bool pieceOfPower)
        {
            StopSuckSound();

            _damageState.BaseOnDeath(pieceOfPower);

            // remove the suck animation particles
            Map.Objects.DeleteObjects.Add(_suckParticles);

            // free the player
            if (_hasPlayerTrapped)
                MapManager.ObjLink.FreeTrappedPlayer();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_hasPlayerTrapped)
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.5f, _body.Velocity.Z);

            return true;
        }
    }
}