using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallsV3 : MonoBehaviour
{
    [SerializeField] GameObject Wall;
    Mesh mesh;
    private int faceIndex = -1; // index of the face to flip
    private bool flipped = false;
    void Start()
    {
        mesh = Wall.GetComponent<Mesh>();
    }
    private void Update()
    {
        // check if we need to flip the face
        if (faceIndex >= 0 && !flipped)
        {
            // flip the normal of the face
            Vector3[] normals = mesh.normals;
            normals[faceIndex] = -normals[faceIndex];
            normals[faceIndex + 1] = -normals[faceIndex + 1];
            normals[faceIndex + 2] = -normals[faceIndex + 2];
            mesh.normals = normals;
            flipped = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // check if the collision is with a cube
        if (other.gameObject.CompareTag("CamObstacleWall"))
        {
            // get the face that is colliding with the trigger
            Vector3 contactPoint = other.ClosestPoint(transform.position);
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = mesh.vertices[triangles[i]];
                Vector3 v2 = mesh.vertices[triangles[i + 1]];
                Vector3 v3 = mesh.vertices[triangles[i + 2]];
                if (PointInTriangle(contactPoint, v1, v2, v3))
                {
                    faceIndex = i;
                    break;
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        // check if the collision is with a cube
        if (other.gameObject.CompareTag("CamObstacleWall"))
        {
            // reset the face index
            faceIndex = -1;
            flipped = false;
        }
    }
    private bool PointInTriangle(Vector3 p, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // implementation of the point-in-triangle test
        Vector3 e1 = v2 - v1;
        Vector3 e2 = v3 - v1;
        Vector3 normal = Vector3.Cross(e1, e2);
        float d = Vector3.Dot(normal, v1);
        float dist = Vector3.Dot(normal, p) - d;
        if (dist < 0)
            return false;
        Vector3 c = Vector3.Cross(e1, p - v1);
        if (Vector3.Dot(normal, c) < 0)
            return false;
        c = Vector3.Cross(e2, p - v1);
        if (Vector3.Dot(normal, c) < 0)
            return false;
        return true;
    }
}