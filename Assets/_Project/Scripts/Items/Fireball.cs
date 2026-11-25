using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class Fireball : MonoBehaviour
{
    const float MinimumProjectileSetting = 0.1f;
    const float InitialVerticalSpeed = -1f;
    const float MinimumBounceNormalY = 0.6f;

    [SerializeField, Min(MinimumProjectileSetting)] float speed = 9f;
    [SerializeField, Min(MinimumProjectileSetting)] float bounceSpeed = 4f;
    [SerializeField, Min(MinimumProjectileSetting)] float lifetime = 3f;
    Rigidbody body;
    SphereCollider hitbox;
    int direction;
    bool spent;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<SphereCollider>();
    }

    public void Launch(int facing, Collider owner)
    {
        direction = facing;
        Physics.IgnoreCollision(hitbox, owner);
        body.linearVelocity = new Vector3(direction * speed, InitialVerticalSpeed, 0f);
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (spent)
        {
            return;
        }

        GoombaEnemy goomba = collision.collider.GetComponentInParent<GoombaEnemy>();
        if (goomba != null)
        {
            goomba.Defeat();
            Despawn();
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > MinimumBounceNormalY)
            {
                body.linearVelocity = new Vector3(direction * speed, bounceSpeed, 0f);
                return;
            }
        }

        Despawn();
    }

    public void Despawn()
    {
        spent = true;
        hitbox.enabled = false;
        Destroy(gameObject);
    }
}
