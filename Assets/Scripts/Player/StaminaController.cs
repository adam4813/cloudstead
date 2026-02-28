using UnityEngine;

public class StaminaController : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float lowStaminaThreshold = 20f;

    private float currentStamina;
    private PlayerController playerController;
    private float yawnTimer;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsLowStamina => currentStamina <= lowStaminaThreshold;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        currentStamina = maxStamina;
    }

    private void Start()
    {
        EventBus.Subscribe<DayStartedEvent>(OnDayStarted);
        PublishStaminaChanged();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<DayStartedEvent>(OnDayStarted);
    }

    private void Update()
    {
        if (!IsLowStamina) return;

        // Cozy low-stamina: slow movement, periodic yawn
        if (playerController != null)
            playerController.MoveSpeed = playerController.BaseSpeed * 0.6f;

        yawnTimer -= Time.deltaTime;
        if (yawnTimer <= 0f)
        {
            yawnTimer = 30f;
            Debug.Log("[Stamina] *yawn* — Time to rest...");
        }
    }

    /// <summary>
    /// Use stamina. Always succeeds (cozy design — never blocks the player).
    /// Returns true if player had enough, false if they're running on fumes.
    /// </summary>
    public bool UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        PublishStaminaChanged();

        if (!IsLowStamina && playerController != null)
            playerController.MoveSpeed = playerController.BaseSpeed;

        return currentStamina > 0f;
    }

    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        PublishStaminaChanged();

        if (!IsLowStamina && playerController != null)
            playerController.MoveSpeed = playerController.BaseSpeed;
    }

    public void FullRestore()
    {
        currentStamina = maxStamina;
        PublishStaminaChanged();

        if (playerController != null)
            playerController.MoveSpeed = playerController.BaseSpeed;
    }

    private void OnDayStarted(DayStartedEvent evt)
    {
        FullRestore();
    }

    private void PublishStaminaChanged()
    {
        EventBus.Publish(new StaminaChangedEvent
        {
            Current = currentStamina,
            Max = maxStamina
        });
    }
}
