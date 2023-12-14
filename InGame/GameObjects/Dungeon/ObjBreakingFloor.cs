using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    // TODO: this needs some kind of effect while standing on
    internal class ObjBreakingFloor : GameObject
    {
        private readonly DrawSpriteComponent _drawComponent;
        private readonly ObjHole _objHole;

        private readonly Box _collisionBox;

        private const int BreakTime = 750;
        private float _breakCounter;

        private const int RespawnTime = 15000;
        private float _respawnCounter;

        private bool _isActive;

        public ObjBreakingFloor(Map.Map map, int posX, int posY, string spriteId) : base(map, spriteId)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            var margin = 4;
            _collisionBox = new Box(posX, posY + margin, 0, 16, 16 - margin * 2, 1);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new DrawSpriteComponent(spriteId, EntityPosition, Vector2.Zero, Values.LayerBottom));

            _objHole = new ObjHole(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 16, 14, Rectangle.Empty, 0, 1, 0);
            _objHole.IsActive = false;
            Map.Objects.SpawnObject(_objHole);
        }

        private void Update()
        {
            if (!_isActive)
            {
                // respawn the floor after some time
                _respawnCounter -= Game1.DeltaTime;
                if (_respawnCounter <= 0)
                    Activate();

                return;
            }

            // is the player standing on the floor tile?
            if (MapManager.ObjLink._body.BodyBox.Box.Intersects(_collisionBox))
            {
                if (MapManager.ObjLink.CurrentState != ObjLink.State.Idle &&
                    MapManager.ObjLink.CurrentState != ObjLink.State.Stunned)
                    return;

                _breakCounter += Game1.DeltaTime;

                // reset the time while hitting; in dungeon 8 there is a lot of breaking floor with enemies ontop that would be otherwise really hard to clear
                if (MapManager.ObjLink.CurrentState == ObjLink.State.Attacking)
                    _breakCounter = 0;

                // spawn the hole and delete itself
                if (_breakCounter >= BreakTime)
                {
                    Game1.GameManager.PlaySoundEffect("D378-43-2B");
                    Deactivate();
                }
            }
            else
            {
                // reset the time
                _breakCounter -= Game1.DeltaTime * 1.5f;
                if (_breakCounter < 0)
                    _breakCounter = 0;
            }
        }

        private void Activate()
        {
            _breakCounter = 0;

            _isActive = true;
            _drawComponent.IsActive = true;

            // activate the hole
            _objHole.IsActive = false;
        }

        private void Deactivate()
        {
            _respawnCounter = RespawnTime;

            _isActive = false;
            _drawComponent.IsActive = false;

            // activate the hole
            _objHole.IsActive = true;
        }
    }
}