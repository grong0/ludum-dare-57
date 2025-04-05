using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.Analytics;

public class Movement : MonoBehaviour
{
	InputAction look;
	InputAction move;
	InputAction jump;
	InputAction crouch;
	Transform camera;
	Rigidbody rb;
	bool sliding = false;
	float timeLeftTillEndOfSlide = 0f;
	public float angleLimit = 80;
	public float speed = 8;
	public float jumpStrength = 200;
	public float lerpSmoothness = 6f;
	public float slideStrength = 200;
	public float slideDuration = 1f;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		look = InputSystem.actions.FindAction("Look");
		look.Enable();
		move = InputSystem.actions.FindAction("Move");
		move.Enable();
		jump = InputSystem.actions.FindAction("Jump");
		jump.Enable();
		crouch = InputSystem.actions.FindAction("Crouch");
		crouch.Enable();

		camera = transform.GetChild(0);
		camera.rotation.eulerAngles.Set(0, 0, 0);

		rb = GetComponent<Rigidbody>();
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

		// restrict movement durring slide
		if (sliding)
		{
			timeLeftTillEndOfSlide -= Time.deltaTime;
			if (timeLeftTillEndOfSlide < 0)
			{
				timeLeftTillEndOfSlide = 0;
				// Stop the slide
			}
			else
			{
				return;
			}
		}

		// moving
		Vector2 movePositionDelta = move.ReadValue<Vector2>();
		Vector3 xVelocity = transform.forward * movePositionDelta.y * speed;
		Vector3 zVelocity = transform.right * movePositionDelta.x * speed;
		Vector3 destinationVector = new Vector3(0, rb.linearVelocity.y, 0) + xVelocity + zVelocity;
		float lerpValue = lerpSmoothness * (movePositionDelta.sqrMagnitude > 0 ? 1 : 1) * Time.deltaTime;
		rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, destinationVector, lerpValue);

		// jumping
		if (jump.WasPerformedThisFrame())
		{
			rb.AddForce(new Vector3(0, jumpStrength, 0));
		}

		// sliding
		if (crouch.WasPerformedThisFrame() && movePositionDelta.sqrMagnitude != 0 && !sliding)
		{
			sliding = true;
			timeLeftTillEndOfSlide = slideDuration;
			// rb.AddForce(rb.linearVelocity * slideStrength);
			// Start the slide
		}
	}
}
