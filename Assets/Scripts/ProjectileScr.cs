using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileScr : MonoBehaviour
{
    [SerializeField] LayerMask ImpactLayer;
    [SerializeField] float Durability;
    [SerializeField] GunController GunControllerScript;
    [SerializeField] public ParticleSystem Explo;
    [SerializeField] public ParticleSystem Trail;
    // Start is called before the first frame update
    void Start()
    {
        GunControllerScript = FindObjectOfType<GunController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision collision) 
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("DungLoose"))
        {//Si choca con una pared en un angulo determinado, dependiendo de su durabilidad, se elimina.
            float speedAttime = gameObject.GetComponent<Rigidbody>().velocity.magnitude;
            float dotProduct = Vector3.Dot(collision.contacts[0].normal, collision.relativeVelocity.normalized);
            float impactForce = collision.relativeVelocity.magnitude * dotProduct;
            //Debug.Log("Collision force: " + impactForce);
            if (impactForce > speedAttime*Durability)
            {
                if(GunControllerScript.LifetimeCRT.ContainsKey(gameObject))
                {
                    StartCoroutine(GunControllerScript.DestroyRN(gameObject));
                }
            }
        }
        else if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Hittable"))
        {//Si choca con una pared en un angulo determinado, dependiendo de su durabilidad, se elimina.
            collision.collider.GetComponent<CharacterStats>().HP--;
            if(GunControllerScript.LifetimeCRT.ContainsKey(gameObject))
            {
                StartCoroutine(GunControllerScript.DestroyRN(gameObject));
            }
        }
    }
}
