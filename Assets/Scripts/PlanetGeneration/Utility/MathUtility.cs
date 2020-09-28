using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Circle
{
    public Vector2 Center;
    float Radius;

    public Circle(Vector2 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool ContainsInclusive(Vector2 SomePoint)
    {
        float diffX2 = Mathf.Pow(SomePoint.x- Center.x,2);
        float diffY2 = Mathf.Pow(SomePoint.y - Center.y, 2);
        float radius2 = Radius * Radius;

        return diffX2 + diffY2 <= radius2;
    }

    public bool ContainsExclusive(Vector2 SomePoint)
    {
        float diffX2 = Mathf.Pow(SomePoint.x - Center.x, 2);
        float diffY2 = Mathf.Pow(SomePoint.y - Center.y, 2);
        float radius2 = Radius * Radius;

        return diffX2 + diffY2 < radius2;
    }
}

public struct Sphere
{
    public Vector3 Center;
    float Radius;
    float Radius2;

    public Sphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
        Radius2 = Radius * Radius;
    }

    public float Distance(Vector3 Point1, Vector3 Point2)
    {
        return Radius * Mathf.Acos(((Point1.x * Point2.x) +(Point1.y * Point2.y) +(Point1.z * Point2.z)) / Radius2);
    }
}