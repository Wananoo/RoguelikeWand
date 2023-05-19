using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] float speed;// Player Speed on walking
    [SerializeField] float gravityValue;// Gravity
    CharacterController controller;//componente CharacterController
    Animator playerAnimator;//componente Animator
    [SerializeField] private bool groundedPlayer;//Bool para verificar que el pj toca el piso
    [SerializeField] private Vector3 playerVelocity;//Velocity Vector3
    [SerializeField] static Vector3 move;//Vector3 que guarda informacion sobre el proximo movimiento segun input
    [SerializeField] Vector3 aimPos;//Posicion de apuntado del mouse
    [SerializeField] float rotationSpeed;//Vel rotacion player.

    // Start is called before the first frame update
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();//Asignar controller
        playerAnimator = gameObject.GetComponent<Animator>();//Asignar Animator
    }
    // Update is called once per frame
    void Update()
    {
        groundedPlayer = controller.isGrounded;//bool (reduntante) que guarda el estado de grounded entegado por controller
        if (groundedPlayer && playerVelocity.y < 0)//No aumentar velocidad -y si esta grounded
        {
            playerVelocity.y = 0f;
        }
        //Ponemos los floats entregados por Unity, Axis Horiz y Vertical, invertidos en los ejes x y z de move
        move = new Vector3(Input.GetAxis("Horizontal")*-1, 0, Input.GetAxis("Vertical")*-1);
        Vector3 fixedmove = Quaternion.Euler(0, -45, 0) * move;//move rotated 45 to compensate camera
        if (fixedmove != Vector3.zero)//Si hay movimiento, i.e. si hay input
        {
            //gameObject.transform.forward = fixedmove;//El personaje mira hacia la direccion de movimiento, deshabilitado
            //pues el pj ahora mira al mouse
            playerAnimator.SetBool("isMoving", true);//bool para animator
            playerAnimator.SetFloat("moveSpeed",Mathf.Clamp01(move.magnitude));//vel para animator
        }
        else// si no hay mov
        {
            playerAnimator.SetBool("isMoving", false);//bool para animator
        }
        controller.Move(fixedmove * Time.deltaTime * speed);//pj se mueve de acuerdo a los inputs
        playerVelocity.y -= gravityValue * Time.deltaTime;//aumenta gravedad
        if (playerVelocity.y <= -9)
        {
            playerVelocity.y = -9;//Gravedad no pasa de -9 - se recomienda reemplazar -9 por una variable.
        }
        
        LookAtMouse();
        setLocalMoveVel();
    }
    void LookAtMouse()
    {
        
        //Vector3 input = Input.mousePosition;//Get mouse pos en pantalla}
        //Camera.main.ScreentoWorldPoint genera un punto en el mundo dependiendo de la pos del mouse en pantalla
        //Pasmos los valores de x e y en pantalla, ademas de un Z que es la altura de la camara.
        //Vector3 mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(input.x, input.y, Camera.main.transform.position.y));
        //aimPos es un nuevo punto usando la pos del mouse en el mundo y la altura del personaje.
        //aimPos = new Vector3(mousePosition.x, transform.position.y, mousePosition.z);
        Vector3 posToLook = GetMouseWorldPosition();
        posToLook.y = transform.position.y;
        transform.LookAt(posToLook);// el pj mira al punto
    }
    public Vector3 GetMouseWorldPosition()
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero); // Define a plane with normal Vector3.up and passing through the origin
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Create a ray from the camera through the mouse position
        float distanceToPlane;
        if (plane.Raycast(ray, out distanceToPlane)) // Get the intersection point of the ray with the plane
        {
            return ray.GetPoint(distanceToPlane); // Return the intersection point
        }
        return Vector3.zero; // Return Vector3.zero if there is no intersection
    }
    void FixedUpdate()
    {
        controller.Move(playerVelocity * Time.deltaTime);
    }
    void setLocalMoveVel()//Sets vector3 Velocities (X and Z) to Animator params
    {
        //Toma la velocidad y la traduce a movimiento local, para aplicar velocidad hacia donde este mirando el personaje
        Vector3 vel = transform.InverseTransformDirection(controller.velocity);
        playerAnimator.SetFloat("moveX",vel.x);
        playerAnimator.SetFloat("moveZ",vel.z);
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Debug.Log("COL: "+hit.gameObject.name);
        if (hit.gameObject.CompareTag("Ground"))//El mapa tiene un collider, no se ocupa el isGrounded
        {
            playerAnimator.SetFloat("moveSpeed", 1f);//Params de animator
            playerVelocity.y = -9;
            //canJump = true;
            //grounded = true;
        }
        else
        {
            //grounded = false;
        }
    }
    void OnDrawGizmos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction * 100);
    }
}
