using System;
using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : NetworkBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		private void Start()
		{
			if (!isOwned)
			{
				GetComponent<StarterAssetsInputs>().enabled = false;
			}
		}
		
		void FixedUpdate()
		{
			move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
		}

		void Update()
		{
			if (cursorInputForLook)
			{
				look = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
			}
			else
			{
				look = Vector2.zero;
			}
			
			if (Input.GetKeyDown(KeyCode.Tab))
			{
				cursorLocked = !cursorLocked;
				cursorInputForLook = !cursorInputForLook;
				SetCursorState(cursorLocked);
			}
    
			jump = Input.GetKeyDown(KeyCode.Space);
			sprint = Input.GetKey(KeyCode.LeftShift);
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

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}