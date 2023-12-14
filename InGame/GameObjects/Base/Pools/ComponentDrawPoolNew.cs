using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Base.Pools
{
    class ComponentDrawPoolNew
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

        private List<DrawComponent> _currentObjects = new List<DrawComponent>();

        public ComponentDrawPoolNew(int width, int height, int tileWidth, int tileHeight)
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
                    ComponentTiles[x, y].DrawComponents.Add(gameObject.Components[DrawComponent.Index] as DrawComponent);

            // update tile placement after the entity changes its position
            gameObject.EntityPosition.AddPositionListener(typeof(ComponentPool), position => UpdatePartition(gameObject));
        }

        public void RemoveEntity(GameObject gameObject)
        {
            if (gameObject.EntityPosition == null)
            {
                NoneTiledObjects.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);
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
                    ComponentTiles[x, y].DrawComponents.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);
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
                        ComponentTiles[x, y].DrawComponents.Remove(gameObject.Components[DrawComponent.Index] as DrawComponent);

            // add the entity at the new position
            for (var y = topNew; y <= bottomNew; y++)
                for (var x = leftNew; x <= rightNew; x++)
                    if (!(left <= x && x <= right &&
                          top <= y && y <= bottom))
                        ComponentTiles[x, y].DrawComponents.Add(gameObject.Components[DrawComponent.Index] as DrawComponent);
        }

        public void DrawPool(SpriteBatch spriteBatch, int posX, int posY, int width, int height, int startLayer, int endLayer)
        {
            var left = posX / TileWidth;
            var right = (posX + width) / TileWidth;
            var top = posY / TileHeight;
            var bottom = (posY + height) / TileHeight;

            left = MathHelper.Clamp(left, 0, ComponentTiles.GetLength(0) - 1);
            right = MathHelper.Clamp(right, 0, ComponentTiles.GetLength(0) - 1);
            top = MathHelper.Clamp(top, 0, ComponentTiles.GetLength(1) - 1);
            bottom = MathHelper.Clamp(bottom, 0, ComponentTiles.GetLength(1) - 1);

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
                    DrawTileRow(spriteBatch, z, y, left, right, top, bottom);

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

        private void DrawTileRow(SpriteBatch spriteBatch, int layer, int y, int left, int right, int top, int bottom)
        {
            var currentX = left;
            var finishedX = left;

            for (var x = left; x <= right; x++)
            {
                // finished drawing the tile?
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
                // skip tile if it is already drawn
                if (ComponentTiles[currentX, y].DrawPosition == float.MaxValue)
                {
                    currentX++;
                    if (currentX > right)
                        currentX = left;

                    continue;
                }

                while (ComponentTiles[currentX, y].DrawPosition < float.MaxValue)
                {
                    // TODO_Opt: this does only support components that are not split over more than one tile to the left or right of the Position.X tile
                    // tile on the left needs to be drawn first?
                    if (left <= currentX - 1 && ComponentTiles[currentX - 1, y].DrawPosition < ComponentTiles[currentX, y].DrawPosition)
                    {
                        currentX--;
                        break;
                    }
                    // does the tile on the right need to be drawn first?
                    if (currentX + 1 <= right && ComponentTiles[currentX, y].DrawPosition > ComponentTiles[currentX + 1, y].DrawPosition)
                    {
                        currentX++;
                        break;
                    }

                    // draw the object
                    var drawComponent = ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex];
                    var posX = (int)(drawComponent.Position.X / TileWidth);
                    var posY = (int)(drawComponent.Position.Y / TileHeight);
                    // make sure to only draw the component only one time
                    if (drawComponent.Owner.IsActive &&
                        (posX == currentX || posX < left && currentX == left || right < posX && currentX == right) &&
                          (posY == y || posY < top && y == top || bottom < posY && y == bottom))
                    {
                        drawComponent.Draw(spriteBatch);
                    }

                    ComponentTiles[currentX, y].DrawIndex++;

                    // all objects in the tile are drawn?
                    if (ComponentTiles[currentX, y].DrawIndex == ComponentTiles[currentX, y].DrawComponents.Count ||
                        ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Layer > layer)
                    {
                        finishedX++;
                        ComponentTiles[currentX, y].DrawPosition = float.MaxValue;
                        continue;
                    }

                    // update the draw position of the tile
                    ComponentTiles[currentX, y].DrawPosition =
                        ComponentTiles[currentX, y].DrawComponents[ComponentTiles[currentX, y].DrawIndex].Position.Y;
                }
            }
        }
    }
}
