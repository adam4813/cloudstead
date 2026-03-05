using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Data-driven cutscene: an ordered list of CutsceneSteps executed by CutscenePlayer.
/// Create via Assets ▸ Cloudstead ▸ Cutscene ▸ Cutscene Definition.
/// </summary>
[CreateAssetMenu(menuName = "Cloudstead/Cutscene/Cutscene Definition", fileName = "NewCutscene")]
public class CutsceneDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string cutsceneId;
    [SerializeField, TextArea(1, 3)] private string description;

    [Header("Steps")]
    [SerializeField, ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ShowPaging = false)]
    private CutsceneStep[] steps = System.Array.Empty<CutsceneStep>();

    public string CutsceneId => cutsceneId;
    public string Description => description;
    public CutsceneStep[] Steps => steps;
    public bool IsValid => steps != null && steps.Length > 0;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(cutsceneId))
            cutsceneId = name;
    }
#endif
}
