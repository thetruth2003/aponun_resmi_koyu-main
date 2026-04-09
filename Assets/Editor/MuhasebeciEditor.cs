using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Muhasebeci))]
public class MuhasebeciEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Bu script oyunun para merkezi. Satış ve alış kesintilerini aşağıdan tek tek ekleyebilir, geçici olarak kapatabilir ve hazır presetleri tek tıkla yükleyebilirsin.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Hazır Presetler", EditorStyles.boldLabel);

        Muhasebeci muhasebeci = (Muhasebeci)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Satış / Yumuşak"))
        {
            Undo.RecordObject(muhasebeci, "Load Soft Sale Charges");
            muhasebeci.LoadSoftSaleCharges();
            EditorUtility.SetDirty(muhasebeci);
        }

        if (GUILayout.Button("Satış / Acımasız"))
        {
            Undo.RecordObject(muhasebeci, "Load Harsh Sale Charges");
            muhasebeci.LoadHarshSaleCharges();
            EditorUtility.SetDirty(muhasebeci);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Satış / Recep Modu"))
        {
            Undo.RecordObject(muhasebeci, "Load Recep Sale Charges");
            muhasebeci.LoadRecepSaleCharges();
            EditorUtility.SetDirty(muhasebeci);
        }

        if (GUILayout.Button("Satış / Temizle"))
        {
            Undo.RecordObject(muhasebeci, "Clear Sale Charges");
            muhasebeci.ClearSaleCharges();
            EditorUtility.SetDirty(muhasebeci);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Alış / Standart"))
        {
            Undo.RecordObject(muhasebeci, "Load Standard Purchase Charges");
            muhasebeci.LoadStandardPurchaseCharges();
            EditorUtility.SetDirty(muhasebeci);
        }

        if (GUILayout.Button("Alış / Temizle"))
        {
            Undo.RecordObject(muhasebeci, "Clear Purchase Charges");
            muhasebeci.ClearPurchaseCharges();
            EditorUtility.SetDirty(muhasebeci);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "İpucu: Percent için 0.15 = %15. PerKg kilogram başına işler. Flat sabit tutar keser. Gross brüt satışa, Subtotal o ana kadarki ara toplama uygulanır.",
            MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }
}
