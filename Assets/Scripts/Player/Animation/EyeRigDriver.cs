using UnityEngine;

[ExecuteAlways]
public class EyeRigDriver : MonoBehaviour
{
    public Renderer eyesRenderer;

    [Header("Controles")]
    public Transform eyeshapeCtrl;   // EyeshapeCTRL
    public Transform irisCtrl;       // IrisCTRL
    [Header("Ossos por olho")]
    public Transform shapeL;
    public Transform shapeR;
    public Transform irisL;
    public Transform irisR;

    [Header("Eixos: Blender X/Y/Z -> eixo local do osso no Unity")]
    public Vector3 blenderX = Vector3.right;
    public Vector3 blenderY = Vector3.up;
    public Vector3 blenderZ = Vector3.forward;
    public bool swapScaleXY;

    [Header("Índices")]
    public float indexGain = 5f;
    public int shapeCount = 5;
    public int irisCount = 3;

    [System.Serializable]
    public struct Rest { public Vector3 pos; public Quaternion rot; public Vector3 scale; }
    [SerializeField] Rest rShapeCtrl, rIrisCtrl, rShapeL, rShapeR, rIrisL, rIrisR;
    [SerializeField] bool restCaptured;

    static int ID(string n) => Shader.PropertyToID(n);
    static readonly int LashL = ID("_LashIndexL"), LashR = ID("_LashIndexR"),
        ScleraL = ID("_ScleraIndexL"), ScleraR = ID("_ScleraIndexR"),
        IrisIdxL = ID("_IrisIndexL"), IrisIdxR = ID("_IrisIndexR"),
        OffL = ID("_IrisOffsetL"), OffR = ID("_IrisOffsetR"),
        ScaleL = ID("_IrisScaleL"), ScaleR = ID("_IrisScaleR");

    static Rest Cap(Transform t) =>
        new Rest { pos = t.localPosition, rot = t.localRotation, scale = t.localScale };

    bool AllAssigned() =>
        eyesRenderer && eyeshapeCtrl && irisCtrl && shapeL && shapeR && irisL && irisR;

    [ContextMenu("Capturar pose de repouso")]
    void CaptureRest()
    {
        if (!AllAssigned()) { Debug.LogWarning("EyeRigDriver: atribua todos os ossos primeiro.", this); return; }
        rShapeCtrl = Cap(eyeshapeCtrl); rIrisCtrl = Cap(irisCtrl);
        rShapeL = Cap(shapeL); rShapeR = Cap(shapeR);
        rIrisL = Cap(irisL);   rIrisR = Cap(irisR);
        restCaptured = true;
    }

    // equivalente ao "Location, Local Space" do Blender
    Vector3 Loc(Transform t, Rest r)
    {
        Vector3 d = Quaternion.Inverse(r.rot) * (t.localPosition - r.pos);
        return new Vector3(Vector3.Dot(d, blenderX), Vector3.Dot(d, blenderY), Vector3.Dot(d, blenderZ));
    }

    Vector2 Scl(Transform t, Rest r)
    {
        var s = new Vector2(t.localScale.x / Mathf.Max(r.scale.x, 1e-4f),
                            t.localScale.y / Mathf.Max(r.scale.y, 1e-4f));
        return swapScaleXY ? new Vector2(s.y, s.x) : s;
    }

    int Idx(float v, int count) => Mathf.Clamp(Mathf.RoundToInt(v * indexGain), 0, count - 1);

    void LateUpdate()
    {
        if (!AllAssigned()) return;
        if (!restCaptured) CaptureRest();   // primeira vez: grava a pose atual como repouso
        var m = Application.isPlaying ? eyesRenderer.material : eyesRenderer.sharedMaterial;

        float cy = Loc(eyeshapeCtrl, rShapeCtrl).y;
        int sL = Idx(Loc(shapeL, rShapeL).y + cy, shapeCount);
        int sR = Idx(Loc(shapeR, rShapeR).y + cy, shapeCount);
        m.SetFloat(LashL, sL);   m.SetFloat(LashR, sR);
        m.SetFloat(ScleraL, sL); m.SetFloat(ScleraR, sR);

        Vector3 c = Loc(irisCtrl, rIrisCtrl);
        Vector3 l = Loc(irisL, rIrisL), r = Loc(irisR, rIrisR);
        m.SetFloat(IrisIdxL, Idx(l.z + c.z, irisCount));
        m.SetFloat(IrisIdxR, Idx(r.z + c.z, irisCount));
        m.SetVector(OffL, new Vector4(l.x + c.x, -(l.y + c.y), 0, 0));
        m.SetVector(OffR, new Vector4(r.x + c.x, -(r.y + c.y), 0, 0));

        Vector2 sc = Scl(irisCtrl, rIrisCtrl);
        Vector2 a = Vector2.Scale(Scl(irisL, rIrisL), sc), b = Vector2.Scale(Scl(irisR, rIrisR), sc);
        m.SetVector(ScaleL, new Vector4(a.x, a.y, 0, 0));
        m.SetVector(ScaleR, new Vector4(b.x, b.y, 0, 0));
    }
}