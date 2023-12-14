using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjChickenDude : GameObject
    {
        private readonly ObjAnimator _objChicken;

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private Vector2 _direction;
        private int _aniDirection;

        private string _dialogId;
        private double _flyCounter;
        private bool _flyingMode;

        private double _dialogCounter;

        private int FlyTime = 1000;

        private int _powderDir = -1;

        public ObjChickenDude() : base("npc_chicken_dude") { }

        public ObjChickenDude(Map.Map map, int posX, int posY, string dialogId) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _dialogId = dialogId;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_chicken_dude");
            _animator.Play("idle");

            if (!string.IsNullOrEmpty(_dialogId) && Game1.GameManager.SaveManager.GetString(_dialogId) == "2")
            {
                _flyingMode = true;
                _animator.Play("fly_forward_1");

                _objChicken = new ObjAnimator(map, posX, posY, Values.LayerPlayer, "NPCs/cock", "stand_0", false);
                _objChicken.Animator.SpeedMultiplier = 2;
                map.Objects.SpawnObject(_objChicken);

                EntityPosition.Z = 16;
                EntityPosition.AddPositionListener(typeof(ObjChickenDude), UpdateChickenPosition);
                UpdateChickenPosition(EntityPosition);

                NewDirection();
            }

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
            {
                Gravity = -0.15f,
                Drag = 0.95f,
                DragAir = 0.995f,
                IgnoresZ = true,
                JumpStartHeight = 8,
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                MoveCollision = OnMoveCollision
            };

            var stateIdle = new AiState() { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerCountdown(250, null, () => _aiComponent.ChangeState("powder")));
            var statePowder = new AiState() { Init = InitPowder };
            statePowder.Trigger.Add(new AiTriggerCountdown(850, null, () => _aiComponent.ChangeState("idle")));
            var stateFlying = new AiState(UpdateFlying) { Init = InitIdle };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("powder", statePowder);
            _aiComponent.States.Add("flying", stateFlying);

            _aiComponent.ChangeState(_flyingMode ? "flying" : "idle");

            if (!string.IsNullOrEmpty(_dialogId) && !_flyingMode)
                AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void InitIdle()
        {
            _animator.Play("idle_" + _powderDir);
        }

        private void InitPowder()
        {
            // change direction?
            if (Game1.RandomNumber.Next(0, 3) == 0)
                _powderDir = -_powderDir;

            _animator.Play("powder_" + _powderDir);

            var spawnPosition = new Vector2(EntityPosition.X + _powderDir * 10, EntityPosition.Y);
            Map.Objects.SpawnObject(new ObjPowder(Map, spawnPosition.X, spawnPosition.Y, 0, false));
        }

        private void NewDirection()
        {
            var radDirection = Game1.RandomNumber.Next(0, 100) / 100f * MathF.PI * 2;
            _direction = new Vector2(MathF.Sin(radDirection), MathF.Cos(radDirection));
        }

        private void StartMoving()
        {
            // do not move while the dialog is open
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return;

            _body.Velocity.X = _direction.X * 0.65f;
            _body.Velocity.Y = _direction.Y * 0.65f;
        }

        private void UpdateChickenPosition(CPosition newPosition)
        {
            _objChicken.EntityPosition.Set(new Vector3(newPosition.X, newPosition.Y, newPosition.Z + 16));
        }

        private void UpdateFlying()
        {
            DialogTriggerUpdate();

            _flyCounter -= Game1.DeltaTime;
            var _hoverCounter = (_flyCounter + FlyTime - FlyTime / 4) % FlyTime;

            if (_direction.X != 0)
                _aniDirection = _direction.X < 0 ? -1 : 1;

            // we align the animation with the forward movement
            if (FlyTime / 4 < _hoverCounter && _hoverCounter < FlyTime - FlyTime / 4)
                _animator.Play("fly_stop_" + _aniDirection);
            else
                _animator.Play("fly_forward_" + _aniDirection);

            _objChicken.Animator.Play("stand_" + (_aniDirection == -1 ? 0 : 2));

            // move up/down
            EntityPosition.Z = 13 + (1.5f + MathF.Sin((float)_hoverCounter / FlyTime * MathF.PI * 2) * 1.5f);

            if (_flyCounter < 0)
            {
                _flyCounter += FlyTime;
                // change the direction randomly
                if (Game1.RandomNumber.Next(0, 4) == 0)
                    NewDirection();

                StartMoving();
            }
        }

        private void DialogTriggerUpdate()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return;

            // start the dialgo when we are near the player
            // make sure there to pause between the dialogs
            if (_dialogCounter > 0)
                _dialogCounter -= Game1.DeltaTime;
            else
            {
                var playerDirection = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                if (playerDirection.Length() < 24)
                {
                    Game1.GameManager.StartDialogPath(_dialogId);
                    _dialogCounter = 3500;
                    _body.Velocity = Vector3.Zero;
                }
            }
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            // change the direction when we hit a wall
            if ((collision & Values.BodyCollision.Horizontal) != 0)
            {
                _body.Velocity.X = -_body.Velocity.X;
                _direction.X = -_direction.X;
            }
            if ((collision & Values.BodyCollision.Vertical) != 0)
            {
                _body.Velocity.Y = -_body.Velocity.Y;
                _direction.Y = -_direction.Y;
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_dialogId);
            return true;
        }
    }
}