using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjMonkeyWorker : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;

        private readonly CPosition _monkeyPosition;

        private readonly AiTriggerSwitch _waitTimer;

        private readonly Vector2 _workPosition;
        private readonly Vector2 _endPosition;

        private const int FadeTime = 150;

        private int _direction;

        public ObjMonkeyWorker(Map.Map map, Vector2 startPosition, Vector2 workPosition, Vector2 endPosition) : base(map)
        {
            _monkeyPosition = new CPosition(startPosition.X + 8, startPosition.Y + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _workPosition = workPosition;
            _endPosition = endPosition;

            _body = new BodyComponent(_monkeyPosition, -6, -8, 12, 8, 8)
            {
                MaxJumpHeight = 4f,
                DragAir = 0.99f,
                CollisionTypes = Values.CollisionTypes.None,
                Drag = 0.85f,
                Gravity = -0.15f,
                MoveCollision = OnCollision
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/monkey");
            _sprite = new CSprite(_monkeyPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _waitTimer = new AiTriggerSwitch(150);

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerCountdown(
                Game1.RandomNumber.Next(0, 1000), null, () => _aiComponent.ChangeState("come")));
            var stateCome = new AiState(UpdateCome);
            stateCome.Trigger.Add(_waitTimer);
            var stateWork = new AiState(UpdateWork);
            stateWork.Trigger.Add(_waitTimer);
            var stateLeave = new AiState(UpdateLeave);
            stateLeave.Trigger.Add(_waitTimer);
            var stateFade = new AiState();
            stateFade.Trigger.Add(new AiTriggerCountdown(FadeTime, TickFade, () => TickFade(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("come", stateCome);
            _aiComponent.States.Add("work", stateWork);
            _aiComponent.States.Add("leave", stateLeave);
            _aiComponent.States.Add("fade", stateFade);

            _aiComponent.ChangeState("init");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            // finished jumping?
            if (moveCollision.HasFlag(Values.BodyCollision.Floor))
            {
                _waitTimer.Reset();

                if (_body.Velocity.Y > 0)
                    _animator.Play("idle_" + _direction);
                else
                    _animator.Play("idle_u_" + _direction);

                _body.Velocity = Vector3.Zero;
            }
        }

        private void UpdateCome()
        {
            if (!_body.IsGrounded || !_waitTimer.State)
                return;

            var direction = _workPosition - _monkeyPosition.Position;
            var distance = direction.Length();

            if (distance > 16)
            {
                direction.Normalize();
                var strength = Game1.RandomNumber.Next(150, 200) / 100.0f;

                if (distance < 64)
                    strength -= 0.5f;

                _body.Velocity = new Vector3(direction.X * strength, direction.Y * strength, 1.75f);

                _direction = direction.X < 0 ? 0 : 1;
                _animator.Play("jump_" + _direction);

                Game1.GameManager.PlaySoundEffect("D360-36-24", false);
            }
            else
            {
                _aiComponent.ChangeState("work");
            }
        }

        private void UpdateWork()
        {
            if (!_body.IsGrounded || !_waitTimer.State)
                return;

            var direction = _workPosition - _monkeyPosition.Position;

            direction.Normalize();
            var strength = Game1.RandomNumber.Next(20, 40) / 100.0f;
            _body.Velocity = new Vector3(direction.X * strength, direction.Y * strength, 1.25f);

            _direction = direction.X < 0 ? 0 : 1;

            _animator.Play("jump_" + _direction);
        }

        public void ToLeave()
        {
            _aiComponent.ChangeState("leave");
        }

        private void UpdateLeave()
        {
            if (!_body.IsGrounded || !_waitTimer.State)
                return;

            var direction = _endPosition - _monkeyPosition.Position;
            var distance = direction.Length();

            direction.Normalize();
            var strength = Game1.RandomNumber.Next(150, 200) / 100.0f;
            _body.Velocity = new Vector3(direction.X * strength, direction.Y * strength, 1.75f);

            _direction = direction.X < 0 ? 0 : 1;
            _animator.Play("jump_" + _direction);

            Game1.GameManager.PlaySoundEffect("D360-36-24", false);

            // start fading away
            if (distance < 48)
                _aiComponent.ChangeState("fade");
        }

        private void TickFade(double time)
        {
            _sprite.Color = Color.White * (float)(time / FadeTime);

            // delete the monkey after it is faded away
            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}