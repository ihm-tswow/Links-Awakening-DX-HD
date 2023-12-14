using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Overlay;

namespace ProjectZ.InGame.SaveLoad
{
    class DialogPathLoader
    {
        public static void LoadScripts(string filePath, Dictionary<string, List<DialogPath>> dialogPaths)
        {
            var reader = new StreamReader(filePath);

            // go from line to line
            while (!reader.EndOfStream)
            {
                var strLine = reader.ReadLine().Replace(" ", "");

                // ignore comment
                if (strLine.Length == 0 || strLine.StartsWith("//"))
                    continue;

                var split = strLine.Split(new[] { "->" }, StringSplitOptions.None);

                if (split.Length <= 1) continue;

                var splitKey = split[0].Split(':');

                DialogPath newPath;
                if (splitKey.Length == 2)
                    newPath = new DialogPath(splitKey[0], splitKey[1]);
                else
                    newPath = new DialogPath(splitKey[1], splitKey[2]);

                for (var i = 1; i < split.Length; i++)
                    AddAction(newPath, split[i], splitKey[0]);

                // create dictionary entry or create one
                if (dialogPaths.ContainsKey(splitKey[0]))
                    dialogPaths[splitKey[0]].Add(newPath);
                else
                    dialogPaths.Add(splitKey[0], new List<DialogPath> { newPath });
            }

            reader.Close();
        }

