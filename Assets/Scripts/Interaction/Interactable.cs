using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "Interagir";
    [SerializeField] private UnityEvent onInteract;

    // Called by PointTargetScript when player presses the interact button
    public void Interact()
    {
        onInteract?.Invoke();
    }

    public void SetInteractionTarget(UnityEvent newEvent)
    {
        onInteract = newEvent;
    }
}