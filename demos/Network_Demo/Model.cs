/**
  @file Network_Demo/Model.cs

  Port of Network_Demo/Model.h and Model.cpp.
 */

using System;
using G3D;

namespace Network_Demo
{
    public class Model : ReferenceCountedObject
    {
    }

    /**
      Overrides the material and position settings. Used by Helicopter to
      override values set inside posed models so that they can be reused
      without being re-posed.
     */
    public class PositionOverride : Shader
    {
        public GMaterial material = new GMaterial();
        public CoordinateFrame cframe = CoordinateFrame.identity();

        public override void beforePrimitive(RenderDevice renderDevice)
        {
            renderDevice.setObjectToWorldMatrix(cframe);
            material.configure(renderDevice);
        }

        public override void afterPrimitive(RenderDevice renderDevice)
        {
        }
    }

    public class Helicopter : Model
    {
        private ReferenceCountedPointer<PosedModel> body;
        private ReferenceCountedPointer<PosedModel> rotor;
        private PositionOverride positionShader;

        private Helicopter(string dataDir)
        {
            ReferenceCountedPointer<IFSModel> bodyModel = IFSModel.create(dataDir + "ifs/ah64-body.ifs", 8.5);
            ReferenceCountedPointer<IFSModel> rotorModel = IFSModel.create(dataDir + "ifs/ah64-rotor.ifs", 8.5);

            body = bodyModel.pointer().pose();
            rotor = rotorModel.pointer().pose();
            positionShader = new PositionOverride();
        }

        public static ReferenceCountedPointer<Helicopter> create(string dataDir)
        {
            return new ReferenceCountedPointer<Helicopter>(new Helicopter(dataDir));
        }

        public void render(
            RenderDevice rd,
            CoordinateFrame cframe,
            Pose pose,
            GMaterial material)
        {
            rd.setShader(positionShader);

            // The original GMaterial has a Color4 "color" and a double
            // "specularCoefficient"; this port's GMaterial stores the equivalent
            // as separate diffuse/specular colors and a float shininess.
            positionShader.material.diffuseColor = material.diffuseColor;
            positionShader.material.specularColor = material.specularColor;
            positionShader.material.shininess = material.shininess;
            positionShader.cframe = cframe;
            body.pointer().render(rd);

            positionShader.material.diffuseColor = Color3.black();

            Matrix3 rot1 = Matrix3.fromAxisAngle(Vector3.UNIT_Z, (float)pose.rotorAngle);
            Matrix3 rot2 = Matrix3.fromAxisAngle(Vector3.UNIT_X, (float)g3dmath.toRadians(-90));

            positionShader.cframe.rotation = positionShader.cframe.rotation * rot2 * rot1;
            positionShader.cframe.translation +=
                cframe.rotation.getColumnVector(1) * 0.85f +
                cframe.rotation.getColumnVector(2) * -2.25f;

            rotor.pointer().render(rd);

            rd.setShader(null);
        }
    }
}
