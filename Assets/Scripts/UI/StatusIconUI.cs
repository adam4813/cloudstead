using UnityEngine;
using System.Collections;

public class StatusIconUI : MonoBehaviour
{
    [SerializeField] private GameObject tiredIcon;
    [SerializeField] private GameObject restedIcon;

    private Coroutine restedCoroutine;

    private void Start()
    {
        if (tiredIcon != null) tiredIcon.SetActive(false);
        if (restedIcon != null) restedIcon.SetActive(false);

        EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Subscribe<PlayerSleptEvent>(OnPlayerSlept);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
        EventBus.Unsubscribe<PlayerSleptEvent>(OnPlayerSlept);
    }

    private void OnStaminaChanged(StaminaChangedEvent evt)
    {
        bool isTired = evt.Current / evt.Max < 0.2f;
        if (tiredIcon != null) tiredIcon.SetActive(isTired);
    }

    private void OnPlayerSlept(PlayerSleptEvent evt)
    {
        if (tiredIcon != null) tiredIcon.SetActive(false);

        if (restedIcon != null)
        {
            if (restedCoroutine != null) StopCoroutine(restedCoroutine);
            restedCoroutine = StartCoroutine(ShowRestedBriefly());
        }
    }

    private IEnumerator ShowRestedBriefly()
    {
        restedIcon.SetActive(true);
        yield return new WaitForSeconds(2f);
        restedIcon.SetActive(false);
        restedCoroutine = null;
    }
}
