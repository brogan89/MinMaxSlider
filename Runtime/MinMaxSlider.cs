using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class MinMaxSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	private enum DragState { Both, Min, Max }

	[SerializeField] private RectTransform sliderBounds = null;
	[SerializeField] private RectTransform minHandle = null;
	[SerializeField] private RectTransform maxHandle = null;
	[SerializeField] private RectTransform middleGraphic = null;

	// text components (optional)
	[SerializeField] private TextMeshProUGUI minText = null;
	[SerializeField] private TextMeshProUGUI maxText = null;

	// values
	[SerializeField] private float minLimit = 0;
	[SerializeField] private float maxLimit = 100;

	public bool wholeNumbers;
	[SerializeField] private float minValue = 25;
	[SerializeField] private float maxValue = 75;

	public MinMaxValues Values => new MinMaxValues(minValue, maxValue, minLimit, maxLimit);

	/// <summary>
	/// Event invoked when either slider value has changed
	/// <para></para>
	/// T0 = min, T1 = max
	/// </summary>
	[Serializable]
	public class SliderEvent : UnityEvent<float, float> { }
	public SliderEvent onValueChanged = new SliderEvent();

	private Vector3 dragStartPosition;
	private float dragStartMinValue01;
	private float dragStartMaxValue01;
	private DragState dragState;
	private readonly Vector3[] worldCorners = new Vector3[4];
	private bool passDragEvents; // this allows drag events to be passed through to scrollers

	private Camera mainCamera;
	private Canvas parentCanvas;
	private bool isOverlayCanvas; 

	private void Start()
	{
		if (!sliderBounds) sliderBounds = transform as RectTransform;

		parentCanvas = GetComponentInParent<Canvas>();
		isOverlayCanvas = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
		mainCamera = Camera.main;
	}

	public void SetLimits(float minLimit, float maxLimit)
	{
		this.minLimit = wholeNumbers ? Mathf.RoundToInt(minLimit) : minLimit;
		this.maxLimit = wholeNumbers ? Mathf.RoundToInt(maxLimit) : maxLimit;
	}

	public void SetValues(MinMaxValues values)
	{
		SetValues(values.minValue, values.maxValue, values.minLimit, values.maxLimit);
	}

	public void SetValues(float minValue, float maxValue)
	{
		SetValues(minValue, maxValue, this.minLimit, this.maxLimit);
	}

	public void SetValues(float minValue, float maxValue, float minLimit, float maxLimit)
	{
		this.minValue = wholeNumbers ? Mathf.RoundToInt(minValue) : minValue;
		this.maxValue = wholeNumbers ? Mathf.RoundToInt(maxValue) : maxValue;
		SetLimits(minLimit, maxLimit);

		RefreshSliders();
		UpdateText();
		UpdateMiddleGraphic();

		// event
		onValueChanged.Invoke(this.minValue, this.maxValue);
	}

	private void RefreshSliders()
	{
		SetSliderAnchors();

		float clampedMin = Mathf.Clamp(minValue, minLimit, maxLimit);
		SetHandleValue01(minHandle, GetPercentage(minLimit, maxLimit, clampedMin));

		float clampedMax = Mathf.Clamp(maxValue, minLimit, maxLimit);
		SetHandleValue01(maxHandle, GetPercentage(minLimit, maxLimit, clampedMax));
	}

	private void SetSliderAnchors()
	{
		minHandle.anchorMin = new Vector2(0, 0.5f);
		minHandle.anchorMax = new Vector2(0, 0.5f);
		minHandle.pivot = new Vector2(0.5f, 0.5f);

		maxHandle.anchorMin = new Vector2(1, 0.5f);
		maxHandle.anchorMax = new Vector2(1, 0.5f);
		maxHandle.pivot = new Vector2(0.5f, 0.5f);
	}

	private void UpdateText()
	{
		minText?.SetText(Mathf.RoundToInt(minValue).ToString());
		maxText?.SetText(Mathf.RoundToInt(maxValue).ToString());
	}

	private void UpdateMiddleGraphic()
	{
		if (middleGraphic)
		{
			middleGraphic.anchorMin = Vector2.zero;
			middleGraphic.anchorMax = Vector2.one;
			middleGraphic.offsetMin = new Vector2(minHandle.anchoredPosition.x, 0);
			middleGraphic.offsetMax = new Vector2(maxHandle.anchoredPosition.x, 0);
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		var clickPosition = isOverlayCanvas
			? (Vector3) eventData.position
			: mainCamera.ScreenToWorldPoint(eventData.position);

		passDragEvents = Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y);

		if (passDragEvents)
		{
			PassDragEvents<IBeginDragHandler>(x => x.OnBeginDrag(eventData));
		}
		else
		{
			dragStartPosition = clickPosition;
			dragStartMinValue01 = GetValue01(minHandle.position.x);
			dragStartMaxValue01 = GetValue01(maxHandle.position.x);

			// set drag state
			if (dragStartPosition.x < minHandle.position.x || IsWithinRect(minHandle, dragStartPosition))
			{
				dragState = DragState.Min;
				minHandle.SetAsLastSibling();
			}
			else if (dragStartPosition.x > maxHandle.position.x || IsWithinRect(maxHandle, dragStartPosition))
			{
				dragState = DragState.Max;
				maxHandle.SetAsLastSibling();
			}
			else
				dragState = DragState.Both;
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		var clickPosition = isOverlayCanvas
			? (Vector3) eventData.position
			: mainCamera.ScreenToWorldPoint(eventData.position);

		if (passDragEvents)
		{
			PassDragEvents<IDragHandler>(x => x.OnDrag(eventData));
		}
		else if (minHandle && maxHandle)
		{
			SetSliderAnchors();

			if (dragState == DragState.Min || dragState == DragState.Max)
			{
				float dragPosition01 = GetValue01(clickPosition.x);
				float minHandleValue = GetValue01(minHandle.position.x);
				float maxHandleValue = GetValue01(maxHandle.position.x);

				if (dragState == DragState.Min)
					SetHandleValue01(minHandle, Mathf.Clamp(dragPosition01, 0, maxHandleValue));
				else if (dragState == DragState.Max)
					SetHandleValue01(maxHandle, Mathf.Clamp(dragPosition01, minHandleValue, 1));
			}
			else
			{
				var sliderBoundsRect = sliderBounds.rect;
				var rectStart = sliderBoundsRect.position;
				var rectEnd = rectStart;
				rectEnd.x += sliderBoundsRect.width;

				var worldWidth = isOverlayCanvas ? sliderBoundsRect.width : mainCamera.ScreenToWorldPoint(rectEnd).x - mainCamera.ScreenToWorldPoint(rectStart).x;
				
				float distancePercent = (clickPosition.x - dragStartPosition.x) / worldWidth;
				SetHandleValue01(minHandle, dragStartMinValue01 + distancePercent);
				SetHandleValue01(maxHandle, dragStartMaxValue01 + distancePercent);

			}

			// set values
			float min = Mathf.Lerp(minLimit, maxLimit, GetValue01(minHandle.position.x));
			float max = Mathf.Lerp(minLimit, maxLimit, GetValue01(maxHandle.position.x));
			SetValues(min, max);

			UpdateText();
			UpdateMiddleGraphic();
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (passDragEvents)
		{
			PassDragEvents<IEndDragHandler>(x => x.OnEndDrag(eventData));
		}
		else
		{
			float minHandleValue = GetValue01(minHandle.position.x);
			float maxHandleValue = GetValue01(maxHandle.position.x);

			// this safe guards a possible situation where the slides can get stuck
			if (minHandleValue == 0 && maxHandleValue == 0)
				maxHandle.SetAsLastSibling();
			else if (minHandleValue == 1 && maxHandleValue == 1)
				minHandle.SetAsLastSibling();
		}
	}

	private void PassDragEvents<T>(Action<T> callback) where T : IEventSystemHandler
	{
		Transform parent = transform.parent;
		while (parent != null)
		{
			foreach (var component in parent.GetComponents<Component>())
			{
				if (component is T)
				{
					callback.Invoke((T)(IEventSystemHandler)component);
					return;
				}
			}

			parent = parent.parent;
		}
	}

	/// <summary>
	/// Generates rectTransforms world corners
	/// </summary>
	private void GetWorldCorners()
	{
		sliderBounds.GetWorldCorners(worldCorners);
	}

	/// <summary>
	/// Sets handles positon 
	/// </summary>
	/// <param name="handle"></param>
	/// <param name="value01"></param>
	private void SetHandleValue01(RectTransform handle, float value01)
	{
		GetWorldCorners();
		Vector2 pos = new Vector2(
			Mathf.Lerp(worldCorners[0].x, worldCorners[2].x, value01),
			worldCorners[0].y + (worldCorners[1].y - worldCorners[0].y) / 2f);

		handle.position = pos;
	}

	/// <summary>
	/// Returns a values from 0-1 based on this rects world corners
	/// </summary>
	/// <param name="worldPosition"></param>
	/// <returns></returns>
	private float GetValue01(float worldPositionX)
	{
		GetWorldCorners();
		float posX = Mathf.Clamp(worldPositionX, worldCorners[0].x, worldCorners[2].x);
		return GetPercentage(worldCorners[0].x, worldCorners[2].x, posX);
	}

	/// <summary>
	/// Returns percentage of input based on min and max values
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="input"></param>
	/// <returns></returns>
	private float GetPercentage(float min, float max, float input)
	{
		return (input - min) / (max - min);
	}

	private bool IsWithinRect(RectTransform rect, Vector2 worldPosition)
	{
		Vector3[] corners = new Vector3[4];
		rect.GetWorldCorners(corners);
		return worldPosition.x > corners[0].x && worldPosition.x < corners[2].x;
	}

	[Serializable]
	public struct MinMaxValues
	{
		public float minValue, maxValue, minLimit, maxLimit;
		public static MinMaxValues DEFUALT = new MinMaxValues(25, 75, 0, 100);

		public MinMaxValues(float minValue, float maxValue, float minLimit, float maxLimit)
		{
			this.minValue = minValue;
			this.maxValue = maxValue;
			this.minLimit = minLimit;
			this.maxLimit = maxLimit;
		}

		public override string ToString()
		{
			return string.Format("Values(min:{0}, max:{1}) | Limits(min:{2}, max:{3})", minValue, maxValue, minLimit, maxLimit);
		}
	}
}