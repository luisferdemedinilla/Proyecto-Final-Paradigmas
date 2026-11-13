using UnityEngine;
namespace Creator
{
    public sealed class CreatorGoal : MonoBehaviour
    {
        public CreatorGameFlow flow;
        void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<MarioPlayerController>() != null) flow.Finish(true);
        }
    }
}
