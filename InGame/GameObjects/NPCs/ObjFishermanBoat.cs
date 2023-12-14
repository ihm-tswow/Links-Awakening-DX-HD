using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjFishermanBoat : GameObject
    {
        private readonly ObjPhotoMouse _photoMouse;
        private bool _pullMouse;
        private bool _pulledMouse;
        private bool _falling;

        public BodyComponent Body;
        public readonly Animator Animator;
        private readonly CSprite _sprite;
        private readonly BodyDrawComponent _drawComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly BodyCollisionComponent _collisionComponent;
        private readonly InteractComponent _interactionComponent;

        private readonly Vector2 _spawnPosition;

        private readonly string _dialogId;
        private string _currentAnimation;
        private string _spawnCondition;
        private bool _directionMode = true;

        public ObjFishermanBoat(Map.Map map, int posX, int posY, string spawnCondition, string animationId, string dialogId, Rectangle bodyRectangle) : base(map)
        {
            SprEditorImage = Resources.SprNpCs;
            EditorIconSource = new Rectangle(276, 2, 15, 16);

            if (string.IsNullOrEmpty(animationId))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(bodyRectangle.X - bodyRectangle.Width / 2, bodyRectangle.Y - bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height);

            _spawnPosition = EntityPosition.Position;

            _spawnCondition = spawnCondition;
            _dialogId = dialogId;
            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/" + animationId);

            if (Animator == null)
            {
                IsDead = true;
                return;
            }

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, _sprite, Vector2.Zero);

            Body = new BodyComponent(EntityPosition,
                bodyRectangle.X - bodyRectangle.Width / 2, bodyRectangle.Y - bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height, bodyRectangle.Height)
            {
                Gravity = -0.15f,
            };

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(BodyComponent.Index, Body);
            // only the player should collide with the npc
            AddComponent(CollisionComponent.Index, _collisionComponent = new BodyCollisionComponent(Body, Values.CollisionTypes.Enemy | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, _interactionComponent = new InteractComponent(Body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new BodyDrawComponent(Body, _sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(Body, _sprite));

            if (Game1.GameManager.SaveManager.GetString("photoMouseActive") == "1" &&
                Game1.GameManager.SaveManager.GetString("photo_sequence_bridge") == null)
            {
                _photoMouse = new ObjPhotoMouse(map, posX - 17, posY + 40, null, "mouseSeqBoat");
                map.Objects.SpawnObject(_photoMouse);
            }
        }

        private void SetActive(bool isActive)
        {
            _collisionComponent.IsActive = isActive;
            _interactionComponent.IsActive = isActive;
            _drawComponent.IsActive = isActive;
            _shadowComponent.IsActive = isActive;
        }

        private void Update()
        {
            if (_directionMode)
            {
                var playerDistance = new Vector2(
                    MapManager.ObjLink.EntityPosition.X - (EntityPosition.X),
                    MapManager.ObjLink.EntityPosition.Y - (EntityPosition.Y - 4));

                var dir = 3;

                // rotate in the direction of the player
                if (playerDistance.Length() < 32)
                    dir = AnimationHelper.GetDirection(playerDistance);

                // look at the player
                if (_currentAnimation == null)
                {
                    var animationIndex = Animator.GetAnimationIndex("stand_" + dir);
                    if (animationIndex >= 0)
                        Animator.Play(animationIndex);
                    else
                        Animator.Play("stand_" + (playerDistance.Y < 0 ? "1" : "3"));
                }
            }

            // finished playing
            if (_currentAnimation != null && !Animator.IsPlaying)
            {
                _currentAnimation = null;
                Game1.GameManager.SaveManager.SetString(_dialogId + "Finished", "1");
            }

            if (_pullMouse && _photoMouse != null && !_falling)
            {
                var targetPosition = new Vector2(EntityPosition.X - 25, EntityPosition.Y - 1);
                var pullDirection = targetPosition - _photoMouse.EntityPosition.Position;
                // pull slower in water
                var pullSpeed = (_photoMouse.Body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0 ? 0.25f : 0.5f;

                if (pullDirection.Length() > pullSpeed * Game1.TimeMultiplier)
                {
                    pullDirection.Normalize();
                    _photoMouse.Body.VelocityTarget = pullDirection * pullSpeed;
                }
                else
                {
                    if (!_pulledMouse)
                    {
                        _pulledMouse = true;
                        Game1.GameManager.SaveManager.SetString("mousePulledUp", "1");
                    }

                    _photoMouse.Body.VelocityTarget = Vector2.Zero;
                    _photoMouse.EntityPosition.Set(targetPosition);
                }
            }
        }

        public void DisableRotating()
        {
            _directionMode = false;
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_dialogId);
            return true;
        }

        private void SetVisibility(bool visible)
        {
            _sprite.IsVisible = visible;
            _shadowComponent.IsActive = visible;
        }

        private void OnKeyChange()
        {
            if (_spawnCondition != null)
            {
                var spawnValue = Game1.GameManager.SaveManager.GetString(_spawnCondition);
                if (spawnValue == "1")
                    SetActive(true);
            }

            // start new animation?
            var animationString = _dialogId + "Animation";
            var animationValues = Game1.GameManager.SaveManager.GetString(animationString);
            if (animationValues != null)
            {
                if (animationValues == "-")
                {
                    _currentAnimation = null;
                }
                else if (animationValues != "")
                {
                    SetVisibility(true);
                    _currentAnimation = animationValues;
                    Animator.Play(_currentAnimation);
                }
                else
                {
                    SetVisibility(false);
                    _currentAnimation = null;
                }

                Game1.GameManager.SaveManager.RemoveString(animationString);
            }

            var pullMouseString = "mousePullUp";
            var pullMouseValue = Game1.GameManager.SaveManager.GetString(pullMouseString);
            if (!string.IsNullOrEmpty(pullMouseValue))
            {
                _pullMouse = true;
                Game1.GameManager.SaveManager.RemoveString(pullMouseString);
            }

            var fallString = "fisherman_fall";
            var fallValue = Game1.GameManager.SaveManager.GetString(fallString);
            if (!string.IsNullOrEmpty(fallValue))
            {
                _falling = true;
                Body.Velocity = new Vector3(-1.75f, -0.75f, 0);
                if (_photoMouse != null)
                {
                    _photoMouse.Body.IgnoresZ = false;
                    _photoMouse.Body.Velocity.X = -0.25f;
                }
                Game1.GameManager.SaveManager.RemoveString(fallString);
            }

            var resetString = "fisherman_reset";
            var resetValue = Game1.GameManager.SaveManager.GetString(resetString);
            if (!string.IsNullOrEmpty(resetValue))
            {
                _pullMouse = false;
                // reset the position and remove the photo mouse
                // must be ontop of the boat
                EntityPosition.Set(new Vector2(_spawnPosition.X, _spawnPosition.Y - 2));
                if (_photoMouse != null)
                    Map.Objects.DeleteObjects.Add(_photoMouse);
                Game1.GameManager.SaveManager.RemoveString(resetString);
            }

        }
    }
}