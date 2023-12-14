using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.Base.UI
{
    public class UiManager
    {
        public string CurrentScreen
        {
            get => _currentScreen;
            set => _currentScreen = value.ToUpper();
        }

        private readonly List<UiElement> _elementList = new List<UiElement>();

        private string _currentScreen;

        public void Update()
        {
            //remove elements
            _elementList.RemoveAll(element => element.Remove);

            foreach (var element in _elementList)
                if (element.Screens.Contains(_currentScreen))
                    element.Update();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _elementList.Count; i++)
                if (_elementList[i].Screens.Contains(_currentScreen))
                    if (_elementList[i].IsVisible)
                        _elementList[i].Draw(spriteBatch);
        }

        public void DrawBlur(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _elementList.Count; i++)
                if (_elementList[i].Screens.Contains(_currentScreen))
                    if (_elementList[i].IsVisible)
                        _elementList[i].DrawBlur(spriteBatch);
        }

        public void SizeChanged()
        {
            foreach (var uiElement in _elementList)
                uiElement.SizeUpdate?.Invoke(uiElement);
        }

        public UiElement AddElement(UiElement element)
        {
            if (element != null)
                _elementList.Add(element);

            return element;
        }

        public UiElement GetElement(string elementId)
        {
            //search for the elementId
            for (var i = 0; i < _elementList.Count; i++)
                if (_elementList[i].ElementId == elementId)
                    return _elementList[i];

            return null;
        }

        public void RemoveElement(string elementId, string screenId)
        {
            for (var i = 0; i < _elementList.Count; i++)
                if (_elementList[i].ElementId.Contains(elementId) && _elementList[i].Screens.Contains(screenId))
                    _elementList[i].Remove = true;
        }
    }
}
