using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider),typeof(Animator))]
public sealed class MarioPlayerController : MonoBehaviour
{
    [Header("Controles (teclado)")]
    [SerializeField] Key leftKey=Key.A;
    [SerializeField] Key rightKey=Key.D;
    [SerializeField] Key jumpKey=Key.W;
    [SerializeField] Key alternateJumpKey=Key.Space;
    [SerializeField] Key crouchKey=Key.S;
    [SerializeField] Key runKey=Key.LeftShift;
    [SerializeField] Key respawnKey=Key.R;
    [Header("Movimiento")]
    [SerializeField,Min(0.1f)] float walkSpeed=3.5f;
    [SerializeField,Min(0.1f)] float runSpeed=6f;
    [SerializeField,Min(0)] float acceleration=32f;
    [SerializeField,Min(0)] float braking=40f;
    [SerializeField,Min(0)] float airAcceleration=18f;
    [SerializeField,Min(0.1f)] float jumpSpeed=8.5f;
    [SerializeField,Range(0,0.3f)] float coyoteTime=0.1f;
    [SerializeField,Range(0,0.3f)] float jumpBuffer=0.12f;
    [SerializeField] LayerMask groundMask=~0;
    [Header("Agacharse")]
    [SerializeField,Min(0.1f)] float crouchSpeed=1.4f;
    [SerializeField,Min(0.64f)] float crouchHeight=1.28f;
    [SerializeField,Min(0)] float crouchVisualDrop=0.30f;
    [Header("Referencias y reinicio")]
    [SerializeField] Transform visual;
    [SerializeField] Transform spawnPoint;
    [SerializeField] bool respawnWhenFalling=true;
    [SerializeField] float respawnBelowY=-10f;

    Rigidbody body;
    CapsuleCollider capsule;
    Animator animator;
    readonly RaycastHit[] groundHits=new RaycastHit[16];
    readonly Collider[] ceilingHits=new Collider[16];
    Vector3 standingCenter,visualRestPosition,startPosition;
    float standingHeight,planeZ,lastGrounded=-100,lastJump=-100,jumpUntil=-100;
    float horizontal;
    bool wantsRun,wantsCrouch,respawnRequested;
    int facing=1;
    public bool IsGrounded {get;private set;}
    public bool IsCrouching {get;private set;}
    public float HorizontalSpeed => body==null?0:body.linearVelocity.x;

    static readonly int SpeedId=Animator.StringToHash("Speed");
    static readonly int GroundedId=Animator.StringToHash("Grounded");
    static readonly int VerticalId=Animator.StringToHash("VerticalSpeed");
    static readonly int CrouchId=Animator.StringToHash("Crouching");

    void Awake()
    {
        body=GetComponent<Rigidbody>();capsule=GetComponent<CapsuleCollider>();animator=GetComponent<Animator>();
        if(visual==null) visual=transform.Find("Visual");
        standingHeight=capsule.height;standingCenter=capsule.center;
        if(visual!=null) visualRestPosition=visual.localPosition;
        startPosition=body.position;planeZ=body.position.z;
        body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        animator.applyRootMotion=false;
    }

    static bool Held(Keyboard keyboard,Key key) => key!=Key.None && keyboard[key].isPressed;
    static bool Pressed(Keyboard keyboard,Key key) => key!=Key.None && keyboard[key].wasPressedThisFrame;
    void Update()
    {
        var keyboard=Keyboard.current;
        if(keyboard==null) {horizontal=0;wantsRun=wantsCrouch=false;return;}
        horizontal=(Held(keyboard,rightKey)?1:0)-(Held(keyboard,leftKey)?1:0);
        wantsRun=Held(keyboard,runKey);wantsCrouch=Held(keyboard,crouchKey);
        if(Pressed(keyboard,jumpKey)||Pressed(keyboard,alternateJumpKey)) jumpUntil=Time.time+jumpBuffer;
        if(Pressed(keyboard,respawnKey)) respawnRequested=true;
    }
    void OnApplicationFocus(bool focus)
    {
        if(!focus) {horizontal=0;wantsRun=wantsCrouch=false;jumpUntil=-100;}
    }

