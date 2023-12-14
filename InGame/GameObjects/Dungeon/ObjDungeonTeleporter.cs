using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    public class ObjDungeonTeleporter : GameObject
    {
        public Vector2 TeleportPosition;

        private readonly Animator _centerAnimator;
        private readonly AnimationComponent _animationComponent;
        private readonly CSprite _sprite;
        private readonly Rectangle _pointRectangle;
        private readonly Vector2[] _pointPositions = new Vector2[4];
        private readonly Point _origin;

        private readonly string _teleportMap;
        private readonly string _teleporterId;

        private float _rotateCount;
        private bool _lockTeleporter;
        private bool _isColliding;

        public ObjDungeonTeleporter() : base("teleporter_middle") { }

        public ObjDungeonTeleporter(Map.Map map, int posX, int posY, string teleportMap, string teleporterId) : base(map)
        {
            _pointRectangle = Resources.SourceRectangle("teleporter_outer");
            var sourceRectangle = Resources.SourceRectangle("teleporter_middle");

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            TeleportPosition = new Vector2(EntityPosition.X, EntityPosition.Y + MapManager.ObjLink.CollisionBoxSize.Y / 2f);

            _teleportMap = teleportMap;
            _teleporterId = teleporterId;

            _origin = new Point(posX + 8, posY + 8);

            _centerAnimator = AnimatorSaveLoad.LoadAnimator("Objects/dTeleporter");
            _centerAnimator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_centerAnimator, _sprite, Vector2.Zero);

            // has the player just teleported to this teleporter?
            if (teleporterId != null && MapManager.ObjLink.NextMapPositionId == teleporterId)
            {
                PlacePlayer();
                Lock();
            }

            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight) { Layer = Values.LightLayer1 });

            UpdatePositions();
        }

        public void Lock()
        {
            _isColliding = true;
            _lockTeleporter = true;
        }

        private void Update()
        {
            // is the player close enough?
            var distance = TeleportPosition - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() < 2.5f)
                OnCollision();

            _rotateCount -= Game1.DeltaTime;
            UpdatePositions();

            if (!_isColliding)
                _lockTeleporter = false;

            _isColliding = false;
        }

        private void OnCollision()
        {
            _isColliding = true;

            if (_lockTeleporter || MapManager.ObjLink.EntityPosition.Z > 1)
                return;

            Game1.GameManager.PlaySoundEffect("D360-28-1C");

            // teleport into a new map?
            if (!string.IsNullOrEmpty(_teleportMap) && Map.MapName != _teleportMap)
            {
                MapManager.ObjLink.SetPosition(TeleportPosition);
                MapManager.ObjLink.StartTeleportation(_teleportMap, _teleporterId);

                _lockTeleporter = true;
                return;
            }

            var teleporterList = Map.Objects.GetObjectsOfType(typeof(ObjDungeonTeleporter));

            foreach (var entity in teleporterList)
            {
                var teleporter = ((ObjDungeonTeleporter)entity);
                if (teleporter != this && teleporter._teleporterId == _teleporterId)
                {
                    MapManager.ObjLink.SetPosition(TeleportPosition);
                    MapManager.ObjLink.StartTeleportation(teleporter);

                    _lockTeleporter = true;
                    break;
                }
            }
        }

        private void UpdatePositions()
        {
            var radiants = _rotateCount / 150f;

            // rotate around the field
            for (var i = 0; i < 4; i++)
            {
                _pointPositions[i] = new Vector2(
                    _origin.X + (float)(7 * Math.Sin(radiants + Math.PI / 2 * i) - 2),
                    _origin.Y + (float)(7 * Math.Cos(radiants + Math.PI / 2 * i)) - 2);
            }
        }

        private void PlacePlayer()
        {
            //MapManager.ObjLink.SaveMap = Map.MapName;
            //MapManager.ObjLink.SavePosition = TeleportPosition;
            //MapManager.ObjLink.SaveDirection = 3;

            MapManager.ObjLink.NextMapPositionStart = TeleportPosition;
            MapManager.ObjLink.NextMapPositionEnd = TeleportPosition;
            MapManager.ObjLink.TransitionInWalking = false;
            MapManager.ObjLink.DirectionEntry = 3;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // draw the circles around
            for (var i = 0; i < _pointPositions.Length; i++)
                spriteBatch.Draw(Resources.SprObjects, _pointPositions[i], _pointRectangle, Color.White);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 32, (int)EntityPosition.Y - 32, 64, 64), new Color(255, 175, 175) * 1.00f);
        }
    }
}