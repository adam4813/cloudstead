using UnityEngine;

public static class Extensions
{
    public static Vector2 ToVector2(this Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }

    public static Vector3 ToVector3(this Vector2 v, float z = 0f)
    {
        return new Vector3(v.x, v.y, z);
    }

    public static Vector2Int ToVector2Int(this Vector3Int v)
    {
        return new Vector2Int(v.x, v.y);
    }

    public static Vector3Int ToVector3Int(this Vector2 v)
    {
        return new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), 0);
    }

    public static Vector3Int WorldToTile(this Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x),
            Mathf.FloorToInt(worldPos.y),
            0);
    }

    public static Vector3 TileToWorld(this Vector3Int tilePos)
    {
        return new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);
    }

    public static Vector2 DirectionToVector(this Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector2.up,
            Direction.Down => Vector2.down,
            Direction.Left => Vector2.left,
            Direction.Right => Vector2.right,
            _ => Vector2.down
        };
    }
}
