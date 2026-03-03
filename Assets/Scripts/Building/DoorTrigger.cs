using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("The interior to enter. Leave null for exit doors — InteriorManager already knows the current interior.")]
    [SerializeField] private BuildingInterior targetInterior;
    [SerializeField] private bool isExitDoor;
    [SerializeField] private AudioClip doorSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (InteriorManager.Instance == null) return;

        if (doorSound != null)
            AudioSource.PlayClipAtPoint(doorSound, transform.position);

        if (isExitDoor)
            InteriorManager.Instance.ExitInterior();
        else
            InteriorManager.Instance.EnterInterior(targetInterior);
    }
}
