namespace FpsoFrequencyDomain;

/// <summary>
/// Body-fixed point in metres. Convention: x forward, y port, z up.
/// The default RAO reference origin is the waterline centre unless the caller translates it.
/// </summary>
public readonly record struct BodyPoint(double X, double Y, double Z)
{
    public static BodyPoint Origin { get; } = new(0.0, 0.0, 0.0);

    public static BodyPoint operator +(BodyPoint left, BodyPoint right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static BodyPoint operator -(BodyPoint left, BodyPoint right) =>
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
}

public enum MotionDof
{
    Surge,
    Sway,
    Heave,
    Roll,
    Pitch,
    Yaw
}

public enum TranslationComponent
{
    X,
    Y,
    Z
}

public enum SpectrumKind
{
    PiersonMoskowitz,
    Jonswap
}
