using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBossFireball : GameObject
    {
        private readonly BossFinalBoss _finalBoss;

        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly CBox _damageBox;

        private ObjAnimator _objTrail0;
        private ObjAnimator _objTrail1;

        private bool _isMoving;
        private bool _isRepelled;
        private double _moveTime = 0;

        private const double BlinkTime = 2000 / 60.0;
        private const float Speed = 1.5f;

        private bool _isReady;

        private bool _damageState;

        public bool IsReady { get => _isReady; }

        public BossFinalBossFireball(BossFinalBoss boss, Vector2 position) : base(boss.Map)
        {
            _finalBoss = boss;

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare fireball");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -3, -3, 6, 6, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true
            };

            _aiComponent = new AiComponent();

            var state0 = new AiState() { Init = () => _animator.Play("idle_0") };
            state0.Trigger.Add(new AiTriggerCountdown(1100, null, () => _aiComponent.ChangeState("idle1")));
            var state1 = new AiState() { Init = () => _animator.Play("idle_1") };
            state1.Trigger.Add(new AiTriggerCountdown(1100, null, () => _aiComponent.ChangeState("idle2")));
            var state2 = new AiState() { Init = () => _animator.Play("idle_2") };
            state2.Trigger.Add(new AiTriggerCountdown(1100, null, () => _isReady = true));
            var stateMoving = new AiState();

            _aiComponent.States.Add("idle0", state0);
            _aiComponent.States.Add("idle1", state1);
            _aiComponent.States.Add("idle2", state2);
            _aiComponent.States.Add("moving", stateMoving);

            _aiComponent.ChangeState("idle0");

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);
            var hittableBox = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8);
            _damageBox = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        public void Fire()
        {
            Game1.GameManager.PlaySoundEffect("D378-56-38");

            _isMoving = true;
            _aiComponent.ChangeState("moving");

            if (Game1.RandomNumber.Next(0, 4) == 0)
            {
                _damageState = true;
                _body.MoveCollision = OnMoveCollision;
                _animator.Play("idle_3");
            }
            else
            {
                EntityPosition.AddPositionListener(typeof(BossFinalBossFireball), UpdateTrail);
                _body.CollisionTypes = Values.CollisionTypes.None;
                _animator.Play("idle_2");
                SpawnTrail();
            }

            var playerDirection = new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y - 4) - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
            {
                playerDirection.Normalize();
                _body.VelocityTarget = playerDirection * Speed;
            }
        }

        private void Update()
        {
            if (_isMoving)
            {
                _moveTime += Game1.DeltaTime;

                if (_isRepelled && _finalBoss.HitBoss(this, _body.VelocityTarget * 0.75f, _damageBox))
                    _moveTime = 1300;

                // fade away
                if (_moveTime > 1300)
                {
                    _sprite.Color = Color.White * MathHelper.Clamp(1 - ((float)_moveTime - 1300) / 100, 0, 1);

                    if (_objTrail0 != null)
                        _objTrail0.Sprite.Color = Color.White * MathHelper.Clamp(1 - ((float)_moveTime - 1350) / 100, 0, 1);
                    if (_objTrail1 != null)
                        _objTrail1.Sprite.Color = Color.White * MathHelper.Clamp(1 - ((float)_moveTime - 1400) / 100, 0, 1);
                }

                if (_moveTime > 1500)
                {
                    DeleteObject();
                    return;
                }
            }

            // blink
            var shader = Game1.TotalGameTime % (BlinkTime * 2) < BlinkTime ? Resources.DamageSpriteShader0 : null;
            _sprite.SpriteShader = shader;
            if (_objTrail0 != null)
                _objTrail0.Sprite.SpriteShader = shader;
            if (_objTrail1 != null)
                _objTrail1.Sprite.SpriteShader = shader;
        }

        private void SpawnTrail()
        {
            _objTrail0 = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerPlayer, "Nightmares/nightmare fireball", "idle_1", false);
            _objTrail1 = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerBottom, "Nightmares/nightmare fireball", "idle_0", false);

            Map.Objects.SpawnObject(_objTrail0);
            Map.Objects.SpawnObject(_objTrail1);
        }

        private void UpdateTrail(CPosition position)
        {
            var dist0 = _objTrail0.EntityPosition.Position - position.Position;
            if (dist0.Length() > 7)
            {
                dist0.Normalize();
                dist0 *= 7;
            }
            _objTrail0.EntityPosition.Set(EntityPosition.Position + dist0);

            var dist1 = _objTrail1.EntityPosition.Position - _objTrail0.EntityPosition.Position;
            if (dist1.Length() > 8)
            {
                dist1.Normalize();
                dist1 *= 8;
            }
            _objTrail1.EntityPosition.Set(_objTrail0.EntityPosition.Position + dist1);
        }

        private void Repell()
        {
            Game1.GameManager.PlaySoundEffect("D378-56-38");

            _isRepelled = true;
            _moveTime = 0;

            var playerDirection = EntityPosition.Position -
                new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y - 4);
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();

            _body.VelocityTarget = playerDirection * Speed;
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            OnDeath();
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if ((type & HitType.Sword) == 0 || (type & HitType.SwordHold) != 0)
                return Values.HitCollision.None;

            // can not be hit before moving for a little while
            if (_moveTime < 125)
                return Values.HitCollision.None;

            if (_damageState)
                OnDeath();
            else
                Repell();

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_damageState && type == PushableComponent.PushType.Impact)
                OnDeath();

            return true;
        }

        private void OnDeath()
        {
            var posX = (int)EntityPosition.X;
            var posY = (int)EntityPosition.Y;
            var speed = 2.75f / 1.4f;

            var objFireball0 = new EnemyFireball(Map, posX - 2, posY - 2, 1, false);
            var objFireball1 = new EnemyFireball(Map, posX + 2, posY - 2, 1, false);
            var objFireball2 = new EnemyFireball(Map, posX - 2, posY + 2, 1, false);
            var objFireball3 = new EnemyFireball(Map, posX + 2, posY + 2, 1, false);

            objFireball0.SetVelocity(new Vector2(-1, -1) * speed);
            objFireball1.SetVelocity(new Vector2(1, -1) * speed);
            objFireball2.SetVelocity(new Vector2(-1, 1) * speed);
            objFireball3.SetVelocity(new Vector2(1, 1) * speed);

            Map.Objects.SpawnObject(objFireball0);
            Map.Objects.SpawnObject(objFireball1);
            Map.Objects.SpawnObject(objFireball2);
            Map.Objects.SpawnObject(objFireball3);

            Map.Objects.DeleteObjects.Add(this);
        }

        private void DeleteObject()
        {
            if (_objTrail0 != null)
                Map.Objects.DeleteObjects.Add(_objTrail0);
            if (_objTrail1 != null)
                Map.Objects.DeleteObjects.Add(_objTrail1);

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}