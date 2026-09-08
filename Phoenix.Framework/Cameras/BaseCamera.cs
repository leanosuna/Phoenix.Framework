using System.Numerics;

namespace Phoenix.Framework.Cameras;

public abstract class BaseCamera : Camera
{
    public override Vector3 Position { get; set; }
    public Vector3 Front;
    public Vector3 Up;
    public Vector3 Right;
    public float Yaw;
    public float Pitch;
    public float FOV;
    public float AspectRatio;
    public float NearPlane;
    public float FarPlane;
    private Vector3 _tempFront;

    /// <summary>
    /// Initializes a camera with position, orientation, field of view, and clipping plane parameters.
    /// </summary>
    public BaseCamera(Vector3 position, float yaw, float pitch, float fov, float nearPlane, float farPlane, float aspectRatio)
    {
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
        FOV = fov;
        NearPlane = nearPlane;
        FarPlane = farPlane;
        AspectRatio = aspectRatio;
        Up = Vector3.UnitY;
        CalculateVectors();
        CalculateView();
        CalculateProjection();
    }

    /// <summary>
    /// Computes front and right directional vectors from current yaw and pitch angles.
    /// </summary>
    protected void CalculateVectors()
    {
        _tempFront.X = MathF.Cos(Yaw) * MathF.Cos(Pitch);
        _tempFront.Y = MathF.Sin(Pitch);
        _tempFront.Z = MathF.Sin(Yaw) * MathF.Cos(Pitch);

        Front = Vector3.Normalize(_tempFront);

        var flatFront = new Vector3(_tempFront.X, 0, _tempFront.Z);
        flatFront = Vector3.Normalize(flatFront);

        Right = Vector3.Normalize(Vector3.Cross(flatFront, Up));
    }

    /// <summary>
    /// Computes the view matrix using position, front, and up vectors.
    /// </summary>
    protected void CalculateView()
    {
        View = Matrix4x4.CreateLookAt(Position, Position + Front, Up);
    }

    /// <summary>
    /// Computes the perspective projection matrix using field of view, aspect ratio, and clipping planes.
    /// </summary>
    protected void CalculateProjection()
    {
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(FOV, AspectRatio, NearPlane, FarPlane);
    }

    /// <summary>
    /// Updates camera state over time.
    /// </summary>
    public abstract void Update(double deltaTime);
}

