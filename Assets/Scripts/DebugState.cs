using UnityEngine;

public class DebugState : StateMachineBehaviour
{
    public string nombreEstado;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"[Animator] Entrando al estado: {nombreEstado}");
    }
}