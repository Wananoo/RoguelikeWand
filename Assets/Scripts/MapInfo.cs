using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Conexion
{
    public GameObject Point;
    public List<GameObject> Next;
    Conexion(GameObject p, List<GameObject> n)
    {
        Point = p;
        Next = n;
    }
    public Conexion Clone()
    {
        return new Conexion(Point,Next);
    }
}
//Script que es vacio, pero contiene la lista de Conexiones para cada piso. 
//Se hace separado para mantener un poco de orden.
public class MapInfo : MonoBehaviour
{
    [SerializeField] public List<Conexion> Conexiones;
    void Start()
    {

    }
}
