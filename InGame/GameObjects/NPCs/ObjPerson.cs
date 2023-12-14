using System.Globalization;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    // @TODO: should probably be replaced with ObjPersonNew in most places
    internal class ObjPerson : GameObject
    {
        public BodyComponent Body;
        public readonly Animator Animator;

        private readonly string _personId;
        private string _currentAnimation;
        private int _lastDirection = -1;
        private bool _directionMode = true;

        private bool _isMoving;
        private float _movementSpeed;
        private float _movementCounter;
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        
        public ObjPerson() : base("person") { }

        public ObjPerson(Map.Map map, int posX, int posY, string personId, Rectangle bodyRectangle, Vector2 offset, string animationName) : base(map)
        {
            if (string.IsNullOrEmpty(personId))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-bodyRectangle.Width / 2, -bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height);

            _personId = personId;
            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/" + _personId);

            if (Animator == null)
            {
                IsDead = true;
                return;
            }

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, sprite, new Vector2(
                -Animator.CurrentAnimation.AnimationWidth / 2f + offset.X, 
                -Animator.CurrentAnimation.AnimationHeight + offset.Y));

            Body = new BodyComponent(EntityPosition,
                -bodyRectangle.Width / 2, -bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height, bodyRectangle.Height)
            {
                Gravity = -0.15f
            };

            if (!string.IsNullOrEmpty(animationName))
            {
                _directionMode = false;
                Animator.Play(animationName);
            }

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, Body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(Body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(Body.BodyBox, Interact));
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void Update()
        {
            if (_isMoving)
            {
                _movementCounter += Game1.DeltaTime * _movementSpeed;

                // finished moving?
                if (_movementCounter >= 1000)
                {
                    EntityPosition.Set(_endPosition);
                    _isMoving = false;
                }
                else
                {
                    var newPosition = Vector2.Lerp(_startPosition, _endPosition, _movementCounter / 1000);
                    EntityPosition.Set(newPosition);
                }
            }

            if (_directionMode)
            {
                var playerDistance = new Vector2(
                    MapManager.ObjLink.EntityPosition.X - (EntityPosition.X),
                    MapManager.ObjLink.EntityPosition.Y - (EntityPosition.Y - 4));

                var dir = 3;

                // rotate in the direction of the player
                if (playerDistance.Length() < 32)
                    dir = AnimationHelper.GetDirection(playerDistance);

                if (_lastDirection != dir)
                {
                    // look at the player
                    Animator.Play("stand_" + dir);
                    _lastDirection = dir;
                }
            }

            // finished playing
            if (_currentAnimation != null && !Animator.IsPlaying)
            {
                _currentAnimation = null;
                Game1.GameManager.SaveManager.SetString(_personId + "Finished", "1");
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_personId);
            return true;
        }

        private void KeyChanged()
        {
            // start new animation?
            var animationString = _personId + "Animation";
            var animationValues = Game1.GameManager.SaveManager.GetString(animationString);
            if (animationValues != null)
            {
                _currentAnimation = animationValues.ToLower();
                Animator.Play(_currentAnimation);
                Game1.GameManager.SaveManager.RemoveString(animationString);
            }

            // start moving?
            var moveString = _personId + "Move";
            var moveValue = Game1.GameManager.SaveManager.GetString(moveString);
            if (moveValue != null)
            {
                // offsetX, offsetY, movementSpeed
                var split = moveValue.Split(',');
                if (split.Length == 3)
                {
                    var offsetX = int.Parse(split[0]);
                    var offsetY = int.Parse(split[1]);
                    var speed = float.Parse(split[2], CultureInfo.InvariantCulture);

                    _startPosition = EntityPosition.Position;
                    _endPosition = _startPosition + new Vector2(offsetX, offsetY);
                    _movementSpeed = speed;

                    _isMoving = true;
                    _movementCounter = 0;
                }

                Game1.GameManager.SaveManager.RemoveString(moveString);
            }
        }
    }
}