using UnityEditor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CrowdData))]
    class CrowdDataEditor : UnityEditor.Editor
    {
        SerializedProperty m_MaxSlope;
        SerializedProperty m_MaxHeight;
        SerializedProperty m_Radius;
        SerializedProperty m_Collection;
        SerializedProperty m_GizmosColor;
        SerializedProperty m_Baked;

        void OnEnable()
        {
            m_MaxSlope = serializedObject.FindProperty("m_MaxSlope");
            m_MaxHeight = serializedObject.FindProperty("m_MaxHeight");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Collection = serializedObject.FindProperty("m_Collection");
            m_GizmosColor = serializedObject.FindProperty("m_GizmosColor");
            m_Baked = serializedObject.FindProperty("m_Baked");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_MaxSlope);
            EditorGUILayout.PropertyField(m_MaxHeight);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Collection);
            EditorGUILayout.PropertyField(m_GizmosColor);

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(m_Baked);
            }

            EditorGUILayout.HelpBox("This is experimental feature. Not everything is set to work and will change in the future. Use at your own risk.", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
