using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool block;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;
		private bool _jumpPressedThisFrame;
		private bool _rollPressedThisFrame;
		private bool _previousRollPressed;



#if ENABLE_INPUT_SYSTEM
		

		
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
			if (value.isPressed)
			{
				_jumpPressedThisFrame = true;
			}
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
#endif

	private void Awake()
	{
		SetCursorState(cursorLocked);
		Cursor.visible = false;
	}

		private void Update()
		{
#if ENABLE_INPUT_SYSTEM
			move = ReadMoveInput();
			look = ReadLookInput();
			sprint = ReadSprintInput();
			jump = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
			block = Mouse.current != null && Mouse.current.rightButton.isPressed;

			if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
			{
				_jumpPressedThisFrame = true;
			}

			bool rollPressed = Keyboard.current != null &&
				(Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
			if (rollPressed && !_previousRollPressed)
			{
				_rollPressedThisFrame = true;
			}

			_previousRollPressed = rollPressed;
#else
			move = new Vector2(
				(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
				(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f),
				(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
				(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f));
			move = Vector2.ClampMagnitude(move, 1f);
			look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
			sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			jump = Input.GetKey(KeyCode.Space);
			block = Input.GetMouseButton(1);
			if (Input.GetKeyDown(KeyCode.Space))
			{
				_jumpPressedThisFrame = true;
			}

			if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
			{
				_rollPressedThisFrame = true;
			}
#endif
		}

#if ENABLE_INPUT_SYSTEM
		private Vector2 ReadMoveInput()
		{
			Vector2 keyboardMove = Vector2.zero;
			if (Keyboard.current != null)
			{
				keyboardMove.x =
					(Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) -
					(Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
				keyboardMove.y =
					(Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f) -
					(Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f);
			}

			if (keyboardMove.sqrMagnitude > 0f)
			{
				return Vector2.ClampMagnitude(keyboardMove, 1f);
			}

			if (Gamepad.current != null)
			{
				return Vector2.ClampMagnitude(Gamepad.current.leftStick.ReadValue(), 1f);
			}

			return Vector2.zero;
		}

		private bool ReadSprintInput()
		{
			bool keyboardSprint = Keyboard.current != null &&
				(Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
			bool gamepadSprint = Gamepad.current != null &&
				(Gamepad.current.leftTrigger.ReadValue() > 0.5f || Gamepad.current.rightShoulder.isPressed);
			return keyboardSprint || gamepadSprint;
		}

		private Vector2 ReadLookInput()
		{
			if (!cursorInputForLook)
			{
				return Vector2.zero;
			}

			Vector2 mouseLook = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
			if (mouseLook.sqrMagnitude > 0f)
			{
				return mouseLook;
			}

			if (Gamepad.current != null)
			{
				return Gamepad.current.rightStick.ReadValue();
			}

			return Vector2.zero;
		}

#endif

		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public bool ConsumeJumpPressedThisFrame()
		{
			bool result = _jumpPressedThisFrame;
			_jumpPressedThisFrame = false;
			return result;
		}

		public bool ConsumeRollPressedThisFrame()
		{
			bool result = _rollPressedThisFrame;
			_rollPressedThisFrame = false;
			return result;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;  
			

		}
	}
	
}
