using System.Numerics;

public static class MathUtil
{
    public static float Map01(float t, float mmin, float mmax)
    {
        return (t-mmin)/(mmax-mmin);
    }
}