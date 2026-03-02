using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class CropEditorWindow : OdinEditorWindow
{
    [MenuItem("Cloudstead/Crop Editor")]
    private static void OpenWindow() => GetWindow<CropEditorWindow>("Crop Editor").Show();

    // ── Tab bar ───────────────────────────────────────────────────────────────

    [HideInInspector]
    public int activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_browseTabLabel"), GUIColor("@activeTab==0 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowBrowse() => activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_editTabLabel"), GUIColor("@activeTab==1 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowEdit() => activeTab = 1;

    [HorizontalGroup("Tabs"), Button("Validate"), GUIColor("@activeTab==2 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowValidate() => activeTab = 2;

    private string _browseTabLabel => $"Browse ({_allCrops.Count})";
    private string _editTabLabel   => _editingCrop != null ? $"✏  {_editingCrop.cropName}" : "+ New Crop";

    // ── Browse ────────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 0")]
    [LabelText("Search"), OnValueChanged("FilterCrops")]
    [SerializeField] private string _searchQuery = "";

    [ShowIf("@activeTab == 0")]
    [ShowInInspector]
    [TableList(ShowPaging = false, AlwaysExpanded = true, ScrollViewHeight = 440)]
    private List<CropRow> _displayedCrops = new();

    private List<CropDefinition> _allCrops = new();

    [ShowIf("@activeTab == 0")]
    [Button("Refresh List")]
    private void LoadAllCrops()
    {
        _allCrops.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:CropDefinition"))
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (crop != null) _allCrops.Add(crop);
        }
        FilterCrops();
    }

    private void FilterCrops()
    {
        _displayedCrops = _allCrops
            .Where(c => string.IsNullOrEmpty(_searchQuery) ||
                        (c.cropName != null &&
                         c.cropName.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .Select(c => new CropRow(c, this))
            .ToList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadAllCrops();
    }

    // ── Edit / Create ─────────────────────────────────────────────────────────

    private CropDefinition _editingCrop;

    [ShowIf("@activeTab == 1 && _editingCrop != null")]
    [ShowInInspector, HideLabel]
    [InfoBox("$_editingBanner", InfoMessageType.None)]
    private bool _editBannerAnchor;

    private string _editingBanner => _editingCrop != null
        ? $"Editing: {_editingCrop.cropName}   |   {AssetDatabase.GetAssetPath(_editingCrop)}"
        : "";

    [ShowIf("@activeTab == 1 && _editingCrop != null")]
    [Button("Clear — Start New Crop"), GUIColor(1f, 0.8f, 0.6f)]
    private void ClearEditMode()
    {
        _editingCrop = null;
        ResetForm();
    }

    // Basic Info
    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Basic Info"), LabelText("Crop Name")]
    [SerializeField] private string _cropName = "";

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Basic Info"), LabelText("Description"), TextArea(2, 3)]
    [SerializeField] private string _cropDescription = "";

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Basic Info"), LabelText("Icon"), PreviewField(64)]
    [SerializeField] private Sprite _cropIcon;

    // Growth
    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Growth"), LabelText("Seed Item"), PreviewField(32, ObjectFieldAlignment.Left)]
    [SerializeField] private ItemDefinition _seedItem;

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Growth"), LabelText("Growth Days"), Min(1)]
    [SerializeField] private int _growthDays = 4;

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Growth"), LabelText("Grow Seasons")]
    [SerializeField] private Season[] _growSeasons = new Season[0];

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Growth"), LabelText("Stage Sprites (Seed → Sprout → Growing → Mature)")]
    [ListDrawerSettings(ShowPaging = false, ShowItemCount = false, NumberOfItemsPerPage = 4)]
    [SerializeField] private Sprite[] _stageSprites = new Sprite[4];

    // Harvest
    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Harvest"), LabelText("Harvest Outputs")]
    [ListDrawerSettings(ShowPaging = false, ShowItemCount = true)]
    [SerializeField] private List<EditableHarvestOutput> _harvestOutputs = new();

    [ShowIf("@activeTab == 1")]
    [FoldoutGroup("Harvest"), LabelText("Regrow Days (-1 = no regrowth)")]
    [SerializeField] private int _regrowDays = -1;

    [ShowIf("@activeTab == 1")]
    [Button("$_saveButtonLabel"), GUIColor("@_editingCrop != null ? new UnityEngine.Color(0.7f,1f,0.7f) : UnityEngine.Color.white")]
    private void SaveCrop()
    {
        if (string.IsNullOrEmpty(_cropName))
        {
            EditorUtility.DisplayDialog("Validation Error", "Crop name cannot be empty.", "OK");
            return;
        }

        var harvestOutputs = _harvestOutputs
            .Select(h => new HarvestOutput { item = h.Item, minYield = h.MinYield, maxYield = h.MaxYield })
            .ToArray();

        if (_editingCrop != null)
        {
            _editingCrop.cropName      = _cropName;
            _editingCrop.description   = _cropDescription;
            _editingCrop.icon          = _cropIcon;
            _editingCrop.seedItem      = _seedItem;
            _editingCrop.growthDays    = _growthDays;
            _editingCrop.growSeasons   = _growSeasons;
            _editingCrop.stageSprites  = _stageSprites;
            _editingCrop.harvestOutputs = harvestOutputs;
            _editingCrop.regrowDays    = _regrowDays;

            EditorUtility.SetDirty(_editingCrop);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_editingCrop);
            LoadAllCrops();
        }
        else
        {
            EnsureFolderExists("Assets/Data/Crops");
            var safeName = string.Concat(_cropName.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ", "");
            var path     = $"Assets/Data/Crops/{safeName}.asset";

            var asset            = ScriptableObject.CreateInstance<CropDefinition>();
            asset.cropName       = _cropName;
            asset.description    = _cropDescription;
            asset.icon           = _cropIcon;
            asset.seedItem       = _seedItem;
            asset.growthDays     = _growthDays;
            asset.growSeasons    = _growSeasons;
            asset.stageSprites   = _stageSprites;
            asset.harvestOutputs = harvestOutputs;
            asset.regrowDays     = _regrowDays;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            LoadAllCrops();
            ClearEditMode();
            EditorUtility.DisplayDialog("Crop Created", $"'{_cropName}' saved to:\n{path}", "OK");
        }
    }

    private string _saveButtonLabel => _editingCrop != null ? "Save Changes" : "Create Crop Asset";

    public void LoadCropForEditing(CropDefinition crop)
    {
        _editingCrop      = crop;
        _cropName         = crop.cropName;
        _cropDescription  = crop.description;
        _cropIcon         = crop.icon;
        _seedItem         = crop.seedItem;
        _growthDays       = crop.growthDays;
        _growSeasons      = crop.growSeasons ?? new Season[0];
        _stageSprites     = crop.stageSprites ?? new Sprite[4];
        _regrowDays       = crop.regrowDays;

        _harvestOutputs = new List<EditableHarvestOutput>();
        if (crop.harvestOutputs != null)
        {
            foreach (var h in crop.harvestOutputs)
                _harvestOutputs.Add(new EditableHarvestOutput { Item = h.item, MinYield = h.minYield, MaxYield = h.maxYield });
        }

        activeTab = 1;
    }

    private void ResetForm()
    {
        _cropName        = "";
        _cropDescription = "";
        _cropIcon        = null;
        _seedItem        = null;
        _growthDays      = 4;
        _growSeasons     = new Season[0];
        _stageSprites    = new Sprite[4];
        _harvestOutputs  = new List<EditableHarvestOutput>();
        _regrowDays      = -1;
    }

    // ── Validate ──────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 2")]
    [ShowInInspector, ReadOnly, HideLabel]
    [GUIColor("@_validationIssues.Count == 0 ? new UnityEngine.Color(0.3f,0.9f,0.3f) : new UnityEngine.Color(1f,0.35f,0.35f)")]
    private string _validationStatus = "Press 'Run Validation' to check all crops.";

    [ShowIf("@activeTab == 2 && _validationIssues.Count > 0")]
    [ShowInInspector, ReadOnly]
    [TableList(IsReadOnly = true, ShowPaging = false, AlwaysExpanded = true)]
    private List<CropIssue> _validationIssues = new();

    [ShowIf("@activeTab == 2")]
    [Button("Run Validation")]
    private void RunValidation()
    {
        _validationIssues = new List<CropIssue>();

        foreach (var guid in AssetDatabase.FindAssets("t:CropDefinition"))
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (crop == null) continue;

            if (string.IsNullOrEmpty(crop.cropName))
                _validationIssues.Add(new CropIssue(crop, "Empty crop name", this));
            if (crop.seedItem == null)
                _validationIssues.Add(new CropIssue(crop, "Missing seed item reference", this));
            if (crop.harvestOutputs == null || crop.harvestOutputs.Length == 0)
                _validationIssues.Add(new CropIssue(crop, "No harvest outputs defined", this));
            if (crop.growthDays < 1)
                _validationIssues.Add(new CropIssue(crop, "growthDays must be >= 1", this));
            if (crop.stageSprites == null || crop.stageSprites.All(s => s == null))
                _validationIssues.Add(new CropIssue(crop, "No stage sprites assigned", this));
        }

        _validationStatus = _validationIssues.Count == 0
            ? "✓ All crops valid"
            : $"{_validationIssues.Count} issue(s) found";
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private static void EnsureFolderExists(string folderPath)
    {
        var parts   = folderPath.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    [System.Serializable]
    public class EditableHarvestOutput
    {
        [PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public ItemDefinition Item;

        [LabelText("Min"), Min(1), TableColumnWidth(50, Resizable = false)]
        public int MinYield = 1;

        [LabelText("Max"), Min(1), TableColumnWidth(50, Resizable = false)]
        public int MaxYield = 1;
    }

    private class CropRow
    {
        private readonly CropDefinition   _crop;
        private readonly CropEditorWindow _window;

        public CropRow(CropDefinition crop, CropEditorWindow window)
        {
            _crop   = crop;
            _window = window;
        }

        [ShowInInspector, PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public Sprite Icon
        {
            get => _crop?.icon;
            set { if (_crop != null) { _crop.icon = value; EditorUtility.SetDirty(_crop); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(100)]
        public string Name
        {
            get => _crop?.cropName;
            set { if (_crop != null) { _crop.cropName = value; EditorUtility.SetDirty(_crop); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(80), ReadOnly]
        public string Seed => _crop?.seedItem?.itemName ?? "(none)";

        [ShowInInspector, TableColumnWidth(55, Resizable = false)]
        public int GrowDays
        {
            get => _crop?.growthDays ?? 1;
            set { if (_crop != null) { _crop.growthDays = Mathf.Max(1, value); EditorUtility.SetDirty(_crop); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(130)]
        public string Seasons => _crop?.growSeasons != null
            ? string.Join(", ", _crop.growSeasons.Select(s => s.ToString()))
            : "";

        [ShowInInspector, ReadOnly, TableColumnWidth(80)]
        public string Regrows => _crop != null ? (_crop.regrowDays > 0 ? $"Every {_crop.regrowDays}d" : "No") : "";

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Edit() => _window?.LoadCropForEditing(_crop);
    }

    private class CropIssue
    {
        private readonly CropDefinition   _crop;
        private readonly CropEditorWindow _window;

        public CropIssue(CropDefinition crop, string issue, CropEditorWindow window)
        {
            _crop   = crop;
            _window = window;
            Issue   = issue;
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(120)]
        public string CropName => _crop?.cropName ?? "(unknown)";

        [ShowInInspector, ReadOnly]
        public string Issue { get; }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Edit() => _window?.LoadCropForEditing(_crop);
    }
}
