using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjScriptOnTouch : GameObject
    {
        private readonly Box _collisionBox;
        private readonly string _scriptName;

        private bool _wasColliding;

        public override bool IsActive
        {
            get => base.IsActive;
            set
            {
                base.IsActive = value;
                if (!value)
                    _wasColliding = false;
            }
        }

        public ObjScriptOnTouch() : base("editor script on touch") { }

        public ObjScriptOnTouch(Map.Map map, int posX, int posY, int width, int height, string scriptName) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _collisionBox = new Box(posX, posY, 0, width, height, 32);
            _scriptName = scriptName;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        public void Update()
        {
            var colliding = MapManager.ObjLink._body.BodyBox.Box.Intersects(_collisionBox);

            // player just started colliding?
            if (!_wasColliding && colliding)
                Game1.GameManager.StartDialogPath(_scriptName);

            _wasColliding = colliding;
        }
    }
}