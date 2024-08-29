using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public struct Ray
{
    public Vector2 Origin;
    public Vector2 DirLength;
}
public struct RayCastHit
{
    public Entity entity;
    public float distance;
}

public struct PhysicsUtilities
{
    [Flags]
    public enum CollisionLayer
    {
        //None = 0,
        //All = ~0,         // Represents all layers (1111...)
        PlayerLayer = (1 << 0),    // 0001
        MonsterLayer = (1 << 1),    // 0010
        Unassigned = (1 << 2),    // 0100
                                // Add more layers as needed
    }

    

    public static float Proximity(AABB A, AABB B)
    {
        // Calculate intersection in the X-axis
        float intersectionX = Mathf.Min(A.UpperBound.x, B.UpperBound.x) - Mathf.Max(A.LowerBound.x, B.LowerBound.x);
        // Calculate intersection in the Y-axis
        float intersectionY = Mathf.Min(A.UpperBound.y, B.UpperBound.y) - Mathf.Max(A.LowerBound.y, B.LowerBound.y);

        //return the area of the intersection
        // < 0 -> out ; > 0 -> in
        return Mathf.Sign(Mathf.Max(intersectionX, intersectionY)) * (intersectionX * intersectionY);
    }
    public static float Proximity(AABB A, CircleShapeData B)
    {

        /*Often wrong direction but doest pose problem for proximity calculs*/
        float minX = Mathf.Min(B.Position.x - A.LowerBound.x, 0, A.UpperBound.x - B.Position.x);
        float minY = Mathf.Min(B.Position.y - A.LowerBound.y, 0, A.UpperBound.y - B.Position.y);
        float pX = B.Position.x + (minX * (Mathf.Sign(B.Position.x)));
        float pY = B.Position.y + (minY * (Mathf.Sign(B.Position.y)));

        //Debug purpose
        //Debug.DrawLine(new Vector3(B.Position.x, B.Position.y, 0), new Vector3(pX, pY,0), Color.red, 0.1f);

        float dX = B.Position.x - pX;
        float dY = B.Position.y - pY;

        float distance = Mathf.Sqrt(dX * dX + dY * dY);

        //Debug.LogError(distance - B.radius);

        return distance - B.radius;
    }

    public static float Proximity(CircleShapeData A, CircleShapeData B)
    {

        // Calculate squared distance between the centers of the circles
        float deltaX = B.Position.x - A.Position.x;
        float deltaY = B.Position.y - A.Position.y;
        float distanceSquared = deltaX * deltaX + deltaY * deltaY;

        // Calculate squared sum of radii
        float sumRadiiSquared = (A.radius + B.radius) * (A.radius + B.radius);

        // Calculate squared proximity (squared distance - squared sum of radii)
        float proximitySquared = distanceSquared - sumRadiiSquared;

        // < 0 -> out ; > 0 -> in
        return proximitySquared;
    }

    public static bool PointInsideShape(Vector2 point, AABB shape)
    {
        return (shape.LowerBound.x <= point.x && point.x <= shape.UpperBound.x) && (shape.LowerBound.y <= point.y && point.y <= shape.UpperBound.y);
    }

    public static float Intersect(AABB A, Ray B)
    {
        Vector2 dirfrac = new Vector2(1.0f / B.DirLength.x, 1.0f / B.DirLength.y);

        float t1 = (A.LowerBound.x - B.Origin.x) * dirfrac.x;
        float t2 = (A.UpperBound.x - B.Origin.x) * dirfrac.x;
        float t3 = (A.LowerBound.y - B.Origin.y) * dirfrac.y;
        float t4 = (A.UpperBound.y - B.Origin.y) * dirfrac.y;

        float tmin = Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4));
        float tmax = Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4));

        // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
        if (tmax < 0)
        {
            return -1f;
        }

        // if tmin > tmax, ray doesn't intersect AABB
        if (tmin > tmax)
        {
            return -1f;
        }

        // tmin is the distance to the intersection point ; tmin negative if origin is inside
        return Mathf.Abs(tmin);


    }

    public static float Intersect(CircleShapeData circle, Ray ray)
    {
        float maxRange = ray.DirLength.magnitude;
        Vector2 m = ray.Origin - circle.Position;
        float b = Vector2.Dot(m, ray.DirLength.normalized);
        float c = Vector2.Dot(m, m) - circle.radius * circle.radius;

        float discriminant = b * b - c;

        // A negative discriminant corresponds to a ray missing the circle
        if (discriminant < 0.0f)
            return -1.0f;

        // Compute the smallest t value of the intersection points
        float t1 = -b - Mathf.Sqrt(discriminant);
        float t2 = -b + Mathf.Sqrt(discriminant);

        // Determine the smallest positive t value within the range [0, maxRange]
        float t = Mathf.Min(t1, t2); // Take the minimum value to ensure non-negative

        // Check if t is within the valid range 
        if (t > maxRange)
            return -1.0f;

        // Ensure discriminant is positive and t is within the valid range, else return -1.0f
        float noIntersectionMask = Mathf.Sign(discriminant) * Mathf.Sign(t); // If either is negative, return -1.0f
        return t * noIntersectionMask;
    }

    //Move away from physics utils ?
    public static float DirectionToRadians(Vector2 dir)
    {

        /*
        output:
             -pi|pi
        -pi/2       pi/2
              -0|0
         */

        //angle in radians
        float radians = Mathf.Atan2(dir.x, dir.y);

        //Debug.LogError(radians);

        return (Mathf.Sign(radians) * Mathf.PI) - (radians);
    }
    //Move away from physics utils ?
    public static float DirectionToRadians(Vector2 dir, float offset)
    {

        /*
        output - offset: 
             -pi|pi
        -pi/2       pi/2
              -0|0
         */

        //angle in radians
        float radians = Mathf.Atan2(dir.x, dir.y) + offset;

        //Debug.LogError(radians);

        return (Mathf.Sign(radians) * Mathf.PI) - (radians);
    }

    // Converts an angle in radians to a direction vector in 2D space
    public static Vector2 RadianToDirection2D(float radians)
    {
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }


}
