using UnityEngine;

public class CropParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem plantPuff;
    [SerializeField] private ParticleSystem waterDrops;
    [SerializeField] private ParticleSystem harvestSparkle;

    private void Start()
    {
        EventBus.Subscribe<CropPlantedEvent>(OnCropPlanted);
        EventBus.Subscribe<CropWateredEvent>(OnCropWatered);
        EventBus.Subscribe<CropHarvestedEvent>(OnCropHarvested);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<CropPlantedEvent>(OnCropPlanted);
        EventBus.Unsubscribe<CropWateredEvent>(OnCropWatered);
        EventBus.Unsubscribe<CropHarvestedEvent>(OnCropHarvested);
    }

    private void OnCropPlanted(CropPlantedEvent evt)
    {
        PlayAt(plantPuff, evt.Position.TileToWorld());
    }

    private void OnCropWatered(CropWateredEvent evt)
    {
        PlayAt(waterDrops, evt.Position.TileToWorld());
    }

    private void OnCropHarvested(CropHarvestedEvent evt)
    {
        // Play at player position since we don't have crop position in this event
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            PlayAt(harvestSparkle, player.transform.position);
    }

    private void PlayAt(ParticleSystem ps, Vector3 position)
    {
        if (ps == null) return;
        ps.transform.position = position;
        ps.Play();
    }
}
