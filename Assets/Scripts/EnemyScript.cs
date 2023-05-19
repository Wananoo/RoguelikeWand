using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour
{
    [SerializeField] float HP;
    [SerializeField] float Range;
    [SerializeField] bool runsAway;
    [SerializeField] float runDistance;
    [SerializeField] public GameObject victim;
    [SerializeField] float distanceFromVictim;
    [SerializeField] bool canHear;
    [SerializeField] float walkSpeed;
    [SerializeField] float attackSpeed;
    [SerializeField] float roamFreq;
    [SerializeField] float roamLength;
    [SerializeField] float lastRoam;
    [SerializeField] public List<EnemyProjectile> attackList;
    [SerializeField] public Dictionary<string, AudioClip> soundsList;
    [SerializeField] NavMeshAgent nMAgent;
    [SerializeField] AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        lastRoam = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (victim is null)
        {
            victim = FindPlayer();
        }
        if (victim is not null)
        {
            distanceFromVictim = Mathf.Abs((transform.position-victim.transform.position).magnitude);
            if (distanceFromVictim <= runDistance)
            {
                Attack();
            }
            else
            {
                Run();
            }
        }
        else
        {
            distanceFromVictim = float.MaxValue;
        }
        if (Time.time - roamFreq >= lastRoam)
        {
            Roam();
            lastRoam = Time.time;
        }
        
    }
    public virtual void Attack()
    {

    }
    void Run()
    {
        Vector3 RunPoint = Vector3.Normalize(transform.position - victim.transform.position);
        float EnoughDistance = Mathf.Abs(runDistance - (transform.position - victim.transform.position).magnitude);
        Vector3 destination = transform.position + RunPoint * EnoughDistance;
        nMAgent.SetDestination(destination);
    }
    GameObject FindPlayer()
    {
        GameObject player = null;
        Collider[] colliders = Physics.OverlapSphere(transform.position, Range);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                // Do something with the GameObject that has the tag
                player = collider.gameObject;
            }
        }
        return player;
    }
    void Roam()
    {

    }
}
