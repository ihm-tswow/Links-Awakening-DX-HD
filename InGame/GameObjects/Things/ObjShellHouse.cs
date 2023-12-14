using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjShellHouse : GameObject
    {
        private readonly DictAtlasEntry _barSprite;
        private readonly Animator _barAnimator;

        private bool _triggerEntryDialog;
        private bool _triggerDialog;

        private float _barHeight = 16;
        private int _shellCount;
        private int _targetHeight;
        private bool _fillBar;

        private float _soundCounter;
        private float _partileCounter = 1250;
        private bool _particle;

        private float _spawnCounter = 300;
        private bool _spawnPresent;

        public ObjShellHouse() : base("shell_bar") { }

        public ObjShellHouse(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 16, 0);
            EntitySize = new Rectangle(0, -16, 16, 16);

            // already collected the sword
            if (Game1.GameManager.SaveManager.GetString("hasSword2") == "1")
            {
                IsDead = true;
                return;
            }

            _barSprite = Resources.GetSprite("shell_bar");
            _barAnimator = AnimatorSaveLoad.LoadAnimator("Objects/shell_mansion_bar");

            var objShells = Game1.GameManager.GetItem("shell");
            if (objShells != null)
            {
                _shellCount = objShells.Count;
                _targetHeight = 16;
                // the first 10 shells move the bar more
                _targetHeight += (int)(MathHelper.Min(_shellCount, 10) / 5f * 32);
                // the second 10 shells move the bar half as much
                _targetHeight += (int)MathHelper.Max(0, (_shellCount - 10) / 10f * 32);
            }

            if (objShells == null || objShells.Count == 0)
            {
                _triggerDialog = true;
                _targetHeight = 0;
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void Update()
        {
            var playerDistance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (!_triggerEntryDialog && playerDistance.X < 105)
            {
                _triggerEntryDialog = true;
                Game1.GameManager.StartDialogPath("shell_mansion_entry");
            }

            if (!_triggerDialog && playerDistance.X < 66)
            {
                _fillBar = true;
                _triggerDialog = true;
            }

            if (_fillBar)
            {
                MapManager.ObjLink.FreezePlayer();

                _soundCounter -= Game1.DeltaTime;
                if (_soundCounter < 0)
                {
                    _soundCounter += 150;
                    Game1.GameManager.PlaySoundEffect("D370-06-06");
                }

                // 2sec -> 16px
                // 2000 / 16 = 125ms
                var addValue = Game1.DeltaTime / 125 * 2;
                if (_targetHeight > _barHeight + addValue)
                {
                    _barHeight += addValue;
                }
                else
                {
                    _fillBar = false;
                    _barHeight = _targetHeight;

                    _particle = true;

                    if (_shellCount == 20)
                        _barAnimator.Play("idle");

                    if (_shellCount == 5 || _shellCount == 10 || _shellCount == 20)
                        Game1.GameManager.PlaySoundEffect("D360-02-02");
                    else
                        Game1.GameManager.PlaySoundEffect("D360-29-1D");

                    var objParticle0 = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerPlayer, "Particles/shell_mansion_particle", "idle", true);
                    objParticle0.Animator.CurrentAnimation.LoopCount = 1;
                    objParticle0.EntityPosition.Set(new Vector2((int)EntityPosition.X - 8, (int)EntityPosition.Y - (int)_barHeight + 7));
                    Map.Objects.SpawnObject(objParticle0);

                    var objParticle1 = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerPlayer, "Particles/shell_mansion_particle", "idle", true);
                    objParticle1.Animator.CurrentAnimation.LoopCount = 1;
                    objParticle1.EntityPosition.Set(new Vector2((int)EntityPosition.X + 16 + 8, (int)EntityPosition.Y - (int)_barHeight + 7));
                    Map.Objects.SpawnObject(objParticle1);
                }
            }

            // wait a little bit while showing the particles
            if (_particle)
            {
                MapManager.ObjLink.FreezePlayer();

                if (_partileCounter > 0)
                    _partileCounter -= Game1.DeltaTime;
                else
                {
                    _particle = false;
                    if (_shellCount == 5 || _shellCount == 10)
                    {
                        _spawnPresent = true;

                        Game1.GameManager.PlaySoundEffect("D378-12-0C");

                        var objExplosion = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerBottom, "Particles/explosionBomb", "run", true);
                        objExplosion.EntityPosition.Set(new Vector2((int)EntityPosition.X - 48, (int)EntityPosition.Y - 64));
                        Map.Objects.SpawnObject(objExplosion);
                    }
                    else if (_shellCount == 20)
                    {
                        Game1.GameManager.StartDialogPath("shell_mansion_sword");
                    }
                    else
                    {
                        Game1.GameManager.StartDialogPath("shell_mansion_nothing");
                    }
                }
            }

            if (_spawnPresent)
            {
                if (_spawnCounter > 0)
                    _spawnCounter -= Game1.DeltaTime;
                else
                {
                    _spawnPresent = false;

                    var objItem = new ObjItem(Map, 0, 0, null, null, "shellPresent", null);
                    objItem.EntityPosition.Set(new Vector2((int)EntityPosition.X - 48, (int)EntityPosition.Y - 56));
                    Map.Objects.SpawnObject(objItem);
                }
            }

            if (_barAnimator.IsPlaying)
                _barAnimator.Update();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the animated bar
            if (_barAnimator.IsPlaying)
            {
                for (int i = 1; i < 8; i++)
                    _barAnimator.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y - 16 * i), Color.White);
            }
            else
            {
                spriteBatch.Draw(_barSprite.Texture, new Rectangle((int)EntityPosition.X, (int)EntityPosition.Y - (int)_barHeight, 16, (int)_barHeight), _barSprite.ScaledRectangle, Color.White);
            }
        }
    }
}