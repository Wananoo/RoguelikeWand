using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cinemachine;
//using System.Linq;
using System.Security.Cryptography;
using UnityEngine.Rendering;
//Scritp que mantiene info sobre todos los puntos ocupados por pisos y en base a estos, genera muros, los une y
//une los pisos. 
public class SpawnPoints : MonoBehaviour
{
    [SerializeField] public Queue<IEnumerator> coroutineQueue = new Queue<IEnumerator>();
    [SerializeField] public string Seed;
    [SerializeField] public int CurrentRoom=0;
    public List<Vector3> UsedPoints;
    public List<Vector3> fullEmpties;
    public List<GameObject> Floors;
    [SerializeField ]public float BiasWeight;
    [SerializeField] const float epsilon = 2f;
    [SerializeField] float Rooms;
    [SerializeField] public float MaxRooms;
    [SerializeField] List<float> RoomSize;
    [SerializeField] public List<GameObject> NormalFloors;
    [SerializeField] GameObject Wall;
    [SerializeField] GameObject fullWall;
    [SerializeField] float RayHeight;
    [SerializeField] float RayDistance;
    bool Filled = false;
    [SerializeField] public float treeChance;
    [SerializeField] public float treeDistancePenalty;
    [SerializeField] public float trees;
    [SerializeField] public float extraRoomChance;
    [SerializeField] GameObject floorHolder;
    [SerializeField] GameObject firstFloor;
    [SerializeField] GameObject firstFloorPrefab;
    [SerializeField] GameObject gameObjectCenter;
    [SerializeField] GameObject targetGroup;
    [SerializeField] CinemachineVirtualCamera VCam;
    bool coroutineRunning = false;
    bool Done = false;
    public bool TreeDone = false;
    //bool initialized=false;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public IEnumerator Build(float roomNumber, MapInfo ownInfo,GameObject current, bool isTree, List<string> bias)
    {
        Debug.Log("Enter Build in room "+roomNumber+"is "+(isTree?"":"not ")+"tree");
        CurrentRoom++;
        bool MaxRoomsReached = roomNumber >= MaxRooms+1;//*(MaxRooms/(trees+1));
        TreeDone = (MaxRoomsReached)?true:false;
        if (MaxRoomsReached && !isTree)
        {
            //Deja de generar si llegamos al max de salas.
            yield break;
        }
        //Debug.Log("Build start in room "+roomNumber);
        //Debug.Log("building from "+current.name);
        List<Conexion> ListaConex = new List<Conexion>();
        if (ownInfo != null && ownInfo.Conexiones != null)
        {
            //Obtiene las posibles conexiones del actual elemento.
            ListaConex = ownInfo.Conexiones;
            Debug.Log(" - ListaConex de "+current.name+"("+roomNumber+"): "+string.Join(", ",ListaConex.Select(a=>a.Point.name).ToList()));
            List<Conexion> conexToClone = new List<Conexion>();
            for (int i = 1; i<=BiasWeight;i++)
            {
                if ((current.CompareTag("HWY Prefab") && !isTree) || (current.CompareTag("HWX Prefab") && isTree))
                {
                    Debug.Log("Skipping "+current.name+"("+roomNumber+")");
                    break;
                }
                else
                {
                    Debug.Log("Not Skipping "+current.name+"("+roomNumber+")");
                }
                foreach (string conBias in bias)
                {
                    Conexion newConex = ListaConex.FirstOrDefault(obj => obj.Point.name.Equals(conBias)).Clone();
                    if (!newConex.Equals(default(Conexion)))
                    {
                        conexToClone.Add(newConex);
                    }
                }
                ListaConex.AddRange(conexToClone);
            }
        }
        else
        {
            Debug.LogError("ownInfo or ownInfo.Conexiones is null.");
        }
        //Elegir un punto y bloque al azar
        Conexion nuevaConex;
        bool ok=false;
        int errorcount=0;
        // Toma una conexion, comprueba que la sala no esté ocupada, si lo está, busca otra conexion, para evitar
        //que el mapa se construya dando vueltas sobre si mismo, aunque es inevitable a menos que se ocupe un algoritmo
        //especial, por lo que se ocupa un conteo de erorres, si no encuentra punto disponible, solo contstruye encima.
        //Esto puede generar mapas en los que podria ser imposible atravesar.
        do 
        {
            Debug.Log("ListaConex de "+current.name+"("+roomNumber+"): "+string.Join(", ",ListaConex.Select(a=>a.Point.name).ToList()));
            nuevaConex = ListaConex[Random.Range(0, ListaConex.Count)];
            ok = !isUsed(nuevaConex.Point.transform.position);
            errorcount++;
        }while(!ok && errorcount<100);
        //Toma un nuevo piso de los posibles siguientes para el piso actual. i.e. PasilloX no puede conectarse a PasilloY
        GameObject nuevo = nuevaConex.Next[Random.Range(0, nuevaConex.Next.Count)];
        //Luego, fuerza un piso de tipo sala, dependiendo de la probabilidad de salas extra.
        if (Random.Range(1,101) <= extraRoomChance)
        {
            //Manager.NormalFloors contiene los 2 prefab de salas; la normal y el prefab alternativo
            //Si la sala es forzada, toma Manager.NormalFloors[0] que llamaremos sala 1.
            nuevo =  NormalFloors[0];
            //Si el piso actual es la sala 1, generamos la sala 2, al contrario tambien funciona.
            //Esto se ve innecesario pero en el momento en que fue implementado, tomaba uno de las salas al azar.
            if (NormalFloors.Contains(current))
            {
                nuevo = NormalFloors.Where(element => element != current).ToList()[0];
            }
            //Debug.Log("Room forced at"+transform.position);
        }
        Debug.Log("RoomNumber= "+roomNumber);
        GameObject nuevoGO = Instantiate(nuevo);//Instancia el nuevo piso.
        //Instanciar bloque y asignar este script al bloque, con el roomnumber++ y el bloque usado
        nuevoGO.gameObject.transform.position = nuevaConex.Point.transform.position;
        MapBuilder nuevoBuilder = nuevoGO.AddComponent<MapBuilder>();
        nuevoGO.transform.SetParent(current.transform);
        //Set sala nueva, ya que hereda el script antiguo.
        nuevoBuilder.Set(CurrentRoom+1, ConvertConnectorString(nuevaConex.Point.name));
        /*while(!MaxRoomsReached)
        {
            yield return null;
        }*/
        yield break;
    }
    // Update is called once per frame
    void Update()
    {
        if(coroutineQueue.Count>0)
        {
            Debug.Log("Dequque "+coroutineQueue.Count);
            StartCoroutine(coroutineQueue.Dequeue());
        }
        //Debug.Log("coroutineQueue.Count= "+coroutineQueue.Count);
        if (Input.GetKeyDown(KeyCode.I))
        {
            //Re hacer
            Redo();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            //Re hacer con nueva seed
            Seed = Random.Range(0,int.MaxValue).ToString();
            Redo();
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            //Re hacer con 1 mas rooms
            MaxRooms++;
            Redo();
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            //Re hacer con 1 menos rooms
            MaxRooms--;
            if(MaxRooms <=0 )
            {
                MaxRooms = 1;
            }
            Redo();
        }
        Rooms = UsedPoints.Count;//Esto es para llevar cuenta de salas y empezar la construccion de muros.
        //Debug.Log("Rooms= "+Rooms);
        if (TreeDone && Rooms > MaxRooms && !Filled && !coroutineRunning && coroutineQueue.Count==0)
        {
            //Una vez esten todas las salas listas, se incia el coroutine para Fill, con un tiempo de espera pues
            //si se regeneran los pisos, el contador de pisos alcanza el total antes de spawnearlos, por lo que se 
            //da una ventana de tiempo para generar los muros.
            Done = false;
            coroutineRunning = true;
            Debug.Log("Fill Started");
            StartCoroutine(ToFill());
        }
        if (Filled && !Done)
        {
            //Una vez hecho el llenado de walls y union de meshes, se detiene la CR ToFill y se genera el TargetGroup
            //para enfocar el mapa generado.
            FinalStep();
        }
    }
    void FinalStep()
    {
        StopCoroutine(ToFill());
        gameObjectCenter.transform.position = GetComponent<Renderer>().bounds.center;
        Vector3 size = GetComponent<Renderer>().bounds.size;
        //Debug.Log("Center bounds size: "+size.x +", "+ size.y +", "+ size.z);
        float largestSize = Mathf.Max(size.x, size.y, size.z);
        float meshRadius = 0;
        if(largestSize - size.z < 1f) //threshold
        {
            VCam.GetCinemachineComponent<CinemachineGroupComposer>().m_FramingMode= CinemachineGroupComposer.FramingMode.Vertical;
            //gameObject.transform.rotation = Quaternion.identity*Quaternion.Euler(0,90,0);
            //floorHolder.transform.rotation = Quaternion.identity*Quaternion.Euler(0,90,0);
        }
        else
        {
                        VCam.GetCinemachineComponent<CinemachineGroupComposer>().m_FramingMode= CinemachineGroupComposer.FramingMode.Horizontal;
            //gameObject.transform.rotation = Quaternion.Euler(0,0,0);
            //floorHolder.transform.rotation = Quaternion.Euler(0,0,0);
        }
        meshRadius = largestSize/2;
        targetGroup.GetComponent<CinemachineTargetGroup>().m_Targets[0].radius = meshRadius;
        Done = true;
    }
    IEnumerator ToFill()
    {
        //Wait.
        yield return new WaitForSeconds(0.05f);
        Fill();
    }
    void Redo()
    {
        //Se reinician todas las estadisticas
        Vector3 position = firstFloor.transform.position;
        GameObject prefab = firstFloorPrefab;
        UsedPoints.Clear();
        Floors.Clear();
        fullEmpties.Clear();
        GameObject newObj = Instantiate(prefab);
        newObj.transform.position = position;
        Destroy(firstFloor);
        firstFloor = newObj;
        firstFloor.SetActive(true);
        Rooms = 0;
        trees = 0;
        CurrentRoom=0;
        Filled = false;
    }
    public bool isUsed(Vector3 newPoint)//Toma el punto enviado desde MapBuilder para comprobar que el punto esté
    //vacio, ademas toma un valor epsilon, ya que Unity aproxima valores, nos aseguramos que nos refiramos al mismo punto
    //Unity aproxima valores cambiando hasta 0.1 del original, le asignamos un threshold de 2 solo por asegurarme.
    //Los puntos estan separados entre 10 unidades, con 2 es suficiente.
    {
        bool used = false;
        foreach (Vector3 point in UsedPoints)
        {
            if (Vector3.Distance(point, newPoint) < epsilon)
            {
                // the two points are approximately equal, do something here
                //Debug.Log("Point "+newPoint+" is used");
                used = true;
                break;
            }
        }
        return used;
    }
    public void Fill()
    {
        //Se recomienda hacer el recorrido de llamados para comprender.
        //Comienza el Llenado, primero se obtienen todos los PUNTOS de espacios vacíos de los bloques
        List<Vector3> emptySpaces = getAllEmptySpaces();
        //Se crea un MeshFilter array con los espacios vacios y las salas completamente vacias (exteriores adyacentes)
        //que fueron calculadas en getAllEmptySpaces() pero enviadas a otro List.
        MeshFilter[] meshFilters = new MeshFilter[emptySpaces.Count+fullEmpties.Count];
        //Un array que contiene todos los espacios vacios, encontrados y salas completas vacias.
        List<GameObject> walls = new List<GameObject>();
        foreach(Vector3 pos in emptySpaces)
        {
            //Se añade una pequeña pared para cada seccion vacia encontrada en los pisos generados.
            GameObject wall = Instantiate(Wall);
            wall.transform.position = pos;
            wall.transform.SetParent(gameObject.transform);
            //Agregamos el MeshFilter al Array.
            meshFilters[emptySpaces.IndexOf(pos)]=wall.GetComponent<MeshFilter>();
            walls.Add(wall);
        }
        foreach(Vector3 pos in fullEmpties)
        {
            //Se añade una pared completa en los espacios completamente vacios.
            GameObject fullwall = Instantiate(fullWall);
            fullwall.transform.position = pos;
            //Debug.Log("Placed FullWall at "+pos);
            fullwall.transform.SetParent(gameObject.transform);
            //Agregamos el MeshFilter al Array, continuando desde las paredes individuales.
            meshFilters[fullEmpties.IndexOf(pos)+emptySpaces.Count]=fullwall.GetComponent<MeshFilter>();
            walls.Add(fullwall);
        }
        WallMerge(meshFilters);//Unimos las paredes
        foreach(GameObject wall in walls)
        {
            //Destruimos las paredes individuales
            Destroy(wall);
        }
        FloorMerge();//Unimos los pisos
        foreach(GameObject floor in Floors)
        {
            //Destruimos los pisos individuales
            Destroy(floor);
        }
        Debug.Log("Fill Done");
        coroutineRunning = false;
        Filled = true;
    }
    public void FloorMerge()//Funcion que une los meshfilter de los pisos y lo asigna a un GO empty (floorHolder).
    {
        MeshFilter[] meshFilters = new MeshFilter[Floors.Count];
        foreach(GameObject floor in Floors)
        {
            //Extraemos cada MeshFilter y (no se si es necesario) emparentamos cada floor al holder
            meshFilters[Floors.IndexOf(floor)]=floor.GetComponent<MeshFilter>();
            floor.transform.SetParent(floorHolder.transform);
            //Debug.Log("Setting parent of "+floor);
        }
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];
        // Loop through each mesh filter and add it to the CombineInstance array
        for (int i = 0; i < meshFilters.Length; i++)
        {
            // Make sure the mesh filter has a valid mesh
            if (meshFilters[i].sharedMesh == null)
            {
                continue;
            }

            // Create a new CombineInstance and set its transform and mesh
            combineInstances[i] = new CombineInstance();
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
        }
        // Create a new mesh for the combined meshes
        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineInstances, true);
        MeshFilter mainMeshFilter = floorHolder.GetComponent<MeshFilter>();
        //mainMeshFilter.sharedMesh = combinedMesh;
        mainMeshFilter.mesh = combinedMesh;
    }
    public void WallMerge(MeshFilter[] meshFilters)//Funcion que une las paredes y las pone en el gameObject de este script
    {
        // Create a new CombineInstance array
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

        // Loop through each mesh filter and add it to the CombineInstance array
        for (int i = 0; i < meshFilters.Length; i++)
        {
            // Make sure the mesh filter has a valid mesh
            /*if (meshFilters[i].sharedMesh == null)
            {
                continue;
            }*/
            // Create a new CombineInstance and set its transform and mesh
            combineInstances[i] = new CombineInstance();
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
        }

        // Create a new mesh for the combined meshes
        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineInstances, true);
        MeshFilter mainMeshFilter = GetComponent<MeshFilter>();
        //mainMeshFilter.sharedMesh = combinedMesh;
        mainMeshFilter.mesh = combinedMesh;
        // Combine the meshes into the new mesh
        /*combinedMesh.CombineMeshes(combineInstances);

        // Set the combined mesh to the main object's mesh filter
        MeshFilter mainMeshFilter = GetComponent<MeshFilter>();
        mainMeshFilter.sharedMesh = combinedMesh;*/
    }
    List<Vector3> getAllEmptySpaces()//Funcion que obtiene todos los espacios vacios, osea
    // secciones vacias de pisos y puntos exteriores.
    {
        List<Vector3> emptySpaces = new List<Vector3>();
        HashSet<Vector3> doneSpaces = new HashSet<Vector3>();
        foreach(Vector3 pos in  UsedPoints)
        {
            //Obtenemos el punto + los 4 adyacentes.
            List<Vector3> posAndAdj = addAdjacent(pos);
            foreach (Vector3 pos2 in posAndAdj)
            {
                //Revisamos cada sala y los adyacentes si ya hicimos el punto, lo saltamos.
                if (!doneSpaces.Contains(pos2))
                {
                    emptySpaces.AddRange(getEmptySpaces(pos2));//Añadimos los espacios vacios a emptySpaces
                    doneSpaces.Add(pos2);//Agregamos el punto como ya hecho.
                    //Debug.Log("Agregado: "+pos2);
                }
            }
        }
        //string vectorString = string.Join(",\n ", doneSpaces.Select(v => v.ToString()).ToArray());
        //Debug.Log("Spaces checked for: "+vectorString);
        //Debug.Log("All Spaces done!");
        return emptySpaces.Distinct().ToList();//Devolvemos todos los espacios vacios unicos a Fill()
    }
    List<Vector3> addAdjacent(Vector3 center)//Funcion que dado un punto, obtiene los 4 adyacentes con un offset de
    //10 unidades, en el caso de los pisos.
    {
        float offset= 10f;
        List<Vector3> AllPoints = new List<Vector3>();
        AllPoints.Add(center);
        AllPoints.Add(center + new Vector3(offset, 0, 0));
        AllPoints.Add(center + new Vector3(-offset, 0, 0));
        AllPoints.Add(center + new Vector3(0, 0, offset));
        AllPoints.Add(center + new Vector3(0, 0, -offset));
        //string vectorString = string.Join(", ", AllPoints.Select(v => v.ToString()).ToArray());
        //Debug.Log("Adjacent ponints for "+center+"= "+vectorString);
        return AllPoints;//Devuelve a getAllEmptySpaces();
    }
    List<Vector3> getEmptySpaces(Vector3 room)//Funcion que toma cada punto enviado desde getAllEmptySpaces y determina
    //las secciones vacias en el caso de pisos y si es un vacio completo en el caso de adyacentes externas.
    {
        List<Vector3> localEmptySpaces = new List<Vector3>();
        //Obtenemos ancho y alto dado por Unity.
        float Width = RoomSize[0];
        float Height = RoomSize[1];
        // Calculate room boundaries
        Vector3 topLeft = new Vector3(room.x - Width / 2, room.y, room.z - Height / 2);
        Vector3 bottomRight = new Vector3(room.x + Width / 2, room.y, room.z + Height / 2);
        int gridside = 9;//Gridsize de 9 cumple con todos los pisos y pasillos.
        // Divide room into a gridsize x gridsize grid
        float xStep = (bottomRight.x - topLeft.x) / gridside;
        float zStep = (bottomRight.z - topLeft.z) / gridside;
        bool isFullEmpty = true;
        for (int x = 0; x < gridside; x++)
        {
            for (int z = 0; z < gridside; z++)
            {
                //Recorre cada seccion del area del punto.
                //Lanza un raycast desde el centro del grid, de una altura X, hasta la distancia (pre-establecida) + 3.
                Vector3 origin = new Vector3(topLeft.x + x * xStep + xStep / 2, RayHeight, topLeft.z + z * zStep + zStep / 2);
                Vector3 direction = Vector3.down;
                float distance = RayHeight+3;
                RaycastHit hitInfo;
                // Perform raycast
                if (!Physics.Raycast(origin, direction, out hitInfo, distance))
                {
                    // Hit not obstacle, mark cell as empty
                    //y agrega la seccion a emptyspaces (local).
                    //Debug.LogFormat("Empty space detected at "+new Vector3(origin.x,room.y,origin.z));
                    localEmptySpaces.Add(new Vector3(origin.x,room.y,origin.z));
                    
                    //Debug.LogFormat("Obstacle detected at ({0},{1})", x, z);
                }
                else
                {
                    //Si llega a impactar con algo, se determina que no es una sala completamente vacia
                    isFullEmpty = false;
                }
            }
        }
        if (isFullEmpty)
        {
            //Si la sala es completamente vacia, no devolvemos nada a getAllEmptySpaces y solo agregamos la sala vacia
            //a la lista de salas vacias.
            //Debug.Log("Detected FullEmpty at "+room);
            fullEmpties.Add(room);
            return new List<Vector3>();
        }
        return localEmptySpaces;//De lo contrario, devolvemos las secciones vacias a getAllEmptySpaces
    }
    public string ConvertConnectorString(string inputString)//Funcion que cambia el nombre del conector al contrario.
    {
        // Check if the input string starts with "Connector" and has a length of at least 9
        if (inputString.StartsWith("Connector") && inputString.Length == 10)
        {
            // Get the connector letter (X or Y) by taking the 9th character of the input string
            string connectorLetter = inputString.Substring(9, 1);

            // Replace the connector letter with a dash (-) and return the modified string
            return "Connector-" + connectorLetter;
        }
        else if (inputString.StartsWith("Connector-") && inputString.Length == 11)
        {
            // Get the connector letter (X or Y) by taking the 10th character of the input string
            string connectorLetter = inputString.Substring(10, 1);

            // Replace the dash with the connector letter and return the modified string
            return "Connector" + connectorLetter;
        }
        else
        {
            // If the input string doesn't match either pattern, return the input string as is
            return inputString;
        }
    }
}
