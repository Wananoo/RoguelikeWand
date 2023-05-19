using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
public struct Rune// Struct publica para guardar datos sobre cada runa una vez calculados, para no acceder 
//a los gameObjects desde otros scripts
    {
        public GameObject rune;//gO correspontiente
        public float angle;//Angulo dentro del poligono
        public float anglepercent;//angulo en % respecto a la suma de angulos interiores
        public int quantity;//cuantas runas del mismo tipo hay

        public Rune(GameObject r, float a, float ap,int q)//constructor
        {
            rune = r;
            angle = a;
            anglepercent = ap;
            quantity = q;
        }
    }
//Script que mantiene el GUI, obtiene los datos de las runas, se comunica con Gun para establecer los stats.
public class GUIController : MonoBehaviour
{
    [SerializeField] Canvas runeCanvas;
    [SerializeField] HullGenerator hullGenerator;
    bool CanvasEnabled;
    private Vector3 offset;
    public RectTransform panel;
    bool dragging;
    private List<Vector3> hullPoints = new List<Vector3>();
    private List<Vector3> runesPoints = new List<Vector3>();
    private LineRenderer lineRenderer;
    private Plane dragPlane;
    [SerializeField] GameObject currentRune = null;
    [SerializeField] float maxDistance;
    [SerializeField] Dictionary<GameObject, float> angles = new Dictionary<GameObject, float>();
    
