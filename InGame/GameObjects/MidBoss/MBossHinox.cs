using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MBossHinox : GameObject
    {
        private readonly Color[] _colors = new Color[] { new Color(248, 120, 8), new Color(248, 8, 40), new Color(24, 128, 248) };

        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly DamageFieldComponent _damageFieldComponent;
        private readonly CBox _grabBox;

        private readonly string _saveKey;

        private const int GrabTime = 300;
        private const int Lives = 8;

        private float _runParticleCount;

        private Vector3 _grabStartPosition;
        private int _grabDirection;

        public MBossHinox() : base("hinox") { }

        public MBossHinox(Map.Map map, int posX, int posY, string saveKey, int color) : base(map)
        {
            if (!string.IsNullOrEmpty(saveKey) &&
                Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 16, posY + 32, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _saveKey = saveKey;

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/hinox");
            _animator.Play("idle_0");

            color = MathHelper.Clamp(color, 0, _colors.Length - 1);
            var sprite = new CSprite(EntityPosition) { SpriteShader = Resources.ColorShader, Color = _colors[color] };
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -14, -20, 28, 20, 8)
            {
                IgnoreHoles = true,
                FieldRectangle = Map.GetField(posX, posY, 8)
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            var stateWait = new AiState(UpdateWait) { Init = InitWait };
            stateWait.Trigger.Add(new AiTriggerRandomTime(WalkOrRun, 1000, 1500));
            var stateWalk = new AiState { Init = InitWalking };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(EndWalking, 750, 1250));
            var statePreRun = new AiState(UpdatePreRun) { Init = InitPreRun };
            statePreRun.Trigger.Add(new AiTriggerCountdown(750, null, () => _aiComponent.ChangeState("run")));
            var stateRun = new AiState(UpdateRunning) { Init = InitRun };
            stateRun.Trigger.Add(new AiTriggerRandomTime(EndRun, 500, 750));
            var stateThrowLink = new AiState();
            var stateThrowBomb = new AiState() { Init = InitThrowBomb };
            stateThrowBomb.Trigger.Add(new AiTriggerCountdown(250, null, ThrowBomb));
            var stateThrownBomb = new AiState(UpdateThrownBomb);
            var stateGrab = new AiState { Init = InitGrab };
            stateGrab.Trigger.Add(new AiTriggerCountdown(GrabTime, GrabTick, () => _aiComponent.ChangeState("grabbed")));
            var stateGrabbed = new AiState();
            stateGrabbed.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("throw")));
            var stateThrow = new AiState { Init = InitThrow };
            stateThrow.Trigger.Add(new AiTriggerCountdown(600, null, () => _aiComponent.ChangeState("walk")));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("walk", stateWalk);
            _aiComponent.States.Add("preRun", statePreRun);
            _aiComponent.States.Add("run", stateRun);
            _aiComponent.States.Add("throwLink", stateThrowLink);
            _aiComponent.States.Add("throwBomb", stateThrowBomb);
            _aiComponent.States.Add("thrownBomb", stateThrownBomb);
            _aiComponent.States.Add("grab", stateGrab);
            _aiComponent.States.Add("grabbed", stateGrabbed);
            _aiComponent.States.Add("throw", stateThrow);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, sprite, Lives, true, false)
            {
                BossHitSound = true
            };
            _aiDamageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("idle");

            _grabBox = new CBox(EntityPosition, -20, -20, 0, 40, 24, 8);
            var damageCollider = new CBox(EntityPosition, -14, -24, 0, 28, 24, 8);
            var hittableBox = new CBox(EntityPosition, -14, -28, 0, 28, 28, 8);
            AddComponent(DamageFieldComponent.Index, _damageFieldComponent = new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void UpdateIdle()
        {
            // player entered the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                // start boss music
                Game1.GameManager.SetMusic(79, 2);

                _aiComponent.ChangeState("walk");
            }
        }

        private void InitGrab()
        {
            MapManager.ObjLink.Stun(2000);
            MapManager.ObjLink.StartGrab();

            Game1.GameManager.PlaySoundEffect("D370-22-16");

            _body.VelocityTarget = Vector2.Zero;

            _damageFieldComponent.IsActive = false;

            if (MapManager.ObjLink.PosX < EntityPosition.X)
                _grabDirection = 1;
            else
                _grabDirection = -1;

            _animator.Pause();
            _animator.SetFrame(_grabDirection == 1 ? 0 : 1);

            _grabStartPosition = MapManager.ObjLink.EntityPosition.ToVector3();
        }

        private void GrabTick(double counter)
        {
            var percentage = 1 - (float)(counter / GrabTime);
            var grabEndPosition = new Vector3(EntityPosition.X + 16 * _grabDirection, EntityPosition.Y + 1, 26);
            var newPosition = Vector3.Lerp(_grabStartPosition, grabEndPosition, percentage);
            MapManager.ObjLink.EntityPosition.Set(newPosition);
        }

        private void InitThrow()
        {
            Game1.GameManager.PlaySoundEffect("D360-08-08");

            // set the position to be inside of the hinox body to not start throwing the player into a collider
            var grabEndPosition = new Vector3(EntityPosition.X + 16 * _grabDirection, EntityPosition.Y, 25);
            MapManager.ObjLink.EntityPosition.Set(grabEndPosition);
            MapManager.ObjLink.EndGrab();
            MapManager.ObjLink.StartThrow(new Vector3(-4.5f * _grabDirection, 2.5f, 0));
            Game1.GameManager.InflictDamage(4);

            _damageFieldComponent.IsActive = true;
            _animator.SetFrame(_grabDirection == 1 ? 1 : 0);
        }

        private void ContinueAnimation()
        {
            _animator.Continue();
            _animator.SpeedMultiplier = 1.0f;

            // set the animation to the next frame
            _animator.ResetFrameCounter();
            _animator.SetFrame((_animator.CurrentFrameIndex + 1) % _animator.CurrentAnimation.Frames.Length);
        }

        private void InitWait()
        {
            _animator.Pause();
            _body.VelocityTarget = Vector2.Zero;
        }

        private void UpdateWait()
        {
            // player left the room?
            if (!_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                // stop boss music
                Game1.GameManager.SetMusic(-1, 2);
                _aiComponent.ChangeState("idle");
            }
        }

        private void WalkOrRun()
        {
            var random = Game1.RandomNumber.Next(0, 2);
            _aiComponent.ChangeState(random == 0 ? "preRun" : "walk");
            _animator.Play("idle_0");
        }

        private void InitWalking()
        {
            ContinueAnimation();

            // walk into a random direction
            var direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[direction] * 0.5f;
        }

        private void EndWalking()
        {
            _aiComponent.ChangeState("wait");
        }

        private void InitPreRun()
        {
            _animator.Continue();
            _animator.SpeedMultiplier = 2.0f;
        }

        private void UpdatePreRun()
        {
            Game1.GameManager.PlaySoundEffect("D360-32-20", false);
        }

        private void InitRun()
        {
            // run towards the player
            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (direction != Vector2.Zero)
                direction.Normalize();
            _body.VelocityTarget = direction * 1.5f;
        }

        private void UpdateRunning()
        {
            // grab the player
            if (Game1.GameManager.CurrentHealth > 0 &&
                _grabBox.Box.Rectangle().Intersects(MapManager.ObjLink.BodyRectangle))
                _aiComponent.ChangeState("grab");

            // spawn run particle
            _runParticleCount -= Game1.DeltaTime;
            if (_runParticleCount <= 0)
            {
                _runParticleCount = 133;

                var animator = new ObjAnimator(Map,
                    (int)EntityPosition.X, (int)(EntityPosition.Y + 1),
                    0, -1 - (int)EntityPosition.Z, Values.LayerPlayer, "Particles/run", "spawn", true);
                Map.Objects.SpawnObject(animator);
            }
        }

        private void EndRun()
        {
            _aiComponent.ChangeState("wait");
        }

        private void InitThrowBomb()
        {
            _animator.Pause();
            _body.VelocityTarget = Vector2.Zero;
        }

        private void ThrowBomb()
        {
            var handOffset = _animator.CurrentFrameIndex == 0 ? 8 : -8;
            var spawnPosition = new Vector2(EntityPosition.X + handOffset, EntityPosition.Y);
            var throwDirection = MapManager.ObjLink.EntityPosition.Position - spawnPosition;
            var maxRange = 48f;
            var mult = 1.5f;
            if (throwDirection.Length() > maxRange)
            {
                throwDirection.Normalize();
                throwDirection *= mult;
            }
            else
                throwDirection = (throwDirection / maxRange) * mult;

            // spawn a bomb
            var bomb = new ObjBomb(Map, 0, 0, false, true);
            bomb.EntityPosition.Set(new Vector3(spawnPosition.X, spawnPosition.Y, 20));
            bomb.Body.Velocity = new Vector3(throwDirection.X, throwDirection.Y, 1);
            bomb.Body.Gravity = -0.2f;
            bomb.Body.Bounciness = 0.25f;
            bomb.Body.DragAir = 1.0f;
            Map.Objects.SpawnObject(bomb);

            Game1.GameManager.PlaySoundEffect("D360-08-08");

            // play throw animation
            _animator.Play("throw_" + _animator.CurrentFrameIndex);

            _aiComponent.ChangeState("thrownBomb");
        }

        private void UpdateThrownBomb()
        {
            // finished throw animation?
            if (!_animator.IsPlaying)
                WalkOrRun();
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop the music
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
            if (_aiComponent.CurrentStateId == "idle")
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId == "throwBomb")
                ThrowBomb();

            // the boss will throw a bomb right after getting damaged
            if (!_aiDamageState.IsInDamageState() &&
                _aiComponent.CurrentStateId != "deathBoss" &&
                _aiComponent.CurrentStateId != "preRun" &&
                _aiComponent.CurrentStateId != "run")
                _aiComponent.ChangeState("throwBomb");

            if (damageType == HitType.Bow || damageType == HitType.Bomb || damageType == HitType.MagicRod)
                damage *= 2;

            if (damageType == HitType.Boomerang)
                damage = 2;

            var hitCollision = _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            // stop walking and stop the animation when dead
            if (_aiDamageState.CurrentLives <= 0)
            {
                RemoveComponent(HittableComponent.Index);

                _animator.Stop();
                _body.VelocityTarget = Vector2.Zero;

                // make sure to let the player go if he was grabbed
                MapManager.ObjLink.EndGrab();
            }

            return hitCollision;
        }
    }
}
