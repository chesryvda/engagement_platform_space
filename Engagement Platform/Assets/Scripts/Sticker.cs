using UnityEngine;

/// <summary>
/// A flat sticker: a quad with one shared material (M_Sticker) that shows its own picture.
/// The picture is set with a MaterialPropertyBlock, so every sticker can show something else
/// without its own material. Works in edit mode too, so you see it while placing.
/// The quad keeps the picture's proportions; "Width" sets its size in metres.
/// Placing: GameObject → Space → Sticker, then hold Ctrl+Shift while dragging the move handle
/// to slide it over a surface (surface snapping). Keep it ~1 mm off the surface against flicker.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class Sticker : MonoBehaviour
{
    [Tooltip("The picture on the sticker (a PNG with transparency).")]
    public Texture2D picture;
    [Tooltip("Width of the sticker in metres.")]
    [Min(0.005f)] public float width = 0.08f;
    [Tooltip("Tint, for faded or dirty stickers.")]
    public Color tint = Color.white;

    static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock block;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var r = GetComponent<MeshRenderer>();
        if (r == null) return;
        block ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        if (picture != null) block.SetTexture(BaseMap, picture);
        block.SetColor(BaseColor, tint);
        r.SetPropertyBlock(block);

        float aspect = picture != null && picture.width > 0 ? (float)picture.height / picture.width : 1f;
        // Width is in real metres, even when the parent (e.g. a Blender import) is scaled.
        Vector3 p = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(width / SafeDiv(p.x), width * aspect / SafeDiv(p.y), 1f / SafeDiv(p.z));
    }

    static float SafeDiv(float v) => Mathf.Abs(v) < 1e-6f ? 1f : v;

#if UNITY_EDITOR
    const string MaterialPath = "Assets/Materials/Stickers/M_Sticker.mat";

    [UnityEditor.MenuItem("GameObject/Space/Sticker", false, 10)]
    static void Create(UnityEditor.MenuCommand command)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Sticker";
        DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<Sticker>();

        // Under the selected object (e.g. the dashboard), at its position.
        if (command.context is GameObject parent)
            UnityEditor.GameObjectUtility.SetParentAndAlign(go, parent);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create sticker");
        UnityEditor.Selection.activeObject = go;
    }
#endif
}
