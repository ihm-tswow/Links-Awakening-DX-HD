using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.Base;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBossWeapon : GameObject
    {
        private readonly BossFinalBoss _owner;

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private float _soundCounter;

        public BossFinalBossWeapon(Map.Map map, BossFinalBoss owner, int posX, int posY, int dir) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-28, -28, 56, 56);

            _owner = owner;

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare ganon weapon");
            _animator.Play("throw_" + dir);

            _sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -4, 10, 8, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            _aiComponent = new AiComponent();

            var stateForward = new AiState(UpdateForward) { Init = InitForward };
            stateForward.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("backward")));
            var stateBackward = new AiState(UpdateBackward) { Init = InitBackward };

            _aiComponent.States.Add("forward", stateForward);
            _aiComponent.States.Add("backward", stateBackward);

            _aiComponent.ChangeState("forward");

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void InitForward()
        {
            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _body.VelocityTarget = direction * 1.25f;
            }
        }

        private void UpdateForward()
        {

        }

        private void InitBackward()
        {

        }

        private void UpdateBackward()
        {
            var direction = new Vector2(_owner.EntityPosition.X, _owner.EntityPosition.Y - 24) - EntityPosition.Position;

            if (direction.Length() < 4)
            {
                _owner.CatchWeapon();
                Despawn();
                return;
            }

            if (direction != Vector2.Zero)
                direction.Normalize();

            _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, direction * 1.25f, 0.075f * Game1.TimeMultiplier);
        }

        private void Update()
        {
            _soundCounter -= Game1.DeltaTime;
            if (_soundCounter < 0)
            {
                _soundCounter += 250;
                Game1.GameManager.PlaySoundEffect("D378-58-3A");
            }

            var rectangle = _animator.CollisionRectangle;

            // collider is mirrored because the animation only supports one collider
            var collisionBox0 = new Box(EntityPosition.X + rectangle.X, EntityPosition.Y + rectangle.Y, 0, rectangle.Width, rectangle.Height, 8);
            var collisionBox1 = new Box(EntityPosition.X - rectangle.X - rectangle.Width,
                EntityPosition.Y - rectangle.Y - rectangle.Height, 0, rectangle.Width, rectangle.Height, 8);

            // hit the player
            if (collisionBox0.Intersects(MapManager.ObjLink._body.BodyBox.Box))
                MapManager.ObjLink.HitPlayer(collisionBox0, HitType.Enemy, 1);
            if (collisionBox1.Intersects(MapManager.ObjLink._body.BodyBox.Box))
                MapManager.ObjLink.HitPlayer(collisionBox1, HitType.Enemy, 1);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            return;

            var rectangle = _animator.CollisionRectangle;

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)EntityPosition.Position.X + rectangle.X, (int)EntityPosition.Position.Y + rectangle.Y, rectangle.Width, rectangle.Height), Color.White * 0.5f);

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)EntityPosition.Position.X - rectangle.X - rectangle.Width, (int)EntityPosition.Position.Y - rectangle.Y - rectangle.Height, rectangle.Width, rectangle.Height), Color.White * 0.5f);
        }

        private void Despawn()
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}