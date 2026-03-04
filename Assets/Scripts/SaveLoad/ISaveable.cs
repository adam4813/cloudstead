public interface ISaveable
{
    /// <summary>Unique key for this saveable. Defaults to type name. Override for multi-instance types (e.g. multiple airships).</summary>
    string SaveKey => GetType().Name;
    string SaveState();
    void RestoreState(string json);
}
