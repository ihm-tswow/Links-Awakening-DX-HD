using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class FinalSequence : GameSequence
    {
        private int _screenIndex;

        private float _screenFadeCounter;
        private float _screenFadeWaitCounter;
        private float ScreenFadeTime = 250;
        private float ScreenFadeTimeWait = 500;

        // screen 0 (island)
        private float _screen0Counter;
        private bool _playedIslandSound;

        // screen 1
        private SeqAnimation _s1Seagull0;
        private SeqAnimation _s1Seagull1;

        private float _screen1Counter;
        private float _fadeCounter;
        private float _segullSoundCounter;
        private float _waveSoundCounter = 5000;
        private const int FadeTime = 1600;

        private SeqColor _screen1FadeColor;

        // screen 2
        private SeqAnimation _s2Link;
        private SeqSprite _s2Log0;
        private SeqSprite _s2Log1;

        private float _s2LinkPosY;
        private float _s2Log0PosY;
        private float _s2Log1PosY;

        private float _screen2Counter;

        // screen 3
        private SeqAnimation _s3Link;
        private SeqAnimation _s3Shadow;
        private SeqAnimation _s3Seagull;

        private SeqSprite _s3Log;
        private SeqSprite _s3Barrel0;
        private SeqSprite _s3Barrel1;

        private float _screen3Counter;

        private float _s3LogPosY;
        private float _s3Barrel0PosY;
        private float _s3Barrel1PosY;

        private float _s3LinkPosY;

        private bool _s3LinkAwoken;

        // screen 4
        private SeqAnimation _s4Link;

        private SeqAnimation[] _s4Water = new SeqAnimation[24];
        private SeqSprite _s4Background;
        private SeqSprite _s4Wale;

        private SeqSprite _s4Sun0;
        private SeqSprite _s4Sun1;
        private SeqSprite _s4Sun2;

        private float _screen4Counter;
        private float _s4CameraPosition;
        private float _s4LinkPosY;
        private float _s4Brightness;

        private bool _playedWaleSound0;
        private bool _playedWaleSound1;
        private bool _s4MoveCamera;
        private bool _s4LookedUp;

        // screen 5
        private SeqAnimation _s5Link;
        private SeqSprite _s5Background;

        private float _screen5Counter;
        private bool _screen5Smile;

        // screen 6
        private SeqAnimation _s6Link;
        private SeqAnimation _s6Wale;

        private SeqSprite _s6Marin;
        private float _s6MarinTransparency;

        private float _screen6Counter;

        private float _s6LinkPosition;
        private float _s6CameraPosition;

        private float _creditCounter;

        private int _creditsHeaderIndex;
        private int _creditsContentIndex;

        private bool _finishedCredits;
        private bool _marinEnding;
        private bool _marinEndingTriggered;

        private string _creditsHeader;
        private string _creditsContent;

        public FinalSequence()
        {
            _useUiScale = false;
            _textBoxOffset = false;

            // 16/9
            _sequenceWidth = 360;
            _sequenceHeight = 180;
        }

        public override void OnStart()
        {
            var finalState = Game1.GameManager.SaveManager.GetString("final_state");

            if (!string.IsNullOrEmpty(finalState) && finalState == "1")
                InitScreen1();
            else
                InitScreen0();

            base.OnStart();
        }

        private void InitScreen0()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _screenIndex = 0;
            _screen0Counter = 0;
            _playedIslandSound = false;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 + 144 / 2 - 128);

            // background
            Sprites.Add(new SeqColor(new Rectangle(0, 0, _sequenceWidth, (int)position.Y + 80), new Color(122, 156, 253), 1));
            Sprites.Add(new SeqColor(new Rectangle(0, (int)position.Y + 80, _sequenceWidth, _sequenceHeight - (int)position.Y - 80), new Color(24, 33, 154), 1));

            Sprites.Add(new SeqSprite("final_island_background", position, 2));
            Sprites.Add(new SeqSprite("final_island", new Vector2(position.X + 8, position.Y + 16), 3) { Shader = Resources.ThanosSpriteShader1 });

            for (int i = 0; i < 8; i++)
            {
                Sprites.Add(new SeqAnimation("Sequences/water", "idle", new Vector2(position.X - 16 * i, position.Y + 80), 3));
                Sprites.Add(new SeqAnimation("Sequences/water", "idle", new Vector2(position.X + 144 + 16 * i, position.Y + 80), 3));
            }
        }

        private void InitScreen1()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _screenIndex = 1;
            _screen1Counter = 0;

            var position = Vector2.Zero;
            var screenPosition = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2);

            // background
            Sprites.Add(new SeqSprite("final_sky", position, 0));

            Sprites.Add(_screen1FadeColor = new SeqColor(new Rectangle(0, 0, _sequenceWidth, _sequenceHeight), Color.White, 2));

            Sprites.Add(_s1Seagull0 = new SeqAnimation("Sequences/seagull final", "fly_1", new Vector2(screenPosition.X + 16, screenPosition.Y + 8), 1));
            Sprites.Add(_s1Seagull1 = new SeqAnimation("Sequences/seagull final", "fly_-1", new Vector2(screenPosition.X + 128, screenPosition.Y + 104), 1));

            _s1Seagull0.Animator.SetFrame(Game1.RandomNumber.Next(1, 4));
            _s1Seagull0.Animator.SetTime(Game1.RandomNumber.Next(0, 500));
            _s1Seagull1.Animator.SetTime(Game1.RandomNumber.Next(0, 500));

            _fadeCounter = FadeTime;
        }

        private void InitScreen2()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _screenIndex = 2;
            _screen2Counter = 0;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2 + 8);

            // background
            Sprites.Add(new SeqColor(new Rectangle(0, 0, _sequenceWidth, (int)position.Y + 56), new Color(66, 89, 254), 0));
            Sprites.Add(new SeqColor(new Rectangle(0, (int)position.Y + 56, _sequenceWidth, _sequenceHeight - (int)position.Y - 56), new Color(24, 33, 154), 0));

            // background
            Sprites.Add(new SeqSprite("final_lay", new Vector2(0, position.Y), 1));

            for (int i = 0; i < 24; i++)
                Sprites.Add(new SeqAnimation("Sequences/water", "idle", new Vector2(position.X + 16 * (i - 7), position.Y + 48), 2));

            _s2LinkPosY = position.Y + 82;
            Sprites.Add(_s2Link = new SeqAnimation("Sequences/link final", "idle", new Vector2(position.X + 64, _s2LinkPosY), 1));

            _s2Log0PosY = position.Y + 90;
            Sprites.Add(_s2Log0 = new SeqSprite("final_log", new Vector2(position.X + 32, _s2Log0PosY), 1));
            _s2Log1PosY = position.Y + 80;
            Sprites.Add(_s2Log1 = new SeqSprite("final_log", new Vector2(position.X + 112, _s2Log1PosY), 1));
        }

        private void InitScreen3()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            Game1.GameManager.SetMusic(60, 2);

            _screenIndex = 3;
            _screen3Counter = 0;
            _s3LinkAwoken = false;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2);

            // background
            Sprites.Add(new SeqColor(new Rectangle(0, 0, _sequenceWidth, (int)position.Y + 56), new Color(66, 89, 254), 0));
            Sprites.Add(new SeqColor(new Rectangle(0, (int)position.Y + 56, _sequenceWidth, _sequenceHeight - (int)position.Y - 56), new Color(24, 33, 154), 0));

            // background
            Sprites.Add(new SeqSprite("final_sky_1", new Vector2(0, position.Y), 1));

            for (int i = 0; i < 24; i++)
                Sprites.Add(new SeqAnimation("Sequences/water", "idle", new Vector2(position.X + 16 * (i - 7), position.Y + 48), 1));

            _s3LinkPosY = position.Y + 130;
            Sprites.Add(_s3Shadow = new SeqAnimation("Sequences/link awake", "shadow", new Vector2(position.X + 16, _s3LinkPosY), 2));
            Sprites.Add(_s3Link = new SeqAnimation("Sequences/link awake", "sleep", new Vector2(position.X + 16, _s3LinkPosY), 2));

            Sprites.Add(_s3Seagull = new SeqAnimation("Sequences/seagull big final", "idle", new Vector2(position.X + 16, position.Y + 72), 2));

            _s3LogPosY = position.Y + 72;
            Sprites.Add(_s3Log = new SeqSprite("final_log_2", new Vector2(position.X + 16, _s3LogPosY), 1));
            _s3Barrel0PosY = position.Y + 70;
            Sprites.Add(_s3Barrel0 = new SeqSprite("final_barrel", new Vector2(position.X + 116, _s3Barrel0PosY), 1) { SpriteEffect = SpriteEffects.FlipHorizontally });
            _s3Barrel1PosY = position.Y + 78;
            Sprites.Add(_s3Barrel1 = new SeqSprite("final_barrel", new Vector2(position.X + 148, _s3Barrel1PosY), 1));
        }

        private void InitScreen4()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _screenIndex = 4;
            _screen4Counter = 0;
            _s4CameraPosition = 720;
            _s4Brightness = 255;
            _s4MoveCamera = false;
            _s4LookedUp = false;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2);

            // background
            Sprites.Add(_s4Background = new SeqSprite("final_sky", Vector2.Zero, 0));

            var sunPosition = new Vector2(position.X + 64, position.Y + 48);
            Sprites.Add(new SeqSprite("sun", sunPosition, 1));
            Sprites.Add(_s4Sun0 = new SeqSprite("sun_0", new Vector2(sunPosition.X + 29, sunPosition.Y + 29), 1) { Color = Color.Transparent });
            Sprites.Add(_s4Sun1 = new SeqSprite("sun_1", new Vector2(sunPosition.X + 34, sunPosition.Y + 34), 2) { Color = Color.Transparent });
            Sprites.Add(_s4Sun2 = new SeqSprite("sun_2", new Vector2(sunPosition.X + 38, sunPosition.Y + 38), 3) { Color = Color.Transparent });

            Sprites.Add(_s4Link = new SeqAnimation("Sequences/link awake", "sit", new Vector2(position.X + 56, _s4LinkPosY = position.Y + 16 * 49 + 4), 2));

            Sprites.Add(_s4Wale = new SeqSprite("wale_sky", new Vector2(position.X + 80, position.Y + 56), 2) { RoundPosition = true });

            // animated water
            for (int i = 0; i < _s4Water.Length; i++)
                Sprites.Add(_s4Water[i] = new SeqAnimation("Sequences/water", "idle", new Vector2(position.X + 16 * (i - 7), position.Y + 16 * 50), 1));
        }

        private void InitScreen5()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _screenIndex = 5;

            _screen5Counter = 0;
            _screen5Smile = false;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2);

            Sprites.Add(_s5Link = new SeqAnimation("Sequences/link awake", "pre_smile", new Vector2(position.X + 48, position.Y + 64 + 18), 2));
            var brighness = 100;
            _s5Link.Color = new Color(brighness, brighness, brighness);

            // background
            Sprites.Add(_s5Background = new SeqSprite("final_sky_2", Vector2.Zero, 0));
        }

        private void InitScreen6()
        {
            Sprites.Clear();
            SpriteDict.Clear();
            _cameraPosition = Vector2.Zero;

            _marinEndingTriggered = false;
            _s6MarinTransparency = 0;
            _marinEnding = Game1.GameManager.DeathCount == 0;

            _finishedCredits = false;
            _creditCounter = -2500;
            _creditsHeaderIndex = -1;
            _creditsContentIndex = 0;

            _screenIndex = 6;
            _s6CameraPosition = 144;

            _screen6Counter = 0;

            _creditsHeader = null;
            _creditsContent = null;

            var position = new Vector2(_sequenceWidth / 2 - 80, _sequenceHeight / 2 - 144 / 2);

            // background
            Sprites.Add(new SeqSprite("final_s6", Vector2.Zero, 0));
            Sprites.Add(_s6Marin = new SeqSprite("final_marin_ending", new Vector2(155, 58), 0) { Color = Color.Transparent });

            for (int i = 0; i < 24; i++)
                Sprites.Add(new SeqAnimation("Sequences/water", "idle", new Vector2(position.X + 16 * (i - 7), position.Y + 16 * 15), 1));

            _s6LinkPosition = position.Y + 16 * 15 + 7;
            Sprites.Add(_s6Link = new SeqAnimation("Sequences/link final", "sitting", new Vector2(position.X + 64, _s6LinkPosition), 2));
            Sprites.Add(_s6Wale = new SeqAnimation("Sequences/wale", "idle", new Vector2(32, 80), 2) { RoundPosition = true });
        }

        public override void Update()
        {
            _scale = MathHelper.Min(Game1.WindowWidth / 160, Game1.WindowHeight / 144);

            // fade out and wait in the middle
            if (_screenFadeCounter > ScreenFadeTime)
            {
                _screenFadeCounter -= Game1.DeltaTime;
                if (_screenFadeCounter <= ScreenFadeTime)
                {
                    _screenFadeCounter = ScreenFadeTime;
                    _screenFadeWaitCounter = ScreenFadeTimeWait;
                }
            }
            else if (_screenFadeCounter == ScreenFadeTime)
            {
                _screenFadeWaitCounter -= Game1.DeltaTime;
                if (_screenFadeWaitCounter <= 0)
                    _screenFadeCounter -= Game1.DeltaTime;
            }
            else if (_screenFadeCounter > 0)
                _screenFadeCounter -= Game1.DeltaTime;
            else
                _screenFadeCounter = 0;

            if (_screenIndex == 0)
                UpdateScreen0();
            else if (_screenIndex == 1)
                UpdateScreen1();
            else if (_screenIndex == 2)
                UpdateScreen2();
            else if (_screenIndex == 3)
                UpdateScreen3();
            else if (_screenIndex == 4)
                UpdateScreen4();
            else if (_screenIndex == 5)
                UpdateScreen5();
            else if (_screenIndex == 6)
                UpdateScreen6();

            base.Update();
        }

        private void UpdateScreen0()
        {
            // show the next screen
            _screen0Counter += Game1.DeltaTime;

            if (_screen0Counter > 3000 && !_playedIslandSound)
            {
                _playedIslandSound = true;
                Game1.GameManager.PlaySoundEffect("D378-53-35");
            }

            var percentage = MathHelper.Clamp((float)(_screen0Counter - 3000) / 3000, 0, 1);
            // start slow and speed up
            var sinPercentage = 1 - MathF.Sin(-MathF.PI * 0.45f + percentage * MathF.PI * 0.45f) / MathF.Sin(-MathF.PI * 0.45f);
            Resources.ThanosSpriteShader1.FloatParameter["Percentage"] = sinPercentage;

            // start fade out
            if (_screen0Counter > 8000 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            if (_screen0Counter > 8000)
            {
                Game1.GameManager.SaveManager.SetString("final_state", "1");
                Game1.GameManager.SaveManager.SetString("activate_fountain", "1");
                Game1.GameManager.InGameOverlay.CloseOverlay();

                Game1.GameManager.StopMusic();

                MapManager.ObjLink.InitEnding();
            }
        }

        private void UpdateSeaSounds()
        {
            // seagull sound
            _segullSoundCounter -= Game1.DeltaTime;
            if (_segullSoundCounter < 0)
            {
                _segullSoundCounter += Game1.RandomNumber.Next(1500, 2500);
                Game1.GameManager.PlaySoundEffect("D360-33-21");
            }

            _waveSoundCounter -= Game1.DeltaTime;
            if (_waveSoundCounter < 0)
            {
                _waveSoundCounter += 3500;
                Game1.GameManager.PlaySoundEffect("D378-15-0F");
            }
        }

        private void UpdateScreen1()
        {
            UpdateSeaSounds();

            _screen1Counter += Game1.DeltaTime;

            // start fade out
            if (_screen1Counter > 10000 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            // show the next screen
            if (_screen1Counter > 10000 && _screenFadeCounter <= ScreenFadeTime)
            {
                InitScreen2();
                return;
            }

            _fadeCounter -= Game1.DeltaTime;
            if (_fadeCounter < 0)
                _fadeCounter = 0;

            float percentage = MathF.Sin(_fadeCounter / FadeTime);
            _screen1FadeColor.Color = Color.White * percentage;

            _s1Seagull0.Position.X += 1 / 5.5f * Game1.TimeMultiplier;
            _s1Seagull1.Position.X -= 1 / 5.5f * Game1.TimeMultiplier;
        }

        private void UpdateScreen2()
        {
            UpdateSeaSounds();

            _screen2Counter += Game1.DeltaTime;

            // start fade out
            if (_screen2Counter > 10000 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            // show the next screen
            if (_screen2Counter > 10000 && _screenFadeCounter <= ScreenFadeTime)
            {
                InitScreen3();
                return;
            }

            _s2Link.Position.Y = _s2LinkPosY + MathF.Round(MathF.Sin((float)_screen2Counter / 2000 * MathF.PI * 2));
            _s2Log0.Position.Y = _s2Log0PosY + MathF.Round(0.5f + MathF.Sin((float)(_screen2Counter - 450) / 2000 * MathF.PI * 2) * 0.5f);
            _s2Log1.Position.Y = _s2Log1PosY + MathF.Round(0.5f + MathF.Sin((float)(_screen2Counter + 350) / 2000 * MathF.PI * 2) * 0.5f);
        }

        private void UpdateScreen3()
        {
            UpdateSeaSounds();

            _screen3Counter += Game1.DeltaTime;

            if (_screen3Counter > 5000 && !_s3LinkAwoken)
            {
                _s3LinkAwoken = true;
                _s3Link.Animator.Play("awake");
            }

            // start fade out
            if (_screen3Counter > 17500 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            // show the next screen
            if (_screen3Counter > 17500 && _screenFadeCounter <= ScreenFadeTime)
            {
                InitScreen4();
            }

            _s3Shadow.Position.Y = _s3LinkPosY + MathF.Round(MathF.Sin((float)_screen3Counter / 3300 * MathF.PI * 2) * 6);
            _s3Link.Position.Y = _s3LinkPosY + MathF.Round(MathF.Sin((float)_screen3Counter / 3300 * MathF.PI * 2) * 6);

            _s3Log.Position.Y = _s3LogPosY + MathF.Round(0.5f + MathF.Sin((float)(_screen3Counter + 250) / 3300 * MathF.PI * 2) * 1.5f);
            _s3Barrel0.Position.Y = _s3Barrel0PosY + MathF.Round(MathF.Sin((float)(_screen3Counter + 750 + 0) / 3300 * MathF.PI * 2) * 2);
            _s3Barrel1.Position.Y = _s3Barrel1PosY + MathF.Round(MathF.Sin((float)(_screen3Counter + 750 + 200) / 3300 * MathF.PI * 2) * 2);

            _s3Seagull.Position.X += 1 / 2f * Game1.TimeMultiplier;
            _s3Seagull.Position.Y -= 1 / 2.75f * Game1.TimeMultiplier;
        }

        private void UpdateScreen4()
        {
            _screen4Counter += Game1.DeltaTime;

            // start fade out
            if (_screen4Counter > 25500 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            // show the next screen
            if (_screen4Counter > 25500 && _screenFadeCounter <= ScreenFadeTime)
                InitScreen5();

            // shadow
            if (!_s4MoveCamera && _screen4Counter > 7500)
                _s4Brightness = AnimationHelper.MoveToTarget(_s4Brightness, 155, 6f * Game1.TimeMultiplier);

            // move the camera up
            if (_screen4Counter > 10500)
                _s4MoveCamera = true;
            if (_s4MoveCamera)
            {
                var cameraPercentage = 0.5f + MathF.Sin(-MathF.PI / 2 + MathHelper.Clamp((float)(_screen4Counter - 10500) / 4500, 0, 1) * MathF.PI) / 2;
                _s4CameraPosition = MathHelper.Lerp(720f, 0, cameraPercentage);
            }

            _cameraPosition = new Vector2(0, _s4CameraPosition);

            // move the wale up
            if (_s4CameraPosition < 144)
                _s4Wale.Position.Y -= 1 / 8f * Game1.TimeMultiplier;

            if (!_s4LookedUp && _screen4Counter > 8800)
            {
                _s4LookedUp = true;
                _s4Link.Animator.Play("look_up");
            }

            if (!_playedWaleSound0 && _s4Wale.Position.Y < 62)
            {
                _playedWaleSound0 = true;
                Game1.GameManager.PlaySoundEffect("D370-23-17");
            }
            if (!_playedWaleSound1 && _s4Wale.Position.Y < 32)
            {
                _playedWaleSound1 = true;
                Game1.GameManager.PlaySoundEffect("D370-23-17");
            }

            if (_s4Wale.Position.Y < 24)
            {
                _s4Sun0.Color = Color.White;
                // wale blocks the sunlight
                _s4Brightness = AnimationHelper.MoveToTarget(_s4Brightness, 255, 2f * Game1.TimeMultiplier);
            }
            if (_s4Wale.Position.Y < 20 - 5)
                _s4Sun1.Color = Color.White;
            if (_s4Wale.Position.Y < 20 - 10)
                _s4Sun2.Color = Color.White;

            _s4Link.Position.Y = _s4LinkPosY + MathF.Round(MathF.Sin((float)_screen4Counter / 3300 * MathF.PI * 2) * 4f);

            var colorBrightness = new Color((int)_s4Brightness, (int)_s4Brightness, (int)_s4Brightness);
            _s4Link.Color = colorBrightness;
            _s4Background.Color = colorBrightness;
            for (int i = 0; i < _s4Water.Length; i++)
                _s4Water[i].Color = colorBrightness;
        }

        private void UpdateScreen5()
        {
            _screen5Counter += Game1.DeltaTime;
            if (_screen5Counter > 3500 && !_screen5Smile)
            {
                _screen5Smile = true;
                _s5Link.Animator.Play("smile");
            }

            // start fade out
            if (_screen5Counter > 14000 - ScreenFadeTime && _screenFadeCounter == 0)
                _screenFadeCounter = ScreenFadeTime * 2;

            // show the next screen
            if (_screen5Counter > 14000 && _screenFadeCounter <= ScreenFadeTime)
            {
                InitScreen6();
            }

            // brighten up the sprite
            var brighness = 155 + (int)(100 * MathHelper.Clamp((_screen5Counter - 750) / 500, 0, 1));
            _s5Link.Color = new Color(brighness, brighness, brighness);

            // offset the position of the link animation to not cut off the sprite
            var posY = (int)MathF.Ceiling((Game1.WindowHeight - _sequenceHeight * _scale) / 2) / _scale;
            if (posY > 0)
                posY = 0;

            _s5Link.Position.Y = _sequenceHeight + posY + MathF.Round(2 + MathF.Sin(_screen5Counter / 3000 * MathF.PI * 2) * 2);
            _s5Background.Position.Y = posY;
        }

        private void UpdateScreen6()
        {
            if (_finishedCredits)
            {
                if (_marinEndingTriggered)
                {
                    _screen6Counter += Game1.DeltaTime;

                    if (_screen6Counter > 13000)
                    {
                        // hide marin
                        _s6MarinTransparency = AnimationHelper.MoveToTarget(_s6MarinTransparency, 0, 0.025f * Game1.TimeMultiplier);

                        var musicVolume = Math.Clamp(((_s6MarinTransparency - 13000) / 1000), 0, 1);
                        Game1.GbsPlayer.SetVolumeMultiplier(musicVolume);
                    }
                    // fade the singing out
                    else
                    {
                        // show marin
                        _s6MarinTransparency = AnimationHelper.MoveToTarget(_s6MarinTransparency, 1, 0.025f * Game1.TimeMultiplier);

                        Game1.GbsPlayer.SetVolumeMultiplier(0);
                    }

                    _s6Marin.Color = Color.White * _s6MarinTransparency;

                    if (_screen6Counter > 16000)
                        ExitToIntro();
                }

                // return to the intro screen
                if (ControlHandler.ButtonPressed(CButtons.Start) ||
                    ControlHandler.ButtonPressed(CButtons.A))
                {
                    if (_marinEnding)
                    {
                        _creditsHeader = null;
                        _creditsContent = null;
                        _marinEndingTriggered = true;

                        Game1.GameManager.SetMusic(46, 2);
                    }
                    else
                    {
                        ExitToIntro();
                    }
                }

                return;
            }

            _screen6Counter += Game1.DeltaTime;

            // move the camera up
            if (_screen6Counter > 72500)
            {
                _s6CameraPosition -= 1 / 8f * Game1.TimeMultiplier;
                if (_s6CameraPosition < 0)
                    _s6CameraPosition = 0;
            }
            _cameraPosition = new Vector2(0, _s6CameraPosition);

            // link move up and down
            _s6Link.Position.Y = _s6LinkPosition + MathF.Round(MathF.Sin(_screen6Counter / 3000 * MathF.PI * 2) * 2);

            if (_s6CameraPosition < 128)
            {
                _s6Wale.Position.X += 1 / 8f * Game1.TimeMultiplier;
                _s6Wale.Position.Y -= 1 / 4f / 8f * Game1.TimeMultiplier;
            }

            // credits
            {
                _creditCounter += Game1.DeltaTime;
                if (_creditCounter > 3450)
                {
                    _creditCounter -= 3450;
                    if (!NextCredits())
                    {
                        _screen6Counter = 0;
                        _finishedCredits = true;
                    }
                }
            }
        }

        private void ExitToIntro()
        {
            Game1.ScreenManager.ChangeScreen(Values.ScreenNameIntro);
            Game1.GameManager.InGameOverlay.CloseOverlay();
        }

        private bool NextCredits()
        {
            _creditsContentIndex++;
            var newContent = Game1.LanguageManager.GetString("credits_" + _creditsHeaderIndex + "_" + _creditsContentIndex, null);

            if (newContent == null)
            {
                _creditsHeaderIndex++;
                var newHeader = Game1.LanguageManager.GetString("credits_" + _creditsHeaderIndex, null);

                if (newHeader != null)
                {
                    _creditsHeader = newHeader;

                    _creditsContentIndex = 0;
                    _creditsContent = Game1.LanguageManager.GetString("credits_" + _creditsHeaderIndex + "_" + _creditsContentIndex, null);
                }
                // finished credits?
                else
                    return false;
            }
            else
            {
                _creditsContent = newContent;
            }

            return true;
        }

        public override void Draw(SpriteBatch spriteBatch, float transparency)
        {
            base.Draw(spriteBatch, transparency);

            if (_screenIndex == 6)
                DrawCredits(spriteBatch);

            DrawSidebars(spriteBatch);

            if (_screenFadeCounter > 0)
            {
                var percentage = MathF.Sin((_screenFadeCounter / (ScreenFadeTime * 2)) * MathF.PI);
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.WindowWidth, Game1.WindowHeight), Color.White * percentage);
            }
        }

        private void DrawCredits(SpriteBatch spriteBatch)
        {
            var creditOffset = new Vector3(
                MathF.Round((-_sequenceWidth / 2 - _cameraPosition.X) * _scale) / _scale,
                MathF.Round((-_sequenceHeight / 2 - _cameraPosition.Y) * _scale) / _scale, 0);

            // round the camera position to align with pixels
            var matrix =
                Matrix.CreateTranslation(creditOffset) *
                Matrix.CreateScale(_scale) *
                Matrix.CreateTranslation(new Vector3((int)(Game1.WindowWidth * 0.5f), (int)(Game1.WindowHeight * 0.5f), 0)) *
                Game1.GetMatrix;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, matrix);

            if (_creditsHeader != null)
            {
                // draw the header centered
                var textSize = Resources.FontCreditsHeader.MeasureString(_creditsHeader);
                var position = new Vector2(_sequenceWidth / 2 - textSize.X / 2, _cameraPosition.Y + (18 + 48));
                spriteBatch.DrawString(Resources.FontCreditsHeader, _creditsHeader, position, Color.White);
            }

            if (_creditsContent != null)
            {
                // draw the content centered
                var textSize = Resources.FontCreditsHeader.MeasureString(_creditsContent);
                var position = new Vector2(_sequenceWidth / 2 - textSize.X / 2, _cameraPosition.Y + (18 + 70));
                spriteBatch.DrawString(Resources.FontCredits, _creditsContent, position, Color.White);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);
        }

        private void DrawSidebars(SpriteBatch spriteBatch)
        {
            var width = (int)MathF.Ceiling((Game1.WindowWidth - _sequenceWidth * _scale) / 2);
            if (width > 0)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, width, Game1.WindowHeight), Color.Black);
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(Game1.WindowWidth / 2 + _sequenceWidth / 2 * _scale, 0, width + 1, Game1.WindowHeight), Color.Black);
            }

            var height = (int)MathF.Ceiling((Game1.WindowHeight - _sequenceHeight * _scale) / 2);
            if (height > 0)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.WindowWidth, height), Color.Black);
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, Game1.WindowHeight / 2 + _sequenceHeight / 2 * _scale, Game1.WindowWidth, height + 1), Color.Black);
            }
        }
    }
}
