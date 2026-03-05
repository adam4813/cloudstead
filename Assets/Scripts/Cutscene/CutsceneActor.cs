using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identifies a GameObject that can participate in cutscenes.
/// Registers itself by actorId so CutscenePlayer can resolve actors by name.
/// Provides WalkTo / TeleportTo helpers used by CutscenePlayer step execution.
/// </summary>
public class CutsceneActor : MonoBehaviour
{
    [SerializeField] private string actorId;
    [SerializeField] private float defaultMoveSpeed = 2.5f;

    public string ActorId => actorId;

    private static readonly Dictionary<string, CutsceneActor> _registry
        = new Dictionary<string, CutsceneActor>();

    private SpriteRenderer _sr;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(actorId))
            _registry[actorId] = this;
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(actorId) && _registry.TryGetValue(actorId, out var current) && current == this)
            _registry.Remove(actorId);
    }

    /// <summary>Resolves a registered actor by ID. Returns null if not found.</summary>
    public static CutsceneActor Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _registry.TryGetValue(id, out var actor);
        return actor;
    }

    /// <summary>Coroutine: smoothly walks the actor to a world position.</summary>
    public IEnumerator WalkTo(Vector2 target, float speedOverride = 0f)
    {
        float speed = speedOverride > 0f ? speedOverride : defaultMoveSpeed;

        // Freeze rigidbody physics during cutscene walk (NPC or airship)
        RigidbodyType2D originalType = RigidbodyType2D.Dynamic;
        if (_rb != null)
        {
            originalType = _rb.bodyType;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;

            if (_rb != null)
                _rb.MovePosition(Vector2.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime));
            else
                transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
                _sr.flipX = dir.x < 0;

            yield return null; // one frame per step; smooth enough for cutscene pacing
        }

        if (_rb != null)
            _rb.bodyType = originalType;
    }

    /// <summary>Instantly teleports the actor to a world position.</summary>
    public void TeleportTo(Vector2 target)
    {
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.MovePosition(target);
        }
        transform.position = new Vector3(target.x, target.y, transform.position.z);
    }
}
