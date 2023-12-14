using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.Editor;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;
using System;
using System.Collections.Generic;

namespace ProjectZ.InGame.Map
{
    public class Map
    {
        public TileMap TileMap;
        public TileMap HoleMap;
        public string[,] DigMap;

        public ObjectManager Objects;

        public MapStates.FieldStates[,] StateMap;
        public Vector2? CameraTarget;

        private Point _lastFieldPosition;
        public int[,] UpdateMap;

        public Color LightColor;

        public string MapName;
        public string MapFileName;

        // TODO_Opt: this is currently only set in dungeons
        // this should probably be saved inside each mapfile
        public string LocationName;
        public string LocationFullName;

        public float LightState;

        public int MapWidth => TileMap.ArrayTileMap?.GetLength(0) ?? 0;
        public int MapHeight => TileMap.ArrayTileMap?.GetLength(1) ?? 0;

        public int MapOffsetX;
        public int MapOffsetY;

        public int[] MapMusic = new[] { -1, -1, -1 };

        public float ShadowHeight;
        public float ShadowRotation;

        public bool Is2dMap;
        public bool DungeonMode;
        public bool UseLight;
        public bool UseShadows;
        public bool IsOverworld;

        private List<GameObject> _digList = new List<GameObject>();

        public Map()
        {
            TileMap = new TileMap();
            HoleMap = new TileMap();
            Objects = new ObjectManager(this);
        }

        public static Map CreateEmptyMap()
        {
            var emptyMap = new Map();

            emptyMap.Objects.Clear();
            emptyMap.Objects.LoadObjects();

            emptyMap.StateMap = new MapStates.FieldStates[1, 1];
            emptyMap.UpdateMap = new int[1, 1];

            return emptyMap;
        }

        public void Reset()
        {
            CameraTarget = null;
            Is2dMap = false;
            IsOverworld = false;
            DungeonMode = false;
            LocationName = null;
            UseLight = false;
            MapMusic = new[] { -1, -1, -1 };
            UseShadows = true;
            LightState = 0;

            ShadowHeight = Values.ShadowHeightDefault;
            ShadowRotation = Values.ShadowRotationDefault;
        }

        public void SetFieldState(int posX, int posY, MapStates.FieldStates newState)
        {
            if (0 <= posX && posX < StateMap.GetLength(0) &&
                0 <= posY && posY < StateMap.GetLength(1))
                StateMap[posX, posY] = newState;
        }

        public void AddFieldState(int posX, int posY, MapStates.FieldStates addState)
        {
            if (0 <= posX && posX < StateMap.GetLength(0) &&
                0 <= posY && posY < StateMap.GetLength(1))
                StateMap[posX, posY] |= addState;
        }

        public void RemoveFieldState(int posX, int posY, MapStates.FieldStates removeState)
        {
            if (0 <= posX && posX < StateMap.GetLength(0) &&
                0 <= posY && posY < StateMap.GetLength(1))
                StateMap[posX, posY] &= ~removeState;
        }

        public MapStates.FieldStates GetFieldState(int posX, int posY)
        {
            if (0 <= posX && posX < StateMap.GetLength(0) &&
                0 <= posY && posY < StateMap.GetLength(1))
                return StateMap[posX, posY];

            return MapStates.FieldStates.None;
        }

        public MapStates.FieldStates GetFieldState(Vector2 centerF)
        {
            if (centerF.X < 0 || centerF.Y < 0)
                return MapStates.FieldStates.None;

            var posX = (int)(centerF.X / 16);
            var posY = (int)(centerF.Y / 16);
            return GetFieldState(posX, posY);
        }

        public int GetUpdateState(Vector2 center)
        {
            var posX = (int)(center.X - MapOffsetX * Values.TileSize) / Values.FieldWidth;
            var posY = (int)(center.Y - MapOffsetY * Values.TileSize) / Values.FieldHeight;
            return GetUpdateState(posX, posY);
        }

        public int GetUpdateState(int posX, int posY)
        {
            if (0 <= posX && posX < UpdateMap.GetLength(0) &&
                0 <= posY && posY < UpdateMap.GetLength(1))
                return UpdateMap[posX, posY];

            return 0;
        }

