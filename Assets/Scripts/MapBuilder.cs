using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography;
//Script para cada bloque de mapa que contiene instrucciones sobre como monstruir, obtiene info de MapInfo 
//y la envia a SpawnPoints Manager
public class MapBuilder : MonoBehaviour
{
    [SerializeField] GameObject usedPoint;    
    [SerializeField] public GameObject nextBlock;
    [SerializeField] float roomNumber;
    [SerializeField] MapInfo ownInfo;
    SpawnPoints Manager;
    // Start is called before the first frame update
    void Start()
    {
        Manager = GameObject.FindGameObjectWithTag("SpawnManager").GetComponent<SpawnPoints>();
        //Obiene info de datos sobre el piso actual
        ownInfo = gameObject.GetComponent<MapInfo>();
        if (usedPoint != null)
        {
            //If utilizado solo para el primer piso, que es un empty con solamente un conector
            if(gameObject.CompareTag("EventEmpty"))
            {
                Set(roomNumber, "ConnectorX");
            }
            //Para todos los demas pisos.
            else
            {
                //Elimina el conector contrario al usado por el piso anterior, es decir que si vienen de un 
                //ConnectorX, se elimina el Connector-X de los posibles conectores para crear la siguiente sala esto
                //para evitar que la siguiente sala se genere encima de la anterior de la actual.
                ownInfo.Conexiones.Remove(ownInfo.Conexiones.First(u => u.Point==usedPoint));
                //Se agrega este piso al Manager de Puntos.
                Manager.Floors.Add(gameObject);
            }
        }
    }
    public void Set(float CurrentRoom, string usado)//Funcion usada para establecer valores de el piso
    //No se ocupa un constructor ya que entra en conflicto para el primer piso, que es un empty, y se debe iniciar
    //solo. Ademas, cuando creamos una sala nueva, esta hereda este mismo script de la anterior, por lo que se debe
    //cambiar sus valores.
    {
        Manager = GameObject.FindGameObjectWithTag("SpawnManager").GetComponent<SpawnPoints>();
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Manager.Seed));
        int seedValue = System.BitConverter.ToInt32(hash, 0);
        md5 = MD5.Create();
        byte[] prevRoomHhash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(gameObject.transform.position.ToString()));
        int prevRoomSeed = System.BitConverter.ToInt32(prevRoomHhash, 0);
        Random.InitState(seedValue+(int)CurrentRoom+prevRoomSeed);
        //Debug.Log("Setting " + gameObject.name+" using  "+usado);
        //Encuentra nuevamente al manager, puede ser innecesario.
        //Agrega la ubicacion a los puntos del manager.
        Manager.UsedPoints.Add(new Vector3(transform.position.x,0,transform.position.z));
        //Encuentra, nuevamente su propio script de info de donde puede construir.
        ownInfo = gameObject.GetComponent<MapInfo>();
        //Asigna un numero de sala.
        roomNumber = CurrentRoom;
        //Asigna el punto usado a usedPoint, para excluirlo del pool de puntos para la siguiente sala.
        usedPoint = gameObject.transform.Find(usado).gameObject;
        //Inicia Build. Es CR para poder generar los trees al mismo tiempo que el resto del codigo, no solo al final.
        StartCoroutine(Manager.Build(roomNumber,ownInfo,gameObject,false, new List<string>{"ConnectorX"}));
        Debug.Log("Build end in room "+roomNumber);
        //Si es treeable, dependiendo de treeChance puede generar otras salas.
        //ESTO ENTRA EN CONFLICTO CON MAXROOMS, PUES MANAGER GENERA WALLS CADA VEZ QUE SE CREA UNA SALA QUE EXCEDE EL TOTAL
        //FIX
        //Se puede arreglar agregando un flag en manager, pero el Manager va a generar walls cada vez que un tree termine.
        //Hay que encontrar la forma de saber cuando se dejo de crear salas definitivamente.
        float lengthPercentage = Mathf.Clamp01((float)roomNumber / Manager.MaxRooms);
        float length1to10Value = Mathf.Lerp(1f, 10f, lengthPercentage);
        if (Random.Range(1,101) <= Manager.treeChance/(length1to10Value*Manager.treeDistancePenalty) && gameObject.CompareTag("Treeable"))
        {
            Manager.trees++;
            Manager.coroutineQueue.Enqueue((Manager.Build(roomNumber,ownInfo,gameObject,true,new List<string>{"ConnectorY","Connector-Y"})));
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
