using Silk.NET.Input;
using System.Numerics;

namespace Phoenix.Framework.Input
{
    public class InputManager
    {
        private PhoenixGame _game;
        private IInputContext _input;
        private IKeyboard _keyboard = default!;
        private IMouse _mouse;
        private Vector2 _lastMousePosition;

        public float MouseSensitivity = .001f;
        public Vector2 MouseDelta = Vector2.Zero;
        public float MouseWheelValue = 0;
        public InputManager(PhoenixGame game)
        {
            _game = game;
            _input = _game.Window.CreateInput();

            //TODO: handle all mice/keyboard inputs
            _keyboard = _input.Keyboards.FirstOrDefault()!;
            //for (int i = 0; i < input.Mice.Count; i++)
            //{
            //    input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
            //    input.Mice[i].MouseMove += OnMouseMove;
            //    input.Mice[i].Scroll += OnMouseWheel;
            //}

            _mouse = _input.Mice.FirstOrDefault()!;
            _mouse.Cursor.CursorMode = CursorMode.Raw;
            _mouse.Position = (Vector2)game.Window.Position + (Vector2)game.Window.Size / 2;

            _lastMousePosition = _mouse.Position;
            MouseDelta = Vector2.Zero;

        }

        public IInputContext GetInputContext() { return _input; }

        public void SetMouseMode(CursorMode mode)
        {
            for (int i = 0; i < _input.Mice.Count; i++)
            {
                _input.Mice[i].Position = (Vector2)_game.Window.Size / 2;
                _input.Mice[i].Cursor.CursorMode = mode;

            }
        }
        public void ToggleMouseMode()
        {
            var m = _mouse.Cursor.CursorMode == CursorMode.Normal ? CursorMode.Raw : CursorMode.Normal;
            //Console.WriteLine($"Mouse mode: {m}");
            SetMouseMode(m);
        }

        public void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            MouseWheelValue -= scrollWheel.Y;
        }

        static List<Key> keysDown = new List<Key>();
        public void Update()
        {
            keysDown.RemoveAll(k => !_keyboard.IsKeyPressed(k));

            MouseDelta.X = (float)(_mouse.Position.X - _lastMousePosition.X) * MouseSensitivity;
            MouseDelta.Y = (float)(_mouse.Position.Y - _lastMousePosition.Y) * MouseSensitivity;
            _lastMousePosition = _mouse.Position;

        }
        public bool KeyDown(Key key)
        {
            return _keyboard.IsKeyPressed(key);
        }

        public bool KeyDownOnce(Key key)
        {
            if (_keyboard.IsKeyPressed(key) && !keysDown.Contains(key))
            {
                keysDown.Add(key);
                return true;
            }
            return false;
        }

        CursorMode _beforeTemp;
        
        internal void SetTemporaryMouseMode(CursorMode mode)
        {
            _beforeTemp = _input.Mice[0].Cursor.CursorMode;

            SetMouseMode(mode);
        }

        internal void RestoreMouseMode()
        {
            SetMouseMode(_beforeTemp);
        }
    }
}