        public void ChangeUpdateState(int posX, int posY, int addition)
        {
            if (0 <= posX && posX < UpdateMap.GetLength(0) &&
                0 <= posY && posY < UpdateMap.GetLength(1))
                UpdateMap[posX, posY] += addition;
        }

        public void UpdateMapUpdateState()
        {
            var cameraPosition = new Vector2(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY - 4);
            var fieldPosition = new Point(
                (int)(cameraPosition.X - MapOffsetX * Values.TileSize) / Values.FieldWidth,
                (int)(cameraPosition.Y - MapOffsetY * Values.TileSize) / Values.FieldHeight);

            // increment the update counter for the fields the player left
            for (var y = _lastFieldPosition.Y - 2; y <= _lastFieldPosition.Y + 2; y++)
                for (var x = _lastFieldPosition.X - 2; x <= _lastFieldPosition.X + 2; x++)
                {
                    if (Math.Abs(x - fieldPosition.X) > 2 ||
                        Math.Abs(y - fieldPosition.Y) > 2)
                    {
                        ChangeUpdateState(x, y, 1);
                        ClearHoleMap(x, y);
                    }
                }

            _lastFieldPosition = fieldPosition;
        }

        private void ClearHoleMap(int posX, int posY)
        {
            if (HoleMap.ArrayTileMap == null)
                return;

            if (posX < 0 || HoleMap.ArrayTileMap.GetLength(0) < (posX + 1) * 10 ||
                posY < 0 || HoleMap.ArrayTileMap.GetLength(1) < (posY + 1) * 8)
                return;

            for (var y = 0; y < 8; y++)
                for (var x = 0; x < 10; x++)
                {
                    HoleMap.ArrayTileMap[posX * 10 + x, posY * 8 + y, 0] = -1;
                }
        }

