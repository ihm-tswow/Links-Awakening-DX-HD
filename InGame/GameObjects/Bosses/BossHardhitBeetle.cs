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

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossHardhitBeetle : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly CSprite _sprite;
        private readonly AiTriggerCountdown _hitCooldown;
        private readonly AiTriggerCountdown _colorCountdown;
        private readonly DamageFieldComponent _damageField;

        private EnemyStalfosGreen[] _stalfos = new EnemyStalfosGreen[2];

        private readonly Color[] _colors = {
            new Color(42, 41, 254),
            new Color(0, 149, 114),
            new Color(34, 212, 16),
            new Color(141, 206, 9),
            new Color(254, 198, 1),
            new Color(253, 131, 0),
            new Color(255, 66, 1),
            new Color(253, 0, 0)
        };

        private readonly string _saveKey;

        // small delay before starting to walk
        private float _idleDelayCounter = 250;

        private const int CooldownTime = 250;
        private const float MoveSpeed = 0.375f;
        private int _colorIndex;

        private bool _isDead;

        private float _stalfosCounter;
        private bool _spawnedStalfos;

        public BossHardhitBeetle() : base("hardhit beetle") { }

        public BossHardhitBeetle(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 16, posY + 32, 0);
            EntitySize = new Rectangle(-16, -40, 32, 40);

            _saveKey = saveKey;

            // was already killed?
            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/hardhit beetle");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -14, -26, 28, 26, 8)
            {
                Gravity = -0.1f,
                FieldRectangle = Map.GetField(posX, posY, 16)
            };

            var stateIdle = new AiState(UpdateIdle);
            var stateIdleDelay = new AiState(UpdateIdleDelay);
            var stateWalk = new AiState(UpdateWalk) { Init = InitWalk };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(EndWalk, 500, 1000));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(new AiTriggerRandomTime(Shoot, 750, 2500));
            _aiComponent.Trigger.Add(_colorCountdown = new AiTriggerCountdown(2000, null, OnColorReset));
            _aiComponent.Trigger.Add(_hitCooldown = new AiTriggerCountdown(CooldownTime, TickCooldown, null));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("idleDelay", stateIdleDelay);
            _aiComponent.States.Add("walk", stateWalk);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1)
            {
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                BossHitSound = true
            };
            _aiDamageState.DamageSpriteShader = Resources.DamageSpriteShader1;
            _aiDamageState.AddBossDamageState(OnDeathAnimationEnd);

            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -14, -24, 0, 28, 24, 8);
            var hittableBox = new CBox(EntityPosition, -13, -34, 0, 26, 30, 8);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void UpdateIdle()
        {
            // player enters the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.StartDialogPath("hardhit_beetle_enter");
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

            _aiComponent.ChangeState("walk");
        }

        private void TickCooldown(double counter)
        {
            _sprite.SpriteShader = (CooldownTime - counter) <= 4200 / 60f ? Resources.DamageSpriteShader0 : null;
        }

        private void OffsetColor(int offset)
        {
            _colorIndex = MathHelper.Clamp(_colorIndex + offset, 0, _colors.Length - 1);

            if (_colorIndex == 0)
                Game1.GameManager.StartDialogPath("hardhit_beetle_1");

            if (_colorIndex < 4)
                _spawnedStalfos = false;

            // spawn stalfos?
            if (!_spawnedStalfos && offset > 0 && _colorIndex >= 6)
            {
                Game1.GameManager.StartDialogPath("hardhit_beetle_2");

                // spawn stalfos with a little delay
                _spawnedStalfos = true;
                _stalfosCounter = 250;
            }
        }

        private void OnColorReset()
        {
            OffsetColor(-1);
            _colorCountdown.OnInit();
        }

        private void InitWalk()
        {
            var direction = Game1.RandomNumber.Next(0, 8) / 4f * MathF.PI;
            _body.VelocityTarget = new Vector2(MathF.Sin(direction), MathF.Cos(direction)) * MoveSpeed;
        }

        private void UpdateWalk()
        {
            if (!_spawnedStalfos || _stalfosCounter <= 0)
                return;

            _stalfosCounter -= Game1.DeltaTime;
            if (_stalfosCounter <= 0)
            {
                for (var i = 0; i < _stalfos.Length; i++)
                {
                    if (_stalfos[i] != null && _stalfos[i].Map != null)
                        continue;

                    var randomOffsetX = Game1.RandomNumber.Next(0, 13) - 6;
                    var randomOffsetY = Game1.RandomNumber.Next(0, 8) - 4;

                    _stalfos[i] = new EnemyStalfosGreen(Map,
                        (int)MapManager.ObjLink.EntityPosition.X - 8 + randomOffsetX,
                        (int)MapManager.ObjLink.EntityPosition.Y - 15 + randomOffsetY);
                    _stalfos[i].SetAirPosition(32);
                    Map.Objects.SpawnObject(_stalfos[i]);
                }
            }
        }

        private void Shoot()
        {
            if (_aiComponent.CurrentStateId != "walk")
                return;

            var objShot = new BossHardhitBeetleShot(Map, new Vector2(EntityPosition.X, EntityPosition.Y - 16), 1);
            Map.Objects.SpawnObject(objShot);
        }

        private void EndWalk()
        {
            _aiComponent.ChangeState("walk");
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            var sourceRectangle = _sprite.SourceRectangle;

            _sprite.SourceRectangle.X += sourceRectangle.Width + (int)(_sprite.Scale * 2);

            _sprite.Color = _colors[_colorIndex];
            _sprite.Draw(spriteBatch);

            _sprite.SourceRectangle = sourceRectangle;
            _sprite.Color = Color.White;
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

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_hitCooldown.CurrentTime > 0 || _isDead || _aiComponent.CurrentStateId == "idle")
                return Values.HitCollision.None;

            _hitCooldown.OnInit();

            if (damageType == HitType.Boomerang)
                damage = 2;

            if (_colorIndex == 0)
                _colorCountdown.OnInit();

            if (_colorIndex == 7)
            {
                _isDead = true;
                _animator.Pause();
                _damageField.IsActive = false;
                _body.VelocityTarget = Vector2.Zero;
                _aiDamageState.OnHit(gameObject, direction, damageType, damage, false);
            }
            else
            {
                _body.Velocity.X = direction.X;
                _body.Velocity.Y = direction.Y;
                Game1.GameManager.PlaySoundEffect("D370-07-07");
            }

            OffsetColor(damage);

            return Values.HitCollision.Repelling | Values.HitCollision.Repelling0;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}
