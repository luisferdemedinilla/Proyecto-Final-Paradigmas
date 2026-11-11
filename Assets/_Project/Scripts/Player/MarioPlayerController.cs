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
    [SerializeField] bool requireRunKey=false;
    [SerializeField,Min(0)] float acceleration=8f;
    [SerializeField,Min(0)] float braking=18f;
    [SerializeField,Min(0)] float airAcceleration=6f;
    [Header("Salto variable")]
    [SerializeField,Min(0.1f)] float jumpSpeed=8.5f;
    [SerializeField,Min(0.1f)] float minimumJumpSpeed=4f;
    [SerializeField,Min(0)] float jumpHoldAcceleration=18f;
    [SerializeField,Range(0,0.6f)] float maxJumpHoldTime=0.3f;
    [SerializeField,Range(0,0.3f)] float coyoteTime=0.1f;
    [SerializeField,Range(0,0.3f)] float jumpBuffer=0.12f;
    [SerializeField] LayerMask groundMask=~0;
    [Header("Giro en el aire")]
    [SerializeField,Min(0.05f)] float airSpinDuration=0.45f;
    [SerializeField,Min(0)] float airSpinFallSpeed=1.5f;
    [SerializeField,Min(0.01f)] float airSpinClipLength=0.45f;
    [Header("Golpe hacia abajo")]
    [SerializeField,Min(.05f)] float groundPoundWindup=.2f;
    [SerializeField,Min(1)] float groundPoundSpeed=18f;
    [SerializeField,Min(.05f)] float groundPoundRecovery=.18f;
    [SerializeField,HideInInspector] float groundPoundStartClipLength=.5f;
    [SerializeField,HideInInspector] float groundPoundLandClipLength=.5f;
    [Header("Agacharse")]
    [SerializeField,Min(0.1f)] float crouchSpeed=1.4f;
    [SerializeField,Min(0.64f)] float crouchHeight=1.4f;
    [Tooltip("Limite del ajuste visual calculado a partir de los tobillos; mantiene los pies apoyados.")]
    [SerializeField,Min(0)] float crouchVisualDrop=0.35f;
    [SerializeField] bool authoredCrouchHeight=false;
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
    float horizontal,jumpHoldRemaining;
    bool wantsRun,wantsCrouch,respawnRequested,jumpHeld,sustainingJump;
    bool jumpPressed,hasJumped,airSpinUsed;
    float airSpinUntil=-100;
    Transform spinRoot,authoredAnimationRoot;
    Quaternion authoredRootRestRotation;
    Quaternion spinRootRestRotation;
    public bool IsAirSpinning {get;private set;}
    Transform leftAnkle,rightAnkle;
    float leftAnkleRestY,rightAnkleRestY;
    int facing=1,poundPhase;
    float poundUntil;
    bool poundRequested,supportsPound,supportsJumping;
    public bool IsGroundPounding => poundPhase==1 || poundPhase==2;
    public int GroundPoundPhase => poundPhase;
    public float PrePhysicsVerticalSpeed {get;private set;}
    public event System.Action Damaged;
    public void TakeHit(){Damaged?.Invoke();}
    public void ResetToSpawn(){Respawn();}
    public void BounceOffEnemy(float speed)
    {
        CancelPound();sustainingJump=false;IsGrounded=false;lastGrounded=-100;lastJump=Time.time;
        hasJumped=true;airSpinUsed=false;airSpinUntil=-100;IsAirSpinning=false;jumpUntil=-100;
        body.linearVelocity=new Vector3(body.linearVelocity.x,speed,0);
        animator.SetBool(AirSpinId,false);if(supportsJumping)animator.SetBool("Jumping",true);animator.Play("Jump",0,0);
    }
    void CancelPound(){poundPhase=0;poundRequested=false;if(supportsPound)animator.SetBool("GroundPounding",false);}

    public bool IsGrounded {get;private set;}
    public bool IsCrouching {get;private set;}
    public float HorizontalSpeed => body==null?0:body.linearVelocity.x;

    static readonly int SpeedId=Animator.StringToHash("Speed");
    static readonly int GroundedId=Animator.StringToHash("Grounded");
    static readonly int VerticalId=Animator.StringToHash("VerticalSpeed");
    static readonly int CrouchId=Animator.StringToHash("Crouching");

    static readonly int AirSpinId=Animator.StringToHash("AirSpinning");
    static readonly int AirSpinSpeedId=Animator.StringToHash("AirSpinSpeed");

    void Awake()
    {
        body=GetComponent<Rigidbody>();capsule=GetComponent<CapsuleCollider>();animator=GetComponent<Animator>();
        if(visual==null) visual=transform.Find("Visual");
        foreach(var bone in (visual==null?transform:visual).GetComponentsInChildren<Transform>(true))
        {
            if(bone.name=="AllRoot") {authoredAnimationRoot=bone;authoredRootRestRotation=bone.localRotation;}
            if(bone.name=="skl_root") {spinRoot=bone;spinRootRestRotation=bone.localRotation;}
            if((bone.name=="ankle_l1" || bone.name=="FootL")) leftAnkle=bone;
            if((bone.name=="ankle_r1" || bone.name=="FootR")) rightAnkle=bone;
        }
        if(leftAnkle!=null) leftAnkleRestY=transform.InverseTransformPoint(leftAnkle.position).y;
        if(rightAnkle!=null) rightAnkleRestY=transform.InverseTransformPoint(rightAnkle.position).y;
        standingHeight=capsule.height;standingCenter=capsule.center;
        if(visual!=null) visualRestPosition=visual.localPosition;
        startPosition=body.position;planeZ=body.position.z;
        body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        animator.applyRootMotion=false;
        supportsPound=animator.HasState(0,Animator.StringToHash("GroundPoundStart"));
        foreach(var parameter in animator.parameters)if(parameter.name=="Jumping")supportsJumping=true;
    }

    static bool Held(Keyboard keyboard,Key key) => key!=Key.None && keyboard[key].isPressed;
    static bool Pressed(Keyboard keyboard,Key key) => key!=Key.None && keyboard[key].wasPressedThisFrame;
    void Update()
    {
        var keyboard=Keyboard.current;
        if(keyboard==null) {horizontal=0;wantsRun=wantsCrouch=jumpHeld=sustainingJump=false;return;}
        horizontal=(Held(keyboard,rightKey)?1:0)-(Held(keyboard,leftKey)?1:0);
        wantsRun=Held(keyboard,runKey);wantsCrouch=Held(keyboard,crouchKey);
        bool previouslyHeld=jumpHeld;
        jumpHeld=Held(keyboard,jumpKey)||Held(keyboard,alternateJumpKey);
        if(!jumpHeld) sustainingJump=false;
        if(!previouslyHeld && (Pressed(keyboard,jumpKey)||Pressed(keyboard,alternateJumpKey))) jumpPressed=true;
        if(Pressed(keyboard,crouchKey)) poundRequested=true;
        if(Pressed(keyboard,respawnKey)) respawnRequested=true;
    }
    void OnApplicationFocus(bool focus)
    {
        if(!focus) {horizontal=0;wantsRun=wantsCrouch=jumpHeld=sustainingJump=false;jumpUntil=-100;jumpPressed=poundRequested=false;}
    }

    bool Grounded()
    {
        // A real jump must leave support immediately. Small upward contact
        // impulses at block seams do not mean the feet have left the floor.
        if(Time.time-lastJump<0.12f || (hasJumped && body.linearVelocity.y>0.15f)) return false;
        float radius=capsule.radius*0.8f;
        float bottom=capsule.center.y-capsule.height/2;
        var origin=body.position+Vector3.up*(bottom+radius+0.08f);
        int count=Physics.SphereCastNonAlloc(origin,radius,Vector3.down,groundHits,0.16f,groundMask,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++) if(groundHits[i].collider.attachedRigidbody!=body && groundHits[i].normal.y>0.6f) return true;
        // Near adjacent box edges the sphere may report an edge normal. Probe
        // actual support under the centre and both sides of the foot as well.
        for(int probe=-1;probe<=1;probe++)
        {
            var foot=body.position+Vector3.up*(bottom+.12f)+Vector3.right*(probe*radius*.7f);
            count=Physics.RaycastNonAlloc(foot,Vector3.down,groundHits,.2f,groundMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(groundHits[i].collider.attachedRigidbody!=body && groundHits[i].normal.y>.6f)return true;
        }
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
        int previousPoundPhase=poundPhase;
        IsGrounded=Grounded();
        if(IsGrounded && poundPhase==2){poundPhase=3;poundUntil=Time.time+groundPoundRecovery;}
        if(poundPhase==3 && Time.time>=poundUntil)CancelPound();
        if(poundRequested && supportsPound && !IsGrounded && hasJumped && poundPhase==0)
        {
            poundPhase=1;poundUntil=Time.time+groundPoundWindup;sustainingJump=false;airSpinUntil=-100;IsAirSpinning=false;jumpUntil=-100;

        }
        poundRequested=false;
        if(poundPhase==1 && Time.time>=poundUntil){poundPhase=2;}
        if(poundPhase!=0)jumpPressed=false;
        if(IsGrounded) {lastGrounded=Time.time;hasJumped=false;airSpinUsed=false;airSpinUntil=-100;}
        if(jumpPressed)
        {
            if(!IsGrounded && hasJumped)
            {
                if(!airSpinUsed)
                {
                    airSpinUsed=true;airSpinUntil=Time.time+Mathf.Max(.05f,airSpinDuration);
                    sustainingJump=false;jumpUntil=-100;
                    // Start immediately even while Jump/Fall is blending.
                    animator.Play("AirSpin",0,0f);
                }
                // Extra airborne presses cannot buffer an unintended landing jump.
            }
            else jumpUntil=Time.time+jumpBuffer;
            jumpPressed=false;
        }
        IsAirSpinning=!IsGrounded && Time.time<airSpinUntil;
        bool crouch=wantsCrouch && IsGrounded && poundPhase==0;
        if(IsCrouching && !crouch && !CanStand()) crouch=true;
        IsCrouching=crouch;
        capsule.height=crouch?Mathf.Clamp(crouchHeight,2*capsule.radius,standingHeight):standingHeight;
        capsule.center=standingCenter+Vector3.up*((capsule.height-standingHeight)/2);
        var velocity=body.linearVelocity;
        float speed=crouch?crouchSpeed:(!requireRunKey || wantsRun)?runSpeed:walkSpeed;
        float rate=IsGrounded?(horizontal==0?braking:acceleration):airAcceleration;
        velocity.x=Mathf.MoveTowards(velocity.x,horizontal*speed,rate*Time.fixedDeltaTime);
        velocity.z=0;
        if(jumpUntil>=Time.time && Time.time-lastGrounded<=coyoteTime && !crouch && poundPhase==0)
        {
            velocity.y=Mathf.Min(minimumJumpSpeed,jumpSpeed);jumpUntil=-100;lastGrounded=-100;lastJump=Time.time;IsGrounded=false;sustainingJump=jumpHeld;hasJumped=true;jumpHoldRemaining=maxJumpHoldTime;
        }
        // Releasing ends assistance permanently for this jump. Holding longer
        // than the configured window cannot add more height.
        if(sustainingJump)
        {
            if(!jumpHeld || jumpHoldRemaining<=0.00001f || velocity.y<=0) sustainingJump=false;
            else
            {
                float assistTime=Mathf.Min(Time.fixedDeltaTime,jumpHoldRemaining);
                velocity.y=Mathf.Min(jumpSpeed,velocity.y+jumpHoldAcceleration*assistTime);
                jumpHoldRemaining=Mathf.Max(0,jumpHoldRemaining-assistTime);
            }
        }
        if(IsAirSpinning && velocity.y<0)
            velocity.y=Mathf.Max(velocity.y,-airSpinFallSpeed-Physics.gravity.y*Time.fixedDeltaTime);
        if(poundPhase==1)velocity=new Vector3(0,-Physics.gravity.y*Time.fixedDeltaTime,0);
        else if(poundPhase==2)velocity=new Vector3(0,-groundPoundSpeed,0);
        else if(poundPhase==3)velocity.x=0;
        PrePhysicsVerticalSpeed=velocity.y;
        body.linearVelocity=velocity;
        if(horizontal!=0) facing=horizontal>0?1:-1;
        animator.SetFloat(SpeedId,Mathf.Abs(velocity.x)/Mathf.Max(0.1f,runSpeed));
        animator.SetBool(GroundedId,IsGrounded);animator.SetFloat(VerticalId,velocity.y);
        if(supportsJumping)animator.SetBool("Jumping",hasJumped && !IsGrounded && velocity.y>0 && poundPhase==0);
        animator.SetBool(CrouchId,IsCrouching && IsGrounded);
        animator.SetBool(AirSpinId,IsAirSpinning);
        if(supportsPound)
        {
            animator.SetBool("GroundPounding",poundPhase!=0);
            animator.SetFloat("PoundStartSpeed",groundPoundStartClipLength/Mathf.Max(.05f,groundPoundWindup));
            animator.SetFloat("PoundLandSpeed",groundPoundLandClipLength/Mathf.Max(.05f,groundPoundRecovery));
            // Select the phase only after updating all transition parameters.
            if(poundPhase!=previousPoundPhase && poundPhase!=0)
                animator.Play(poundPhase==1?"GroundPoundStart":poundPhase==2?"GroundPoundFall":"GroundPoundLand",0,0);
        }
        animator.SetFloat(AirSpinSpeedId,airSpinClipLength/Mathf.Max(.05f,airSpinDuration));
    }
    void LateUpdate()
    {
        // Other clips do not animate this bone: restore it on completion or interruption.
        if(!IsAirSpinning && spinRoot!=null) spinRoot.localRotation=spinRootRestRotation;
        if(visual==null) return;
        visual.rotation=Quaternion.Euler(0,90*facing,0);
        bool crouchPose=IsCrouching || animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch") ||
            (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("Crouch"));
        float drop=0;
        if(!authoredCrouchHeight && crouchPose && leftAnkle!=null && rightAnkle!=null)
        {
            float applied=visual.localPosition.y-visualRestPosition.y;
            float left=transform.InverseTransformPoint(leftAnkle.position).y-applied-leftAnkleRestY;
            float right=transform.InverseTransformPoint(rightAnkle.position).y-applied-rightAnkleRestY;
            drop=Mathf.Clamp(Mathf.Min(left,right),0,crouchVisualDrop);
        }
        // Follow the evaluated pose, including transitions, rather than sinking
        // the whole character by a fixed distance before its knees have bent.
        visual.localPosition=visualRestPosition+Vector3.down*drop;
    }
    void Respawn()
    {
        CancelPound();
        ResetAirSpin();
        respawnRequested=false;sustainingJump=false;IsCrouching=false;IsGrounded=false;jumpUntil=lastGrounded=-100;
        capsule.height=standingHeight;capsule.center=standingCenter;
        var point=spawnPoint==null?startPosition:spawnPoint.position;point.z=planeZ;
        body.position=point;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        animator.SetFloat(SpeedId,0);animator.SetFloat(VerticalId,0);animator.SetBool(CrouchId,false);
        animator.Play("Idle",0,0);
        if(visual!=null) visual.localPosition=visualRestPosition;
    }
    void ResetAirSpin()
    {
        jumpPressed=hasJumped=airSpinUsed=IsAirSpinning=false;airSpinUntil=-100;
        if(authoredAnimationRoot!=null) authoredAnimationRoot.localRotation=authoredRootRestRotation;
        if(spinRoot!=null) spinRoot.localRotation=spinRootRestRotation;
        if(animator!=null) animator.SetBool(AirSpinId,false);
    }
    void OnDisable()
    {
        CancelPound();
        ResetAirSpin();
        horizontal=0;wantsRun=wantsCrouch=jumpHeld=sustainingJump=false;
        if(capsule!=null && standingHeight>0) {capsule.height=standingHeight;capsule.center=standingCenter;}
        if(visual!=null) visual.localPosition=visualRestPosition;
    }
}
