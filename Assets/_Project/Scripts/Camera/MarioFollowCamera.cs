using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class MarioFollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset=new Vector3(2,1.8f,-25);
    [SerializeField,Min(0.01f)] float smoothTime=0.18f;
    [SerializeField] bool followVertical=true;
    [SerializeField] float minimumY=1.8f;
    [SerializeField,Min(1)] float orthographicSize=4.5f;
    Vector3 velocity;
    public Transform Target => target;
    void Start()
    {
        var camera=GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=orthographicSize;
        if(target!=null) transform.position=Desired();
        transform.rotation=Quaternion.identity;
    }
    Vector3 Desired()
    {
        var desired=target.position+offset;
        desired.y=followVertical?Mathf.Max(minimumY,desired.y):transform.position.y;
        desired.z=offset.z;return desired;
    }
    void LateUpdate()
    {
        if(target==null) return;
        transform.position=Vector3.SmoothDamp(transform.position,Desired(),ref velocity,smoothTime);
    }
}
