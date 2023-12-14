using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    class InventoryOverlay
    {
        private const int Margin = 6;

        private RenderTarget2D _renderTarget;

        private readonly Rectangle _background0 = new Rectangle(0, 0, 268, 24);
        private readonly Rectangle _background1 = new Rectangle(0, 26, 268, 182);

        private readonly Rectangle _keyRectangle = new Rectangle(94, 6, 20, 114);
        private readonly Rectangle[] _keyPositions = new Rectangle[5];

        private readonly Rectangle _relictsRectangle = new Rectangle(118, 6, 144, 22);
        private readonly Point _relictPosition = new Point(123, 9);
        private readonly Rectangle[] _relicOffsets = new Rectangle[8];

        private readonly DictAtlasEntry[] _ocarinaFaces = new DictAtlasEntry[3];

        private readonly Point _heartsPosition = new Point(6, 5);
        private readonly Point _rubeePosition;

        private readonly Rectangle _flipperRectangle;
        private readonly Rectangle _potionRectangle;

        private readonly Rectangle _tradeStuffRectangle = new Rectangle(6, 6, 84, 22);
        private Rectangle _tradeRectangle;
        private Rectangle _shellRectangle;
        private Rectangle _leafRectangle;

        private readonly Point _skirtPosition;
        private readonly Point _heartPiecePosition;

        private readonly Point _itemSlotsPosition = new Point(14, 41);

        private readonly Rectangle _skirtRectangle = new Rectangle(180, 31, 16, 15);
        private readonly Rectangle _skirtColorRectangle = new Rectangle(198, 37, 14, 10);
        private readonly Rectangle _heartPiecesRectangle = new Rectangle(4, 72, 16, 14);

        private const int ItemSlotWidth = 4;

        public static Rectangle RecItemselection = new Rectangle(0, 0, 30, 20);

        public const int DistX = 8;
        public const int DistY = 5;

        private static string[] _itemSlotString = new[] { "A", "B", "X", "Y" };

        //  3
        // 2 1
        //  0
        private static Rectangle[] _itemSlots = {
            new Rectangle(RecItemselection.Width + DistX / 2 - RecItemselection.Width / 2,
                RecItemselection.Height * 2 + DistY * 2,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(RecItemselection.Width + DistX, RecItemselection.Height + DistY,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(0, RecItemselection.Height + DistY,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(RecItemselection.Width + DistX / 2 - RecItemselection.Width / 2, 0,
                RecItemselection.Width, RecItemselection.Height)
        };


        private int _selectedItemSlot;

        private readonly Point _itemRectangleSize = new Point(27, 26);
        private readonly Point _itemRecMargin = new Point(0, 0);

        private readonly Point _equipmentPosition = new Point(6, 123);
        private readonly Rectangle _itemsRectangle = new Rectangle(6, 124, 108, 52);

        private readonly int _width;
        private readonly int _height;

        private float _selectionCounter;
        private const int SelectionTime = 125;
        private bool _selectionButtonPressed;

        public InventoryOverlay(int width, int height)
        {
            _width = width;
            _height = height;

            _rubeePosition = new Point(width - ItemDrawHelper.RubeeSize.X - Margin, 10);

            var blockPosition = 97;
            _skirtPosition = new Point(blockPosition, 5);
            _heartPiecePosition = new Point(blockPosition += 34, 6);
            _flipperRectangle = new Rectangle(blockPosition += 18, 0, 12, _background0.Height);
            _potionRectangle = new Rectangle(blockPosition += 12, 0, 12, _background0.Height);

            // key positions
            for (var i = 0; i < 5; i++)
                _keyPositions[i] = new Rectangle(96, 12 + i * 21, 16, 16);

            // relict positions
            for (var i = 0; i < 8; i++)
                _relicOffsets[i] = new Rectangle(i * 17, 0, 16, 16);

            _ocarinaFaces[0] = Resources.GetSprite("ocarina1");
            _ocarinaFaces[1] = Resources.GetSprite("ocarina2");
            _ocarinaFaces[2] = Resources.GetSprite("ocarina3");
        }

        public void UpdateRenderTarget()
        {
            if (_renderTarget == null || _renderTarget.Width != _width * Game1.UiScale || _renderTarget.Height != _height * Game1.UiScale)
                _renderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, _width * Game1.UiScale, _height * Game1.UiScale);
        }

        public void UpdateMenu()
        {
            for (var i = 0; i < 4; i++)
            {
                if (ControlHandler.ButtonPressed((CButtons)((int)CButtons.A * Math.Pow(2, i))))
                {
                    Game1.GameManager.PlaySoundEffect("D360-19-13");
                    Game1.GameManager.ChangeItem(i, _selectedItemSlot + Values.HandItemSlots);
                }
            }

            var selectionOffset = 0;

            var direction = ControlHandler.GetMoveVector2();
            if (direction.Length() > Values.ControllerDeadzone)
            {
                _selectionCounter -= Game1.DeltaTime;
                if (_selectionCounter <= 0 || !_selectionButtonPressed)
                {
                    _selectionCounter += SelectionTime;

                    var dir = AnimationHelper.GetDirection(direction);
                    if (dir == 0)
                        selectionOffset -= 1;
                    else if (dir == 1)
                        selectionOffset -= ItemSlotWidth;
                    else if (dir == 2)
                        selectionOffset += 1;
                    else if (dir == 3)
                        selectionOffset += ItemSlotWidth;
                }

                _selectionButtonPressed = true;
            }
            else
            {
                _selectionButtonPressed = false;
                _selectionCounter = SelectionTime;
            }

            // update the selected ocarina song
            var selectedItem = Game1.GameManager.Equipment[Values.HandItemSlots + _selectedItemSlot];
            if (selectedItem != null && selectedItem.Name == "ocarina")
            {
                if ((selectionOffset == -1 || selectionOffset == 1) &&
                    MoveOcarinaSelection(selectionOffset))
                    selectionOffset = 0;
            }

            _selectedItemSlot += selectionOffset;

            var slots = GameManager.EquipmentSlots - 4;
            if (_selectedItemSlot < 0)
                _selectedItemSlot += slots;
            if (_selectedItemSlot >= slots)
                _selectedItemSlot = _selectedItemSlot % slots;
        }

        private bool MoveOcarinaSelection(int direction)
        {
            var previousSong = Game1.GameManager.SelectedOcarinaSong;

            for (var i = 0; i < Game1.GameManager.Equipment.Length; i++)
            {
                Game1.GameManager.SelectedOcarinaSong += direction;
                if (Game1.GameManager.SelectedOcarinaSong < 0 ||
                    Game1.GameManager.SelectedOcarinaSong >= _ocarinaFaces.Length)
                {
                    Game1.GameManager.SelectedOcarinaSong = previousSong;
                    return false;
                }

                if (Game1.GameManager.SelectedOcarinaSong != -1 &&
                    Game1.GameManager.OcarinaSongs[Game1.GameManager.SelectedOcarinaSong] == 1)
                    return Game1.GameManager.SelectedOcarinaSong != previousSong;
            }

            Game1.GameManager.SelectedOcarinaSong = -1;
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle drawPosition, Color color)
        {
            spriteBatch.Draw(_renderTarget, drawPosition, color);
        }

        public void DrawRT(SpriteBatch spriteBatch)
        {
            Game1.Graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);

            Resources.RoundedCornerEffect.Parameters["scale"].SetValue(Game1.UiRtScale);
            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(3f);

            // draw the background
            spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, Resources.RoundedCornerEffect, Matrix.CreateScale(Game1.UiRtScale));

            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_background0.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_background0.Height);
            spriteBatch.Draw(Resources.SprWhite, _background0, Values.InventoryBackgroundColorTop);

            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_background1.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_background1.Height);
            spriteBatch.Draw(Resources.SprWhite, _background1, Values.InventoryBackgroundColor);

            spriteBatch.End();

            // draw the backgrounds of the items
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, Resources.RoundedCornerEffect, Matrix.CreateScale(Game1.UiRtScale));

                var offset = new Point(_background1.X, _background1.Y);

                for (var i = 0; i < _itemSlots.Length; i++)
                    DrawBackground(spriteBatch, offset + _itemSlotsPosition, _itemSlots[i]);

                DrawBackground(spriteBatch, offset, _keyRectangle);
                DrawBackground(spriteBatch, offset, _relictsRectangle);
                DrawBackground(spriteBatch, offset, _itemsRectangle);
                DrawBackground(spriteBatch, offset, _tradeStuffRectangle);

                // draw the item selection
                var selectionPosition = new Point(
                    (_itemsRectangle.X + _selectedItemSlot % ItemSlotWidth * (_itemRectangleSize.X + _itemRecMargin.X)),
                    (_itemsRectangle.Y + _selectedItemSlot / ItemSlotWidth * (_itemRectangleSize.Y + _itemRecMargin.Y)));
                DrawBackground(spriteBatch, offset + selectionPosition, new Rectangle(0, 0, _itemRectangleSize.X, _itemRectangleSize.Y));

                // draw the collected items
                for (var i = 0; i < Game1.GameManager.Equipment.Length - Values.HandItemSlots; i++)
                {
                    var slotRectangle = new Rectangle(
                        i % ItemSlotWidth * (_itemRectangleSize.X + _itemRecMargin.X) + _itemRectangleSize.X / 2 - 2,
                        i / ItemSlotWidth * (_itemRectangleSize.Y + _itemRecMargin.Y) + _itemRectangleSize.Y - 8, 4, 2);

                    if (Game1.GameManager.Equipment[Values.HandItemSlots + i] == null)
                        DrawBackground(spriteBatch, offset + _equipmentPosition, slotRectangle, 1);
                }

                // key background dots
                for (var i = 0; i < 5; i++)
                {
                    var slotRectangle = new Rectangle(
                        _keyPositions[i].X + _keyPositions[i].Width / 2 - 2,
                        _keyPositions[i].Y + _keyPositions[i].Height - 2, 4, 2);

                    var itemKey = Game1.GameManager.GetItem("dkey" + (i + 1));
                    if (itemKey == null)
                        DrawBackground(spriteBatch, offset, slotRectangle, 1);
                }

                for (var i = 0; i < _relicOffsets.Length; i++)
                {
                    var name = "instrument" + i;
                    var hasItem = Game1.GameManager.GetItem(name) != null;

                    if (!hasItem)
                    {
                        var position = new Point(_relictPosition.X + _relicOffsets[i].X + _relicOffsets[i].Width / 2 - 2, _relictPosition.Y + _relicOffsets[i].Bottom - 2);
                        DrawBackground(spriteBatch, offset + position, new Rectangle(0, 0, 4, 2), 1);
                    }
                }

                spriteBatch.End();
            }

            // draw the map
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Game1.UiRtScale));

            {
                var heartOffset = new Point(0, Game1.GameManager.MaxHearths > 7 ? 0 : 4);
                ItemDrawHelper.DrawHearts(spriteBatch, _heartsPosition + heartOffset, 1, Color.White);

                // draw the skirt
                DrawSkirt(spriteBatch, _skirtPosition);

                DrawHeartContainer(spriteBatch, _heartPiecePosition);

                ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.GetItem("flippers"), Point.Zero, _flipperRectangle, 1, Color.White);

                ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.GetItem("potion"), Point.Zero, _potionRectangle, 1, Color.White);

                ItemDrawHelper.DrawRubee(spriteBatch, _rubeePosition, 1, Color.Black);

                var offsetBottom = new Point(_background1.X, _background1.Y);

                // center the items
                {
                    var width = 0;
                    var hasTradeItem = false;
                    for (var i = 0; i < 15; i++)
                    {
                        hasTradeItem = Game1.GameManager.GetItem("trade" + i) != null;
                        if (hasTradeItem)
                        {
                            width += 28;
                            break;
                        }
                    }
                    var itemShell = Game1.GameManager.GetItem("shell");
                    var itemLeaf = Game1.GameManager.GetItem("goldLeaf");
                    if (itemShell != null)
                        width += 28;
                    if (itemLeaf != null)
                        width += 28;

                    var posX = _tradeStuffRectangle.Width / 2 - width / 2;
                    _tradeRectangle = new Rectangle(6 + posX, 6, 28, 22);

                    if (hasTradeItem)
                        posX += 28;
                    _shellRectangle = new Rectangle(6 + posX, 6, 28, 22);

                    if (itemShell != null)
                        posX += 28;
                    _leafRectangle = new Rectangle(6 + posX, 6, 28, 22);

                    // draw the current trade item
                    DrawTradeItem(spriteBatch, offsetBottom, 1);
                    ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.GetItem("shell"), offsetBottom, _shellRectangle, 1, Color.White);
                    ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.GetItem("goldLeaf"), offsetBottom, _leafRectangle, 1, Color.White);
                }

                // draw the collected equipment
                DrawEquipment(spriteBatch, offsetBottom + _equipmentPosition);

                // draw the item slots
                for (var i = 0; i < _itemSlots.Length; i++)
                {
                    ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.Equipment[i], offsetBottom + _itemSlotsPosition, _itemSlots[i], 1, Color.White);

                    spriteBatch.DrawString(Resources.GameFont, _itemSlotString[i], new Vector2(
                        offsetBottom.X + _itemSlotsPosition.X + _itemSlots[i].Right - 4,
                        offsetBottom.Y + _itemSlotsPosition.Y + _itemSlots[i].Bottom - 4), Color.Black);
                }

                // draw the collected keys
                for (var i = 0; i < 5; i++)
                    ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.GetItem("dkey" + (i + 1)), offsetBottom, _keyPositions[i], 1, Color.White);

                DrawRelicts(spriteBatch, offsetBottom + _relictPosition);
            }

            spriteBatch.End();
        }

        private void DrawBackground(SpriteBatch spriteBatch, Point offset, Rectangle rectangle, float radius = 3f)
        {
            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(radius);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(rectangle.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(rectangle.Height);

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(offset.X + rectangle.X, offset.Y + rectangle.Y, rectangle.Width, rectangle.Height), Color.Black * 0.15f);
        }

        public void DrawEquipment(SpriteBatch spriteBatch, Point drawPosition)
        {
            // draw the collected items
            for (var i = 0; i < Game1.GameManager.Equipment.Length - Values.HandItemSlots; i++)
            {
                var slotRectangle = new Rectangle(
                    i % ItemSlotWidth * (_itemRectangleSize.X + _itemRecMargin.X),
                    i / ItemSlotWidth * (_itemRectangleSize.Y + _itemRecMargin.Y),
                    _itemRectangleSize.X, _itemRectangleSize.Y);

                // draw the item
                var itemIndex = i + Values.HandItemSlots;
                var offsetY = _selectedItemSlot == i ? -1 : 0;

                if (_selectedItemSlot == i &&
                    Game1.GameManager.Equipment[itemIndex] != null &&
                    Game1.GameManager.Equipment[itemIndex].Name == "ocarina")
                {
                    var hasSong = false;
                    for (var j = 0; j < Game1.GameManager.OcarinaSongs.Length; j++)
                        if (Game1.GameManager.OcarinaSongs[j] != 0)
                        {
                            hasSong = true;
                            break;
                        }

                    if (hasSong)
                        continue;
                }

                ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.Equipment[itemIndex], new Point(drawPosition.X, drawPosition.Y + offsetY + 1), slotRectangle, 1, Color.White);
            }

            // draw the ocarina face selection
            var selectedItem = Game1.GameManager.Equipment[4 + _selectedItemSlot];
            if (selectedItem != null && selectedItem.Name == "ocarina")
            {
                var selectedSong = Game1.GameManager.SelectedOcarinaSong;
                if (selectedSong != -1)
                {
                    var hasSong = Game1.GameManager.OcarinaSongs[selectedSong] == 1;
                    var position = new Vector2(
                        drawPosition.X + (_selectedItemSlot % ItemSlotWidth * (_itemRectangleSize.X + _itemRecMargin.X)) +
                        _itemRectangleSize.X / 2 - _ocarinaFaces[selectedSong].ScaledRectangle.Width / 2,
                        drawPosition.Y + (_selectedItemSlot / ItemSlotWidth * (_itemRectangleSize.Y + _itemRecMargin.Y)) +
                        _itemRectangleSize.Y / 2 - _ocarinaFaces[selectedSong].ScaledRectangle.Height / 2);

                    DrawHelper.DrawNormalized(spriteBatch, _ocarinaFaces[selectedSong], position, hasSong ? Color.White : Color.Gray);
                }
            }
        }

        public void DrawRelicts(SpriteBatch spriteBatch, Point drawPosition)
        {
            // draw the relicts
            for (var i = 0; i < _relicOffsets.Length; i++)
            {
                var name = "instrument" + i;
                var hasItem = Game1.GameManager.GetItem(name) != null;
                var item = Game1.GameManager.ItemManager[name];

                if (hasItem)
                    ItemDrawHelper.DrawInstrument(spriteBatch, item.Sprite, new Vector2(
                        drawPosition.X + _relicOffsets[i].X, drawPosition.Y + _relicOffsets[i].Y));
            }
        }

        public void DrawHeartContainer(SpriteBatch spriteBatch, Point drawPosition)
        {
            // heartMeter
            var item = Game1.GameManager.GetItem("heartMeter");
            var count = 0;

            if (item != null)
                count = item.Count;

            // draw the heart container
            spriteBatch.Draw(Resources.SprItem, new Rectangle(
                    drawPosition.X, drawPosition.Y, _heartPiecesRectangle.Width, _heartPiecesRectangle.Height),
                new Rectangle(_heartPiecesRectangle.X + (_heartPiecesRectangle.Width + 2) * count,
                    _heartPiecesRectangle.Y, _heartPiecesRectangle.Width, _heartPiecesRectangle.Height), Color.White);
        }

        public void DrawTradeItem(SpriteBatch spriteBatch, Point drawPosition, int scale)
        {
            // draw the current trade item
            for (var i = 0; i < 15; i++)
            {
                var hasItem = Game1.GameManager.GetItem("trade" + i) != null;

                if (!hasItem)
                    continue;

                // draw the key
                DrawHelper.DrawCenter(spriteBatch, Resources.SprItem,
                    drawPosition, _tradeRectangle, Game1.GameManager.ItemManager["trade" + i].SourceRectangle.Value, scale);

                break;
            }
        }

        public void DrawSkirt(SpriteBatch spriteBatch, Point drawPosition)
        {
            // draw GBR
            spriteBatch.Draw(Resources.SprItem, new Rectangle(
                    drawPosition.X,
                    drawPosition.Y + 2,
                    _skirtColorRectangle.Width, _skirtColorRectangle.Height),
                new Rectangle(
                    _skirtColorRectangle.X,
                    _skirtColorRectangle.Y + Game1.GameManager.CloakType * (_skirtColorRectangle.Height + 1),
                    _skirtColorRectangle.Width, _skirtColorRectangle.Height), Color.White);

            var skirtPosition = new Point(drawPosition.X + _skirtColorRectangle.Width + 1, drawPosition.Y);

            // draw the skirt
            spriteBatch.Draw(Resources.SprItem, new Rectangle(
                skirtPosition.X, skirtPosition.Y,
                _skirtRectangle.Width, _skirtRectangle.Height), _skirtRectangle, Color.White);

            // draw the skirt color
            spriteBatch.Draw(Resources.SprItem, new Rectangle(
                    skirtPosition.X, skirtPosition.Y,
                    _skirtRectangle.Width, _skirtRectangle.Height),
                new Rectangle(_skirtRectangle.X, _skirtRectangle.Y + _skirtRectangle.Height,
                    _skirtRectangle.Width, _skirtRectangle.Height), Values.SkirtColors[Game1.GameManager.CloakType]);
        }
    }
}
