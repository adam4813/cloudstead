
using UnityEngine;

public class ResourceNode : MonoBehaviour, IInteractable
{
    [SerializeField] private ResourceNodeDefinition definition;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int _currentHits;
    private bool _isDepleted;
    private Collider2D _col;
    private Sprite _originalSprite;

    public ResourceNodeDefinition Definition => definition;
    public int CurrentHits => _currentHits;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (spriteRenderer != null)
            _originalSprite = spriteRenderer.sprite;
    }

    private void Start()
    {
        if (definition != null)
            ResourceNodeManager.Instance?.RegisterNode(definition);
    }

    private void OnDestroy()
    {
        if (definition != null)
            ResourceNodeManager.Instance?.UnregisterNode(definition);
    }

    /// <summary>Called by ResourceNodeManager when dynamically spawning a respawned node.</summary>
    public void Initialize(ResourceNodeDefinition def)
    {
        definition = def;
        if (spriteRenderer != null)
            _originalSprite = spriteRenderer.sprite;
    }

    public bool CanInteract(uint playerId) => !_isDepleted && definition != null;

    public string GetInteractionPrompt() => $"Gather {definition?.nodeName ?? "resource"}";

    public void Interact(uint playerId)
    {
        if (_isDepleted || definition == null) return;

        _currentHits++;

        if (definition.hitSprites != null && definition.hitSprites.Length > 0 && spriteRenderer != null)
        {
            int spriteIndex = Mathf.Min(_currentHits - 1, definition.hitSprites.Length - 1);
            spriteRenderer.sprite = definition.hitSprites[spriteIndex];
        }

        if (definition.hitSound != null)
            AudioSource.PlayClipAtPoint(definition.hitSound, transform.position);

        if (_currentHits >= definition.maxHits)
            Deplete();
    }

    public void Restore(int hits)
    {
        _currentHits = hits;
        _isDepleted = false;
        if (_col != null) _col.enabled = true;
        if (definition != null && definition.hitSprites != null && definition.hitSprites.Length > 0
            && spriteRenderer != null && hits > 0)
        {
            int spriteIndex = Mathf.Min(hits - 1, definition.hitSprites.Length - 1);
            spriteRenderer.sprite = definition.hitSprites[spriteIndex];
        }
    }

    private void Deplete()
    {
        _isDepleted = true;

        if (_col != null)
            _col.enabled = false;

        if (definition.depletedSound != null)
            AudioSource.PlayClipAtPoint(definition.depletedSound, transform.position);

        InventoryManager.Instance?.AddItem(
            definition.dropItem,
            Random.Range(definition.dropCountMin, definition.dropCountMax + 1));

        EventBus.Publish(new ResourceNodeDepletedEvent { Definition = definition });

        Destroy(gameObject);
    }
}
