using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class RecipeEditorWindow : OdinEditorWindow
{
    [MenuItem("Cloudstead/Recipe Editor")]
    private static void OpenWindow() => GetWindow<RecipeEditorWindow>("Recipe Editor").Show();

    // ── Tab bar ───────────────────────────────────────────────────────────────

    [HideInInspector]
    public int activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_browseTabLabel"), GUIColor("@activeTab==0 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowBrowse() => activeTab = 0;

    [HorizontalGroup("Tabs"), Button("$_editTabLabel"), GUIColor("@activeTab==1 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowEdit() => activeTab = 1;

    [HorizontalGroup("Tabs"), Button("Validate"), GUIColor("@activeTab==2 ? new UnityEngine.Color(0.55f,0.85f,1f) : UnityEngine.Color.white")]
    private void ShowValidate() => activeTab = 2;

    private string _browseTabLabel => $"Browse ({_allRecipes.Count})";
    private string _editTabLabel   => _editingRecipe != null ? $"✏  {_editingRecipe.recipeName}" : "+ New Recipe";

    // ── Browse ────────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 0")]
    [LabelText("Search"), OnValueChanged("FilterRecipes")]
    [SerializeField] private string _searchQuery = "";

    [ShowIf("@activeTab == 0")]
    [ShowInInspector]
    [TableList(ShowPaging = false, AlwaysExpanded = true, ScrollViewHeight = 440)]
    private List<RecipeRow> _displayedRecipes = new();

    private List<RecipeDefinition> _allRecipes = new();

    [ShowIf("@activeTab == 0")]
    [Button("Refresh List")]
    private void LoadAllRecipes()
    {
        _allRecipes.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:RecipeDefinition"))
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe != null) _allRecipes.Add(recipe);
        }
        FilterRecipes();
    }

    private void FilterRecipes()
    {
        _displayedRecipes = _allRecipes
            .Where(r => string.IsNullOrEmpty(_searchQuery) ||
                        (r.recipeName != null &&
                         r.recipeName.IndexOf(_searchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0))
            .Select(r => new RecipeRow(r, this))
            .ToList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        LoadAllRecipes();
    }

    // ── Edit / Create ─────────────────────────────────────────────────────────

    private RecipeDefinition _editingRecipe;

    [ShowIf("@activeTab == 1 && _editingRecipe != null")]
    [ShowInInspector, HideLabel]
    [InfoBox("$_editingBanner", InfoMessageType.None)]
    private bool _editBannerAnchor;

    private string _editingBanner => _editingRecipe != null
        ? $"Editing: {_editingRecipe.recipeName}   |   {AssetDatabase.GetAssetPath(_editingRecipe)}"
        : "";

    [ShowIf("@activeTab == 1 && _editingRecipe != null")]
    [Button("Clear — Start New Recipe"), GUIColor(1f, 0.8f, 0.6f)]
    private void ClearEditMode()
    {
        _editingRecipe = null;
        ResetForm();
    }

    [ShowIf("@activeTab == 1")]
    [LabelText("Recipe Name")]
    [SerializeField] private string _recipeName = "";

    [ShowIf("@activeTab == 1")]
    [LabelText("Description"), TextArea(2, 3)]
    [SerializeField] private string _recipeDescription = "";

    [ShowIf("@activeTab == 1")]
    [LabelText("Craft Time (seconds)"), Min(0.1f)]
    [SerializeField] private float _craftTime = 1f;

    [ShowIf("@activeTab == 1")]
    [LabelText("Ingredients")]
    [ListDrawerSettings(ShowPaging = false, DraggableItems = true, ShowItemCount = true)]
    [SerializeField] private List<EditableIngredient> _ingredients = new();

    [ShowIf("@activeTab == 1")]
    [BoxGroup("Output")]
    [LabelText("Output Item"), PreviewField(48, ObjectFieldAlignment.Left)]
    [SerializeField] private ItemDefinition _outputItem;

    [ShowIf("@activeTab == 1")]
    [BoxGroup("Output")]
    [LabelText("Output Count"), Min(1)]
    [SerializeField] private int _outputCount = 1;

    [ShowIf("@activeTab == 1")]
    [Button("$_saveButtonLabel"), GUIColor("@_editingRecipe != null ? new UnityEngine.Color(0.7f,1f,0.7f) : UnityEngine.Color.white")]
    private void SaveRecipe()
    {
        if (string.IsNullOrEmpty(_recipeName))
        {
            EditorUtility.DisplayDialog("Validation Error", "Recipe name cannot be empty.", "OK");
            return;
        }
        if (_outputItem == null)
        {
            EditorUtility.DisplayDialog("Validation Error", "Output item must be assigned.", "OK");
            return;
        }
        if (_ingredients.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Error", "Recipe must have at least one ingredient.", "OK");
            return;
        }

        var inputItems  = _ingredients.Select(i => i.Item).ToArray();
        var inputCounts = _ingredients.Select(i => i.Count).ToArray();

        if (_editingRecipe != null)
        {
            _editingRecipe.recipeName  = _recipeName;
            _editingRecipe.description = _recipeDescription;
            _editingRecipe.craftTime   = _craftTime;
            _editingRecipe.inputItems  = inputItems;
            _editingRecipe.inputCounts = inputCounts;
            _editingRecipe.outputItem  = _outputItem;
            _editingRecipe.outputCount = _outputCount;

            EditorUtility.SetDirty(_editingRecipe);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_editingRecipe);
            LoadAllRecipes();
        }
        else
        {
            EnsureFolderExists("Assets/Data/Recipes");
            var safeName = string.Concat(_recipeName.Split(System.IO.Path.GetInvalidFileNameChars())).Replace(" ", "");
            var path     = $"Assets/Data/Recipes/{safeName}.asset";

            var asset         = ScriptableObject.CreateInstance<RecipeDefinition>();
            asset.recipeName  = _recipeName;
            asset.description = _recipeDescription;
            asset.craftTime   = _craftTime;
            asset.inputItems  = inputItems;
            asset.inputCounts = inputCounts;
            asset.outputItem  = _outputItem;
            asset.outputCount = _outputCount;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            LoadAllRecipes();
            ClearEditMode();
            EditorUtility.DisplayDialog("Recipe Created", $"'{_recipeName}' saved to:\n{path}", "OK");
        }
    }

    private string _saveButtonLabel => _editingRecipe != null ? "Save Changes" : "Create Recipe Asset";

    public void LoadRecipeForEditing(RecipeDefinition recipe)
    {
        _editingRecipe      = recipe;
        _recipeName         = recipe.recipeName;
        _recipeDescription  = recipe.description;
        _craftTime          = recipe.craftTime;
        _outputItem         = recipe.outputItem;
        _outputCount        = recipe.outputCount;
        _ingredients        = new List<EditableIngredient>();

        if (recipe.inputItems != null)
        {
            for (int i = 0; i < recipe.inputItems.Length; i++)
            {
                _ingredients.Add(new EditableIngredient
                {
                    Item  = recipe.inputItems[i],
                    Count = (recipe.inputCounts != null && i < recipe.inputCounts.Length) ? recipe.inputCounts[i] : 1,
                });
            }
        }

        activeTab = 1;
    }

    private void ResetForm()
    {
        _recipeName        = "";
        _recipeDescription = "";
        _craftTime         = 1f;
        _outputItem        = null;
        _outputCount       = 1;
        _ingredients       = new List<EditableIngredient>();
    }

    // ── Validate ──────────────────────────────────────────────────────────────

    [ShowIf("@activeTab == 2")]
    [ShowInInspector, ReadOnly, HideLabel]
    [GUIColor("@_validationIssues.Count == 0 ? new UnityEngine.Color(0.3f,0.9f,0.3f) : new UnityEngine.Color(1f,0.35f,0.35f)")]
    private string _validationStatus = "Press 'Run Validation' to check all recipes.";

    [ShowIf("@activeTab == 2 && _validationIssues.Count > 0")]
    [ShowInInspector, ReadOnly]
    [TableList(IsReadOnly = true, ShowPaging = false, AlwaysExpanded = true)]
    private List<RecipeIssue> _validationIssues = new();

    [ShowIf("@activeTab == 2")]
    [Button("Run Validation")]
    private void RunValidation()
    {
        _validationIssues = new List<RecipeIssue>();

        foreach (var guid in AssetDatabase.FindAssets("t:RecipeDefinition"))
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe == null) continue;

            if (string.IsNullOrEmpty(recipe.recipeName))
                _validationIssues.Add(new RecipeIssue(recipe, "Empty recipe name", this));
            if (recipe.outputItem == null)
                _validationIssues.Add(new RecipeIssue(recipe, "Missing output item", this));
            if (recipe.inputItems == null || recipe.inputItems.Length == 0)
                _validationIssues.Add(new RecipeIssue(recipe, "No ingredients", this));
            if (recipe.inputItems != null && recipe.inputCounts != null &&
                recipe.inputItems.Length != recipe.inputCounts.Length)
                _validationIssues.Add(new RecipeIssue(recipe, "inputItems / inputCounts length mismatch", this));
            if (recipe.inputItems != null)
            {
                for (int i = 0; i < recipe.inputItems.Length; i++)
                {
                    if (recipe.inputItems[i] == null)
                        _validationIssues.Add(new RecipeIssue(recipe, $"Ingredient [{i}] is null", this));
                }
            }
        }

        _validationStatus = _validationIssues.Count == 0
            ? "✓ All recipes valid"
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
    public class EditableIngredient
    {
        [PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public ItemDefinition Item;

        [Min(1), TableColumnWidth(50, Resizable = false)]
        public int Count = 1;
    }

    private class RecipeRow
    {
        private readonly RecipeDefinition  _recipe;
        private readonly RecipeEditorWindow _window;

        public RecipeRow(RecipeDefinition recipe, RecipeEditorWindow window)
        {
            _recipe = recipe;
            _window = window;
        }

        [ShowInInspector, TableColumnWidth(150)]
        public string Name
        {
            get => _recipe?.recipeName;
            set { if (_recipe != null) { _recipe.recipeName = value; EditorUtility.SetDirty(_recipe); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(180)]
        public string Ingredients
        {
            get
            {
                if (_recipe?.inputItems == null) return "";
                var parts = new List<string>();
                for (int i = 0; i < _recipe.inputItems.Length; i++)
                {
                    var item  = _recipe.inputItems[i];
                    var count = (_recipe.inputCounts != null && i < _recipe.inputCounts.Length) ? _recipe.inputCounts[i] : 1;
                    parts.Add($"{count}x {item?.itemName ?? "?"}");
                }
                return string.Join(", ", parts);
            }
        }

        [ShowInInspector, PreviewField(32, ObjectFieldAlignment.Left), HideLabel, TableColumnWidth(44, Resizable = false)]
        public ItemDefinition Output
        {
            get => _recipe?.outputItem;
            set { if (_recipe != null) { _recipe.outputItem = value; EditorUtility.SetDirty(_recipe); AssetDatabase.SaveAssets(); } }
        }

        [ShowInInspector, TableColumnWidth(55, Resizable = false)]
        public float CraftTime
        {
            get => _recipe?.craftTime ?? 1f;
            set { if (_recipe != null) { _recipe.craftTime = Mathf.Max(0.1f, value); EditorUtility.SetDirty(_recipe); AssetDatabase.SaveAssets(); } }
        }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Edit() => _window?.LoadRecipeForEditing(_recipe);
    }

    private class RecipeIssue
    {
        private readonly RecipeDefinition   _recipe;
        private readonly RecipeEditorWindow _window;

        public RecipeIssue(RecipeDefinition recipe, string issue, RecipeEditorWindow window)
        {
            _recipe = recipe;
            _window = window;
            Issue   = issue;
        }

        [ShowInInspector, ReadOnly, TableColumnWidth(140)]
        public string RecipeName => _recipe?.recipeName ?? "(unknown)";

        [ShowInInspector, ReadOnly]
        public string Issue { get; }

        [Button("Edit"), TableColumnWidth(55, Resizable = false)]
        public void Edit() => _window?.LoadRecipeForEditing(_recipe);
    }
}
