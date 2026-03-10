using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class BuildingEditorWindow : OdinEditorWindow
{
    [MenuItem("Cloudstead/Building Editor")]
    private static void OpenWindow() => GetWindow<BuildingEditorWindow>("Building Editor").Show();

    // ── Tab bar ───────────────────────────────────────────────────────────────

    [HideInInspector]
    public int activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_browseTabLabel"), GUIColor("@activeTab==0 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowBrowse() => activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_editTabLabel"), GUIColor("@activeTab==1 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowEdit() => activeTab = 1;

    [HorizontalGroup("Tabs"), Button("Validate"), GUIColor("@activeTab==2 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowValidate() => activeTab = 2;

    private string _browseTabLabel => $"Browse ({_allBuildings.Count})";
    private string _editTabLabel   => _editingBuilding != null ? $"✏  {_editingBuilding.buildingName}" : "+ New Building";

    // ── Browse ────────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 0")]
    [LabelText("Search"), OnValueChanged("FilterBuildings")]
    [SerializeField] private string _searchQuery = "";

    [ShowIf("@activeTab == 0")]
    [LabelText("Type Filter"), OnValueChanged("FilterBuildings")]
    [SerializeField] private BuildingTypeFilter _typeFilter = BuildingTypeFilter.All;

    [ShowIf("@activeTab == 0")]
    [ShowInInspector]
    [TableList(ShowPaging = false, AlwaysExpanded = true, ScrollViewHeight = 400)]
    private List<BuildingRow> _displayedBuildings = new();

    private List<BuildingDefinition> _allBuildings = new();

    [ShowIf("@activeTab == 0")]
    [Button("Refresh List")]
    private void LoadAllBuildings()
    {
        _allBuildings.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:BuildingDefinition"))
        {
            var b = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (b != null) _allBuildings.Add(b);
        }
        FilterBuildings();
    }

    private void FilterBuildings()
    {
        _displayedBuildings = _allBuildings
            .Where(b =>
            {
                if (!string.IsNullOrEmpty(_searchQuery) &&
                    (b.buildingName == null ||
                     b.buildingName.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) < 0))
                    return false;
                if (_typeFilter != BuildingTypeFilter.All &&
                    b.buildingType != (BuildingType)((int)_typeFilter - 1))
                    return false;
                return true;
            })
            .Select(b => new BuildingRow(b, this))
            .ToList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadAllBuildings();
    }

    // ── Edit / Create ─────────────────────────────────────────────────────────

    private BuildingDefinition _editingBuilding;

    [ShowIf("@activeTab == 1 && _editingBuilding != null")]
    [ShowInInspector, HideLabel]
    [InfoBox("$_editingBanner", InfoMessageType.None)]
    private bool _editBannerAnchor;

    private string _editingBanner => _editingBuilding != null
        ? $"Editing: {_editingBuilding.buildingName}   |   {AssetDatabase.GetAssetPath(_editingBuilding)}"
        : "";

    [ShowIf("@activeTab == 1 && _editingBuilding != null")]
    [Button("Clear — Start New Building"), GUIColor(1f, 0.8f, 0.6f)]
    private void ClearEditMode()
    {
        _editingBuilding = null;
        ResetForm();
    }

    // Identity
    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Identity"), LabelText("Building Name")]
    [SerializeField] private string _newBuildingName = "";

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Identity"), LabelText("Type")]
    [SerializeField] private BuildingType _newBuildingType;

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Identity"), LabelText("Description"), TextArea(2, 4)]
    [SerializeField] private string _newDescription = "";

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Identity"), LabelText("Owner NPC")]
    [SerializeField] private NPCDefinition _newOwnerNPC;

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Identity"), LabelText("Icon"), PreviewField(64)]
    [SerializeField] private Sprite _newIcon;

    // Schedule
    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule"), LabelText("Always Open")]
    [SerializeField] private bool _newAlwaysOpen = true;

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule")]
    [ButtonGroup("Schedule/Presets")]
    [Button("Standard (8–6)")]
    private void PresetStandard() { _newAlwaysOpen = false; _newOpenHour = 8f; _newCloseHour = 18f; }

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule")]
    [ButtonGroup("Schedule/Presets")]
    [Button("Late (10–6)")]
    private void PresetLate() { _newAlwaysOpen = false; _newOpenHour = 10f; _newCloseHour = 18f; }

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule")]
    [ButtonGroup("Schedule/Presets")]
    [Button("Evening (12–12)")]
    private void PresetEvening() { _newAlwaysOpen = false; _newOpenHour = 12f; _newCloseHour = 24f; }

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule")]
    [ButtonGroup("Schedule/Presets")]
    [Button("Dawn (6–8p)")]
    private void PresetDawn() { _newAlwaysOpen = false; _newOpenHour = 6f; _newCloseHour = 20f; }

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Schedule")]
    [ButtonGroup("Schedule/Presets")]
    [Button("Always Open")]
    private void PresetAlwaysOpen() { _newAlwaysOpen = true; }

    [ShowIf("@activeTab == 1 && !_newAlwaysOpen")]
    [FoldoutGroup("Schedule"), LabelText("Opens At"), Range(0f, 24f)]
    [SerializeField] private float _newOpenHour = 9f;

    [ShowIf("@activeTab == 1 && !_newAlwaysOpen")]
    [FoldoutGroup("Schedule"), LabelText("Closes At"), Range(0f, 24f)]
    [SerializeField] private float _newCloseHour = 17f;

    [ShowIf("@activeTab == 1 && !_newAlwaysOpen")]
    [FoldoutGroup("Schedule"), LabelText("Days Open (day 1–7 of season)")]
    [SerializeField] private bool[] _newDaysOpen = { true, true, true, true, true, true, true };

    [ShowIf("@activeTab == 1")]
    [Button("$_saveButtonLabel"), GUIColor("@_editingBuilding != null ? new UnityEngine.Color(0.7f,1f,0.7f) : UnityEngine.Color.white")]
    private void SaveBuildingAsset()
    {
        if (string.IsNullOrEmpty(_newBuildingName))
        {
            EditorUtility.DisplayDialog("Validation Error", "Building name cannot be empty.", "OK");
            return;
        }

        if (_editingBuilding != null)
        {
            _editingBuilding.buildingName = _newBuildingName;
            _editingBuilding.buildingType = _newBuildingType;
            _editingBuilding.description  = _newDescription;
            _editingBuilding.ownerNPC     = _newOwnerNPC;
            _editingBuilding.icon         = _newIcon;
            _editingBuilding.alwaysOpen   = _newAlwaysOpen;
            _editingBuilding.openHour     = _newOpenHour;
            _editingBuilding.closeHour    = _newCloseHour;
            _editingBuilding.daysOpen     = (bool[])_newDaysOpen.Clone();

            EditorUtility.SetDirty(_editingBuilding);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_editingBuilding);
            LoadAllBuildings();
        }
        else
        {
            EnsureFolderExists("Assets/Data/Buildings");

            var asset = ScriptableObject.CreateInstance<BuildingDefinition>();
            asset.buildingName = _newBuildingName;
            asset.buildingType = _newBuildingType;
            asset.description  = _newDescription;
            asset.ownerNPC     = _newOwnerNPC;
            asset.icon         = _newIcon;
            asset.alwaysOpen   = _newAlwaysOpen;
            asset.openHour     = _newOpenHour;
            asset.closeHour    = _newCloseHour;
            asset.daysOpen     = (bool[])_newDaysOpen.Clone();

            var safeName = string.Concat(
                _newBuildingName.Split(System.IO.Path.GetInvalidFileNameChars()))
                .Replace(" ", "").Replace("'", "");
            var path = $"Assets/Data/Buildings/{safeName}.asset";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            LoadAllBuildings();
            ClearEditMode();
            EditorUtility.DisplayDialog("Building Created", $"'{_newBuildingName}' saved to:\n{path}", "OK");
        }
    }

    private string _saveButtonLabel => _editingBuilding != null ? "Save Changes" : "Create Building Asset";

    public void LoadBuildingForEditing(BuildingDefinition building)
    {
        _editingBuilding = building;
        _newBuildingName = building.buildingName;
        _newBuildingType = building.buildingType;
        _newDescription  = building.description;
        _newOwnerNPC     = building.ownerNPC;
        _newIcon         = building.icon;
        _newAlwaysOpen   = building.alwaysOpen;
        _newOpenHour     = building.openHour;
        _newCloseHour    = building.closeHour;
        _newDaysOpen     = building.daysOpen != null
            ? (bool[])building.daysOpen.Clone()
            : new bool[] { true, true, true, true, true, true, true };
        activeTab = 1;
    }

    private void ResetForm()
    {
        _newBuildingName = "";
        _newBuildingType = default;
        _newDescription  = "";
        _newOwnerNPC     = null;
        _newIcon         = null;
        _newAlwaysOpen   = true;
        _newOpenHour     = 9f;
        _newCloseHour    = 17f;
        _newDaysOpen     = new bool[] { true, true, true, true, true, true, true };
    }

    // ── Validate ──────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 2")]
    [ShowInInspector, ReadOnly, HideLabel]
    [GUIColor("@_validationIssues.Count == 0 ? new UnityEngine.Color(0.3f,0.9f,0.3f) : new UnityEngine.Color(1f,0.35f,0.35f)")]
    private string _validationStatus = "Press 'Run Validation' to check all buildings.";

    [ShowIf("@activeTab == 2 && _validationIssues.Count > 0")]
    [ShowInInspector, ReadOnly]
    [TableList(IsReadOnly = true, ShowPaging = false, AlwaysExpanded = true)]
    private List<ValidationIssue> _validationIssues = new();

    [ShowIf("@activeTab == 2")]
    [Button("Run Validation")]
    private void RunValidation()
    {
        _validationIssues = new List<ValidationIssue>();

        foreach (var guid in AssetDatabase.FindAssets("t:BuildingDefinition"))
        {
            var b = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (b == null) continue;

            if (string.IsNullOrEmpty(b.buildingName))
                _validationIssues.Add(new ValidationIssue(b, "Empty building name", this));
            if (b.icon == null)
                _validationIssues.Add(new ValidationIssue(b, "Missing icon sprite", this));
            if (b.ownerNPC == null &&
                b.buildingType != BuildingType.Public &&
                b.buildingType != BuildingType.Decorative &&
                b.buildingType != BuildingType.Home)
                _validationIssues.Add(new ValidationIssue(b, "Shop/Service has no owner NPC", this));
            if (!b.alwaysOpen)
            {
                if (Mathf.Approximately(b.openHour, b.closeHour))
                    _validationIssues.Add(new ValidationIssue(b, "Open hour equals close hour", this));
                if (b.daysOpen != null && b.daysOpen.All(d => !d))
                    _validationIssues.Add(new ValidationIssue(b, "All days set to closed", this));
            }
        }

        _validationStatus = _validationIssues.Count == 0
            ? "✓ All buildings valid"
            : $"{_validationIssues.Count} issue(s) found";
    }

    // ── Shared utilities ──────────────────────────────────────────────────────

    private static void EnsureFolderExists(string folderPath)
    {
        var parts = folderPath.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string FormatHour(float hour)
    {
        int h = Mathf.FloorToInt(hour) % 24;
        string period = h >= 12 ? "PM" : "AM";
        int display = h > 12 ? h - 12 : (h == 0 ? 12 : h);
        return $"{display}{period}";
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private enum BuildingTypeFilter { All, Shop, Home, Service, Public, Decorative }

    private class BuildingRow
    {
        private readonly BuildingDefinition _building;
        private readonly BuildingEditorWindow _window;

        public BuildingRow(BuildingDefinition building, BuildingEditorWindow window)
        {
            _building = building;
            _window   = window;
        }

        private void MarkDirty() { if (_building != null) { EditorUtility.SetDirty(_building); AssetDatabase.SaveAssets(); } }

        [ShowInInspector, PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public Sprite Icon
        {
            get => _building?.icon;
            set { if (_building != null) { _building.icon = value; MarkDirty(); } }
        }

        [ShowInInspector, TableColumnWidth(130)]
        public string Name
        {
            get => _building?.buildingName;
            set { if (_building != null) { _building.buildingName = value; MarkDirty(); } }
        }

        [ShowInInspector, TableColumnWidth(80, Resizable = false)]
        public BuildingType Type
        {
            get => _building != null ? _building.buildingType : default;
            set { if (_building != null) { _building.buildingType = value; MarkDirty(); } }
        }

        [ShowInInspector, TableColumnWidth(100)]
        public NPCDefinition Owner
        {
            get => _building?.ownerNPC;
            set { if (_building != null) { _building.ownerNPC = value; MarkDirty(); } }
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(100, Resizable = false)]
        public string Hours => _building != null
            ? (_building.alwaysOpen
                ? "Always Open"
                : $"{FormatHour(_building.openHour)}–{FormatHour(_building.closeHour)}")
            : "";

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Select() => _window?.LoadBuildingForEditing(_building);
    }

    private class ValidationIssue
    {
        private readonly BuildingDefinition _building;
        private readonly BuildingEditorWindow _window;

        public ValidationIssue(BuildingDefinition building, string issue, BuildingEditorWindow window)
        {
            _building = building;
            _window   = window;
            Issue     = issue;
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(130)]
        public string BuildingName => _building?.buildingName ?? "(unknown)";

        [ShowInInspector, ReadOnly]
        public string Issue { get; }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Select() => _window?.LoadBuildingForEditing(_building);
    }
}
