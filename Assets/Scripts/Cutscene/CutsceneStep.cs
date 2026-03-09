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

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.MoveActor || type == CutsceneStepType.FollowActor")]
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
    [LabelText("Landing Cloud"), Tooltip("The CloudIslandDefinition the actor lands on. Leave empty to clear.")]
    public CloudIslandDefinition disembarkCloud;

    [BoxGroup("Parameters"), ShowIf("@type == CutsceneStepType.FollowActor")]
    [LabelText("Trail Distance"), Tooltip("Distance to maintain behind the leader per axis (always positive). Sign is auto-determined: x=1 stays 1 unit behind on X, y=1 stays 1 unit behind on Y.")]
    public Vector2 followOffset;

    // ── ShowIf predicates ──────────────────────────────────────────────────

    private bool NeedsActorId => type != CutsceneStepType.Wait
                               && type != CutsceneStepType.ReturnCamera
                               && type != CutsceneStepType.FocusCamera
                               && type != CutsceneStepType.ReturnPlayerControl;

    private bool NeedsTargetId => type == CutsceneStepType.FocusCamera
                                || type == CutsceneStepType.BoardAirship
                                || type == CutsceneStepType.FollowActor;

    private bool NeedsWorldPosition => type == CutsceneStepType.MoveActor
                                     || type == CutsceneStepType.TeleportActor
                                     || type == CutsceneStepType.DisembarkAirship;

    private bool NeedsDuration => type == CutsceneStepType.Wait;
}
