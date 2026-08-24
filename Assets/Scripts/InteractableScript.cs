using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    public string interactionPrompt = "Interagir";
    public UnityEvent onInteract;

    // Chamado pelo PointTargetScript quando o jogador confirma a interação
    public void Interact()
    {
        onInteract?.Invoke();
    }

    public void SetInteractionTarget(UnityEvent newEvent)
    {
        onInteract = newEvent;
    }
}