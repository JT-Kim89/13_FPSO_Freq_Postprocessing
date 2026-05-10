using System.Numerics;

namespace FpsoFrequencyDomain;

/// <summary>
/// Complex 6DOF motion RAO. Translations are m/m; rotations are rad/m.
/// </summary>
public sealed class SixDofRao
{
    private readonly double[] _frequencyHz;
    private readonly Complex[] _surge;
    private readonly Complex[] _sway;
    private readonly Complex[] _heave;
    private readonly Complex[] _roll;
    private readonly Complex[] _pitch;
    private readonly Complex[] _yaw;

    public SixDofRao(
        IReadOnlyList<double> frequencyHz,
        IReadOnlyList<Complex> surge,
        IReadOnlyList<Complex> sway,
        IReadOnlyList<Complex> heave,
        IReadOnlyList<Complex> roll,
        IReadOnlyList<Complex> pitch,
        IReadOnlyList<Complex> yaw,
        string name = "")
    {
        _frequencyHz = Numerics.Copy(frequencyHz, nameof(frequencyHz));
        Numerics.EnsureAscending(_frequencyHz, nameof(frequencyHz));
        _surge = CopyDof(surge, nameof(surge));
        _sway = CopyDof(sway, nameof(sway));
        _heave = CopyDof(heave, nameof(heave));
        _roll = CopyDof(roll, nameof(roll));
        _pitch = CopyDof(pitch, nameof(pitch));
        _yaw = CopyDof(yaw, nameof(yaw));
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyList<double> FrequencyHz => _frequencyHz;

    public int Count => _frequencyHz.Length;

    public IReadOnlyList<Complex> Surge => _surge;

    public IReadOnlyList<Complex> Sway => _sway;

    public IReadOnlyList<Complex> Heave => _heave;

    public IReadOnlyList<Complex> Roll => _roll;

    public IReadOnlyList<Complex> Pitch => _pitch;

    public IReadOnlyList<Complex> Yaw => _yaw;

    public static Complex[] FromAmplitudePhaseDegrees(
        IReadOnlyList<double> amplitude,
        IReadOnlyList<double> phaseDegrees)
    {
        var amp = Numerics.Copy(amplitude, nameof(amplitude));
        var phase = Numerics.Copy(phaseDegrees, nameof(phaseDegrees));
        Numerics.EnsureSameLength(amp.Length, phase.Length, nameof(phaseDegrees));

        var values = new Complex[amp.Length];
        for (var i = 0; i < amp.Length; i++)
        {
            values[i] = Complex.FromPolarCoordinates(amp[i], phase[i] * Math.PI / 180.0);
        }

        return values;
    }

    public IReadOnlyList<Complex> GetRao(MotionDof dof) =>
        dof switch
        {
            MotionDof.Surge => _surge,
            MotionDof.Sway => _sway,
            MotionDof.Heave => _heave,
            MotionDof.Roll => _roll,
            MotionDof.Pitch => _pitch,
            MotionDof.Yaw => _yaw,
            _ => throw new ArgumentOutOfRangeException(nameof(dof), dof, "Unsupported DOF.")
        };

    public SixDofRao InterpolateTo(IReadOnlyList<double> targetFrequencyHz)
    {
        var target = Numerics.Copy(targetFrequencyHz, nameof(targetFrequencyHz));
        return new SixDofRao(
            target,
            Numerics.InterpolateComplex(_frequencyHz, _surge, target),
            Numerics.InterpolateComplex(_frequencyHz, _sway, target),
            Numerics.InterpolateComplex(_frequencyHz, _heave, target),
            Numerics.InterpolateComplex(_frequencyHz, _roll, target),
            Numerics.InterpolateComplex(_frequencyHz, _pitch, target),
            Numerics.InterpolateComplex(_frequencyHz, _yaw, target),
            Name);
    }

    /// <summary>
    /// Translates the RAO origin. The vector is from current origin to the new origin.
    /// Rotational RAOs are unchanged; translational RAOs become the motion at the new origin.
    /// </summary>
    public SixDofRao TranslateReferenceTo(BodyPoint newOriginFromCurrentOrigin)
    {
        var point = AtPoint(newOriginFromCurrentOrigin);
        return new SixDofRao(
            _frequencyHz,
            point.X,
            point.Y,
            point.Z,
            _roll,
            _pitch,
            _yaw,
            Name);
    }

    /// <summary>
    /// Gets local point translational RAOs for a point measured from the current RAO origin.
    /// Small-angle rigid body kinematics: u(P) = u(O) + theta x r.
    /// </summary>
    public PointMotionRao AtPoint(BodyPoint pointFromCurrentOrigin)
    {
        var x = new Complex[Count];
        var y = new Complex[Count];
        var z = new Complex[Count];

        for (var i = 0; i < Count; i++)
        {
            x[i] = _surge[i] + (_pitch[i] * pointFromCurrentOrigin.Z) - (_yaw[i] * pointFromCurrentOrigin.Y);
            y[i] = _sway[i] + (_yaw[i] * pointFromCurrentOrigin.X) - (_roll[i] * pointFromCurrentOrigin.Z);
            z[i] = _heave[i] + (_roll[i] * pointFromCurrentOrigin.Y) - (_pitch[i] * pointFromCurrentOrigin.X);
        }

        return new PointMotionRao(_frequencyHz, x, y, z);
    }

    /// <summary>
    /// Convenience method when local points are defined relative to COG but the RAO origin is still waterline centre.
    /// </summary>
    public PointMotionRao AtPointFromCog(BodyPoint cogFromReferenceOrigin, BodyPoint pointFromCog)
    {
        return AtPoint(cogFromReferenceOrigin + pointFromCog);
    }

    private Complex[] CopyDof(IReadOnlyList<Complex> values, string name)
    {
        var copy = Numerics.Copy(values, name);
        Numerics.EnsureSameLength(_frequencyHz.Length, copy.Length, name);
        return copy;
    }
}

public sealed class PointMotionRao
{
    private readonly double[] _frequencyHz;
    private readonly Complex[] _x;
    private readonly Complex[] _y;
    private readonly Complex[] _z;

    public PointMotionRao(
        IReadOnlyList<double> frequencyHz,
        IReadOnlyList<Complex> x,
        IReadOnlyList<Complex> y,
        IReadOnlyList<Complex> z)
    {
        _frequencyHz = Numerics.Copy(frequencyHz, nameof(frequencyHz));
        Numerics.EnsureAscending(_frequencyHz, nameof(frequencyHz));
        _x = CopyComponent(x, nameof(x));
        _y = CopyComponent(y, nameof(y));
        _z = CopyComponent(z, nameof(z));
    }

    public IReadOnlyList<double> FrequencyHz => _frequencyHz;

    public IReadOnlyList<Complex> X => _x;

    public IReadOnlyList<Complex> Y => _y;

    public IReadOnlyList<Complex> Z => _z;

    public IReadOnlyList<Complex> GetRao(TranslationComponent component) =>
        component switch
        {
            TranslationComponent.X => _x,
            TranslationComponent.Y => _y,
            TranslationComponent.Z => _z,
            _ => throw new ArgumentOutOfRangeException(nameof(component), component, "Unsupported component.")
        };

    private Complex[] CopyComponent(IReadOnlyList<Complex> values, string name)
    {
        var copy = Numerics.Copy(values, name);
        Numerics.EnsureSameLength(_frequencyHz.Length, copy.Length, name);
        return copy;
    }
}
