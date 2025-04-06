using UnityEngine;
using UnityEngine.InputSystem;
public class Movement : MonoBehaviour
{
	InputAction look;
	InputAction move;
	InputAction jump;
	InputAction crouch;
	InputAction sprint;
	Transform cameraTransform;
	Camera mainCamera;
	Rigidbody rb;
	bool sliding = false;
	float timeLeftTillEndOfSlide = 0f;
	Vector3 initialSlideVector;
	bool onGround = true;
	Vector3 cameraStandPosition;
	Vector3 cameraSlidePosition;
	bool sprinting = false;
	public float angleLimit = 80;
	public float speed = 8;
	public float jumpStrength = 250;
	public float lerpSmoothness = 6f;
	public float slideStrength = 200;
	public float slideJumpStrength = 1.2f;
	public float slideDuration = 0.75f;
	public float floorThreshold = 0.70f;
	public float cameraSwitchSpeed = 6f;
	public int baseFieldOfView = 100;
	public float fieldOfViewMod = 1.1f;
	public float slideSpeedMod = 1.2f;
	public float slideFieldOfViewMod = 0.2f;
	public float sprintSpeedMod = 1.2f;
	public float sprintFieldOfViewMod = 0.2f;
	public float fieldOfViewSmoothness = 6f;

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
		sprint = InputSystem.actions.FindAction("Sprint");
		sprint.Enable();

		cameraTransform = transform.GetChild(0);
		cameraTransform.rotation.eulerAngles.Set(0, 0, 0);
		cameraStandPosition = cameraTransform.localPosition;
		cameraSlidePosition = new Vector3(
			cameraTransform.transform.localPosition.x,
			cameraTransform.transform.localPosition.y - 0.5f,
			cameraTransform.transform.localPosition.z
		);

		mainCamera = Camera.main;
		mainCamera.fieldOfView = baseFieldOfView;

		rb = GetComponent<Rigidbody>();
	}

	// Update is called once per frame
	void Update()
	{
		if(look.WasPerformedThisFrame())
		{
			Vector2 mousePositionDelta = look.ReadValue<Vector2>();
			float delta = mousePositionDelta.y;
			if(mousePositionDelta.y > 0)
			{
				// looking up and in the top half
				float distanceToMax = cameraTransform.rotation.eulerAngles.x - (360 - angleLimit);
				if(cameraTransform.rotation.eulerAngles.x <= angleLimit + 1)
				{
					distanceToMax = cameraTransform.rotation.eulerAngles.x + angleLimit;
				}

				if(delta > distanceToMax)
				{
					delta = distanceToMax;
				}
			}
			else if(mousePositionDelta.y < 0)
			{
				// looking down and in the bottom half
				float distanceToMax = angleLimit - cameraTransform.rotation.eulerAngles.x;
				if(cameraTransform.rotation.eulerAngles.x >= 359 - angleLimit)
				{
					distanceToMax = cameraTransform.rotation.eulerAngles.x - (360 - angleLimit) + angleLimit;
				}

				if(-1 * delta > distanceToMax)
				{
					delta = distanceToMax * -1;
				}
			}
			cameraTransform.localRotation *= Quaternion.Euler(-delta, 0, 0);
			transform.rotation *= Quaternion.Euler(0, mousePositionDelta.x, 0);
		}

		// jumping (have jump cancel sliding)
		if(jump.WasPerformedThisFrame() && onGround)
		{
			if(sliding)
			{
				sliding = false;
				timeLeftTillEndOfSlide = 0;
				rb.AddForce(new Vector3(0, jumpStrength * slideJumpStrength, 0));
			}
			else
			{
				rb.AddForce(new Vector3(0, jumpStrength, 0));
			}
		}

		Vector2 movePositionDelta = move.ReadValue<Vector2>();

		// field of view warping
		float fieldOfViewMod = 1 + (sliding ? slideFieldOfViewMod : 0) + (sprinting ? sprintFieldOfViewMod : 0);
		mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, baseFieldOfView * fieldOfViewMod, fieldOfViewSmoothness * Time.deltaTime);

		// sliding
		if(crouch.WasPerformedThisFrame())
		{
			if(!sliding && movePositionDelta.sqrMagnitude != 0)
			{
				sliding = true;
				timeLeftTillEndOfSlide = slideDuration;
				Vector3 xSlideVelocity = transform.forward * movePositionDelta.y * speed * slideSpeedMod;
				Vector3 zSlideVelocity = transform.right * movePositionDelta.x * speed * slideSpeedMod;
				rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0) + xSlideVelocity + zSlideVelocity;
			}
			else if(sliding && onGround)
			{
				sliding = false;
			}
		}
		// restrict movement durring slide
		if(sliding)
		{
			if(onGround)
			{
				timeLeftTillEndOfSlide -= Time.deltaTime;
				if(timeLeftTillEndOfSlide < 0)
				{
					timeLeftTillEndOfSlide = 0;
					sliding = false;
				}
			}
			cameraTransform.transform.localPosition = Vector3.Lerp(cameraTransform.transform.localPosition, cameraSlidePosition, cameraSwitchSpeed * Time.deltaTime);
			return;
		}
		else if(cameraTransform.transform.localPosition != cameraStandPosition) // TODO: snap to position when close enough beauces it will never get there
		{
			cameraTransform.transform.localPosition = Vector3.Lerp(cameraTransform.transform.localPosition, cameraStandPosition, cameraSwitchSpeed * Time.deltaTime);
		}

		// sprint check
		sprinting = sprint.IsPressed() && !sliding && movePositionDelta.sqrMagnitude != 0;
		// moving
		Vector3 xVelocity = transform.forward * movePositionDelta.y * speed * (sprinting ? sprintSpeedMod : 1);
		Vector3 zVelocity = transform.right * movePositionDelta.x * speed * (sprinting ? sprintSpeedMod : 1);
		Vector3 destinationVector = new Vector3(0, rb.linearVelocity.y, 0) + xVelocity + zVelocity;
		float lerpValue = lerpSmoothness * Time.deltaTime * (!onGround ? 0.1f : 1);
		rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, destinationVector, lerpValue);
	}

	void OnCollisionExit(Collision collision)
	{
		foreach(ContactPoint contact in collision.contacts)
		{
			if(contact.normal.y >= floorThreshold)
			{
				return;
			}
		}
		onGround = false;
	}

	void OnCollisionStay(Collision collision)
	{
		foreach(ContactPoint contact in collision.contacts)
		{
			if(contact.normal.y >= floorThreshold)
			{
				onGround = true;
				return;
			}
		}
		onGround = false;
	}
}