    [SerializeField] List<Rune> RunesData = new List<Rune>();
    [SerializeField] GunController GC;
    [SerializeField] GameObject CadenceText;
    [SerializeField] GameObject PrecisionText;
    [SerializeField] GameObject PotencyText;
    [SerializeField] GameObject LifeText;
    [SerializeField] GameObject textPanel;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = panel.GetComponent<LineRenderer>();
        SwitchCanvas();//Canvas aparece apagado, se inicia automaticamente
    }
    Plane PlaneFromPanel()//Crea un Plane infinito del panel principal de GUI
    {
        Vector3[] corners = PanelCorners();
        //Debug.Log("Corners: "+ string.Join(',',corners.Select(c => $"({c.x},{c.y},{c.z})").ToArray()));
        // Create a plane based on the corners of the panel
        Plane plane = new Plane(corners[0], corners[1], corners[2]);//Toma el panel, crea un plane infinito
        return plane;
    }
    Vector3[] PanelCorners()//Obtiene solamente las esquinas de panel principal de GUI
    {
        // Get the RectTransform of the panel
        RectTransform panelRectTransform = panel.GetComponent<RectTransform>();
        // Get the four corners of the panel in world space
        Vector3[] corners = new Vector3[4];
        panelRectTransform.GetWorldCorners(corners);
        //Debug.Log("Corners: "+ string.Join(',',corners.Select(c => $"({c.x},{c.y},{c.z})").ToArray()));
        return corners;
    }
    public void Drag()//Llamado cada vez que se esta arrastrando una runa
    {
        //Debug.Log("Dragging on: "+ GetFirstItem().gameObject.name);
        dragPlane = PlaneFromPanel();//se actualiza dragPlane
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float distanceToPlane;
        if (dragPlane.Raycast(ray, out distanceToPlane))// Determine where the mouse ray intersects the plane
        {//obtiene la distancia al plano. 
            //Debug.Log("distance:"+distanceToPlane);
            if (distanceToPlane<=maxDistance)//Hay una maxdistance, establecida en unity que prohibe que las runas salgan
            //del circulo
            {
                Vector3 targetPosition = ray.GetPoint(distanceToPlane);//Obtiene la posicion
                currentRune.transform.position = targetPosition;//mueve la runa actual presionada
            }
            // Set the position of the object to the intersection point
        }
    }
    void Awake()
    {
        CanvasEnabled = false;//dehabilita el canvas
        runeCanvas.gameObject.SetActive(CanvasEnabled);
        dragging = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))//Si Tab se presiona, switch.
        {
            SwitchCanvas();
        }
        if (Input.GetMouseButtonUp(0))//Si se suelta el boton del mouse, se cambian los valores de dragging y currentrune
        {
            dragging = false;
            currentRune = null;
        }
        if (Input.GetMouseButtonDown(0))//Si recien se aprieta el boton del mouse, obtiene la runa y dragging
        {
            dragging = true;
            currentRune = GetFirstItem();
        }
        if (Input.GetMouseButton(0) && dragging && currentRune is not null)//Si continua apretando, aun sobre una runa
        {
            Drag();//Recien comienza drag
        }
        if (CanvasEnabled)
        {
            DrawHull();//Dibuja el borde
        }
        //GetFirstItem();
    }
    private void OnPreRender()
    {
        if (CanvasEnabled)
        {
            DrawHull();//Dibuja el borde una vez antes de update.
        }
    }
    void SwitchCanvas()//Si se presiona tab, cambia los valores de active de linerenderer y canvas
    {
        CanvasEnabled = !CanvasEnabled;
        runeCanvas.gameObject.SetActive(CanvasEnabled);
        lineRenderer.enabled = false;
        //cambiamos tamaño del panel text dependiendo si esta enabled el cavas principal.
        textPanel.gameObject.GetComponent<RectTransform>().localScale = (CanvasEnabled)?Vector3.one:Vector3.one/2;
    }
    void ToggleMenu()// same as SwitchCanvas pero aun esta aqui
    {
        if (CanvasEnabled)
        {
            //StartCoroutine(runePanel.Reload());
        }
    }
    public GameObject GetFirstItem()//Obtiene el primer elemento que toca un rayo desde la camara, por el mouse
    {//mientras sea tag Rune3D
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin,ray.direction, out hit))
        {
            GameObject hitObject = hit.transform.gameObject;
            if (hitObject is not null && hitObject.CompareTag("Rune3D"))
            {
                return hitObject;
            }
        }
        return null;
    }
    void DrawHull()//Dibuja el borde, crea puntos en el linerenderer
    {
        runesPoints = new List<Vector3>();
        foreach(Transform child in panel.transform)//Obtiene cada posicion de cada runa
        {
            if (child.CompareTag("Rune3D"))
            {
                runesPoints.Add(child.position);
            }
        }
        //Debug.Log(runesPoints.Count);
        hullPoints = hullGenerator.GenerateHull(runesPoints);//Llama a GenerateHull y obtiene los puntos correspondientes
        lineRenderer.enabled = true;
        // Set the number of points in the LineRenderer
        lineRenderer.positionCount = hullPoints.Count;//Crea los puntos de LR
        // Set the positions of the LineRenderer to the hull points
        for (int i = 0; i < hullPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, hullPoints[i]);//Asigna los puntos de LR
        }
        GetAngles();
    }
    void GetAngles()//Una vez con los puntos en LR, se calculan los angulos
    {
        angles = new Dictionary<GameObject, float>();//limpia el diccionario de <gO, angulos>
        int pointCount = lineRenderer.positionCount;
        // Calculate the angles between each adjacent pair of points
        for (int i = 0; i < pointCount; i++)//Itera por cada punto y los 2 adyacentes, calcula el angulo
        {
            Vector3 prevPoint = lineRenderer.GetPosition((i - 1 + pointCount) % pointCount);
            Vector3 currPoint = lineRenderer.GetPosition(i);
            Vector3 nextPoint = lineRenderer.GetPosition((i + 1) % pointCount);

            float angle = Vector3.SignedAngle(nextPoint - currPoint, prevPoint - currPoint, Vector3.forward);
            angle = (angle + 360f) % 360f;
            // Find the closest game object to the current point
            GameObject closestObject = FindClosestObject(currPoint);
            //Debug.Log("Point: "+currPoint+" Found: "+closestObject);
            // Add the angle and its corresponding game object to the dictionary
            angles[closestObject] = angle;//asigna un angulo al correspondiente objeto en el diccionario de angulos
        }
        //Debug.Log("Corners: "+ string.Join(',',angles.Select(c => $"({c.Key.name} , {c.Value}°)").ToArray()));
        // Calculate the number of sides in the polygon
        int numSides = pointCount - 1;
        if (lineRenderer.loop) numSides++;
        RunesData = new List<Rune>();//Limpia la list runesdata, que tiene instancias del struct rune
        Dictionary<string,int> RuneQuant = new Dictionary<string, int>();
        //Crea un dict que contendra el principio del nombre de la runa y la cantidad. Se ocupa solo el principio
        //para considerar los clones.
        Dictionary<string,float> RuneTotalAngle = new Dictionary<string, float>();
        //Diccionario que contempla el total de los % de cada tipo de runa, para escalar correctamente si la runa
        //está 2 veces en el hull, tome un valor para ambas y no solo tome el ultimo valor y modifique el stat
        foreach(KeyValuePair<GameObject, float> pair in angles)
        {
            //para cada runa
            float percentage = (pair.Value / GetSumOfInnerAngles(numSides)) * 100f;
            percentage = 100 - Mathf.Abs(100-percentage);
            //calculamos el % de la runa individual
            //Si el alguno es mayor a 180, el angulo pasa a ser externo
            //por lo que se calcula el % de el complementario
            //En cualquier caso y en poligonos de mas de 3 lados, toma el lado menor
            if(RuneQuant.ContainsKey(pair.Key.gameObject.name.Substring(0,4)))
            {
                //sumamos el % de la runa y añadimos una a la cantidad
                RuneQuant[pair.Key.gameObject.name.Substring(0,4)] ++;
                RuneTotalAngle[pair.Key.gameObject.name.Substring(0,4)] += percentage;
            }
            else
            {
                //si no esta en el dicc, las agregamos.
                RuneQuant[pair.Key.gameObject.name.Substring(0,4)] = 1;
                RuneTotalAngle[pair.Key.gameObject.name.Substring(0,4)] = percentage;
            }
        }
        foreach(KeyValuePair<GameObject, float> pair in angles)//Por cada angulo, obtiene el % relacion al total
        {
            //Creamos el objeto runa, con: nombre, angulo individual, angulo % (del total de runas del mismo tipo)
            //y cantidad, la agregamos a la lista de runas
            Rune data = new Rune(pair.Key, pair.Value,RuneTotalAngle[pair.Key.gameObject.name.Substring(0,4)], RuneQuant[pair.Key.gameObject.name.Substring(0,4)]); // (rune,angle,anglepercent)
            RunesData.Add(data);
        }
        GC = GameObject.FindGameObjectWithTag("MainWand").GetComponent<GunController>();
        GC.SetStats(RunesData,GetLength());//Cambia los stats de la wand.
        //Debug.Log("Runes: "+ string.Join(',',RunesData.Select(r => $"({r.rune.name} , {r.angle}° , ({r.anglepercent}%)) ").ToArray()));
    }
    float GetLength()//Obtiene el largo del linerenderer
    {
        float length = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            length += Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
        }
        length += Vector3.Distance(lineRenderer.GetPosition(lineRenderer.positionCount - 1), lineRenderer.GetPosition(0));

        return length;
    }
    public static float GetSumOfInnerAngles(int numSides)//Da un total de la suma de angulos internos dependiendo de lados
    {
        // Calculate the sum of the inner angles using the formula (n-2) * 180 degrees
        float sumOfAngles = (numSides - 2) * 180f;

        // Return the sum of the inner angles
        return sumOfAngles;
    }
    private GameObject FindClosestObject(Vector3 pointPosition)
    {
        // Find the closest game object to the given point position
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Rune3D");
        float minDistance = Mathf.Infinity;
        GameObject closestObject = null;

        foreach (GameObject obj in objects)
        {
            float distance = Vector3.Distance(pointPosition, obj.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = obj;
            }
        }

        return closestObject;
    }
    private void OnDrawGizmos()
    {
        // Get the RectTransform component of the Panel
        //RectTransform rectTransform = panel.GetComponent<RectTransform>();

        // Get the corners of the RectTransform in world space
        Vector3[] corners = new Vector3[4];
        corners = PanelCorners();
        //rectTransform.GetWorldCorners(corners);

        // Draw the gizmo lines
        Gizmos.color = Color.red;
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(corners[3], corners[0]);
    }
}
