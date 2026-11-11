using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody),typeof(BoxCollider),typeof(Animator))]
public sealed class GoombaEnemy:MonoBehaviour
{
    [SerializeField] MarioPlayerController target;
    [SerializeField] Transform visual;
    [SerializeField] float patrolLeft=10,patrolRight=17;
    [SerializeField,Min(0)] float speed=1.8f,detectionDistance=9;
    public bool IsDefeated {get;private set;}
    public bool IsChasing {get;private set;}
    public bool IsBlocked {get;private set;}
    readonly RaycastHit[] probeHits=new RaycastHit[16];
    Rigidbody body;BoxCollider hitbox;Animator animator;Vector3 initialPosition;bool stopped;int direction=-1;float hideAt;
    void Awake(){body=GetComponent<Rigidbody>();hitbox=GetComponent<BoxCollider>();animator=GetComponent<Animator>();initialPosition=transform.position;if(visual==null)visual=transform.Find("Visual");animator.applyRootMotion=false;}
    void FixedUpdate()
    {
        if(stopped||IsDefeated)return;
        IsChasing=target!=null && target.enabled && Mathf.Abs(target.transform.position.x-body.position.x)<detectionDistance;
        if(IsChasing)direction=target.transform.position.x<body.position.x?-1:1;
        else if(body.position.x<patrolLeft+.1f)direction=1;else if(body.position.x>patrolRight-.1f)direction=-1;
        // Patrol limits choose a direction; they are not invisible walls during pursuit.
        IsBlocked=!CanMove(direction);
        if(IsBlocked && !IsChasing){direction=-direction;IsBlocked=!CanMove(direction);}
        float dx=IsBlocked?0:direction*speed;
        body.linearVelocity=new Vector3(dx,body.linearVelocity.y,0);
        visual.rotation=Quaternion.Euler(0,90*direction,0);animator.SetBool("Chasing",IsChasing);animator.speed=IsBlocked?0:1;
    }
    bool CanMove(int travelDirection)
    {
        var bounds=hitbox.bounds;
        // A finite footprint bridges tiny seams between adjacent floor blocks.
        var ahead=new Vector3(bounds.center.x+travelDirection*(bounds.extents.x+.1f),bounds.min.y+.25f,bounds.center.z);
        int count=Physics.SphereCastNonAlloc(ahead,.06f,Vector3.down,probeHits,.6f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        bool supported=false;
        for(int i=0;i<count;i++)if(probeHits[i].collider.attachedRigidbody!=body && probeHits[i].collider.GetComponentInParent<MarioPlayerController>()==null && probeHits[i].normal.y>.6f)supported=true;
        if(!supported)return false;
        count=Physics.RaycastNonAlloc(bounds.center,Vector3.right*travelDirection,probeHits,bounds.extents.x+.12f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)if(probeHits[i].collider.attachedRigidbody!=body && probeHits[i].collider.GetComponentInParent<MarioPlayerController>()==null)return false;
        return true;
    }
    void Update(){if(IsDefeated && Time.time>=hideAt)visual.gameObject.SetActive(false);}
    void OnCollisionEnter(Collision collision)
    {
        if(IsDefeated||stopped)return;
        var player=collision.collider.GetComponentInParent<MarioPlayerController>();if(player==null||!player.enabled)return;
        var capsule=player.GetComponent<CapsuleCollider>();float feet=capsule.bounds.min.y;
        bool top=false;for(int i=0;i<collision.contactCount;i++)if(Mathf.Abs(collision.GetContact(i).normal.y)>.55f)top=true;
        if(player.PrePhysicsVerticalSpeed<-.1f && top && feet>=hitbox.bounds.max.y-.22f)
        {Defeat();player.BounceOffEnemy(6.5f);}
        else player.TakeHit();
    }
    public void Defeat()
    {
        if(IsDefeated)return;IsDefeated=true;hitbox.enabled=false;body.linearVelocity=Vector3.zero;body.isKinematic=true;
        animator.speed=1;animator.Play("Defeated",0,0);hideAt=Time.time+.5f;
    }
    public void SetStopped(bool value){stopped=value;animator.speed=value?0:1;if(value&&!body.isKinematic)body.linearVelocity=Vector3.zero;body.isKinematic=value||IsDefeated;}
    public void RestartEnemy(){IsDefeated=false;stopped=false;IsBlocked=false;direction=-1;visual.gameObject.SetActive(true);hitbox.enabled=true;body.isKinematic=false;body.position=initialPosition;body.linearVelocity=Vector3.zero;animator.speed=1;animator.Play("Walk",0,0);}
}
