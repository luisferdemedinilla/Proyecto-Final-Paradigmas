using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class QuestionBlock : MonoBehaviour, ILevelResettable
{
    [SerializeField] GameObject questionVisual;
    [SerializeField] GameObject emptyVisual;
    [Tooltip("Objeto que aparece una sola vez sobre el bloque. Su comportamiento pertenece a su prefab.")]
    [SerializeField] GameObject contentPrefab;
    [SerializeField,Min(0)] float contentClearance=.1f;
    public bool IsEmpty {get;private set;}
    public GameObject SpawnedContent {get;private set;}
    BoxCollider hitbox;
    void Awake(){hitbox=GetComponent<BoxCollider>(); ResetForLevel();}
    void OnCollisionEnter(Collision collision){
        if (!IsEmpty)
        {
            if (BlockHitDetector.CheckHit(collision, hitbox)) ReleaseContent();
        }
    }
    void OnCollisionStay(Collision collision)
    {
        if (!IsEmpty)
        {
            if (BlockHitDetector.CheckHit(collision, hitbox)) ReleaseContent();
        }
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
    public void ResetForLevel()
    {
        if(SpawnedContent!=null)Destroy(SpawnedContent);
        SpawnedContent=null;IsEmpty=false;questionVisual.SetActive(true);emptyVisual.SetActive(false);
    }
}
