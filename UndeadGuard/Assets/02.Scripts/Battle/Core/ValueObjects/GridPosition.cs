using System;

/// <summary>
/// 전투 그리드에서 사용하는 논리 좌표를 표현하는 값 타입.
/// Unity 월드 좌표가 아니라 x, z 기반의 칸 좌표를 다루며,
/// 위치 비교와 거리 계산의 기준으로 사용한다.
/// </summary>
[Serializable]
public readonly struct GridPosition : IEquatable<GridPosition>
{
    public readonly int x;
    public readonly int z;

    public GridPosition(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public GridPosition Add(GridPosition other)
    {
        return new GridPosition(x + other.x, z + other.z);
    }

    public int ManhattanDistance(GridPosition other)
    {
        return Math.Abs(x - other.x) + Math.Abs(z - other.z);
    }

    public bool Equals(GridPosition other)
    {
        return x == other.x && z == other.z;
    }

    public override bool Equals(object obj)
    {
        return obj is GridPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (x * 397) ^ z;
        }
    }

    public static bool operator ==(GridPosition left, GridPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GridPosition left, GridPosition right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"({x}, {z})";
    }
}