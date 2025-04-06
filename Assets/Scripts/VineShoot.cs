using UnityEngine;
using UnityEngine.InputSystem;

public class VineShoot : MonoBehaviour
{
    public GameObject vineObject;
    public GameObject leaf;



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
            VineGeneration vineGenerator = Instantiate(vineObject, hitInfo.point, Quaternion.LookRotation(hitInfo.normal, Vector3.up)).GetComponent<VineGeneration>();
            vineGenerator.leaf = leaf;
            vineGenerator.Generate();
        }
    }
}
