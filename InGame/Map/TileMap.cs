using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Map
{
    public class TileMap
    {
        public Texture2D SprTileset;
        public Texture2D SprTilesetBlur;

        public string TilesetPath;
        public int[,,] ArrayTileMap;

        public int TileSize;
        public int TileCountX;
        public int TileCountY;

        public bool BlurLayer = false;

        public void SetTileset(Texture2D sprTileset, int tileSize = 16)
        {
            SprTileset = sprTileset;
            SprTilesetBlur = Resources.SprBlurTileset;

            TileSize = tileSize;

            // calculate how many tiles are horizontally and vertically
            TileCountX = SprTileset.Width / TileSize;
            TileCountY = SprTileset.Height / TileSize;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (ArrayTileMap == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, MapManager.Camera.Scale >= 1 ?
                SamplerState.PointWrap : SamplerState.AnisotropicWrap, null, null, null, MapManager.Camera.TransformMatrix);

            for (var i = 0; i < ArrayTileMap.GetLength(2) - (BlurLayer ? 1 : 0); i++)
                DrawTileLayer(spriteBatch, SprTileset, i);

            spriteBatch.End();
        }

        // TODO_End: this could be optimized like in MonoGame.Extended
        public void DrawTileLayer(SpriteBatch spriteBatch, Texture2D tileset, int layer, int padding = 0)
        {
            var halfWidth = Game1.RenderWidth / 2;
            var halfHeight = Game1.RenderHeight / 2;

            var tileSize = Values.TileSize;

            var camera = MapManager.Camera;
            var startX = Math.Max(0, (int)((camera.X - halfWidth) / (camera.Scale * tileSize)) - padding);
            var startY = Math.Max(0, (int)((camera.Y - halfHeight) / (camera.Scale * tileSize)) - padding);
            var endX = Math.Min(ArrayTileMap.GetLength(0), (int)((camera.X + halfWidth) / (camera.Scale * tileSize)) + 1 + padding);
            var endY = Math.Min(ArrayTileMap.GetLength(1), (int)((camera.Y + halfHeight) / (camera.Scale * tileSize)) + 1 + padding);

            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                {
                    if (ArrayTileMap[x, y, layer] >= 0)
                        spriteBatch.Draw(tileset,
                            new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize),
                            new Rectangle((ArrayTileMap[x, y, layer] % (tileset.Width / TileSize)) * TileSize,
                                ArrayTileMap[x, y, layer] / (tileset.Width / TileSize) * TileSize, TileSize, TileSize),
                            Color.White);
                }
        }

        // this should probably be at a different location
        public void DrawBlurLayer(SpriteBatch spriteBatch)
        {
            if (ArrayTileMap == null)
                return;

            //SprTilesetBlur.Dispose();
            //Resources.LoadTexture(out SprTilesetBlur, "D:\\Dev\\ProjectZ\\ProjectZ\\bin\\Data\\Maps\\Tilesets\\blur tileset.png");

            DrawTileLayer(spriteBatch, SprTilesetBlur, ArrayTileMap.GetLength(2) - 1, 1);
        }
    }
}
