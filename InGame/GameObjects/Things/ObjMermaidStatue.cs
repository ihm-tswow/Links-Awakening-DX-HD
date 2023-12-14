using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjMermaidStatue : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly CBox _box;

        private Vector2 _startPosition;
        private Vector2 _endPosition;

        private readonly string _strKey;
        private readonly int _moveTime = 650;

        private bool _moved;

        public ObjMermaidStatue() : base("mermaid_statue") { }

        public ObjMermaidStatue(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            int offset = -4;

            EntityPosition = new CPosition(posX + 8, posY + 32 + offset, 0);
            EntitySize = new Rectangle(-8, -32 - offset, 16, 32);

            _startPosition = EntityPosition.Position;
            _endPosition = _startPosition - new Vector2(16, 0);

            _strKey = strKey;

            _body = new BodyComponent(EntityPosition, -8, -16 - offset, 16, 16, 8);

            var stateIdle = new AiState();
            var statePreMoving = new AiState(UpdateFreezePlayer);
            statePreMoving.Trigger.Add(new AiTriggerCountdown(250, null, () => _aiComponent.ChangeState("moving")));
            var stateMoving = new AiState(UpdateFreezePlayer) { Init = InitMoving };
            stateMoving.Trigger.Add(new AiTriggerCountdown(_moveTime, MoveTick, MoveEnd));
            var stateMoved = new AiState();

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("preMoving", statePreMoving);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("moved", stateMoved);
            _aiComponent.ChangeState("idle");

            _box = new CBox(EntityPosition, -8, -16 - offset, 16, 16, 16);

            AddComponent(InteractComponent.Index, new InteractComponent(_box, OnInteract));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_box, Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent("mermaid_statue", EntityPosition, Values.LayerPlayer));

            // already moved?
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
            {
                MoveTick(0);
                _aiComponent.ChangeState("moved");
            }
        }

        private bool OnInteract()
        {
            var itemScale = Game1.GameManager.GetItem("trade12");
            if (itemScale != null && itemScale.Count >= 1)
            {
                Game1.GameManager.RemoveItem("trade12", 1);
                Game1.GameManager.StartDialogPath("mermaid_statue_1");
                Game1.GameManager.PlaySoundEffect("D378-04-04");
                _aiComponent.ChangeState("preMoving");
            }
            else if (!_moved)
            {
                Game1.GameManager.StartDialogPath("mermaid_statue_0");
            }

            return true;
        }

        private void UpdateFreezePlayer()
        {
            MapManager.ObjLink.FreezePlayer();
        }

        private void InitMoving()
        {
            Game1.GameManager.PlaySoundEffect("D378-17-11");
        }

        private void MoveTick(double time)
        {
            // the movement is fast in the beginning and slows down at the end
            var amount = (float)Math.Sin((_moveTime - time) / _moveTime * (Math.PI / 2f));
            var newPosition = Vector2.Lerp(_startPosition, _endPosition, amount);
            EntityPosition.Set(newPosition);
        }

        private void MoveEnd()
        {
            MoveTick(0);
            Game1.GameManager.SaveManager.SetString(_strKey, "1");
            _aiComponent.ChangeState("moved");

            Game1.GameManager.PlaySoundEffect("D360-02-02");
        }
    }
}