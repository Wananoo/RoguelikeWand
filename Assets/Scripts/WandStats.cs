using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WandStats : MonoBehaviour
{
    //Basicamente un struct en forma de Script, pero asignado al gO wand para conservar sus stats originales
    //Orientado para diferentes varitas como prefabs, cada una con su valor
    [SerializeField] public float GunBPS;
    [SerializeField] public float ArmSpeed;
    [SerializeField] public GameObject prefab;
    [SerializeField] public float speed;
    [SerializeField] public float distanceThreshold;
    [SerializeField] public float lifetime;
    [SerializeField] public float spreadAngle;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
