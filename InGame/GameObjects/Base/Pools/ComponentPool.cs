using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base.Pools
{
    public class ComponentPool
    {
        public class ObjectTile
        {
            public List<GameObject> GameObjects = new List<GameObject>();
        }

        public Map.Map Map;

        public List<GameObject> NoneTiledObjects = new List<GameObject>();
        public ObjectTile[,] ComponentTiles;

        public readonly int TileWidth;
        public readonly int TileHeight;

        public ComponentPool(Map.Map map, int width, int height, int tileWidth, int tileHeight)
        {
            Map = map;

            if (width <= 0)
                width = 1;
            if (height <= 0)
                height = 1;

            var tWidth = width * 16;
            var tHeight = height * 16;

            TileWidth = tileWidth;
            TileHeight = tileHeight;

            ComponentTiles = new ObjectTile[
                (int)Math.Ceiling(tWidth / (float)TileWidth),
                (int)Math.Ceiling(tHeight / (float)TileHeight)];

            for (var y = 0; y < ComponentTiles.GetLength(1); y++)
                for (var x = 0; x < ComponentTiles.GetLength(0); x++)
                    ComponentTiles[x, y] = new ObjectTile();
        }

        public void AddEntity(GameObject gameObject)
        {
            if (gameObject.EntityPosition == null)
            {
                NoneTiledObjects.Add(gameObject);
                return;
            }

            var left = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X) / TileWidth);
            var right = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width) / TileWidth);

            var top = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y) / TileHeight);
            var bottom = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height) / TileHeight);

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    ComponentTiles[x, y].GameObjects.Add(gameObject);

            // the pool position is needed to not add duplicates to the component list
            gameObject.EntityPoolPosition = new Point(left, top);

            gameObject.EntityPosition.LastPosition = gameObject.EntityPosition.Position;

            // update tile placement after the entity changes its position
            gameObject.EntityPosition.AddPositionListener(typeof(ComponentPool), position => UpdatePartition(gameObject));
        }

        public void RemoveEntity(GameObject gameObject)
        {
            if (gameObject.EntityPosition == null)
            {
                NoneTiledObjects.Remove(gameObject);
                return;
            }

            gameObject.EntityPosition.PositionChangedDict.Remove(typeof(ComponentPool));

            var left = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X) / TileWidth);
            var right = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width) / TileWidth);

            var top = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y) / TileHeight);
            var bottom = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height) / TileHeight);

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    ComponentTiles[x, y].GameObjects.Remove(gameObject);
        }

        private void UpdatePartition(GameObject gameObject)
        {
            var left = (int)((gameObject.EntityPosition.LastPosition.X + gameObject.EntitySize.X) / TileWidth);
            var right = (int)((gameObject.EntityPosition.LastPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width) / TileWidth);

            var top = (int)((gameObject.EntityPosition.LastPosition.Y + gameObject.EntitySize.Y) / TileHeight);
            var bottom = (int)((gameObject.EntityPosition.LastPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height) / TileHeight);

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            var leftNew = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X) / TileWidth);
            var rightNew = (int)((gameObject.EntityPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width) / TileWidth);

            var topNew = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y) / TileHeight);
            var bottomNew = (int)((gameObject.EntityPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height) / TileHeight);

            leftNew = MathHelper.Clamp(leftNew, 0, ComponentTiles.GetLength(0) - 1);
            rightNew = MathHelper.Clamp(rightNew, 0, ComponentTiles.GetLength(0) - 1);
            topNew = MathHelper.Clamp(topNew, 0, ComponentTiles.GetLength(1) - 1);
            bottomNew = MathHelper.Clamp(bottomNew, 0, ComponentTiles.GetLength(1) - 1);

            // remove the entity at the old position
            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    if (!(leftNew <= x && x <= rightNew &&
                       topNew <= y && y <= bottomNew))
                        ComponentTiles[x, y].GameObjects.Remove(gameObject);

            // add the entity at the new position
            for (var y = topNew; y <= bottomNew; y++)
                for (var x = leftNew; x <= rightNew; x++)
                    if (!(left <= x && x <= right &&
                          top <= y && y <= bottom))
                        ComponentTiles[x, y].GameObjects.Add(gameObject);

            // the pool position is needed to not add duplicates to the component list
            gameObject.EntityPoolPosition = new Point(leftNew, topNew);
        }

        public void GetObjectList(List<GameObject> gameObjectList, int recLeft, int recTop, int recWidth, int recHeight)
        {
            var left = recLeft / TileWidth;
            var right = (recLeft + recWidth) / TileWidth;
            var top = recTop / TileHeight;
            var bottom = (recTop + recHeight) / TileHeight;

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    foreach (var gameObject in ComponentTiles[x, y].GameObjects)
                    {
                        // check to not add objects more than once
                        if (gameObject.EntityPoolPosition.X == x && gameObject.EntityPoolPosition.Y == y ||
                            x == left && y == top ||
                            gameObject.EntityPoolPosition.X == x && y == top ||
                            x == left && gameObject.EntityPoolPosition.Y == y)
                            gameObjectList.Add(gameObject);
                    }

            foreach (var gameObject in NoneTiledObjects)
                gameObjectList.Add(gameObject);
        }

        public GameObject GetObjectOfType(int recLeft, int recTop, int recWidth, int recHeight, Type type)
        {
            var left = recLeft / TileWidth;
            var right = (recLeft + recWidth) / TileWidth;
            var top = recTop / TileHeight;
            var bottom = (recTop + recHeight) / TileHeight;

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    foreach (var gameObject in ComponentTiles[x, y].GameObjects)
                    {
                        // check to not add objects more than once
                        if ((gameObject.EntityPoolPosition.X == x && gameObject.EntityPoolPosition.Y == y ||
                            x == left && y == top ||
                            gameObject.EntityPoolPosition.X == x && y == top ||
                            x == left && gameObject.EntityPoolPosition.Y == y) &&
                            gameObject.GetType() == type)
                            return gameObject;
                    }

            foreach (var gameObject in NoneTiledObjects)
                if (gameObject.GetType() == type)
                    return gameObject;

            return null;
        }

        public void GetComponentList(List<GameObject> gameObjectList, int recLeft, int recTop, int recWidth, int recHeight, int componentMask)
        {
            var left = recLeft / TileWidth;
            var right = (recLeft + recWidth) / TileWidth;
            var top = recTop / TileHeight;
            var bottom = (recTop + recHeight) / TileHeight;

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    foreach (var gameObject in ComponentTiles[x, y].GameObjects)
                    {
                        if ((gameObject.ComponentsMask & componentMask) != 0 &&
                            (gameObject.EntityPoolPosition.X == x && gameObject.EntityPoolPosition.Y == y ||
                             x == left && y == top ||
                             gameObject.EntityPoolPosition.X == x && y == top ||
                             x == left && gameObject.EntityPoolPosition.Y == y))
                            gameObjectList.Add(gameObject);
                    }

            foreach (var gameObject in NoneTiledObjects)
                if ((gameObject.ComponentsMask & componentMask) != 0)
                    gameObjectList.Add(gameObject);
        }
    }
}