using ProjectZ.InGame.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectZ.InGame.SaveLoad
{
    internal class SaveStateManager
    {
        public class SaveState
        {
            public string Name;
            public int MaxHearth;
            public int CurrentHearth;
            public int CurrentRubee;
        }

        public static SaveState[] SaveStates = new SaveState[SaveCount];

        public const int SaveCount = 4;

        public static void LoadSaveData()
        {
            for (var i = 0; i < SaveCount; i++)
            {
                var saveManager = new SaveManager();

                // check if the save was loaded or not
                if (saveManager.LoadFile(Values.PathSaveFolder + "/" + SaveGameSaveLoad.SaveFileName + i))
                {
                    SaveStates[i] = new SaveState();
                    SaveStates[i].Name = saveManager.GetString("savename");
                    SaveStates[i].CurrentHearth = saveManager.GetInt("currentHearth");
                    SaveStates[i].MaxHearth = saveManager.GetInt("maxHearth");
                    SaveStates[i].CurrentRubee = saveManager.GetInt("rubyCount", 0);
                }
                else
                    SaveStates[i] = null;
            }
        }
    }
}