        public void ResizeMap(int newWidth, int newHeight, int posX, int posY)
        {
            var newTileMap = new TileMap();
            newTileMap.SetTileset(TileMap.SprTileset, TileMap.TileSize);
            newTileMap.TilesetPath = TileMap.TilesetPath;

            var depth = TileMap.ArrayTileMap.GetLength(2);
            newTileMap.ArrayTileMap = new int[newWidth, newHeight, depth];

            for (var z = 0; z < newTileMap.ArrayTileMap.GetLength(2); z++)
                for (var y = 0; y < newTileMap.ArrayTileMap.GetLength(1); y++)
                    for (var x = 0; x < newTileMap.ArrayTileMap.GetLength(0); x++)
                        newTileMap.ArrayTileMap[x, y, z] = -1;

            for (var z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
            {
                for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                {
                    for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                    {
                        if (0 <= posX + x && posX + x < newWidth &&
                            0 <= posY + y && posY + y < newHeight &&
                            0 <= z && z < depth)
                            newTileMap.ArrayTileMap[posX + x, posY + y, z] = TileMap.ArrayTileMap[x, y, z];
                    }
                }
            }

            TileMap = newTileMap;

            // @TODO: this does not set the new parts to ""
            OffsetDigMap(newWidth, newHeight, posX, posY);

            // offset the objects
            ObjectEditorScreen.OffsetObjects(this, posX * Values.TileSize, posY * Values.TileSize);
        }

        private void OffsetDigMap(int newWidth, int newHeight, int posX, int posY)
        {
            var newDigMap = new string[newWidth, newHeight];

            for (var y = 0; y < DigMap.GetLength(1); y++)
                for (var x = 0; x < DigMap.GetLength(0); x++)
                {
                    if (0 <= posX + x && posX + x < newWidth &&
                        0 <= posY + y && posY + y < newHeight)
                        newDigMap[posX + x, posY + y] = DigMap[x, y];
                }

            DigMap = newDigMap;
        }

        public Vector2 GetRoomCenter(float x, float y)
        {
            return new Vector2(
                ((int)((x - MapOffsetX * Values.TileSize) / Values.FieldWidth) + 0.5f) * Values.FieldWidth + MapOffsetX * Values.TileSize,
                ((int)((y - MapOffsetY * Values.TileSize) / Values.FieldHeight) + 0.5f) * Values.FieldHeight + MapOffsetY * Values.TileSize);
        }

        public Rectangle GetField(int x, int y, int margin)
        {
            return new Rectangle(
                (x - MapOffsetX * Values.TileSize) / Values.FieldWidth * Values.FieldWidth + margin + MapOffsetX * Values.TileSize,
                (y - MapOffsetY * Values.TileSize) / Values.FieldHeight * Values.FieldHeight + margin + MapOffsetY * Values.TileSize,
                Values.FieldWidth - 2 * margin, Values.FieldHeight - 2 * margin);
        }

        public Rectangle GetField(int x, int y)
        {
            return GetField(x, y, 0);
        }

        public Box GetFieldBox(int x, int y, int height, int margin)
        {
            return new Box(
                (x - MapOffsetX * Values.TileSize) / Values.FieldWidth * Values.FieldWidth + margin + MapOffsetX * Values.TileSize,
                (y - MapOffsetY * Values.TileSize) / Values.FieldHeight * Values.FieldHeight + margin + MapOffsetY * Values.TileSize, 0,
                Values.FieldWidth - 2 * margin, Values.FieldHeight - 2 * margin, height);
        }

        public Box GetFieldBox(int x, int y, int height)
        {
            return GetFieldBox(x, y, height, 0);
        }

        public bool CanDig(Point position)
        {
            // no grass or water? can dig? was already dug?
            if (0 > position.X || position.X >= HoleMap.ArrayTileMap.GetLength(0) ||
                0 > position.Y || position.Y >= HoleMap.ArrayTileMap.GetLength(1) ||
                (StateMap[position.X, position.Y] | MapStates.FieldStates.UpperLevel) != MapStates.FieldStates.UpperLevel ||
                HoleMap.ArrayTileMap[position.X, position.Y, 0] >= 0 ||
                position.X < DigMap.GetLength(0) &&
                position.Y < DigMap.GetLength(1) &&
                string.IsNullOrEmpty(DigMap[position.X, position.Y]))
            {
                return false;
            }
            else
            {
                // check if there is something blocking the digging
                _digList.Clear();
                var digBox = new Box(position.X * 16 + 2, position.Y * 16 + 2, 0, 12, 12, 8);
                Objects.GetComponentList(_digList, (int)digBox.X, (int)digBox.Y, 12, 12, CollisionComponent.Mask);
                var collidingBox = Box.Empty;
                foreach (var gameObject in _digList)
                {
                    var collisionObject = gameObject.Components[CollisionComponent.Index] as CollisionComponent;
                    if (collisionObject.Owner.IsActive &&
                        (collisionObject.CollisionType & (Values.CollisionTypes.Normal | Values.CollisionTypes.Hole)) != 0 &&
                        collisionObject.Collision(digBox, 0, 0, ref collidingBox))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Dig(Point position, Vector2 diggerPosition, int dir)
        {
            string strObject = null;
            string strSaveKey = null;

            var digTileIndex = 0;

            var itemString = DigMap[position.X, position.Y];
            if (int.TryParse(itemString, out int result))
            {
                var random = Game1.RandomNumber.Next(0, 100);
                if (random < 6)
                    strObject = "ruby";
                else if (random < 9)
                    strObject = "heart";

                digTileIndex = result - 1;
            }
            else if (!string.IsNullOrEmpty(itemString))
            {
                var split = itemString.Split(':');
                strObject = split[0];
                if (split.Length >= 2)
                    strSaveKey = split[1];
                if (split.Length >= 3)
                {
                    if (int.TryParse(split[2], out int tileIndex))
                        digTileIndex = tileIndex - 1;
                }
            }

            // spawn a heart or a ruby
            if (strObject != null)
            {
                // calculate the item hop direction
                var itemPosition = new Vector2(position.X * Values.TileSize + 8, position.Y * Values.TileSize + 12);
                var direction = itemPosition - diggerPosition;
                if (direction != Vector2.Zero)
                    direction.Normalize();
                direction = direction * 0.75f + AnimationHelper.DirectionOffset[dir] * 0.75f;

                var objItem = new ObjItem(this, 0, 0, "j", strSaveKey, strObject, null, true);
                if (!objItem.IsDead)
                {
                    objItem.EntityPosition.Set(itemPosition);
                    objItem.SetVelocity(new Vector3(direction.X, direction.Y, 1.25f));
                    Objects.SpawnObject(objItem);
                }
            }

            Game1.GameManager.StartDialogPath("dig_dialog");

            HoleMap.ArrayTileMap[position.X, position.Y, 0] = digTileIndex;
        }
    }
}
