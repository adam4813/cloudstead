#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Cutscene Editor — Cloudstead ▸ Cutscene Editor
///
/// Left panel: all CutsceneDefinition assets in the project.
/// Right panel: selected cutscene with step list + "Add Step" toolbox.
///
/// The toolbox shows one button per step type. Clicking it appends
/// a pre-configured step to the selected cutscene's steps array.
/// </summary>
public class CutsceneEditorWindow : OdinEditorWindow
{
    [MenuItem("Cloudstead/Cutscene Editor")]
    private static void OpenWindow()
    {
        var win = GetWindow<CutsceneEditorWindow>();
        win.titleContent = new GUIContent("Cutscene Editor");
        win.minSize = new Vector2(800, 500);
        win.Show();
    }

    // ── Left panel: asset list ──────────────────────────────────────────────

    [HorizontalGroup("Split", 0.3f)]
    [BoxGroup("Split/Assets")]
    private List<CutsceneDefinition> _assetList = new();

    private Vector2 _assetScrollPos;

    // ── Right panel: selected cutscene ──────────────────────────────────────

    [HorizontalGroup("Split")]
    [BoxGroup("Split/Editor")]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden, Expanded = true)]
    [ShowInInspector, HideLabel]
    private CutsceneDefinition _selected;

    // ── Toolbar ─────────────────────────────────────────────────────────────

    [BoxGroup("Split/Editor")]
    [HorizontalGroup("Split/Editor/Bar")]
    [Button("+ Move", ButtonSizes.Small), GUIColor(0.6f, 0.9f, 0.6f)]
    private void AddMove()       => AppendStep(CutsceneStepType.MoveActor);

    [HorizontalGroup("Split/Editor/Bar")]
    [Button("+ Teleport", ButtonSizes.Small), GUIColor(0.6f, 0.9f, 0.6f)]
    private void AddTeleport()   => AppendStep(CutsceneStepType.TeleportActor);

    [HorizontalGroup("Split/Editor/Bar")]
    [Button("+ Camera", ButtonSizes.Small), GUIColor(0.6f, 0.85f, 1f)]
    private void AddCamera()     => AppendStep(CutsceneStepType.FocusCamera);

    [HorizontalGroup("Split/Editor/Bar")]
    [Button("+ Cam ↩", ButtonSizes.Small), GUIColor(0.6f, 0.85f, 1f)]
    private void AddReturnCam()  => AppendStep(CutsceneStepType.ReturnCamera);

    [HorizontalGroup("Split/Editor/Bar2")]
    [Button("+ Dialogue", ButtonSizes.Small), GUIColor(1f, 0.85f, 0.5f)]
    private void AddDialogue()   => AppendStep(CutsceneStepType.ShowDialogue);

    [HorizontalGroup("Split/Editor/Bar2")]
    [Button("+ Wait", ButtonSizes.Small), GUIColor(0.9f, 0.9f, 0.9f)]
    private void AddWait()       => AppendStep(CutsceneStepType.Wait);

    [HorizontalGroup("Split/Editor/Bar2")]
    [Button("+ Board ✈", ButtonSizes.Small), GUIColor(0.9f, 0.7f, 1f)]
    private void AddBoard()      => AppendStep(CutsceneStepType.BoardAirship);

    [HorizontalGroup("Split/Editor/Bar2")]
    [Button("+ Disembark", ButtonSizes.Small), GUIColor(0.9f, 0.7f, 1f)]
    private void AddDisembark()  => AppendStep(CutsceneStepType.DisembarkAirship);

    [HorizontalGroup("Split/Editor/Bar3")]
    [Button("+ Show/Hide", ButtonSizes.Small), GUIColor(0.9f, 0.8f, 0.6f)]
    private void AddSetActive()  => AppendStep(CutsceneStepType.SetActorActive);

    [HorizontalGroup("Split/Editor/Bar3")]
    [Button("+ Ownership", ButtonSizes.Small), GUIColor(0.9f, 0.8f, 0.6f)]
    private void AddOwnership()  => AppendStep(CutsceneStepType.TransferOwnership);

    [HorizontalGroup("Split/Editor/Bar4")]
    [Button("+ Return Control", ButtonSizes.Small), GUIColor(0.6f, 1f, 0.8f)]
    private void AddReturnControl() => AppendStep(CutsceneStepType.ReturnPlayerControl);

    [HorizontalGroup("Split/Editor/Bar4")]
    [Button("+ Follow", ButtonSizes.Small), GUIColor(0.6f, 1f, 0.8f)]
    private void AddFollow()     => AppendStep(CutsceneStepType.FollowActor);

    [HorizontalGroup("Split/Editor/Bar4")]
    [Button("+ Stop Follow", ButtonSizes.Small), GUIColor(0.6f, 1f, 0.8f)]
    private void AddStopFollow() => AppendStep(CutsceneStepType.StopFollowActor);

    // ── Lifecycle ───────────────────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshAssetList();
    }

    [BoxGroup("Split/Assets")]
    [Button("Refresh", ButtonSizes.Small)]
    private void RefreshAssetList()
    {
        _assetList.Clear();
        var guids = AssetDatabase.FindAssets("t:CutsceneDefinition");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<CutsceneDefinition>(path);
            if (asset != null) _assetList.Add(asset);
        }
    }

    [BoxGroup("Split/Assets")]
    [Button("New Cutscene", ButtonSizes.Medium), GUIColor(0.5f, 1f, 0.5f)]
    private void CreateNewCutscene()
    {
        const string folder = "Assets/Data/Cutscenes";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Cutscenes");
        }

        var asset = CreateInstance<CutsceneDefinition>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewCutscene.asset");
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        RefreshAssetList();
        _selected = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // Clicking an asset in the list selects it
    [OnInspectorGUI, BoxGroup("Split/Assets")]
    private void DrawAssetList()
    {
        _assetScrollPos = EditorGUILayout.BeginScrollView(_assetScrollPos);
        foreach (var asset in _assetList)
        {
            if (asset == null) continue;
            bool isSelected = asset == _selected;

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? new Color(0.4f, 0.8f, 1f) : new Color(0.85f, 0.85f, 0.85f);
            if (GUILayout.Button(asset.name, EditorStyles.toolbarButton, GUILayout.Height(22)))
            {
                _selected = asset;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = prev;
        }
        EditorGUILayout.EndScrollView();
    }

    protected override void OnImGUI()
    {
        base.OnImGUI();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void AppendStep(CutsceneStepType stepType)
    {
        if (_selected == null)
        {
            Debug.LogWarning("[CutsceneEditor] No cutscene selected.");
            return;
        }

        var so = new SerializedObject(_selected);
        var stepsProp = so.FindProperty("steps");

        stepsProp.arraySize++;
        var newStepProp = stepsProp.GetArrayElementAtIndex(stepsProp.arraySize - 1);
        newStepProp.FindPropertyRelative("type").enumValueIndex = (int)stepType;

        // Sensible defaults
        if (stepType == CutsceneStepType.Wait)
            newStepProp.FindPropertyRelative("duration").floatValue = 1f;
        if (stepType == CutsceneStepType.SetActorActive)
            newStepProp.FindPropertyRelative("boolValue").boolValue = true;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(_selected);
    }
}
#endif
