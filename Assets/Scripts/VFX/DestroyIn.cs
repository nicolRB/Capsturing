using UnityEngine;

public class DestroyIn : MonoBehaviour
{
    [SerializeField] private float seconds = 1;
    
    void Start()
    {
        Destroy(gameObject, seconds);
    }
}
