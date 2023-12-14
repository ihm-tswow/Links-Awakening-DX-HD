using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.SaveLoad
{
    public class MapData
    {
        public static void AddObject(Map.Map map, GameObjectItem gameObject)
        {
            // this can be used to update gameobjects
            //if (gameObject.Index == "door")
            //{
            //    var parameterArray = GetParameterArray("newDoor");

            //    // set the object position
            //    parameterArray[1] = gameObject.Parameter[1];
            //    parameterArray[2] = gameObject.Parameter[2];

            //    parameterArray[5] = gameObject.Parameter[3];
            //    parameterArray[6] = gameObject.Parameter[4];
            //    parameterArray[7] = gameObject.Parameter[3];
            //    parameterArray[8] = gameObject.Parameter[5];

            //    var newDoor = new GameObjectItem("newDoor", parameterArray);

            //    map.Objects.ObjectList.Add(newDoor);
            //}

            //if (gameObject.Index == "gravestone" || gameObject.Index == "moveStone" ||
            //    gameObject.Index == "moveStoneCave")
            //{
            //    gameObject.Parameter[3] = 0x01 << (int)gameObject.Parameter[3];
            //}

            // @HACK: we need this value to be set before calling any object constructor
            if (gameObject.Index == "link2dspawner")
                map.Is2dMap = true;

            map.Objects.ObjectList.Add(gameObject);
        }

        public static object[] GetParameterArray(string objectId)
        {
            var objParameter = GameObjectTemplates.ObjectTemplates[objectId].Parameter;

            // object has only posX and posY as parameter
            if (objParameter == null)
                return new object[3];

            var outParameter = new object[objParameter.Length + 3];

            for (var i = 0; i < objParameter.Length; i++)
            {
                var parameter = objParameter[i];

                // arrays need to be cloned
                if (parameter is Array recArray)
                    outParameter[i + 3] = recArray.Clone();
                else
                    outParameter[i + 3] = parameter;
            }

            return outParameter;
        }

        public static object[] StringToParameter(string objectIndex, string[] parameters)
        {
            if (objectIndex == null || !GameObjectTemplates.ObjectTemplates.ContainsKey(objectIndex))
                return null;

            var parameterArray = GetParameterArray(objectIndex);

            // set the object position
            parameterArray[1] = Convert.ToInt32(parameters[1]);
            parameterArray[2] = Convert.ToInt32(parameters[2]);

            if (parameters.Length <= 3)
                return parameterArray;

            var parCount = Math.Min(parameters.Length, parameterArray.Length);
            for (var i = 3; i < parCount; i++)
                if (parameters[i].Length > 0)
                    parameterArray[i] = ConvertToObject(parameters[i], GameObjectTemplates.GameObjectParameter[objectIndex][i].ParameterType);

            return parameterArray;
        }

        public static object[] GetParameter(string objectIndex, string[] parameters)
        {
            if (objectIndex == null || !GameObjectTemplates.ObjectTemplates.ContainsKey(objectIndex))
                return null;

            var parameterArray = GetParameterArray(objectIndex);

            if (parameters == null)
                return parameterArray;

            var length = MathHelper.Min(parameters.Length, parameterArray.Length - 3);
            for (var i = 0; i < length; i++)
                if (parameters[i].Length > 0)
                    parameterArray[i + 3] = ConvertToObject(parameters[i], GameObjectTemplates.GameObjectParameter[objectIndex][i + 3].ParameterType);

            return parameterArray;
        }

        public static object ConvertToObject(string strInput, Type outputType)
        {
            object output;

            if (outputType == typeof(bool))
            {
                bool.TryParse(strInput, out var boolOutput);
                output = boolOutput;
            }
            else if (outputType == typeof(int))
            {
                int.TryParse(strInput, out var intOutput);
                output = intOutput;
            }
            else if (outputType == typeof(float))
            {
                float.TryParse(strInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatOutput);
                output = floatOutput;
            }
            else if (outputType == typeof(string))
            {
                output = strInput;
            }
            else if (outputType == typeof(Rectangle))
            {
                var split = strInput.Split('.');
                var outRectangle = new Rectangle(0, 0, 0, 0);

                if (split.Length == 4)
                {
                    if (split[0].Length > 0)
                        int.TryParse(split[0], out outRectangle.X);
                    if (split[1].Length > 0)
                        int.TryParse(split[1], out outRectangle.Y);
                    if (split[2].Length > 0)
                        int.TryParse(split[2], out outRectangle.Width);
                    if (split[3].Length > 0)
                        int.TryParse(split[3], out outRectangle.Height);
                }

                output = outRectangle;
            }
            else
            {
                output = null;
            }

            return output;
        }

        public static string GetObjectString(int index, string objectIndex, object[] parameter)
        {
            var strOutput = "";
            var originalParameter = GameObjectTemplates.ObjectTemplates[objectIndex].Parameter;

            for (var i = 1; i < parameter.Length; i++)
            {
                // only write the parameter that are not equal to the original ones
                if (i < 3 || !ParameterEqual(parameter[i], originalParameter[i - 3]))
                {
                    if (parameter[i] is bool || parameter[i] is int || parameter[i] is string)
                        strOutput += parameter[i];
                    else if (parameter[i] is float)
                        strOutput += ((float) parameter[i]).ToString(CultureInfo.InvariantCulture);
                    else if (parameter[i] is Rectangle rectangle)
                        strOutput += rectangle.X + "." + rectangle.Y + "." + rectangle.Width + "." + rectangle.Height;
                    else
                    {
                        Debug.Fail("tried to save not supported type " + objectIndex + " argument " + i);
                    }
                }

                // saves space
                if (i < parameter.Length - 1)
                    strOutput += ";";
            }

            return index + ";" + strOutput;
        }

        public static bool ParameterEqual(object parameterOne, object parameterTwo)
        {
            if (parameterOne == null && parameterTwo == null)
                return true;

            if (parameterOne == null || parameterTwo == null)
                return false;

            // type does not match
            // is this even possible?
            if (parameterOne.GetType() != parameterTwo.GetType())
                return false;

            if (parameterOne is bool || parameterOne is int || parameterOne is float || parameterOne is string ||
                parameterOne is Vector2 || parameterOne is Rectangle || parameterOne is Color || parameterOne is Texture2D || parameterOne is Values.CollisionTypes)
                return parameterOne.Equals(parameterTwo);
            else if (parameterOne is Rectangle[])
            {
                if (((Rectangle[])parameterOne).Length != ((Rectangle[])parameterTwo).Length)
                    return false;

                for (var i = 0; i < ((Rectangle[])parameterOne).Length; i++)
                {
                    if (((Rectangle[])parameterOne)[i] != ((Rectangle[])parameterTwo)[i])
                        return false;
                }
            }
            else
            {
                Debug.Fail("can not compare objects from type " + parameterOne.GetType() + "; need to implement missing type");
            }

            return true;
        }
    }
}
