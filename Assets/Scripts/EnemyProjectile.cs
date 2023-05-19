using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyProjectile : MonoBehaviour
{
    public float Duration;
    public float EndTime;
    public string Name;
    public GameObject Object;
    public AudioClip sound;
    float Damage {get;set;}
    List<string> Effects;
    float speed;
    bool pierce;
    public void Shoot(Vector3 origin, Vector3 objective)
    {
        GameObject projectile = GameObject.Instantiate(Object);
        Vector3 direction = (origin-objective).normalized;
        projectile.transform.position = origin;
        projectile.GetComponent<Rigidbody>().velocity = direction * speed;
    }
    // Start is called before the first frame update
    void Start()
    {
        EndTime = Duration + Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time>EndTime)
        {
            Destroy(gameObject);
        }
    }
    void OnCollisionEnter(Collision collision) 
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Hittable"))
        {
            collision.collider.GetComponent<CharacterStats>().HP--;
            Destroy(gameObject);
        }
    }
}
