using UnityEngine;

public interface IBuildable
{
    Vector2Int GridSize { get; }
    bool CanPlaceAt(Vector3Int gridPosition);
    void OnPlaced(Vector3Int gridPosition);
    void OnRemoved();
}
