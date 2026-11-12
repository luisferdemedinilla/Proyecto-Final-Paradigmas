using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class QuestionBlock : MonoBehaviour
{
    [SerializeField] GameObject questionVisual;
    [SerializeField] GameObject emptyVisual;
    [Tooltip("Objeto que aparece una sola vez sobre el bloque. Su comportamiento pertenece a su prefab.")]
    [SerializeField] GameObject contentPrefab;
    [SerializeField,Min(0)] float contentClearance=.1f;
    public bool IsEmpty {get;private set;}
    public GameObject SpawnedContent {get;private set;}
    BoxCollider hitbox;
    void Awake(){hitbox=GetComponent<BoxCollider>();ResetBlock();}
    void OnCollisionEnter(Collision collision){CheckHit(collision);}
    void OnCollisionStay(Collision collision){CheckHit(collision);}
    void CheckHit(Collision collision)
    {
        if(IsEmpty)return;
        var player=collision.collider.GetComponentInParent<MarioPlayerController>();
        if(player==null || !player.enabled)return;
        var bounds=hitbox.bounds;var playerBounds=player.GetComponent<CapsuleCollider>().bounds;
        bool vertical=false;
        for(int i=0;i<collision.contactCount;i++)if(Mathf.Abs(collision.GetContact(i).normal.y)>.6f)vertical=true;
        if(!vertical)return;
        bool below=player.PrePhysicsVerticalSpeed>.1f && playerBounds.center.y<bounds.center.y && playerBounds.max.y<=bounds.min.y+.2f;
        bool pound=(player.GroundPoundPhase==2 || player.GroundPoundPhase==3) && player.PrePhysicsVerticalSpeed<-.1f && playerBounds.center.y>bounds.center.y && playerBounds.min.y>=bounds.max.y-.2f;
        if(below || pound)ReleaseContent();
    }
    void ReleaseContent()
    {
        IsEmpty=true;questionVisual.SetActive(false);emptyVisual.SetActive(true);
        if(contentPrefab==null)return;
        var bounds=hitbox.bounds;var origin=new Vector3(bounds.center.x,bounds.max.y+contentClearance,bounds.center.z);
        SpawnedContent=Instantiate(contentPrefab,origin,Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(SpawnedContent,gameObject.scene);
        // Allow content prefabs with either a centred pivot or a pivot at their feet.
        bool found=false;var contentBounds=new Bounds();
        foreach(var renderer in SpawnedContent.GetComponentsInChildren<Renderer>())
        {if(!renderer.enabled)continue;if(!found){contentBounds=renderer.bounds;found=true;}else contentBounds.Encapsulate(renderer.bounds);}
        if(found)SpawnedContent.transform.position+=Vector3.up*(origin.y-contentBounds.min.y);
    }
    public void ResetBlock()
    {
        if(SpawnedContent!=null)Destroy(SpawnedContent);
        SpawnedContent=null;IsEmpty=false;questionVisual.SetActive(true);emptyVisual.SetActive(false);
    }
}
