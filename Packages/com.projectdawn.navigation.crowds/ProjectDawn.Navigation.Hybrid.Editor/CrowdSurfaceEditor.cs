using UnityEngine;
using UnityEditor;
using Unity.Entities;
using ProjectDawn.Navigation;
using ProjectDawn.ContinuumCrowds;
using System;
using Unity.Mathematics;
using ProjectDawn.Navigation.Editor;
using Unity.AI.Navigation.Editor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CrowdSurfaceAuthoring))]
    class CrowdSurfaceEditor : UnityEditor.Editor
    {
        SerializedProperty m_Size;
        SerializedProperty m_Width;
        SerializedProperty m_Height;
        SerializedProperty m_Density;
        SerializedProperty m_Slope;
        SerializedProperty m_Layers;
        SerializedProperty m_Data;

        UnityEditor.Editor m_DataEditor;

        CrowdSurfaceDrawGizmos.Mode DrawMode
        {
            get
            {
                if (EditorPrefs.HasKey("ProjectDawn.Navigation.Hybrid.Editor.CrowdSurfaceEditor.DrawMode"))
                    return (CrowdSurfaceDrawGizmos.Mode) EditorPrefs.GetInt("ProjectDawn.Navigation.Hybrid.Editor.CrowdSurfaceEditor.DrawMode");
                return CrowdSurfaceDrawGizmos.Mode.Density;
            }

            set
            {
                EditorPrefs.SetInt("ProjectDawn.Navigation.Hybrid.Editor.CrowdSurfaceEditor.DrawMode", (int)value);
            }
        }

        CrowdSurfaceAuthoring Surface => target as CrowdSurfaceAuthoring;

        void OnEnable()
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                m_Size = serializedObject.FindProperty("m_Size");
                m_Width = serializedObject.FindProperty("m_Width");
                m_Height = serializedObject.FindProperty("m_Height");
                m_Density = serializedObject.FindProperty("m_Density");
                m_Slope = serializedObject.FindProperty("m_Slope");
                m_Layers = serializedObject.FindProperty("m_Layers");
                m_Data = serializedObject.FindProperty("m_Data");
            }

            UpdateDataEditor();

            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                var manager = world.EntityManager;
                foreach (var target in targets)
                {
                    var auth = target as CrowdSurfaceAuthoring;
                    if (!manager.HasComponent<CrowdSurfaceDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.AddComponentData(auth.GetOrCreateEntity(), new CrowdSurfaceDrawGizmos { Value = DrawMode });
                    }
                }
            }
        }

        void OnDisable()
        {
            if (m_DataEditor != null)
                GameObject.DestroyImmediate(m_DataEditor);

            if (Application.isPlaying)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null)
                    return;
                var manager = world.EntityManager;
                foreach (var target in targets)
                {
                    var auth = target as CrowdSurfaceAuthoring;
                    if (manager.HasComponent<CrowdSurfaceDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.RemoveComponent<CrowdSurfaceDrawGizmos>(auth.GetOrCreateEntity());
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
                    var auth = target as CrowdSurfaceAuthoring;
                    if (manager.HasComponent<CrowdSurfaceDrawGizmos>(auth.GetOrCreateEntity()))
                    {
                        manager.SetComponentData(auth.GetOrCreateEntity(), new CrowdSurfaceDrawGizmos { Value = DrawMode });
                    }
                }
            }
        }

        void UpdateDataEditor()
        {
            if (m_DataEditor != null)
                GameObject.DestroyImmediate(m_DataEditor);
            if (m_Data.objectReferenceValue != null)
                m_DataEditor = CreateEditor(m_Data.objectReferenceValue);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(m_Size);
                EditorGUILayout.PropertyField(m_Width);
                EditorGUILayout.PropertyField(m_Height);
                EditorGUILayout.PropertyField(m_Density);
                EditorGUILayout.PropertyField(m_Slope);
                EditorGUILayout.PropertyField(m_Layers);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_Data);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateDataEditor();
                }

                using (new EditorGUI.DisabledGroupScope(Surface.Data == null))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);

                    if (GUILayout.Button("Bake"))
                    {
                        Surface.Data.BuildHeightFieldFromCollidersWithRadius(Surface.Width, Surface.Height, Surface.Transform);
                        EditorUtility.SetDirty(Surface.Data);
                    }

                    GUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                var drawMode = (CrowdSurfaceDrawGizmos.Mode) EditorGUILayout.EnumPopup("Gizmos", DrawMode);
                if (EditorGUI.EndChangeCheck())
                {
                    DrawMode = drawMode;
                    UpdateMode();
                }
            }

            // Check, if crowd date is compatible with this component
            if (Surface.Data && !Surface.IsDataValid())
                EditorGUILayout.HelpBox("Data is not valid, please rebuild!", MessageType.Error);

            if (m_DataEditor != null)
            {
                m_DataEditor.DrawHeader();
                m_DataEditor.OnInspectorGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected, typeof(CrowdSurfaceAuthoring))]
        static void DrawGizmos(CrowdSurfaceAuthoring surface, GizmoType gizmoType)
        {
            bool selected = gizmoType.HasFlag(GizmoType.Selected);
            if (surface.IsDataValid())
            {
                Color solidColor = surface.Data.GizmosColor;
                Color wireColor = surface.Data.GizmosColor * 0.4f;

                solidColor.a = selected ? 0.5f : 0.35f;
                wireColor.a = 1;

                var heightField = surface.Data.HeightField.ToArray();
                var obstacleField = surface.Data.ObstacleField.ToArray();
                GizmosField.DrawDepth(heightField, obstacleField, surface.Data.Width, surface.Data.Height, surface.Transform.ToMatrix());
                GizmosField.DrawSolid(heightField, obstacleField, surface.Data.Width, surface.Data.Height, surface.Transform.ToMatrix(), solidColor);
                GizmosField.DrawWire(heightField, obstacleField, surface.Data.Width, surface.Data.Height, surface.Transform.ToMatrix(), wireColor);
            }
            if (selected)
            {
                var size = new int2(surface.Width, surface.Height);

                float3 a = new float3(0, 0, 0);
                float3 b = new float3(size.x, 0, 0);
                float3 c = new float3(size.x, size.y, 0);
                float3 d = new float3(0, size.y, 0);

                var transform = surface.Transform;

                a = transform.TransformPoint(a);
                b = transform.TransformPoint(b);
                c = transform.TransformPoint(c);
                d = transform.TransformPoint(d);

                Handles.color = Color.white;
                Handles.DrawLine(a, b);
                Handles.DrawLine(b, c);
                Handles.DrawLine(c, d);
                Handles.DrawLine(d, a);
                DrawArrow(a, math.conjugate(transform.Rotation), math.min(surface.CellSize.x, surface.CellSize.y));
                DrawArrow(b, math.conjugate(transform.Rotation), math.min(surface.CellSize.x, surface.CellSize.y));
                DrawArrow(c, math.conjugate(transform.Rotation), math.min(surface.CellSize.x, surface.CellSize.y));
                DrawArrow(d, math.conjugate(transform.Rotation), math.min(surface.CellSize.x, surface.CellSize.y));
            }
        }

        static void DrawArrow(float3 position, quaternion rotation, float size)
        {
            Handles.ArrowHandleCap(0, position, rotation, size, EventType.Repaint);
        }

        [MenuItem("GameObject/AI/Crowd Surface", false, 1000)]
        static void CreateCrowdSurface(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("Crowd Surface", parent);
            go.AddComponent<CrowdSurfaceAuthoring>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }
    }
}
