using UnityEngine;

public static class MathUtil
{
    public static float Map01(float t, float mmin, float mmax)
    {
        return (t-mmin)/(mmax-mmin);
    }
    /// <summary>
    /// 生成一个随机方向，方向的基准轴是 (0,1,0)，
    /// 与基准轴的夹角范围是 0 ~ maxAngleDegree。
    /// </summary>
    public static Vector3 RandomDirection(float maxAngleDegree)
    {
        // 将角度转成弧度
        float maxAngleRad = maxAngleDegree * Mathf.Deg2Rad;

        // 随机选择夹角：cos(theta) 在 [cos(max), 1] 之间均匀采样
        float cosTheta = Random.Range(Mathf.Cos(maxAngleRad), 1f);
        float sinTheta = Mathf.Sqrt(1f - cosTheta * cosTheta);

        // 随机选择绕基准方向的旋转
        float phi = Random.Range(0f, 2f * Mathf.PI);

        // 在 baseDir = (0,1,0) 坐标系下生成方向
        // y = cosTheta
        // x,z = sinTheta 的随机方向
        float x = sinTheta * Mathf.Cos(phi);
        float z = sinTheta * Mathf.Sin(phi);
        float y = cosTheta;

        Vector3 dir = new Vector3(x, y, z).normalized;
        return dir;
    }
}