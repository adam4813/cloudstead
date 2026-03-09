using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identifies a GameObject that can participate in cutscenes.
/// Registers itself by actorId so CutscenePlayer can resolve actors by name.
/// Provides WalkTo / TeleportTo / StartFollowing helpers used by CutscenePlayer step execution.
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

    // ── Follow state ───────────────────────────────────────────────────────
    private CutsceneActor _followLeader;
    private Vector2 _followOffset;   // magnitude per axis; sign determined by leader's movement
    private float _followSpeed;
    private Vector2 _lastLeaderPos;
    private float _trailSignX = 0f;
    private float _trailSignY = 0f;

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
        _followLeader = null;
    }

    private void Update()
    {
        if (_followLeader == null) return;

        Vector2 leaderPos = _followLeader.transform.position;

        // Track leader movement to determine which direction is "behind"
        Vector2 delta = leaderPos - _lastLeaderPos;
        if (Mathf.Abs(delta.x) > 0.001f) _trailSignX = -Mathf.Sign(delta.x);
        if (Mathf.Abs(delta.y) > 0.001f) _trailSignY = -Mathf.Sign(delta.y);
        _lastLeaderPos = leaderPos;

        // Target = leader + (distance behind on each axis)
        Vector2 target = leaderPos + new Vector2(_trailSignX * _followOffset.x,
                                                 _trailSignY * _followOffset.y);
        float speed = _followSpeed > 0f ? _followSpeed : defaultMoveSpeed;

        Vector2 dir = target - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0025f) return; // within ~0.05 units, stop

        Vector2 next = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (_rb != null)
            _rb.MovePosition(next);
        else
            transform.position = new Vector3(next.x, next.y, transform.position.z);

        if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
            _sr.flipX = dir.x < 0;
    }

    /// <summary>Resolves a registered actor by ID. Returns null if not found.</summary>
    public static CutsceneActor Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _registry.TryGetValue(id, out var actor);
        return actor;
    }

    /// <summary>
    /// Non-blocking: actor continuously moves toward leader.position + offset each Update.
    /// Call StopFollowing() to cancel.
    /// </summary>
    public void StartFollowing(CutsceneActor leader, Vector2 offset, float speed = 0f)
    {
        _followLeader = leader;
        _followOffset = offset;
        _followSpeed  = speed;
        _lastLeaderPos = leader.transform.position; // seed to avoid first-frame direction spike

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>Cancels any active follow behaviour.</summary>
    public void StopFollowing()
    {
        if (_followLeader == null) return;
        _followLeader = null;
        if (_rb != null)
            _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>Coroutine: smoothly walks the actor to a world position.</summary>
    public IEnumerator WalkTo(Vector2 target, float speedOverride = 0f)
    {
        float speed = speedOverride > 0f ? speedOverride : defaultMoveSpeed;

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

            yield return null;
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