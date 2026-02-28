using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactableMask;

    private PlayerController playerController;
    private IInteractable currentTarget;
    private uint ownerId = 0;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        FindNearestInteractable();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (currentTarget != null && currentTarget.CanInteract(ownerId))
        {
            currentTarget.Interact(ownerId);
            EventBus.Publish(new InteractionEvent
            {
                Target = (currentTarget as MonoBehaviour)?.gameObject
            });
        }
    }

    private void FindNearestInteractable()
    {
        Vector2 origin = (Vector2)transform.position + playerController.GetFacingVector() * 0.5f;
        var hits = Physics2D.OverlapCircleAll(origin, interactionRadius, interactableMask);

        IInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(ownerId))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = interactable;
                }
            }
        }

        currentTarget = nearest;
    }
}
