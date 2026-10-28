using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testVAR()
        {
            printf("VAR\n");

            {
                VAR var = VAR.create(VAR.Type.VERTEX, 3, VAR.Usage.STATIC);
                Debug.debugAssert(var.getType() == VAR.Type.VERTEX);
                Debug.debugAssert(var.getStride() == 12);
                Debug.debugAssert(var.getUsage() == VAR.Usage.STATIC);

                Vector3[] vertices = new Vector3[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9) };
                int version = var.getDataVersion();
                var.setData(vertices);

                Debug.debugAssert(var.getDataVersion() != version);
                Debug.debugAssert(var.getCount() == 3);
                Debug.debugAssert(var.getData().Length == 3 * 12);

                Vector3[] result = var.getData<Vector3>();
                Debug.debugAssert(result.Length == 3);
                for (int i = 0; i < 3; ++i)
                {
                    Debug.debugAssert(result[i] == vertices[i]);
                }
            }

            {
                VAR var = VAR.create(VAR.Type.INDEX, 3, VAR.Usage.STATIC);
                Debug.debugAssert(var.getStride() == 4);

                uint[] indices = new uint[] { 0, 1, 2 };
                var.setData(indices);

                Debug.debugAssert(var.getCount() == 3);
                Debug.debugAssert(var.getData().Length == 3 * 4);

                uint[] result = var.getData<uint>();
                Debug.debugAssert(result.Length == 3);
                for (int i = 0; i < 3; ++i)
                {
                    Debug.debugAssert(result[i] == indices[i]);
                }
            }

            {
                VAR var = VAR.create(VAR.Type.INDEX, 3, VAR.Usage.DYNAMIC);
                var.setData(new uint[] { 0, 1, 2 });

                var.setSubData(4, new uint[] { 7, 8 });

                Debug.debugAssert(var.getCount() == 3);
                uint[] result = var.getData<uint>();
                Debug.debugAssert(result.Length == 3);
                Debug.debugAssert(result[0] == 0);
                Debug.debugAssert(result[1] == 7);
                Debug.debugAssert(result[2] == 8);
            }

            printf("  passed\n");
        }
    }
}
