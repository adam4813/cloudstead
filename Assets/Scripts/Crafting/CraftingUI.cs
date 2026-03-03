using UnityEngine;
using UnityEngine.UI;

public class CraftingUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform recipeListParent;
    [SerializeField] private GameObject recipeEntryPrefab;
    [SerializeField] private AudioClip craftSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private Button closeButton;

    private RecipeDefinition _selectedRecipe;
    private bool _isOpen;

    private void Start()
    {
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (panel != null) panel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        if (_isOpen)
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
    }

    public void Open()
    {
        _isOpen = true;
        if (panel != null) panel.SetActive(true);
        EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
        RefreshRecipes();
    }

    public void Close()
    {
        _isOpen = false;
        if (panel != null) panel.SetActive(false);
        EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        _selectedRecipe = null;

        EventBus.Publish(new GameStateChangedEvent
        {
            Previous = GameState.Crafting,
            Current = GameState.Playing
        });
    }

    public void RefreshRecipes()
    {
        if (recipeListParent == null || recipeEntryPrefab == null) return;

        for (int i = recipeListParent.childCount - 1; i >= 0; i--)
            Destroy(recipeListParent.GetChild(i).gameObject);

        if (CraftingManager.Instance == null) return;

        foreach (var recipe in CraftingManager.Instance.GetAllRecipes())
        {
            var go = Instantiate(recipeEntryPrefab, recipeListParent);
            var entry = go.GetComponent<RecipeEntryUI>();
            entry?.Setup(recipe, this);
        }
    }

    public void OnRecipeSelected(RecipeDefinition recipe)
    {
        _selectedRecipe = recipe;
    }

    public void OnCraftButton()
    {
        if (_selectedRecipe == null) return;

        bool success = CraftingManager.Instance != null && CraftingManager.Instance.Craft(_selectedRecipe);

        if (success)
        {
            if (craftSound != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(craftSound, Camera.main.transform.position);
        }
        else
        {
            if (failSound != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(failSound, Camera.main.transform.position);
        }

        RefreshRecipes();
    }

    private void OnInventoryChanged(InventoryChangedEvent evt)
    {
        if (_isOpen)
            RefreshRecipes();
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (_isOpen && evt.Current != GameState.Crafting)
            Close();
    }
}
