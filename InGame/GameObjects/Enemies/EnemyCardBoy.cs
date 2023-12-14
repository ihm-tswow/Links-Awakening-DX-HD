using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyCardBoy : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly DamageFieldComponent _damgeField;

        private readonly string _key;
        private readonly int _index;

        private float _changeTime = 500;
        private float _changeCounter;
        private float _walkSpeed = 0.5f;
        private int _cardIndex;
        private int _dir;

        public EnemyCardBoy() : base("card boy") { }

        public EnemyCardBoy(Map.Map map, int posX, int posY, int index, string key) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _index = index;

            if (string.IsNullOrEmpty(key))
            {
                IsDead = true;
                return;
            }

            _key = key;
            if (Game1.GameManager.SaveManager.GetString(_key) == "1")
                IsDead = true;

            Game1.GameManager.SaveManager.RemoveInt(_key + _index);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/card boy");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy |
                    Values.CollisionTypes.Player |
                    Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Drag = 0.75f,
            };

            var stateIdle = new AiState(Update);
            stateIdle.Trigger.Add(new AiTriggerRandomTime(ToWalking, 250, 500));
            var stateWalking = new AiState(Update);
            stateWalking.Trigger.Add(new AiTriggerRandomTime(ToIdle, 750, 1000));
            var stateWaiting = new AiState();
            var stateDamage = new AiState();
            stateDamage.Trigger.Add(new AiTriggerCountdown(400, DamageTick, FinishDamage));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("damage", stateDamage);
            _aiComponent.States.Add("waiting", stateWaiting);

            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -8, -14, 0, 16, 13, 4);
            var hittableBox = new CBox(EntityPosition, -8, -15, 16, 14, 8);
            var pushableBox = new CBox(EntityPosition, -7, -14, 14, 13, 8);

            AddComponent(DamageFieldComponent.Index, _damgeField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));
        }

        private void ToIdle()
        {
            _damgeField.IsActive = true;
            _aiComponent.ChangeState("idle");
            _body.VelocityTarget = Vector2.Zero;
        }

        private void ToWalking()
        {
            _aiComponent.ChangeState("walking");
            // random new direction
            _dir = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_dir] * _walkSpeed;
        }

        private void Update()
        {
            _changeCounter += Game1.DeltaTime;
            if (_changeCounter > _changeTime * 4)
                _changeCounter -= _changeTime * 4;

            _cardIndex = (int)(_changeCounter / _changeTime);

            var time = _animator.FrameCounter;
            var frame = _animator.CurrentFrameIndex;
            _animator.Play((_cardIndex + 1).ToString(), frame, time);
            _animator.IsPlaying = _aiComponent.CurrentStateId == "walking";
        }

        private void KeyChanged()
        {
            // reset boy
            if (_aiComponent.CurrentStateId == "waiting" &&
                Game1.GameManager.SaveManager.GetInt(_key + _index, -1) == -1)
                _aiComponent.ChangeState("idle");

            if (Game1.GameManager.SaveManager.GetString(_key) == "1")
                RemoveEntity();
            else
                CheckOther();
        }

        private void RemoveEntity()
        {
            Map.Objects.SpawnObject(
                new ObjAnimator(Map, (int)EntityPosition.X - 16, (int)EntityPosition.Y - 24, Values.LayerTop, "Particles/explosion", "run", true));
            Map.Objects.DeleteObjects.Add(this);
        }

        private void CheckOther()
        {
            // all boys set
            var resetBoys = true;
            // all boy states equal
            var allEqual = true;
            for (var i = 0; i < 3; i++)
            {
                if (Game1.GameManager.SaveManager.GetInt(_key + i, -1) == -1)
                    resetBoys = false;
                if (Game1.GameManager.SaveManager.GetInt(_key + i, -1) != _cardIndex)
                    allEqual = false;
            }

            if (!allEqual && resetBoys)
            {
                Game1.GameManager.PlaySoundEffect("D360-29-1D");

                for (var i = 0; i < 3; i++)
                    Game1.GameManager.SaveManager.RemoveInt(_key + i);
            }

            // all card boys have the same state
            if (allEqual)
            {
                Game1.GameManager.SaveManager.SetString(_key, "1");
                Game1.GameManager.PlaySoundEffect("D378-19-13");
            }
        }

        private void AddDamage()
        {
            _aiComponent.ChangeState("damage");
            _body.VelocityTarget = Vector2.Zero;
            _animator.IsPlaying = false;
            _damgeField.IsActive = false;
        }

        private void DamageTick(double time)
        {
            _sprite.SpriteShader = time % 133 < 66 ? Resources.DamageSpriteShader0 : null;
        }

        private void FinishDamage()
        {
            _sprite.SpriteShader = null;
            _aiComponent.ChangeState("waiting");
            Game1.GameManager.SaveManager.SetInt(_key + _index, _cardIndex);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "damage")
                return Values.HitCollision.None;

            Game1.GameManager.PlaySoundEffect("D360-03-03");

            _body.Velocity.X = direction.X * 2.5f;
            _body.Velocity.Y = direction.Y * 2.5f;

            AddDamage();

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

                if (_aiComponent.CurrentStateId != "damage" &&
                    _aiComponent.CurrentStateId != "waiting")
                    AddDamage();
            }

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId != "walking")
                return;

            if (direction == Values.BodyCollision.Vertical)
                _body.VelocityTarget.Y = 0;
            else if (direction == Values.BodyCollision.Horizontal)
                _body.VelocityTarget.X = 0;
        }
    }
}