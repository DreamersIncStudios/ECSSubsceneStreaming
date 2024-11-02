using UnityEditor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AgentCrowdPathingAuthoring))]
    class AgentCrowdPathingEditor : UnityEditor.Editor
    {
        SerializedProperty m_Group;

        void OnEnable()
        {
            m_Group = serializedObject.FindProperty("m_Group");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Group);

            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as AgentCrowdPathingAuthoring;
                    if (authoring.HasEntityPath)
                        authoring.EntityPath = authoring.DefaultPath;
                }
            }
        }
    }
}
