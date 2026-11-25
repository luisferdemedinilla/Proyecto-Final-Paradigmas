using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(Animator))]
public sealed class GoombaEnemy : MonoBehaviour, ILevelResettable
{
    const int ProbeHitCapacity = 16;
    const int FacingLeft = -1;
    const int FacingRight = 1;
    const int BaseAnimationLayer = 0;
    const float PatrolTurnMargin = 0.1f;
    const float FloorProbeForwardOffset = 0.1f;
    const float FloorProbeStartHeight = 0.25f;
    const float FloorProbeRadius = 0.06f;
    const float FloorProbeDistance = 0.6f;
    const float MinimumGroundNormalY = 0.6f;
    const float WallProbeExtraDistance = 0.12f;
    const float MinimumStompNormalY = 0.55f;
    const float MinimumStompDownwardSpeed = 0.1f;
    const float StompHeightTolerance = 0.22f;
    const float StompBounceSpeed = 6.5f;
    const float DefeatVisibleDuration = 0.5f;
    const float FacingYawDegrees = 90f;

    [SerializeField] MarioPlayerController target;
    [SerializeField] Transform visual;
    [SerializeField] float patrolLeft = 10;
    [SerializeField] float patrolRight = 17;
    [SerializeField, Min(0)] float speed = 1.8f;
    [SerializeField, Min(0)] float detectionDistance = 9;
    public bool IsDefeated { get; private set; }
    public bool IsChasing { get; private set; }
    public bool IsBlocked { get; private set; }

    public void Configure(MarioPlayerController player, float left, float right)
    {
        target = player;
        patrolLeft = left;
        patrolRight = right;
    }

    readonly RaycastHit[] probeHits = new RaycastHit[ProbeHitCapacity];
    Rigidbody body;
    BoxCollider hitbox;
    Animator animator;
    Vector3 initialPosition;
    bool stopped;
    int direction = FacingLeft;
    float hideAt;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<BoxCollider>();
        animator = GetComponent<Animator>();
        initialPosition = transform.position;
        if (visual == null)
        {
            visual = transform.Find("Visual");
        }

        animator.applyRootMotion = false;
    }

    void FixedUpdate()
    {
        if (stopped || IsDefeated)
        {
            return;
        }

        IsChasing = target != null && target.enabled &&
            Mathf.Abs(target.transform.position.x - body.position.x) < detectionDistance;
        if (IsChasing)
        {
            direction = target.transform.position.x < body.position.x ? FacingLeft : FacingRight;
        }
        else if (body.position.x < patrolLeft + PatrolTurnMargin)
        {
            direction = FacingRight;
        }
        else if (body.position.x > patrolRight - PatrolTurnMargin)
        {
            direction = FacingLeft;
        }

        // Patrol limits choose a direction; they are not invisible walls during pursuit.
        IsBlocked = !CanMove(direction);
        if (IsBlocked && !IsChasing)
        {
            direction = -direction;
            IsBlocked = !CanMove(direction);
        }

        float horizontalSpeed = IsBlocked ? 0 : direction * speed;
        body.linearVelocity = new Vector3(horizontalSpeed, body.linearVelocity.y, 0);
        visual.rotation = Quaternion.Euler(0, FacingYawDegrees * direction, 0);
        animator.SetBool("Chasing", IsChasing);
        animator.speed = IsBlocked ? 0 : 1;
    }

    bool CanMove(int travelDirection)
    {
        Bounds bounds = hitbox.bounds;
        // A finite footprint bridges tiny seams between adjacent floor blocks.
        Vector3 ahead = new Vector3(
            bounds.center.x + travelDirection * (bounds.extents.x + FloorProbeForwardOffset),
            bounds.min.y + FloorProbeStartHeight,
            bounds.center.z);
        int count = Physics.SphereCastNonAlloc(
            ahead, FloorProbeRadius, Vector3.down, probeHits, FloorProbeDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool supported = false;
        for (int i = 0; i < count; i++)
        {
            if (probeHits[i].collider.attachedRigidbody != body &&
                probeHits[i].collider.GetComponentInParent<MarioPlayerController>() == null &&
                probeHits[i].normal.y > MinimumGroundNormalY)
            {
                supported = true;
            }
        }

        if (!supported)
        {
            return false;
        }

        count = Physics.RaycastNonAlloc(
            bounds.center, Vector3.right * travelDirection, probeHits,
            bounds.extents.x + WallProbeExtraDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (probeHits[i].collider.attachedRigidbody != body &&
                probeHits[i].collider.GetComponentInParent<MarioPlayerController>() == null)
            {
                return false;
            }
        }

        return true;
    }

    void Update()
    {
        if (IsDefeated && Time.time >= hideAt)
        {
            visual.gameObject.SetActive(false);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsDefeated || stopped)
        {
            return;
        }

        MarioPlayerController player = collision.collider.GetComponentInParent<MarioPlayerController>();
        if (player == null || !player.enabled)
        {
            return;
        }

        CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
        float feet = capsule.bounds.min.y;
        bool top = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.y) > MinimumStompNormalY)
            {
                top = true;
            }
        }

        if (player.PrePhysicsVerticalSpeed < -MinimumStompDownwardSpeed && top &&
            feet >= hitbox.bounds.max.y - StompHeightTolerance)
        {
            Defeat();
            player.BounceOffEnemy(StompBounceSpeed);
        }
        else
        {
            player.TakeHit();
        }
    }

    public void Defeat()
    {
        if (IsDefeated)
        {
            return;
        }

        IsDefeated = true;
        hitbox.enabled = false;
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        animator.speed = 1;
        animator.Play("Defeated", BaseAnimationLayer, 0);
        hideAt = Time.time + DefeatVisibleDuration;
    }

    public void SetStopped(bool value)
    {
        stopped = value;
        animator.speed = value ? 0 : 1;
        if (value && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
        }

        body.isKinematic = value || IsDefeated;
    }

    public void ResetForLevel()
    {
        IsDefeated = false;
        stopped = false;
        IsBlocked = false;
        direction = FacingLeft;
        visual.gameObject.SetActive(true);
        hitbox.enabled = true;
        body.isKinematic = false;
        body.position = initialPosition;
        body.linearVelocity = Vector3.zero;
        animator.speed = 1;
        animator.Play("Walk", BaseAnimationLayer, 0);
    }
}
