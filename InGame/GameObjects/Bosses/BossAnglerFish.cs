using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossAnglerFish : GameObject
    {
        private readonly AiTriggerCountdown _damageCountdown;
        private readonly AiTriggerCountdown _stoneCountdown;
        private readonly AiTriggerRandomTime _fishCountdown;
        private readonly AiTriggerRandomTime _blobCountdown;

        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private readonly Color _lightColor = new Color(255, 200, 200);

        private readonly Vector2 _startPosition;

        private readonly string _saveKey;

        private const int CooldownTime = 350;
        private const int StoneSpawnTime = 1500;

        private float _waitingCounter;
        private float _deathCount;
        private int _stoneCount;

        private const int Lives = 10;
        private int _currentLives = Lives;
        private bool _isAlive = true;

        private Vector2 _preAttackVelocity;

        public BossAnglerFish() : base("angler fish") { }

        public BossAnglerFish(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 16, posY, 0);
            EntitySize = new Rectangle(-26, -23 + 16, 56, 48);

            _startPosition = EntityPosition.Position;

            _saveKey = saveKey;

            if (!string.IsNullOrWhiteSpace(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                // respawn the heart if the player died after he killed the boss without collecting the heart
                SpawnHeart();

                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/anger fish");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);

            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-28, -24 + 16));

            _body = new BodyComponent(EntityPosition, -20, -23 + 16, 50, 48, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                Bounciness = 0.25f,
                Drag = 0.85f,
                IgnoresZ = true,
                IsGrounded = false,
                MoveCollision = OnCollision,
                FieldRectangle = Map.GetField(posX, posY)
            };

            var hittableRectangle = new CBox(EntityPosition, -22, 0, 26, 26, 8);
            var damageCollider = new CBox(EntityPosition, -18, -20 + 16, 47, 38, 8);

            var stateWaiting = new AiState(UpdateWaiting);
            var stateMoving = new AiState();
            stateMoving.Trigger.Add(new AiTriggerRandomTime(StartAttacking, 5000, 7500));

            var statePreAttack = new AiState();
            statePreAttack.Trigger.Add(new AiTriggerCountdown(500, null, ToAttack));
            var stateAttack = new AiState();
            var stateShaking = new AiState();
            stateShaking.Trigger.Add(new AiTriggerCountdown(800, null, ToRetrieving));
            var statePostAttack = new AiState(UpdatePostAttack);

            var stateBlink = new AiState();
            stateBlink.Trigger.Add(new AiTriggerCountdown(1000, DamageTick, ToDeath));
            var stateDeath = new AiState(UpdateDeath);
            stateDeath.Trigger.Add(new AiTriggerCountdown(2000, DamageTick, RemoveObject));

            _aiComponent = new AiComponent();

            _aiComponent.Trigger.Add(_damageCountdown = new AiTriggerCountdown(CooldownTime, DamageTick, FinishDamage));
            _aiComponent.Trigger.Add(_stoneCountdown = new AiTriggerCountdown(StoneSpawnTime, null, SpawnStone));
            _aiComponent.Trigger.Add(_fishCountdown = new AiTriggerRandomTime(SpawnFish, 2000, 5000) { IsRunning = false });
            _aiComponent.Trigger.Add(_blobCountdown = new AiTriggerRandomTime(SpawnBlob, 1000, 1500));

            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("preAttack", statePreAttack);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("shaking", stateShaking);
            _aiComponent.States.Add("postAttack", statePostAttack);
            _aiComponent.States.Add("blink", stateBlink);
            _aiComponent.States.Add("death", stateDeath);

            _aiComponent.ChangeState("waiting");

            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 6));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        private void UpdateWaiting()
        {
            _waitingCounter += Game1.DeltaTime;

            // move up/down while waiting
            EntityPosition.Set(new Vector2(EntityPosition.X, _startPosition.Y + MathF.Sin(_waitingCounter / 500f) * 7.5f));

            if (MapManager.ObjLink.PosY > 160)
                StartMoving();
        }

        private void StartMoving()
        {
            _aiComponent.ChangeState("moving");

            // spawn dialog
            Game1.GameManager.StartDialogPath("d4_nightmare");

            // start moving and start spawning fish
            _body.VelocityTarget.Y = 0.5f;
            _fishCountdown.OnInit();
        }

        private void StartAttacking()
        {
            _aiComponent.ChangeState("preAttack");

            _animator.SpeedMultiplier = 2.75f;
            _preAttackVelocity = _body.VelocityTarget;
            _body.VelocityTarget.Y = 0;
        }

        private void ToAttack()
        {
            _aiComponent.ChangeState("attack");

            Game1.GameManager.PlaySoundEffect("D370-13-0D");

            _body.VelocityTarget.X = -3;
        }

        private void ToShaking()
        {
            _aiComponent.ChangeState("shaking");

            Game1.GameManager.PlaySoundEffect("D378-12-0C");
            Game1.GameManager.ShakeScreen(750, 2, 0, 5.0f, 0);

            _body.VelocityTarget.X = 0;

            // start spawning stones
            _stoneCount = Game1.RandomNumber.Next(2, 4); // 2-3 stones
            _stoneCountdown.OnInit();
            _stoneCountdown.CurrentTime = 1; // directly spawn a stone
            _stoneCountdown.StartTime = StoneSpawnTime + Game1.RandomNumber.Next(0, 1000);
        }

        private void ToRetrieving()
        {
            _aiComponent.ChangeState("postAttack");

            _body.VelocityTarget.X = 2;
        }

        private void UpdatePostAttack()
        {
            if (EntityPosition.X > _startPosition.X)
            {
                _aiComponent.ChangeState("moving");

                _animator.SpeedMultiplier = 1.0f;
                _body.VelocityTarget = _preAttackVelocity;
                EntityPosition.Set(new Vector2(_startPosition.X, EntityPosition.Y));
            }
        }

        private void SpawnStone()
        {
            _stoneCount--;

            if (_stoneCount > 0 && _isAlive)
                _stoneCountdown.OnInit();

            var randomX = Math.Clamp(MapManager.ObjLink.EntityPosition.X,
                _body.FieldRectangle.Left + 25 + 8, _body.FieldRectangle.Right - 25 - 8) + (Game1.RandomNumber.Next(0, 50) - 25);

            var objStone = new AnglerFishStone(Map, (int)randomX, 16);
            Map.Objects.SpawnObject(objStone);
        }

        private void SpawnFish()
        {
            if (_isAlive || _currentLives < Lives)
                _fishCountdown.OnInit();

            var randomDir = (Game1.RandomNumber.Next(0, 2) * 2 - 1);
            var randomX = 80 + randomDir * 88;
            var randomY = Math.Clamp(MapManager.ObjLink.EntityPosition.Y, 124 + 35, 256 - 35) + (Game1.RandomNumber.Next(0, 70) - 35);

            var objFish = new EnemyAnglerFry(Map, randomX, (int)randomY, -randomDir);
            Map.Objects.SpawnObject(objFish);
        }

        private void SpawnBlob()
        {
            if (_isAlive)
                _blobCountdown.OnInit();

            var posX = (int)EntityPosition.X - 20;
            var posY = (int)EntityPosition.Y - 12 + 16;
            var objBlob = new AngerFishBlob(Map, posX, posY);
            Map.Objects.SpawnObject(objBlob);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
            else if (_aiComponent.CurrentStateId == "attack")
                ToShaking();
        }

        private void ToDeath()
        {
            _aiComponent.ChangeState("death");
            SetDamageSprite(false);
        }

        private void DamageTick(double time)
        {
            var useDamageSprite = time % 133 < 66;
            SetDamageSprite(useDamageSprite);
        }

        private void SetDamageSprite(bool useDamageSprite)
        {
            // @HACK: not sure how this should be handled
            // cant use a shader because there is no real color mapping that looks good
            // in the original it does not look good and the sprites are not well connected
            if (useDamageSprite && _sprite.SourceRectangle.X < 40)
                _sprite.SourceRectangle.X += 64;
            if (!useDamageSprite && _sprite.SourceRectangle.X > 40)
                _sprite.SourceRectangle.X -= 64;
        }

        private void UpdateDeath()
        {
            _deathCount += Game1.DeltaTime;
            if (_deathCount > 100)
                _deathCount -= 100;
            else
                return;

            Game1.GameManager.PlaySoundEffect("D378-19-13");

            var posX = (int)EntityPosition.X + Game1.RandomNumber.Next(0, 28) - 34;
            var posY = (int)EntityPosition.Y - (int)EntityPosition.Z + Game1.RandomNumber.Next(0, 28) - 10;

            // spawn explosion effect
            Map.Objects.SpawnObject(new ObjAnimator(Map, posX, posY, Values.LayerTop, "Particles/spawn", "run", true));
        }

        private void RemoveObject()
        {
            SetDamageSprite(false);

            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            SpawnHeart();

            Game1.GameManager.PlaySoundEffect("D378-26-1A");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Map.Objects.DeleteObjects.Add(this);
        }

        private void SpawnHeart()
        {
            // spawn big heart
            Map.Objects.SpawnObject(new ObjItem(Map,
                (int)EntityPosition.X - 20, (int)EntityPosition.Y + 8, "", "d4_nHeart", "heartMeterFull", null));
        }

        private void FinishDamage()
        {
            if (_sprite.SourceRectangle.X > 40)
                _sprite.SourceRectangle.X -= 64;
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprLight, new Rectangle((int)EntityPosition.X - 22 - 32, (int)EntityPosition.Y - 18 - 16, 64, 64), _lightColor);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_currentLives <= 0)
                return Values.HitCollision.None;

            if (damageType == HitType.Bow)
                damage = 1;
            if (damageType == HitType.Bomb)
                damage = 4;

            if (_damageCountdown.CurrentTime <= 0)
            {
                _currentLives -= damage;

                // just died?
                if (_currentLives <= 0)
                {
                    Game1.GameManager.PlaySoundEffect("D370-16-10");
                    _aiComponent.ChangeState("blink");
                    _body.VelocityTarget = Vector2.Zero;
                    return Values.HitCollision.Repelling;
                }
                else
                {
                    Game1.GameManager.PlaySoundEffect("D370-07-07");
                }

                _damageCountdown.OnInit();
                return Values.HitCollision.Repelling;
            }

            return Values.HitCollision.None;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}