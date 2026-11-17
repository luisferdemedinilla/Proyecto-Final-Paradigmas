using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MarioDamageForm : MonoBehaviour
{
    [Serializable]
    struct BoneBinding
    {
        public Transform source;
        public Transform destination;
        public Quaternion rotationOffset;
    }

    [SerializeField] Transform smallModel;
    [SerializeField] Renderer[] largeRenderers;
    [SerializeField] BoneBinding[] bones;
    [SerializeField] Transform largePelvis;
    [SerializeField] Transform smallPelvis;
    [SerializeField] float largePelvisRestY;
    [SerializeField] float smallPelvisUnitsPerLargeUnit;
    [SerializeField] float smallHeight;
    [SerializeField] float smallRadius;
    [SerializeField, Min(0)] float hitInvulnerability = 1f;

    Animator animator;
    Vector3 smallPelvisRestPosition;
    float invulnerableUntil;

    public bool IsSmall { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public float SmallHeight => smallHeight;
    public float SmallRadius => smallRadius;

    void Awake()
    {
        animator = GetComponent<Animator>();
        smallPelvisRestPosition = smallPelvis.localPosition;
    }

    public bool AbsorbHit()
    {
        if (IsInvulnerable)
            return true;
        if (IsSmall)
            return false;

        SetSmall(true);
        ProtectAfterHit();
        animator.Play("ShrinkHit", 0, 0f);
        return true;
    }

    public void ProtectAfterHit()
    {
        invulnerableUntil = Time.time + hitInvulnerability;
    }

    public void RestoreLargeForm()
    {
        SetSmall(false);
        invulnerableUntil = 0f;
    }

    void SetSmall(bool value)
    {
        IsSmall = value;
        // Keep the large rig evaluating so both forms use the same Animator.
        foreach (Renderer renderer in largeRenderers)
            renderer.forceRenderingOff = value;
        smallModel.gameObject.SetActive(value);
    }

    void LateUpdate()
    {
        if (!IsSmall)
            return;

        smallPelvis.localPosition = smallPelvisRestPosition + Vector3.up *
            ((largePelvis.localPosition.y - largePelvisRestY) * smallPelvisUnitsPerLargeUnit);
        foreach (BoneBinding bone in bones)
            bone.destination.rotation = bone.source.rotation * bone.rotationOffset;
    }
}
