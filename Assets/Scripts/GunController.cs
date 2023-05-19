using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
public struct Stats// Struct que guarda los componentes originales de la wand.
    {
        public float OGGunBPS;
        public float OGArmSpeed;
        public GameObject OGprefab;
        public float OGspeed;
        public float OGdistanceThreshold;
        public float OGlifetime;
        public float OGspreadAngle;

        public Stats(float bps,float arms,GameObject pre, float sp, float dt, float lt, float SA)
        {
            OGGunBPS = bps;
            OGArmSpeed = arms;
            OGprefab = pre;
            OGspeed = sp;
            OGdistanceThreshold = dt;
            OGlifetime = lt;
            OGspreadAngle = SA;
        }
    }
//Script que maneja el Gun, toma datos de runas y las pasa a los proyectiles, los cuales tambien los controla.
public class GunController : MonoBehaviour
{
    [SerializeField] float GunBPS;
    [SerializeField] float ArmSpeed;
    [SerializeField] GameObject prefab;
    [SerializeField] int poolSize = 1000;
    [SerializeField] float speed;
    [SerializeField] GameObject WandTip;
    private List<GameObject> objectPool;
    [SerializeField] bool Shooting;
    [SerializeField] GameObject Wand;
    [SerializeField] GameObject WandHolder;
    [SerializeField] GameObject WandHolderPosRef;
    [SerializeField] float distanceThreshold;
    [SerializeField] float spreadAngle;
    [SerializeField] float lifetime;
    [SerializeField] float lifetimeFlickerSecs;
    [SerializeField] float FlickerRate;
    [SerializeField] List<GameObject> CurrentBullets = new List<GameObject>();
    [SerializeField] public Dictionary<GameObject,Coroutine> LifetimeCRT = new Dictionary<GameObject,Coroutine>();
    [SerializeField] WandStats wandStats;
    [SerializeField] Stats ogStats;
    [SerializeField] GameObject CadenceText;
    [SerializeField] GameObject PrecisionText;
    [SerializeField] GameObject PotencyText;
    [SerializeField] GameObject LifeText;
    [SerializeField] bool foundTexts=false;
    //Diccionario que guarda gameobject con sus coroutines.
    
