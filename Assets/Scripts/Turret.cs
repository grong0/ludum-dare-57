using UnityEngine;

public class Turret : MonoBehaviour
{
	public float projectileSpeed = 10;

	Transform playerTransform;
	Transform headTransform;
	int bulletsInMag;
	float shotTimeLeft = 0f;
	float reloadTimeLeft = 0f;
	public GameObject bullet;
	public int magSize = 3;
	public float rateOfFire = 0.1f;
	public float reloadDuration = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		playerTransform = GameObject.Find("Player").transform;
		headTransform = transform.GetChild(0);
		bulletsInMag = magSize;
    }

    // Update is called once per frame
    void Update()
    {
		Vector3 toPlayer = playerTransform.position - transform.position;
		transform.localRotation = Quaternion.Euler(0, (180 * Mathf.Atan2(toPlayer.x, toPlayer.z) / Mathf.PI) - 90, 0);
		headTransform.transform.localRotation = Quaternion.Euler(0, 0, (180 * Mathf.Atan2(toPlayer.y, Vector3.Distance(headTransform.position, playerTransform.position)) / Mathf.PI) + 90);
		if (reloadTimeLeft > 0)
		{
			reloadTimeLeft -= Time.deltaTime;
			if (reloadTimeLeft <= 0)
			{
				reloadTimeLeft = 0;
				bulletsInMag = magSize;
			}
		}
		else if (shotTimeLeft > 0)
		{
			shotTimeLeft -= Time.deltaTime;
			if (shotTimeLeft <= 0)
			{
				shotTimeLeft = 0;
			}
		}
		else
		{
			print("shooting");
			Instantiate(bullet, headTransform.position, transform.localRotation * headTransform.localRotation);
			bulletsInMag -= 1;
			if (bulletsInMag > 0)
			{
				shotTimeLeft = rateOfFire;
			}
			else
			{
				reloadTimeLeft = reloadDuration;
			}
		}
    }
}
