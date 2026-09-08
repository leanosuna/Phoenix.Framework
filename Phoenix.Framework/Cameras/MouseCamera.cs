using Phoenix.Framework.Maths;
using System.Numerics;

namespace Phoenix.Framework.Cameras;

public abstract class MouseCamera : BaseCamera
{
    public float MoveSpeed = 10f;
    public bool MouseAim = true;
    internal PhoenixGame _game;

    /// <summary>
    /// Initializes a mouse-controlled camera bound to the game instance.
    /// </summary>
    public MouseCamera(PhoenixGame game, Vector3 position, float yaw, float pitch, float fov, float nearPlane, float farPlane, float aspectRatio)
        : base(position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
    {
        _game = game;
    }

    /// <summary>
    /// Updates camera orientation from mouse delta inputs.
    /// </summary>
    public override void Update(double deltaTime)
    {
        CalculateMouseAim();
    }

    /// <summary>
    /// Reads mouse delta and adjusts yaw and pitch angles accordingly.
    /// </summary>
    protected void CalculateMouseAim()
    {
        if (!MouseAim)
            return;

        var mouseDelta = _game.Input.MouseDelta;
        if (mouseDelta != Vector2.Zero)
        {
            Yaw += mouseDelta.X;
            Pitch -= mouseDelta.Y;

            var maxAbs = MathHelper.PiOver2 - 0.0001f;

            Pitch = Math.Clamp(Pitch, -maxAbs, maxAbs);

            CalculateVectors();
            CalculateView();
        }
    }
}

