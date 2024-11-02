using UnityEngine;
using UnityEditor;
using ProjectDawn.ContinuumCrowds;
using Unity.Mathematics;

namespace ProjectDawn.Navigation.Editor
{
    [CustomPropertyDrawer(typeof(CostWeights))]
    public class CostWeightDrawer : PropertyDrawer
    {
        const float LineHeight = 20f;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var normalize = property.FindPropertyRelative("Normalize");
            if (!normalize.boolValue)
            {
                EditorGUI.PropertyField(position, property, true);
                return;
            }

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, LineHeight), property, false);
            position.y += LineHeight;

            if (property.isExpanded)
            {
                var distance = property.FindPropertyRelative("Distance");
                var time = property.FindPropertyRelative("Time");
                var discomfort = property.FindPropertyRelative("Discomfort");

                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUI.Slider(new Rect(position.x, position.y, position.width, LineHeight), distance, 0, 1);
                position.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    var nornmalized = Normalize(new float3(distance.floatValue, time.floatValue, discomfort.floatValue));
                    distance.floatValue = nornmalized.x;
                    time.floatValue = nornmalized.y;
                    discomfort.floatValue = nornmalized.z;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.Slider(new Rect(position.x, position.y, position.width, LineHeight), time, 0, 1);
                position.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    var nornmalized = Normalize(new float3(time.floatValue, distance.floatValue, discomfort.floatValue));
                    time.floatValue = nornmalized.x;
                    distance.floatValue = nornmalized.y;
                    discomfort.floatValue = nornmalized.z;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.Slider(new Rect(position.x, position.y, position.width, LineHeight), discomfort, 0, 1);
                position.y += LineHeight;
                if (EditorGUI.EndChangeCheck())
                {
                    var nornmalized = Normalize(new float3(discomfort.floatValue, time.floatValue, distance.floatValue));
                    discomfort.floatValue = nornmalized.x;
                    time.floatValue = nornmalized.y;
                    distance.floatValue = nornmalized.z;
                }

                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, LineHeight), property.FindPropertyRelative("Normalize"));
                EditorGUI.indentLevel--;
            }
        }

        static float3 Normalize(float3 value)
        {
            value.x = math.saturate(value.x);

            float divider = 1f - value.x;

            if (divider == 0)
            {
                value.y = 0;
                value.z = 0;
                return value;
            }

            float sum = (value.y + value.z) / divider;
            value.y = math.saturate(value.y / sum);
            value.z = math.saturate(value.z / sum);
            return value;
        }
    }
}
