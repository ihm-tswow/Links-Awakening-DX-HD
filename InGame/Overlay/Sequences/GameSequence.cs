using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    public class SeqDrawable
    {
        public SpriteShader Shader;
        public Color Color = Color.White;

        public Vector2 Position;

        // position transition
        public Vector2 PositionStart;
        public Vector2 PositionEnd;

        public float PositionTransitionTime;
        public float PositionTransitionCounter;

        // color transition
        public Color ColorStart;
        public Color ColorEnd;

        public float ColorTransitionTime;
        public float ColorTransitionCounter;

        public int Layer;

        public virtual void Draw(SpriteBatch spriteBatch) { }
    }

    public class SeqColor : SeqDrawable
    {
        public Rectangle Rect;

        public SeqColor(Rectangle rect, Color color, int layer)
        {
            Rect = rect;
            Color = color;
            Layer = layer;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprWhite, Rect, Color);
        }
    }

    public class SeqSprite : SeqDrawable
    {
        public DictAtlasEntry Sprite;
        public SpriteEffects SpriteEffect = SpriteEffects.None;
        public bool RoundPosition;

        public SeqSprite(string spriteId, Vector2 position, int layer)
        {
            Sprite = Resources.GetSprite(spriteId);
            Position = position;
            Layer = layer;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var drawPosition = Position;
            if (RoundPosition)
            {
                drawPosition.X = (int)drawPosition.X;
                drawPosition.Y = (int)drawPosition.Y;
            }

            // flip around the origin
            if ((SpriteEffect & SpriteEffects.FlipHorizontally) != 0)
                drawPosition.X += -Sprite.ScaledRectangle.Width + Sprite.Origin.X * Sprite.Scale;
            else
                drawPosition.X -= Sprite.Origin.X * Sprite.Scale;

            if ((SpriteEffect & SpriteEffects.FlipVertically) != 0)
                drawPosition.Y += -Sprite.ScaledRectangle.Height + Sprite.Origin.Y * Sprite.Scale;
            else
                drawPosition.Y -= Sprite.Origin.Y * Sprite.Scale;

            spriteBatch.Draw(Sprite.Texture, drawPosition, Sprite.ScaledRectangle,
                Color, 0, Vector2.Zero, new Vector2(Sprite.Scale), SpriteEffect, 0);
        }
    }

    public class SeqAnimation : SeqDrawable
    {
        public Animator Animator;
        public bool RoundPosition;

        public SeqAnimation(string animatorId, string animationId, Vector2 position, int layer)
        {
            Animator = AnimatorSaveLoad.LoadAnimator(animatorId);
            Animator.Play(animationId);

            Position = position;
            Layer = layer;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var drawPosition = Position;
            if (RoundPosition)
            {
                drawPosition.X = (int)drawPosition.X;
                drawPosition.Y = (int)drawPosition.Y;
            }

            Animator.DrawBasic(spriteBatch, drawPosition, Color);
        }
    }

    public class GameSequence
    {
        protected RenderTarget2D _renderTarget;

        protected List<SeqDrawable> Sprites = new List<SeqDrawable>();
        protected Dictionary<string, SeqDrawable> SpriteDict = new Dictionary<string, SeqDrawable>();

        protected Vector2 _cameraPosition;

        protected int _sequenceWidth = 160;
        protected int _sequenceHeight = 144;

        protected double _sequenceCounter;

        protected int _scale;
        protected bool _useUiScale = true;
        protected bool _textBoxOffset = true;

        public virtual void OnStart()
        {
            _sequenceCounter = 0;
        }

        public void AddDrawable(string key, SeqDrawable drawable)
        {
            Sprites.Add(drawable);
            SpriteDict.TryAdd(key, drawable);
        }

        public void PlayAnimation(string key, string animationId)
        {
            SpriteDict.TryGetValue(key, out SeqDrawable drawable);

            if (drawable == null)
                return;

            var animator = (SeqAnimation)drawable;
            if (animator != null)
                animator.Animator.Play(animationId);
        }

        /// <summary>
        /// Finish the animation that is currently playing; This will not directly stop the animation but continue it till the end and even start the next animation that is set
        /// </summary>
        /// <param name="key"></param>
        public void FinishAnimation(string key, int stopFrameIndex)
        {
            SpriteDict.TryGetValue(key, out SeqDrawable drawable);

            if (drawable == null)
                return;

            var animator = (SeqAnimation)drawable;
            if (animator != null)
                animator.Animator.FinishAnimation(stopFrameIndex);
        }

        public void SetPosition(string key, Vector2 newPosition)
        {
            SpriteDict.TryGetValue(key, out SeqDrawable drawable);

            if (drawable != null)
                drawable.Position = newPosition;
        }

        public void StartPositionTransition(string drawableId, Vector2 positionOffset, float speed)
        {
            SpriteDict.TryGetValue(drawableId, out SeqDrawable drawable);

            if (drawable != null)
            {
                drawable.PositionStart = drawable.Position;
                drawable.PositionEnd = drawable.Position + positionOffset;
                drawable.PositionTransitionCounter = 0;

                // calculate the time needed to finishe the transition
                var length = drawable.PositionStart - drawable.PositionEnd;
                drawable.PositionTransitionTime = ((length.Length() / speed) / 60) * 1000;

                Game1.GameManager.SaveManager.SetString(drawableId + "Moving", "1");
            }
        }

        public void StartColorTransition(string drawableId, Color targetColor, int time)
        {
            SpriteDict.TryGetValue(drawableId, out SeqDrawable drawable);

            if (drawable != null)
            {
                if (time == 0)
                {
                    drawable.Color = targetColor;
                    return;
                }

                drawable.ColorStart = drawable.Color;
                drawable.ColorEnd = targetColor;
                drawable.ColorTransitionTime = time;
                drawable.ColorTransitionCounter = 0;
            }
        }

        public virtual void Update()
        {
            _sequenceCounter += Game1.DeltaTime;

            for (var i = 0; i < Sprites.Count; i++)
            {
                if (Sprites[i] is SeqAnimation animator)
                    animator.Animator.Update();

            }

            foreach (var spriteEntry in SpriteDict)
            {
                var sprite = spriteEntry.Value;

                // update position transition
                if (sprite.PositionTransitionTime != 0)
                {
                    sprite.PositionTransitionCounter += Game1.DeltaTime;
                    if (sprite.PositionTransitionCounter > sprite.PositionTransitionTime)
                    {
                        sprite.PositionTransitionTime = 0;
                        sprite.Position = sprite.PositionEnd;
                        Game1.GameManager.SaveManager.SetString(spriteEntry.Key + "Moving", "0");
                        continue;
                    }

                    var percentage = sprite.PositionTransitionCounter / sprite.PositionTransitionTime;
                    sprite.Position = Vector2.Lerp(sprite.PositionStart, sprite.PositionEnd, percentage);
                }

                // update color transition
                if (sprite.ColorTransitionTime != 0)
                {
                    sprite.ColorTransitionCounter += Game1.DeltaTime;
                    if (sprite.ColorTransitionCounter > sprite.ColorTransitionTime)
                    {
                        sprite.ColorTransitionTime = 0;
                        sprite.Color = sprite.ColorEnd;
                        continue;
                    }

                    var percentage = sprite.ColorTransitionCounter / sprite.ColorTransitionTime;
                    sprite.Color = Color.Lerp(sprite.ColorStart, sprite.ColorEnd, percentage);
                }
            }
        }

        private void UpdateRenderTarget()
        {
            if (_renderTarget != null &&
                _sequenceWidth * _scale == _renderTarget.Width && _sequenceHeight * _scale == _renderTarget.Height)
                return;

            _renderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, _sequenceWidth * _scale, _sequenceHeight * _scale);
        }

        public virtual void DrawRT(SpriteBatch spriteBatch)
        {
            if (_useUiScale)
                _scale = Game1.UiScale;

            UpdateRenderTarget();

            // sort the sprites
            Sprites.Sort((sprite0, sprite1) => sprite0.Layer - sprite1.Layer);

            // round the camera position to align with pixels
            var matrix =
                Matrix.CreateTranslation(new Vector3(
                    MathF.Round((-_cameraPosition.X) * _scale) / _scale,
                    MathF.Round((-_cameraPosition.Y) * _scale) / _scale, 0)) *
                Matrix.CreateScale(_scale);

            spriteBatch.GraphicsDevice.SetRenderTarget(_renderTarget);
            spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, matrix);

            for (var i = 0; i < Sprites.Count; i++)
            {
                // change the draw effect
                if (Sprites[i].Shader != null)
                {
                    spriteBatch.End();

                    ObjectManager.SetSpriteShader(Sprites[i].Shader);
                    spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, Sprites[i].Shader.Effect, matrix);
                }

                Sprites[i].Draw(spriteBatch);

                // change the draw effect
                // this would not be very efficient if a lot of sprite used effects
                if (Sprites[i].Shader != null)
                {
                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, matrix);
                }
            }

            DrawScaled(spriteBatch);

            spriteBatch.End();
            spriteBatch.GraphicsDevice.SetRenderTarget(null);
        }

        public virtual void Draw(SpriteBatch spriteBatch, float transparency)
        {
            var position = new Vector2(
                Game1.WindowWidth / 2 - _renderTarget.Width / 2 + MapManager.Camera.ShakeOffsetX * _scale,
                Game1.WindowHeight / 2 - _renderTarget.Height / 2 + MapManager.Camera.ShakeOffsetY * _scale);

            // push the sequence rt up if the textbox would overlap
            if (_textBoxOffset)
                position.Y = Math.Min(position.Y, Game1.GameManager.InGameOverlay.TextboxOverlay.DialogBoxTextBox.Y - _renderTarget.Height - 12 * _scale);

            spriteBatch.Draw(_renderTarget, position, Color.White * transparency);
        }

        public virtual void DrawScaled(SpriteBatch spriteBatch) { }
    }
}
