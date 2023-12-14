using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjGhost : GameObjectFollower
    {
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly AiComponent _aiComponent;

        private Vector2 _followVelocity;
        private Vector2 _targetPosition;

        private float _moveSpeed;
        private float _fadeTime;
        private float _fadeCounter;

        private const int FlyHeight = 14;

        private bool _returning;
        private bool _fadingIn;

        // @TODO: 2d maps?
        // the position when the position dialog was started the last time
        private Vector2 _lastDialogPosition;

        public ObjGhost() : base("ghost") { }

        public ObjGhost(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, FlyHeight);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            var ghostState = Game1.GameManager.SaveManager.GetString("ghost_state");

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/ghost");

            _sprite = new CSprite(EntityPosition);
            _sprite.Color = Color.White * 0.75f;
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 8)
            {
                IgnoreHoles = true,
                IgnoresZ = true,
                Gravity = -0.15f,
                CollisionTypes = Values.CollisionTypes.None
            };

            _aiComponent = new AiComponent();

            var stateFade = new AiState(UpdateFade);
            var stateMove = new AiState(UpdateMoving);
            var stateStartFollow = new AiState(UpdateFollowPlayer);
            var stateFollow = new AiState(UpdateFollowPlayer);
            var stateReturn = new AiState(UpdateMoving) { Init = InitReturn };

            _aiComponent.States.Add("fade", stateFade);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("startFollow", stateStartFollow);
            _aiComponent.States.Add("follow", stateStartFollow);
            _aiComponent.States.Add("return", stateReturn);

            _aiComponent.ChangeState("follow");

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite));
        }

        public override void Init()
        {
            _shadowComponent.IsActive = !Map.Is2dMap;
        }

        public void StartFollowing()
        {
            // offset the position in a random direction
            var radiant = Game1.RandomNumber.Next(0, 314 * 2) / 100f;
            var offset = new Vector2(MathF.Sin(radiant), MathF.Cos(radiant)) * 48;
            EntityPosition.Offset(offset);
            _lastDialogPosition = EntityPosition.Position;

            _fadingIn = true;
            _fadeCounter = 0;
            _fadeTime = 500;

            // set up variables
            Game1.GameManager.StartDialogPath("ghost_start_following");

            _aiComponent.ChangeState("startFollow");
        }

        public override void SetPosition(Vector2 position)
        {
            _lastDialogPosition = position;
            EntityPosition.Set(position);
        }

        private void InitReturn()
        {
            var graveStone = Map.Objects.GetObjectOfType(
                (int)MapManager.ObjLink.EntityPosition.X - 32,
                (int)MapManager.ObjLink.EntityPosition.Y - 32, 64, 64, typeof(ObjMoveStone));
            if (graveStone != null)
            {
                _targetPosition = new Vector2(graveStone.EntityPosition.X + 8, graveStone.EntityPosition.Y);
                _moveSpeed = 0.5f;
            }
        }

        private void UpdateFollowPlayer()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var playerDistance = playerDirection.Length();
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            var movementSpeed = MathHelper.Clamp((playerDistance - 16) / 32, 0, 2);

            // move towards the player
            _followVelocity = AnimationHelper.MoveToTarget(_followVelocity, playerDirection * movementSpeed, 0.1f * Game1.DeltaTime);
            _body.VelocityTarget = _followVelocity;

            // fly up and down
            var targetPosZ = FlyHeight + MathF.Sin(((float)Game1.TotalGameTime / 1000) * MathF.PI * 2) * 1.5f;
            EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, targetPosZ, 1 * Game1.TimeMultiplier);

            // play walk/stand animation
            if (_followVelocity.Length() > 0.1f)
                UpdateAnimation(_followVelocity);

            // start the dialog if we walked some amount
            var dialogDistance = _lastDialogPosition - EntityPosition.Position;
            if (dialogDistance.Length() > 160)
            {
                _lastDialogPosition = EntityPosition.Position;
                Game1.GameManager.StartDialogPath("ghost_return");
            }

            // fade in
            if (_fadingIn)
            {
                _fadeCounter += Game1.DeltaTime;
                if (_fadeCounter >= _fadeTime)
                {
                    _fadeCounter = _fadeTime;
                    _fadingIn = false;
                }

                var percentage = _fadeCounter / _fadeTime;
                _sprite.Color = Color.White * percentage;
                _shadowComponent.Transparency = percentage;
            }
        }

        private void UpdateMoving()
        {
            // move towards the target position
            var targetDirection = _targetPosition - EntityPosition.Position;
            if (targetDirection.Length() > _moveSpeed * Game1.TimeMultiplier)
            {
                targetDirection.Normalize();
                _body.VelocityTarget = targetDirection * _moveSpeed;
                UpdateAnimation(targetDirection * _moveSpeed);
            }
            // finished walking
            else
            {
                _body.VelocityTarget = Vector2.Zero;
                EntityPosition.Set(_targetPosition);
                if (_returning)
                    _animator.Play("right");
            }
        }

        private void UpdateFade()
        {
            if (_fadeTime <= 0)
                return;

            _fadeCounter -= Game1.DeltaTime;

            if (_fadeCounter <= 0)
                Map.Objects.DeleteObjects.Add(this);
            else
            {
                EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, 0, 0.5f * Game1.TimeMultiplier);

                var percentage = _fadeCounter / _fadeTime;
                _sprite.Color = Color.White * percentage;
                _shadowComponent.Transparency = percentage;
            }
        }

        private void UpdateAnimation(Vector2 direction)
        {
            var dir = Math.Abs(direction.X) / Math.Abs(direction.Y);
            if (direction.Y >= 0 || dir > 0.75f)
                _animator.Play(direction.X < 0 ? "left" : "right");
            else
                _animator.Play(direction.X < 0 ? "up_left" : "up_right");
        }

        private void KeyChanged()
        {
            if (!IsActive)
                return;

            // start following
            var followValue = Game1.GameManager.SaveManager.GetString("ghost_follow");
            if (!string.IsNullOrEmpty(followValue))
            {
                _aiComponent.ChangeState("follow");
                Game1.GameManager.SaveManager.RemoveString("ghost_follow");
            }

            // start fading away?
            var fadeValue = Game1.GameManager.SaveManager.GetString("ghost_fade");
            if (!string.IsNullOrEmpty(fadeValue))
            {
                _fadeTime = int.Parse(fadeValue);
                _fadeCounter = _fadeTime;
                _aiComponent.ChangeState("fade");

                Game1.GameManager.SaveManager.RemoveString("ghost_fade");
            }

            // start moving? [set:ghost_move,-16,32,1]
            var moveValue = Game1.GameManager.SaveManager.GetString("ghost_move");
            if (!string.IsNullOrEmpty(moveValue))
            {
                var split = moveValue.Split(',');
                var offsetX = int.Parse(split[0]);
                var offsetY = int.Parse(split[1]);
                _moveSpeed = float.Parse(split[2], CultureInfo.InvariantCulture);
                _targetPosition = new Vector2(EntityPosition.X + offsetX, EntityPosition.Y + offsetY);
                _aiComponent.ChangeState("move");

                Game1.GameManager.SaveManager.RemoveString("ghost_move");
            }

            // return to the grave
            var returnValue = Game1.GameManager.SaveManager.GetString("ghost_return");
            if (!string.IsNullOrEmpty(returnValue))
            {
                _returning = true;
                _aiComponent.ChangeState("return");
                Game1.GameManager.SaveManager.RemoveString("ghost_return");
            }
        }
    }
}