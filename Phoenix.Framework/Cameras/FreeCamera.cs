using Silk.NET.Input;
using System.Numerics;

namespace Phoenix.Framework.Cameras
{
    public class FreeCamera : MouseCamera
    {
        Key _forward = default!;
        Key _backward = default!;
        Key _left = default!;
        Key _right = default!;
        Key _up = default!;
        Key _down = default!;
        Key _speedModifierKey = default!;
        float _speedModifier = default!;
        bool _moveKeysSet = false;

        Key _pitchUp = default!;
        Key _pitchDown = default!;
        Key _yawLeft = default!;
        Key _yawRight = default!;
        Vector2 _turnSpeed = Vector2.Zero;
        bool _pitchYawKeysSet = false;
        public FreeCamera(PhoenixGame game, Vector3 position, float yaw, float pitch, float fov, float nearPlane, 
            float farPlane, float aspectRatio)
            : base(game, position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
        {

        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            var viewChanged = HandleMoveKeys(deltaTime);
            var vectorsChanged = HandlePitchYawKeys(deltaTime);
            
            if (vectorsChanged)
                CalculateVectors();

            if (viewChanged || vectorsChanged)
                CalculateView();
        }
        public void SetMoveKeys(Key forward, Key backward, Key left, Key right, Key up, Key down, Key speedModifierKey, float speedModifier)
        {
            _forward = forward;
            _backward = backward;
            _left = left;
            _right = right;
            _up = up;
            _down = down;
            _speedModifierKey = speedModifierKey;
            _speedModifier = speedModifier;
            _moveKeysSet = true;
        }
        public void SetPitchYawKeys(Key pitchUp, Key pitchDown, Key yawLeft, Key yawRight, Vector2 turnSpeed)
        {
            _pitchUp = pitchUp;
            _pitchDown = pitchDown;
            _yawLeft = yawLeft;
            _yawRight = yawRight;
            _turnSpeed = turnSpeed;
            _pitchYawKeysSet = true;
        }
        private bool HandleMoveKeys(double deltaTime)
        {
            if (!_moveKeysSet)
                return false;

            var dir = Vector3.Zero;

            dir += Front * (_game.Input.KeyDown(_forward) ? 1 : 0);
            dir -= Front * (_game.Input.KeyDown(_backward) ? 1 : 0);
            dir -= Right * (_game.Input.KeyDown(_left) ? 1 : 0);
            dir += Right * (_game.Input.KeyDown(_right) ? 1 : 0);
            dir += Up * (_game.Input.KeyDown(_up) ? 1 : 0);
            dir -= Up * (_game.Input.KeyDown(_down) ? 1 : 0);

            if (dir != Vector3.Zero)
            {
                dir = Vector3.Normalize(dir);

                var speed = MoveSpeed;

                if (_game.Input.KeyDown(_speedModifierKey))
                    speed *= _speedModifier;
                Position += dir * speed * (float)deltaTime;

                return true;
            }
            return false;
        }

        private bool HandlePitchYawKeys(double deltaTime)
        {
            if (!_pitchYawKeysSet)
                return false;
            bool res = false;

            if (_game.Input.KeyDown(_pitchUp))
            {
                Pitch += _turnSpeed.Y * (float)deltaTime;
                res = true;
            }
            if (_game.Input.KeyDown(_pitchDown))
            {
                Pitch -= _turnSpeed.Y * (float)deltaTime;
                res = true;
            }
            if (_game.Input.KeyDown(_yawRight))
            {
                Yaw += _turnSpeed.X * (float)deltaTime;
                res = true;
            }
            if (_game.Input.KeyDown(_yawLeft))
            {
                Yaw -= _turnSpeed.X * (float)deltaTime;
                res = true;
            }

            return res;
        }


    }
}
