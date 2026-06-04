# Math Helpers

Extension methods for `Vector2`, `Vector3`, `Vector4`, `Matrix4x4`, `Quaternion`, and `float`.

## Constants

```csharp
MathHelper.Pi            // MathF.PI
MathHelper.TwoPi         // MathF.PI * 2.0f
MathHelper.PiOver2       // MathF.PI / 2.0f
MathHelper.PiOver4       // MathF.PI / 4.0f
```

## float Extensions

### Conversion

```csharp
float rads = 90f.ToRad();    // → 1.5708 (π/2)
float degs = MathF.PI.ToDeg();  // → 180f
```

### Math Operations

```csharp
float lerp = 0.5f.Lerp(0f, 1f);      // Linear interpolation: 0.5
float wrapped = 400f.WrapAngle(360f); // Wrap angle: 40

// Note: WrapAngle takes modulus value, not a range
```

## Vector2 Extensions

### Conversion

```csharp
int x = vector2.ToNum();      // X component as int
float[] arr = vector2.ToFloatArray();  // [X, Y]
```

### Conversion to Other Types

```csharp
Vector2D<float> v = vector2.To2Df();
Vector2D<int> v = vector2.To2Di();
```

## Vector3 Extensions

### Conversion

```csharp
float[] arr = vector3.ToFloatArray();  // [X, Y, Z]
```

### String Formatting

```csharp
string s = vector3.ToStr();        // "1.0000, 2.0000, 3.0000"
string s = vector3.ToStrF2();      // "1.00, 2.00, 3.00"
string s = vector3.ToStrInt();     // "1, 2, 3"
```

### Normalization

```csharp
vector3.Normalize();  // Modifies in-place, returns ref
```

## Vector4 Extensions

### String Formatting

```csharp
string s = vector4.ToStrF2();  // "1.00, 2.00, 3.00, 4.00"
string s = vector4.ToStrInt(); // "1, 2, 3, 4"
```

## Matrix4x4 Extensions

### String Formatting

```csharp
string s = matrix.ToStr();        // Full matrix with all 16 values
string s = matrix.ToStrF2();      // 2 decimal places
```

### Conversion

```csharp
float[] arr = matrix.ToFloatArray();  // Row-major 16 floats
```

### Transformation

```csharp
// In-place inverse
matrix.Invert();
matrix.InverseTranspose();  // Invert then transpose (for normal matrices)
matrix.Transpose();

// Yaw/pitch/roll rotation matrix
Matrix4x4 rot = Matrix4x4.RotationMxFromYawPitchRoll(yaw, pitch, roll);
```

## Quaternion Extensions

### Yaw/Pitch/Roll

```csharp
// Create from Euler angles
Quaternion q = Quaternion.RotationFromYawPitchRoll(yaw, pitch, roll);

// Decompose to Euler angles
q.ExtractYawPitchRoll(out float yaw, out float pitch, out float roll);
```

### String Formatting

```csharp
string s = q.ToStr();        // "X: 0.0000, Y: 0.0000, Z: 0.0000, W: 1.0000"
string s = q.ToStrF2();      // "X: 0.00, Y: 0.00, Z: 0.00, W: 1.00"
```

## String Extensions

### Bone Name Trimming

```csharp
string clean = "mixamorig:Hips".TrimBoneName();
// Result: "Hips"
// Removes common Mixamo prefix patterns: "mixamorig:", "mixamorig:", "mixamo:", etc.
```

## List<Vector3> Extensions

### Float Array Conversion

```csharp
var floats = vertexPositions.ToFloatArrayList();
// Flattens [v1, v2, v3] → [v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, ...]
```

## Vector2D<T> Extensions

```csharp
float num = vector2D.ToNum();  // Returns X as the numeric value
```

## See Also

- [Camera](../core/camera.md) — uses `MathF.PI` for angles, `Quaternion` for rotation
- [Shaders](../rendering/shaders.md) — `Matrix4x4.ToFloatArray()` for passing matrices to uniforms
