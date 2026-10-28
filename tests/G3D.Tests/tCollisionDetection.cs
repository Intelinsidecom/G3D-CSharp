using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void measureTriangleCollisionPerformance()
        {
            printf("----------------------------------------------------------\n");
            {
                ulong raw = 0, opt = 0;

                Vector3 v0 = new Vector3(0, 0, 0);
                Vector3 v1 = new Vector3(0, 0, -1);
                Vector3 v2 = new Vector3(-1, 0, 0);
                Sphere sphere = new Sphere(new Vector3(.5f, 1, -.5f), 1);
                Vector3 vel = new Vector3(0, -1, 0);
                Vector3 location, normal;
                Triangle triangle = new Triangle(v0, v1, v2);
                int n = 1024;
                int i;

                System.beginCycleCount(ref raw);
                for (i = 0; i < n; ++i)
                {
                    float t = CollisionDetection.collisionTimeForMovingSphereFixedTriangle(sphere, vel, new Triangle(v0, v1, v2), out location, out normal);
                }
                System.endCycleCount(ref raw);

                System.beginCycleCount(ref opt);
                for (i = 0; i < n; ++i)
                {
                    float t = CollisionDetection.collisionTimeForMovingSphereFixedTriangle(sphere, vel, triangle, out location, out normal);
                }
                System.endCycleCount(ref opt);

                printf("Sphere-Triangle collision detection on 3 vertices: %d cycles\n", (int)(raw / (ulong)n));
                printf("Sphere-Triangle collision detection on Triangle:   %d cycles\n", (int)(opt / (ulong)n));
            }
            {
                ulong raw = 0;

                Vector3 v0 = new Vector3(0, 0, 0);
                Vector3 v1 = new Vector3(0, 0, -1);
                Vector3 v2 = new Vector3(-1, 0, 0);
                Sphere sphere = new Sphere(new Vector3(.5f, 1, -.5f), 1);
                Vector3 vel = new Vector3(0, -1, 0);
                Vector3 location, normal;
                Triangle triangle = new Triangle(v0, v1, v2);
                int n = 1024;
                int i;
                Ray ray = Ray.fromOriginAndDirection(new Vector3(3, -1, -.25f), vel);

                System.beginCycleCount(ref raw);
                for (i = 0; i < n; ++i)
                {
                    float t = ray.intersectionTime(triangle);
                }
                System.endCycleCount(ref raw);
                printf("Miss:\n");
                printf("ray.intersectionTime(triangle): %d cycles\n", (int)(raw / (ulong)n));
            }
            {
                ulong raw = 0;

                Vector3 v0 = new Vector3(0, 0, 0);
                Vector3 v1 = new Vector3(0, 0, -1);
                Vector3 v2 = new Vector3(-1, 0, 0);
                Vector3 vel = new Vector3(0, -1, 0);
                Vector3 location, normal;
                Triangle triangle = new Triangle(v0, v1, v2);
                int n = 1024;
                int i;
                Ray ray = Ray.fromOriginAndDirection(new Vector3(-.15f, 1, -.15f), vel);

                System.beginCycleCount(ref raw);
                for (i = 0; i < n; ++i)
                {
                    float t = ray.intersectionTime(triangle);
                }
                System.endCycleCount(ref raw);

                printf("Hit:\n");
                printf("ray.intersectionTime(triangle): %d cycles\n", (int)(raw / (ulong)n));
            }
        }

        private static void measureAABoxCollisionPerformance()
        {
            printf("----------------------------------------------------------\n");

            ulong raw = 0, opt = 0;

            AABox aabox = new AABox(new Vector3(-1, -1, -1), new Vector3(1, 2, 3));
            Box box = aabox.toBox();

            Vector3 pt1 = new Vector3(0, 10, 0);
            Vector3 vel1 = new Vector3(0, -1, 0);
            Vector3 location, normal;
            int n = 1024;
            int i;

            System.beginCycleCount(ref raw);
            for (i = 0; i < n; ++i)
            {
                float t = CollisionDetection.collisionTimeForMovingPointFixedBox(
                    pt1, vel1, box, out location, out normal);
            }
            System.endCycleCount(ref raw);

            System.beginCycleCount(ref opt);
            for (i = 0; i < n; ++i)
            {
                float t = CollisionDetection.collisionTimeForMovingPointFixedAABox(
                    pt1, vel1, aabox, out location);
            }
            System.endCycleCount(ref opt);

            printf("Ray-Box:   %d cycles\n", (int)(raw / (ulong)n));
            printf("Ray-AABox: %d cycles\n", (int)(opt / (ulong)n));
        }

        private static void testCollisionDetection()
        {
            printf("CollisionDetection ");

            {
                Vector3 pos = new Vector3(2.0f, 0.0f, 2.5f);
                Vector3 dir = new Vector3(-1.0f, 0.0f, -1.0f);
                dir.unitize();

                Box myBox = new Box(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
                Vector3 loc, normal;

                float t = CollisionDetection.collisionTimeForMovingPointFixedBox(pos, dir, myBox, out loc, out normal);
                Debug.debugAssert(normal.fuzzyEq(new Vector3(0, 0, 1)));
            }


            {
                Sphere s = new Sphere(new Vector3(0, 1, 0), 1);
                Plane p = Plane.fromEquation(0, 1, 0, 0);
                Array<Vector3> contact = new Array<Vector3>();
                Array<Vector3> outNormal = new Array<Vector3>();

                float depth = CollisionDetection.penetrationDepthForFixedSphereFixedPlane(
                    s, p, contact, outNormal);

                Debug.debugAssert(outNormal[0] == p.normal());
                Debug.debugAssert(contact[0] == Vector3.zero());
                Debug.debugAssert(depth == 0);

                s = new Sphere(new Vector3(0, 2, 0), 1);

                depth = CollisionDetection.penetrationDepthForFixedSphereFixedPlane(
                    s, p, contact, outNormal);

                Debug.debugAssert(depth < 0);
            }

            {
                Sphere s = new Sphere(new Vector3(0, 1, 0), 1);
                Sphere r = new Sphere(new Vector3(0, -.5f, 0), 1);
                Array<Vector3> contact = new Array<Vector3>();
                Array<Vector3> outNormal = new Array<Vector3>();

                float depth = CollisionDetection.penetrationDepthForFixedSphereFixedSphere(
                    s, r, contact, outNormal);

                Debug.debugAssert(outNormal[0] == new Vector3(0, -1, 0));
                Debug.debugAssert(depth == .5f);
            }

            {
                Sphere s = new Sphere(new Vector3(5, 0, 0), 1);
                Sphere f = new Sphere(new Vector3(4.5f, 0, 0), 1);
                Debug.debugAssert(CollisionDetection.fixedSolidSphereIntersectsFixedSolidSphere(s, f));
            }

            {
                Sphere s = new Sphere(new Vector3(5, 0, 0), 1);
                Sphere f = new Sphere(new Vector3(1, 0, 0), 2);
                Debug.debugAssert(!CollisionDetection.fixedSolidSphereIntersectsFixedSolidSphere(s, f));
            }

            {
                Sphere s = new Sphere(new Vector3(5, 0, 0), 1);
                Box f = new Box(new Vector3(-1, -1.5f, -2), new Vector3(4.5f, 2, 8));
                Debug.debugAssert(CollisionDetection.fixedSolidSphereIntersectsFixedSolidBox(s, f));
            }

            {
                Sphere s = new Sphere(new Vector3(0, 0, 0), .1f);
                Box f = new Box(new Vector3(-1, -1.5f, -2), new Vector3(4.5f, 2, 8));
                Debug.debugAssert(CollisionDetection.fixedSolidSphereIntersectsFixedSolidBox(s, f));
            }

            {
                Sphere s = new Sphere(new Vector3(6, 0, 0), 1);
                Box f = new Box(new Vector3(-1, -1.5f, -2), new Vector3(4.5f, 2, 8));
                Debug.debugAssert(!CollisionDetection.fixedSolidSphereIntersectsFixedSolidBox(s, f));
            }

            {
                Vector3 v0 = new Vector3(0, 0, 0);
                Vector3 v1 = new Vector3(0, 0, -1);
                Vector3 v2 = new Vector3(-1, 0, 0);
                Ray ray = Ray.fromOriginAndDirection(new Vector3(-.15f, 1, -.15f), new Vector3(0, -1, 0));
                Vector3 location, normal;
                float t = ray.intersectionTime(v0, v1, v2);
                Debug.debugAssert(t == 1.0f);

                ray.origin.y = -ray.origin.y;
                t = ray.intersectionTime(v0, v1, v2);
                Debug.debugAssert(t == (float)g3dmath.inf());

                // One-sided test
                ray.direction.y = -ray.direction.y;
                ray.origin.y = -ray.origin.y;
                t = ray.intersectionTime(v0, v1, v2);
                Debug.debugAssert(t == (float)g3dmath.inf());

                // Time scale
                ray = Ray.fromOriginAndDirection(new Vector3(-.15f, 1, -.15f), new Vector3(0, -2, 0));
                t = ray.intersectionTime(v0, v1, v2);

                Debug.debugAssert(t == 0.5f);
            }

            printf("passed\n");
        }

        private static void perfCollisionDetection()
        {
            measureTriangleCollisionPerformance();
            measureAABoxCollisionPerformance();
        }
    }
}
