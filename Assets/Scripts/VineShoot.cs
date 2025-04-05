using UnityEngine;

public class VineShoot : MonoBehaviour
{
    public GameObject vineObject;


    void Start()
    {
        Shoot(transform.position, transform.forward);
    }

    void Update()
    {
        
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
