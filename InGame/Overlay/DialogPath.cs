using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    class DialogAction
    {
        public virtual void Init() { }

        public virtual bool Execute()
        {
            return true;
        }
    }

    class DialogActionStartDialog : DialogAction
    {
        private readonly string _key;

        public DialogActionStartDialog(string key)
        {
            _key = key;
        }

        public override bool Execute()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return false;

            Game1.GameManager.StartDialog(_key);
            return true;
        }
    }

    class DialogActionStartPath : DialogAction
    {
        private readonly string _key;

        public DialogActionStartPath(string key)
        {
            _key = key;
        }

        public override bool Execute()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return false;

            // add the dialog path as the first element to be executed directly after this dialog
            Game1.GameManager.AddFirstDialogPath(_key);
            return true;
        }
    }

    class DialogActionDialog : DialogAction
    {
        private readonly string _key;
        private readonly string _choiceKey;
        private readonly string[] _choicesKeys;

        public DialogActionDialog(string key, string choiceKey, params string[] choices)
        {
            _key = key;
            _choiceKey = choiceKey;
            _choicesKeys = choices;
        }

        public override bool Execute()
        {
            var choiceHeader = Game1.LanguageManager.GetString(_choiceKey, "error");

            var choices = new string[_choicesKeys.Length];
            for (var i = 0; i < _choicesKeys.Length; i++)
                choices[i] = Game1.LanguageManager.GetString(_choicesKeys[i], "error");

            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return false;

            Game1.GameManager.InGameOverlay.TextboxOverlay.StartChoice(_key, choiceHeader, choices);
            return true;
        }
    }

    class DialogActionSetVariable : DialogAction
    {
        private readonly string _key;
        private readonly string _value;

        public DialogActionSetVariable(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override bool Execute()
        {
            Game1.GameManager.SaveManager.SetString(_key, _value);
            return true;
        }
    }

    class DialogActionUpdateObjects : DialogAction
    {
        public override bool Execute()
        {
            Game1.GameManager.InGameOverlay.TextboxOverlay.UpdateObjects = true;
            return true;
        }
    }

    class DialogActionWait : DialogAction
    {
        private readonly string _key;
        private readonly string _value;

        public DialogActionWait(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override bool Execute()
        {
            return Game1.GameManager.SaveManager.GetString(_key) == _value;
        }
    }

    /// <summary>
    /// DialogAction used to stop the dialog path for a certain amount of time.
    /// </summary>
    class DialogActionCountdown : DialogAction
    {
        private readonly float Time;
        private float _counter;

        public DialogActionCountdown(float time)
        {
            Time = time;
        }

        public override void Init()
        {
            _counter = Time;
        }

        public override bool Execute()
        {
            _counter -= Game1.DeltaTime;
            return _counter <= 0;
        }
    }

    class DialogActionFreezePlayer : DialogAction
    {
        private readonly string _key;
        private readonly string _value;

        public DialogActionFreezePlayer(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override bool Execute()
        {
            MapManager.ObjLink.FreezePlayer();
            return Game1.GameManager.SaveManager.GetString(_key) == _value;
        }
    }

    class DialogActionFreezePlayerTime : DialogAction
    {
        private float _counter;
        private readonly float _time;

        public DialogActionFreezePlayerTime(int time)
        {
            _time = time;
        }

        public override void Init()
        {
            _counter = _time;
        }

        public override bool Execute()
        {
            // freeze the player while the time is running
            var finished = _counter <= 0;
            _counter -= Game1.DeltaTime;

            MapManager.ObjLink.FreezePlayer();

            return finished;
        }
    }

    class DialogActionLockPlayerTime : DialogAction
    {
        private float _counter;
        private readonly float _time;

        public DialogActionLockPlayerTime(int time)
        {
            _time = time;
        }

        public override void Init()
        {
            _counter = _time;
        }

        public override bool Execute()
        {
            // freeze the player while the time is running
            var finished = _counter <= 0;
            _counter -= Game1.DeltaTime;

            MapManager.ObjLink.SeqLockPlayer();

            return finished;
        }
    }

    /// <summary>
    /// Lock the player as long as the key is not yet set to the value
    /// </summary>
    class DialogActionLockPlayer : DialogAction
    {
        private string _key;
        private string _value;

        public DialogActionLockPlayer(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override bool Execute()
        {
            MapManager.ObjLink.SeqLockPlayer();
            return Game1.GameManager.SaveManager.GetString(_key) == _value;
        }
    }

    class DialogActionShake : DialogAction
    {
        private readonly int _time;
        private readonly int _maxX;
        private readonly int _maxY;
        private readonly float _shakeSpeedX;
        private readonly float _shakeSpeedY;

        public DialogActionShake(int time, int maxX, int maxY, float shakeSpeedX, float shakeSpeedY)
        {
            _time = time;
            _maxX = maxX;
            _maxY = maxY;
            _shakeSpeedX = shakeSpeedX;
            _shakeSpeedY = shakeSpeedY;
        }

        public override bool Execute()
        {
            Game1.GameManager.ShakeScreen(_time, _maxX, _maxY, _shakeSpeedX, _shakeSpeedY);
            return true;
        }
    }

    class DialogActionStopMusic : DialogAction
    {
        public override bool Execute()
        {
            Game1.GbsPlayer.Stop();
            return true;
        }
    }

    class DialogActionStopMusicTime : DialogAction
    {
        private readonly int _time;
        private readonly int _priority;

        public DialogActionStopMusicTime(int time, int priority)
        {
            _time = time;
            _priority = priority;
        }

        public override bool Execute()
        {
            Game1.GameManager.StopMusic(_time, _priority);
            return true;
        }
    }

    class DialogActionPlayMusic : DialogAction
    {
        private readonly int _songNr;
        private readonly int _priority;

        public DialogActionPlayMusic(int songNr, int priority)
        {
            _songNr = songNr;
            _priority = priority;
        }

        public override bool Execute()
        {
            if (_priority < 0)
            {
                Game1.GameManager.StopMusic();
                return true;
            }

            // play the music after the transition if the game is transitioning
            if (MapManager.ObjLink.IsTransitioning)
                Game1.GameManager.MapManager.NextMap.MapMusic[_priority] = _songNr;
            else
            {
                Game1.GameManager.SetMusic(_songNr, _priority);
                if (_songNr >= 0)
                    Game1.GbsPlayer.Play();
            }

            return true;
        }
    }

    class DialogActionMusicSpeed : DialogAction
    {
        private readonly float _playbackSpeed;

        public DialogActionMusicSpeed(float playbackSpeed)
        {
            _playbackSpeed = playbackSpeed;
        }

        public override bool Execute()
        {
#if WINDOWS
            Game1.GbsPlayer.Cpu.SetPlaybackSpeed(_playbackSpeed);
#endif
            return true;
        }
    }

    class DialogActionSoundEffect : DialogAction
    {
        private readonly string _soundEffect;

        public DialogActionSoundEffect(string soundEffect)
        {
            _soundEffect = soundEffect;
        }

        public override bool Execute()
        {
            Game1.GameManager.PlaySoundEffect(_soundEffect);
            return true;
        }
    }

    class DialogActionCheckItem : DialogAction
    {
        private readonly string _itemName;
        private readonly string _resultKey;
        private readonly int _count;

        public DialogActionCheckItem(string itemName, int count, string resultKey)
        {
            _itemName = itemName;
            _count = count;
            _resultKey = resultKey;
        }

        public override bool Execute()
        {
            // get the item and check if enough are available
            var item = Game1.GameManager.GetItem(_itemName);
            var checkState = item != null && item.Count >= _count;

            Game1.GameManager.SaveManager.SetString(_resultKey, checkState ? "1" : "0");
            return true;
        }
    }

    class DialogActionCooldown : DialogAction
    {
        private readonly string _resultKey;
        private readonly int _cooldownTime;

        private double _lastExecutionTime;

        public DialogActionCooldown(int cooldownTime, string resultKey)
        {
            _cooldownTime = cooldownTime;
            _resultKey = resultKey;
            _lastExecutionTime = -_cooldownTime;
        }

        public override bool Execute()
        {
            // check when the last time the check was successful
            // this does not work directly after loading the save if it was running shortly after the last save loading
            if (_lastExecutionTime <= Game1.TotalGameTime && Game1.TotalGameTime < _lastExecutionTime + _cooldownTime)
            {
                Game1.GameManager.SaveManager.SetString(_resultKey, "0");
                return true;
            }

            _lastExecutionTime = Game1.TotalGameTime;

            Game1.GameManager.SaveManager.SetString(_resultKey, "1");
            return true;
        }
    }

    class DialogActionAddItem : DialogAction
    {
        private readonly string _itemName;
        private readonly int _amount;

        public DialogActionAddItem(string itemName, int amount)
        {
            _itemName = itemName;
            _amount = amount;
        }

        public override bool Execute()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return false;

            var item = new GameItemCollected(_itemName);

            // use the set amount or that from the item description
            if (_amount > 0)
                item.Count = _amount;
            else
                item.Count = Game1.GameManager.ItemManager[_itemName].Count;

            MapManager.ObjLink.PickUpItem(item, true);
            return true;
        }
    }

    class DialogActionSeqSetPosition : DialogAction
    {
        private string _animatorId;
        private Vector2 _newPosition;

        public DialogActionSeqSetPosition(string animatorId, Vector2 newPosition)
        {
            _animatorId = animatorId;
            _newPosition = newPosition;
        }

        public override bool Execute()
        {
            var gameSequence = Game1.GameManager.InGameOverlay.GetCurrentGameSequence();

            if (gameSequence == null)
                return false;

            gameSequence.SetPosition(_animatorId, _newPosition);
            return true;
        }
    }

    class DialogActionSeqLerp : DialogAction
    {
        private string _drawableId;
        private Vector2 _targetPosition;
        private float _moveSpeed;

        public DialogActionSeqLerp(string animatorId, Vector2 newPosition, float moveSpeed)
        {
            _drawableId = animatorId;
            _targetPosition = newPosition;
            _moveSpeed = moveSpeed;
        }

        public override bool Execute()
        {
            var gameSequence = Game1.GameManager.InGameOverlay.GetCurrentGameSequence();

            if (gameSequence == null)
                return false;

            gameSequence.StartPositionTransition(_drawableId, _targetPosition, _moveSpeed);
            return true;
        }
    }

    class DialogActionSeqColorLerp : DialogAction
    {
        private string _drawableId;
        private Color _targetColor;
        private int _transitionTime;

        public DialogActionSeqColorLerp(string animatorId, Color targetColor, int transitionTime)
        {
            _drawableId = animatorId;
            _targetColor = targetColor;
            _transitionTime = transitionTime;
        }

        public override bool Execute()
        {
            var gameSequence = Game1.GameManager.InGameOverlay.GetCurrentGameSequence();

            if (gameSequence == null)
                return false;

            gameSequence.StartColorTransition(_drawableId, _targetColor, _transitionTime);
            return true;
        }
    }

    class DialogActionSeqPlay : DialogAction
    {
        private string _animatorId;
        private string _animationId;

        public DialogActionSeqPlay(string animatorId, string animationId)
        {
            _animatorId = animatorId;
            _animationId = animationId;
        }

        public override bool Execute()
        {
            var gameSequence = Game1.GameManager.InGameOverlay.GetCurrentGameSequence();

            if (gameSequence == null)
                return false;

            gameSequence.PlayAnimation(_animatorId, _animationId);
            return true;
        }
    }

    class DialogActionFinishAnimation : DialogAction
    {
        private string _animatorId;
        private int _stopFrameIndex;

        public DialogActionFinishAnimation(string animatorId, int stopFrameIndex)
        {
            _animatorId = animatorId;
            _stopFrameIndex = stopFrameIndex;
        }

        public override bool Execute()
        {
            var gameSequence = Game1.GameManager.InGameOverlay.GetCurrentGameSequence();

            if (gameSequence == null)
                return false;

            gameSequence.FinishAnimation(_animatorId, _stopFrameIndex);
            return true;
        }
    }

    /// <summary>
    /// Only add an amount to an existing item
    /// </summary>
    class DialogActionAddItemAmount : DialogAction
    {
        private readonly string _itemName;
        private readonly int _amount;

        public DialogActionAddItemAmount(string itemName, int amount)
        {
            _itemName = itemName;
            _amount = amount;
        }

        public override bool Execute()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return false;

            if (Game1.GameManager.GetItem(_itemName) == null)
                return true;

            var item = new GameItemCollected(_itemName);
            if (_amount > 0)
                item.Count = _amount;

            MapManager.ObjLink.PickUpItem(item, false, false, false);
            return true;
        }
    }

    class DialogActionRemoveItem : DialogAction
    {
        private readonly string _itemName;
        private readonly string _resultKey;
        private readonly int _count;

        public DialogActionRemoveItem(string itemName, int count, string resultKey)
        {
            _itemName = itemName;
            _count = count;
            _resultKey = resultKey;
        }

        public override bool Execute()
        {
            // remove the item if possible
            if (Game1.GameManager.RemoveItem(_itemName, _count))
                Game1.GameManager.SaveManager.SetString(_resultKey, "1");
            else
                Game1.GameManager.SaveManager.SetString(_resultKey, "0");

            return true;
        }
    }

    class DialogActionBuyItem : DialogAction
    {
        private readonly string _key;

        public DialogActionBuyItem(string key)
        {
            _key = key;
        }

        public override bool Execute()
        {
            var itemName = Game1.GameManager.SaveManager.GetString("itemShopItem");
            var itemPriceString = Game1.GameManager.SaveManager.GetString("itemShopPrice");
            var itemPrice = int.Parse(itemPriceString);
            var itemCountString = Game1.GameManager.SaveManager.GetString("itemShopCount");
            var itemCount = int.Parse(itemCountString);

            var baseItem = Game1.GameManager.ItemManager[itemName];
            var buyItem = Game1.GameManager.GetItem(baseItem.Name);
            var rubyItem = Game1.GameManager.GetItem("ruby");

            var ownedCount = 0;
            var maxCount = 99;

            // check if the player has the mirror shield
            if (itemName == "arrow")
                buyItem = Game1.GameManager.GetItem("bow");
            if (itemName == "shield" && buyItem == null)
                buyItem = Game1.GameManager.GetItem("mirrorShield");

            if (itemName == "heart")
            {
                ownedCount = Game1.GameManager.CurrentHealth;
                maxCount = Game1.GameManager.MaxHearths * 4;
            }
            else if (buyItem != null)
            {
                ownedCount = buyItem.Count;
                maxCount = Game1.GameManager.ItemManager[buyItem.Name].MaxCount;
            }

            if (buyItem != null && buyItem.Name == "powder" && Game1.GameManager.SaveManager.GetString("upgradePowder") == "1")
                maxCount += 20;
            if (buyItem != null && buyItem.Name == "bomb" && Game1.GameManager.SaveManager.GetString("upgradeBomb") == "1")
                maxCount += 30;
            if (buyItem != null && buyItem.Name == "bow" && Game1.GameManager.SaveManager.GetString("upgradeBow") == "1")
                maxCount += 30;

            // does the player already own the item?
            if (ownedCount >= maxCount)
            {
                Game1.GameManager.SaveManager.SetString(_key, "2");
            }
            // does the player have enough money to buy this item?
            else if (rubyItem != null && rubyItem.Count >= itemPrice)
            {
                var item = new GameItemCollected(itemName);
                item.Count = itemCount;

                // gets picked up
                MapManager.ObjLink.PickUpItem(item, false);

                rubyItem.Count -= itemPrice;

                Game1.GameManager.SaveManager.SetString(_key, "0");
            }
            // player does not have enough money
            else
            {
                Game1.GameManager.SaveManager.SetString(_key, "1");
            }

            return true;
        }
    }

    class DialogActionOpenBook : DialogAction
    {
        public override bool Execute()
        {
            Game1.GameManager.InGameOverlay.OpenPhotoOverlay();
            return true;
        }
    }

    class DialogActionStartSequence : DialogAction
    {
        private readonly string _sequenceName;

        public DialogActionStartSequence(string sequenceName)
        {
            _sequenceName = sequenceName;
        }

        public override bool Execute()
        {
            Game1.GameManager.InGameOverlay.StartSequence(_sequenceName);
            return true;
        }
    }

    class DialogActionCloseOverlay : DialogAction
    {
        public DialogActionCloseOverlay() { }

        public override bool Execute()
        {
            Game1.GameManager.InGameOverlay.CloseOverlay();
            return true;
        }
    }

    class DialogActionFillHearts : DialogAction
    {
        public DialogActionFillHearts() { }

        public override bool Execute()
        {
            var fullHearts = Game1.GameManager.CurrentHealth >= Game1.GameManager.MaxHearths * 4;
            Game1.GameManager.SaveManager.SetString("fullHearts", fullHearts ? "1" : "0");
            Game1.GameManager.HealPlayer(99);
            ItemDrawHelper.EnableHeartAnimationSound();
            return true;
        }
    }

    class DialogActionSpawnObject : DialogAction
    {
        private readonly string _positionKey;
        private readonly string _objectId;
        private readonly string _strParameter;

        public DialogActionSpawnObject(string positionKey, string objectId, string strParameter)
        {
            _positionKey = positionKey;
            _objectId = objectId;
            _strParameter = strParameter;
        }

        public override bool Execute()
        {
            // @HACK: this is not really a good way and could lead to problems
            // a better way would probably be a way to pass parameters into a dialog path so that the actions could read them

            // @HACK: some parameters need the '.' character
            var parameters = _strParameter.Split('.');
            for (var i = 0; i < parameters.Length; i++)
                parameters[i] = parameters[i].Replace("$", ".");

            var objectParameter = MapData.GetParameter(_objectId, parameters);
            objectParameter[0] = ObjPositionDialog.CurrentMap;
            objectParameter[1] = Game1.GameManager.SaveManager.GetInt(_positionKey + "posX");
            objectParameter[2] = Game1.GameManager.SaveManager.GetInt(_positionKey + "posY");

            ObjPositionDialog.CurrentMap.Objects.SpawnObject(_objectId, objectParameter);

            return true;
        }
    }

    class DialogActionChangeMap : DialogAction
    {
        private readonly string _mapName;
        private readonly string _entryName;

        public DialogActionChangeMap(string mapName, string entryName)
        {
            _mapName = mapName;
            _entryName = entryName;
        }

        public override bool Execute()
        {
            var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
            transitionSystem.AppendMapChange(_mapName, _entryName, false, false, Color.White, true);
            transitionSystem.SetColorMode(Color.White, 1);

            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.TransitionOutWalking = false;

            return true;
        }
    }

    class DialogActionSaveHistory : DialogAction
    {
        private readonly bool _enable;

        public DialogActionSaveHistory(bool enable)
        {
            _enable = enable;
        }

        public override bool Execute()
        {
            if (_enable && !Game1.GameManager.SaveManager.HistoryEnabled)
            {
                SaveGameSaveLoad.FillSaveState(Game1.GameManager);
                Game1.GameManager.SaveManager.EnableHistory();
            }
            // the history will be cleared by the player
            else if (!_enable && !MapManager.ObjLink.SavePreItemPickup)
            {
                SaveGameSaveLoad.ClearSaveState();
                Game1.GameManager.SaveManager.DisableHistory();
            }

            return true;
        }
    }

    class DialogPath
    {
        public string VariableKey;
        public string Condition;

        public List<DialogAction> Action = new List<DialogAction>();

        public DialogPath(string variableKey, string condition)
        {
            VariableKey = variableKey;
            Condition = condition;
        }

        public DialogPath(string variableKey)
        {
            VariableKey = variableKey;
        }
    }
}
