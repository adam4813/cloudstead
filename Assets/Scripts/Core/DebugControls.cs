using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControls : MonoBehaviour
{
    private void Update()
    {
        // F5 = Advance day
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.Sleep();
                Debug.Log("[Debug] Day advanced via F5");
            }
            else
            {
                // Fallback if TimeManager doesn't exist yet
                EventBus.Publish(new DayStartedEvent
                {
                    Day = 1,
                    Season = Season.Spring
                });
                Debug.Log("[Debug] DayStartedEvent published (no TimeManager)");
            }
        }

        // F6 = Add 50 stamina
        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            var stamina = FindFirstObjectByType<StaminaController>();
            if (stamina != null)
            {
                stamina.RestoreStamina(50f);
                Debug.Log("[Debug] Restored 50 stamina");
            }
        }
    }
}
