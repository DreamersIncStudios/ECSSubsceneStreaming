using System.Reflection.Emit;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectDawn.Navigation.Editor
{
    [CustomPropertyDrawer(typeof(Range))]
    public class RangeDrawer : PropertyDrawer
    {
        static class Content
        {
            public static readonly GUIContent[] Labels = { new GUIContent("Start"), new GUIContent("End") };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            var iterator = property.FindPropertyRelative("Start");
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiPropertyField(position, Content.Labels, iterator, label);
            if (EditorGUI.EndChangeCheck())
            {
                var startProperty = property.FindPropertyRelative("Start");
                var endProperty = property.FindPropertyRelative("End");
                if (startProperty.floatValue > endProperty.floatValue)
                    endProperty.floatValue = startProperty.floatValue;
            }
            EditorGUI.EndProperty();
        }
    }
}
