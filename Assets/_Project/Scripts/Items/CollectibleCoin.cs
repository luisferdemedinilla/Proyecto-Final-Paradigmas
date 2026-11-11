using System;
using UnityEngine;
[DisallowMultipleComponent]
public sealed class CollectibleCoin:MonoBehaviour
{
    [SerializeField] Transform visual;
    [SerializeField] float degreesPerSecond=150;
    public bool Collected {get;private set;}
    public event Action<MarioPlayerController> CollectedBy;
    Collider hitbox;Quaternion initialRotation;
    void Awake(){hitbox=GetComponent<Collider>();if(visual==null)visual=transform.Find("Visual");initialRotation=visual.localRotation;}
    void Update(){if(!Collected)visual.Rotate(Vector3.up,degreesPerSecond*Time.deltaTime,Space.Self);}
    void OnTriggerEnter(Collider other){var p=other.GetComponentInParent<MarioPlayerController>();if(p!=null && p.enabled)Collect(p);}
    public void Collect(MarioPlayerController player)
    {
        if(Collected||player==null)return;Collected=true;hitbox.enabled=false;visual.gameObject.SetActive(false);CollectedBy?.Invoke(player);
    }
    public void ResetCoin(){Collected=false;hitbox.enabled=true;visual.gameObject.SetActive(true);visual.localRotation=initialRotation;}
}
