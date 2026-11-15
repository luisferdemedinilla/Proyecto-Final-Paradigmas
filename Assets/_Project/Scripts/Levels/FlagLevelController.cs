using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public sealed class FlagLevelController : MonoBehaviour
{
    public enum LevelState { Playing, Finishing, Won, Lost }
    [SerializeField] MarioPlayerController player;
    [SerializeField] CollectibleCoin coin;
    [SerializeField] FlagGoal goal;
    [SerializeField] GameObject resultPanel;
    [SerializeField] UnityEngine.UI.Text resultTitle, resultMessage;
    public LevelState State { get; private set; }
    public MarioPlayerController Player => player;
    public IReadOnlyList<GoombaEnemy> Enemies => goombas;
    public CollectibleCoin Coin => coin;
    public FlagGoal Goal => goal;
    private readonly List<ILevelResettable> resettableObjects = new List<ILevelResettable>();
    private readonly List<GoombaEnemy> goombas = new List<GoombaEnemy>();
    void OnEnable() { player.Damaged += Lose; goal.Grabbed += BeginFinish; goal.Completed += Win; }
    void OnDisable() { if (player != null) player.Damaged -= Lose; if (goal != null) { goal.Grabbed -= BeginFinish; goal.Completed -= Win; } }
    void Start() { resultPanel.SetActive(false); FindResettableObjects(); }
    void Update() { if (State == LevelState.Playing && player.GetComponent<Rigidbody>().position.y < -6) EndLoss("Has caído por el precipicio."); if ((State == LevelState.Won || State == LevelState.Lost) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RestartLevel(); }
    void BeginFinish(MarioPlayerController p) { if (p != player) return; State = LevelState.Finishing; StopGoombas(); }
    void Win() { if (State != LevelState.Finishing) return; State = LevelState.Won; Show("¡NIVEL COMPLETADO!", "Has alcanzado la bandera."); }
    void Lose() { EndLoss("El Goomba te ha alcanzado. Salta sobre él para eliminarlo."); }
    void EndLoss(string message) { if (State != LevelState.Playing) return; State = LevelState.Lost; player.enabled = false; var body = player.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.zero; body.isKinematic = true; player.GetComponent<Animator>().speed = 0; StopGoombas(); Show("HAS PERDIDO", message); }
    void Show(string title, string message) { resultTitle.text = title; resultMessage.text = message; resultPanel.SetActive(true); }

    private void StopGoombas()
    {
        foreach (GoombaEnemy goomba in goombas)
        {
            if (goomba != null)
                goomba.SetStopped(true);
        }
    }


    private void FindResettableObjects()
    {
        resettableObjects.Clear();
        goombas.Clear();

        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (component == null || !component.gameObject.activeInHierarchy)
                    continue;

                if (component is ILevelResettable resettable)
                {
                    resettableObjects.Add(resettable);
                }

                if (component is GoombaEnemy goomba)
                    goombas.Add(goomba);
            }
        }
    }
    public void RestartLevel()
    {
        foreach (ILevelResettable resettable in resettableObjects)
        {
            if (resettable is MonoBehaviour component && component != null)
                resettable.ResetForLevel();
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        RigidbodyInterpolation previousInterpolation = body.interpolation;

        body.interpolation = RigidbodyInterpolation.None;
        body.isKinematic = false;

        player.ResetToSpawn();
        player.transform.position = body.position;
        Physics.SyncTransforms();

        body.interpolation = previousInterpolation;
        player.GetComponent<Animator>().speed = 1f;
        player.enabled = true;

        resultPanel.SetActive(false);
        State = LevelState.Playing;
    }
}
