using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjWeatherBird : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly Point[] _moveOffset = { new Point(-1, 0), new Point(0, -1), new Point(1, 0), new Point(0, 1) };

        private readonly string _saveKey;
        private readonly int _allowedDirection = 1;

        private Vector2 _startPosition;
        private Vector2 _aimPosition;

        private const int PushTime = 250;
        private const int MoveTime = 500;

        private float _pushCounter;
        private bool _wasPushed;

        public ObjWeatherBird() : base("weather_bird") { }

        public ObjWeatherBird(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 1, posY + 30, 0);
            EntitySize = new Rectangle(0, -30, 16, 32);

            _saveKey = saveKey;
            
            var movingTrigger = new AiTriggerCountdown(MoveTime, MoveTick, MoveEnd);
            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", new AiState(UpdateIdle));
            _aiComponent.States.Add("moving", new AiState { Init = InitMoving, Trigger = { movingTrigger } });
            _aiComponent.States.Add("moved", new AiState());

            if (_saveKey != null && Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 16));
                _aiComponent.ChangeState("moved");
            }
            else
                _aiComponent.ChangeState("idle");

            var body = new BodyComponent(EntityPosition, 0, -10, 14, 12, 8)
            {
                FieldRectangle = map.GetField(posX, posY)
            };

            var animator = AnimatorSaveLoad.LoadAnimator("Objects/weatherBird");
            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(0, -30));
            animator.Play("idle");

            AddComponent(InteractComponent.Index, new InteractComponent(body.BodyBox, Interact));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(body.BodyBox, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer));
            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("weatherBird");
            return true;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_aiComponent.CurrentStateId != "idle")
                return false;

            // move the stone
            if (type != PushableComponent.PushType.Continues)
                return false;

            var stoneLifter = Game1.GameManager.GetItem("stonelifter2");
            if (stoneLifter == null)
                return false;

            var pushDirection = AnimationHelper.GetDirection(direction);

            if (_allowedDirection != -1 && _allowedDirection != pushDirection)
                return false;

            _wasPushed = true;
            _pushCounter += Game1.DeltaTime;

            // start moving
            if (_pushCounter < PushTime)
                return false;

            _startPosition = EntityPosition.Position;

            _aimPosition = new Vector2(
                _startPosition.X + _moveOffset[pushDirection].X * 16,
                _startPosition.Y + _moveOffset[pushDirection].Y * 16);

            _aiComponent.ChangeState("moving");

            return true;
        }

        private void UpdateIdle()
        {
            // reset the moveCounter used for measuring how long the player is already pushing
            // this is used so the stone does not move instantly after the first collision with the player
            if (!_wasPushed)
                _pushCounter = 0;

            _wasPushed = false;
        }

        private void InitMoving()
        {
            Game1.GameManager.PlaySoundEffect("D378-17-11");

            // set the key
            if (_saveKey != null)
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");
        }

        private void MoveTick(double time)
        {
            // freeze the player 
            MapManager.ObjLink.FreezePlayer();

            // the movement is fast in the beginning and slows down at the end
            var amount = (float)Math.Sin((MoveTime - time) / MoveTime * (Math.PI / 2f));
            EntityPosition.Set(Vector2.Lerp(_startPosition, _aimPosition, amount));
        }

        private void MoveEnd()
        {
            Game1.GameManager.PlaySoundEffect("D360-35-23");

            // finished moving
            EntityPosition.Set(_aimPosition);

            _aiComponent.ChangeState("moved");
        }
    }
}