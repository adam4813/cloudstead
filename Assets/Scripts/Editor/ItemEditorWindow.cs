using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class ItemEditorWindow : OdinEditorWindow
{
    [MenuItem("Cloudstead/Item Editor")]
    private static void OpenWindow() => GetWindow<ItemEditorWindow>("Item Editor").Show();

    // ── Tab bar ───────────────────────────────────────────────────────────────

    [HideInInspector]
    public int activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_browseTabLabel"), GUIColor("@activeTab==0 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowBrowse() => activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_editTabLabel"), GUIColor("@activeTab==1 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowEdit() => activeTab = 1;

    [HorizontalGroup("Tabs"), Button("Validate"), GUIColor("@activeTab==2 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowValidate() => activeTab = 2;

    private string _browseTabLabel => $"Browse ({_allItems.Count})";
    private string _editTabLabel   => _editingItem != null ? $"✏  {_editingItem.itemName}" : "+ New Item";

    // ── Browse ────────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 0")]
    [LabelText("Search"), OnValueChanged("FilterItems")]
    [SerializeField] private string _searchQuery = "";

    [ShowIf("@activeTab == 0")]
    [ShowInInspector]
    [TableList(ShowPaging = false, AlwaysExpanded = true, ScrollViewHeight = 420)]
    private List<ItemRow> _displayedItems = new();

    private List<ItemDefinition> _allItems = new();

    [ShowIf("@activeTab == 0")]
    [Button("Refresh List")]
    private void LoadAllItems()
    {
        _allItems.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) _allItems.Add(item);
        }
        FilterItems();
    }

    private void FilterItems()
    {
        _displayedItems = _allItems
            .Where(i => string.IsNullOrEmpty(_searchQuery) ||
                        (i.itemName != null &&
                         i.itemName.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .Select(i => new ItemRow(i, this))
            .ToList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadAllItems();
    }

    // ── Edit / Create ─────────────────────────────────────────────────────────

    private ItemDefinition _editingItem;

    [ShowIf("@activeTab == 1 && _editingItem != null")]
    [ShowInInspector, HideLabel]
    [InfoBox("$_editingBanner", InfoMessageType.None)]
    private bool _editBannerAnchor; // anchor for InfoBox — value unused

    private string _editingBanner => _editingItem != null
        ? $"Editing: {_editingItem.itemName}   |   {AssetDatabase.GetAssetPath(_editingItem)}"
        : "";

    [ShowIf("@activeTab == 1 && _editingItem != null")]
    [Button("Clear — Start New Item"), GUIColor(1f, 0.8f, 0.6f)]
    private void ClearEditMode()
    {
        _editingItem = null;
        _newItemName = "";
        _newItemDescription = "";
        _newItemCategory = default;
        _newItemIcon = null;
        _newItemBuyPrice = 0;
        _newItemSellPrice = 0;
        _newItemStaminaRestore = 0;
        _newItemIsPlaceable = false;
    }

    [ShowIf("@activeTab == 1")]
    [LabelText("Item Name")]
    [SerializeField] private string _newItemName = "";

    [ShowIf("@activeTab == 1")]
    [LabelText("Description"), TextArea(2, 4)]
    [SerializeField] private string _newItemDescription = "";

    [ShowIf("@activeTab == 1")]
    [LabelText("Category")]
    [SerializeField] private ItemCategory _newItemCategory;

    [ShowIf("@activeTab == 1")]
    [LabelText("Icon"), PreviewField(64)]
    [SerializeField] private Sprite _newItemIcon;

    [ShowIf("@activeTab == 1")]
    [LabelText("Buy Price"), Min(0)]
    [SerializeField] private int _newItemBuyPrice;

    [ShowIf("@activeTab == 1")]
    [LabelText("Sell Price"), Min(0)]
    [SerializeField] private int _newItemSellPrice;

    [ShowIf("@activeTab == 1")]
    [LabelText("Stamina Restore"), Min(0)]
    [SerializeField] private int _newItemStaminaRestore;

    [ShowIf("@activeTab == 1")]
    [LabelText("Is Placeable")]
    [SerializeField] private bool _newItemIsPlaceable;

    [ShowIf("@activeTab == 1")]
    [Button("$_saveButtonLabel"), GUIColor("@_editingItem != null ? new UnityEngine.Color(0.7f,1f,0.7f) : UnityEngine.Color.white")]
    private void SaveItemAsset()
    {
        if (string.IsNullOrEmpty(_newItemName))
        {
            EditorUtility.DisplayDialog("Validation Error", "Item name cannot be empty.", "OK");
            return;
        }

        if (_editingItem != null)
        {
            // Update existing SO in place
            _editingItem.itemName        = _newItemName;
            _editingItem.description     = _newItemDescription;
            _editingItem.category        = _newItemCategory;
            _editingItem.icon            = _newItemIcon;
            _editingItem.buyPrice        = _newItemBuyPrice;
            _editingItem.sellPrice       = _newItemSellPrice;
            _editingItem.staminaRestore  = _newItemStaminaRestore;
            _editingItem.isPlaceable     = _newItemIsPlaceable;

            EditorUtility.SetDirty(_editingItem);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_editingItem);
            LoadAllItems();
        }
        else
        {
            // Create new asset
            var categoryFolder = $"Assets/Data/Items/{_newItemCategory}";
            EnsureFolderExists(categoryFolder);

            var asset = ScriptableObject.CreateInstance<ItemDefinition>();
            asset.itemName       = _newItemName;
            asset.description    = _newItemDescription;
            asset.category       = _newItemCategory;
            asset.icon           = _newItemIcon;
            asset.buyPrice       = _newItemBuyPrice;
            asset.sellPrice      = _newItemSellPrice;
            asset.staminaRestore = _newItemStaminaRestore;
            asset.isPlaceable    = _newItemIsPlaceable;

            var safeName = string.Concat(
                _newItemName.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ", "");
            var path = $"{categoryFolder}/{safeName}.asset";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            LoadAllItems();
            ClearEditMode();
            EditorUtility.DisplayDialog("Item Created", $"'{_newItemName}' saved to:\n{path}", "OK");
        }
    }

    private string _saveButtonLabel => _editingItem != null ? "Save Changes" : "Create Item Asset";

    public void LoadItemForEditing(ItemDefinition item)
    {
        _editingItem         = item;
        _newItemName         = item.itemName;
        _newItemDescription  = item.description;
        _newItemCategory     = item.category;
        _newItemIcon         = item.icon;
        _newItemBuyPrice     = item.buyPrice;
        _newItemSellPrice    = item.sellPrice;
        _newItemStaminaRestore = item.staminaRestore;
        _newItemIsPlaceable  = item.isPlaceable;
        activeTab = 1;
    }

    // ── Validate ──────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 2")]
    [ShowInInspector, ReadOnly, HideLabel]
    [GUIColor("@_validationIssues.Count == 0 ? new UnityEngine.Color(0.3f,0.9f,0.3f) : new UnityEngine.Color(1f,0.35f,0.35f)")]
    private string _validationStatus = "Press 'Run Validation' to check all items.";

    [ShowIf("@activeTab == 2 && _validationIssues.Count > 0")]
    [ShowInInspector, ReadOnly]
    [TableList(IsReadOnly = true, ShowPaging = false, AlwaysExpanded = true)]
    private List<ValidationIssue> _validationIssues = new();

    [ShowIf("@activeTab == 2")]
    [Button("Run Validation")]
    private void RunValidation()
    {
        _validationIssues = new List<ValidationIssue>();

        foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) continue;

            if (item.icon == null)
                _validationIssues.Add(new ValidationIssue(item, "Missing icon", this));
            if (string.IsNullOrEmpty(item.itemName))
                _validationIssues.Add(new ValidationIssue(item, "Empty item name", this));
            if (item.sellPrice == 0 && item.buyPrice == 0)
                _validationIssues.Add(new ValidationIssue(item, "Both sellPrice and buyPrice are 0", this));
            if (item.staminaRestore > 0 && item.category != ItemCategory.General)
                _validationIssues.Add(new ValidationIssue(item, $"staminaRestore={item.staminaRestore} on non-consumable category '{item.category}'", this));
        }

        _validationStatus = _validationIssues.Count == 0
            ? "✓ All items valid"
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

    // ── Inner types ───────────────────────────────────────────────────────────

    private class ItemRow
    {
        private readonly ItemDefinition _item;
        private readonly ItemEditorWindow _window;

        public ItemRow(ItemDefinition item, ItemEditorWindow window)
        {
            _item   = item;
            _window = window;
        }

        [ShowInInspector, PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public Sprite Icon
        {
            get => _item?.icon;
            set { if (_item != null) { _item.icon = value; EditorUtility.SetDirty(_item); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(120)]
        public string Name
        {
            get => _item?.itemName;
            set { if (_item != null) { _item.itemName = value; EditorUtility.SetDirty(_item); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(110, Resizable = false)]
        public ItemCategory Category
        {
            get => _item != null ? _item.category : default;
            set { if (_item != null) { _item.category = value; EditorUtility.SetDirty(_item); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(70, Resizable = false)]
        public int SellPrice
        {
            get => _item?.sellPrice ?? 0;
            set { if (_item != null) { _item.sellPrice = value; EditorUtility.SetDirty(_item); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(70, Resizable = false)]
        public int BuyPrice
        {
            get => _item?.buyPrice ?? 0;
            set { if (_item != null) { _item.buyPrice = value; EditorUtility.SetDirty(_item); AssetDatabase.SaveAssets(); } }
        }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Select() => _window?.LoadItemForEditing(_item);
    }

    private class ValidationIssue
    {
        private readonly ItemDefinition _item;
        private readonly ItemEditorWindow _window;

        public ValidationIssue(ItemDefinition item, string issue, ItemEditorWindow window)
        {
            _item   = item;
            _window = window;
            Issue   = issue;
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(120, Resizable = true)]
        public string ItemName => _item?.itemName ?? "(unknown)";

        [ShowInInspector, ReadOnly]
        public string Issue { get; }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Select() => _window?.LoadItemForEditing(_item);
    }
}
