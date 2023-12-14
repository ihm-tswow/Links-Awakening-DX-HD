using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.GameObjects.Enemies;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class BossHotHead : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly CSprite _sprite;
        private readonly Animator _animator;
        private readonly Animator _animatorFaceTrail;
        private readonly AnimationComponent _animationComponent;
        private readonly AiTriggerCountdown _hitMoveReset;

        private readonly Vector2 _spawnPosition;
        private readonly string _saveKey;

        private const float MoveSpeed = 2f;

        private Vector2[] _facePosition = new Vector2[2];

        private bool _damaged;
        private bool _hasSpawned;

        public BossHotHead() : base("hot head") { }

        public BossHotHead(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _spawnPosition = EntityPosition.Position;
            _saveKey = saveKey;

            if (!string.IsNullOrEmpty(saveKey) &&
                Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                SpawnHeart();
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/hot head");
            _animator.Play("flame");

            _animatorFaceTrail = AnimatorSaveLoad.LoadAnimator("Nightmares/hot head");
            _animatorFaceTrail.Play("green");

            _sprite = new CSprite(EntityPosition) { IsVisible = false };
            _animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -9, -12, 18, 12, 8)
            {
                Gravity = -0.075f,
                FieldRectangle = map.GetField(posX, posY, 16),
                MoveCollision = OnMoveCollision,
            };

            var stateIdle = new AiState(UpdateIdle);
            var stateEndIdle = new AiState();
            stateEndIdle.Trigger.Add(new AiTriggerCountdown(1000, null, EndIdle));
            var stateJumping = new AiState(UpdateJumping) { Init = InitJump };
            var stateHidden = new AiState() { Init = InitHidden };
            stateHidden.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("jumping")));
            var stateDamaged = new AiState() { Init = InitDamaged };
            stateDamaged.Trigger.Add(new AiTriggerCountdown(1000, TickDamaged, DamageEnd));
            var stateMoving = new AiState(UpdateMoving) { Init = InitMoving };
            stateMoving.Trigger.Add(new AiTriggerCountdown(100, null, UpdateFaceTrail) { ResetAfterEnd = true });
            stateMoving.Trigger.Add(_hitMoveReset = new AiTriggerCountdown(200, null, ContinueMoving, false));
            stateMoving.Trigger.Add(new AiTriggerCountdown(2500, null, FallDown));
            var stateBreaking = new AiState() { Init = InitBreaking };
            stateBreaking.Trigger.Add(new AiTriggerCountdown(1000, TickDamaged, BreakingEnd));
            var stateBroken = new AiState();
            stateBroken.Trigger.Add(new AiTriggerCountdown(1000, null, FallDown));
            var stateFrozen = new AiState() { Init = InitFrozen };
            stateFrozen.Trigger.Add(new AiTriggerCountdown(250, null, EndFreeze));
            var stateDead = new AiState();

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("endIdle", stateEndIdle);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("damaged", stateDamaged);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("breaking", stateBreaking);
            _aiComponent.States.Add("broken", stateBroken);
            _aiComponent.States.Add("freeze", stateFrozen);
            _aiComponent.States.Add("dead", stateDead);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 20, false, false)
            {
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                ExplosionOffsetY = 4,
                BossHitSound = true
            };
            _damageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("idle");

            var drawPosition = new CPosition(EntityPosition.X, EntityPosition.Y, 0);
            drawPosition.SetParent(EntityPosition, Vector2.Zero, true);

            var damageCollider = new CBox(EntityPosition, -7, -11, 0, 14, 11, 8, true);
            var hittableBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 16));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, drawPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 20, ShadowHeight = 6 });
        }

        private void UpdateIdle()
        {
            // player entered the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.StartDialogPath("hot_head");
                _aiComponent.ChangeState("endIdle");
            }
        }

        private void EndIdle()
        {
            _hasSpawned = true;
            _aiComponent.ChangeState("jumping");
        }

        private void InitHidden()
        {
            _body.VelocityTarget = Vector2.Zero;
            _sprite.IsVisible = false;
        }

        private void InitJump()
        {
            var newPosition = new Vector2(_body.FieldRectangle.Center.X - 8, _body.FieldRectangle.Center.Y);
            var dir = Game1.RandomNumber.Next(0, 2) * 2 - 1;
            newPosition.X = MathF.Round(newPosition.X) - dir * Game1.RandomNumber.Next(24, 32);
            newPosition.Y = MathF.Round(newPosition.Y) - 14 + Game1.RandomNumber.Next(0, 36);
            EntityPosition.Set(newPosition);

            _body.VelocityTarget = new Vector2(dir * 1.35f, (Game1.RandomNumber.Next(0, 9) - 4) / 12f);
            _body.Velocity.Z = 1.5f;

            _sprite.IsVisible = true;
            _animator.Play(_damaged ? "damaged" : "flame");
            Game1.GameManager.PlaySoundEffect("D370-22-16");

            SpawnSplashAnimation();
        }

        private void UpdateJumping()
        {
            // landed after a jump?
            if (!_body.IsGrounded)
                return;

            Game1.GameManager.PlaySoundEffect("D360-50-32");

            SpawnSplashAnimation();
            SpawnSplashParticles();

            _aiComponent.ChangeState("hidden");
        }

        private void InitFrozen()
        {
            _body.IsActive = false;
        }

        private void EndFreeze()
        {
            _body.IsActive = true;
            _aiComponent.ChangeState("jumping", true);
        }

        private void FallDown()
        {
            _body.IsActive = true;
            _body.IgnoresZ = false;
            _body.VelocityTarget = Vector2.Zero;
            _aiComponent.ChangeState("jumping", true);
        }

        private void InitMoving()
        {
            _body.IsActive = true;
            _body.IgnoresZ = true;
            _body.VelocityTarget = new Vector2(1, 1);
            _body.VelocityTarget.Normalize();
            _body.VelocityTarget *= MoveSpeed;

            _facePosition[0] = new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
            _facePosition[1] = new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
        }

        private void UpdateMoving()
        {
            Game1.GameManager.PlaySoundEffect("D378-13-0D", false);
        }

        private void ContinueMoving()
        {
            _body.IsActive = true;
        }

        private void UpdateFaceTrail()
        {
            _facePosition[0] = _facePosition[1];
            _facePosition[1] = new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
        }

        private void InitDamaged()
        {
            _body.IsActive = false;
            _body.Velocity = Vector3.Zero;
            _animator.Play("red");

            Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X - 16, (int)(EntityPosition.Y - EntityPosition.Z - 32), Values.LayerPlayer, "Particles/spawn", "run", true));
            Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X, (int)(EntityPosition.Y - EntityPosition.Z - 32), Values.LayerPlayer, "Particles/spawn", "run", true));
        }

        private void TickDamaged(double counter)
        {
            // 2 frames to move left to right
            _animationComponent.SpriteOffset.X = MathF.Sin((float)Game1.TotalGameTime / 1000f * 30 * MathF.PI) / 2f;
            _animationComponent.UpdateSprite();
        }

        private void DamageEnd()
        {
            _animationComponent.SpriteOffset.X = 0;
            _animationComponent.UpdateSprite();

            _aiComponent.ChangeState("moving");
        }

        private void InitBreaking()
        {
            _body.IsActive = false;
            _body.Velocity = Vector3.Zero;
        }

        private void BreakingEnd()
        {
            _animationComponent.SpriteOffset.X = 0;
            _animationComponent.UpdateSprite();

            FaceBreak();
        }

        private void FaceBreak()
        {
            _damaged = true;

            Game1.GameManager.PlaySoundEffect("D378-41-29");

            _animator.Play("damaged");
            _aiComponent.ChangeState("broken");

            Map.Objects.SpawnObject(new BossHotHeadFace(Map, new Vector3(EntityPosition.X - 7, EntityPosition.Y, EntityPosition.Z), new Vector3(-1, 0.125f, 0.25f), "face_left"));
            Map.Objects.SpawnObject(new BossHotHeadFace(Map, new Vector3(EntityPosition.X + 7, EntityPosition.Y, EntityPosition.Z), new Vector3(1, 0.125f, 0.25f), "face_right"));
        }

        private void SpawnSplashAnimation()
        {
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerPlayer, "Nightmares/hot head", "splash", true);
            splashAnimator.EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y + 0.01f));
            Map.Objects.SpawnObject(splashAnimator);
        }

        private void SpawnSplashParticles()
        {
            var speedMult = 0.65f;
            Map.Objects.SpawnObject(new BossHotHeadSplash(Map, new Vector2(EntityPosition.X - 8, EntityPosition.Y - 8), new Vector2(-1, -1) * speedMult));
            Map.Objects.SpawnObject(new BossHotHeadSplash(Map, new Vector2(EntityPosition.X - 8, EntityPosition.Y), new Vector2(-1, 1) * speedMult));
            Map.Objects.SpawnObject(new BossHotHeadSplash(Map, new Vector2(EntityPosition.X + 8, EntityPosition.Y - 8), new Vector2(1, -1) * speedMult));
            Map.Objects.SpawnObject(new BossHotHeadSplash(Map, new Vector2(EntityPosition.X + 8, EntityPosition.Y), new Vector2(1, 1) * speedMult));
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // is hidden?
            if (!_hasSpawned)
                return;

            if (_aiComponent.CurrentStateId == "moving")
                for (var i = 0; i < _facePosition.Length; i++)
                    _animatorFaceTrail.Draw(spriteBatch, _facePosition[i], Color.White);

            _sprite.Draw(spriteBatch);
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            Map.Objects.DeleteObjects.Add(this);

            // spawn big heart
            var spawnPosition = new Vector2((int)(MapManager.ObjLink.EntityPosition.X / 16) * 16, (int)((MapManager.ObjLink.EntityPosition.Y - 1) / 16) * 16);
            // make sure to not spawn the heart over the lava   
            if (Map.GetFieldState(spawnPosition) != MapStates.FieldStates.None)
                spawnPosition = new Vector2((int)_spawnPosition.X, (int)_spawnPosition.Y + 32);

            var objHeart = new ObjItem(Map, (int)spawnPosition.X, (int)spawnPosition.Y, "d", _saveKey + "Heart", "heartMeterFull", null);
            Map.Objects.SpawnObject(objHeart);
        }

        private void SpawnHeart()
        {
            var objHeart = new ObjItem(Map, (int)_spawnPosition.X, (int)_spawnPosition.Y + 32, "d", _saveKey + "Heart", "heartMeterFull", null);
            Map.Objects.SpawnObject(objHeart);
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId == "moving")
            {
                if ((collision & Values.BodyCollision.Horizontal) != 0)
                    _body.VelocityTarget.X = -_body.VelocityTarget.X;
                if ((collision & Values.BodyCollision.Vertical) != 0)
                    _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
            }
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType != HitType.MagicRod || _damageState.CurrentLives <= 0)
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId == "breaking" || _aiComponent.CurrentStateId == "broken" ||
                _damageState.IsInDamageState())
                return Values.HitCollision.Enemy;

            // flame jump => damaged
            if (!_damaged && _aiComponent.CurrentStateId == "jumping")
            {
                _damageState.SetDamageState();
                _aiComponent.ChangeState("damaged");
                Game1.GameManager.PlaySoundEffect("D370-07-07");
                return Values.HitCollision.Enemy;
            }

            // moving/wobbling => hit
            if (_aiComponent.CurrentStateId == "moving" || _aiComponent.CurrentStateId == "damaged")
            {
                _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

                if (!_damaged && _damageState.CurrentLives <= 4)
                {
                    _aiComponent.ChangeState("breaking");
                    return Values.HitCollision.Enemy;
                }

                // stop moving for a short while
                _body.IsActive = false;
                _hitMoveReset.Restart();

                return Values.HitCollision.Enemy;
            }

            // damage state while jumping => hit
            if (_damaged && _aiComponent.CurrentStateId == "jumping")
            {
                if (_damageState.CurrentLives > 2)
                    _aiComponent.ChangeState("freeze");
                else
                    _aiComponent.ChangeState("dead");

                _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

                // freeze in the air + show final dialog
                if (_damageState.CurrentLives <= 0)
                {
                    _body.IsActive = false;
                    Game1.GameManager.StartDialogPath("hot_head_death");
                }

                return Values.HitCollision.Enemy;
            }

            return Values.HitCollision.None;
        }
    }
}