using System;
using UnityEngine;

public sealed class FireFlower : MonoBehaviour, ILevelResettable, ICollectibleItem
{
    [SerializeField] Transform visual;

    public bool Collected { get; private set; }
    public event Action<MarioPlayerController> CollectedBy;
    Collider hitbox;
    void Awake()
    {
        hitbox = GetComponent<Collider>();
        if (visual == null)
            visual = transform.Find("Visual");
    }
    void OnTriggerEnter(Collider other) { MarioPlayerController p = other.GetComponentInParent<MarioPlayerController>(); if (p != null && p.enabled) Collect(p); }
    public void Collect(MarioPlayerController player)
    {
        if (Collected || player == null || !player.isActiveAndEnabled ||
            !player.TryGainFirePower(this))
            return;

        Collected = true;
        hitbox.enabled = false;
        if (visual != null)
            visual.gameObject.SetActive(false);
        CollectedBy?.Invoke(player);
    }
    public void ResetForLevel()
    {
        Collected = false;
        hitbox.enabled = true;
        if (visual != null)
            visual.gameObject.SetActive(true);
    }
}
