using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Staticground : MonoBehaviour
{
    public enum Axis { X, Z }
    [Header("要生成的预制体")]
    public GameObject prefab;

    [Header("指定生成数量")]
    [Min(1)] public int amount = 10;

    [Header("生成起始位置")]
    public Vector3 startPosition = Vector3.zero;

    [Header("生成方向（X/Z 轴向）")]
    public Axis axis = Axis.X; // 仅沿 X 或 Z（XZ 平面）

    [Header("是否反向")]
    public bool reverseDirection = false;

    [Header("间距设置")]
    public bool autoSpacing = true; // 自动根据预制体尺寸计算间距
    [Min(0)] public float spacing = 1f; // 手动设置间距（当 autoSpacing 为 false 时使用）

    [Header("每个物体依次偏移（用于避免重叠，默认为0）")]
    public Vector3 perItemOffset = Vector3.zero;

    [Header("标记为静态（勾选以启用静态批处理）")]
    public bool markAsStatic = true;

#if UNITY_EDITOR
    [ContextMenu("生成（使用 amount）")]
    public void PlacePrefabs() => PlacePrefabsInternal(amount);

    // 获取预制体在指定轴向的尺寸
    public float GetPrefabSize(GameObject prefab, Axis axis)
    {
        if (prefab == null) return 1f;

        // 尝试获取 Renderer 组件的 bounds
        Renderer renderer = prefab.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            return axis == Axis.X ? bounds.size.x : bounds.size.z;
        }

        // 如果没有 Renderer，尝试获取 Collider 的 bounds
        Collider collider = prefab.GetComponent<Collider>();
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            return axis == Axis.X ? bounds.size.x : bounds.size.z;
        }

        // 递归检查子物体的 Renderer
        Renderer[] childRenderers = prefab.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            Bounds combinedBounds = childRenderers[0].bounds;
            for (int i = 1; i < childRenderers.Length; i++)
            {
                combinedBounds.Encapsulate(childRenderers[i].bounds);
            }
            return axis == Axis.X ? combinedBounds.size.x : combinedBounds.size.z;
        }

        // 如果都没有，返回默认值
        return 1f;
    }

    public void PlacePrefabsInternal(int n)
    {
        if (prefab == null)
        {
            Debug.LogError("请在 Inspector 中指定预制体。");
            return;
        }
        if (n < 1)
        {
            Debug.LogWarning("指定数量应 >= 1。");
            return;
        }

        // 基于起始位置与轴向计算放置方向
        Vector3 basePosition = startPosition;
        Vector3 dir3 = (axis == Axis.X ? Vector3.right : Vector3.forward) * (reverseDirection ? -1f : 1f);

        // 计算实际使用的间距
        float actualSpacing = autoSpacing ? GetPrefabSize(prefab, axis) : spacing;

        for (int i = 0; i < n; i++)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);
            Vector3 spawnPos = basePosition + dir3 * (actualSpacing * i) + perItemOffset * i;

            obj.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            if (markAsStatic) obj.isStatic = true;

            Undo.RegisterCreatedObjectUndo(obj, "Place Prefab");
        }

        EditorUtility.SetDirty(gameObject);
        string spacingInfo = autoSpacing ? $"自动间距 {actualSpacing:F2}" : $"手动间距 {spacing}";
        Debug.Log($"生成 {n} 个预制体，起点 {startPosition}，轴向 {axis}{(reverseDirection ? "（反向）" : "")}，{spacingInfo}。");
    }

    [ContextMenu("清空子物体（可撤销）")]
    public void ClearChildren()
    {
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in transform) toDelete.Add(t.gameObject);
        foreach (var go in toDelete) Undo.DestroyObjectImmediate(go);
        Debug.Log("已清空当前对象下的所有子物体（可撤销）");
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(Staticground))]
public class StaticPrefabPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var placer = (Staticground)target;

        EditorGUILayout.Space();
        
        // 显示当前预制体的尺寸信息（如果开启了自动间距）
        if (placer.autoSpacing && placer.prefab != null)
        {
            float sizeX = placer.GetPrefabSize(placer.prefab, Staticground.Axis.X);
            float sizeZ = placer.GetPrefabSize(placer.prefab, Staticground.Axis.Z);
            
            EditorGUILayout.HelpBox($"预制体尺寸: X={sizeX:F2}, Z={sizeZ:F2}\n当前轴向 {placer.axis} 将使用间距: {(placer.axis == Staticground.Axis.X ? sizeX : sizeZ):F2}", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        placer.amount = Mathf.Max(1, EditorGUILayout.IntField("指定数量 (amount)", placer.amount));
        if (GUILayout.Button("生成 amount 个", GUILayout.Height(28)))
        {
            placer.PlacePrefabsInternal(placer.amount);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("清空子物体（可撤销）", GUILayout.Height(22)))
        {
            placer.ClearChildren();
        }
    }
}
#endif

