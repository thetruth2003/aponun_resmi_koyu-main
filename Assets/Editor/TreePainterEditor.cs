#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TreePainter))]
public class TreePainterEditor : Editor
{
    void OnSceneGUI()
    {
        TreePainter painter = (TreePainter)target;

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.shift)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, painter.groundLayer))
            {
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(painter.treePrefab);
                obj.transform.position = hit.point;
                Undo.RegisterCreatedObjectUndo(obj, "Paint Tree");
            }

            e.Use(); // olayý tükettik
        }
    }
}
#endif
