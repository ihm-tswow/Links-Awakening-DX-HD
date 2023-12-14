using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjMonkey : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerSwitch _hitCooldown;
        private readonly CSprite _sprite;
        private readonly AiTriggerSwitch _waitTimer;
        private readonly BodyDrawComponent _drawComponent;

        private ObjBowWow _bowWow;

        private readonly Vector2 _resetPosition;
        private readonly Vector2 _endPosition;

        // lives are used to fight with the bowwow
        private const int MaxLives = 5;
        private int _currentLives = MaxLives;

        private int _direction;

        private bool _initBusiness;
        private const int FadeTime = 150;

        private int _directionChangeCounter = 0;
        private float _damageCounter;
        private const int DamageTime = 400;

        public ObjMonkey() : base("monkey") { }

        public ObjMonkey(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            // already build the bridge?
            var value = Game1.GameManager.SaveManager.GetString("monkeyBusiness");
            if (value == "3")
            {
                IsDead = true;
                return;
            }

            _resetPosition = EntityPosition.Position;

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                MaxJumpHeight = 4f,
                DragAir = 0.99f,
                Drag = 0.85f,
                Gravity = -0.15f
            };

            var randomDir = (Game1.RandomNumber.Next(0, 50) / 50.0f) * MathF.PI * 2;
            _endPosition = new Vector2(EntityPosition.X + 8, EntityPosition.Y - 24) +
                           new Vector2(MathF.Sin(randomDir), MathF.Cos(randomDir)) * 150;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/monkey");
            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _hitCooldown = new AiTriggerSwitch(250);
            _waitTimer = new AiTriggerSwitch(150);

            var stateWaiting = new AiState(UpdateWaiting);
            var stateSitInit = new AiState();
            stateSitInit.Trigger.Add(new AiTriggerRandomTime(ToJump, 125, 1000));
            stateSitInit.Trigger.Add(_hitCooldown);
            var stateSit = new AiState(UpdateSit);
            stateSit.Trigger.Add(_hitCooldown);
            stateSit.Trigger.Add(new AiTriggerRandomTime(ToJump, 750, 1500));
            var stateJump = new AiState(UpdateJump);
            stateJump.Trigger.Add(_hitCooldown);
            var stateFlee = new AiState(UpdateFlee) { Init = ToFlee };
            var stateFleeSit = new AiState(UpdateFleeSit);
            stateFleeSit.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("flee"), 500, 1000));
            var stateReset = new AiState(UpdateReset);
            var stateBanana = new AiState(UpdateBusiness);
            stateBanana.Trigger.Add(new AiTriggerCountdown(1500, null, ToBusiness));
            var stateBusiness = new AiState(UpdateBusiness);
            stateBusiness.Trigger.Add(new AiTriggerCountdown(250, null, ChangeDirection));
            var stateLeave = new AiState(UpdateLeave);
            stateLeave.Trigger.Add(_waitTimer);
            var stateFade = new AiState();
            stateFade.Trigger.Add(new AiTriggerCountdown(FadeTime, TickFade, () => TickFade(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("sitInit", stateSitInit);
            _aiComponent.States.Add("sit", stateSit);
            _aiComponent.States.Add("jump", stateJump);
            _aiComponent.States.Add("flee", stateFlee);
            _aiComponent.States.Add("fleeSit", stateFleeSit);
            _aiComponent.States.Add("reset", stateReset);
            _aiComponent.States.Add("banana", stateBanana);
            _aiComponent.States.Add("business", stateBusiness);
            _aiComponent.States.Add("leave", stateLeave);
            _aiComponent.States.Add("fade", stateFade);

            // start by locking into a random direction
            _direction = Game1.RandomNumber.Next(0, 2);
            _animator.Play("idle_" + _direction);
            _aiComponent.ChangeState("waiting");

            _drawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer);

            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, OnInteract));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString("monkeyBusiness");

            if (!_initBusiness && value == "1")
            {
                _initBusiness = true;
                ToBanana();
            }
            else if (_aiComponent.CurrentStateId != "leave" && _aiComponent.CurrentStateId != "fade" && value == "3")
            {
                ToLeave();
            }
        }

        private bool OnInteract()
        {
            Game1.GameManager.StartDialogPath("castle_monkey");

            return true;
        }

        private void UpdateWaiting()
        {
            var distance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            if (distance.Length() < 24)
            {
                // start fighting with the bowwow
                var bowWowState = Game1.GameManager.SaveManager.GetString("bowWow");
                if (bowWowState == "2" || bowWowState == "3")
                {
                    _aiComponent.ChangeState("sit");
                    Game1.GameManager.StartDialogPath("castle_monkey");
                    Tags = Values.GameObjectTag.Enemy;

                    // search the bowwow
                    _bowWow = (ObjBowWow)Map.Objects.GetObjectOfType(
                        (int)EntityPosition.X - 80, (int)EntityPosition.Y - 40, 160, 160, typeof(ObjBowWow));
                }
            }
        }

        private void ToSit()
        {
            _aiComponent.ChangeState("sit");

            // stop and wait
            _body.Velocity = Vector3.Zero;

            _animator.Play("idle_" + _direction);
        }

        private void UpdateSit()
        {
            DamageTick();
        }

        private void ToJump()
        {
            _aiComponent.ChangeState("jump");

            Vector2 direction;

            // _bowWow should actually never be null...
            if (_bowWow != null)
            {
                if (!_body.FieldRectangle.Contains(_bowWow.EntityPosition.Position))
                {
                    _aiComponent.ChangeState("sit");
                    return;
                }

                // jump towards the bowwow
                direction = _bowWow.EntityPosition.Position - EntityPosition.Position;
                if (direction != Vector2.Zero)
                    direction.Normalize();
            }
            else
            {
                // change the direction
                var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
                direction = new Vector2(
                    (float)Math.Sin(rotation),
                    (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(25, 40) / 50f;
            }

            _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, 1.75f);

            _direction = direction.X < 0 ? 0 : 1;
            _animator.Play("jump_" + _direction);
        }

        private void UpdateJump()
        {
            DamageTick();

            // finished jumping
            if (_body.IsGrounded)
                ToSit();
        }

        private void ToFlee()
        {
            if (EntityPosition.Y < _resetPosition.Y - 90)
            {
                _aiComponent.ChangeState("reset");
                _animator.Play("idle_" + _direction);
                Tags = Values.GameObjectTag.None;
                return;
            }

            _direction = EntityPosition.X < _resetPosition.X ? 1 : 0;
            // jump up
            _body.Velocity = new Vector3(_direction == 0 ? -0.5f : 0.5f, -1, 2.0f);
            _animator.Play("jump_" + _direction);

            _body.CollisionTypes = Values.CollisionTypes.None;
        }

        private void UpdateFlee()
        {
            DamageTick();
        }

        private void UpdateFleeSit()
        {
            _animator.Play("idle_u_" + _direction);
        }

        private void UpdateReset()
        {
            var distance = MapManager.ObjLink.EntityPosition.Position - _resetPosition;

            // come back to the start position?
            if (distance.Length() > 128)
            {
                _currentLives = MaxLives;
                EntityPosition.Set(_resetPosition);
                _aiComponent.ChangeState("waiting");

                _damageCounter = 0;
                _body.CollisionTypes = Values.CollisionTypes.Normal |
                                       Values.CollisionTypes.NPCWall;
            }
        }

        private void ToBanana()
        {
            _animator.Play("jump_1");
            _aiComponent.ChangeState("banana");

            Game1.GameManager.PlaySoundEffect("D360-01-01");
        }

        private void ToBusiness()
        {
            Game1.GameManager.StartDialogPath("castle_monkey_business");
            _aiComponent.ChangeState("business");
            _animator.Play("idle_1");
            _direction = 1;
        }

        private void UpdateBusiness()
        {
            // freeze the player while the big business is happening
            MapManager.ObjLink.FreezePlayer();

            Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;
        }

        private void ChangeDirection()
        {
            if (_directionChangeCounter < 3)
            {
                _directionChangeCounter++;
                _direction = (_direction + 1) % 2;
                _animator.Play("idle_" + _direction);
            }

            // done to reset the direction change trigger
            _aiComponent.ChangeState("business");
        }

        private void ToLeave()
        {
            _body.CollisionTypes = Values.CollisionTypes.None;
            _aiComponent.ChangeState("leave");
        }

        private void UpdateLeave()
        {
            if (!_body.IsGrounded || !_waitTimer.State)
                return;

            var direction = _endPosition - EntityPosition.Position;
            var distance = direction.Length();

            direction.Normalize();
            var strength = Game1.RandomNumber.Next(150, 200) / 100.0f;
            _body.Velocity = new Vector3(direction.X * strength, direction.Y * strength, 1.75f);

            _direction = direction.X < 0 ? 0 : 1;
            _animator.Play("jump_" + _direction);

            // start fading away
            if (distance < 48)
                _aiComponent.ChangeState("fade");
        }

        private void TickFade(double time)
        {
            _sprite.Color = Color.White * (float)(time / FadeTime);

            // delete the monkey after it is faded away
            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void DamageTick()
        {
            if (_damageCounter > 0)
                _damageCounter -= Game1.DeltaTime;
            _sprite.SpriteShader = _damageCounter % 133 > 66 ? Resources.DamageSpriteShader0 : null;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _drawComponent.Draw(spriteBatch);

            if (_aiComponent.CurrentStateId != "banana")
                return;

            // draw the banana
            var sourceRectangle = Game1.GameManager.ItemManager["trade3"].SourceRectangle;
            spriteBatch.Draw(Resources.SprItem, new Vector2(EntityPosition.X - 8, EntityPosition.Y - 30), sourceRectangle, Color.White);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_hitCooldown.State || damageType != HitType.BowWow)
                return Values.HitCollision.None;

            Game1.GameManager.PlaySoundEffect("D360-03-03");

            // fighting with the great bowwow?
            _damageCounter = DamageTime;

            _currentLives--;
            if (_currentLives <= 0)
                _aiComponent.ChangeState("flee");

            _hitCooldown.Reset();

            _body.Velocity.X += direction.X * 4.0f;
            _body.Velocity.Y += direction.Y * 4.0f;

            return Values.HitCollision.Blocking;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            // finished jumping?
            if (_aiComponent.CurrentStateId == "leave" && moveCollision.HasFlag(Values.BodyCollision.Floor))
            {
                _waitTimer.Reset();

                if (_body.Velocity.Y > 0)
                    _animator.Play("idle_" + _direction);
                else
                    _animator.Play("idle_u_" + _direction);

                _body.Velocity = Vector3.Zero;
            }

            if (_aiComponent.CurrentStateId == "flee" && moveCollision.HasFlag(Values.BodyCollision.Floor))
                _aiComponent.ChangeState("fleeSit");

            if (_aiComponent.CurrentStateId != "jump")
                return;

            // repel after wall collision
            if (moveCollision.HasFlag(Values.BodyCollision.Horizontal))
                _body.Velocity.X *= -0.25f;
            else if (moveCollision.HasFlag(Values.BodyCollision.Vertical))
                _body.Velocity.Y *= -0.25f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.25f, _body.Velocity.Z);

            return true;
        }
    }
}