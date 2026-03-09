using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes CutsceneDefinition sequences. Singleton managed by GameBootstrapper.
///
/// During playback:
///   - GameState is set to Cutscene (PlayerInput disabled via GameStateController)
///   - Each step executes in order; most wait for completion
///   - Dialogue steps temporarily enter Dialogue state then restore Cutscene
///   - Camera is controlled via CameraController.SetTarget
///
/// After the last step, state is restored to Playing and camera returns to player.
/// </summary>
public class CutscenePlayer : Singleton<CutscenePlayer>
{
    private CameraController _camera;
    private bool _isPlaying;
    private Coroutine _current;
    private string _activeCutsceneId;
    private readonly List<CutsceneActor> _followingActors = new List<CutsceneActor>();

    public bool IsPlaying => _isPlaying;

    public override void Initialize()
    {
        _camera = FindFirstObjectByType<CameraController>();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void Play(CutsceneDefinition def)
    {
        if (def == null)
        {
            Debug.LogWarning("[CutscenePlayer] Play called with null definition.");
            return;
        }
        if (!def.IsValid)
        {
            Debug.LogWarning($"[CutscenePlayer] '{def.name}' has no steps — nothing to play.");
            return;
        }
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ExecuteSequence(def));
    }

    public void Stop()
    {
        if (_current != null) { StopCoroutine(_current); _current = null; }
        EndCutscene(_activeCutsceneId);
    }

    // ── Sequence ────────────────────────────────────────────────────────────

    private IEnumerator ExecuteSequence(CutsceneDefinition def)
    {
        _isPlaying = true;
        _activeCutsceneId = def.CutsceneId;

        GameManager.Instance?.SetState(GameState.Cutscene);
        EventBus.Publish(new CutsceneStartedEvent { CutsceneId = def.CutsceneId });

        // One frame for GameStateController to process state change and disable input
        yield return null;

        foreach (var step in def.Steps)
        {
            if (step != null)
                yield return ExecuteStep(step);
        }

        EndCutscene(def.CutsceneId);
    }

    private void EndCutscene(string id)
    {
        _isPlaying = false;
        _current = null;

        // Stop any ongoing follow behaviours
        foreach (var actor in _followingActors)
            actor?.StopFollowing();
        _followingActors.Clear();

        // Return camera to player
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null && _camera != null)
            _camera.SetTarget(player.transform);

