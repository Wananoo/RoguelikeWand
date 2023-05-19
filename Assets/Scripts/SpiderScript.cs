using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderScript : EnemyScript
{
    public override void Attack()
    {
        EnemyProjectile web = attackList[0];
        GetComponent<AudioSource>().PlayOneShot(web.sound);
        web.Shoot(gameObject.transform.position, victim.transform.position);
    }
}
