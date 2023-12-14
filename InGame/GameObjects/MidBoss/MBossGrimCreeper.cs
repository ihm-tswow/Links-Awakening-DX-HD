using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossGrimCreeper : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AnimationComponent _animatorComponent;
        private readonly CSprite _sprite;
        private readonly AiComponent _aiComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;

        private readonly MBossGrimCreeperFly[] _fly = new MBossGrimCreeperFly[6];

        private Vector2 _flyOrigin;
        private Vector2[] _positions = new[] {
            new Vector2(1, 2), new Vector2(6, 2), new Vector2(2, 3), new Vector2(3, 2),new Vector2(5, 3), new Vector2(4, 2),
            new Vector2(2, 1), new Vector2(5, 1), new Vector2(2, 3), new Vector2(5, 3), new Vector2(2, 5), new Vector2(5, 5),
            new Vector2(2, 2), new Vector2(5, 4), new Vector2(3.5f, 1), new Vector2(3.5f, 5), new Vector2(2, 4), new Vector2(5, 2),
            new Vector2(1, 1), new Vector2(6, 5), new Vector2(6, 1), new Vector2(1, 5), new Vector2(3.5f, 1), new Vector2(3.5f, 5),
            new Vector2(1, 0), new Vector2(2, 1), new Vector2(3, 2), new Vector2(4, 2), new Vector2(5, 1), new Vector2(6, 0),
            new Vector2(1, 1), new Vector2(6, 1), new Vector2(6, 5), new Vector2(1, 5), new Vector2(4.5f, 3), new Vector2(2.5f, 3),
            new Vector2(0, 1.5f), new Vector2(7, 1.5f), new Vector2(1, 3.5f), new Vector2(6, 3.5f), new Vector2(4.5f, 5), new Vector2(2.5f, 5),
            new Vector2(2, 4), new Vector2(5, 4), new Vector2(3, 3), new Vector2(4, 5), new Vector2(3, 5), new Vector2(4, 3),
        };

        private readonly string _saveKey;

        private int _flyIndex;

        private float _spawnCounter;
        private const float SpawnTime = 500;
        private int _spawnIndex;

        private float _attackCounter;
        private const float AttackTime = 550;
        private int _attackIndex;

        private bool _initDialog;

        public MBossGrimCreeper() : base("grim creeper") { }

        public MBossGrimCreeper(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 32);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _saveKey = saveKey;

            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/grim creeper");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                IsActive = false,
                IsGrounded = false,
                Gravity = -0.125f,
                FieldRectangle = Map.GetField(posX, posY, 16)
            };

            var stateIdle = new AiState(UpdateIdle);
            var stateSpawn = new AiState(UpdateSpawn) { Init = InitSpawn };
            var statePostSpawn = new AiState();
            statePostSpawn.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("summoning")));
            var stateSummoning = new AiState(UpdateSummoning) { Init = InitSummoning };
            var stateWait = new AiState() { Init = InitWait };
            stateWait.Trigger.Add(new AiTriggerCountdown(3500, null, () => _aiComponent.ChangeState("attack")));
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };
            var statePostAttack = new AiState(UpdatePostAttack);
            statePostAttack.Trigger.Add(new AiTriggerCountdown(1500, null, EndPostAttack));
            var stateFinished = new AiState();
            stateFinished.Trigger.Add(new AiTriggerCountdown(750, null, () => _aiComponent.ChangeState("preJump")));
            var statePreJump = new AiState() { Init = InitPreJump };
            statePreJump.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("jump")));
            var stateJump = new AiState(UpdateJump) { Init = InitJump };

            // evil eagle states
            var stateSequence = new AiState(UpdateSaddleJump);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("postSpawn", statePostSpawn);
            _aiComponent.States.Add("summoning", stateSummoning);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("postAttack", statePostAttack);
            _aiComponent.States.Add("finished", stateFinished);
            _aiComponent.States.Add("preJump", statePreJump);
            _aiComponent.States.Add("jump", stateJump);
            _aiComponent.States.Add("sequence", stateSequence);
            _aiComponent.ChangeState("idle");

            var damageCollider = new CBox(EntityPosition, -8, -16, 0, 16, 16, 8);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite));

            UpdateTransparency(24);
            _flyOrigin = new Vector2(posX - 48, posY + 8 + 43);
        }

        public void StartNightmareSequnece()
        {
            EntityPosition.Z = 0;
            UpdateTransparency(24);

            _aiComponent.ChangeState("sequence");
            _animator.Play("stand");
        }

        public void StartSaddleJump()
        {
            _body.IsActive = true;
            _body.Velocity.X = 0.9f;
            _body.Velocity.Y = -2.65f;
            _body.DragAir = 1;
            _body.IsGrounded = false;
        }

        private void UpdateSaddleJump()
        {
            // check if we are behind the evil eagle
            if (_body.Velocity.Y > 2 || _body.IsGrounded)
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void UpdateIdle()
        {
            // player entered the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                _aiComponent.ChangeState("spawn");
            }
        }

        private void InitSpawn()
        {
            // fall down
            _body.IsActive = true;
            _animator.Play("attack");

            Game1.GameManager.SetMusic(79, 2);
        }

        private void UpdateSpawn()
        {
            UpdateTransparency(24);

            if (_body.IsGrounded)
            {
                Game1.GameManager.StartDialogPath("grim_creeper_enter");
                _animator.Play("play");
                _aiComponent.ChangeState("postSpawn");
            }
        }

        private void InitSummoning()
        {
            _flyIndex = Game1.RandomNumber.Next(0, 8);
            _spawnCounter = 0;
            _spawnIndex = 0;
            _animator.Play("play");
        }

        private void UpdateSummoning()
        {
            _spawnCounter += Game1.DeltaTime;

            if (_spawnCounter < SpawnTime * _spawnIndex)
                return;

            var index = _flyIndex * 6 + _spawnIndex;
            var targetPosition = _flyOrigin + new Vector2(_positions[index].X, _positions[index].Y) * 16;
            var randomOffsetX = Game1.RandomNumber.Next(0, 32) - 16;
            var startPosition = targetPosition + new Vector2(randomOffsetX, 64) * (_positions[index].Y > 4 ? 1 : -1);
            _fly[_spawnIndex] = new MBossGrimCreeperFly(Map, startPosition, targetPosition);
            _fly[_spawnIndex].FightInit();
            Map.Objects.SpawnObject(_fly[_spawnIndex]);

            _spawnIndex++;

            if (_spawnIndex >= 6)
                _aiComponent.ChangeState("wait");
        }

        private void InitWait()
        {
            _animator.Play("idle");
        }

        private void InitAttack()
        {
            _attackCounter = 0;
            _attackIndex = 0;
            _animator.Play("attack");
        }

        private void UpdateAttack()
        {
            _attackCounter += Game1.DeltaTime;

            if (_attackCounter < AttackTime * _attackIndex)
                return;

            if (_attackIndex >= 6)
            {
                _aiComponent.ChangeState("postAttack");
            }
            else
            {
                _fly[_attackIndex].StartAttack();
                _attackIndex++;
            }
        }

        private void UpdatePostAttack()
        {
            // killed all flies?
            var finished = true;
            for (var i = 0; i < _fly.Length; i++)
            {
                if (_fly[i].IsAlive())
                {
                    finished = false;
                    break;
                }
            }

            if (finished)
                _aiComponent.ChangeState("finished");
        }

        private void EndPostAttack()
        {
            if (!_initDialog)
            {
                _initDialog = true;
                Game1.GameManager.StartDialogPath("grim_creeper_1");
            }

            _aiComponent.ChangeState("summoning");
        }

        private void InitJump()
        {
            Game1.GameManager.PlaySoundEffect("D378-62-3F");

            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");
        }

        private void UpdateJump()
        {
            if (_body.IsGrounded)
                _body.Velocity.Z = 3.5f;

            // fade out
            UpdateTransparency(16);

            // completely invisible => despawn
            if (_shadowComponent.Transparency == 0 || _body.Velocity.Z < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void InitPreJump()
        {
            Game1.GameManager.StartDialogPath("grim_creeper_end");
        }

        private void UpdateTransparency(int offset)
        {
            var transparency = 1 - MathHelper.Clamp((EntityPosition.Z - offset) / 8, 0, 1);
            _sprite.Color = Color.White * transparency;
            _shadowComponent.Transparency = transparency;
        }
    }
}