        GameManager.Instance?.SetState(GameState.Playing);
        EventBus.Publish(new CutsceneEndedEvent { CutsceneId = id ?? "" });
    }

    // ── Step Dispatch ───────────────────────────────────────────────────────

    private IEnumerator ExecuteStep(CutsceneStep step)
    {
        switch (step.type)
        {
            case CutsceneStepType.MoveActor:
                var mover = CutsceneActor.Get(step.actorId);
                if (mover != null)
                    yield return mover.WalkTo(step.worldPosition, step.moveSpeed);
                else
                    Debug.LogWarning($"[CutscenePlayer] MoveActor: actor '{step.actorId}' not found.");
                break;

            case CutsceneStepType.TeleportActor:
                var teleporter = CutsceneActor.Get(step.actorId);
                if (teleporter != null)
                    teleporter.TeleportTo(step.worldPosition);
                else
                    Debug.LogWarning($"[CutscenePlayer] TeleportActor: actor '{step.actorId}' not found.");
                break;

            case CutsceneStepType.FocusCamera:
                if (_camera != null)
                {
                    var focusActor = CutsceneActor.Get(step.targetActorId);
                    if (focusActor != null)
                        _camera.SetTarget(focusActor.transform);
                    else
                        Debug.LogWarning($"[CutscenePlayer] FocusCamera: actor '{step.targetActorId}' not found.");
                }
                break;

            case CutsceneStepType.ReturnCamera:
                var returnTarget = FindFirstObjectByType<PlayerController>();
                if (returnTarget != null && _camera != null)
                    _camera.SetTarget(returnTarget.transform);
                break;

            case CutsceneStepType.ShowDialogue:
                yield return ShowDialogueStep(step);
                break;

            case CutsceneStepType.Wait:
                yield return new WaitForSeconds(step.duration);
                break;

            case CutsceneStepType.BoardAirship:
                yield return BoardAirshipStep(step);
                break;

            case CutsceneStepType.DisembarkAirship:
                yield return DisembarkAirshipStep(step);
                break;

            case CutsceneStepType.SetActorActive:
                var setActor = CutsceneActor.Get(step.actorId);
                if (setActor != null)
                    setActor.gameObject.SetActive(step.boolValue);
                else
                    Debug.LogWarning($"[CutscenePlayer] SetActorActive: actor '{step.actorId}' not found.");
                break;

            case CutsceneStepType.TransferOwnership:
                var ownerActor = CutsceneActor.Get(step.actorId);
                var airshipCtrl = ownerActor?.GetComponent<AirshipController>();
                if (airshipCtrl != null)
                    airshipCtrl.OwnerId = step.ownerIdValue;
                else
                    Debug.LogWarning($"[CutscenePlayer] TransferOwnership: no AirshipController on '{step.actorId}'.");
                break;

            case CutsceneStepType.ReturnPlayerControl:
                var playerCtrlRPC = FindFirstObjectByType<PlayerController>();
                var playerActor = playerCtrlRPC != null ? CutsceneActor.Get("Player") : null;
                playerActor?.StopFollowing();
                var playerInput = playerCtrlRPC?.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = true;
                    playerInput.SwitchCurrentActionMap("Player");
                    GameManager.Instance?.SetState(GameState.Playing);
                }
                break;

            case CutsceneStepType.FollowActor:
                var follower = CutsceneActor.Get(step.actorId);
                var leader   = CutsceneActor.Get(step.targetActorId);
                if (follower != null && leader != null)
                {
                    follower.StartFollowing(leader, step.followOffset, step.moveSpeed);
                    _followingActors.Add(follower);
                }
                else
                    Debug.LogWarning($"[CutscenePlayer] FollowActor: could not find '{step.actorId}' or leader '{step.targetActorId}'.");
                break;

            case CutsceneStepType.StopFollowActor:
                var stopActor = CutsceneActor.Get(step.actorId);
                if (stopActor != null)
                {
                    stopActor.StopFollowing();
                    _followingActors.Remove(stopActor);
                }
                else
                    Debug.LogWarning($"[CutscenePlayer] StopFollowActor: actor '{step.actorId}' not found.");
                break;
        }
    }

    // ── Step Helpers ────────────────────────────────────────────────────────

    private IEnumerator ShowDialogueStep(CutsceneStep step)
    {
        if (step.dialogue == null) yield break;
        if (DialogueManager.Instance == null) yield break;

        DialogueManager.Instance.StartDialogue(step.dialogue, step.speaker);

        // Wait for dialogue to fully close (DialogueManager sets state back to Playing)
        yield return new WaitUntil(() => !DialogueManager.Instance.IsActive);

        // Restore Cutscene state since dialogue ended with Playing
        GameManager.Instance?.SetState(GameState.Cutscene);
    }

    /// <summary>
    /// Boards the 'actorId' actor onto the 'targetActorId' airship as a passenger.
    /// The actor is parented to the airship so it moves with it during subsequent MoveActor steps.
    /// </summary>
    private IEnumerator BoardAirshipStep(CutsceneStep step)
    {
        var passengerActor = CutsceneActor.Get(step.actorId);
        var airshipActor   = CutsceneActor.Get(step.targetActorId);

        if (passengerActor == null || airshipActor == null)
        {
            Debug.LogWarning($"[CutscenePlayer] BoardAirship: could not find actor '{step.actorId}' or airship '{step.targetActorId}'.");
            yield break;
        }

        var airshipCtrl = airshipActor.GetComponent<AirshipController>();
        var playerCtrl  = passengerActor.GetComponent<PlayerController>();
        var passengerRb = passengerActor.GetComponent<Rigidbody2D>();

        // Disable passenger physics
        if (passengerRb != null)
        {
            passengerRb.linearVelocity = Vector2.zero;
            passengerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Disable passenger colliders
        foreach (var col in passengerActor.GetComponents<Collider2D>())
            col.enabled = false;

        // Parent passenger to airship — they now move with it
        passengerActor.transform.SetParent(airshipActor.transform);

        // Move to helm position (world → local space handled by SetParent above)
        if (airshipCtrl != null)
        {
            var helm = airshipCtrl.GetHelmTransform();
            if (helm != null)
                passengerActor.transform.position = helm.position;
        }

        if (playerCtrl != null && airshipCtrl != null)
        {
            playerCtrl.CurrentAirship = airshipCtrl;
            playerCtrl.CurrentCloud = null;
        }

        yield break;
    }

    /// <summary>
    /// Disembarks the 'actorId' actor from its current airship parent at a world position.
    /// </summary>
    private IEnumerator DisembarkAirshipStep(CutsceneStep step)
    {
        var passengerActor = CutsceneActor.Get(step.actorId);
        if (passengerActor == null)
        {
            Debug.LogWarning($"[CutscenePlayer] DisembarkAirship: actor '{step.actorId}' not found.");
            yield break;
        }

        var passengerRb = passengerActor.GetComponent<Rigidbody2D>();
        var playerCtrl  = passengerActor.GetComponent<PlayerController>();

        // Unparent from airship
        passengerActor.transform.SetParent(null);
        passengerActor.transform.position = new Vector3(step.worldPosition.x, step.worldPosition.y, passengerActor.transform.position.z);

        // Re-enable colliders
        foreach (var col in passengerActor.GetComponents<Collider2D>())
            col.enabled = true;

        // Restore physics
        if (passengerRb != null)
            passengerRb.bodyType = RigidbodyType2D.Dynamic;

        if (playerCtrl != null)
        {
            playerCtrl.CurrentAirship = null;

            // Find and assign landing cloud if ID provided
            if (!string.IsNullOrEmpty(step.disembarkCloudId))
            {
                foreach (var island in FindObjectsByType<CloudIsland>(FindObjectsSortMode.None))
                {
                    if (island.CloudId == step.disembarkCloudId)
                    {
                        playerCtrl.CurrentCloud = island;
                        break;
                    }
                }
            }
        }

        yield break;
    }
}
