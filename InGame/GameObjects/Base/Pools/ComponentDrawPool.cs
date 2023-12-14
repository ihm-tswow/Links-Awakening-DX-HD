using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Base.Pools
{
    class ComponentDrawPool
    {
        // this sorting stuff is probably unnecessary if most objects are not using transparency between 0-1
        public class DrawTile
        {
            public List<DrawComponent> DrawComponents = new List<DrawComponent>();
            public float DrawPosition;
            public int DrawIndex;
        }

        public List<DrawComponent> NoneTiledObjects = new List<DrawComponent>();
        public DrawTile[,] ComponentTiles;

        public readonly int TileWidth;
        public readonly int TileHeight;

        private int _noneTiledIndex;

        public ComponentDrawPool(int width, int height, int tileWidth, int tileHeight)
        {
            if (width <= 0)
                width = 1;
            if (height <= 0)
                height = 1;

            var tWidth = width * 16;
            var tHeight = height * 16;

            TileWidth = tileWidth;
            TileHeight = tileHeight;

            ComponentTiles = new DrawTile[
                (int)Math.Ceiling(tWidth / (float)TileWidth),
                (int)Math.Ceiling(tHeight / (float)TileHeight)];

            for (var y = 0; y < ComponentTiles.GetLength(1); y++)
                for (var x = 0; x < ComponentTiles.GetLength(0); x++)
                    ComponentTiles[x, y] = new DrawTile();
        }

        public void AddEntity(GameObject gameObject)
        {
            if (gameObject.EntityPosition == null)
            {
                NoneTiledObjects.Add(gameObject.Components[DrawComponent.Index] as DrawComponent);
                return;
            }

            var left = (int)(gameObject.EntityPosition.X / TileWidth);
            var bottom = (int)(gameObject.EntityPosition.Y / TileHeight);

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            ComponentTiles[left, bottom].DrawComponents.Add(gameObject.Components[DrawComponent.Index] as DrawComponent);

            gameObject.EntityPosition.LastPosition = gameObject.EntityPosition.Position;

            // update tile placement after position the entity changes its position
            gameObject.EntityPosition.AddPositionListener(typeof(ComponentDrawPool), newPosition => { UpdatePartition(gameObject); });
        }

        public virtual void RemoveEntity(GameObject gameObject)
        {
            if (gameObject.EntityPosition == null)
            {
                NoneTiledObjects.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);
                return;
            }

            gameObject.EntityPosition.PositionChangedDict.Remove(typeof(ComponentDrawPool));

            var left = (int)(gameObject.EntityPosition.X / TileWidth);
            var bottom = (int)(gameObject.EntityPosition.Y / TileHeight);

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

            ComponentTiles[left, bottom].DrawComponents.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);
        }

        private void UpdatePartition(GameObject gameObject)
        {
            var leftPre = (int)(gameObject.EntityPosition.LastPosition.X / TileWidth);
            var bottomPre = (int)(gameObject.EntityPosition.LastPosition.Y / TileHeight);

            leftPre = MathHelper.Clamp(leftPre, 0, ComponentTiles.GetLength(0) - 1);
            bottomPre = MathHelper.Clamp(bottomPre, 0, ComponentTiles.GetLength(1) - 1);

            var leftNow = (int)(gameObject.EntityPosition.Position.X / TileWidth);
            var bottomNow = (int)(gameObject.EntityPosition.Position.Y / TileHeight);

            leftNow = MathHelper.Clamp(leftNow, 0, ComponentTiles.GetLength(0) - 1);
            bottomNow = MathHelper.Clamp(bottomNow, 0, ComponentTiles.GetLength(1) - 1);

            // moved to another tile?
            if (leftNow == leftPre && bottomNow == bottomPre)
                return;

            // remove from the old tile 
            ComponentTiles[leftPre, bottomPre].DrawComponents.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);
            // add to the new tile
            ComponentTiles[leftNow, bottomNow].DrawComponents.Add(gameObject.Components[DrawComponent.Index] as DrawComponent);
        }

        public void DrawPool(SpriteBatch spriteBatch, int posX, int posY, int width, int height, int startLayer, int endLayer)
        {
            var left = (posX - TileWidth) / TileWidth;
            var right = (posX + width) / TileWidth;
            var top = (posY - TileHeight) / TileHeight;
            var bottom = (posY + height) / TileHeight;

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom + 1, 0, ComponentTiles.GetLength(1) - 1);

            // sort the tiles
            if (startLayer == 0)
            {
                NoneTiledObjects.Sort();
                _noneTiledIndex = 0;

                for (var y = top; y <= bottom; y++)
                    for (var x = left; x <= right; x++)
                    {
                        ComponentTiles[x, y].DrawIndex = 0;
                        ComponentTiles[x, y].DrawComponents.Sort();
                    }
            }

            // draw from start to end layer
            for (var z = startLayer; z < endLayer; z++)
            {
                for (var y = top; y <= bottom; y++)
                    DrawTileRow(spriteBatch, z, y, left, right);

                for (; _noneTiledIndex < NoneTiledObjects.Count; _noneTiledIndex++)
                {
                    var gameObject = NoneTiledObjects[_noneTiledIndex];
                    if (!gameObject.IsActive)
                        continue;

                    if (gameObject.Layer > z)
                        break;

                    gameObject.Draw(spriteBatch);
                }
            }
        }

        void DrawTileRow(SpriteBatch spriteBatch, int layer, int y, int left, int right)
        {
            var currentX = left;
            var finishedX = left;

            for (var x = left; x <= right; x++)
            {
                if (ComponentTiles[x, y].DrawIndex == ComponentTiles[x, y].DrawComponents.Count ||
                    ComponentTiles[x, y].DrawComponents[ComponentTiles[x, y].DrawIndex].Layer > layer)
                {
                    ComponentTiles[x, y].DrawPosition = float.MaxValue;
                    finishedX++;
                }
                else
                    ComponentTiles[x, y].DrawPosition =
                        ComponentTiles[x, y].DrawComponents[ComponentTiles[x, y].DrawIndex].Position.Y;
            }

            while (finishedX <= right)
            {
                if (ComponentTiles[currentX, y].DrawPosition == float.MaxValue)
                {
                    currentX++;
                    if (currentX > right)
                        currentX = left;

                    continue;
                }

                while (ComponentTiles[currentX, y].DrawPosition < float.MaxValue)
                {
                    // tile on the left needs to be drawn first?
                    if (currentX - 1 >= left && ComponentTiles[currentX, y].DrawPosition > ComponentTiles[currentX - 1, y].DrawPosition)
                    {
                        currentX--;
                        break;
                    }

                    // does the tile to the right need to draw first?
                    if (currentX + 1 <= right && ComponentTiles[currentX, y].DrawPosition > ComponentTiles[currentX + 1, y].DrawPosition)
                    {
                        currentX++;
                        break;
                    }

                    // draw the object
                    if (ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Owner.IsActive)
                        ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Draw(spriteBatch);
                    ComponentTiles[currentX, y].DrawIndex++;

                    // all objects in the tile are drawn?
                    if (ComponentTiles[currentX, y].DrawIndex == ComponentTiles[currentX, y].DrawComponents.Count ||
                        ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Layer > layer)
                    {
                        finishedX++;
                        ComponentTiles[currentX, y].DrawPosition = float.MaxValue;
                        continue;
                    }

                    // update the draw position of the pool
                    ComponentTiles[currentX, y].DrawPosition =
                        ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Position.Y;
                }
            }
        }
    }
}
