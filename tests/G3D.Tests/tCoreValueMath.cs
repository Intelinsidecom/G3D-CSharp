using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testColor4uint8()
        {
            printf("G3D::Color4uint8  ");

            Color4uint8 white = new Color4uint8(new Color4(1.0f, 1.0f, 1.0f, 1.0f));
            Check(white.r == 255 && white.g == 255 && white.b == 255 && white.a == 255,
                  "Color4uint8(Color4) saturates at 255");

            Color4uint8 mixed = new Color4uint8(new Color4(0.5f, 0.25f, 0.0f, 1.0f));
            Check(mixed.r == 128, "Color4uint8 red channel");
            Check(mixed.g == 64, "Color4uint8 green channel");
            Check(mixed.b == 0, "Color4uint8 blue channel");
            Check(mixed.a == 255, "Color4uint8 alpha channel");

            Color4uint8 argb = Color4uint8.fromARGB(0x80112233);
            Check(argb.a == 0x80 && argb.r == 0x11 && argb.g == 0x22 && argb.b == 0x33,
                  "Color4uint8.fromARGB");
            Check(argb.asUInt32() == 0x80112233, "Color4uint8.asUInt32");

            byte[] bytes = argb.ToArray();
            Check(bytes.Length == 4 && bytes[0] == argb.r && bytes[1] == argb.g &&
                  bytes[2] == argb.b && bytes[3] == argb.a, "Color4uint8.ToArray");

            Check(argb[0] == argb.r && argb[1] == argb.g &&
                  argb[2] == argb.b && argb[3] == argb.a, "Color4uint8 indexer");

            byte[] buffer = new byte[4];
            argb.CopyTo(buffer, 0);
            Check(buffer[0] == argb.r && buffer[1] == argb.g &&
                  buffer[2] == argb.b && buffer[3] == argb.a, "Color4uint8.CopyTo");

            byte[] serialized = new byte[4];
            BinaryOutput bo = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
            argb.serialize(bo);
            bo.commit(serialized);

            BinaryInput bi = new BinaryInput(serialized, 4, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            Color4uint8 roundTrip = new Color4uint8();
            roundTrip.deserialize(bi);
            Check(roundTrip == argb, "Color4uint8 serialize/deserialize round trip");

            printf("passed\n");
        }

        private static void testCone()
        {
            printf("G3D::Cone  ");

            Cone cone = new Cone(new Vector3(0, 0, 0), new Vector3(0, 1, 0), (float)(g3dmath.G3D_PI / 4.0));

            Check(cone.contains(new Vector3(0, 1, 0)), "Cone contains a point on the axis");
            Check(cone.contains(new Vector3(0, 4, 0)), "Cone contains a distant point on the axis");
            Check(!cone.contains(new Vector3(1, 0, 0)), "Cone excludes a point perpendicular to the axis");
            Check(!cone.contains(new Vector3(10, 1, 0)), "Cone excludes a point beyond the edge");

            Check(cone.intersects(new Sphere(new Vector3(0, 1, 0), 0.1f)),
                  "Cone intersects a sphere inside the cone");
            Check(!cone.intersects(new Sphere(new Vector3(10, 1, 0), 0.1f)),
                  "Cone does not intersect a sphere outside the cone");

            Box box = new Box(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
            Cone boxCone = new Cone(new Vector3(0, 0, -10), box);
            Check(boxCone.contains(box.getCenter()), "Cone(tip, box) contains the box center");
            Check(!boxCone.contains(new Vector3(10, 0, 0)), "Cone(tip, box) excludes points beyond the box");

            printf("passed\n");
        }

        private static void testPhysicsFrame()
        {
            printf("G3D::PhysicsFrame  ");

            PhysicsFrame identity = PhysicsFrame.identity();
            Check(identity.rotation.fuzzyEq(Quat.identity()), "PhysicsFrame.identity rotation");
            Check(identity.translation == Vector3.zero(), "PhysicsFrame.identity translation");

            PhysicsFrame translated = new PhysicsFrame(new Vector3(1, 2, 3));
            Check(translated.rotation.fuzzyEq(Quat.identity()), "PhysicsFrame(translation) has an identity rotation");
            Check(translated.translation == new Vector3(1, 2, 3), "PhysicsFrame(translation) translation");

            PhysicsFrame composed = translated * new PhysicsFrame(new Vector3(0, 1, 0));
            Check(composed.translation.fuzzyEq(new Vector3(1, 3, 3)), "PhysicsFrame operator* translation");

            PhysicsFrame lerped = translated.lerp(new PhysicsFrame(new Vector3(3, 2, 3)), 0.5f);
            Check(lerped.translation.fuzzyEq(new Vector3(2, 2, 3)), "PhysicsFrame lerp translation");

            PhysicsFrame integrated = PhysicsFrame.identity().integrate(2.0f, new PhysicsFrame(new Vector3(1, 0, 0)));
            Check(integrated.translation.fuzzyEq(new Vector3(2, 0, 0)), "PhysicsFrame integrate translation");

            CoordinateFrame frame = translated.toCoordinateFrame();
            Check(frame.translation.fuzzyEq(new Vector3(1, 2, 3)), "PhysicsFrame toCoordinateFrame translation");
            Check(frame.rotation.fuzzyEq(Matrix3.identity()), "PhysicsFrame toCoordinateFrame rotation");

            byte[] serialized = new byte[64];
            BinaryOutput bo = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
            translated.serialize(bo);
            bo.commit(serialized);

            BinaryInput bi = new BinaryInput(serialized, 64, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            PhysicsFrame roundTrip = PhysicsFrame.identity();
            roundTrip.deserialize(bi);
            Check(roundTrip.translation.fuzzyEq(translated.translation), "PhysicsFrame serialize translation");
            Check(roundTrip.rotation.fuzzyEq(translated.rotation), "PhysicsFrame serialize rotation");

            printf("passed\n");
        }

        private static void testGLight()
        {
            printf("G3D::GLight  ");

            GLight light = new GLight();
            Check(light.position == new Vector4(0, 0, 0, 0), "GLight default position");
            Check(light.color == Color3.white(), "GLight default color");
            Check(light.spotDirection == new Vector3(0, 0, -1), "GLight default spot direction");
            Check(light.spotCutoff == 180, "GLight default spot cutoff");
            Check(!light.enabled, "GLight default enabled");
            Check(light.specular && light.diffuse, "GLight default specular and diffuse");
            Check(light.attenuation[0] == 1.0 && light.attenuation[1] == 0.0 && light.attenuation[2] == 0.0,
                  "GLight default attenuation");

            GLight directional = GLight.directional(new Vector3(0, 2, 0), Color3.white());
            Check(directional.position.w == 0, "GLight.directional has a zero w coordinate");
            Check(directional.position.xyz().fuzzyEq(new Vector3(0, 1, 0)), "GLight.directional normalizes its direction");
            Check(directional.specular && directional.diffuse, "GLight.directional specular and diffuse");

            GLight point = GLight.point(new Vector3(1, 2, 3), Color3.white(), 2.0, 3.0, 4.0);
            Check(point.position.w == 1, "GLight.point has a one w coordinate");
            Check(point.position.xyz() == new Vector3(1, 2, 3), "GLight.point position");
            Check(point.attenuation[0] == 2.0 && point.attenuation[1] == 3.0 && point.attenuation[2] == 4.0,
                  "GLight.point attenuation");

            GLight spot = GLight.spot(new Vector3(1, 2, 3), new Vector3(0, 0, -2), 30.0, Color3.white());
            Check(spot.position.xyz() == new Vector3(1, 2, 3), "GLight.spot position");
            Check(spot.spotDirection == new Vector3(0, 0, -2), "GLight.spot does not normalize its direction");
            Check(spot.spotCutoff == 30.0, "GLight.spot cutoff");

            Check(GLight.directional(new Vector3(0, 1, 0), Color3.white()) ==
                  GLight.directional(new Vector3(0, 1, 0), Color3.white()), "GLight equality");
            Check(GLight.directional(new Vector3(0, 1, 0), Color3.white()) !=
                  GLight.directional(new Vector3(0, 1, 0), Color3.black()), "GLight inequality");

            GLight copy = new GLight(directional);
            copy.color = Color3.black();
            Check(directional.color == Color3.white(), "GLight copy constructor copies the color");
            Check(copy.attenuation[0] == directional.attenuation[0], "GLight copy constructor copies attenuation");

            printf("passed\n");
        }

        private static void testVectorMath()
        {
            printf("G3D::vectorMath  ");

            Matrix3 a = Matrix3.fromAxisAngle(Vector3.unitY(), 0.5f);
            Matrix3 b = Matrix3.fromAxisAngle(Vector3.unitX(), 0.25f);

            Check(VectorMath.mul(a, b).fuzzyEq(a * b), "vectorMath mul(Matrix3, Matrix3)");
            Check(VectorMath.mul(a, new Vector3(1, 2, 3)).fuzzyEq(a * new Vector3(1, 2, 3)),
                  "vectorMath mul(Matrix3, Vector3)");

            Check(g3dmath.fuzzyEq(VectorMath.dot(new Vector2(1, 2), new Vector2(3, 4)), 11.0), "vectorMath dot(Vector2)");
            Check(g3dmath.fuzzyEq(VectorMath.dot(new Vector3(1, 2, 3), new Vector3(4, 5, 6)), 32.0), "vectorMath dot(Vector3)");
            Check(g3dmath.fuzzyEq(VectorMath.dot(new Vector4(1, 2, 3, 4), new Vector4(5, 6, 7, 8)), 70.0), "vectorMath dot(Vector4)");

            Check(VectorMath.normalize(new Vector3(0, 3, 0)).fuzzyEq(new Vector3(0, 1, 0)), "vectorMath normalize(Vector3)");
            Check(VectorMath.normalize(new Vector2(3, 4)).fuzzyEq(new Vector2(0.6f, 0.8f)), "vectorMath normalize(Vector2)");
            Check(VectorMath.normalize(new Vector4(0, 0, 0, 2)).fuzzyEq(new Vector4(0, 0, 0, 1)), "vectorMath normalize(Vector4)");

            Check(VectorMath.abs(new Vector3(-1, 2, -3)) == new Vector3(1, 2, 3), "vectorMath abs(Vector3)");
            Check(VectorMath.abs(new Vector2(-1, 2)) == new Vector2(1, 2), "vectorMath abs(Vector2)");
            Check(VectorMath.abs(new Vector4(-1, 2, -3, 4)) == new Vector4(1, 2, 3, 4), "vectorMath abs(Vector4)");

            Check(VectorMath.all(new Vector3(1, 2, 3)), "vectorMath all(Vector3)");
            Check(!VectorMath.all(new Vector3(1, 0, 3)), "vectorMath all(Vector3) with a zero component");
            Check(VectorMath.any(new Vector3(0, 0, 3)), "vectorMath any(Vector3)");
            Check(!VectorMath.any(new Vector3(0, 0, 0)), "vectorMath any(Vector3) with no nonzero component");

            Check(VectorMath.clamp(new Vector3(-1, 0.5f, 2), Vector3.zero(), new Vector3(1, 1, 1)) ==
                  new Vector3(0, 0.5f, 1), "vectorMath clamp(Vector3)");

            Check(VectorMath.lerp(new Vector3(0, 0, 0), new Vector3(2, 4, 6), 0.5f) == new Vector3(1, 2, 3),
                  "vectorMath lerp(Vector3)");
            Check(VectorMath.lerp(new Vector3(0, 0, 0), new Vector3(2, 4, 6), 0.5f).fuzzyEq(new Vector3(1, 2, 3)),
                  "vectorMath lerp(Vector3) value");
            Check(g3dmath.fuzzyEq(VectorMath.lerp(new Color3(0, 0, 0), new Color3(1, 1, 1), 0.5f).r, 0.5),
                  "vectorMath lerp(Color3)");

            Check(VectorMath.cross(new Vector3(1, 0, 0), new Vector3(0, 1, 0)) == new Vector3(0, 0, 1),
                  "vectorMath cross");

            Check(g3dmath.fuzzyEq(VectorMath.determinant(Matrix3.identity()), 1.0), "vectorMath determinant(Matrix3)");
            Check(g3dmath.fuzzyEq(VectorMath.determinant(Matrix4.identity()), 1.0), "vectorMath determinant(Matrix4)");

            Check(VectorMath.min(new Vector3(1, 5, 3), new Vector3(4, 2, 6)) == new Vector3(1, 2, 3),
                  "vectorMath min(Vector3)");
            Check(VectorMath.max(new Vector3(1, 5, 3), new Vector3(4, 2, 6)) == new Vector3(4, 5, 6),
                  "vectorMath max(Vector3)");
            Check(VectorMath.min(new Color3(1, 5, 3), new Color3(4, 2, 6)) == new Color3(1, 2, 3),
                  "vectorMath min(Color3)");
            Check(VectorMath.max(new Color3(1, 5, 3), new Color3(4, 2, 6)) == new Color3(4, 5, 6),
                  "vectorMath max(Color3)");
            Check(VectorMath.min(new Color4(1, 5, 3, 2), new Color4(4, 2, 6, 7)) == new Color4(1, 2, 3, 2),
                  "vectorMath min(Color4)");
            Check(VectorMath.max(new Color4(1, 5, 3, 2), new Color4(4, 2, 6, 7)) == new Color4(4, 5, 6, 7),
                  "vectorMath max(Color4)");

            Check(VectorMath.sign(new Vector3(-2, 0, 3)) == new Vector3(-1, 0, 1), "vectorMath sign(Vector3)");
            Check(VectorMath.sign(new Vector2(-2, 3)) == new Vector2(-1, 1), "vectorMath sign(Vector2)");

            Check(g3dmath.fuzzyEq(VectorMath.length(-4.0f), 4.0), "vectorMath length(float)");
            Check(g3dmath.fuzzyEq(VectorMath.length(new Vector3(0, 3, 4)), 5.0), "vectorMath length(Vector3)");

            Check(g3dmath.fuzzyEq(VectorMath.log(new Color3(1, 1, 1)).r, 0.0), "vectorMath log(Color3)");
            Check(g3dmath.fuzzyEq(VectorMath.log(new Color3(2, 4, 8)).g, global::System.Math.Log(4.0f)),
                  "vectorMath log(Color3) value");

            printf("passed\n");
        }

        private static void testVectorConversions()
        {
            printf("G3D::Vector conversions  ");

            Vector3int16 packed = new Vector3int16(-2, 3, 7);
            Vector3 unpacked = new Vector3(packed);
            Check(unpacked == new Vector3(-2, 3, 7), "Vector3(Vector3int16)");

            Vector4 color = new Vector4(new Color4(0.25f, 0.5f, 0.75f, 1.0f));
            Check(color == new Vector4(0.25f, 0.5f, 0.75f, 1.0f), "Vector4(Color4)");

            printf("passed\n");
        }

        private static void testCoreValueMath()
        {
            testColor4uint8();
            testCone();
            testPhysicsFrame();
            testGLight();
            testVectorMath();
            testVectorConversions();
        }
    }
}
