using UnityEngine;
using UnityEditor;
using Positron;

[CustomPropertyDrawer(typeof(PitchYawBehaviour))]
public class PitchYawDrawer : PropertyDrawer
{
    GUIContent m_TweenPitchContent = new GUIContent("Tween Pitch", "This should be true if you want to change the pitch.");
    GUIContent m_TweenYawContent = new GUIContent("Tween Yaw", "This should be true if you want to change the yaw.");
    GUIContent m_TweenTypeContent = new GUIContent("Tween Type", "Linear - moves the same amount each frame (assuming static start and end locations).\n"
        + "Deceleration - moves slower the closer to the end location it is.\n"
        + "Harmonic - moves faster in the middle of its tween.\n"
        + "Custom - uses the customStartingSpeed and customEndingSpeed to create a curve for the desired tween.");
    GUIContent m_StartingSpeedContent = new GUIContent("Starting Speed", "This is used when the Tween Type is set to Custom.  It determines how fast the we will be moving near the Start.");
    GUIContent m_EndingSpeedContent = new GUIContent("Ending Speed", "This is used when the Tween Type is set to Custom.  It determines how fast the transform will be moving near the End.");

    public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
    {
        int fieldCount = property.FindPropertyRelative ("tweenType").enumValueIndex == (int)PitchYawBehaviour.TweenType.Custom ? 5 : 3;
		fieldCount += 4;
        return fieldCount * (EditorGUIUtility.singleLineHeight);
    }

    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty tweenPitchProp = property.FindPropertyRelative ("tweenPitch");
        SerializedProperty tweenYawProp = property.FindPropertyRelative("tweenYaw");
        SerializedProperty tweenTypeProp = property.FindPropertyRelative ("tweenType");
        SerializedProperty startPitchProp = property.FindPropertyRelative ("startPitch");
        SerializedProperty endPitchProp = property.FindPropertyRelative ("endPitch");
        SerializedProperty startYawProp = property.FindPropertyRelative ("startYaw");
        SerializedProperty endYawProp = property.FindPropertyRelative ("endYaw");
        
        Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField (singleFieldRect, tweenPitchProp, m_TweenPitchContent);
        
        singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField (singleFieldRect, tweenYawProp, m_TweenYawContent);

        singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(singleFieldRect, tweenTypeProp, m_TweenTypeContent);

        if (tweenTypeProp.enumValueIndex == (int)PitchYawBehaviour.TweenType.Custom)
        {
            SerializedProperty startingSpeedProp = property.FindPropertyRelative ("customStartingSpeed");
            SerializedProperty endingSpeedProp = property.FindPropertyRelative ("customEndingSpeed");

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.Slider(singleFieldRect, startingSpeedProp, 0f, 1f, m_StartingSpeedContent);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.Slider (singleFieldRect, endingSpeedProp, 0f, 1f, m_EndingSpeedContent);
        }

		singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(singleFieldRect, startPitchProp);

		singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(singleFieldRect, endPitchProp);

		singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(singleFieldRect, startYawProp);

		singleFieldRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(singleFieldRect, endYawProp);
    }
}
