using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    public class MapOverlay
    {
        public bool IsSelected;

        private RenderTarget2D _renderTarget;

        private readonly Rectangle _recMap = new Rectangle(8, 0, 144, 144);
        private readonly Rectangle _recHide = new Rectangle(167, 103, 9, 9);
        private readonly Rectangle _recIcon = new Rectangle(161, 1, 30, 30);

        private Point _selectionPosition;
        private Point _iconPosition;

        private Animator _animationPlayer = new Animator();
        private Animator _animationSelection = new Animator();

        private readonly int[,] _mapIcons = new int[16, 16];

        private readonly string[,] _mapDialog = new string[,] {
        { "map_tal_tal", "map_tal_tal", "map_tal_tal", "map_tal_tal", "map_tal_tal", "map_tal_tal", "map_wind_fishs_egg", "map_mt_tamaranch", "map_owl_bridge", "map_tal_tal", "map_hen_house", "map_tal_tal", "map_tal_tal", "map_tal_tal", "map_level_7", "map_tal_tal" },
        { "map_level_8","map_telephone_booth","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal","map_owl_fish","map_owl_mountain","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal","map_tal_tal" },
        { "map_goponga_swamp","map_goponga_swamp","map_goponga_swamp","map_goponga_swamp","map_level_2","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_level_4","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights" },
        { "map_weird_mr_write","map_telephone_booth","map_goponga_swamp","map_goponga_swamp","map_goponga_swamp","map_tal_tal_heights","map_owl_tal_tal_heights","map_photo_both","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_tal_tal_heights","map_raft_shop" },
        { "map_mysterious_woods","map_owl_woods","map_mysterious_woods","map_mysterious_woods","map_koholint_prairie","map_crazy_tracy","map_tabahl_wasteland","map_tabahl_wasteland","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_telephone_booth","map_rapids_ride","map_rapids_ride","map_rapids_ride","map_rapids_ride" },
        { "map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_koholint_prairie","map_koholint_prairie","map_tabahl_wasteland","map_tabahl_wasteland","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_rapids_ride","map_rapids_ride","map_rapids_ride","map_rapids_ride" },
        { "map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_owl_prairie","map_witchs_hut","map_cementry","map_cementry","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_rapids_ride","map_rapids_ride","map_rapids_ride","map_rapids_ride" },
        { "map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_mysterious_woods","map_koholint_prairie","map_koholint_prairie","map_cementry","map_cementry","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_kanalet_castle","map_rapids_ride","map_rapids_ride","map_rapids_ride","map_rapids_ride" },
        { "map_owl_woods_entry","map_fishing_pond","map_quadruplets_house","map_dream_shrine","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_telephone_booth","map_ukuku_prairie","map_seashell_mansion","map_ukuku_prairie","map_level_6","map_face_shrine","map_rapids_ride","map_rapids_ride" },
        { "map_mysterious_woods","map_mysterious_woods","map_mabe_village","map_town_tool_shop","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_owl_level_6_post","map_owl_level_6","map_rapids_ride","map_rapids_ride"},
        { "map_mabe_village","map_madam_meow","map_marin_tarin","map_mabe_village","map_telephone_booth","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_owl_shrine","map_face_shrine","map_face_shrine","map_face_shrine" },
        { "map_village_library","map_old_man_house","map_telephone_booth","map_trendy_game","map_ukuku_prairie","map_level_3","map_owl_level_3","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_ukuku_prairie","map_face_shrine","map_face_shrine","map_face_shrine","map_face_shrine"},
        { "map_south_village","map_south_village","map_south_village","map_south_village","map_signpost_maze","map_signpost_maze","map_pothole_field","map_pothole_field","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_animal_village","map_animal_village","map_yarna_desert","map_yarna_desert"},
        { "map_south_village","map_south_village","map_owl_level_1","map_level_1","map_signpost_maze","map_signpost_maze","map_richards_villa","map_pothole_field","map_maraths_bay","map_level_5","map_maraths_bay","map_telephone_booth","map_animal_village","map_animal_village","map_yarna_desert","map_yarna_desert"},
        { "map_toronbo_shores","map_toronbo_shores","map_toronbo_shores","map_banana_house","map_toronbo_shores","map_toronbo_shores","map_maraths_bay","map_maraths_bay","map_telephone_booth","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_bay_east","map_bay_east","map_owl_desert","map_yarna_desert"},
        { "map_toronbo_shores","map_toronbo_shores","map_owl_shore","map_toronbo_shores","map_toronbo_shores","map_toronbo_shores","map_house_bay","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_maraths_bay","map_bay_east","map_bay_east","map_yarna_desert","map_yarna_desert"}};

        private float _animationCount;
        private float _animationState;

        private int _width;
        private int _height;
        private int _margin;

        private int _iconAnimationDirection;
        private int _shownSelection;

        private double _buttonDownCounter;

        private bool _iconAnimationRunning;
        private bool _fullMap;

        public MapOverlay(int width, int height, int margin, bool fullMap)
        {
            _width = width;
            _height = height;
            _margin = margin;

            _fullMap = fullMap;

            // 1 shop
            // 2 ?
            // 3 cave
            // 4 owl
            _mapIcons[6, 0] = 2;
            _mapIcons[8, 0] = 4;
            _mapIcons[10, 0] = 2;
            _mapIcons[14, 0] = 3;

            _mapIcons[0, 1] = 3;
            _mapIcons[1, 1] = 2;
            _mapIcons[6, 1] = 4;
            _mapIcons[7, 1] = 4;

            _mapIcons[4, 2] = 3;
            _mapIcons[11, 2] = 3;

            _mapIcons[0, 3] = 2;
            _mapIcons[1, 3] = 2;
            _mapIcons[6, 3] = 4;
            _mapIcons[7, 3] = 1;
            _mapIcons[15, 3] = 3;

            _mapIcons[1, 4] = 4;
            _mapIcons[5, 4] = 1;
            _mapIcons[11, 4] = 2;

            _mapIcons[4, 6] = 4;
            _mapIcons[5, 6] = 1;

            _mapIcons[0, 8] = 4;
            _mapIcons[1, 8] = 1;
            _mapIcons[2, 8] = 2;
            _mapIcons[3, 8] = 2;
            _mapIcons[8, 8] = 2;
            _mapIcons[10, 8] = 2;
            _mapIcons[12, 8] = 3;

            _mapIcons[3, 9] = 2;
            _mapIcons[12, 9] = 4;
            _mapIcons[13, 9] = 4;

            _mapIcons[1, 10] = 2;
            _mapIcons[2, 10] = 2;
            _mapIcons[4, 10] = 2;
            _mapIcons[12, 10] = 4;

            _mapIcons[0, 11] = 2;
            _mapIcons[1, 11] = 2;
            _mapIcons[2, 11] = 2;
            _mapIcons[3, 11] = 1;
            _mapIcons[5, 11] = 3;
            _mapIcons[6, 11] = 4;

            _mapIcons[2, 13] = 4;
            _mapIcons[3, 13] = 3;
            _mapIcons[6, 13] = 2;
            _mapIcons[9, 13] = 3;
            _mapIcons[11, 13] = 2;

            _mapIcons[3, 14] = 2;
            _mapIcons[8, 14] = 2;
            _mapIcons[14, 14] = 4;

            _mapIcons[2, 15] = 4;
            _mapIcons[6, 15] = 2;
        }

        public void Load()
        {
            _animationPlayer = AnimatorSaveLoad.LoadAnimator("mapPlayer");
            _animationSelection = AnimatorSaveLoad.LoadAnimator("mapSelector");

            _animationPlayer.Play("idle");
            _animationSelection.Play("idle");
        }

        public void UpdateRenderTarget()
        {
            if (_renderTarget == null || _renderTarget.Width != _width * Game1.UiScale || _renderTarget.Height != _height * Game1.UiScale)
                _renderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, _width * Game1.UiScale, _height * Game1.UiScale);
        }

        public void Update()
        {
            _animationPlayer.Update();

            var mapIcon = _mapIcons[_selectionPosition.X, _selectionPosition.Y];

            // for owl icons we only show the icon if the owl key was already set
            if (mapIcon == 4 && Game1.GameManager.SaveManager.GetString(_mapDialog[_selectionPosition.Y, _selectionPosition.X], "0") != "1")
                mapIcon = 0;

            if ((mapIcon != _shownSelection || (mapIcon != 0 && !IsSelected)) && !_iconAnimationRunning)
            {
                if (mapIcon != 0 && _shownSelection == 0 && IsSelected)
                    PlayStartAnimation();
                else
                    PlayStopAnimation();
            }

            // update the icon run animation
            if (_iconAnimationRunning)
            {
                _animationCount += Game1.DeltaTime / 100f * _iconAnimationDirection;

                if (_animationCount >= Math.PI / 2)
                {
                    _iconAnimationRunning = false;
                    _animationCount = (float)(Math.PI / 2);
                }
                else if (_animationCount < 0)
                {
                    _iconAnimationRunning = false;
                    _shownSelection = 0;
                }

                _animationState = (float)Math.Sin(_animationCount);
            }

            if (!IsSelected)
                return;

            _animationSelection.Update();

            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                UpdateInput();
        }

        private void UpdateInput()
        {
            if (ControlHandler.ButtonDown(CButtons.Left) || ControlHandler.ButtonDown(CButtons.Right) ||
                ControlHandler.ButtonDown(CButtons.Up) || ControlHandler.ButtonDown(CButtons.Down))
                _buttonDownCounter -= Game1.DeltaTime;
            else
                _buttonDownCounter = 225;

            if (ControlHandler.ButtonPressed(CButtons.Left) || (ControlHandler.ButtonDown(CButtons.Left) && _buttonDownCounter < 0))
            {
                _buttonDownCounter += 50;
                MoveSelection(_selectionPosition + new Point(-1, 0));
            }
            if (ControlHandler.ButtonPressed(CButtons.Right) || (ControlHandler.ButtonDown(CButtons.Right) && _buttonDownCounter < 0))
            {
                _buttonDownCounter += 50;
                MoveSelection(_selectionPosition + new Point(1, 0));
            }
            if (ControlHandler.ButtonPressed(CButtons.Up) || (ControlHandler.ButtonDown(CButtons.Up) && _buttonDownCounter < 0))
            {
                _buttonDownCounter += 50;
                MoveSelection(_selectionPosition + new Point(0, -1));
            }
            if (ControlHandler.ButtonPressed(CButtons.Down) || (ControlHandler.ButtonDown(CButtons.Down) && _buttonDownCounter < 0))
            {
                _buttonDownCounter += 50;
                MoveSelection(_selectionPosition + new Point(0, 1));
            }

            if (ControlHandler.ButtonPressed(CButtons.A))
            {
                if (0 <= _selectionPosition.X && _selectionPosition.X < _mapDialog.GetLength(1) &&
                    0 <= _selectionPosition.Y && _selectionPosition.Y < _mapDialog.GetLength(0))
                    Game1.GameManager.RunDialog(_mapDialog[_selectionPosition.Y, _selectionPosition.X]);
            }
        }

        public void PlayStartAnimation()
        {
            _iconPosition = new Point(8, 8);

            _iconPosition.X += _selectionPosition.X >= _mapIcons.GetLength(0) / 2 ? 8 : _mapIcons.GetLength(0) * 8 - _recIcon.Width - 8;
            _iconPosition.Y += _selectionPosition.Y >= _mapIcons.GetLength(1) / 2 ? _mapIcons.GetLength(1) * 8 - _recIcon.Height - 8 : 8;

            _animationCount = 0;
            _iconAnimationDirection = 1;
            _iconAnimationRunning = true;
            _shownSelection = _mapIcons[_selectionPosition.X, _selectionPosition.Y];
        }

        public void PlayStopAnimation()
        {
            _animationCount = (float)(Math.PI / 2);
            _iconAnimationDirection = -1;
            _iconAnimationRunning = true;
        }

        public void OnFocus()
        {
            _shownSelection = 0;
            _animationState = 0;

            if (Game1.GameManager.PlayerMapPosition != null)
                _selectionPosition = Game1.GameManager.PlayerMapPosition.Value;
        }

        public void MoveSelection(Point newPosition)
        {
            if (newPosition.X < 0)
                newPosition.X += Game1.GameManager.MapVisibility.GetLength(0);
            if (newPosition.X >= Game1.GameManager.MapVisibility.GetLength(0))
                newPosition.X -= Game1.GameManager.MapVisibility.GetLength(0);
            if (newPosition.Y < 0)
                newPosition.Y += Game1.GameManager.MapVisibility.GetLength(1);
            if (newPosition.Y >= Game1.GameManager.MapVisibility.GetLength(1))
                newPosition.Y -= Game1.GameManager.MapVisibility.GetLength(1);

            // only move the selection if the new position is visible
            if (newPosition.X >= 0 && newPosition.Y >= 0 &&
                newPosition.X < Game1.GameManager.MapVisibility.GetLength(0) &&
                newPosition.Y < Game1.GameManager.MapVisibility.GetLength(1) &&
                (_fullMap || Game1.GameManager.MapVisibility[newPosition.X, newPosition.Y]))
            {
                _selectionPosition = newPosition;

                _animationSelection.Stop();
                _animationSelection.Play("idle");
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle drawPosition, Color color, Matrix? matrix = null)
        {
            Resources.RoundedCornerEffect.Parameters["scale"].SetValue(Game1.UiRtScale);
            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(2f);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_height);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, Resources.RoundedCornerEffect, matrix);

            spriteBatch.Draw(_renderTarget, drawPosition, color);

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, matrix);

            if (Game1.GameManager.PlayerMapPosition != null)
            {
                var mapRectangle = new Point(drawPosition.X + _margin, drawPosition.Y + _margin);
                // draw the player
                var position = new Vector2(
                    mapRectangle.X + (8 + Game1.GameManager.PlayerMapPosition.Value.X * 8 + 2) * Game1.UiRtScale,
                    mapRectangle.Y + (8 + Game1.GameManager.PlayerMapPosition.Value.Y * 8 + 2) * Game1.UiRtScale);
                _animationPlayer.DrawBasic(spriteBatch, position, color, Game1.UiRtScale);

                // draw the selection
                if (IsSelected)
                {
                    position = new Vector2(
                        mapRectangle.X + (8 + _selectionPosition.X * 8 + 1) * Game1.UiRtScale,
                        mapRectangle.Y + (8 + _selectionPosition.Y * 8 + 1) * Game1.UiRtScale);
                    _animationSelection.DrawBasic(spriteBatch, position, color, Game1.UiRtScale);
                }
            }

            spriteBatch.End();
        }

        public void DrawRenderTarget(SpriteBatch spriteBatch)
        {
            Game1.Graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);

            // draw the map
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Matrix.CreateScale(Game1.UiRtScale));
            DrawMap(spriteBatch);
            spriteBatch.End();
        }

        public void DrawMap(SpriteBatch spriteBatch)
        {
            var mapRectangle = new Point(_margin, _margin);

            // draw the map
            spriteBatch.Draw(Resources.SprMiniMap,
                new Rectangle(mapRectangle.X, mapRectangle.Y, _recMap.Width, _recMap.Height), _recMap, Color.White);

            // overlay the not discovered parts of the map
            if (!_fullMap)
                for (var x = 0; x < 16; x++)
                {
                    for (var y = 0; y < 16; y++)
                    {
                        if (!Game1.GameManager.MapVisibility[x, y])
                            spriteBatch.Draw(Resources.SprMiniMap, new Rectangle(
                                mapRectangle.X + 8 + x * 8,
                                mapRectangle.Y + 8 + y * 8,
                                _recHide.Width, _recHide.Height), _recHide, Color.White);
                    }
                }

            // draw icon of the selection
            if (_shownSelection > 0)
            {
                DrawIcon(spriteBatch, new Point(
                    mapRectangle.X + _iconPosition.X,
                    mapRectangle.Y + _iconPosition.Y), _shownSelection, 1, _animationState);
            }
        }

        public void DrawIcon(SpriteBatch spriteBatch, Point position, int icon, int scale, float animationPercentage)
        {
            var width = (int)(_recIcon.Width * animationPercentage) / 2;
            var height = (int)(_recIcon.Height * animationPercentage) / 2;
            var posX = position.X + (_recIcon.Width / 2 - width);
            var posY = position.Y + (_recIcon.Height / 2 - height);

            spriteBatch.Draw(Resources.SprMiniMap, new Rectangle(posX, posY, width * 2, height * 2),
                new Rectangle(_recIcon.X + _recIcon.Width * (icon - 1),
                    _recIcon.Y, _recIcon.Width, _recIcon.Height), Color.White);
        }
    }
}