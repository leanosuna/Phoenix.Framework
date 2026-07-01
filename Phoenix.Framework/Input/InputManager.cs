using Silk.NET.Input;
using System.Numerics;

namespace Phoenix.Framework.Input
{
    public class InputManager
    {
        private PhoenixGame _game;
        private IInputContext _input;
        private List<IKeyboard> _keyboards = new List<IKeyboard>();
        private List<IMouse> _mice = new List<IMouse>();
        private Vector2[] _lastMousePositions = Array.Empty<Vector2>();

        public float MouseSensitivity = .001f;
        public Vector2 MouseDelta = Vector2.Zero;
        public float MouseWheelValue = 0;
        public InputManager(PhoenixGame game)
        {
            _game = game;
            _input = _game.Window.CreateInput();

            _keyboards = _input.Keyboards.ToList();
            _mice = _input.Mice.ToList();

            for (int i = 0; i < _mice.Count; i++)
            {
                _mice[i].Cursor.CursorMode = CursorMode.Raw;
                _mice[i].Scroll += OnMouseWheel;
            }

            _lastMousePositions = new Vector2[_mice.Count];
            for (int i = 0; i < _mice.Count; i++)
            {
                _mice[i].Position = (Vector2)game.Window.Position + (Vector2)game.Window.Size / 2;
                _lastMousePositions[i] = _mice[i].Position;
            }

            MouseDelta = Vector2.Zero;

        }

        public IInputContext GetInputContext() { return _input; }

        public void SetMouseMode(CursorMode mode)
        {
            for (int i = 0; i < _mice.Count; i++)
            {
                _mice[i].Position = (Vector2)_game.Window.Size / 2;
                _lastMousePositions[i] = _mice[i].Position;
                _mice[i].Cursor.CursorMode = mode;

            }
        }
        public void ToggleMouseMode()
        {
            var mode = _mice.Count > 0 && _mice[0].Cursor.CursorMode == CursorMode.Normal ? CursorMode.Raw : CursorMode.Normal;
            SetMouseMode(mode);
        }

        public void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            MouseWheelValue -= scrollWheel.Y;
        }

        static List<Key> keysDown = new List<Key>();
        static List<MouseButton> buttonsDown = new List<MouseButton>();
        public void Update()
        {
            keysDown.RemoveAll(k => !_keyboards.Any(kb => kb.IsKeyPressed(k)));

            MouseDelta = Vector2.Zero;
            for (int i = 0; i < _mice.Count; i++)
            {
                MouseDelta.X += (float)(_mice[i].Position.X - _lastMousePositions[i].X) * MouseSensitivity;
                MouseDelta.Y += (float)(_mice[i].Position.Y - _lastMousePositions[i].Y) * MouseSensitivity;
                _lastMousePositions[i] = _mice[i].Position;
            }

            buttonsDown.RemoveAll(b => !_mice.Any(m => m.IsButtonPressed(b)));

        }
        public bool KeyDown(Key key)
        {
            return _keyboards.Any(kb => kb.IsKeyPressed(key));
        }

        public bool KeyDownOnce(Key key)
        {
            if (_keyboards.Any(kb => kb.IsKeyPressed(key)) && !keysDown.Contains(key))
            {
                keysDown.Add(key);
                return true;
            }
            return false;
        }

        public bool MouseDown(MouseButton button)
        {
            return _mice.Any(m => m.IsButtonPressed(button));
        }

        public bool MouseDownOnce(MouseButton button)
        {
            if (_mice.Any(m => m.IsButtonPressed(button)) && !buttonsDown.Contains(button))
            {
                buttonsDown.Add(button);
                return true;
            }
            return false;
        }

        public bool MouseLeftDown() => MouseDown(MouseButton.Left);
        public bool MouseRightDown() => MouseDown(MouseButton.Right);
        public bool MouseLeftDownOnce() => MouseDownOnce(MouseButton.Left);
        public bool MouseRightDownOnce() => MouseDownOnce(MouseButton.Right);

        CursorMode _beforeTemp;
        
        internal void SetTemporaryMouseMode(CursorMode mode)
        {
            _beforeTemp = _mice.Count > 0 ? _mice[0].Cursor.CursorMode : CursorMode.Normal;

            SetMouseMode(mode);
        }

        internal void RestoreMouseMode()
        {
            SetMouseMode(_beforeTemp);
        }
    }
}
