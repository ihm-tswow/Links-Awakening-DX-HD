using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameSystems
{
    class EndingSystem : GameSystem
    {
        private float _counter;
        private bool _isActive;

        public override void OnLoad()
        {
            _isActive = false;
        }

        public override void Update()
        {
            if(!_isActive)
                return;

            _counter -= Game1.DeltaTime;

            if (_counter < 0)
            {
                _isActive = false;
                Game1.ScreenManager.ChangeScreen(Values.ScreenEnding);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {

        }

        public void StartEnding()
        {
            _isActive = true;
            _counter = 2000;

            MapManager.ObjLink.MapTransitionStart = new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y);
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.MapTransitionStart;
            //MapManager.ObjLink.DirectionExit = Direction;

            // append a map change
            ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]).AppendMapChange("ending.map", "entry");
        }
    }
}
