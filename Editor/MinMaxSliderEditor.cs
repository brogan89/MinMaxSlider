using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MinMaxSlider))]
[CanEditMultipleObjects]
public class MinMaxSliderEditor : Editor
{
	private SerializedProperty _sliderBounds;
	private SerializedProperty _minHandle;
	private SerializedProperty _maxHandle;
	private SerializedProperty _minText;
	private SerializedProperty _maxText;
	private SerializedProperty _middleGraphic;
	private SerializedProperty _minLimit;
	private SerializedProperty _maxLimit;
	private SerializedProperty _wholeNumbers;
	private SerializedProperty _minValue;
	private SerializedProperty _maxValue;

	private SerializedProperty _onValueChanged;

	private readonly GUIContent label = new GUIContent("Min Max Values");

	private void OnEnable()
	{
		_sliderBounds = serializedObject.FindProperty("sliderBounds");
		_minHandle = serializedObject.FindProperty("minHandle");
		_maxHandle = serializedObject.FindProperty("maxHandle");
		_minText = serializedObject.FindProperty("minText");
		_maxText = serializedObject.FindProperty("maxText");
		_middleGraphic = serializedObject.FindProperty("middleGraphic");
		_minLimit = serializedObject.FindProperty("minLimit");
		_maxLimit = serializedObject.FindProperty("maxLimit");
		_wholeNumbers = serializedObject.FindProperty("wholeNumbers");
		_minValue = serializedObject.FindProperty("minValue");
		_maxValue = serializedObject.FindProperty("maxValue");
		_onValueChanged = serializedObject.FindProperty("onValueChanged");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		float minLimitOld = _minLimit.floatValue;
		float maxLimitOld = _maxLimit.floatValue;
		float minValueOld = _minValue.floatValue;
		float maxValueOld = _maxValue.floatValue;

		Label("UI Controls");
		EditorGUILayout.PropertyField(_sliderBounds);
		EditorGUILayout.PropertyField(_minHandle);
		EditorGUILayout.PropertyField(_maxHandle);
		EditorGUILayout.PropertyField(_middleGraphic);

		Label("Display Text (Optional)");
		EditorGUILayout.PropertyField(_minText);
		EditorGUILayout.PropertyField(_maxText);

		Label("Limits");
		EditorGUILayout.PropertyField(_minLimit);
		EditorGUILayout.PropertyField(_maxLimit);

		Label("Values");
		EditorGUILayout.PropertyField(_wholeNumbers);
		EditorGUILayout.PropertyField(_minValue);
		EditorGUILayout.PropertyField(_maxValue);

		float minValue = Mathf.Clamp(_minValue.floatValue, _minLimit.floatValue, _maxLimit.floatValue);
		float maxValue = Mathf.Clamp(_maxValue.floatValue, _minLimit.floatValue, _maxLimit.floatValue);
		EditorGUILayout.MinMaxSlider(label, ref minValue, ref maxValue, _minLimit.floatValue, _maxLimit.floatValue);

		bool anyValueChanged = minValueOld != minValue
							|| maxValueOld != maxValue
							|| minLimitOld != _minLimit.floatValue
							|| maxLimitOld != _maxLimit.floatValue;

		if (anyValueChanged)
		{
			MinMaxSlider slider = target as MinMaxSlider;
			// force limits to ints if whole numbers.
			// needed to do this here because it wouldn't set in component script for some reason
			if (slider.wholeNumbers)
			{
				_minLimit.floatValue = Mathf.RoundToInt(_minLimit.floatValue);
				_maxLimit.floatValue = Mathf.RoundToInt(_maxLimit.floatValue);
			}

			// set slider values
			slider.SetValues(minValue, maxValue, _minLimit.floatValue, _maxLimit.floatValue);
		}

		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(_onValueChanged);

		serializedObject.ApplyModifiedProperties();
	}

	private void Label(string label)
	{
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
	}
}