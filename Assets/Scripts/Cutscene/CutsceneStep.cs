using System;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// One step in a CutsceneDefinition sequence.
/// Odin's [ShowIf] hides irrelevant fields per step type.
/// </summary>
[Serializable]
public class CutsceneStep
{
    [HideLabel, EnumToggleButtons]
    public CutsceneStepType type = CutsceneStepType.Wait;

    // ── Actor ──────────────────────────────────────────────────────────────

    [BoxGroup("Parameters"), ShowIf("@NeedsActorId")]
    [LabelText("Actor ID"), Tooltip("Must match CutsceneActor.actorId on the target GameObject.")]
    public string actorId;

    [BoxGroup("Parameters"), ShowIf("@NeedsTargetId")]
    [LabelText("Target Actor ID"), Tooltip("Airship actorId for BoardAirship; camera-focus actorId for FocusCamera.")]
    public string targetActorId;

    // ── Position ───────────────────────────────────────────────────────────

    [BoxGroup("Parameters"), ShowIf("@NeedsWorldPosition")]
    [LabelText("World Position")]
    public Vector2 worldPosition;

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.MoveActor")]
    [LabelText("Move Speed (0 = actor default)"), MinValue(0)]
    public float moveSpeed;

    // ── Dialogue ───────────────────────────────────────────────────────────

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.ShowDialogue")]
    [LabelText("Dialogue Tree")]
    public DialogueTree dialogue;

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.ShowDialogue")]
    [LabelText("Speaker Definition")]
    public NPCDefinition speaker;

    // ── Timing ─────────────────────────────────────────────────────────────

    [BoxGroup("Parameters"), ShowIf("@NeedsDuration")]
    [LabelText("Duration (seconds)"), MinValue(0)]
    public float duration = 1f;

    // ── Flags ──────────────────────────────────────────────────────────────

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.SetActorActive")]
    [LabelText("Active")]
    public bool boolValue = true;

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.TransferOwnership")]
    [LabelText("New Owner ID")]
    public uint ownerIdValue;

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.DisembarkAirship")]
    [LabelText("Landing Cloud ID"), Tooltip("CloudIsland.cloudId the player lands on. Leave empty to clear.")]
    public string disembarkCloudId;

    // ── ShowIf predicates ──────────────────────────────────────────────────

    private bool NeedsActorId => type != CutsceneStepType.Wait
                               && type != CutsceneStepType.ReturnCamera
                               && type != CutsceneStepType.FocusCamera;

    private bool NeedsTargetId => type == CutsceneStepType.FocusCamera
                                || type == CutsceneStepType.BoardAirship;

    private bool NeedsWorldPosition => type == CutsceneStepType.MoveActor
                                     || type == CutsceneStepType.TeleportActor
                                     || type == CutsceneStepType.DisembarkAirship;

    private bool NeedsDuration => type == CutsceneStepType.Wait;
}
