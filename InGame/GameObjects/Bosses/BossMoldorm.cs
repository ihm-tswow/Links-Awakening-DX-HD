using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossMoldorm : GameObject
    {
        private BossMoldormTail _tail;

        private BodyDrawComponent _bodyDrawComponent;
        private BodyComponent _body;
        private AiComponent _aiComponent;
        private CSprite _sprite;

        private Rectangle _headSourceRectangle = new Rectangle(2, 4, 28, 24);
        private Rectangle _headSourceRectangleDamage = new Rectangle(34, 4, 28, 24);

        private Rectangle[] _tailRectangles = {
            new Rectangle(8, 40, 16, 16), new Rectangle(8, 40, 16, 16), new Rectangle(41, 41, 14, 14)
        };
        private Rectangle[] _tailRectanglesDamage = {
            new Rectangle(8, 72, 16, 16), new Rectangle(8, 72, 16, 16), new Rectangle(41, 73, 14, 14)
        };

        private Vector2[] _tailPositions = new Vector2[4];
        private float[] _tailDistance = { 6.5f, 4.5f, 4.5f, 4.5f };

        private Vector2[] _savedPosition = new Vector2[20];

        private string _saveKey;
        private string _triggerKey;

        private float _saveInterval = 33;
        private float _saveCounter;
        private int _saveIndex;

        private float _directionChangeMultiplier;
        private float _direction;
        private float _runCounter;
        private int _changeDirCount;
        private int _dir = 1;
        private int _lives = 4;

        private int _dyingState = 4;
        private int _tailState = 2;
        private float _dyingCounter = 250;

        private bool _blinking;

        public BossMoldorm() : base("moldorm") { }

        public BossMoldorm(Map.Map map, int posX, int posY, string saveKey, string triggerKey) : base(map)
        {
            if (!string.IsNullOrEmpty(saveKey) &&
                Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 16, posY + 16, 0);
            EntitySize = new Rectangle(-24, -24, 48, 48);

            for (var i = 0; i < 4; i++)
                _tailPositions[i] = EntityPosition.Position;
            for (var i = 0; i < _savedPosition.Length; i++)
                _savedPosition[i] = EntityPosition.Position;

            _saveKey = saveKey;
            _triggerKey = triggerKey;

            _sprite = new CSprite(EntityPosition);
            _sprite.SprTexture = Resources.SprNightmares;
            _sprite.SourceRectangle = _headSourceRectangle;
            _sprite.Center = new Vector2(16, 12);

            _direction = (float)Math.PI * 2;
            _sprite.Rotation = -_direction - (float)Math.PI / 2;

            _body = new BodyComponent(EntityPosition, -7, -7, 14, 14, 8)
            {
                MoveCollision = OnCollision,
                AbsorbPercentage = 1f,
                Gravity = -0.1f,
                DragAir = 1.0f,
                Drag = 0.925f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Hole,
                FieldRectangle = map.GetField(posX, posY)
            };

            _aiComponent = new AiComponent();

            var stateWaiting = new AiState();
            var stateMoving = new AiState(UpdateMoving);
            var stateRunning = new AiState(UpdateRunning);
            var stateDamage = new AiState(UpdateDamage);
            stateDamage.Trigger.Add(new AiTriggerCountdown(500, UpdateDamageTick, ToRunning));
            var stateDying = new AiState(UpdateDying);

            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("running", stateRunning);
            _aiComponent.States.Add("damage", stateDamage);
            _aiComponent.States.Add("dying", stateDying);

            _aiComponent.ChangeState("waiting");

            _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, 1);
            var damageCollider = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChang));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 2.0f });
            AddComponent(HittableComponent.Index, new HittableComponent(damageCollider, OnHit));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));

            // add the tail to the map
            _tail = new BossMoldormTail(map, this);
            map.Objects.SpawnObject(_tail);
        }

        private void ToWalking()
        {
            _aiComponent.ChangeState("moving");
            _sprite.SourceRectangle = _headSourceRectangle;
            _blinking = false;
        }

        private void UpdateMoving()
        {
            Move(1);
            UpdateTailPositions(1);
        }

        private void ToRunning()
        {
            _aiComponent.ChangeState("running");
            _sprite.SourceRectangle = _headSourceRectangle;
            _blinking = false;
        }

        private void UpdateRunning()
        {
            // dont stop running with one live left
            if (_lives > 1)
            {
                _runCounter -= Game1.DeltaTime;
                if (_runCounter <= 0)
                    _aiComponent.ChangeState("moving");
            }

            Move(1.75f);
            UpdateTailPositions(1.75f);
        }

        private void Move(float speedMultiplier)
        {
            _changeDirCount -= (int)(Game1.DeltaTime * speedMultiplier);
            if (_changeDirCount < 0)
                ChangeDirection();

            _direction += _dir * 0.05f * Game1.TimeMultiplier * speedMultiplier;

            _sprite.Rotation = -_direction - (float)Math.PI / 2;

            // move
            var vecDirection = new Vector2((float)Math.Sin(_direction), (float)Math.Cos(_direction));
            _body.VelocityTarget = vecDirection * 1.0f * speedMultiplier;

            _directionChangeMultiplier = AnimationHelper.MoveToTarget(_directionChangeMultiplier, 1, 0.1f * Game1.TimeMultiplier);
        }

        private void UpdateDamage()
        {
            _body.VelocityTarget = Vector2.Zero;
            UpdateTailPositions(1.5f);
        }

        private void UpdateDamageTick(double time)
        {
            _blinking = (time % 150) >= 75;

            _direction += _dir * (float)Math.Sin(((time + 150) / 800f) * Math.PI) * 0.1f * Game1.TimeMultiplier;
            _sprite.Rotation = -_direction - (float)Math.PI / 2;
            _sprite.SourceRectangle = _blinking ? _headSourceRectangleDamage : _headSourceRectangle;
        }

        private void ToDying()
        {
            _body.Velocity = new Vector3(_body.VelocityTarget.X, _body.VelocityTarget.Y, _body.Velocity.Z);
            _body.VelocityTarget = Vector2.Zero;

            _aiComponent.ChangeState("dying");
            Game1.GameManager.PlaySoundEffect("D370-16-10");
        }

        private void UpdateDying()
        {
            UpdateTailPositions(_body.Velocity.Length());

            _dyingCounter -= Game1.DeltaTime;

            if (_dyingCounter <= 0)
            {
                _dyingCounter = 250;
                _dyingState--;

                if (_tail != null)
                {
                    Map.Objects.DeleteObjects.Add(_tail);
                    _tail = null;
                }
                else
                    _tailState--;

                if (_dyingState < -10)
                {
                    Map.Objects.DeleteObjects.Add(this);

                    // stop boss music
                    Game1.GameManager.SetMusic(-1, 2);

                    // set the save key
                    Game1.GameManager.SaveManager.SetString(_saveKey, "1");

                    // spawn big heart
                    Map.Objects.SpawnObject(new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, "j", "d1_nHeart", "heartMeterFull", null));

                    Game1.GameManager.PlaySoundEffect("D378-26-1A");
                }
                else if (_dyingState < 0)
                {
                    _dyingCounter = 50;
                    Game1.GameManager.PlaySoundEffect("D378-19-13");

                    // spawn explosion effect arount the head
                    var position = new Point((int)(Math.Sin(-_dyingState / 1.5f) * 8), (int)(Math.Cos(-_dyingState / 1.5f) * 8));
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)EntityPosition.X - 8 + position.X,
                        (int)EntityPosition.Y - 8 + position.Y, Values.LayerTop, "Particles/spawn", "run", true));
                }
                else
                {
                    Game1.GameManager.PlaySoundEffect("D378-19-13");

                    // spawn explosion at the tail
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)_tailPositions[_dyingState].X - 8,
                        (int)_tailPositions[_dyingState].Y - 8, Values.LayerTop, "Particles/spawn", "run", true));
                }
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the tail
            var tailRectangles = _blinking ? _tailRectanglesDamage : _tailRectangles;
            for (var i = _tailState; i >= 0; i--)
            {
                spriteBatch.Draw(Resources.SprNightmares, _tailPositions[i] -
                    new Vector2(tailRectangles[i].Width / 2f, tailRectangles[i].Height / 2f), tailRectangles[i], Color.White);
            }

            // draw the head
            _bodyDrawComponent.Draw(spriteBatch);
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }

        public Values.HitCollision OnHitTail(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "damage" || _aiComponent.CurrentStateId == "dying")
                return Values.HitCollision.None;

            // can only be damaged with the sword
            if ((damageType & HitType.Sword) == 0)
                return Values.HitCollision.None;

            _lives -= damage;
            _runCounter = 2000;

            Game1.GameManager.PlaySoundEffect("D370-07-07");

            if (_lives > 0)
                _aiComponent.ChangeState("damage");
            else
                ToDying();

            return Values.HitCollision.Enemy;
        }

        private void UpdateTailPositions(float speedMultiplier)
        {
            SavePosition(speedMultiplier);

            var indexCount = _saveIndex + (_saveCounter / _saveInterval);
            var timeDiff = _saveCounter + _saveInterval * 1000;

            for (var i = 0; i < _tailPositions.Length; i++)
            {
                indexCount -= _tailDistance[i];
                if (indexCount < 0)
                    indexCount += _savedPosition.Length;

                timeDiff -= _tailDistance[i] * _saveInterval;
                var index = (int)indexCount;

                _tailPositions[i] = Vector2.Lerp(
                    _savedPosition[index], _savedPosition[(index + 1) % _savedPosition.Length],
                    (timeDiff % _saveInterval) / _saveInterval);
            }

            // set the position of the tail
            _tail?.EntityPosition.Set(_tailPositions[_tailPositions.Length - 1]);
        }

        private void SavePosition(float speedMultiplier)
        {
            var position = EntityPosition.Position + _body.VelocityTarget * (Game1.DeltaTime / 16.6667f) * speedMultiplier;
            _saveCounter += Game1.DeltaTime * speedMultiplier;
            var diff = _saveCounter % _saveInterval;

            var updateSteps = (int)(_saveCounter / _saveInterval);
            _saveIndex = (_saveIndex + updateSteps) % _savedPosition.Length;
            var index = _saveIndex;

            var currentDirection = _direction;

            while (_saveCounter >= _saveInterval)
            {
                _saveCounter -= _saveInterval;

                index--;
                if (index < 0)
                    index = _savedPosition.Length - 1;

                var vecDir = new Vector2((float)Math.Sin(currentDirection), (float)Math.Cos(currentDirection));
                _savedPosition[index] = position - vecDir * (diff / 16.6667f);

                position = _savedPosition[index];
                diff = _saveInterval;
                currentDirection -= _dir * 0.025f * (_saveInterval / 16.6667f);
            }
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (Game1.RandomNumber.Next(0, 2) == 0)
                _dir = -_dir;

            if ((collision & Values.BodyCollision.Horizontal) != 0)
                _direction = (float)Math.Atan2(-_body.VelocityTarget.X * _directionChangeMultiplier, _body.VelocityTarget.Y);
            else if ((collision & Values.BodyCollision.Vertical) != 0)
                _direction = (float)Math.Atan2(_body.VelocityTarget.X, -_body.VelocityTarget.Y * _directionChangeMultiplier);

            _directionChangeMultiplier *= 0.75f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 0.25f, direction.Y * 0.25f, _body.Velocity.Z);

            return true;
        }

        private void ChangeDirection()
        {
            _changeDirCount = Game1.RandomNumber.Next(500, 2500);
            _dir = -_dir;
        }

        private void OnKeyChang()
        {
            // activate the boss if the trigger key was set
            if (_aiComponent.CurrentStateId == "waiting" &&
                !string.IsNullOrEmpty(_triggerKey) && Game1.GameManager.SaveManager.GetString(_triggerKey) == "1")
            {
                _aiComponent.ChangeState("moving");
            }
        }
    }
}