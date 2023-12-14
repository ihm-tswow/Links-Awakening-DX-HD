using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjTracy : GameObject
    {
        public BodyComponent Body;
        public readonly Animator Animator;

        private float _lookUpdateCounter;

        public ObjTracy() : base("tracy") { }

        public ObjTracy(Map.Map map, int posX, int posY) : base(map)
        {
            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_tracy");
            Animator.Play("idle");

            // 28, 42 or 7
            var price = Game1.GameManager.PieceOfPowerCount % 2 == 0 ? 0 : 1;
            if (Game1.GameManager.SaveManager.GetInt("npc_tracy", 0) == 8)
                price = 2;
            
            Game1.GameManager.SaveManager.SetString("npc_tracy_price", price.ToString());

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, sprite, Vector2.Zero);

            Body = new BodyComponent(EntityPosition, -8, -11, 15, 11, 8);

            AddComponent(BodyComponent.Index, Body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(Body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(Body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void Update()
        {
            _lookUpdateCounter += Game1.DeltaTime;
            if (_lookUpdateCounter > 250)
            {
                _lookUpdateCounter = 0;

                // look at the player
                var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
                if (Math.Abs(playerDirection.X) > Math.Abs(playerDirection.Y))
                {
                    if (playerDirection.X < 0)
                        Animator.Play("left");
                    else
                        Animator.Play("right");
                }
                else
                {
                    Animator.Play("idle");
                }
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("npc_tracy");
            return true;
        }
    }
}