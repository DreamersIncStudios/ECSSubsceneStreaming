using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CrowdObstacleAuthoring))]
    class CrowdObstacleEditor : UnityEditor.Editor
    {
        SerializedProperty m_Type;
        SerializedProperty m_Size;

        void OnEnable()
        {
            m_Type = serializedObject.FindProperty("m_Type");
            m_Size = serializedObject.FindProperty("m_Size");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Type);
            EditorGUILayout.PropertyField(m_Size);

            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as CrowdObstacleAuthoring;
                    if (authoring.HasEntityObstacle)
                        authoring.EntityObstacle = authoring.DefaultObstacle;
                }
            }
        }

        [MenuItem("GameObject/AI/Crowd Obstacle", false, 1002)]
        static void CreateCrowdObstacle(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("Crowd Obstacle", parent);
            go.AddComponent<CrowdObstacleAuthoring>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }
    }
}
