using UnityEngine;
using UnityEditor;
using MLAPI.Prototyping;

namespace MLAPI.Editor
{
    public enum SyncAxis
    {
        X = 1,
        Y = 2,
        Z = 4
    }

    [CustomEditor(typeof(NetworkTransform))]
    [CanEditMultipleObjects]
    public class NetworkTransformEditor : UnityEditor.Editor
    {
        private void ToggleBlock(SyncAxis axisMask, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(135));
            EditorGUILayout.ToggleLeft("X", true, GUILayout.Width(40));
            EditorGUILayout.ToggleLeft("Y", true, GUILayout.Width(40));
            EditorGUILayout.ToggleLeft("Z", true, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var networkTransform = (NetworkTransform)target;
            if (networkTransform == null)
            {
                return;
            }

            EditorGUILayout.Foldout(true, "Sync Props");
            EditorGUI.indentLevel++;
            EditorGUILayout.Toggle("Position", true);
            EditorGUILayout.Toggle("Rotation", true);
            EditorGUILayout.Toggle("Scale", false);
            EditorGUI.indentLevel--;

            EditorGUILayout.Foldout(true, "Sync Matrix");
            EditorGUI.indentLevel++;
            GUI.enabled = networkTransform.SyncProps.HasFlag(SyncMask.Position);
            ToggleBlock(SyncAxis.X | SyncAxis.Y | SyncAxis.Z, "Position");
            GUI.enabled = networkTransform.SyncProps.HasFlag(SyncMask.Rotation);
            ToggleBlock(SyncAxis.X | SyncAxis.Y, "Rotation");
            GUI.enabled = networkTransform.SyncProps.HasFlag(SyncMask.Scale);
            ToggleBlock(SyncAxis.Y | SyncAxis.Z, "Scale");
            GUI.enabled = true;
            EditorGUI.indentLevel--;

            EditorGUILayout.Foldout(true, "Sync Thresholds");
            EditorGUI.indentLevel++;
            EditorGUILayout.FloatField("Position", 0.1f);
            EditorGUILayout.FloatField("Rotation", 1f);
            GUI.enabled = false;
            EditorGUILayout.FloatField("Scale", 0.1f);
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }
    }
}
