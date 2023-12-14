using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MBossCueBall : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Animator _animation;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;

        private const float MovementSpeed = 1.25f;

        private string _saveKey;
        private string _enterKey;

        private int _spinStart;
        private float _spinCounter;
        private float _spinTime;
        private const float SpinStepTime = 66.66f;

        private int Lives = 8;
        private int _count;
        private int _moveClockwise = 1;
        private int _moveDirection;
        private int _lastFrameIndex;

        private bool _entered;

        public MBossCueBall() : base("cue ball") { }

        public MBossCueBall(Map.Map map, int posX, int posY, string saveKey, string enterKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 16, posY + 16, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _saveKey = saveKey;
            _enterKey = enterKey;

            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animation = AnimatorSaveLoad.LoadAnimator("MidBoss/cue ball");
            _animation.Play("move_2");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animation, _sprite, new Vector2(-16, -16));

            _body = new BodyComponent(EntityPosition, -16, -16, 32, 32, 8)
            {
                IgnoreHoles = true,
                IgnoresZ = true,
                Drag = 0.9f,
                MoveCollision = OnCollision,
            };

            var stateWaiting = new AiState();
            stateWaiting.Trigger.Add(new AiTriggerCountdown(400, null, ToMoving));
            var stateMoving = new AiState(UpdateMoving);
            var stateSpinning = new AiState(UpdateSpinning);
            var stateDead = new AiState() { Init = InitDeath };

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("spinning", stateSpinning);
            _aiComponent.States.Add("dead", stateDead);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, false, false)
            { HitMultiplierX = 0, HitMultiplierY = 0, OnDeath = OnDeath, ExplosionOffsetY = 16, BossHitSound = true };
            _aiDamageState.AddBossDamageState(OnDeathEnd);

            ToMoving();

            var damageCollider = new CBox(EntityPosition, -14, -14, 0, 28, 28, 2);
            if (!string.IsNullOrEmpty(enterKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { CooldownTime = 50 });
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void OnKeyChange()
        {
            if (!_entered)
            {
                var strEnterKey = Game1.GameManager.SaveManager.GetString(_enterKey, "0");
                if (strEnterKey == "1")
                {
                    _entered = true;
                    Game1.GameManager.SetMusic(79, 2);
                }
            }
        }

        private void ToMoving()
        {
            _aiComponent.ChangeState("moving");

            _moveDirection = (_moveDirection + _moveClockwise) % 4;
            if (_moveDirection < 0)
                _moveDirection += 4;

            _animation.Play("move_" + _moveDirection);

            _body.VelocityTarget = AnimationHelper.DirectionOffset[_moveDirection] * MovementSpeed;
            _lastFrameIndex = 1;
        }

        private void UpdateMoving()
        {
            // spawn particle?
            if (_lastFrameIndex == 1 && _lastFrameIndex != _animation.CurrentFrameIndex)
            {
                var posX = EntityPosition.X + AnimationHelper.DirectionOffset[_moveDirection].X * 22;
                var posY = EntityPosition.Y + AnimationHelper.DirectionOffset[_moveDirection].Y * 22;

                // spawn splash effect
                Map.Objects.SpawnObject(new ObjAnimator(Map,
                    (int)posX, (int)posY, Values.LayerPlayer, "Particles/big_water_splash", "run_" + _moveDirection, true));

                if (_entered)
                    Game1.GameManager.PlaySoundEffect("D378-47-2F");
            }

            _lastFrameIndex = _animation.CurrentFrameIndex;
        }

        private void ToWaiting()
        {
            _aiComponent.ChangeState("waiting");

            _animation.Pause();
            _body.VelocityTarget = Vector2.Zero;
        }

        private void ToSpinning()
        {
            _aiComponent.ChangeState("spinning");

            // rotate 5 or 5.5 times
            _spinStart = _moveDirection;
            _spinCounter = 0;
            _spinTime = SpinStepTime * 5.75f * 4;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void UpdateSpinning()
        {
            _spinCounter += Game1.DeltaTime;
            if (_spinCounter >= _spinTime)
            {
                _moveDirection += _moveClockwise;
                _moveClockwise = -_moveClockwise;
                ToMoving();
                return;
            }

            var counterDir = (int)(_spinCounter / SpinStepTime);
            var spinDirection = (_spinStart + _moveClockwise * counterDir) % 4;
            if (spinDirection < 0)
                spinDirection += 4;

            if (spinDirection != _moveDirection)
                _count++;

            // spawn splash effect
            if (spinDirection != _moveDirection && _count % 3 == 0)
            {
                Game1.GameManager.PlaySoundEffect("D378-47-2F");

                var offset = 22;
                if (spinDirection == 0)
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)EntityPosition.X - offset, (int)EntityPosition.Y, Values.LayerPlayer, "Particles/big_water_splash", "run_0", true));
                else if (spinDirection == 1)
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)EntityPosition.X, (int)EntityPosition.Y - offset, Values.LayerPlayer, "Particles/big_water_splash", "run_1", true));
                else if (spinDirection == 2)
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)EntityPosition.X + offset, (int)EntityPosition.Y, Values.LayerPlayer, "Particles/big_water_splash", "run_2", true));
                else if (spinDirection == 3)
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)EntityPosition.X, (int)EntityPosition.Y + offset, Values.LayerPlayer, "Particles/big_water_splash", "run_3", true));
            }

            _moveDirection = spinDirection;
            _animation.Play("move_" + _moveDirection);
        }

        private void InitDeath()
        {
            // stop moving
            _body.Velocity.X = _body.VelocityTarget.X;
            _body.Velocity.Y = _body.VelocityTarget.Y;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void OnDeath(bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId != "death")
                _aiComponent.ChangeState("death");
        }

        private void OnDeathEnd()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Game1.GameManager.PlaySoundEffect("D378-26-1A");

            Game1.GameManager.PlaySoundEffect("D360-27-1B");
            Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 8));

            Map.Objects.DeleteObjects.Add(this);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId == "moving")
                ToWaiting();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_entered || _aiDamageState.IsInDamageState() || _aiDamageState.CurrentLives <= 0)
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId == "spinning")
                return Values.HitCollision.RepellingParticle;

            var dir = AnimationHelper.GetDirection(direction);
            dir = (dir + 2) % 4;
            if (dir == _moveDirection)
                return Values.HitCollision.RepellingParticle;

            _aiDamageState.OnHit(gameObject, direction, damageType, damage, false);

            // star spinning
            if (0 < _aiDamageState.CurrentLives)
                ToSpinning();
            else
                _aiComponent.ChangeState("dead");

            return Values.HitCollision.Enemy;
        }
    }
}
