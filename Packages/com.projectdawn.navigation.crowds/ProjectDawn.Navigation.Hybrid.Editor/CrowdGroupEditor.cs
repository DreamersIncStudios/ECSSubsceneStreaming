using UnityEngine;
using UnityEditor;
using Unity.Entities;
using ProjectDawn.Navigation.Editor;
using Unity.AI.Navigation.Editor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CrowdGroupAuthoring))]
    class CrowdGroupEditor : UnityEditor.Editor
    {
        SerializedProperty m_Surface;
        SerializedProperty m_Speed;
        SerializedProperty m_CostWeights;
        SerializedProperty m_GoalSource;
        SerializedProperty m_Grounded;
        SerializedProperty m_MappingRadius;

        CrowdGroupDrawGizmos.Mode DrawMode
        {
            get
            {
                if (EditorPrefs.HasKey("ProjectDawn.Navigation.Hybrid.Editor.CrowdGroupEditor.DrawMode"))
                    return (CrowdGroupDrawGizmos.Mode) EditorPrefs.GetInt("ProjectDawn.Navigation.Hybrid.Editor.CrowdGroupEditor.DrawMode");
                return CrowdGroupDrawGizmos.Mode.Potential;
            }

            set
            {
                EditorPrefs.SetInt("ProjectDawn.Navigation.Hybrid.Editor.CrowdGroupEditor.DrawMode", (int)value);
            }
        }

        CrowdGroupAuthoring Group => target as CrowdGroupAuthoring;

        void OnEnable()
        {
            m_Surface = serializedObject.FindProperty("m_Surface");
            m_Speed = serializedObject.FindProperty("m_Speed");
            m_CostWeights = serializedObject.FindProperty("m_CostWeights");
            m_GoalSource = serializedObject.FindProperty("m_GoalSource");
            m_Grounded = serializedObject.FindProperty("m_Grounded");
            m_MappingRadius = serializedObject.FindProperty("m_MappingRadius");

            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                var manager = world.EntityManager;
                foreach (var target in targets)
                {
                    var auth = target as CrowdGroupAuthoring;
                    if (!manager.HasComponent<CrowdGroupDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.AddComponentData(auth.GetOrCreateEntity(), new CrowdGroupDrawGizmos { Value = DrawMode });
                    }
                }
            }
        }

        void OnDisable()
        {
            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                var manager = world.EntityManager;
                foreach (var target in targets)
                {
                    var auth = target as CrowdGroupAuthoring;
                    if (manager.HasComponent<CrowdGroupDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.RemoveComponent<CrowdGroupDrawGizmos>(auth.GetOrCreateEntity());
                    }
                }
            }
        }

        void UpdateMode()
        {
            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                var manager = world.EntityManager;
                foreach (var target in targets)
                {
                    var auth = target as CrowdGroupAuthoring;
                    if (manager.HasComponent<CrowdGroupDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.SetComponentData(auth.GetOrCreateEntity(), new CrowdGroupDrawGizmos { Value = DrawMode });
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(m_Surface);
            }

            EditorGUILayout.PropertyField(m_Speed);
            EditorGUILayout.PropertyField(m_CostWeights);
            EditorGUILayout.PropertyField(m_GoalSource);
            EditorGUILayout.PropertyField(m_Grounded);
            EditorGUILayout.PropertyField(m_MappingRadius);

            if (serializedObject.ApplyModifiedProperties())
            {
                // Update entities
                foreach (var target in targets)
                {
                    var authoring = target as CrowdGroupAuthoring;
                    if (authoring.HasEntityGroup)
                        authoring.EntityGroup = authoring.DefaultGroup;
                }
            }

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                var drawMode = (CrowdGroupDrawGizmos.Mode) EditorGUILayout.EnumPopup("Gizmos", DrawMode);
                if (EditorGUI.EndChangeCheck())
                {
                    DrawMode = drawMode;
                    UpdateMode();
                }
            }
        }

        [MenuItem("GameObject/AI/Crowd Group", false, 1001)]
        static void CreateCrowdGroup(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("Crowd Group", parent);
            go.AddComponent<CrowdGroupAuthoring>();
        }
    }
}
