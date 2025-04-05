using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;

public class Movement : MonoBehaviour
{
	InputAction look;
	InputAction move;
	InputAction jump;
	Transform camera;
	public float angleLimit = 80;
	public float speed = 10;
	public float jumpStrength = 10;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		look = InputSystem.actions.FindAction("Look");
		move = InputSystem.actions.FindAction("Move");
		jump = InputSystem.actions.FindAction("Jump");
		look.Enable();
		move.Enable();
		jump.Enable();

		camera = transform.GetChild(0);

		camera.rotation.eulerAngles.Set(0, 0, 0);
	}

	// Update is called once per frame
	void Update()
	{
		if (look.WasPerformedThisFrame())
		{
			Vector2 mousePositionDelta = look.ReadValue<Vector2>();
			float delta = mousePositionDelta.y;
			if (mousePositionDelta.y > 0)
			{
				// looking up and in the top half
				float distanceToMax = camera.rotation.eulerAngles.x - (360 - angleLimit);
				if (camera.rotation.eulerAngles.x <= angleLimit + 1)
				{
					distanceToMax = camera.rotation.eulerAngles.x + angleLimit;
				}

				if (delta > distanceToMax)
				{
					delta = distanceToMax;
				}
			}
			else if (mousePositionDelta.y < 0)
			{
				// looking down and in the bottom half
				float distanceToMax = angleLimit - camera.rotation.eulerAngles.x;
				if (camera.rotation.eulerAngles.x >= 359 - angleLimit)
				{
					distanceToMax = camera.rotation.eulerAngles.x - (360 - angleLimit) + angleLimit;
				}

				if (-1 * delta > distanceToMax)
				{
					delta = distanceToMax * -1;
				}
			}
			camera.localRotation *= Quaternion.Euler(-delta, 0, 0);
			transform.rotation *= Quaternion.Euler(0, mousePositionDelta.x, 0);
		}

		// moving
		Vector2 movePositionDelta = move.ReadValue<Vector2>();
		print(movePositionDelta);
		transform.position += transform.forward * movePositionDelta.y * Time.deltaTime * speed;
		transform.position += transform.right * movePositionDelta.x * Time.deltaTime * speed;

		// jumping
		if (jump.WasPerformedThisFrame())
		{
			// transform.position += transform.up * jumpStrength;
			Rigidbody.
		}
	}
}
