using System.Numerics;

namespace FpsoFrequencyDomain;

internal static class Numerics
{
    public const double TwoPi = 2.0 * Math.PI;
    public const double Gravity = 9.80665;
    public const double JulianYearSeconds = 365.25 * 24.0 * 3600.0;
    public const double EulerMascheroni = 0.5772156649015329;

    public static double[] Copy(IReadOnlyList<double> values, string name)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException($"{name} must contain at least one value.", name);
        }

        var copy = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (double.IsNaN(values[i]) || double.IsInfinity(values[i]))
            {
                throw new ArgumentException($"{name} contains a non-finite value at index {i}.", name);
            }

            copy[i] = values[i];
        }

        return copy;
    }

    public static Complex[] Copy(IReadOnlyList<Complex> values, string name)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
        {
            throw new ArgumentException($"{name} must contain at least one value.", name);
        }

        var copy = new Complex[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (!IsFinite(values[i]))
            {
                throw new ArgumentException($"{name} contains a non-finite value at index {i}.", name);
            }

            copy[i] = values[i];
        }

        return copy;
    }

    public static void EnsureSameLength(int expected, int actual, string name)
    {
        if (expected != actual)
        {
            throw new ArgumentException($"{name} length {actual} does not match expected length {expected}.", name);
        }
    }

    public static void EnsureAscending(IReadOnlyList<double> x, string name)
    {
        for (var i = 1; i < x.Count; i++)
        {
            if (x[i] < x[i - 1])
            {
                throw new ArgumentException($"{name} must be sorted in ascending order.", name);
            }
        }
    }

    public static double Trapz(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        EnsureSameLength(x.Count, y.Count, nameof(y));
        if (x.Count < 2)
        {
            return 0.0;
        }

        EnsureAscending(x, nameof(x));

        var sum = 0.0;
        for (var i = 1; i < x.Count; i++)
        {
            var dx = x[i] - x[i - 1];
            sum += 0.5 * dx * (y[i] + y[i - 1]);
        }

        return sum;
    }

    public static Complex[] InterpolateComplex(
        IReadOnlyList<double> sourceX,
        IReadOnlyList<Complex> sourceY,
        IReadOnlyList<double> targetX)
    {
        EnsureSameLength(sourceX.Count, sourceY.Count, nameof(sourceY));
        EnsureAscending(sourceX, nameof(sourceX));

        var result = new Complex[targetX.Count];
        for (var i = 0; i < targetX.Count; i++)
        {
            result[i] = InterpolateComplexAt(sourceX, sourceY, targetX[i]);
        }

        return result;
    }

    public static double[] InterpolateReal(
        IReadOnlyList<double> sourceX,
        IReadOnlyList<double> sourceY,
        IReadOnlyList<double> targetX)
    {
        EnsureSameLength(sourceX.Count, sourceY.Count, nameof(sourceY));
        EnsureAscending(sourceX, nameof(sourceX));

        var result = new double[targetX.Count];
        for (var i = 0; i < targetX.Count; i++)
        {
            result[i] = InterpolateRealAt(sourceX, sourceY, targetX[i]);
        }

        return result;
    }

    public static int MaxIndex(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Cannot find a maximum in an empty list.", nameof(values));
        }

        var index = 0;
        var max = values[0];
        for (var i = 1; i < values.Count; i++)
        {
            if (values[i] > max)
            {
                max = values[i];
                index = i;
            }
        }

        return index;
    }

    private static Complex InterpolateComplexAt(
        IReadOnlyList<double> sourceX,
        IReadOnlyList<Complex> sourceY,
        double target)
    {
        if (target < sourceX[0] || target > sourceX[^1])
        {
            return Complex.Zero;
        }

        if (target == sourceX[0])
        {
            return sourceY[0];
        }

        if (target == sourceX[^1])
        {
            return sourceY[^1];
        }

        var next = UpperBound(sourceX, target);
        var previous = next - 1;
        var span = sourceX[next] - sourceX[previous];
        if (span <= 0.0)
        {
            return sourceY[previous];
        }

        var t = (target - sourceX[previous]) / span;
        return sourceY[previous] + t * (sourceY[next] - sourceY[previous]);
    }

    private static double InterpolateRealAt(
        IReadOnlyList<double> sourceX,
        IReadOnlyList<double> sourceY,
        double target)
    {
        if (target < sourceX[0] || target > sourceX[^1])
        {
            return 0.0;
        }

        if (target == sourceX[0])
        {
            return sourceY[0];
        }

        if (target == sourceX[^1])
        {
            return sourceY[^1];
        }

        var next = UpperBound(sourceX, target);
        var previous = next - 1;
        var span = sourceX[next] - sourceX[previous];
        if (span <= 0.0)
        {
            return sourceY[previous];
        }

        var t = (target - sourceX[previous]) / span;
        return sourceY[previous] + t * (sourceY[next] - sourceY[previous]);
    }

    private static int UpperBound(IReadOnlyList<double> values, double target)
    {
        var low = 0;
        var high = values.Count;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (values[mid] <= target)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return Math.Clamp(low, 1, values.Count - 1);
    }

    private static bool IsFinite(Complex value) =>
        !double.IsNaN(value.Real) &&
        !double.IsInfinity(value.Real) &&
        !double.IsNaN(value.Imaginary) &&
        !double.IsInfinity(value.Imaginary);
}
