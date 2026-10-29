using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;
}