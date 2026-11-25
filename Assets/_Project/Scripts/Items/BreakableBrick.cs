using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BreakableBrick : MonoBehaviour, ILevelResettable
{
    const float MinimumDestructionDuration = 0.05f;
    const int BaseAnimationLayer = 0;

    [SerializeField] GameObject intact;
    [SerializeField] GameObject fragments;
    [SerializeField] Animator destructionAnimator;
    [SerializeField, Min(MinimumDestructionDuration)] float destructionDuration = 1.466667f;
    public bool IsBroken { get; private set; }

    BoxCollider hitbox;
    float hideAt;

    void Awake()
    {
        hitbox = GetComponent<BoxCollider>();
        ResetForLevel();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsBroken)
        {
            if (BlockHitDetector.CheckHit(collision, hitbox))
            {
                Break();
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (!IsBroken)
        {
            if (BlockHitDetector.CheckHit(collision, hitbox))
            {
                Break();
            }
        }
    }

    void Break()
    {
        IsBroken = true;
        hitbox.enabled = false;
        intact.SetActive(false);
        fragments.SetActive(true);
        destructionAnimator.speed = 1;
        destructionAnimator.Play("Break", BaseAnimationLayer, 0);
        hideAt = Time.time + destructionDuration;
    }

    void Update()
    {
        if (IsBroken && fragments.activeSelf && Time.time >= hideAt)
        {
            fragments.SetActive(false);
        }
    }

    public void ResetForLevel()
    {
        if (hitbox == null)
        {
            hitbox = GetComponent<BoxCollider>();
        }

        IsBroken = false;
        hitbox.enabled = true;
        intact.SetActive(true);
        fragments.SetActive(false);
    }
}
