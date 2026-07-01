using Phoenix.Framework.Maths;

using System.Numerics;

namespace Phoenix.Framework.Cameras
{
    public abstract class MouseCamera : BaseCamera
    {
        public float MoveSpeed = 10f;
        public bool MouseAim = true;
        internal PhoenixGame _game;

        public MouseCamera(PhoenixGame game, Vector3 position, float yaw, float pitch, float fov, float nearPlane, float farPlane, float aspectRatio)
           : base(position, yaw, pitch, fov, nearPlane, farPlane, aspectRatio)
        {
            _game = game;
        }

        public override void Update(double deltaTime)
        {
            CalculateMouseAim();
        }

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
}