    // Start is called before the first frame update
    void Start()
    {
        Shooting = false;// bool para no spamear shoot
        objectPool = new List<GameObject>(); 
        Pool();
        wandStats =transform.GetComponentInParent<WandStats>();
        InitialSetStats();
    }
    void InitialSetStats()//Obtiene los stats de la wand original, crea un struct con ellos 
    //y ademas los asigna a los stats actuales
    {
        ogStats = new Stats(wandStats.GunBPS, wandStats.ArmSpeed, prefab, wandStats.speed,wandStats.distanceThreshold,wandStats.lifetime, wandStats.spreadAngle);
        GunBPS = ogStats.OGGunBPS;
        ArmSpeed = ogStats.OGArmSpeed;
        speed = ogStats.OGspeed;
        distanceThreshold = ogStats.OGdistanceThreshold;
        lifetime = ogStats.OGlifetime;
        spreadAngle = ogStats.OGspreadAngle;
    }
    void Pool()//Crea pool con cantidad deseada
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            objectPool.Add(obj);
        }
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0) && !Shooting )
        {
            StartCoroutine(Shoot());
        }
        if(Input.GetMouseButtonUp(0))
        {
            StopCoroutine(Shoot());
        }
        //Si no estan encontrados los textos, intenta inicializarlos.
        if (!foundTexts)//Ya que el GUI se inicia poco despues del primer update, se comprueba que los haya encontrado primero
        {
            //Luego asigna referencias a cada GO, esto puede cambiar en el futuro.
            CadenceText = GameObject.Find("TextCanvas/TextPanel/Cadence");
            PrecisionText = GameObject.Find("TextCanvas/TextPanel/Precision");
            PotencyText = GameObject.Find("TextCanvas/TextPanel/Potency");
            LifeText = GameObject.Find("TextCanvas/TextPanel/Life");
            //Debug.Log("Found: "+CadenceText.gameObject+PrecisionText.gameObject+PotencyText+gameObject);
        }
        //Primero comprueba que los GO de textos esten inicializados
        bool allNotNull = (new GameObject[]{CadenceText,PrecisionText,PotencyText}).All(go => go != null);
        //Si estan inicializados,
        if(allNotNull)
        {
            foundTexts = true;
            //cambia el flag para no buscarlos nuevamente
            CadenceText.GetComponent<TextMeshPro>().text = "Cadence: "+GunBPS.ToString("0.00")+" bps";
            PrecisionText.GetComponent<TextMeshPro>().text = "Precision: -"+spreadAngle.ToString("0.00")+"°";
            PotencyText.GetComponent<TextMeshPro>().text = "Potency: "+speed.ToString("0.00")+"m/s";
            LifeText.GetComponent<TextMeshPro>().text = "Life: "+lifetime.ToString("0.0")+"|"+distanceThreshold.ToString("0.0")+"(s|m)";
        }
        else
        {
            Debug.Log("One or more TMP not found");
        }
    }
    void CheckDistance()// Get distancia y remove todos los bullets fuera del rango.
    {
        List<GameObject> toRemove = new List<GameObject>();
        foreach (GameObject go in CurrentBullets)
        {
            float distance = Vector3.Distance(go.transform.position, WandTip.transform.position);
            if (distance > distanceThreshold)
            {
                go.SetActive(false);
                try
                {
                    //StopCoroutine(LifetimeCRT[go]);//Si se elimina por distancia, se detiene el crt de lifetime
                    DestroyRN(go);// DestroyRN ya incluye StopCR
                }
                catch
                {
                    //
                }
                
                toRemove.Add(go);
            }
        }
        CurrentBullets.RemoveAll(item => toRemove.Contains(item));
    }
    private IEnumerator Shoot()//CR principal de disparo
    {
        Shooting = true;
        yield return new WaitForSeconds(ArmSpeed);
        GameObject obj = GetPooledObject();//Obtiene un objeto del pool
        if (obj is not null)
        {
            //primero obtiene ubicacion y rotacion de la punta de la varita.
            obj.transform.position = WandTip.transform.position;
            obj.transform.rotation = Quaternion.identity;
            //Activa y vuelve visible al objeto
            obj.SetActive(true);
            GoVisible(obj,true);
            //Se le aplica un valor aleatorio al componente X de la velocidad, usando spreadAngle
            Vector3 up = WandTip.transform.up;
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            Vector3 spreadVel = new Vector3(up.x+randomX, up.y, up.z) * speed;
            //obj.GetComponent<Rigidbody>().velocity = WandTip.transform.up * speed;
            obj.GetComponent<Rigidbody>().velocity = spreadVel;
            //La añade a la lista de balas y inicia la CR de eliminar por tiempo.
            CurrentBullets.Add(obj);
            LifetimeCRT[obj] = StartCoroutine(ProjectileLife(obj));//Coroutine de lifetime.
        }
        else
        {
            Debug.Log("Max Bullets reached");
        }
        yield return new WaitForSeconds(1f/GunBPS);//Freq yield antes del shooting = false ya que si no, no se que pasaba
        Shooting = false;
    }
    
    private IEnumerator ProjectileLife(GameObject projectile)//CR que controla la vida del proyectil, parpadea antes de eliminarse
    {
        yield return new WaitForSeconds(lifetime-lifetimeFlickerSecs);
        float startTime = (float)Time.time;
        while (Time.time <= startTime+lifetimeFlickerSecs)
        {
            projectile.GetComponent<Renderer>().enabled = false;
            yield return new WaitForSeconds(FlickerRate);
            projectile.GetComponent<Renderer>().enabled = true;
            yield return new WaitForSeconds(FlickerRate);
        }
        StartCoroutine(DestroyRN(projectile));
        //LifetimeCRT.Remove(projectile);
    }
     private GameObject GetPooledObject()// Get pooled del pool, devueltos una vez fuera del rango
    {
        for (int i = 0; i < objectPool.Count; i++)
        {
            if (!objectPool[i].activeInHierarchy)
            {
                return objectPool[i];
            }
        }
        return null;
    }
    private void LateUpdate()
    {
        WandHolder.transform.position = WandHolderPosRef.transform.position;
        CheckDistance();
    }
    public IEnumerator DestroyRN(GameObject go)//Llamado desde projectile script para eliminar en golpes bruscos,
    //por tiempo, por distancia y tambien sera usado en impacto con enemigos.
    {
        StopCoroutine(LifetimeCRT[go]);//Detiene la CR de lifetime del obj usando el dict
        LifetimeCRT.Remove(go);//Elimina el go del dict.
        ParticleSystem exp = go.GetComponent<ProjectileScr>().Explo;
        exp.gameObject.SetActive(true);
        //Debug.Log("Destroying");
        exp.Play();
        //Debug.Log("explo playing?: "+exp.isPlaying);
        GoVisible(go,false);//Hace invisible al proyectil para que solo quede la explosion
        yield return new WaitForSeconds(1f);
        go.SetActive(false);//Luego lo desactiva para que desaparezca completamente, proj y expl
        exp.gameObject.SetActive(false);
        CurrentBullets.Remove(go);
        yield return null;
    }
    public void SetStats(List<Rune> RunesData, float Length)//Llamado cada vez que Drag, cambia los stats dependiendo de 
    //los angulos de las runas, dado por RunesData que contiene los structs Rune por cada runa
    {
        //bools para penalizar la ausencia de un tipo de runa en el hull
        bool isfire=false;
        bool iswater=false;
        bool isquic=false;
        foreach (Rune r in RunesData)
        {
            //Se usa un substring para tomar, por ejemplo, WaterRune y WaterRune (1) o WaterRune (Clone) como "Wate"
            switch(r.rune.name.Substring(0,4))
            {
                //Se usa quantMult para aumentar los mult si hay mas de una runa, ademas, aumenta extra si estan
                //en el hull y no dentro, pues aumenta el angle% del total de runas del mismo tipo
                case "Wate":
                {
                    //Debug.Log("Water name: "+r.rune.gameObject.name);
                    iswater=true;
                    //para water, divide o multiplica por 6 las bps, dependiendo del % del angulo de water.
                    float quantMult = (r.quantity==1)?0:r.quantity*0.6f;
                    float BPSLerp = Mathf.Lerp(1f / 6f, 6f, r.anglepercent/100);
                    //Debug.Log("quant: "+r.quantity+", r.anglepercent"+r.anglepercent+"BPSLerp: "+BPSLerp);
                    BPSLerp = BPSLerp + BPSLerp*quantMult;
                    GunBPS = ogStats.OGGunBPS*BPSLerp;
                    break;
                }
                case "Fire":
                {
                    isfire = true;
                    //para fire, divide o multiplica por 6 la speed, dependiendo del % del angulo de fire.
                    //AGREGRAR DAÑO DESPUES
                    float quantMult = (r.quantity==1)?0:r.quantity*0.6f;
                    float SpdLerp = Mathf.Lerp(1f / 6f, 6f, r.anglepercent/100);
                    SpdLerp = SpdLerp + SpdLerp*quantMult;
                    speed = ogStats.OGspeed*SpdLerp;
                    //Debug.Log(r.anglepercent);
                    break;
                }
                case "Quic":
                {
                    isquic=true;
                    //para quic, va desde el angulo *2 hasta 0 para la variacion de angulo, dependiendo del ang% de quic
                    float quantMult = (r.quantity==1)?0:r.quantity*0.15f;
                    float angleLerp = r.anglepercent / 100f;
                    angleLerp = angleLerp + angleLerp*quantMult;
                    spreadAngle = Mathf.Lerp(ogStats.OGspreadAngle*2, 0f, angleLerp);
                    //Debug.Log("oldSpread: "+ogStats.OGspreadAngle+". newSpread: "+spreadAngle+". Mult:"+angleLerp);
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
        //Aqui, penaliza los valores originales si no hay runas del tipo
        if (!iswater)
        {
            GunBPS = ogStats.OGGunBPS/2;
        }
        if (!isfire)
        {
            speed = ogStats.OGspeed/2;    
        }
        if (!isquic)
        {
            spreadAngle = ogStats.OGspreadAngle*2;
        }
        distanceThreshold = ogStats.OGdistanceThreshold*Length;
        lifetime = ogStats.OGlifetime*Length;
        
        //Distance y lifetime se guian por el largo
        //Debug.Log("Length: "+Length+". new distanceThreshold: "+distanceThreshold);
    }
    void GoVisible(GameObject go, bool flag)
    {
        go.GetComponent<MeshRenderer>().enabled=flag;
        go.GetComponent<SphereCollider>().enabled=flag;
        go.GetComponent<ProjectileScr>().Trail.gameObject.SetActive(flag);
    }
}
