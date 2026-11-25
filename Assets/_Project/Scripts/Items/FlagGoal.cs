using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class FlagGoal : MonoBehaviour, ILevelResettable
{
    const int HoldingPhase = 0;
    const int SlidingPhase = 1;
    const int CelebratingPhase = 2;
    const int BaseAnimationLayer = 0;
    const float MinimumSlideDuration = 0.1f;
    const float HoldDuration = 0.25f;
    const float FallbackHandOffset = 0.45f;
    const float CelebrationHorizontalOffset = 0.9f;
    const float PoleFacingYawDegrees = 90f;
    const float CelebrationYawDegrees = 180f;

    [SerializeField] Transform flagCarriage;
    [SerializeField] Animator poleAnimator;
    [Serializable]
    public class CharacterClips
    {
        public RuntimeAnimatorController controller;
        public AnimationClip hold;
        public AnimationClip slide;
        public AnimationClip celebrate;
    }

    [SerializeField] CharacterClips[] characters;
    AnimationClip holdClip;
    AnimationClip slideClip;
    AnimationClip celebrateClip;
    [SerializeField, Min(MinimumSlideDuration)] float slideDuration = 1.8f;
    [SerializeField] float loweredFlagHeight = .9f;
    [SerializeField] float landingHeight = .9f;
    public event Action<MarioPlayerController> Grabbed;
    public event Action Completed;
    public bool IsFinishing { get; private set; }
    public bool HasCompleted { get; private set; }
    public float FlagHeight => flagCarriage.localPosition.y;

    RuntimeAnimatorController previousController;
    Vector3 flagStart;
    Vector3 grabPosition;
    MarioPlayerController player;
    Rigidbody body;
    Animator animator;
    PlayableGraph graph;
    AnimationClipPlayable motion;
    float began;
    int phase = HoldingPhase;

    void Awake()
    {
        flagStart = flagCarriage.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        MarioPlayerController p = other.GetComponentInParent<MarioPlayerController>();
        if (p != null && p.enabled && !IsFinishing && !HasCompleted)
        {
            Begin(p);
        }
    }

    void Begin(MarioPlayerController p)
    {
        player = p;
        body = p.GetComponent<Rigidbody>();
        animator = p.GetComponent<Animator>();
        CharacterClips clips = Array.Find(characters, c => c.controller == animator.runtimeAnimatorController);
        if (clips == null)
        {
            return;
        }

        holdClip = clips.hold;
        slideClip = clips.slide;
        celebrateClip = clips.celebrate;
        IsFinishing = true;
        phase = HoldingPhase;
        began = Time.time;
        player.enabled = false;
        previousController = animator.runtimeAnimatorController;
        animator.runtimeAnimatorController = null;
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        animator.speed = 1;
        animator.applyRootMotion = false;
        Transform visual = player.transform.Find("Visual");
        visual.rotation = Quaternion.Euler(0, PoleFacingYawDegrees, 0);
        Play(holdClip);
        graph.Evaluate(0);
        grabPosition = body.position;
        grabPosition.y = Mathf.Max(transform.position.y + landingHeight, grabPosition.y);
        grabPosition.z = transform.position.z;
        Transform hand = visual.Find("Model/AllRoot/JointRoot/Spine1/Spine2/ShoulderL/ArmL1/ArmL2/HandL");
        float handPositionX = hand == null
            ? player.transform.position.x + FallbackHandOffset
            : hand.position.x;
        grabPosition.x += transform.position.x - handPositionX;
        MovePlayer(grabPosition);
        Grabbed?.Invoke(player);
    }

    void Play(AnimationClip clip)
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }

        graph = PlayableGraph.Create("Flag goal character");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        motion = AnimationClipPlayable.Create(graph, clip);
        motion.SetApplyFootIK(false);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Character", animator);
        output.SetSourcePlayable(motion);
        graph.Play();
    }

    void MovePlayer(Vector3 position)
    {
        body.position = position;
        player.transform.position = position;
    }

    void Update()
    {
        if (!IsFinishing)
        {
            return;
        }

        float elapsed = Time.time - began;
        if (phase == HoldingPhase && elapsed >= HoldDuration)
        {
            phase = SlidingPhase;
            began = Time.time;
            elapsed = 0;
            Play(slideClip);
            poleAnimator.Play("Finish", BaseAnimationLayer, 0);
        }

        if (phase == SlidingPhase)
        {
            float slideProgress = Mathf.Clamp01(elapsed / slideDuration);
            Vector3 loweredFlagPosition = new Vector3(flagStart.x, loweredFlagHeight, flagStart.z);
            flagCarriage.localPosition = Vector3.Lerp(flagStart, loweredFlagPosition, slideProgress);
            float playerHeight = Mathf.Lerp(grabPosition.y, transform.position.y + landingHeight, slideProgress);
            MovePlayer(new Vector3(grabPosition.x, playerHeight, grabPosition.z));
            if (slideProgress >= 1)
            {
                phase = CelebratingPhase;
                began = Time.time;
                Play(celebrateClip);
                MovePlayer(transform.position + Vector3.right * CelebrationHorizontalOffset);
                player.transform.Find("Visual").rotation = Quaternion.Euler(0, CelebrationYawDegrees, 0);
                HasCompleted = true;
                Completed?.Invoke();
            }
        }

        if (graph.IsValid())
        {
            graph.Evaluate(Time.deltaTime);
            if (phase == CelebratingPhase && Time.time - began >= celebrateClip.length)
            {
                motion.SetTime(celebrateClip.length);
                graph.Evaluate(0);
                IsFinishing = false;
            }
        }
    }

    public void ResetForLevel()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }

        HasCompleted = false;
        IsFinishing = false;
        phase = HoldingPhase;
        flagCarriage.localPosition = flagStart;
        poleAnimator.Rebind();
        poleAnimator.Update(0);
        if (animator != null)
        {
            animator.runtimeAnimatorController = previousController;
            animator.Rebind();
            animator.Update(0);
        }

        previousController = null;
        player = null;
        body = null;
        animator = null;
    }

    void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
