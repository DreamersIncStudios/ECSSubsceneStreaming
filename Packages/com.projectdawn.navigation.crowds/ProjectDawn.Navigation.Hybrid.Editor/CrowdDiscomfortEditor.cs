using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CrowdDiscomfortAuthoring))]
    class CrowdDiscomfortEditor : UnityEditor.Editor
    {
        SerializedProperty m_Type;
        SerializedProperty m_Size;
        SerializedProperty m_Gradient;

        void OnEnable()
        {
            m_Type = serializedObject.FindProperty("m_Type");
            m_Size = serializedObject.FindProperty("m_Size");
            m_Gradient = serializedObject.FindProperty("m_Gradient");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Type);
            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_Gradient);

            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as CrowdDiscomfortAuthoring;
                    if (authoring.HasEntityDiscomfort)
                        authoring.EntityDiscomfort = authoring.DefaultDiscomfort;
                }
            }
        }

        [MenuItem("GameObject/AI/Crowd Discomfort", false, 1003)]
        static void CreateCrowdDiscomfort(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("Crowd Discomfort", parent);
            go.AddComponent<CrowdDiscomfortAuthoring>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }
    }
}
