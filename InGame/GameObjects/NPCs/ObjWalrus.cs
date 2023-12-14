using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjWalrus : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;

        private RectangleF _triggerRectangle;
        private bool _intersecting;

        private int _jumpCount = 5;

        private Rectangle _spriteSourceRectangle;

        private float _danceCounter;

        private bool _isFalling;
        private bool _splashed;

        public ObjWalrus() : base("walrus") { }

        public ObjWalrus(Map.Map map, int posX, int posY, string strDespawnKey) : base(map)
        {
            if (!string.IsNullOrEmpty(strDespawnKey) && Game1.GameManager.SaveManager.GetString(strDespawnKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/walrus");
            _animator.Play("sleep");

            EntityPosition = new CPosition(posX + 16, posY + 29, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _triggerRectangle = new RectangleF(posX - 8, posY, 32, 32);

            _body = new BodyComponent(EntityPosition, -16, -12, 32, 12, 8)
            {
                Gravity = -0.075f,
            };

            _aiComponent = new AiComponent();

            var stateSleep = new AiState(UpdateSleep);
            stateSleep.Trigger.Add(new AiTriggerCountdown(2200, null, SpawnParticle) { ResetAfterEnd = true });
            var stateAwaken = new AiState(UpdateAwaken) { Init = InitAwaken };
            var stateDance = new AiState(UpdateDance) { Init = InitDance };
            var stateJump = new AiState(UpdateJump) { Init = InitJump };
            var stateRoll = new AiState(UpdateRoll) { Init = InitRoll };
            var stateFall = new AiState(UpdateFall) { Init = InitFall };

            _aiComponent.States.Add("sleep", stateSleep);
            _aiComponent.States.Add("awaken", stateAwaken);
            _aiComponent.States.Add("dance", stateDance);
            _aiComponent.States.Add("jump", stateJump);
            _aiComponent.States.Add("roll", stateRoll);
            _aiComponent.States.Add("fall", stateFall);

            _aiComponent.ChangeState("sleep");

            //AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(EntityPosition, -16, -24, 32, 24, 8), Values.CollisionTypes.Enemy));
            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void KeyChanged()
        {
            // start singing?
            var value = Game1.GameManager.SaveManager.GetString("walrus_dance");
            if (value != null && value == "1")
            {
                Game1.GameManager.SaveManager.SetString("walrus_dance", "0");
                _aiComponent.ChangeState("awaken");
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("walrus");
            return true;
        }

        private void SpawnParticle()
        {
            var objBubble = new ObjBubble(Map, new Vector3(EntityPosition.X - 16, EntityPosition.Y, 19), new Vector3(-0.05f, 0, 0.1f));
            Map.Objects.SpawnObject(objBubble);
        }

        private void UpdateSleep()
        {
            // trigger dialog on entering the trigger area while marin is following the player
            if (MapManager.ObjLink._body.IsGrounded &&
                _triggerRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
            {
                if (!_intersecting)
                {
                    var item = Game1.GameManager.GetItem("marin");
                    if (item != null && item.Count >= 1)
                        Game1.GameManager.StartDialogPath("walrus");
                }

                _intersecting = true;
            }
            else
            {
                _intersecting = false;
            }
        }

        private void InitAwaken()
        {
            _animator.Play("awakening");
        }

        private void UpdateAwaken()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("dance");
        }

        private void InitDance()
        {
            _animator.Play("wobble");
            _danceCounter = 0;
        }

        private void UpdateDance()
        {
            _danceCounter += Game1.DeltaTime;
            if (_danceCounter > 250)
            {
                _danceCounter -= 9999;
                Game1.GameManager.PlaySoundEffect("D360-39-27");
            }

            if (!_animator.IsPlaying)
            {
                if (_jumpCount > 0)
                    _aiComponent.ChangeState("jump");
                else
                    _aiComponent.ChangeState("roll");
            }
        }

        private void InitJump()
        {
            _animator.Play("up");
            _body.Velocity.Z = 1.25f;
            _jumpCount--;

            Game1.GameManager.PlaySoundEffect("D360-36-24");
        }

        private void UpdateJump()
        {
            if (_body.Velocity.Z < 0)
                _animator.Play("down");

            if (_body.IsGrounded)
                _aiComponent.ChangeState("dance");
        }

        private void InitRoll()
        {
            _animator.Play("jump");

            Game1.GameManager.PlaySoundEffect("D360-39-27");
            Game1.GameManager.PlaySoundEffect("D378-17-11");
        }

        private void UpdateRoll()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("fall");
        }

        private void InitFall()
        {
            _animator.Play("fall");
            EntityPosition.Z = 23 + 27;
            EntityPosition.Offset(new Vector2(0, 32));
            _spriteSourceRectangle = _sprite.SourceRectangle;
            _isFalling = true;
        }

        private void UpdateFall()
        {
            if (!_splashed && EntityPosition.Z < _spriteSourceRectangle.Height)
            {
                Game1.GameManager.PlaySoundEffect("D378-36-24");

                var splashAnimator0 = new ObjAnimator(Map, (int)EntityPosition.X - 6, (int)EntityPosition.Y + 1, 0, 0, Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                var splashAnimator1 = new ObjAnimator(Map, (int)EntityPosition.X + 6, (int)EntityPosition.Y + 2, 0, 0, Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                Map.Objects.SpawnObject(splashAnimator0);
                Map.Objects.SpawnObject(splashAnimator1);

                _splashed = true;
                ((BodyDrawShadowComponent)Components[BodyDrawShadowComponent.Index]).IsActive = false;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // disapear into the water
            if (_isFalling)
            {
                _sprite.SourceRectangle.Y = _spriteSourceRectangle.Y + MathHelper.Clamp(_spriteSourceRectangle.Height - (int)EntityPosition.Z, 0, _spriteSourceRectangle.Height);
                _sprite.SourceRectangle.Height = MathHelper.Clamp((int)EntityPosition.Z, 0, _spriteSourceRectangle.Height);
            }

            _sprite.Draw(spriteBatch);
        }
    }
}