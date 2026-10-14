# Math helpers

Namespace: `Phoenix.Framework.Maths`

Extension methods and utilities for `Vector2`, `Vector3`, `Vector4`, `Matrix4x4`, `Quaternion`, and `float`.

---

## Constants

```csharp
MathHelper.Pi            // MathF.PI
MathHelper.TwoPi         // MathF.PI * 2.0f
MathHelper.PiOver2       // MathF.PI / 2.0f
MathHelper.PiOver4       // MathF.PI / 4.0f
```

---

## Float extensions

### Angle conversion

```csharp
float rads = 90f.ToRad();       // 1.5708 radians
float degs = MathF.PI.ToDeg();  // 180.0 degrees
```

### Interpolation

```csharp
float lerp = MathHelper.Lerp(0f, 1f, 0.5f);  // Linear interpolation: 0.5
float wrapped = MathHelper.WrapAngle(400f);  // Wrap to [-pi, pi]
```

---

## Vector3 extensions

### Conversion and arrays

```csharp
float[] arr = vector3.ToFloatArray();  // [X, Y, Z]
```

### String formatting

```csharp
string s1 = vector3.ToStr();        // "1.0000, 2.0000, 3.0000"
string s2 = vector3.ToStrF2();      // "1.00, 2.00, 3.00"
string s3 = vector3.ToStrInt();     // "1, 2, 3"
```

### Normalization

```csharp
vector3.Normalize();  // In-place normalization
```

---

## Matrix4x4 extensions

### Matrix transformations

```csharp
// In-place inverse
matrix.Invert();

// Normal matrix transformation
matrix.InverseTranspose();

// Transpose
matrix.Transpose();

// Yaw, pitch, roll rotation matrix
Matrix4x4 rot = MathHelper.RotationMxFromYawPitchRoll(yaw, pitch, roll);
```

### Float array conversion

```csharp
float[] arr = matrix.ToFloatArray();  // 16 floats row-major
```

---

## Quaternion extensions

```csharp
// Create from Euler angles
Quaternion q = Quaternion.RotationFromYawPitchRoll(yaw, pitch, roll);

// Decompose to Euler angles
q.ExtractYawPitchRoll(out float yaw, out float pitch, out float roll);

// String formatting
string s = q.ToStrF2(); // "X: 0.00, Y: 0.00, Z: 0.00, W: 1.00"
```

---

## Bone name utilities

```csharp
// Strips common export prefixes (mixamorig:, mixamo:, etc.)
string cleanName = "mixamorig:Hips".TrimBoneName(); // Returns "Hips"
```
