using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public struct PhysicsUtilities
{

    public static float Proximity(AABB A, AABB B)
    {
        // Calculate intersection in the X-axis
        float intersectionX = Mathf.Min(A.UpperBound.x, B.UpperBound.x) - Mathf.Max(A.LowerBound.x, B.LowerBound.x);
        // Calculate intersection in the Y-axis
        float intersectionY = Mathf.Min(A.UpperBound.y, B.UpperBound.y) - Mathf.Max(A.LowerBound.y, B.LowerBound.y);

        // If there's no intersection in either axis, return 0
        //if (intersectionX <= 0 || intersectionY <= 0)
        //    return 0;

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
    //TO VERIFY
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


}
