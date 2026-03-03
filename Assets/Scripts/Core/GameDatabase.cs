using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Cloudstead/Core/Game Database", fileName = "GameDatabase")]
public class GameDatabase : ScriptableObject
{
    [FoldoutGroup("Items")]
    [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];

    [FoldoutGroup("Crops")]
    [SerializeField] private CropDefinition[] crops = new CropDefinition[0];

    [FoldoutGroup("Resource Nodes")]
    [SerializeField] private ResourceNodeDefinition[] resourceNodes = new ResourceNodeDefinition[0];

    public IReadOnlyList<ItemDefinition> Items => items;
    public IReadOnlyList<CropDefinition> Crops => crops;
    public IReadOnlyList<ResourceNodeDefinition> ResourceNodes => resourceNodes;

    private Dictionary<string, ItemDefinition> _itemLookup;
    private Dictionary<string, CropDefinition> _cropLookup;
    private Dictionary<string, ResourceNodeDefinition> _nodeLookup;

    public ItemDefinition GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (_itemLookup == null) BuildItemLookup();
        _itemLookup.TryGetValue(itemId, out var result);
        return result;
    }

    public CropDefinition GetCrop(string cropName)
    {
        if (string.IsNullOrEmpty(cropName)) return null;
        if (_cropLookup == null) BuildCropLookup();
        _cropLookup.TryGetValue(cropName, out var result);
        return result;
    }

    public ResourceNodeDefinition GetNode(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName)) return null;
        if (_nodeLookup == null) BuildNodeLookup();
        _nodeLookup.TryGetValue(nodeName, out var result);
        return result;
    }

    private void BuildItemLookup()
    {
        _itemLookup = new Dictionary<string, ItemDefinition>();
        foreach (var item in items)
        {
            if (item == null) continue;
            var key = item.ItemId;
            if (_itemLookup.ContainsKey(key))
                Debug.LogWarning($"[GameDatabase] Duplicate item key '{key}' — skipping {item.name}");
            else
                _itemLookup[key] = item;
        }
    }

    private void BuildCropLookup()
    {
        _cropLookup = new Dictionary<string, CropDefinition>();
        foreach (var crop in crops)
        {
            if (crop == null) continue;
            var key = crop.cropName;
            if (_cropLookup.ContainsKey(key))
                Debug.LogWarning($"[GameDatabase] Duplicate crop key '{key}' — skipping {crop.name}");
            else
                _cropLookup[key] = crop;
        }
    }

    private void BuildNodeLookup()
    {
        _nodeLookup = new Dictionary<string, ResourceNodeDefinition>();
        foreach (var node in resourceNodes)
        {
            if (node == null) continue;
            var key = node.nodeName;
            if (_nodeLookup.ContainsKey(key))
                Debug.LogWarning($"[GameDatabase] Duplicate node key '{key}' — skipping {node.name}");
            else
                _nodeLookup[key] = node;
        }
    }

#if UNITY_EDITOR
    [Button("Populate From Project", ButtonSizes.Large)]
    [FoldoutGroup("Editor Tools")]
    private void PopulateFromProject()
    {
        items = FindAllAssets<ItemDefinition>();
        crops = FindAllAssets<CropDefinition>();
        resourceNodes = FindAllAssets<ResourceNodeDefinition>();

        // Invalidate cached lookups
        _itemLookup = null;
        _cropLookup = null;
        _nodeLookup = null;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[GameDatabase] Populated: {items.Length} items, {crops.Length} crops, {resourceNodes.Length} resource nodes");
    }

    private static T[] FindAllAssets<T>() where T : ScriptableObject
    {
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var results = new T[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            results[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return results;
    }
#endif
}