        private static void AddAction(DialogPath path, string split, string key)
        {
            if (!split.Contains('[') && split.Length > 0)
            {
                path.Action.Add(new DialogActionStartDialog(split));
                return;
            }

            var action = split.Replace("[", "").Replace("]", "");
            var stringSplit = action.Split(':');

            if (stringSplit[0] == "path")
            {
                path.Action.Add(new DialogActionStartPath(stringSplit[1]));
            }
            else if (stringSplit[0] == "wait")
            {
                path.Action.Add(new DialogActionWait(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "countdown")
            {
                var time = int.Parse(stringSplit[1]);
                path.Action.Add(new DialogActionCountdown(time));
            }
            else if (stringSplit[0] == "update_objects")
            {
                path.Action.Add(new DialogActionUpdateObjects());
            }
            else if (stringSplit[0] == "freeze" && stringSplit.Length == 2)
            {
                path.Action.Add(new DialogActionFreezePlayerTime(int.Parse(stringSplit[1])));
            }
            else if (stringSplit[0] == "freeze" && stringSplit.Length == 3)
            {
                path.Action.Add(new DialogActionFreezePlayer(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "lock_player" && stringSplit.Length == 2)
            {
                path.Action.Add(new DialogActionLockPlayerTime(int.Parse(stringSplit[1])));
            }
            else if (stringSplit[0] == "lock_player" && stringSplit.Length == 3)
            {
                path.Action.Add(new DialogActionLockPlayer(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "shake" && stringSplit.Length == 6)
            {
                path.Action.Add(new DialogActionShake(
                    int.Parse(stringSplit[1]), int.Parse(stringSplit[2]), int.Parse(stringSplit[3]),
                    float.Parse(stringSplit[4], CultureInfo.InvariantCulture),
                    float.Parse(stringSplit[5], CultureInfo.InvariantCulture)));
            }
            else if (stringSplit[0] == "set")
            {
                if (stringSplit.Length == 2)
                    path.Action.Add(new DialogActionSetVariable(key, stringSplit[1]));
                else
                    path.Action.Add(new DialogActionSetVariable(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "check_item")
            {
                var itemCount = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionCheckItem(stringSplit[1], itemCount, stringSplit[3]));
            }
            else if (stringSplit[0] == "cooldown")
            {
                var cooldownTime = int.Parse(stringSplit[1]);
                path.Action.Add(new DialogActionCooldown(cooldownTime, stringSplit[2]));
            }
            else if (stringSplit[0] == "add_item")
            {
                var itemCount = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionAddItem(stringSplit[1], itemCount));
            }
            else if (stringSplit[0] == "add_item_amount")
            {
                var itemCount = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionAddItemAmount(stringSplit[1], itemCount));
            }
            else if (stringSplit[0] == "remove_item")
            {
                var itemCount = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionRemoveItem(stringSplit[1], itemCount, stringSplit[3]));
            }
            else if (stringSplit[0] == "stop_music")
            {
                if (stringSplit.Length == 3)
                    path.Action.Add(new DialogActionStopMusicTime(int.Parse(stringSplit[1]), int.Parse(stringSplit[2])));
                else
                    path.Action.Add(new DialogActionStopMusic());
            }
            else if (stringSplit[0] == "music")
            {
                var songNr = int.Parse(stringSplit[1]);
                var priority = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionPlayMusic(songNr, priority));
            }
            else if (stringSplit[0] == "music_speed")
            {
                path.Action.Add(new DialogActionMusicSpeed(
                    float.Parse(stringSplit[1], CultureInfo.InvariantCulture)));
            }
            else if (stringSplit[0] == "sound")
            {
                path.Action.Add(new DialogActionSoundEffect(stringSplit[1]));
            }
            else if (stringSplit[0] == "dialog")
            {
                var choices = new string[stringSplit.Length - 3];
                for (var j = 0; j < stringSplit.Length - 3; j++)
                    choices[j] = stringSplit[j + 3];
                path.Action.Add(new DialogActionDialog(stringSplit[1], stringSplit[2], choices));
            }
            else if (stringSplit[0] == "buy")
            {
                path.Action.Add(new DialogActionBuyItem(stringSplit[1]));
            }
            else if (stringSplit[0] == "open_book")
            {
                path.Action.Add(new DialogActionOpenBook());
            }
            else if (stringSplit[0] == "start_sequence")
            {
                path.Action.Add(new DialogActionStartSequence(stringSplit[1]));
            }
            else if (stringSplit[0] == "seq_set_position")
            {
                var posX = int.Parse(stringSplit[2]);
                var posY = int.Parse(stringSplit[3]);
                path.Action.Add(new DialogActionSeqSetPosition(stringSplit[1], new Vector2(posX, posY)));
            }
            else if (stringSplit[0] == "seq_lerp")
            {
                var posX = int.Parse(stringSplit[2]);
                var posY = int.Parse(stringSplit[3]);
                var time = float.Parse(stringSplit[4], CultureInfo.InvariantCulture);
                path.Action.Add(new DialogActionSeqLerp(stringSplit[1], new Vector2(posX, posY), time));
            }
            else if (stringSplit[0] == "seq_color")
            {
                var colorR = byte.Parse(stringSplit[2]);
                var colorG = byte.Parse(stringSplit[3]);
                var colorB = byte.Parse(stringSplit[4]);
                var colorA = byte.Parse(stringSplit[5]);
                var time = int.Parse(stringSplit[6]);
                path.Action.Add(new DialogActionSeqColorLerp(stringSplit[1], new Color(colorR, colorG, colorB, colorA), time));
            }
            else if (stringSplit[0] == "seq_play")
            {
                path.Action.Add(new DialogActionSeqPlay(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "seq_finish_animation")
            {
                var stopFrameIndex = int.Parse(stringSplit[2]);
                path.Action.Add(new DialogActionFinishAnimation(stringSplit[1], stopFrameIndex));
            }
            else if (stringSplit[0] == "close_overlay")
            {
                path.Action.Add(new DialogActionCloseOverlay());
            }
            else if (stringSplit[0] == "fill_hearts")
            {
                path.Action.Add(new DialogActionFillHearts());
            }
            else if (stringSplit[0] == "spawn")
            {
                path.Action.Add(new DialogActionSpawnObject(key, stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "map_transition")
            {
                path.Action.Add(new DialogActionChangeMap(stringSplit[1], stringSplit[2]));
            }
            else if (stringSplit[0] == "save_history")
            {
                path.Action.Add(new DialogActionSaveHistory(bool.Parse(stringSplit[1])));
            }
        }
    }
}
