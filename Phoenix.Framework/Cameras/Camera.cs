using System.Numerics;

namespace Phoenix.Framework.Cameras;

public abstract class Camera
{
    /// <summary>
    /// Gets or sets the view transformation matrix.
    /// </summary>
    public Matrix4x4 View { get; set; }

    /// <summary>
    /// Gets or sets the projection transformation matrix.
    /// </summary>
    public Matrix4x4 Projection { get; set; }

    /// <summary>
    /// Gets or sets the camera world position.
    /// </summary>
    public virtual Vector3 Position { get; set; }
}

