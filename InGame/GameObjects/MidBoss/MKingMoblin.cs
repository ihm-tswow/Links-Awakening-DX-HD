using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MKingMoblin : GameObject
    {
        private readonly BodyComponent _body;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;
        private readonly Animator _animator;

        private readonly Vector2 _spawnPosition;

        private Vector2 _moveDirection;

        private readonly string _triggerKey;
        private readonly string _saveKey;

        private const int Lives = 8;

        private double _bounceTime;
        private int _direction;
        private bool _endWaiting;

        public MKingMoblin() : base("king_moblin") { }

        public MKingMoblin(Map.Map map, int posX, int posY, string triggerKey, string saveKey) : base(map)
        {
            // was the boss already defeated?
            if (!string.IsNullOrEmpty(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-12, -24, 24, 24);

            _triggerKey = triggerKey;
            _saveKey = saveKey;

            _spawnPosition = EntityPosition.Position;

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/bigMoblin");
            _animator.Play("wait");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -10, -16, 20, 16, 8)
            {
                MoveCollision = OnCollision,
                Drag = 0.65f,
                DragAir = 0.95f,
                Gravity = -0.15f,
                FieldRectangle = map.GetField(posX, posY)
            };

            var stateWaiting = new AiState(UpdateWaiting);
            var stateWalk = new AiState(UpdateWalk);
            var stateSpear = new AiState(UpdateSpear);
            var statePostSpear = new AiState();
            statePostSpear.Trigger.Add(new AiTriggerCountdown(500, null, ToWalking));
            var statePreAttack = new AiState(UpdatePreAttack);
            var stateAttack = new AiState();
            stateAttack.Trigger.Add(new AiTriggerCountdown(550, null, ToWalking));
            var stateBounce = new AiState(UpdateBound);
            var stateLook = new AiState(UpdateLook);

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("walk", stateWalk);
            _aiComponent.States.Add("spear", stateSpear);
            _aiComponent.States.Add("postSpear", statePostSpear);
            _aiComponent.States.Add("preAttack", statePreAttack);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("bounce", stateBounce);
            _aiComponent.States.Add("look", stateLook);

            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, true, false)
            {
                ExplostionWidth = 22,
                ExplostionHeight = 18
            };
            _damageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("waiting");

            var hittableBox = new CBox(EntityPosition, -10, -14, 0, 20, 14, 8, true);
            var damageCollider = new CBox(EntityPosition, -7, -11, 0, 14, 11, 8, true);
            _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer);

            if (!string.IsNullOrEmpty(_triggerKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 1 });
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 4) { OnDamagedPlayer = OnDamagedPlayer });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 16, ShadowHeight = 6 });
        }

        private void KeyChanged()
        {
            // activate the boss after entering the room
            if (IsActive &&
                _aiComponent.CurrentStateId == "waiting" &&
                Game1.GameManager.SaveManager.GetString(_triggerKey) == "1")
            {
                _endWaiting = true;
            }
        }

        private void OnDamagedPlayer()
        {
            if (_aiComponent.CurrentStateId == "attack")
            {
                Game1.GameManager.PlaySoundEffect("D360-11-0B");
                ToWalking();
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Impact)
                return false;

            var mult = _body.IsGrounded ? 3.5f : 1.5f;
            _body.Velocity = new Vector3(direction.X * mult, direction.Y * mult, 0);

            return true;
        }

        private void UpdateWaiting()
        {
            if (_body.IsGrounded)
            {
                if (_endWaiting)
                {
                    ToWalking();
                    Game1.GameManager.StartDialogPath("mc_boss_enter");
                    return;
                }

                {
                    // the farther away the enemy is from the origin the more likely it becomes that he will move towards the center position
                    var directionToStart = _spawnPosition - EntityPosition.Position;
                    var radiusToCenter = MathF.Atan2(directionToStart.Y, directionToStart.X);

                    var maxDistanceX = 15.0f;
                    var maxDistanceY = 15.0f;
                    var distanceMultiplier = Math.Clamp(
                        Math.Min(
                            (maxDistanceX - Math.Abs(directionToStart.X)) / maxDistanceX,
                            (maxDistanceY - Math.Abs(directionToStart.Y)) / maxDistanceY), 0, 1);

                    var dir = radiusToCenter + (Math.PI - Game1.RandomNumber.Next(0, 628) / 100f) * distanceMultiplier;
                    _body.VelocityTarget = new Vector2((float)Math.Cos(dir), (float)Math.Sin(dir)) * 0.125f * Game1.TimeMultiplier;
                }

                _direction = EntityPosition.X < MapManager.ObjLink.EntityPosition.X ? 2 : 0;
                _animator.Play("idle_" + _direction);
                _body.Velocity = new Vector3(0, 0, 1.25f);
            }
        }

        private void ToWalking()
        {
            _aiComponent.ChangeState("walk");

            _damageField.PushMultiplier = 1.75f;
            _direction = EntityPosition.X < MapManager.ObjLink.EntityPosition.X ? 2 : 0;
            _animator.Play("idle_" + _direction);
            _body.Velocity = new Vector3(0, 0, 1.25f);
        }

        private void UpdateWalk()
        {
            // start new jump
            if (_body.IsGrounded)
            {
                // if we are on the same height as the player start attacking
                var distance = MapManager.ObjLink.EntityPosition.Y - EntityPosition.Y;
                if (Math.Abs(distance) < 16)
                {
                    _direction = EntityPosition.X < MapManager.ObjLink.EntityPosition.X ? 2 : 0;

                    if (Game1.RandomNumber.Next(0, 4) == 0)
                    {
                        ToThrowSpear();
                        return;
                    }
                    if (Game1.RandomNumber.Next(0, 4) == 0)
                    {
                        ToPreAttack();
                        return;
                    }
                }

                _animator.Play("idle_" + _direction);

                var targetPosition = new Vector2(MapManager.ObjLink.EntityPosition.X + (_direction == 0 ? 50 : -50), MapManager.ObjLink.EntityPosition.Y);
                _moveDirection = targetPosition - EntityPosition.Position;
                if (_moveDirection != Vector2.Zero)
                    _moveDirection.Normalize();
                _moveDirection *= 0.5f;

                _body.Velocity = new Vector3(0, 0, 1.25f);
            }

            _body.VelocityTarget = _moveDirection;
        }

        private void ToThrowSpear()
        {
            _aiComponent.ChangeState("spear");

            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("throw_" + _direction);
        }

        private void UpdateSpear()
        {
            if (!_animator.IsPlaying)
            {
                _animator.Play("idle_" + _direction);
                _aiComponent.ChangeState("postSpear");

                // spawn spear
                var spear = new EnemySpear(Map, new Vector3(EntityPosition.X, EntityPosition.Y - 9, 3), AnimationHelper.DirectionOffset[_direction] * 2f);
                Map.Objects.SpawnObject(spear);
            }
        }

        private void ToPreAttack()
        {
            _aiComponent.ChangeState("preAttack");

            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("arm_" + _direction);
        }

        private void UpdatePreAttack()
        {
            Game1.GameManager.PlaySoundEffect("D360-09-09", false);

            // start attacking
            if (!_animator.IsPlaying)
                ToAttack();
        }

        private void ToAttack()
        {
            _aiComponent.ChangeState("attack");

            _damageField.PushMultiplier = 3.5f;
            _body.VelocityTarget = new Vector2(_direction == 0 ? -1 : 1, 0) * 2.5f;
            _animator.Play("attack_" + _direction);
        }

        private void ToBounce()
        {
            _aiComponent.ChangeState("bounce");

            Game1.GameManager.PlaySoundEffect("D360-11-0B");
            _animator.Play("wall");

            _damageField.PushMultiplier = 1.75f;

            _body.VelocityTarget = Vector2.Zero;
            _body.Velocity = new Vector3(_direction == 0 ? 1.0f : -1.0f, 0, 1.75f);

            _bounceTime = Game1.TotalGameTime;

            // shake the screen
            Game1.GameManager.ShakeScreen(800, 4, 1, 5, 5);
        }

        private void UpdateBound()
        {
            if (_bounceTime + 1500 < Game1.TotalGameTime)
                ToLook();
        }

        private void ToLook()
        {
            _aiComponent.ChangeState("look");

            _animator.Play("look_" + _direction);
        }

        private void UpdateLook()
        {
            if (!_animator.IsPlaying)
                ToWalking();
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            Game1.GameManager.StartDialogPath("mc_boss_defeat");

            // spawn fairy
            Game1.GameManager.PlaySoundEffect("D360-27-1B");
            Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 8));

            Map.Objects.DeleteObjects.Add(this);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _bodyDrawComponent.Draw(spriteBatch);

            if (_aiComponent.CurrentStateId == "bounce" || _aiComponent.CurrentStateId == "damage")
            {
                var sourceRectangle = new Rectangle(188, 11, 4, 4);
                var distance = new Vector2((float)Math.Sin(Game1.TotalGameTime / 100f) * 8, (float)Math.Cos(Game1.TotalGameTime / 100f) * 3);

                spriteBatch.Draw(Resources.SprMidBoss, new Vector2(
                    EntityPosition.X + distance.X - 2, EntityPosition.Y - EntityPosition.Z - 18 + distance.Y - 2), sourceRectangle, Color.White);
                spriteBatch.Draw(Resources.SprMidBoss, new Vector2(
                    EntityPosition.X - distance.X - 2, EntityPosition.Y - EntityPosition.Z - 18 - distance.Y - 2), sourceRectangle, Color.White);
            }
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "damage")
                return Values.HitCollision.Enemy;
            if (_aiComponent.CurrentStateId != "bounce")
                return Values.HitCollision.RepellingParticle;

            Game1.GameManager.PlaySoundEffect("D370-07-07");

            _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            return Values.HitCollision.Enemy;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Horizontal) != 0 && _aiComponent.CurrentStateId == "attack")
                ToBounce();
        }
    }
}
