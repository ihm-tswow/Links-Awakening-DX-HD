using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    class ObjFish : GameObject
    {
        public BodyComponent Body;
        public string DialogName;

        private readonly Animator _animator = new Animator();
        private readonly AiComponent _aiComponent;
        private readonly AnimationComponent _animationComponent;
        private readonly CSprite _sprite;

        private readonly Vector2 _startPosition;
        private Vector2 _goalPosition;

        private float _speed;

        private int _direction = -1;
        private int _moveDistance;
        private int _speedDistance;

        private bool _interacted;

        private float _lockOnCount = 500;
        private bool _isLockedOn;

        // type: 0: small fish; 1: big fish, 2: big fish with heart
        public ObjFish(Map.Map map, int posX, int posY, int type, int offset, int wait) : base(map)
        {
            SprEditorImage = Resources.SprNpCs;
            EditorIconSource = type == 0 ? new Rectangle(487, 142, 15, 11) : new Rectangle(528, 158, 16, 16);

            EntityPosition = new CPosition(posX, posY + 8, 0);
            EntitySize = new Rectangle(-16, -8, 48, 16);

            _startPosition = EntityPosition.Position;
            EntityPosition.Set(EntityPosition.Position + new Vector2(offset, 0));

            Body = new BodyComponent(EntityPosition, 0, -6, 16, 11, 8)
            {
                IgnoreHeight = true,
            };

            var name = type == 0 ? "fish_small" : "fish_big";

            DialogName = name;
            if (type == 2)
                DialogName += "_heart";

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/" + name);
            _animator.Play("swim");

            _speed = type == 0 ? 0.9f : 0.35f;
            _moveDistance = type == 0 ? 36 : 8;
            _speedDistance = type == 0 ? 12 : 4;

            var offsetY = type == 0 ? -6 : -8;
            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(8, offsetY));

            var waitCountdown = new AiTriggerCountdown(250, null, ToSwim);

            var stateSwim = new AiState(StateSwim);
            var stateWait = new AiState(null);
            stateWait.Trigger.Add(waitCountdown);
            var stateBite = new AiState(StateBite);
            var stateBitten = new AiState(StateBitten);
            var stateJump = new AiState(StateJump);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("swim", stateSwim);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("bite", stateBite);
            _aiComponent.States.Add("bitten", stateBitten);
            _aiComponent.States.Add("jump", stateJump);

            if (wait > 0)
            {
                _aiComponent.ChangeState("wait");
                waitCountdown.CurrentTime = wait;
            }
            else
                ToSwim();

            AddComponent(BodyComponent.Index, Body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, _sprite, Values.LayerPlayer));
            AddComponent(InteractComponent.Index, new InteractComponent(Body.BodyBox, Interact));
        }

        private bool Interact()
        {
            if (ObjLinkFishing.HasFish)
                return false;

            ObjLinkFishing.HasFish = true;

            if (!_interacted)
                ToBite();
            _interacted = true;

            return true;
        }

        private void ToSwim()
        {
            _aiComponent.ChangeState("swim");

            // set the right animation
            //_animator.IsPlaying = true;
            _animator.Play("swim");
            _animationComponent.MirroredH = _direction > 0;

            Body.Drag = 1.0f;

            _goalPosition = _startPosition;
            if (_direction < 0)
                _goalPosition += new Vector2(_moveDistance, 0);

            _direction = -_direction;
        }

        private void StateSwim()
        {
            var distance = _goalPosition - EntityPosition.Position;
            var distanceLength = distance.X * _direction;

            if (distanceLength >= _moveDistance - _speedDistance)
            {
                Body.Velocity.X = Math.Sign(distance.X) * ((1.0f - ((distanceLength - (_moveDistance - _speedDistance)) / _speedDistance)) * _speed + 0.05f);
            }

            if (distanceLength <= 0)
            {
                EntityPosition.Set(_goalPosition);
                Body.Velocity.X = 0;
                _aiComponent.ChangeState("wait");
                return;
            }

            if (distanceLength < _moveDistance / 3.0f)
            {
                _animator.IsPlaying = false;
            }

            if (distanceLength < 16)
            {
                Body.Drag = 0.95f;
            }
        }

        private void ToBite()
        {
            Body.Drag = 0.975f;
            _aiComponent.ChangeState("bite");
        }

        private void StateBite()
        {
            var goalPosition = ObjLinkFishing.HookPosition;
            var flipped = goalPosition.X < EntityPosition.X + 8;
            goalPosition.X += flipped ? 14 : -14;

            goalPosition += new Vector2(
                (float)Math.Sin(Game1.TotalGameTime * 0.005), 
                (float)Math.Cos(Game1.TotalGameTime * 0.004));

            var direction = goalPosition - new Vector2(EntityPosition.Position.X + 8, EntityPosition.Position.Y);
            var swimSpeed = 0.5f;

            if (direction.Length() < 1)
            {
                swimSpeed = direction.Length() * 0.5f;
                _isLockedOn = true;
                //ToBitten();
            }

            if (_isLockedOn)
            {
                _lockOnCount -= Game1.DeltaTime;

                if (_lockOnCount < 0)
                {
                    direction = ObjLinkFishing.HookPosition - new Vector2(EntityPosition.Position.X + 8, EntityPosition.Position.Y);
                    swimSpeed = 2.0f;

                    if (direction.Length() <= 2)
                    {
                        ToBitten();
                        return;
                    }
                }
            }

            if (direction != Vector2.Zero)
                direction.Normalize();

            Body.Velocity.X = direction.X * swimSpeed;
            Body.Velocity.Y = direction.Y * swimSpeed;

            _animator.Play("bite");
            _animationComponent.MirroredH = flipped;
            _animationComponent.UpdateSprite();
        }

        private void ToBitten()
        {
            _aiComponent.ChangeState("bitten");
            ObjLinkFishing.HookedFish = this;
            Body.Velocity = Vector3.Zero;
        }

        private void StateBitten()
        {
            if(ObjLinkFishing.HookedFish == null)
                return;

            _animator.Play("swim");

            //_body.Velocity.X = (float)Math.Sin(Game1.TotalTime * 0.005);

            var flipped = Body.Velocity.X < (_animationComponent.MirroredH ? 0.2f : -0.2f);
            _animationComponent.MirroredH = flipped;
            _animationComponent.UpdateSprite();

            // update fish position
            var position = new Vector2(EntityPosition.X + (flipped ? 0 : 16), EntityPosition.Y - 2);
            ObjLinkFishing.HookPosition = position;

            // swim to the left side
            var goalVelocity = (new Vector2(-16, 80) - EntityPosition.Position) - new Vector2(Body.Velocity.X, Body.Velocity.Y);
            if (goalVelocity != Vector2.Zero)
                goalVelocity.Normalize();
            Body.Velocity.X += goalVelocity.X * 0.025f * Game1.TimeMultiplier;
            Body.Velocity.Y += goalVelocity.Y * 0.025f * Game1.TimeMultiplier;
        }

        public void ToJump()
        {
            if(_aiComponent.CurrentStateId == "jump")
                return;

            _aiComponent.ChangeState("jump");
            Body.Drag = 1.0f;
            Body.Velocity = new Vector3(0.5f, -2.5f, 0.0f);

            // splash effect
            Game1.GameManager.PlaySoundEffect("D360-14-0E");
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 36, 0, "Particles/fishingSplash", "idle", true);
            splashAnimator.EntityPosition.Set(new Vector2(EntityPosition.X + 8, 0));
            Map.Objects.SpawnObject(splashAnimator);
        }

        private void StateJump()
        {
            Body.Velocity.Y += 0.1f * Game1.TimeMultiplier;

            if (Body.Velocity.Y > 0 && EntityPosition.Y > 28)
                _sprite.IsVisible = false;
        }
    }
}