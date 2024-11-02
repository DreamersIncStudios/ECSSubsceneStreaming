using UnityEngine;
using UnityEditor;
using Unity.Entities;

namespace ProjectDawn.Navigation.Astar
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AgentAstarPathingAuthoring))]
    class AgentAstarPathingEditor : UnityEditor.Editor
    {
#if ENABLE_ASTAR_PATHFINDING_PROJECT
        static class Styles
        {
            public static readonly GUIContent Layers = EditorGUIUtility.TrTextContent("Layers", "");
        }

        SerializedProperty m_AutoRepath;
        SerializedProperty m_Pathfinding;
        SerializedProperty m_LinkTraversalMode;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_AutoRepath);
            EditorGUILayout.PropertyField(m_Pathfinding);
            EditorGUILayout.PropertyField(m_LinkTraversalMode);
            EditorGUILayout.HelpBox("This is experimental feature. Not everything is set to work.", MessageType.Warning);
            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as AgentAstarPathingAuthoring;
                    authoring.Path = authoring.DefaultPath;
                }
            }
        }

        void OnEnable()
        {
            var managedState = serializedObject.FindProperty("m_ManagedState");
            m_AutoRepath = serializedObject.FindProperty("m_Path").FindPropertyRelative("AutoRepath");
            m_Pathfinding = managedState.FindPropertyRelative("pathfindingSettings");
            m_LinkTraversalMode = serializedObject.FindProperty("m_LinkTraversalMode");

        }

#else
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This component integrates A* Pathfinding Project Package as alternative to unity builtin navmesh! Requires com.arongranberg.astar 5.0.8!", MessageType.Error);
            if (GUILayout.Button("Open Asset Store"))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744");
            }
        }
#endif
    }
}
