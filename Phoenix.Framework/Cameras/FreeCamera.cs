using Silk.NET.Input;
using System.Numerics;

namespace Phoenix.Framework.Cameras;

public class FreeCamera : MouseCamera
{
    private Key _forward = default!;
    private Key _backward = default!;
    private Key _left = default!;
    private Key _right = default!;
    private Key _up = default!;
    private Key _down = default!;
    private Key _speedModifierKey = default!;
    private float _speedModifier = default!;
    private bool _moveKeysSet = false;

    private Key _pitchUp = default!;
    private Key _pitchDown = default!;
    private Key _yawLeft = default!;
    private Key _yawRight = default!;
    private Vector2 _turnSpeed = Vector2.Zero;
    private bool _pitchYawKeysSet = false;

    /// <summary>
    /// Initializes a free-flying camera with position, orientation, field of view, and clipping plane parameters.
    /// </summary>
    public FreeCamera(PhoenixGame game, Vector3 position, float yaw, float pitch, float fov, float nearPlane, float farPlane, float aspectRatio)
        : base(game, position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
    {
    }

    /// <summary>
    /// Updates camera movement and view angles based on configured input keys.
    /// </summary>
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

    /// <summary>
    /// Configures keyboard keys used for translation and speed modification.
    /// </summary>
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

    /// <summary>
    /// Configures keyboard keys used for camera pitch and yaw rotation.
    /// </summary>
    public void SetPitchYawKeys(Key pitchUp, Key pitchDown, Key yawLeft, Key yawRight, Vector2 turnSpeed)
    {
        _pitchUp = pitchUp;
        _pitchDown = pitchDown;
        _yawLeft = yawLeft;
        _yawRight = yawRight;
        _turnSpeed = turnSpeed;
        _pitchYawKeysSet = true;
    }

    /// <summary>
    /// Translates camera position in world space based on active movement keys.
    /// </summary>
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

    /// <summary>
    /// Adjusts pitch and yaw angles based on active rotation keys.
    /// </summary>
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

