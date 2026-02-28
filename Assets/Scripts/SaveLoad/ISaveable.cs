public interface ISaveable
{
    string SaveState();
    void RestoreState(string json);
}
