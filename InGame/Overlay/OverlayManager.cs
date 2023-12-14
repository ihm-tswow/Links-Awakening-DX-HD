using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Overlay.Sequences;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    public class OverlayManager
    {
        public float HudTransparency = 1;
        public bool DisableOverlayToggle;
        public bool DisableInventoryToggle;

        enum MenuState
        {
            None, Menu, Inventory, PhotoBook, GameSequence
        }

        private MenuState _currentMenuState = MenuState.None;
        private MenuState _lastMenuState = MenuState.None;

        public TextboxOverlay TextboxOverlay;
        public HudOverlay InGameHud;

        private InventoryOverlay _inventoryOverlay;
        private MapOverlay _mapOverlay;
        private DungeonOverlay _dungeonOverlay;
        private PhotoOverlay _photoOverlay;

        private Dictionary<string, GameSequence> _gameSequences = new Dictionary<string, GameSequence>();
        private string _currentSequenceName;

        private RenderTarget2D _menuRenderTarget2D;

        private UiRectangle _blurRectangle;

        private Rectangle _recInventory;
        private Rectangle _recMap;
        private Rectangle _recMapCenter;
        private Rectangle _recDungeon;

        private Vector2 _menuPosition;

        private Point _inventorySize;
        private Point _mapSize;
        private Point _dungeonSize;
        private Point _overlaySize;

        private double _fadeCount;
        private float _fadeAnimationPercentage;

        private float _hudState = 1;
        private float _hudPercentage;
        private bool _hideHud;

        private readonly int _marginMap = 0;
        private readonly int _margin = 2;
        private readonly int _fadeTime = 200;
        private const int ChangeTime = 125;
        private int _fadeDir;
        private int _scale;
        private float _changeCount;

        private int _overlayWidth;
        private int _overlayHeight;

        private bool _fading;
        private bool _updateInventory = true;
        private bool _isChanging;

        public OverlayManager()
        {
            // setup blurry overlay
            _blurRectangle = (UiRectangle)Game1.EditorUi.AddElement(
                new UiRectangle(Rectangle.Empty, "background", Values.ScreenNameGame, Color.Transparent, Color.Transparent, null));
        }

        public void Load(ContentManager content)
        {
            _gameSequences.Add("map", new MapOverlaySequence());
            _gameSequences.Add("marinBeach", new MarinBeachSequence());
            _gameSequences.Add("marinCliff", new MarinCliffSequence());
            _gameSequences.Add("towerCollapse", new TowerCollapseSequence());
            _gameSequences.Add("shrine", new ShrineSequence());
            _gameSequences.Add("picture", new PictureSequence());
            _gameSequences.Add("photo", new PhotoSequence());
            _gameSequences.Add("bowWow", new BowWowSequence());
            _gameSequences.Add("castle", new CastleSequence());
            _gameSequences.Add("gravestone", new GravestoneSequence());
            _gameSequences.Add("weatherBird", new WeatherBirdSequence());
            _gameSequences.Add("final", new FinalSequence());

            _mapSize = new Point(144 + 2 * _marginMap, 144 + 2 * _marginMap);
            _dungeonSize = new Point(80, 106);
            _inventorySize = new Point(268, 208);
            _overlaySize = new Point(_inventorySize.X + _margin + _dungeonSize.X, _inventorySize.Y);

            TextboxOverlay = new TextboxOverlay();
            InGameHud = new HudOverlay();
            _mapOverlay = new MapOverlay(_mapSize.X, _mapSize.Y, _marginMap, false);
            _inventoryOverlay = new InventoryOverlay(_inventorySize.X, _inventorySize.Y);
            _dungeonOverlay = new DungeonOverlay(_dungeonSize.X, _dungeonSize.Y);
            _photoOverlay = new PhotoOverlay();

            _mapOverlay.Load();
            _dungeonOverlay.Load();
            _photoOverlay.Load();
        }

        public void OnLoad()
        {
            CloseOverlay();
            _hideHud = false;
            _fadeCount = 0;
            TextboxOverlay.Init();
        }

        public void Update()
        {
            // toggle game menu
            if ((_currentMenuState == MenuState.None || _currentMenuState == MenuState.Menu) &&
                ControlHandler.ButtonPressed(CButtons.Start))
                ToggleState(MenuState.Menu);

            // toggle inventory/map
            if ((_currentMenuState == MenuState.None || _currentMenuState == MenuState.Inventory) &&
                ControlHandler.ButtonPressed(CButtons.Select) && !DisableInventoryToggle && !_hideHud && !TextboxOverlay.IsOpen)
                ToggleState(MenuState.Inventory);

            if (_currentMenuState == MenuState.None)
            {
                // update the textbox
                TextboxOverlay.Update();
            }
            else if (_currentMenuState == MenuState.Menu)
            {
                Game1.UpdateGame = false;
            }
            else if (_currentMenuState == MenuState.Inventory)
            {
                Game1.UpdateGame = false;

                if (_isChanging)
                {
                    _changeCount += (_updateInventory ? 1 : -1) * Game1.DeltaTime;
                    if (_changeCount >= ChangeTime || _changeCount < 0)
                    {
                        _isChanging = false;
                        _changeCount = _updateInventory ? ChangeTime : 0;
                        _updateInventory = !_updateInventory;
                    }
                }
                else
                {
                    if (ControlHandler.ButtonPressed(CButtons.Start) && !TextboxOverlay.IsOpen)
                        ToggleInventoryMap();

                    if (_updateInventory)
                        _inventoryOverlay.UpdateMenu();

                    _mapOverlay.IsSelected = !_updateInventory;
                    _mapOverlay.Update();
                    // update the text box
                    TextboxOverlay.Update();

                    _dungeonOverlay.Update();
                }
            }
            else if (_currentMenuState == MenuState.PhotoBook)
            {
                Game1.UpdateGame = false;

                // update the text box
                TextboxOverlay.Update();
                _photoOverlay.Update();
            }
            else if (_currentMenuState == MenuState.GameSequence)
            {
                Game1.ForceDialogUpdate = true;
                Game1.UpdateGame = false;

                // update the text box
                TextboxOverlay.Update();
                _gameSequences[_currentSequenceName].Update();
            }

            UpdateFade();

            InGameHud.Update(_hudPercentage, (1 - _hudPercentage) * HudTransparency);

            DisableOverlayToggle = false;
            DisableInventoryToggle = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // draw the game ui; fade out with overlay fadein
            InGameHud.DrawTop(spriteBatch, _hudPercentage, (1 - _hudPercentage) * HudTransparency);

            // draw the text box
            TextboxOverlay.DrawTop(spriteBatch);

            // draw the inventory/map/photo overlay/gamesequence
            if (_fadeAnimationPercentage > 0)
            {
                if (_currentMenuState == MenuState.Inventory || _lastMenuState == MenuState.Inventory)
                {
                    // draw the menu on the screen
                    var menuY = 25 * _scale * (1 - _fadeAnimationPercentage);
                    var menuColor = Color.White * _fadeAnimationPercentage;

                    int dungeonOffset;
                    if (!Game1.GameManager.MapManager.CurrentMap.DungeonMode)
                        dungeonOffset = (_margin + _dungeonSize.X) * _scale / 2;
                    else
                    {
                        // try to align the inventory while the dungeon panel is on the side of it
                        // when the resololution is not wide enough move the inventory to the left
                        dungeonOffset = Math.Clamp((_margin + _dungeonSize.X) * _scale / 2, -16, (Game1.WindowWidth - _overlayWidth) / 2 - 8);
                    }

                    spriteBatch.Draw(_menuRenderTarget2D, new Rectangle(
                        (int)_menuPosition.X + dungeonOffset, (int)(_menuPosition.Y - menuY), _overlayWidth, _overlayHeight), menuColor);
                }
                else if (_currentMenuState == MenuState.PhotoBook || _lastMenuState == MenuState.PhotoBook)
                    _photoOverlay.Draw(spriteBatch, _fadeAnimationPercentage);
                else if (_currentMenuState == MenuState.GameSequence || _lastMenuState == MenuState.GameSequence)
                    _gameSequences[_currentSequenceName].Draw(spriteBatch, _fadeAnimationPercentage);

                if (_currentMenuState == MenuState.Inventory)
                {
                    var selectStr = "";
                    if (ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.Start].Keys.Length > 0)
                        selectStr = ControlHandler.ButtonDictionary[CButtons.Start].Keys[0].ToString();
                    if (!ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.Start].Buttons.Length > 0)
                        selectStr = ControlHandler.ButtonDictionary[CButtons.Start].Buttons[0].ToString();

                    var strType = Game1.LanguageManager.GetString((_updateInventory ? "overlay_map" : "overlay_inventory"), "error");
                    var inputHelper = selectStr + ": " + strType;

                    //var selectTextSize = Resources.GameFont.MeasureString(inputHelper);
                    spriteBatch.DrawString(Resources.GameFont, inputHelper,
                        new Vector2(8 * Game1.UiScale, Game1.WindowHeight - 16 * Game1.UiScale), Color.White * _fadeAnimationPercentage, 0, Vector2.Zero, Game1.UiScale, SpriteEffects.None, 0);
                }
            }
        }

        public void DrawRenderTarget(SpriteBatch spriteBatch)
        {
            if (_fadeAnimationPercentage > 0 && (_currentMenuState == MenuState.GameSequence || _lastMenuState == MenuState.GameSequence))
                _gameSequences[_currentSequenceName].DrawRT(spriteBatch);

            if (_currentMenuState == MenuState.Inventory)
            {
                _mapOverlay.DrawRenderTarget(spriteBatch);
                _inventoryOverlay.DrawRT(spriteBatch);
                _dungeonOverlay.DrawOnRenderTarget(spriteBatch);

                // draw the inventory on a separate rendertarget
                Game1.Graphics.GraphicsDevice.SetRenderTarget(_menuRenderTarget2D);
                Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);

                DrawInventory(spriteBatch);
            }
        }

        private void DrawInventory(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp, null, null, null, null);

            var percentage = MathF.Sin(-MathF.PI / 2 + (_changeCount / ChangeTime) * MathF.PI) * 0.5f + 0.5f;

            // draw the inventory
            _inventoryOverlay.Draw(spriteBatch, _recInventory, Color.White * (1 - percentage));

            spriteBatch.End();

            // draw the map
            var mapRectangle = new Rectangle(
                (int)MathHelper.Lerp(_recMap.X, _recMapCenter.X, percentage),
                (int)MathHelper.Lerp(_recMap.Y, _recMapCenter.Y, percentage), _recMap.Width, _recMap.Height);
            _mapOverlay.Draw(spriteBatch, mapRectangle, Color.White);

            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);

            // draw the dungeon stuff
            _dungeonOverlay.Draw(spriteBatch, _recDungeon, Color.White * (1 - percentage));

            spriteBatch.End();
        }

        public void ResolutionChanged()
        {
            TextboxOverlay.ResolutionChange();

            _blurRectangle.Rectangle.Width = Game1.WindowWidth;
            _blurRectangle.Rectangle.Height = Game1.WindowHeight;

            _scale = Game1.UiScale;

            _overlayWidth = _overlaySize.X * _scale;
            _overlayHeight = _overlaySize.Y * _scale;

            _menuPosition = new Vector2(
                Game1.WindowWidth / 2 - _overlayWidth / 2, Game1.WindowHeight / 2 - _overlayHeight / 2);
        }

        public void UpdateRenderTarget()
        {
            if (_menuRenderTarget2D == null || _menuRenderTarget2D.Width != _overlayWidth || _menuRenderTarget2D.Height != _overlayHeight)
                _menuRenderTarget2D = new RenderTarget2D(Game1.Graphics.GraphicsDevice, _overlayWidth, _overlayHeight);

            _inventoryOverlay.UpdateRenderTarget();

            _mapOverlay.UpdateRenderTarget();

            _dungeonOverlay.UpdateRenderTarget();

            // 144 = size of the map
            _recInventory = new Rectangle(0, 0, _inventorySize.X * _scale, _inventorySize.Y * _scale);

            _recMap = new Rectangle(
                _recInventory.Right - 6 * _scale - _mapSize.X * _scale,
                _recInventory.Bottom - 6 * _scale - _mapSize.Y * _scale, _mapSize.X * _scale, _mapSize.Y * _scale);

            _recMapCenter = new Rectangle(
                _recInventory.Width / 2 - _mapSize.X / 2 * _scale,
                _recInventory.Height / 2 - _mapSize.Y / 2 * _scale, _mapSize.X * _scale, _mapSize.Y * _scale);

            _recDungeon = new Rectangle(
                _recInventory.Right + _margin * _scale,
                _recInventory.Bottom - _dungeonSize.Y * _scale,
                _dungeonSize.X * _scale, _dungeonSize.Y * _scale);
        }

        public void OpenPhotoOverlay()
        {
            _photoOverlay.OnOpen();
            SetState(MenuState.PhotoBook);
        }

        public void StartSequence(string name)
        {
            if (!_gameSequences.ContainsKey(name))
                return;

            _currentSequenceName = name;
            _gameSequences[_currentSequenceName].OnStart();
            SetState(MenuState.GameSequence);
        }

        public GameSequence GetCurrentGameSequence()
        {
            if (_currentSequenceName != null && _gameSequences.ContainsKey(_currentSequenceName))
                return _gameSequences[_currentSequenceName];

            return null;
        }

        public void ToggleInventoryMap()
        {
            _isChanging = true;
        }

        public bool UpdateCameraAndAnimation()
        {
            return (_currentMenuState != MenuState.Inventory && TextboxOverlay.IsOpen) || _currentMenuState == MenuState.GameSequence;
        }

        public void HideHud(bool hidden)
        {
            _hideHud = hidden;
        }

        private void UpdateFade()
        {
            // update the fading effect
            if (_fading)
            {
                _fadeCount += Game1.DeltaTime * _fadeDir;

                // finished closing/opening
                if (_fadeCount <= 0 || _fadeCount >= _fadeTime)
                {
                    _fading = false;
                    _fadeCount = MathHelper.Clamp((float)_fadeCount, 0, _fadeTime);
                }
            }

            var fadePercentage = (float)_fadeCount / _fadeTime;
            _fadeAnimationPercentage = (float)Math.Sin(Math.PI / 2 * fadePercentage);
            _blurRectangle.BackgroundColor = Color.Black * 0.5f * _fadeAnimationPercentage;
            _blurRectangle.BlurColor = Values.GameMenuBackgroundColor * _fadeAnimationPercentage;

            if (_fadeAnimationPercentage <= 0 && _currentSequenceName != null && _currentMenuState == MenuState.None)
                _currentSequenceName = null;

            // hide the hud
            if (TextboxOverlay.IsOpen || _currentMenuState != MenuState.None || _hideHud)
            {
                _hudState = AnimationHelper.MoveToTarget(_hudState, 1, 0.1f * Game1.TimeMultiplier);
            }
            else if (!Game1.GameManager.DialogIsRunning() && (Game1.UpdateGame || Game1.ForceDialogUpdate))
            {
                _hudState = AnimationHelper.MoveToTarget(_hudState, 0, 0.1f * Game1.TimeMultiplier);
            }

            _hudPercentage = (float)Math.Sin(Math.PI / 2 * _hudState);
        }

        private void ToggleState(MenuState newState)
        {
            if (_currentMenuState == MenuState.None)
                SetState(newState);
            else
                CloseOverlay();
        }

        private void SetState(MenuState newState)
        {
            // don't change the state if a textbox is open
            if (TextboxOverlay.IsOpen || DisableOverlayToggle)
                return;

            // pause the currently playing soundeffects
            if (newState == MenuState.Inventory || newState == MenuState.Menu)
                Game1.GameManager.PauseSoundEffects();

            if (newState == MenuState.Inventory || newState == MenuState.Menu)
                Game1.GameManager.PlaySoundEffect("D360-17-11");

            if (newState == MenuState.Inventory)
            {
                _isChanging = false;
                _changeCount = 0;
                _updateInventory = true;

                _mapOverlay.OnFocus();
                _dungeonOverlay.OnFocus();
            }
            else if (newState == MenuState.Menu)
            {
                // don't open the menu while closing it
                Game1.UiPageManager.ChangePage(typeof(GameMenuPage), null, PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);
            }

            _fading = true;
            _fadeDir = 1;
            _lastMenuState = _currentMenuState;
            _currentMenuState = newState;
        }

        public void CloseOverlay()
        {
            if (_currentMenuState == MenuState.Inventory || _currentMenuState == MenuState.Menu)
                Game1.GameManager.PlaySoundEffect("D360-18-12");

            _fading = true;
            _fadeDir = -1;
            _lastMenuState = _currentMenuState;
            _currentMenuState = MenuState.None;

            InputHandler.ResetInputState();
            Game1.UiPageManager.PopAllPages(PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);

            Game1.GameManager.ContinueSoundEffects();
        }

        public bool MenuIsOpen()
        {
            return _currentMenuState == MenuState.Menu;
        }
    }
}
