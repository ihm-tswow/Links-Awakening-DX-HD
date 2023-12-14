using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSwordSpawner : GameObject
    {
        private Animator _animator;
        private CSprite _sprite;
        private SpriteShader _spriteShader;

        private ObjItem _objSword;
        private BodyComponent _swordBody;
        private bool _swordHitFloor;

        private DictAtlasEntry _thunder;

        private float _counter;
        private int _thunderIndex;
        private bool _drawThunder = true;

        private float _animationCounter;
        private bool _animationStarted;

        private bool _spawnedSword;

        private float _thunderCounter;
        private bool _soundEffect;

        public ObjSwordSpawner() : base("sword_spawn") { }

        public ObjSwordSpawner(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/sword spawn");

            _thunder = Resources.GetSprite("sword_thunder_0");

            _sprite = new CSprite(EntityPosition);

            AddComponent(BaseAnimationComponent.Index, new AnimationComponent(_animator, _sprite, Vector2.Zero));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void SpawnSword()
        {
            _spawnedSword = true;
            _objSword = new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y, null, "sword2", "sword2", null);
            ((DrawComponent)_objSword.Components[DrawComponent.Index]).IsActive = false;
            _swordBody = ((BodyComponent)_objSword.Components[BodyComponent.Index]);
            _swordBody.OffsetY -= 1;
            _swordBody.Bounciness2D = 0.5f;
            Map.Objects.SpawnObject(_objSword);
        }

        private void Update()
        {
            _counter += Game1.DeltaTime;
            _animationCounter += Game1.DeltaTime;

            if (!_spawnedSword)
            {
                MapManager.ObjLink.FreezePlayer();
                Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;
            }

            if (_animationCounter > 2200 && !_soundEffect)
            {
                _soundEffect = true;
                Game1.GameManager.PlaySoundEffect("D370-30-1E");
            }

            if (_drawThunder)
            {
                _thunderCounter -= Game1.DeltaTime;
                if (_thunderCounter < 0)
                {
                    _thunderCounter += 66;
                    Game1.GameManager.PlaySoundEffect("D378-51-33");
                }
            }

            if (_animationCounter > 2750 && !_animationStarted)
            {
                _animationStarted = true;
                _animator.Play("idle");
            }

            if (_drawThunder && _animationCounter > 2750 + 4500)
            {
                _drawThunder = false;
                _thunderIndex = -1;
            }

            // finished playing?
            if (_animationCounter > 2750 && !_animator.IsPlaying && !_spawnedSword)
            {
                SpawnSword();
            }

            _spriteShader = _counter % (AiDamageState.BlinkTime * 2) >= AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;

            if (_counter > AiDamageState.BlinkTime * 2)
            {
                _counter -= AiDamageState.BlinkTime * 2;
                _thunderIndex += Game1.RandomNumber.Next(1, 4);
                _thunderIndex %= 4;
            }

            if (_objSword != null && !_swordHitFloor)
            {
                if (_swordBody.Velocity.Y < 0)
                {
                    _swordHitFloor = true;
                    ((DrawComponent)_objSword.Components[DrawComponent.Index]).IsActive = true;
                }
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_spriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _spriteShader);
            }

            // draw the blinking sword
            if (_objSword != null && !_swordHitFloor)
                _objSword.Draw(spriteBatch);

            if (_animator.IsPlaying)
                _sprite.Draw(spriteBatch);

            if (_drawThunder)
            {
                if (_thunderIndex == 0)
                {
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, _thunder.Origin, _thunder.Scale, SpriteEffects.None, 0);
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, Vector2.Zero, _thunder.Scale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
                }
                else if (_thunderIndex == 1)
                {
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X + 16, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, _thunder.Origin, _thunder.Scale, SpriteEffects.FlipVertically, 0);
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X - 16, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, Vector2.Zero, _thunder.Scale, SpriteEffects.FlipHorizontally, 0);
                }
                else if (_thunderIndex == 2)
                {
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X + 16, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, _thunder.Origin, _thunder.Scale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X - 16, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, Vector2.Zero, _thunder.Scale, SpriteEffects.None, 0);
                }
                else if (_thunderIndex == 3)
                {
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X + 32, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, _thunder.Origin, _thunder.Scale, SpriteEffects.FlipHorizontally, 0);
                    spriteBatch.Draw(_thunder.Texture, new Vector2(EntityPosition.X - 32, EntityPosition.Y), _thunder.ScaledRectangle, Color.White, 0, Vector2.Zero, _thunder.Scale, SpriteEffects.FlipVertically, 0);
                }
            }

            if (_spriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }
    }
}