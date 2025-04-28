using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        None = (1 << 0),
        //All = ~0, 
        PlayerLayer = (1 << 1), 
        MonsterLayer = (1 << 2),  
        CollectibleLayer = (1 << 3),
        ProjectileLayer = (1 << 4),
        StaticObstacleLayer = (1 << 5),
        DynamicObstacleLayer = (1 << 6),
    }
    /// MOVE AWAY
    // Precomputed collision masks
    private static readonly CollisionLayer[] CollisionMasks = new CollisionLayer[7]
    {
        CollisionLayer.None,
        CollisionLayer.MonsterLayer | CollisionLayer.CollectibleLayer | CollisionLayer.StaticObstacleLayer | CollisionLayer.DynamicObstacleLayer,
        CollisionLayer.PlayerLayer | CollisionLayer.MonsterLayer |  CollisionLayer.ProjectileLayer | CollisionLayer.StaticObstacleLayer| CollisionLayer.DynamicObstacleLayer,
        CollisionLayer.PlayerLayer,
        CollisionLayer.MonsterLayer,
        CollisionLayer.PlayerLayer | CollisionLayer.MonsterLayer | CollisionLayer.StaticObstacleLayer | CollisionLayer.DynamicObstacleLayer,
        CollisionLayer.PlayerLayer | CollisionLayer.MonsterLayer | CollisionLayer.StaticObstacleLayer | CollisionLayer.DynamicObstacleLayer,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollisionLayer GetMask(CollisionLayer layer)
    {
        return CollisionMasks[(int)math.log2((uint)layer)];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldCollide(CollisionLayer layerA, CollisionLayer layerB)
    {
        return (GetMask(layerA) & layerB) != 0;
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
    public static float Proximity(AABB A, float2 positon, float radius)
    {

        /*Often wrong direction but doest pose problem for proximity calculs*/
        float minX = Mathf.Min(positon.x - A.LowerBound.x, 0, A.UpperBound.x - positon.x);
        float minY = Mathf.Min(positon.y - A.LowerBound.y, 0, A.UpperBound.y - positon.y);
        float pX = positon.x + (minX * (Mathf.Sign(positon.x)));
        float pY = positon.y + (minY * (Mathf.Sign(positon.y)));

        //Debug purpose
        //Debug.DrawLine(new Vector3(B.Position.x, B.Position.y, 0), new Vector3(pX, pY,0), Color.red, 0.1f);

        float dX = positon.x - pX;
        float dY = positon.y - pY;

        float distance = Mathf.Sqrt(dX * dX + dY * dY);

        //Debug.LogError(distance - B.radius);

        return distance - radius;
    }

    public static float Proximity(float2 Apositon, float Aradius, float2 Bpositon, float Bradius)
    {

        // Calculate squared distance between the centers of the circles
        float deltaX = Bpositon.x - Apositon.x;
        float deltaY = Bpositon.y - Apositon.y;
        float distanceSquared = deltaX * deltaX + deltaY * deltaY;

        // Calculate squared sum of radii
        float sumRadiiSquared = (Aradius + Bradius) * (Aradius + Bradius);

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

    /// Sub-optimal, batch raycast instead ?
    public static float Intersect(ShapeData shape, Entity entity,
        in ComponentLookup<CircleShapeData> circleShapeLookUp, in ComponentLookup<BoxShapeData> boxShapeLookUp,
        Ray ray)
    {

        switch (shape.shapeType)
        {
            case ShapeType.Circle:
                var circle = circleShapeLookUp.GetRefRO(entity);
                return IntersectCircle(shape.Position, circle.ValueRO.radius,ray);
            case ShapeType.Box:
                var box = boxShapeLookUp.GetRefRO(entity);
                return IntersectBox(shape.Position,shape.Rotation * Mathf.Deg2Rad, box.ValueRO.dimentions*0.5f,ray);
            default:
                return 0;
        }
    }

    public static float IntersectCircle(Vector2 positon, float radius, Ray ray)
    {
        float maxRange = ray.DirLength.magnitude;
        Vector2 m = ray.Origin - positon;
        float b = Vector2.Dot(m, ray.DirLength.normalized);
        float c = Vector2.Dot(m, m) - radius * radius;

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
    public static float IntersectBox(Vector2 boxCenter, float rotation, Vector2 halfExtents, Ray ray)
    {
        // Step 1: Compute vector from box center to ray origin
        Vector2 delta = ray.Origin - boxCenter;

        // Step 2: Calculate rotation axes from angle
        float cos = Mathf.Cos(rotation);
        float sin = Mathf.Sin(rotation);

        // Rotation matrix
        Vector2 axisX = new Vector2(cos, sin);
        Vector2 axisY = new Vector2(-sin, cos);

        // Step 3: Project ray and delta into box space (no Quaternion, just dot products)
        Vector2 localOrigin = new Vector2(
            Vector2.Dot(delta, axisX),
            Vector2.Dot(delta, axisY)
        );
        Vector2 localDirection = new Vector2(
            Vector2.Dot(ray.DirLength, axisX),
            Vector2.Dot(ray.DirLength, axisY)
        );

        Vector2 invDir = new Vector2(
            localDirection.x != 0.0f ? 1.0f / localDirection.x : float.MaxValue,
            localDirection.y != 0.0f ? 1.0f / localDirection.y : float.MaxValue
        );

        Vector2 t1 = (-halfExtents - localOrigin) * invDir;
        Vector2 t2 = (halfExtents - localOrigin) * invDir;

        float tMin = Mathf.Max(Mathf.Min(t1.x, t2.x), Mathf.Min(t1.y, t2.y));
        float tMax = Mathf.Min(Mathf.Max(t1.x, t2.x), Mathf.Max(t1.y, t2.y));

        // No hit if slabs don't overlap or tMax < 0
        if (tMin > tMax || tMax < 0.0f)
            return -1.0f;

        float maxRange = ray.DirLength.magnitude;

        // If nearest hit is too far, no hit
        if (tMin > maxRange)
            return -1.0f;

        // Choose valid t
        float t = (tMin >= 0.0f) ? tMin : tMax;
        return t;
    }

    /// Move away from physics utils ?
    /// Into a MathUtils ?
    public static float DirectionToRadians(Vector2 dir)
    {

        /*
        output:
              -pi|0
        -pi/2       pi/2
              -0|pi
         */

        //angle in radians
        float radians = Mathf.Atan2(dir.x, dir.y);
        float side = Mathf.Sign(radians);
        //Debug.LogError(radians);

        return (radians*side) +(-side*0.5f+0.5f)*(-Mathf.PI);
    }
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

    // Converts an angle in radians to a direction vector in 2D space according to the DirectionToRadians output
    public static Vector2 RadianToDirection(float radians)
    {
        /// FIX
        /*
        input:
             -pi|pi
        -pi/2       pi/2
              -0|0
        */
        /*
        Input:
            -pi|pi
       -pi/2       1.5pi
             -0|2pi
        */

        float side = Mathf.Sign(-radians);
        // Adjust the angle back from the transformation in DirectionToRadians
        float adjustedRadians = radians + ((-side*0.5f+0.5f) * Mathf.PI);

        //Debug.Log(new Vector2(Mathf.Sin(adjustedRadians), Mathf.Cos(adjustedRadians)));
        // Convert the adjusted radians back to a direction vector
        return new Vector2(Mathf.Sin(adjustedRadians), Mathf.Cos(adjustedRadians));
    }

    public static Vector2 RotatedVector(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        float newX = v.x * cos - v.y * sin;
        float newY = v.x * sin + v.y * cos;

        return new Vector2(newX, newY);
    }

    public static Vector2 Rotatelerp(Vector2 start, Vector2 end, float t)
    {
        // Ensure t is within [0, 1]
        t = Mathf.Clamp01(t);

        /*
        Layout:
              pi/2
        -pi|pi     -0|0
             -pi/2
        */

        /// rotate by 90 for the loopback to happen at the bottom instead of left
        Vector2 rotatedStart = new Vector2(start.y,-start.x);
        Vector2 rotatedEnd = new Vector2(end.y, -end.x);

        // Calculate the angle between the two vectors
        float radRotatedStart = Mathf.Atan2(rotatedStart.y, rotatedStart.x);
        float radRotatedEnd = Mathf.Atan2(rotatedEnd.y, rotatedEnd.x);

        float radRotatedTarget = Mathf.Lerp(radRotatedStart, radRotatedEnd, t);

        // Calculate the length interpolation
        float length = Mathf.Lerp(start.magnitude, end.magnitude, t);

        Vector2 rotatedTarget = new Vector2(Mathf.Cos(radRotatedTarget), Mathf.Sin(radRotatedTarget)) * length;
        /// rotate back to get the right target
        Vector2 Target = new Vector2(-rotatedTarget.y,rotatedTarget.x);

        return Target;
    }

    public static AABB AABBfromShape(Vector2 pos, CircleShapeData circleData)
    {
        return new AABB
        {
            UpperBound = new Vector2(pos.x + circleData.radius, pos.y + circleData.radius),
            LowerBound = new Vector2(pos.x - circleData.radius, pos.y - circleData.radius)
        };
    }
    public static AABB AABBfromShape(Vector2 pos, float rot, BoxShapeData boxData)
    {
        Vector2 halfExtents = boxData.dimentions * 0.5f;
        rot *= Mathf.Deg2Rad;

        float cos = Mathf.Cos(rot);
        float sin = Mathf.Sin(rot);

        // Compute the rotated half extents using absolute value of rotation matrix
        float absCos = Mathf.Abs(cos);
        float absSin = Mathf.Abs(sin);

        Vector2 rotatedHalfExtents = new Vector2(
            halfExtents.x * absCos + halfExtents.y * absSin,
            halfExtents.x * absSin + halfExtents.y * absCos
        );

        Vector2 min = pos - rotatedHalfExtents;
        Vector2 max = pos + rotatedHalfExtents;

        return new AABB { LowerBound = min,UpperBound = max };
    }

    public static void PointSegmentDistance(float2 p, float2 a, float2 b, out float distanceSquared, out float2 closestPoint)
    {
        float2 ab = b - a;
        float2 ap = p - a;

        float proj = math.dot(ap, ab);
        float abLenSq = math.dot(ab, ab);
        float d = proj / abLenSq;

        if (d <= 0f)
        {
            closestPoint = a;
        }
        else if (d >= 1f)
        {
            closestPoint = b;
        }
        else
        {
            closestPoint = a + d * ab;
        }

        distanceSquared = math.lengthsq(p - closestPoint);
    }

    public static bool NearlyEqual(float2 a, float2 b, float epsilon = 1e-4f)
    {
        return math.lengthsq(a - b) < epsilon * epsilon;
    }
    public static bool NearlyEqual(float a, float b, float epsilon = 1e-4f)
    {
        return MathF.Abs(a - b) < epsilon;
    }


    public static float2x2 RotationMatrix(float radians)
    {
        float c = math.cos(radians);
        float s = math.sin(radians);
        return new float2x2(c, -s, s, c);
    }

    public static float CrossProduct(float2 a, float2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

}
