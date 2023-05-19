using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HullGenerator : MonoBehaviour
{
    public float InnerAngle()
    {
        return 0f;
    }
    public List<Vector3> GenerateHull(List<Vector3> points)//Genera un Convex hull para los puntos, selecciona solo 
    //los puntos requeridos en el orden necesario para crear un convex hull.
    {
        if (points.Count < 4)
        {
            // Not enough points to create a convex hull
            return points;
        }

        // Sort points along the x-axis
        points.Sort((p1, p2) => p1.x.CompareTo(p2.x));

        // Initialize list to hold upper and lower hulls
        List<Vector3> hull = new List<Vector3>();

        // Build upper hull
        for (int i = 0; i < points.Count; i++)
        {
            while (hull.Count >= 2 && IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], points[i]))
            {
                // Remove the last point in the hull if it makes a right turn
                hull.RemoveAt(hull.Count - 1);
            }

            // Add the current point to the hull
            hull.Add(points[i]);
        }

        // Build lower hull
        int lowerHullIndex = hull.Count + 1;
        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count >= lowerHullIndex && IsLeftTurn(hull[hull.Count - 2], hull[hull.Count - 1], points[i]))
            {
                // Remove the last point in the hull if it makes a right turn
                hull.RemoveAt(hull.Count - 1);
            }

            // Add the current point to the hull
            hull.Add(points[i]);
        }

        // Remove the duplicate points at the start and end of the hull
        //hull.RemoveAt(hull.Count - 1);
        hull.RemoveAt(0);

        return hull;
    }

    private bool IsLeftTurn(Vector3 a, Vector3 b, Vector3 c)
    {
        // Calculate the cross product of vectors ab and ac
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        float crossProduct = ab.x * ac.y - ab.y * ac.x;

        // If the cross product is negative, the turn is left
        return crossProduct < 0;
    }

    private static float Cross(Vector3 a, Vector3 b)
    {
        return a.x * b.y - a.y * b.x;
    }
    // Function that generates the concave hull from a list of points
    public List<Vector3> GenerateHull2(List<Vector3> points)
    {
        if (points.Count <= 1)
            return points;

        // Find the pivot point
        Vector3 pivot = points[0];
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].x < pivot.x || (points[i].x == pivot.x && points[i].y < pivot.y))
            {
                pivot = points[i];
            }
        }

        // Sort the points by polar angle
        List<Vector3> sortedPoints = points.OrderBy(p => SortByAngle(pivot, p)).ToList();

        // Build the hull
        List<Vector3> hull = new List<Vector3>();
        hull.Add(sortedPoints[0]);
        hull.Add(sortedPoints[1]);

        for (int i = 2; i < sortedPoints.Count; i++)
        {
            while (hull.Count >= 2 && !IsLeftTurn2(hull[hull.Count - 2], hull[hull.Count - 1], sortedPoints[i]))
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(sortedPoints[i]);
        }

        return hull;
    }

    private static float SortByAngle(Vector3 pivot, Vector3 p)
    {
        float angle = Mathf.Atan2(p.y - pivot.y, p.x - pivot.x) * Mathf.Rad2Deg;
        return angle < 0 ? angle + 360 : angle;
    }

    private static bool IsLeftTurn2(Vector3 a, Vector3 b, Vector3 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
    }
}
