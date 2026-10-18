using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    public void Setup(int damageAmount, Color damageColor, float damageScale)
    {
        textMesh.SetText(damageAmount.ToString());
        textMesh.color = damageColor;
        textMesh.transform.localScale = new Vector3(damageScale, damageScale, damageScale);
        textColor = textMesh.color;
        disappearTimer = 1f;
    }

    void Update()
    {
        // Moves the damage up and right softly
        transform.position += new Vector3(0, 1f * Time.deltaTime, 0);

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Fade out
            float alphaSpeed = 3f;
            textColor.a -= alphaSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}