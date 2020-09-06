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