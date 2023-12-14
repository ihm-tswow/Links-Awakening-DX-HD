using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;
using System.Collections.Generic;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class MBossTurtleRock : GameObject
    {
        // list of sprite that are in the area of the turtle; this is used to set the visibility because normal layers do not work with the turtle head
        private readonly List<GameObject> _spriteList = new List<GameObject>();

        private readonly DictAtlasEntry _stoneHead;
        private readonly Rectangle[] _headParts = new Rectangle[6];
        private readonly Vector2[] _headPartOffset = new Vector2[6];

        private readonly Animator _animator;
        private readonly AnimationComponent _animationComponent;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _aiDamageState;

        private readonly DictAtlasEntry _spriteNeck;

        private readonly Vector2 _startPosition;
        private readonly Vector2 _centerPosition;

        private const float AttackSpeed = 1.5f;
        private const float ReturnSpeed = 0.5f;
        private const int WobbleTime = 140;

        private Vector3[] _partPosition = new Vector3[6];
        private Vector3[] _partVelocity = new Vector3[6];
        private int[] _partBreakOrder = new[] { 0, 5, 3, 4, 1, 2 };
        private int _partBreakIndex;
        private float _partCounter = -500;

        private string _saveKey;
        private bool _attackable = false;

        public MBossTurtleRock() : base("turtle rock") { }

        public MBossTurtleRock(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            // do not spawn if the tutle was already killed
            _saveKey = saveKey;
            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            _startPosition = new Vector2(posX + 24, posY + 16);
            _centerPosition = new Vector2(_startPosition.X, _startPosition.Y + 32);

            EntityPosition = new CPosition(_startPosition.X, _startPosition.Y, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _body = new BodyComponent(EntityPosition, -8, 0, 16, 16, 8)
            {
                CollisionTypes = Values.CollisionTypes.None
            };

            _spriteNeck = Resources.GetSprite("turtle neck");
            _stoneHead = Resources.GetSprite("turtle rock");

            // the top gets split into 4 parts with the middle parts beeing 8px wide
            // the bottom is split into two parts
            var headWidth = (_stoneHead.SourceRectangle.Width - 16) / 2;
            var headHeight = _stoneHead.SourceRectangle.Height / 2;

            _headPartOffset[0] = new Vector2(0, 0);
            _headPartOffset[1] = new Vector2(headWidth, 0);
            _headPartOffset[2] = new Vector2(headWidth + 8, 0);
            _headPartOffset[3] = new Vector2(headWidth + 16, 0);
            _headPartOffset[4] = new Vector2(0, headHeight);
            _headPartOffset[5] = new Vector2(_stoneHead.SourceRectangle.Width / 2, headHeight);

            _headParts[0] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[0].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[0].Y, headWidth, headHeight);
            _headParts[1] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[1].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[1].Y, 8, headHeight);
            _headParts[2] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[2].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[2].Y, 8, headHeight);
            _headParts[3] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[3].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[3].Y, headWidth, headHeight);
            _headParts[4] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[4].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[4].Y, _stoneHead.SourceRectangle.Width / 2, headHeight);
            _headParts[5] = new Rectangle(_stoneHead.SourceRectangle.X + (int)_headPartOffset[5].X, _stoneHead.SourceRectangle.Y + (int)_headPartOffset[5].Y, _stoneHead.SourceRectangle.Width / 2, headHeight);

            // set the part position
            for (var i = 0; i < _partPosition.Length; i++)
                _partPosition[i] = new Vector3(EntityPosition.X + _headPartOffset[i].X, EntityPosition.Y + _headPartOffset[i].Y, 0);

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/turtle rock");
            _animator.Play("stone");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(0, -16));

            _aiComponent = new AiComponent();
            // deal damage at the neck positions
            _aiComponent.Trigger.Add(new AiTriggerUpdate(UpdateDamageNeck));

            var stateStone = new AiState();
            var stateWobble = new AiState();
            stateWobble.Trigger.Add(new AiTriggerCountdown(WobbleTime * 18, TickWobble, WobbleEnd));
            var stateBreak = new AiState(UpdateBreak) { Init = InitBreak };
            var stateOpenEyes = new AiState(UpdateEyeOpening) { Init = InitOpenEyes };
            var stateCome = new AiState(UpdateCome) { Init = InitCome };
            var stateInitWait = new AiState();
            stateInitWait.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("move")));
            var stateMove = new AiState(UpdateMove) { Init = InitMove };
            var stateWait = new AiState();
            stateWait.Trigger.Add(new AiTriggerCountdown(150, null, () => _aiComponent.ChangeState("move")));
            var statePreAttack = new AiState();
            statePreAttack.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("attack")));
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };
            stateAttack.Trigger.Add(new AiTriggerCountdown(600, null, () => _aiComponent.ChangeState("return")));
            var stateReturn = new AiState(UpdateReturn);
            var stateDead = new AiState();

            _aiComponent.States.Add("stone", stateStone);
            _aiComponent.States.Add("wobble", stateWobble);
            _aiComponent.States.Add("break", stateBreak);
            _aiComponent.States.Add("openEyes", stateOpenEyes);
            _aiComponent.States.Add("come", stateCome);
            _aiComponent.States.Add("initWait", stateInitWait);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("preAttack", statePreAttack);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("return", stateReturn);
            _aiComponent.States.Add("dead", stateDead);

            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 16, false, false, AiDamageState.BlinkTime * 6)
            {
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                ExplosionOffsetY = 16
            };
            _aiDamageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("stone");

            var damageCollider = new CBox(EntityPosition, -6, -16, 0, 12, 30, 8);
            var hittableBox = new CBox(EntityPosition, -6, -16, 0, 12, 30, 8);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(OcarinaListenerComponent.Index, new OcarinaListenerComponent(OnSongPlayed));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { IsActive = false });
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(EntityPosition, -8, 0, 16, 14, 8), Values.CollisionTypes.Enemy));

        }

        public override void Init()
        {
            Map.Objects.GetComponentList(_spriteList, (int)EntityPosition.X - 8 - 16 * 3, (int)EntityPosition.Y - 16, 16 * 7, 16 * 5, DrawComponent.Mask);
            UpdateSpriteLayers();
        }

        private void UpdateDamageNeck()
        {
            var startOffset = EntityPosition.Position - _startPosition;
            var partCount = (int)((startOffset.Y + 4) / 16);
            for (var i = 0; i < partCount; i++)
            {
                var percentage = 1 - (startOffset.Y - i * 16) / (startOffset.Y + 8);
                var offset = 1 - MathF.Sin(percentage * MathF.PI / 2);
                var position = new Vector2(_startPosition.X - 8 + startOffset.X * offset, EntityPosition.Y - i * 16 - 32);
                var damageBox = new ProjectZ.Base.Box(position.X, position.Y, 0, 16, 16, 8);
                var playerDamageBox = MapManager.ObjLink.DamageCollider.Box;
                var direction = playerDamageBox.Center - damageBox.Center;
                if (direction != Vector2.Zero)
                    direction.Normalize();
                direction *= 1.5f;

                if (damageBox.Intersects(playerDamageBox))
                    MapManager.ObjLink.HitPlayer(direction, HitType.Sword, 2, false, ObjLink.CooldownTime / 4);
            }
        }

        private void UpdateReturn()
        {
            // return to the start position
            var playerDirection = _centerPosition - EntityPosition.Position;
            if (playerDirection.Length() > ReturnSpeed * Game1.TimeMultiplier)
            {
                playerDirection.Normalize();
                _body.VelocityTarget = playerDirection * ReturnSpeed;
            }
            else
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("move");
            }

            UpdateSpriteLayers();
        }

        private void InitAttack()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var angle = MathF.Atan2(playerDirection.Y, playerDirection.X);
            angle = MathHelper.Clamp(angle, 0, MathF.PI);

            _body.VelocityTarget = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * AttackSpeed;
        }

        private void UpdateAttack()
        {
            UpdateSpriteLayers();
        }

        private void InitMove()
        {
            _attackable = true;
            // move to the left or to the right?
            _body.VelocityTarget.X = 0.5f;
            if (EntityPosition.X > _startPosition.X)
                _body.VelocityTarget *= -1;
        }

        private void UpdateMove()
        {
            // finished moving?
            if (_body.VelocityTarget.X < 0 && EntityPosition.X <= _startPosition.X - 15)
            {
                EntityPosition.Set(new Vector2(_startPosition.X - 15, EntityPosition.Y));
                EndMove();
            }
            if (_body.VelocityTarget.X > 0 && EntityPosition.X >= _startPosition.X + 15)
            {
                EntityPosition.Set(new Vector2(_startPosition.X + 15, EntityPosition.Y));
                EndMove();
            }

            UpdateSpriteLayers();
        }

        private void EndMove()
        {
            _body.VelocityTarget.X = 0;

            if (Game1.RandomNumber.Next(0, 2) == 0)
                _aiComponent.ChangeState("wait");
            else
                _aiComponent.ChangeState("preAttack");
        }

        private void InitCome()
        {
            _body.VelocityTarget.Y = 0.25f;
        }

        private void UpdateCome()
        {
            UpdateSpriteLayers();

            // come out of the hole
            if (EntityPosition.Y > _startPosition.Y + 32)
            {
                _body.VelocityTarget.Y = 0;
                EntityPosition.Set(new Vector2(_startPosition.X, _startPosition.Y + 32));
                _aiComponent.ChangeState("initWait");
            }
        }

        private void InitOpenEyes()
        {
            // start opening the eyes
            _animator.Play("open");
        }

        private void UpdateEyeOpening()
        {
            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("come");
            }
        }

        private void InitBreak()
        {
            _animator.Play("closed");
        }

        private void UpdateBreak()
        {
            _partCounter += Game1.DeltaTime;

            // break away
            if (_partCounter > _partBreakIndex * 500)
            {
                if (_partBreakIndex >= 6)
                {
                    _aiComponent.ChangeState("openEyes");
                    return;
                }

                Game1.GameManager.PlaySoundEffect("D378-19-13");

                var partIndex = _partBreakOrder[_partBreakIndex];
                _partVelocity[partIndex] = new Vector3(0.45f, 0, 1.5f);
                if (partIndex == 0 || partIndex == 1 || partIndex == 4)
                    _partVelocity[partIndex].X *= -1;

                _partBreakIndex++;
            }

            // move the part
            for (var i = 0; i < _partBreakIndex; i++)
            {
                var partIndex = _partBreakOrder[i];
                _partPosition[partIndex] += _partVelocity[partIndex] * Game1.TimeMultiplier;
                _partVelocity[partIndex].Z -= 0.25f * Game1.TimeMultiplier;
            }
        }

        private void UpdateSpriteLayers()
        {
            var rectangle = new Rectangle((int)EntityPosition.X - 8, (int)EntityPosition.Y - 16, 16, 32);

            foreach (var sprite in _spriteList)
            {
                if (sprite is ObjSprite)
                {
                    if (!rectangle.Contains(sprite.EntityPosition.Position))
                    {
                        ((DrawComponent)sprite.Components[DrawComponent.Index]).Layer = Values.LayerPlayer;
                        ((DrawShadowComponent)sprite.Components[DrawShadowComponent.Index]).IsActive = true;
                    }
                    else
                    {
                        ((DrawComponent)sprite.Components[DrawComponent.Index]).Layer = Values.LayerBottom;
                        ((DrawShadowComponent)sprite.Components[DrawShadowComponent.Index]).IsActive = false;
                    }
                }
            }
        }

        private void TickWobble(double counter)
        {
            _animationComponent.SpriteOffset.X = MathF.Sin((float)(counter / WobbleTime) * 2 * MathF.PI);
            _animationComponent.UpdateSprite();
        }

        private void WobbleEnd()
        {
            _animationComponent.SpriteOffset.X = 0;
            _aiComponent.ChangeState("break");
        }

        private void OnDeath()
        {
            Game1.GameManager.PlaySoundEffect("D378-12-0C");

            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Map.Objects.DeleteObjects.Add(this);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _sprite.SpriteShader);
            }

            // draw the nack while moving around outside
            var startOffset = EntityPosition.Position - _startPosition;
            var partCount = (int)((startOffset.Y + 4) / 16);
            for (var i = 0; i < partCount; i++)
            {
                var percentage = 1 - (startOffset.Y - i * 16) / (startOffset.Y + 8);
                var offset = 1 - MathF.Sin(percentage * MathF.PI / 2);
                var position = new Vector2(_startPosition.X - 8 + startOffset.X * offset, EntityPosition.Y - i * 16 - 32);
                DrawHelper.DrawNormalized(spriteBatch, _spriteNeck, position, Color.White);
            }

            // draw the head
            _sprite.Draw(spriteBatch);

            // draw the head parts
            if (_aiComponent.CurrentStateId != "wobble")
                for (var i = 0; i < _partPosition.Length; i++)
                {
                    if (_partPosition[i].Z < 0)
                        continue;

                    var position = new Vector2(_partPosition[i].X - 14, _partPosition[i].Y - _partPosition[i].Z - 16);
                    DrawHelper.DrawNormalized(spriteBatch, _stoneHead.Texture, position, _headParts[i], Color.White);
                }

            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_aiDamageState.CurrentLives <= 0)
                return Values.HitCollision.None;

            // can only be hit after beeing spawned
            if (!_attackable || ((type & HitType.Sword) == 0))
                return Values.HitCollision.RepellingParticle;

            // close the eyes for a short time
            if (!_aiDamageState.IsInDamageState())
            {
                Game1.GameManager.PlaySoundEffect("D370-07-07");
                _animator.Play("damaged");
            }

            _aiDamageState.OnHit(originObject, direction, type, damage, false);

            if (_aiDamageState.CurrentLives <= 0)
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("dead");
                Game1.GameManager.StartDialogPath("turtle_rock_killed");
            }

            return Values.HitCollision.Enemy;
        }

        private void OnSongPlayed(int songIndex)
        {
            if (songIndex == 2 && _aiComponent.CurrentStateId == "stone")
            {
                Game1.GameManager.SetMusic(56, 2);

                ((BoxCollisionComponent)Components[CollisionComponent.Index]).IsActive = false;
                ((DamageFieldComponent)Components[DamageFieldComponent.Index]).IsActive = true;
                _aiComponent.ChangeState("wobble");
            }
        }
    }
}