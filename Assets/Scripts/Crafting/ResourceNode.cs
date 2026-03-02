using System.Collections;
using UnityEngine;

public class ResourceNode : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition dropItem;
    [SerializeField] private int dropCount = 1;
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float respawnTime = 120f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip depletedSound;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] hitSprites;

    private int _currentHits;
    private bool _isDepleted;
    private Collider2D _col;
    private Sprite _originalSprite;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (spriteRenderer != null)
            _originalSprite = spriteRenderer.sprite;
    }

    public bool CanInteract(uint playerId) => !_isDepleted;

    public string GetInteractionPrompt() => $"Gather {dropItem?.itemName ?? "resource"}";

    public void Interact(uint playerId)
    {
        if (_isDepleted) return;

        _currentHits++;

        if (hitSprites != null && hitSprites.Length > 0 && spriteRenderer != null)
        {
            int spriteIndex = Mathf.Min(_currentHits - 1, hitSprites.Length - 1);
            spriteRenderer.sprite = hitSprites[spriteIndex];
        }

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);

        if (_currentHits >= maxHits)
            Deplete();
    }

    private void Deplete()
    {
        _isDepleted = true;

        if (_col != null)
            _col.enabled = false;

        if (depletedSound != null)
            AudioSource.PlayClipAtPoint(depletedSound, transform.position);

        if (dropItem != null && dropCount > 0)
            InventoryManager.Instance?.AddItem(dropItem, dropCount);

        if (respawnTime > 0f)
            StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        _currentHits = 0;
        _isDepleted = false;

        if (_col != null)
            _col.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.sprite = _originalSprite;
    }
}
