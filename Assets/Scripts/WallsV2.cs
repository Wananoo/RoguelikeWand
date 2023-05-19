using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//Script que desactiva las paredes que estan en un boxarea entre camara y player.
public class WallsV2 : MonoBehaviour
{
    [SerializeField] Vector3 boxSize;

    [SerializeField] Vector3 playerPos;

    private Transform cameraTransform;
    [SerializeField] GameObject VCam;

    [SerializeField] LayerMask layerMask;
    [SerializeField] private float overlapCheckInterval; // How often to check for overlaps
    [SerializeField] private float disableDuration; // How long to disable the renderer for an object
    private List<Renderer> disabledRenderers = new List<Renderer>(); // List of disabled renderers
    private Bounds overlapBounds;
    [SerializeField] GameObject Wall;
    [SerializeField] Material[] wallMaterials;
    private void Start()
    {
        //wallMaterials = Wall.GetComponent<MeshRenderer>().sharedMaterials;
        cameraTransform = Camera.main.transform;
        playerPos = VCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Follow.position;
        StartCoroutine(CheckOverlaps());
    }
        
        // Check if there are any objects between camera and player
    private IEnumerator CheckOverlaps()
    {
        while (true)
        {
            Collider[] colliders = inBetween();
            foreach (Collider collider in colliders)
            {
                if (collider.GetComponent<BoxCollider>() != null)
                {
                    //int triangleIndex = Wall.GetComponent<MeshCollider>().sharedMesh.triangles[contact.thisTriangleIndex];
                    //int submeshIndex = mesh.GetSubMesh(triangles[triangleIndex]);
                }
            }
            //Debug.Log("found: "+string.Join(", ", colliders.Select(go => go.gameObject.transform.name)));
            // Disable renderers for overlapping objects and add them to the disabledRenderers list
            foreach (Collider collider in colliders)
            {
                Renderer renderer = collider.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    disabledRenderers.Add(renderer);
                }
            }
            // Check if any of the disabled renderers should be re-enabled
            for (int i = 0; i < disabledRenderers.Count; i++)
            {
                Renderer renderer = disabledRenderers[i];
                Collider collider = renderer.GetComponent<Collider>();
                // If the renderer's collider is not overlapping with the box anymore, start the coroutine to re-enable its renderer
                if (!colliders.Contains(collider))
                {
                    StartCoroutine(EnableRenderer(renderer));
                    disabledRenderers.RemoveAt(i);
                    i--;
                }
            }
            yield return new WaitForSeconds(overlapCheckInterval);// para no reactivarlos inmediatamente
        }
    }

    private IEnumerator EnableRenderer(Renderer renderer)
    {
        yield return new WaitForSeconds(disableDuration);// Wait for the specified duration before enabling the renderer
        renderer.enabled = true;
    }
    
    Collider[] inBetween()//Gets all colliders in layermask in between
    {
            playerPos = VCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Follow.position;
            // Calculate direction from camera to player
            Vector3 direction = playerPos - cameraTransform.position;
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 targetPosition = playerPos;
            Vector3 boxCenter = (cameraPosition + targetPosition) *0.5f;
            float dis =  Vector3.Distance(targetPosition, cameraPosition);//Distancia sale sqrd????
            boxSize.y = Mathf.Sqrt(dis);//Sqrt para restaurar distancia
            // Get Rotation, Se le aplica rotacion a LookRotation porque no se pq sale asi pero queda bien
            Quaternion rotation = Quaternion.LookRotation(targetPosition - cameraPosition) * Quaternion.Euler(90f, 45f, 45f);
            Collider[] colliders = Physics.OverlapBox(boxCenter, boxSize , rotation , layerMask);
            //Creamos Bounds para el gizmo
            Vector3 center = boxCenter;
            Vector3 size = boxSize;
            overlapBounds = new Bounds(center, size);
            return colliders;
    }
    void OnDrawGizmos()
        {
            /*Gizmos.color = Color.green;
            Quaternion rotation = Quaternion.LookRotation(playerPos - Camera.main.transform.position) * Quaternion.Euler(90f, 45f, 45f);
            Gizmos.matrix = Matrix4x4.TRS(overlapBounds.center, rotation, overlapBounds.size);
            Gizmos.DrawWireCube(Vector3.zero, overlapBounds.size);*/
        }
}
