using UnityEngine;
using UnityEngine.InputSystem;
public sealed class DemoLevelController:MonoBehaviour
{
    public enum LevelState {Playing,Won,Lost}
    [SerializeField] MarioPlayerController player;
    [SerializeField] GoombaEnemy goomba;
    [Tooltip("Only this designated coin completes this level. Ordinary coins just collect.")]
    [SerializeField] CollectibleCoin goalCoin;
    [SerializeField] GameObject resultPanel;
    [SerializeField] UnityEngine.UI.Text resultTitle,resultMessage;
    [SerializeField] Key retryKey=Key.R;
    public LevelState State {get;private set;}
    public MarioPlayerController Player=>player;
    public CollectibleCoin GoalCoin=>goalCoin;
    public GoombaEnemy Enemy=>goomba;
    void OnEnable(){player.Damaged+=Lose;goalCoin.CollectedBy+=GoalCollected;}
    void OnDisable(){if(player!=null)player.Damaged-=Lose;if(goalCoin!=null)goalCoin.CollectedBy-=GoalCollected;}
    void Start(){resultPanel.SetActive(false);}
    void Update()
    {
        // Rigidbody position is current immediately after respawn; the interpolated
        // Transform can still show the previous position below the death plane.
        if(State==LevelState.Playing && player.GetComponent<Rigidbody>().position.y< -6)
            Finish(LevelState.Lost,"Has caído por el precipicio. Pulsa R o Reintentar para volver a empezar.");
        if(State!=LevelState.Playing && Keyboard.current!=null && retryKey!=Key.None && Keyboard.current[retryKey].wasPressedThisFrame)RestartLevel();
    }
    void GoalCollected(MarioPlayerController collector){if(collector==player)Finish(LevelState.Won);}
    public void Lose(){Finish(LevelState.Lost);}
    void Finish(LevelState state,string lossMessage=null)
    {
        if(State!=LevelState.Playing)return;State=state;
        player.enabled=false;var body=player.GetComponent<Rigidbody>();body.linearVelocity=Vector3.zero;body.isKinematic=true;player.GetComponent<Animator>().speed=0;
        goomba.SetStopped(true);resultTitle.text=state==LevelState.Won?"¡NIVEL COMPLETADO!":"HAS PERDIDO";
        resultMessage.text=state==LevelState.Won?"Has conseguido la moneda dorada.":lossMessage??"Salta sobre el Goomba. Evita tocarlo de lado.";
        resultPanel.SetActive(true);
    }
    public void RestartLevel()
    {
        var body=player.GetComponent<Rigidbody>();var interpolation=body.interpolation;
        body.interpolation=RigidbodyInterpolation.None;body.isKinematic=false;
        player.ResetToSpawn();
        // Teleport both representations before reenabling defeat checks. Otherwise
        // interpolation/transform synchronization can restore the old fall pose.
        player.transform.position=body.position;Physics.SyncTransforms();body.interpolation=interpolation;
        player.enabled=true;player.GetComponent<Animator>().speed=1;goomba.RestartEnemy();goalCoin.ResetCoin();
        resultPanel.SetActive(false);State=LevelState.Playing;
    }
}
