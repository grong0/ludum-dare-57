using UnityEngine;

public class Bullet : MonoBehaviour
{
	float lifeTime = 0;
	public float speed = 30;
	public float maxLifeTime = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
		transform.position += transform.up * speed * Time.deltaTime * -1;
		lifeTime += Time.deltaTime;
		if (lifeTime >= maxLifeTime)
		{
			Destroy(gameObject);
		}
    }

	void OnTriggerEnter(Collider obj)
	{
		if (obj.gameObject.CompareTag("Player"))
		{
			// Damage Player
		}
		Destroy(gameObject);
	}
}
