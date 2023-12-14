using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBladeTrap : GameObject
    {
        private readonly AiComponent _aiComponent;

        private readonly RectangleF[] _collisionRectangles = new RectangleF[4];
        private readonly Vector2[] _directions = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1) };
        private readonly int[] _maxPosition = new int[4];

        private Vector2 _startPosition;
        private float _movePosition;
        private int _moveDir;

        public EnemyBladeTrap() : base("bladeTrap") { }

        public EnemyBladeTrap(Map.Map map, int posX, int posY, int left, int right, int top, int bottom) : base(map)
        {
            Tags = Values.GameObjectTag.Damage;

            EntityPosition = new CPosition(posX, posY, 0);
            _startPosition = new Vector2(posX, posY);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/bladetrap");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(0, 0));

            var padding = 2;
            var width = 16 + 2 * padding;
            var height = 16 + 2 * padding;

            _maxPosition[0] = left * 16;
            _maxPosition[1] = right * 16;
            _maxPosition[2] = top * 16;
            _maxPosition[3] = bottom * 16;

            _collisionRectangles[0] = new RectangleF(posX - left * 16 - 16, posY - padding, left * 16 + 16, height);
            _collisionRectangles[1] = new RectangleF(posX + 16, posY - padding, right * 16 + 16, height);
            _collisionRectangles[2] = new RectangleF(posX - padding, posY - top * 16 - 16, width, top * 16 + 16);
            _collisionRectangles[3] = new RectangleF(posX - padding, posY + 16, width, bottom * 16 + 16);

            var stateWait = new AiState();
            stateWait.Trigger.Add(new AiTriggerCountdown(350, null, () => _aiComponent.ChangeState("back")));
            var stateCooldown = new AiState();
            stateCooldown.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("idle")));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", new AiState(UpdateIdle));
            _aiComponent.States.Add("snap", new AiState(UpdateSnap));
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("back", new AiState(UpdateMoveBack));
            _aiComponent.States.Add("cooldown", stateCooldown);
            _aiComponent.ChangeState("idle");

            var bodyBox = new CBox(EntityPosition, 2, 2, 0, 12, 12, 4);
            AddComponent(PushableComponent.Index, new PushableComponent(bodyBox, OnPush) { RepelMultiplier = 1.5f });
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(bodyBox, HitType.Enemy, 4));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(bodyBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }

        private void UpdateIdle()
        {
            // trigger trap
            for (var i = 0; i < _directions.Length; i++)
                if (_collisionRectangles[i].Intersects(MapManager.ObjLink.BodyRectangle))
                {
                    Game1.GameManager.PlaySoundEffect("D378-10-0A");
                    _aiComponent.ChangeState("snap");
                    _moveDir = i;
                }
        }

        private void UpdateSnap()
        {
            _movePosition += 2 * Game1.TimeMultiplier;

            // collision?
            if (_movePosition >= _maxPosition[_moveDir])
            {
                _movePosition = _maxPosition[_moveDir];
                _aiComponent.ChangeState("wait");
                Game1.GameManager.PlaySoundEffect("D360-07-07");
            }

            UpdatePosition();
        }

        private void UpdateMoveBack()
        {
            if (_movePosition > 0)
                _movePosition -= 0.5f * Game1.TimeMultiplier;
            else
            {
                _movePosition = 0;
                _aiComponent.ChangeState("cooldown");
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            EntityPosition.Set(_startPosition + _directions[_moveDir] * _movePosition);
        }
    }
}