using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MBossGiantBuzzBlob : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly CSprite _sprite;

        private ObjDungeonFairy _dungeonFairy;

        private readonly string _saveKey;

        private const float WalkSpeed = 0.25f;
        private const float JumpSpeed = 0.5f;

        // small delay before starting to walk
        private float _idleDelayCounter = 250;

        private bool _startAttack;
        private bool _swordMessage;
        private bool _toSlime;
        private int _jumpCounter;
        private bool _wasHit;
        private bool _attackable;

        public MBossGiantBuzzBlob() : base("giant buzz blob") { }

        public MBossGiantBuzzBlob(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 16, posY + 32, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _saveKey = saveKey;

            // was already killed?
            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/giant buzz blob");
            _animator.Play("floor");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -10, -16, 20, 16, 8)
            {
                IgnoreHoles = true,
                Gravity = -0.095f,
                FieldRectangle = Map.GetField(posX, posY, 16)
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            var stateIdleDelay = new AiState(UpdateIdleDelay);
            var stateWalk = new AiState(UpdateWalk) { Init = InitWalk };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(EndWalk, 300, 1000));
            var stateAttack = new AiState { Init = InitAttack };
            stateAttack.Trigger.Add(new AiTriggerCountdown(50, null, EndAttack));
            var statePreSlime = new AiState();
            statePreSlime.Trigger.Add(new AiTriggerCountdown(600, null, () => _aiComponent.ChangeState("toSlime")));
            var stateToSlime = new AiState(UpdateToSlime) { Init = InitToSlime };
            var stateSlime = new AiState { Init = InitSlime };
            stateSlime.Trigger.Add(new AiTriggerCountdown(133, null, EndSlime));
            var statePreJump = new AiState(UpdatePreJump) { Init = InitPreJump };
            var stateJump = new AiState(UpdateJump) { Init = InitJump };
            var statePostJump = new AiState(UpdatePostJump) { Init = InitPostJump };
            var stateEndSlime = new AiState(UpdateEndSlime) { Init = InitEndSlime };
            var stateDeath = new AiState { Init = InitDeath };

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("idleDelay", stateIdleDelay);
            _aiComponent.States.Add("walk", stateWalk);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("preSlime", statePreSlime);
            _aiComponent.States.Add("toSlime", stateToSlime);
            _aiComponent.States.Add("slime", stateSlime);
            _aiComponent.States.Add("preJump", statePreJump);
            _aiComponent.States.Add("jump", stateJump);
            _aiComponent.States.Add("postJump", statePostJump);
            _aiComponent.States.Add("endSlime", stateEndSlime);
            _aiComponent.States.Add("death", stateDeath);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 6, false, false)
            {
                HitMultiplierX = 1,
                HitMultiplierY = 1,
                ExplosionOffsetY = 4,
                BossHitSound = true
            };
            _aiDamageState.AddBossDamageState(OnDeathAnimationEnd);
            _aiDamageState.DamageSpriteShader = Resources.DamageSpriteShader1;

            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -8, -28, 0, 16, 28, 8, false);
            var hittableBox = new CBox(EntityPosition, -8, -28, 0, 16, 28, 8, false);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 14, ShadowHeight = 5 });
        }

        private void UpdateIdle()
        {
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.StartDialogPath("giant_buzz_blob_enter");
                _aiComponent.ChangeState("idleDelay");
            }
        }

        private void UpdateIdleDelay()
        {
            if (Game1.GameManager.DialogIsRunning())
                return;

            _idleDelayCounter -= Game1.DeltaTime;
            if (0 < _idleDelayCounter)
                return;

            _aiComponent.ChangeState("endSlime");
        }

        private void InitWalk()
        {
            if (_toSlime)
                return;

            _animator.Play("walk");
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            var direction = new Vector2(MathF.Sin(rotation), MathF.Cos(rotation));
            _body.VelocityTarget = direction * WalkSpeed;
        }

        private void UpdateWalk()
        {
            // spawn a fairy carrying powder for the player?
            var powder = Game1.GameManager.GetItem("powder");
            if ((powder == null || powder.Count <= 0) && (_dungeonFairy == null || !_dungeonFairy.IsActive))
            {
                _dungeonFairy = new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 32, "powder_10");
                Map.Objects.SpawnObject(_dungeonFairy);
            }
            // make sure to be on the straight frame when attacking
            if (!_toSlime && _startAttack && _animator.CurrentFrameIndex % 2 == 0 && _animator.FrameCounter >= 50)
            {
                _startAttack = false;
                _aiComponent.ChangeState("attack");
            }

            if (_toSlime && _animator.CurrentFrameIndex % 2 == 0)
            {
                _toSlime = false;
                _jumpCounter = 0;
                _animator.Pause();
                _aiComponent.ChangeState("preSlime");
            }
        }

        private void EndWalk()
        {
            if (Game1.RandomNumber.Next(0, 3) == 0)
                _startAttack = true;
            else
                _aiComponent.ChangeState("walk");
        }

        private void InitAttack()
        {
            _body.VelocityTarget = Vector2.Zero;
            _sprite.SpriteShader = Resources.DamageSpriteShader1;

            // spawn buzz
            var direction = Game1.RandomNumber.Next(0, 2);
            var spawnOrigin = new Vector2(EntityPosition.X, EntityPosition.Y - 14);
            for (var i = 0; i < 4; i++)
            {
                var rotation = MathF.PI / 2 * i + direction * MathF.PI / 4;
                var offset = new Vector2(-MathF.Cos(rotation), MathF.Sin(rotation));
                var objBuzz = new MBossBuzz(Map, new Vector2(spawnOrigin.X + offset.X * 20, spawnOrigin.Y + offset.Y * 20), offset, "buzz_" + direction, MathF.PI / 2 * i);
                Map.Objects.SpawnObject(objBuzz);
            }
        }

        private void EndAttack()
        {
            _sprite.SpriteShader = null;
            _aiComponent.ChangeState("walk");
        }

        private void InitEndSlime()
        {
            _attackable = false;
            _wasHit = false;
            _animator.Play("deslime");
        }

        private void UpdateEndSlime()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("walk");
        }

        private void InitToSlime()
        {
            _attackable = true;
            _animator.Play("slime");
        }

        private void UpdateToSlime()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("slime");
        }

        private void InitSlime()
        {
            _animator.Play("floor");
        }

        private void EndSlime()
        {
            if (_jumpCounter < 3 && (!_wasHit || _jumpCounter == 0))
                _aiComponent.ChangeState("preJump");
            else
                _aiComponent.ChangeState("endSlime");

            _jumpCounter++;
        }

        private void InitPreJump()
        {
            _animator.Play("jump");
        }

        private void UpdatePreJump()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("jump");
        }

        private void InitJump()
        {
            _animator.Play("fly");
            _body.Velocity.Z = 2.5f;

            // move towards the player
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            _body.VelocityTarget = playerDirection * JumpSpeed;
        }

        private void UpdateJump()
        {
            if (_body.IsGrounded)
                _aiComponent.ChangeState("postJump");
        }

        private void InitPostJump()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("land");
        }

        private void UpdatePostJump()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("slime");
        }

        private void InitDeath()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("land");
        }

        private void OnDeathAnimationEnd()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Game1.GameManager.PlaySoundEffect("D378-26-1A");

            // spawns a fairy
            Game1.GameManager.PlaySoundEffect("D360-27-1B");
            Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 8));

            Map.Objects.DeleteObjects.Add(this);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiDamageState.CurrentLives <= 0 || _aiDamageState.IsInDamageState())
                return Values.HitCollision.None;

            if (damageType == HitType.MagicPowder)
                damage = 1;
            if (damageType == HitType.Boomerang)
                damage = 2;

            if (_attackable)
            {
                _wasHit = true;
                var hit = _aiDamageState.OnHit(gameObject, direction, damageType, damage, false);
                if (_aiDamageState.CurrentLives <= 0)
                    _aiComponent.ChangeState("death");

                return hit;
            }

            _body.Velocity.X = direction.X;
            _body.Velocity.Y = direction.Y;

            // show initial message telling the player that the sword is useless
            if (!_swordMessage && (damageType & HitType.Sword) != 0)
            {
                _swordMessage = true;
                Game1.GameManager.StartDialogPath("giant_buzz_blob_sword");
            }

            if (damageType == HitType.MagicPowder && _aiComponent.CurrentStateId == "walk")
            {
                // do not show the sword message after the player has already figured out that there can be something done with the powder
                _swordMessage = true;
                _toSlime = true;
                _body.VelocityTarget = Vector2.Zero;

                var hit = _aiDamageState.OnHit(gameObject, direction, damageType, damage, false);
                if (_aiDamageState.CurrentLives <= 0)
                    _aiComponent.ChangeState("death");

                return hit;
            }

            if (_aiComponent.CurrentStateId == "walk")
            {
                _aiDamageState.SetDamageState(false);
                Game1.GameManager.PlaySoundEffect("D370-07-07");

                return Values.HitCollision.Repelling | Values.HitCollision.Repelling0;
            }

            return Values.HitCollision.Enemy;
        }
    }
}
