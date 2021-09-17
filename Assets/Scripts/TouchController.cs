using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;


public class TouchController : MonoBehaviour {

	// PUBLIC
	public delegate void TouchDelegate(Vector2 value);
	public event TouchDelegate TouchEvent;

	public delegate void TouchStateDelegate(bool touchPresent);
	public event TouchStateDelegate TouchStateEvent;

	// PRIVATE
	[SerializeField]
	private RectTransform joystickArea;
	private bool touchPresent = false;
	private Vector2 movementVector;

	private PlayerAction _input;


	public Vector2 GetTouchPosition
	{
		get { return movementVector;}
	}

    private void Awake()
    {
		_input = new PlayerAction();

	}
    private void OnEnable()
    {
		_input.Enable();
	}

    private void OnDisable()
	{
		_input.Disable();
	}

    private void Start()
    {
		_input.TouchScreen.TouchInput.started += ctx => StartTouch(ctx);
		_input.TouchScreen.TouchInput.canceled += ctx => EndTouch(ctx);
	}

	private void StartTouch(InputAction.CallbackContext ctx)
	{
		Debug.Log(Vector2.Distance(_input.TouchScreen.TouchPosition.ReadValue<Vector2>(), Camera.main.WorldToScreenPoint(joystickArea.position)));
		if (Vector2.Distance(_input.TouchScreen.TouchPosition.ReadValue<Vector2>(), Camera.main.WorldToScreenPoint(joystickArea.position)) < 80f)
        {
			touchPresent = true;
			if (TouchStateEvent != null)
				TouchStateEvent(touchPresent);
		}
	}

	private void EndTouch(InputAction.CallbackContext ctx)
	{
		touchPresent = false;
		movementVector = joystickArea.anchoredPosition = Vector2.zero;

		if (TouchStateEvent != null)
			TouchStateEvent(touchPresent);
	}

    public void BeginDrag()
	{
		touchPresent = true;
		if(TouchStateEvent != null)
			TouchStateEvent(touchPresent);
	}

	public void EndDrag()
	{
		touchPresent = false;
		movementVector = joystickArea.anchoredPosition = Vector2.zero;

		if(TouchStateEvent != null)
			TouchStateEvent(touchPresent);

	}

	public void OnValueChanged(Vector2 value)
	{
		if(touchPresent)
		{
			// convert the value between 1 0 to -1 +1
			movementVector.x = ((1 - value.x) - 0.5f) * 2f;
			movementVector.y = ((1 - value.y) - 0.5f) * 2f;

			if(TouchEvent != null)
			{
				TouchEvent(movementVector);
			}
		}

	}

}
