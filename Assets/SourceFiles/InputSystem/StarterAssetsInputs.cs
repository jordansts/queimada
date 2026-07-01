using UnityEngine;
using UnityEngine.InputSystem;

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

	private void Awake()
	{
		ApplyCursorState();
	}

		private void Update()
		{
			block = Mouse.current != null && Mouse.current.rightButton.isPressed;

			bool rollPressed = Keyboard.current != null &&
				(Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
			if (rollPressed && !_previousRollPressed)
			{
				_rollPressedThisFrame = true;
			}

			_previousRollPressed = rollPressed;
		}

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
			ApplyCursorState();
		}

		public void ApplyCursorState()
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
