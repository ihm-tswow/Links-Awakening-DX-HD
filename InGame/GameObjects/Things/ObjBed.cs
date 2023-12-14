using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjBed : GameObject
    {
        private readonly string _nextMap;
        private readonly string _lampKey;

        private bool _startBed;
        private bool _startTransition;

        private const int TransitionTime = 2750;
        private float _transitionCounter = TransitionTime;
        private int _lightState;

        public ObjBed() : base("editor bed") { }

        public ObjBed(Map.Map map, int posX, int posY, string nextMap, string lampKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 32);

            _nextMap = nextMap;
            _lampKey = lampKey;

            var boxPushable = new CBox(EntityPosition, 0, 0, 16, 32 - MapManager.ObjLink.BodyRectangle.Height, 8);

            AddComponent(PushableComponent.Index, new PushableComponent(boxPushable, OnPush));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            _startBed = true;

            Game1.GameManager.SetMusic(29, 2);

            // jump into the bed
            MapManager.ObjLink.StartRailJump(new Vector2(EntityPosition.X + 8, EntityPosition.Y + 21), 1, 1);

            MapManager.ObjLink.StartBedTransition();

            return false;
        }

        public void Update()
        {
            if (!_startBed || _startTransition)
                return;

            _transitionCounter -= Game1.DeltaTime;

            // turn of the lights
            if (_lightState < 4 && _transitionCounter < TransitionTime - 1000 - _lightState * 250)
            {
                _lightState++;
                Game1.GameManager.SaveManager.SetString(_lampKey + _lightState, "0");
            }

            if (_transitionCounter < 0)
            {
                _startTransition = true;

                MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
                MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;

                var transitionSystem = ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]);
                transitionSystem.AppendMapChange(_nextMap, "bed");
                transitionSystem.StartDreamTransition = true;
            }
        }
    }
}