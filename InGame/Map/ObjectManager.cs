using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Pools;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Map
{
    public class ObjectManager
    {
        // TODO_End check if everything is cleaned while loading a new map
        public Map Owner;

        public List<GameObjectItem> ObjectList = new List<GameObjectItem>();
        private List<GameObject> SpawnObjects = new List<GameObject>();
        public List<GameObject> DeleteObjects = new List<GameObject>();

        public Texture2D ShadowTexture;
        public static Effect CurrentEffect;

        private List<GameObject> _poolSpawnedObjects = new List<GameObject>();

        private ComponentPool _gameObjectPool;
        private ComponentDrawPoolNew _drawPool;

        private SystemBody _systemBody = new SystemBody();
        private SystemAi _systemAi = new SystemAi();
        private SystemAnimation _systemAnimator = new SystemAnimation();

        private List<KeyChangeListenerComponent.KeyChangeTemplate> _keyChangeListeners =
            new List<KeyChangeListenerComponent.KeyChangeTemplate>();

        // lists are used to not generate new ones every time
        // one list object would probably be enough?
        private readonly List<GameObject> _updateGameObject = new List<GameObject>();
        private readonly List<GameObject> _damageFieldObjects = new List<GameObject>();
        private readonly List<GameObject> _drawShadowObjects = new List<GameObject>();
        private readonly List<GameObject> _depthObjectList = new List<GameObject>();
        private readonly List<GameObject> _objectTypeList = new List<GameObject>();
        private readonly List<GameObject> _objectTagAllList = new List<GameObject>();
        private readonly List<GameObject> _collisionObjectList = new List<GameObject>();

        private readonly List<GameObject> _collidingObjectList = new List<GameObject>();
        private readonly List<GameObject> _lightObjectList = new List<GameObject>();
        private readonly List<GameObject> _carriableObjectList = new List<GameObject>();
        private readonly List<GameObject> _hittableObjectList = new List<GameObject>();
        private readonly List<GameObject> _pushableObjectList = new List<GameObject>();
        private readonly List<GameObject> _interactableObjectList = new List<GameObject>();

        private readonly List<GameObject> db_hittableList = new List<GameObject>();
        private readonly List<GameObject> db_damageList = new List<GameObject>();
        private readonly List<GameObject> db_bodyList = new List<GameObject>();
        private readonly List<GameObject> db_gameObjectList = new List<GameObject>();

        private bool _keyChanged;
        private bool _finishedLoading;

        public ObjectManager(Map owner)
        {
            Owner = owner;
        }

        public void LoadObjects()
        {
            // TODO_End tweak the size for best performance for the finished game
            // the size of the pools can be tweaked for faster update times
            _gameObjectPool = new ComponentPool(Owner, Owner.MapWidth, Owner.MapHeight, 32, 32);
            _drawPool = new ComponentDrawPoolNew(Owner.MapWidth, Owner.MapHeight, 32, 32);

            _systemAnimator.Pool = _gameObjectPool;
            _systemAi.Pool = _gameObjectPool;
            _systemBody.Pool = _gameObjectPool;

            ClearPools();

            foreach (var gameObj in ObjectList)
            {
                var gameObject = GetGameObject(Owner, gameObj.Index, gameObj.Parameter);
                AddObjectToMap(gameObject);
            }

            // done after calling the constructors before the init methode
            // stonespawner adds sprites that can be accessed in the init methode
            // we make sure to not call the init methodes to not call it twice
            AddSpawnedObjects(true);

            // okay so one problem are the dodongo snakes:
            // a chest should get spawned after killing two of them
            // but after reloading the chest should not be there
            // so the snakes will reset a key on reload
            // now the key condition setter sets the key
            // and the position dialog spawns the chest
            // so in the init method the key needs to be set correctly
            // for this to work the key condition setter will need to be updated after the snakes are created
            // for this we need to call the key listeners before calling the init method
            UpdateKeyListeners();

            foreach (var gameObject in _poolSpawnedObjects)
                gameObject.Init();

            // ensures key setters, key condition setter, etc are all set correctly after loading the objects
            UpdateKeyListeners();

            // done before and after init because ObjEnemyTrigger Init expects objects spawned in the constructor
            AddSpawnedObjects();

            _finishedLoading = true;
        }

        public void Update(bool frozen)
        {
            // mode used for opened dialog
            if (frozen)
            {
                // notify all key listener
                UpdateKeyListeners();

                UpdateDeleteObjects();

                AddSpawnedObjects();

                return;
            }

            if (Game1.GameManager.FreezeWorldAroundPlayer)
            {
                Game1.GameManager.FreezeWorldAroundPlayer = false;

                // only update the player
                var updateComponent = (UpdateComponent)MapManager.ObjLink.Components[UpdateComponent.Index];
                updateComponent?.UpdateFunction();

                return;
            }

            Game1.StopWatchTracker.Start("update gameobjects");

            _systemAnimator.Update(false);

            UpdateGameObjects();

            _systemAi.Update();

            // notify all key listener
            UpdateKeyListeners();

            _systemBody.Update(0, 1);

            UpdatePlayerCollision();

            UpdateDamageFields();

            UpdateDeleteObjects();

            AddSpawnedObjects();
        }

        public void UpdateAnimations()
        {
            _systemAnimator.Update(true);
        }

        private void AddSpawnedObjects(bool suppressInit = false)
        {
            // add newly spawned objects
            if (SpawnObjects.Count > 0)
            {
                // ObjChest is adding to the SpawnObjects list in it's init method
                for (var index = 0; index < SpawnObjects.Count; index++)
                {
                    if (!suppressInit)
                        SpawnObjects[index].Init();
                    AddObjectToMap(SpawnObjects[index]);
                }

                SpawnObjects.Clear();
            }
        }

        private void UpdateGameObjects()
        {
            _updateGameObject.Clear();

            // only update the objects that are in a tile that is visible
            var updateFieldSize = new Vector2(Game1.RenderWidth, Game1.RenderHeight);
            _gameObjectPool.GetComponentList(_updateGameObject,
               (int)((MapManager.Camera.X - updateFieldSize.X / 2) / MapManager.Camera.Scale),
               (int)((MapManager.Camera.Y - updateFieldSize.Y / 2) / MapManager.Camera.Scale),
               (int)(updateFieldSize.X / MapManager.Camera.Scale),
               (int)(updateFieldSize.Y / MapManager.Camera.Scale), UpdateComponent.Mask);

            foreach (var gameObject in _updateGameObject)
            {
                if (gameObject.IsActive)
                    (gameObject.Components[UpdateComponent.Index] as UpdateComponent)?.UpdateFunction();
            }
        }

        private void UpdateKeyListeners()
        {
            // notify all key listeners
            // repeat the process so key changes that depend on different key changes get processed in a single frame
            // max is used to avoid an infinite loop in case of a bug
            var max = 5;
            while (_keyChanged && max > 0)
            {
                max--;
                _keyChanged = false;

                // notify key listeners if a key value was changed
                foreach (var listener in _keyChangeListeners)
                    listener();
            }
        }

        private void UpdatePlayerCollision()
        {
            // player collides with stuff
            var player = MapManager.ObjLink;

            _collidingObjectList.Clear();
            _gameObjectPool.GetComponentList(_collidingObjectList,
                (int)player.BodyRectangle.X, (int)player.BodyRectangle.Y,
                (int)player.BodyRectangle.Width, (int)player.BodyRectangle.Height, ObjectCollisionComponent.Mask);

            foreach (var gameObject in _collidingObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var component = gameObject.Components[ObjectCollisionComponent.Index] as ObjectCollisionComponent;
                if (component.TriggerOnCollision && component.CollisionRectangle.Rectangle.Intersects(player.BodyRectangle) ||
                    !component.TriggerOnCollision && component.CollisionRectangle.Rectangle.Contains(player.BodyRectangle))
                    component.OnCollision(player);
            }
        }

        private void UpdateDamageFields()
        {
            var player = MapManager.ObjLink;
            var playerDamageBox = player.DamageCollider.Box;

            // get the objects that could potentially inflic damage
            _damageFieldObjects.Clear();
            _gameObjectPool.GetComponentList(_damageFieldObjects,
                (int)playerDamageBox.X, (int)playerDamageBox.Y,
                (int)playerDamageBox.Width, (int)playerDamageBox.Height, DamageFieldComponent.Mask);

            foreach (var gameObject in _damageFieldObjects)
            {
                if (!gameObject.IsActive)
                    continue;

                var damageField = (gameObject.Components[DamageFieldComponent.Index] as DamageFieldComponent);
                if (damageField.IsActive && damageField.CollisionBox.Box.Intersects(playerDamageBox))
                    damageField.OnDamage?.Invoke();
            }
        }

        private void UpdateDeleteObjects()
        {
            Game1.StopWatchTracker.Start("delete gameObjects");

            if (DeleteObjects.Count > 0)
            {
                foreach (var deletable in DeleteObjects)
                    RemoveObject(deletable);

                DeleteObjects.Clear();
            }

            Game1.StopWatchTracker.Stop();
        }

        public static void SpriteBatchBegin(SpriteBatch spriteBatch, SpriteShader spriteShader)
        {
            SetSpriteShader(spriteShader);

            CurrentEffect = spriteShader?.Effect;
            spriteBatch.Begin(SpriteSortMode.Deferred, null,
                MapManager.Camera.Scale >= 1 ? SamplerState.PointWrap : SamplerState.AnisotropicWrap,
                null, null, CurrentEffect, MapManager.Camera.TransformMatrix);
        }

        public static void SetSpriteShader(SpriteShader spriteShader)
        {
            // update the parameters of the shader
            if (spriteShader != null)
                foreach (var parameter in spriteShader.FloatParameter)
                    spriteShader.Effect.Parameters[parameter.Key].SetValue(parameter.Value);
        }

        public static void SpriteBatchBeginAnisotropic(SpriteBatch spriteBatch, Effect effect)
        {
            CurrentEffect = effect;
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp,
                null, null, effect, MapManager.Camera.TransformMatrix);
        }

        public void DrawBottom(SpriteBatch spriteBatch)
        {
            if (!_finishedLoading)
                return;

            Game1.StopWatchTracker.Start("2 draw sorted objects");

            SpriteBatchBegin(spriteBatch, null);

            _drawPool.DrawPool(spriteBatch,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), 0, 1);

            spriteBatch.End();
        }

        public void DrawMiddle(SpriteBatch spriteBatch)
        {
            if (!_finishedLoading)
                return;

            Game1.StopWatchTracker.Start("2 draw sorted objects shadow");

            SpriteBatchBegin(spriteBatch, null);

            _drawPool.DrawPool(spriteBatch,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), 1, 2);
            spriteBatch.End();

            // draw the hole map
            Owner.HoleMap.Draw(spriteBatch);

            Game1.StopWatchTracker.Start("3 draw the shadows");
            if (GameSettings.EnableShadows && Owner.UseShadows && !Game1.GameManager.UseShockEffect && ShadowTexture != null)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp);//, null, null, null, Game1.GameManager.GetMatrix);
                spriteBatch.Draw(ShadowTexture, new Rectangle(0, 0, Game1.GameManager.CurrentRenderWidth, Game1.GameManager.CurrentRenderHeight), Color.Black * 0.55f);
                spriteBatch.End();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_finishedLoading)
                return;

            Game1.StopWatchTracker.Start("4 draw sorted objects");
            spriteBatch.Begin(SpriteSortMode.Deferred, null,
                MapManager.Camera.Scale >= 1 ? SamplerState.PointWrap : SamplerState.AnisotropicWrap,
                null, null, null, MapManager.Camera.TransformMatrix);
            _drawPool.DrawPool(spriteBatch,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), 2, 4);
            spriteBatch.End();

            // draw the body colliders
            if (Game1.DebugMode)
            {
                Game1.StopWatchTracker.Start("5 debug draw");

                spriteBatch.Begin(SpriteSortMode.Deferred, null,
                    MapManager.Camera.Scale >= 1 ? SamplerState.PointWrap : SamplerState.AnisotropicWrap,
                    null, null, null, MapManager.Camera.TransformMatrix);

                // draw entity size rectangle
                if (Game1.DebugBoxMode == 0)
                {
                    db_gameObjectList.Clear();
                    _gameObjectPool.GetObjectList(db_gameObjectList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale));
                    foreach (var gameObject in db_gameObjectList)
                    {
                        if (gameObject.EntityPosition != null && !(gameObject is ObjLamp))
                        {
                            var rectangle = new RectangleF(
                                gameObject.EntityPosition.X + gameObject.EntitySize.X,
                                gameObject.EntityPosition.Y + gameObject.EntitySize.Y,
                                gameObject.EntitySize.Width, gameObject.EntitySize.Height);

                            DrawRectangle(spriteBatch, rectangle, Color.LightBlue);
                        }
                    }
                }

                // draw the bodies
                if (Game1.DebugBoxMode == 0 || Game1.DebugBoxMode == 1)
                {
                    db_bodyList.Clear();
                    _gameObjectPool.GetComponentList(db_bodyList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale), BodyComponent.Mask);
                    foreach (var drawTile in db_bodyList)
                    {
                        var body = drawTile.Components[BodyComponent.Index] as BodyComponent;
                        DrawRectangle(spriteBatch, body.BodyBox.Box.Rectangle(), Color.Red);
                    }
                }

                // draw the damage fields
                if (Game1.DebugBoxMode == 0 || Game1.DebugBoxMode == 2)
                {
                    db_damageList.Clear();
                    _gameObjectPool.GetComponentList(db_damageList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale), DamageFieldComponent.Mask);
                    foreach (var drawTile in db_damageList)
                    {
                        var damageComponent = drawTile.Components[DamageFieldComponent.Index] as DamageFieldComponent;
                        DrawRectangle(spriteBatch, damageComponent.CollisionBox.Box.Rectangle(), Color.Green);
                    }
                }

                // draw the hittable fields
                if (Game1.DebugBoxMode == 0 || Game1.DebugBoxMode == 3)
                {
                    db_damageList.Clear();
                    _gameObjectPool.GetComponentList(db_damageList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale), HittableComponent.Mask);
                    foreach (var drawTile in db_damageList)
                    {
                        var hittableComponent = drawTile.Components[HittableComponent.Index] as HittableComponent;
                        DrawRectangle(spriteBatch, hittableComponent.HittableBox.Box.Rectangle(), Color.Yellow);
                    }
                }

                // draw the push fields
                if (Game1.DebugBoxMode == 0 || Game1.DebugBoxMode == 4)
                {
                    db_damageList.Clear();
                    _gameObjectPool.GetComponentList(db_damageList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale), PushableComponent.Mask);
                    foreach (var drawTile in db_damageList)
                    {
                        var pushableComponent = drawTile.Components[PushableComponent.Index] as PushableComponent;
                        DrawRectangle(spriteBatch, pushableComponent.PushableBox.Box.Rectangle(), Color.Orange);
                    }
                }

                // draw the interact rectangle
                if (Game1.DebugBoxMode == 0 || Game1.DebugBoxMode == 5)
                {
                    db_damageList.Clear();
                    _gameObjectPool.GetComponentList(db_damageList,
                        (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                        (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                        (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                        (int)(Game1.RenderHeight / MapManager.Camera.Scale), InteractComponent.Mask);
                    foreach (var drawTile in db_damageList)
                    {
                        var pushableComponent = drawTile.Components[InteractComponent.Index] as InteractComponent;
                        DrawRectangle(spriteBatch, pushableComponent.BoxInteractabel.Box.Rectangle(), Color.Aqua);
                    }
                }

                spriteBatch.End();
            }

            Game1.StopWatchTracker.Stop();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, MapManager.Camera.TransformMatrix);
        }

        private void DrawRectangle(SpriteBatch spriteBatch, RectangleF rectangle, Color color)
        {
            spriteBatch.Draw(Resources.SprWhite,
                new Vector2(rectangle.X, rectangle.Y),
                new Rectangle(0, 0, (int)rectangle.Width, (int)rectangle.Height), color * 0.25f);

            var thickness = 1 / (float)Game1.ScreenScale;
            spriteBatch.Draw(Resources.SprWhite,
                new Vector2(rectangle.X, rectangle.Y),
                new Rectangle(0, 0, 1, (int)(rectangle.Height * Game1.ScreenScale)),
                color * 0.75f, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
            spriteBatch.Draw(Resources.SprWhite,
                new Vector2(rectangle.X + rectangle.Width - thickness, rectangle.Y),
                new Rectangle(0, 0, 1, (int)(rectangle.Height * Game1.ScreenScale)),
                color * 0.75f, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
            spriteBatch.Draw(Resources.SprWhite,
                new Vector2(rectangle.X, rectangle.Y),
                new Rectangle(0, 0, (int)(rectangle.Width * Game1.ScreenScale), 1),
                color * 0.75f, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
            spriteBatch.Draw(Resources.SprWhite,
                new Vector2(rectangle.X, rectangle.Y + rectangle.Height - thickness),
                new Rectangle(0, 0, (int)(rectangle.Width * Game1.ScreenScale), 1),
                color * 0.75f, 0, Vector2.Zero, thickness, SpriteEffects.None, 0);
        }

        public void DrawShadow(SpriteBatch spriteBatch)
        {
            DrawHelper.StartShadowDrawing();

            // draw the shadows
            _drawShadowObjects.Clear();
            _gameObjectPool.GetComponentList(_drawShadowObjects,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), DrawShadowComponent.Mask);

            foreach (var gameObject in _drawShadowObjects)
                if (gameObject.IsActive)
                    (gameObject.Components[DrawShadowComponent.Index] as DrawShadowComponent)?.Draw(spriteBatch);

            DrawHelper.EndShadowDrawing();
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            // draw the shadows
            _lightObjectList.Clear();
            _gameObjectPool.GetComponentList(_lightObjectList,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), LightDrawComponent.Mask);

            _lightObjectList.Sort((obj0, obj1) =>
            {
                var light0 = (LightDrawComponent)obj0.Components[LightDrawComponent.Index];
                var light1 = (LightDrawComponent)obj1.Components[LightDrawComponent.Index];

                if (light0 != null && light1 != null)
                {
                    if (light0.Layer == light1.Layer &&
                        light0.Owner.EntityPosition != null && light1.Owner.EntityPosition != null)
                        return (int)light0.Owner.EntityPosition.Y - (int)light1.Owner.EntityPosition.Y;

                    return light0.Layer - light1.Layer;
                }

                return 0;
            });

            for (var i = 0; i < _lightObjectList.Count; i++)
            {
                if (_lightObjectList[i].IsActive)
                    (_lightObjectList[i].Components[LightDrawComponent.Index] as LightDrawComponent)?.Draw(spriteBatch);
            }
        }

        public void DrawBlur(SpriteBatch spriteBatch)
        {
            // draw the shadows
            _drawShadowObjects.Clear();
            _gameObjectPool.GetComponentList(_drawShadowObjects,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), BlurDrawComponent.Mask);

            foreach (var gameObject in _drawShadowObjects)
                if (gameObject.IsActive)
                    (gameObject.Components[BlurDrawComponent.Index] as BlurDrawComponent)?.Draw(spriteBatch);
        }

        public void Clear()
        {
            ObjectList.Clear();
        }

        private void ClearPools()
        {
            _keyChangeListeners.Clear();
            _poolSpawnedObjects.Clear();
        }

        private void AddObjectToMap(GameObject gameObject)
        {
            if (gameObject == null || gameObject.IsDead)
                return;

            _poolSpawnedObjects.Add(gameObject);

            // order is important because the draw pool does not update the last position value
            // add the object to the drawable pool
            if ((gameObject.ComponentsMask & DrawComponent.Mask) == DrawComponent.Mask)
                _drawPool.AddEntity(gameObject);

            // add the object to the pool
            _gameObjectPool.AddEntity(gameObject);

            // add key listeners
            if ((gameObject.ComponentsMask & KeyChangeListenerComponent.Mask) == KeyChangeListenerComponent.Mask)
            {
                var listener = (gameObject.Components[KeyChangeListenerComponent.Index] as KeyChangeListenerComponent).KeyChangeFunction;
                _keyChangeListeners.Add(listener);
            }
        }

        public void RemoveObject(GameObject gameObject)
        {
            gameObject.Map = null;

            _poolSpawnedObjects.Remove(gameObject);

            // remove the object from the drawable pool
            if ((gameObject.ComponentsMask & DrawComponent.Mask) == DrawComponent.Mask)
                _drawPool.RemoveEntity(gameObject);

            // remove the object from the pool
            _gameObjectPool.RemoveEntity(gameObject);

            // remove key listeners
            if ((gameObject.ComponentsMask & KeyChangeListenerComponent.Mask) == KeyChangeListenerComponent.Mask)
            {
                var listener = (gameObject.Components[KeyChangeListenerComponent.Index] as KeyChangeListenerComponent).KeyChangeFunction;
                _keyChangeListeners.Remove(listener);
            }
        }

        public void ReloadObjects()
        {
            LoadObjects();
        }

        public void TriggerKeyChange()
        {
            _keyChanged = true;
        }

        public bool SpawnObject(GameObject newObject)
        {
            if (newObject == null || newObject.IsDead)
                return false;

            SpawnObjects.Add(newObject);
            return true;
        }

        public bool SpawnObject(string objectId, object[] objectParameter)
        {
            if (objectId == null || !GameObjectTemplates.ObjectTemplates.ContainsKey(objectId))
                return false;

            return SpawnObject(GetGameObject(Owner, objectId, objectParameter));
        }

        public static GameObject GetGameObject(Map owner, string objectId, object[] objectParameter)
        {
            if (!GameObjectTemplates.ObjectSpawner.ContainsKey(objectId))
                return null;

            if (objectParameter == null)
            {
                objectParameter = MapData.GetParameter(objectId, null);
                objectParameter[1] = 0;
                objectParameter[2] = 0;
            }

            objectParameter[0] = owner;
            var constructor = GameObjectTemplates.ObjectSpawner[objectId];

            return constructor(objectParameter);
        }

        // TODO_End: be careful to not use this often
        public List<GameObject> GetObjectsOfType(Type type)
        {
            _objectTypeList.Clear();

            for (var i = 0; i < _poolSpawnedObjects.Count; i++)
                if (_poolSpawnedObjects[i].GetType() == type)
                    _objectTypeList.Add(_poolSpawnedObjects[i]);

            return _objectTypeList;
        }

        public List<GameObject> GetObjects(int left, int top, int width, int height)
        {
            // get possible candidates
            _objectTagAllList.Clear();
            _gameObjectPool.GetObjectList(_objectTagAllList, left, top, width, height);

            var outputList = new List<GameObject>();
            foreach (var gameObject in _objectTagAllList)
            {
                if (gameObject.EntityPosition != null &&
                    left <= gameObject.EntityPosition.X + gameObject.EntitySize.X &&
                    gameObject.EntityPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width <= left + width &&
                    top <= gameObject.EntityPosition.Y + gameObject.EntitySize.Y &&
                    gameObject.EntityPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height <= top + height)
                    outputList.Add(gameObject);
            }
            return outputList;
        }

        public void GetObjectsOfType(List<GameObject> outputList, Type type, int left, int top, int width, int height)
        {
            // get possible candidates
            _objectTagAllList.Clear();
            _gameObjectPool.GetObjectList(_objectTagAllList, left, top, width, height);

            foreach (var gameObject in _objectTagAllList)
            {
                if (gameObject.GetType() == type &&
                    left <= gameObject.EntityPosition.X + gameObject.EntitySize.X + gameObject.EntitySize.Width &&
                    gameObject.EntityPosition.X + gameObject.EntitySize.X <= left + width &&
                    top <= gameObject.EntityPosition.Y + gameObject.EntitySize.Y + gameObject.EntitySize.Height &&
                    gameObject.EntityPosition.Y + gameObject.EntitySize.Y <= top + height)
                    outputList.Add(gameObject);
            }
        }

        public GameObject GetObjectOfType(int left, int top, int width, int height, Type type)
        {
            return _gameObjectPool.GetObjectOfType(left, top, width, height, type);
        }

        public void GetGameObjectsWithTag(List<GameObject> outputList, Values.GameObjectTag tag, int left, int top, int width, int height)
        {
            // get possible candidates
            _objectTagAllList.Clear();
            _gameObjectPool.GetObjectList(_objectTagAllList, left, top, width, height);

            outputList.Clear();

            foreach (var gameObject in _objectTagAllList)
            {
                if ((gameObject.Tags & tag) != 0 &&
                   left <= gameObject.EntityPosition.X && gameObject.EntityPosition.X <= left + width &&
                   top <= gameObject.EntityPosition.Y && gameObject.EntityPosition.Y <= top + height)
                    outputList.Add(gameObject);
            }
        }

        public void GetComponentList(List<GameObject> gameObjectList,
            int recLeft, int recTop, int recWidth, int recHeight, int componentMask)
        {
            _gameObjectPool.GetComponentList(gameObjectList, recLeft, recTop, recWidth, recHeight, componentMask);
        }

        public bool Collision(Box box, Box oldBox, Values.CollisionTypes collisionTypes, Values.CollisionTypes ignoreTypes, int dir, int level, ref Box collidingBox)
        {
            // get all the near objects of the rectangle
            _collisionObjectList.Clear();
            _gameObjectPool.GetComponentList(_collisionObjectList, (int)box.X, (int)box.Y, (int)box.Width, (int)box.Height, CollisionComponent.Mask);

            foreach (var gameObject in _collisionObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var collisionObject = gameObject.Components[CollisionComponent.Index] as CollisionComponent;
                if ((collisionObject.CollisionType & collisionTypes) != 0 &&
                    (collisionObject.CollisionType & ignoreTypes) == 0 &&
                     collisionObject.Collision(box, dir, level, ref collidingBox) &&
                    (oldBox == Box.Empty || !collisionObject.Collision(oldBox, dir, level, ref collidingBox)))
                    return true;
            }

            return false;
        }

        public bool Collision(Box box, Box oldBox, Values.CollisionTypes collisionTypes, int dir, int level, ref Box collidingBox)
        {
            return Collision(box, oldBox, collisionTypes, Values.CollisionTypes.None, dir, level, ref collidingBox);
        }

        public float GetDepth(Box box, Values.CollisionTypes collisionType, float maxDepth)
        {
            float outDepth = 0;

            // depth of holes
            var center = new Vector2((box.X + box.Width / 2) / Values.TileSize, (box.Front - 1) / Values.TileSize);
            if (Owner.HoleMap != null && Owner.HoleMap.ArrayTileMap != null &&
                0 <= center.X && center.X < Owner.HoleMap.ArrayTileMap.GetLength(0) &&
                0 <= center.Y && center.Y < Owner.HoleMap.ArrayTileMap.GetLength(1))
            {
                var distance = new Vector2((int)center.X + 0.5f, (int)center.Y + 0.5f) - center;
                outDepth = Owner.HoleMap.ArrayTileMap[(int)center.X, (int)center.Y, 0] < 0 ? 0 : -2 * Math.Clamp(1 - distance.Length() * 1.5f, 0, 1);
            }

            _depthObjectList.Clear();
            _gameObjectPool.GetComponentList(_depthObjectList, (int)box.X, (int)box.Y, (int)box.Width, (int)box.Height, CollisionComponent.Mask);

            var clampDepth = outDepth;

            foreach (var gameObject in _depthObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var newDepth = outDepth;
                var lowestDepth = GetObjectDepth(gameObject, box, collisionType, ref newDepth);

                // floating point inaccuracy can lead to combined intersection areas greater than 100%
                // to fix this problem the maxDepth value will need to be clamped to not exceed the max depth
                if (lowestDepth < clampDepth)
                    clampDepth = lowestDepth;

                if (newDepth <= maxDepth)
                    outDepth = newDepth;
            }

            return MathF.Max(outDepth, clampDepth);
        }

        private float GetObjectDepth(GameObject gameObject, Box box, Values.CollisionTypes collisionType, ref float maxDepth)
        {
            var collisionObject = gameObject.Components[CollisionComponent.Index] as CollisionComponent;
            var collidingBox = Box.Empty;

            // add the object to the list if it is in the mask and is colliding with the provided rectangle
            if ((collisionObject.CollisionType & collisionType) != 0 &&
                collisionObject.Collision(box, 0, 0, ref collidingBox))
            {
                var bottom = collidingBox.Z + collidingBox.Depth;
                if (bottom < 0)
                {
                    // combine the depth values lower than 0 to smoothly transition to lower floors
                    var bodyRec = box.Rectangle();
                    var intersection = collidingBox.Rectangle().GetIntersection(bodyRec);
                    maxDepth += bottom * ((intersection.Width * intersection.Height) / (bodyRec.Width * bodyRec.Height));
                    return bottom;
                }
                if (bottom > maxDepth)
                {
                    maxDepth = bottom;
                    return maxDepth;
                }
            }

            return 0;
        }

        public GameObject GetCarryableObjects(RectangleF rectangle)
        {
            _carriableObjectList.Clear();
            _gameObjectPool.GetComponentList(_carriableObjectList, (int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height, CarriableComponent.Mask);

            foreach (var gameObject in _carriableObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var component = gameObject.Components[CarriableComponent.Index] as CarriableComponent;

                if (component.IsActive && component.Rectangle.Rectangle.Intersects(rectangle))
                    return gameObject;
            }

            return null;
        }

        public Values.HitCollision Hit(GameObject originObject, Vector2 forceOrigin, Box hitBox, HitType type, int damage, bool doubleDamage, bool multidamage = true)
        {
            return Hit(originObject, forceOrigin, hitBox, type, damage, doubleDamage, out var direction, multidamage);
        }

        public Values.HitCollision Hit(GameObject originObject, Vector2 forceOrigin, Box hitBox, HitType type, int damage, bool doubleDamage, out Vector2 direction, bool multidamage = true)
        {
            // get all the near objects of the rectangle
            _hittableObjectList.Clear();
            _gameObjectPool.GetComponentList(_hittableObjectList, (int)hitBox.X, (int)hitBox.Y, (int)hitBox.Width, (int)hitBox.Height, HittableComponent.Mask);

            var hitCollision = Values.HitCollision.None;
            direction = Vector2.Zero;

            foreach (var gameObject in _hittableObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var hittableComponent = gameObject.Components[HittableComponent.Index] as HittableComponent;

                if (!hittableComponent.IsActive || !hittableComponent.HittableBox.Box.Intersects(hitBox))
                    continue;

                // direction goes from the hitter towards the center of hittable box
                direction = hittableComponent.HittableBox.Box.Center - forceOrigin;
                if (direction != Vector2.Zero)
                    direction.Normalize();

                hitCollision |= hittableComponent.Hit(originObject, direction, type, damage, doubleDamage);

                if (!multidamage && hitCollision != Values.HitCollision.None && hitCollision != Values.HitCollision.NoneBlocking)
                    return hitCollision;
            }

            return hitCollision;
        }

        public PushableComponent PushObject(Box box, Vector2 direction, PushableComponent.PushType type)
        {
            // get all the near objects of the rectangle
            _pushableObjectList.Clear();
            _gameObjectPool.GetComponentList(_pushableObjectList, (int)box.X, (int)box.Y, (int)box.Width, (int)box.Height, PushableComponent.Mask);

            PushableComponent outPushComponent = null;

            foreach (var gameObject in _pushableObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var pushableComponent = gameObject.Components[PushableComponent.Index] as PushableComponent;

                // check for collision and if cooldown time is met
                if (pushableComponent.IsActive &&
                    pushableComponent.LastPushTime + pushableComponent.CooldownTime < Game1.TotalGameTime &&
                    box.Intersects(pushableComponent.PushableBox.Box))
                {
                    // object got inertia?
                    if (pushableComponent.InertiaTime > 0 && type == PushableComponent.PushType.Continues)
                    {
                        // the object was pushed the last frame?
                        if (pushableComponent.LastWaitTime >= Game1.TotalGameTimeLast)
                        {
                            pushableComponent.InertiaCounter -= Game1.DeltaTime;
                            if (pushableComponent.InertiaCounter <= 0 && pushableComponent.Push(direction, type))
                            {
                                pushableComponent.InertiaCounter = pushableComponent.InertiaTime;
                                pushableComponent.LastPushTime = Game1.TotalGameTime;
                                outPushComponent = pushableComponent;
                            }
                        }
                        else
                        {
                            // reset inertia counter if pushing has just begone
                            pushableComponent.InertiaCounter = pushableComponent.InertiaTime;
                        }

                        pushableComponent.LastWaitTime = Game1.TotalGameTime;
                    }
                    else if (pushableComponent.Push(direction, type))
                    {
                        pushableComponent.LastPushTime = Game1.TotalGameTime;
                        outPushComponent = pushableComponent;
                    }
                }
            }

            return outPushComponent;
        }

        public bool InteractWithObject(Box box)
        {
            // get the interactable objects at the given position
            _interactableObjectList.Clear();
            _gameObjectPool.GetComponentList(_interactableObjectList, (int)box.X, (int)box.Y, (int)box.Width, (int)box.Height, InteractComponent.Mask);

            // go through all the interactable objects and check for collision before interacting with them
            foreach (var gameObject in _interactableObjectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var component = gameObject.Components[InteractComponent.Index] as InteractComponent;
                if (component.IsActive && component.BoxInteractabel.Box.Intersects(box) && component.InteractFunction())
                    return true;
            }

            return false;
        }
    }
}