    bool Grounded()
    {
        if(Time.time-lastJump<0.12f || body.linearVelocity.y>0.15f) return false;
        float radius=capsule.radius*0.8f;
        float bottom=capsule.center.y-capsule.height/2;
        var origin=body.position+Vector3.up*(bottom+radius+0.08f);
        int count=Physics.SphereCastNonAlloc(origin,radius,Vector3.down,groundHits,0.16f,groundMask,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++) if(groundHits[i].collider.attachedRigidbody!=body && groundHits[i].normal.y>0.6f) return true;
        return false;
    }
    bool CanStand()
    {
        float radius=capsule.radius-0.02f;
        float bottom=standingCenter.y-standingHeight/2;
        var a=body.position+Vector3.up*(bottom+radius+0.03f);
        var b=body.position+Vector3.up*(bottom+standingHeight-radius);
        int count=Physics.OverlapCapsuleNonAlloc(a,b,radius,ceilingHits,groundMask,QueryTriggerInteraction.Ignore);
        if(count==ceilingHits.Length) return false;
        for(int i=0;i<count;i++) if(ceilingHits[i].attachedRigidbody!=body) return false;
        return true;
    }
    void FixedUpdate()
    {
        if(respawnRequested || (respawnWhenFalling && body.position.y<respawnBelowY)) {Respawn();return;}
        IsGrounded=Grounded();if(IsGrounded) lastGrounded=Time.time;
        bool crouch=wantsCrouch && IsGrounded;
        if(IsCrouching && !crouch && !CanStand()) crouch=true;
        IsCrouching=crouch;
        capsule.height=crouch?Mathf.Clamp(crouchHeight,2*capsule.radius,standingHeight):standingHeight;
        capsule.center=standingCenter+Vector3.up*((capsule.height-standingHeight)/2);
        var velocity=body.linearVelocity;
        float speed=crouch?crouchSpeed:wantsRun?runSpeed:walkSpeed;
        float rate=IsGrounded?(horizontal==0?braking:acceleration):airAcceleration;
        velocity.x=Mathf.MoveTowards(velocity.x,horizontal*speed,rate*Time.fixedDeltaTime);
        velocity.z=0;
        if(jumpUntil>=Time.time && Time.time-lastGrounded<=coyoteTime && !crouch)
        {
            velocity.y=jumpSpeed;jumpUntil=-100;lastGrounded=-100;lastJump=Time.time;IsGrounded=false;
        }
        body.linearVelocity=velocity;
        if(horizontal!=0) facing=horizontal>0?1:-1;
        animator.SetFloat(SpeedId,Mathf.Abs(velocity.x)/Mathf.Max(0.1f,runSpeed));
        animator.SetBool(GroundedId,IsGrounded);animator.SetFloat(VerticalId,velocity.y);
        animator.SetBool(CrouchId,IsCrouching && IsGrounded);
    }
    void LateUpdate()
    {
        if(visual==null) return;
        visual.rotation=Quaternion.Euler(0,90*facing,0);
        var target=visualRestPosition+Vector3.down*(IsCrouching?crouchVisualDrop:0);
        visual.localPosition=Vector3.MoveTowards(visual.localPosition,target,3*Time.deltaTime);
    }
    void Respawn()
    {
        respawnRequested=false;IsCrouching=false;IsGrounded=false;jumpUntil=lastGrounded=-100;
        capsule.height=standingHeight;capsule.center=standingCenter;
        var point=spawnPoint==null?startPosition:spawnPoint.position;point.z=planeZ;
        body.position=point;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        if(visual!=null) visual.localPosition=visualRestPosition;
    }
    void OnDisable()
    {
        horizontal=0;wantsRun=wantsCrouch=false;
        if(capsule!=null && standingHeight>0) {capsule.height=standingHeight;capsule.center=standingCenter;}
        if(visual!=null) visual.localPosition=visualRestPosition;
    }
}
