using UnityEngine;

public class Passage : MonoBehaviour
{
    [SerializeField] private int targetSceneIndex;
    [SerializeField] private string entryPointId;
    [SerializeField] private Vector3 targetCoordinates;
    [SerializeField] private Quaternion rotation;
    [SerializeField] private Collider passageZone;
    [SerializeField] private bool manualCoordinates;

    private bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        used = true;
        GoToLocation();
    }

    public void GoToLocation()
    {
        if (SceneChanger.Instance != null && manualCoordinates)
        {
            SceneChanger.Instance.LoadSceneAtCoordinate(targetSceneIndex, targetCoordinates, rotation);
        }
        else if (SceneChanger.Instance != null)
        {
            SceneChanger.Instance.LoadSceneAtEntry(targetSceneIndex, entryPointId);
        }
    }
}
