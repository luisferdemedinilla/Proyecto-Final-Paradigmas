using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(MarioPlayerController))]
public sealed class MarioFirePower : MonoBehaviour
{
    const float ThrowAnimationFadeStart = 0.72f;
    const float ThrowAnimationFadeEnd = 1f;
    const float FullThrowLayerWeight = 1f;

    [SerializeField] Key fireKey = Key.E;
    [SerializeField, Min(0)] float fireCooldown = .2f;
    [SerializeField] Fireball fireballPrefab;
    [SerializeField] Renderer[] fireRenderers;
    [SerializeField] Texture fireTexture;
    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int FireThrow = Animator.StringToHash("Fire Throw.FireThrow");
    readonly List<FireFlower> collectedFlowers = new List<FireFlower>();
    readonly List<Fireball> fireballs = new List<Fireball>();
    MarioPlayerController player;
    CapsuleCollider hitbox;
    Animator animator;
    int throwLayer;
    MaterialPropertyBlock fireAppearance;
    float nextShotTime;
    public bool IsFire { get; private set; }

    void Awake()
    {
        player = GetComponent<MarioPlayerController>();
        hitbox = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        throwLayer = animator.GetLayerIndex("Fire Throw");
        fireAppearance = new MaterialPropertyBlock();
        fireAppearance.SetTexture(BaseMap, fireTexture);
    }

    public void EnableFireForm(FireFlower flower)
    {
        SetFireForm(true);
        if (!collectedFlowers.Contains(flower))
        {
            collectedFlowers.Add(flower);
        }
    }

    void Update()
    {
        if (!player.isActiveAndEnabled)
        {
            return;
        }

        if (throwLayer >= 0 && animator.GetLayerWeight(throwLayer) > 0f)
        {
            float progress = animator.GetCurrentAnimatorStateInfo(throwLayer).normalizedTime;
            float fadeProgress = Mathf.InverseLerp(
                ThrowAnimationFadeStart, ThrowAnimationFadeEnd, progress);
            animator.SetLayerWeight(throwLayer, FullThrowLayerWeight - fadeProgress);
        }

        Keyboard keyboard = Keyboard.current;
        if (!IsFire || Time.time < nextShotTime || keyboard == null ||
            fireKey == Key.None || !keyboard[fireKey].wasPressedThisFrame)
        {
            return;
        }

        Fireball ball = Instantiate(fireballPrefab, hitbox.bounds.center, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(ball.gameObject, gameObject.scene);
        ball.Launch(player.Facing, hitbox);
        fireballs.RemoveAll(fireball => fireball == null);
        fireballs.Add(ball);
        nextShotTime = Time.time + fireCooldown;
        if (throwLayer >= 0)
        {
            animator.SetLayerWeight(throwLayer, FullThrowLayerWeight);
            animator.Play(FireThrow, throwLayer, 0f);
        }
    }

    public void LoseFireForm()
    {
        SetFireForm(false);
    }

    public void ResetPower()
    {
        if (IsFire)
        {
            SetFireForm(false);
        }

        foreach (FireFlower flower in collectedFlowers)
        {
            if (flower != null && flower.Collected)
            {
                flower.ResetForLevel();
            }
        }

        collectedFlowers.Clear();
        foreach (Fireball fireball in fireballs)
        {
            if (fireball != null)
            {
                fireball.Despawn();
            }
        }

        fireballs.Clear();
        nextShotTime = 0f;
    }

    void SetFireForm(bool value)
    {
        IsFire = value;
        if (!value && throwLayer >= 0 && animator.runtimeAnimatorController != null)
        {
            animator.SetLayerWeight(throwLayer, 0f);
        }

        foreach (Renderer renderer in fireRenderers)
        {
            renderer.SetPropertyBlock(value ? fireAppearance : null);
        }
    }
}
