using UnityEngine;
using UnityEngine.InputSystem;

public class VineShoot : MonoBehaviour
{
    public GameObject vineObject;
    InputAction attack;

    void Start()
    {
		attack = InputSystem.actions.FindAction("Attack");
        attack.Enable();
		//Shoot(transform.position, transform.forward);
	}

    void Update()
    {
        if(attack.WasPerformedThisFrame())
        {
			Shoot(transform.position, transform.forward);
		}
	}

    void Shoot(Vector3 pos, Vector3 dir)
    {
		RaycastHit hitInfo;
		if(Physics.Raycast(pos, dir, out hitInfo))
        {
            Instantiate(vineObject, hitInfo.point, Quaternion.LookRotation(hitInfo.normal, Vector3.up));
        }
    }
}
