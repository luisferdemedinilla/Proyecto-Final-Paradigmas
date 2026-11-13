using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class BreakableBrick : MonoBehaviour
{
    [SerializeField] GameObject intact;
    [SerializeField] GameObject fragments;
    [SerializeField] Animator destructionAnimator;
    [SerializeField,Min(.05f)] float destructionDuration=1.466667f;
    public bool IsBroken {get;private set;}
    BoxCollider hitbox;
    float hideAt;
    void Awake(){hitbox=GetComponent<BoxCollider>();ResetBlock();}
    void OnCollisionEnter(Collision collision){CheckHit(collision);}
    void OnCollisionStay(Collision collision){CheckHit(collision);}
    void CheckHit(Collision collision)
    {
        if(IsBroken)return;
        var player=collision.collider.GetComponentInParent<MarioPlayerController>();
        if(player==null || !player.enabled)return;
        var playerCollider=player.GetComponent<CapsuleCollider>();
        var blockBounds=hitbox.bounds;var playerBounds=playerCollider.bounds;
        bool verticalContact=false;
        for(int i=0;i<collision.contactCount;i++)if(Mathf.Abs(collision.GetContact(i).normal.y)>.6f)verticalContact=true;
        if(!verticalContact)return;
        bool hitFromBelow=player.PrePhysicsVerticalSpeed>.1f && playerBounds.center.y<blockBounds.center.y && playerBounds.max.y<=blockBounds.min.y+.2f;
        // The ground probe can enter recovery just before collision callbacks.
        // Require the recorded downward impact as well as the attack phase.
        bool poundFromAbove=(player.GroundPoundPhase==2 || player.GroundPoundPhase==3) && player.PrePhysicsVerticalSpeed<-.1f && playerBounds.center.y>blockBounds.center.y && playerBounds.min.y>=blockBounds.max.y-.2f;
        if(hitFromBelow || poundFromAbove)Break();
    }
    void Break()
    {
        IsBroken=true;hitbox.enabled=false;intact.SetActive(false);fragments.SetActive(true);
        destructionAnimator.speed=1;destructionAnimator.Play("Break",0,0);hideAt=Time.time+destructionDuration;
    }
    void Update(){if(IsBroken && fragments.activeSelf && Time.time>=hideAt)fragments.SetActive(false);}
    public void ResetBlock()
    {
        if(hitbox==null)hitbox=GetComponent<BoxCollider>();
        IsBroken=false;hitbox.enabled=true;intact.SetActive(true);fragments.SetActive(false);
    }
}
