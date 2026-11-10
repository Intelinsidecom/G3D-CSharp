/**
  @file Network_Demo/Entity.cs

  Port of Network_Demo/Entity.h and Entity.cpp.

  One of the objects in the world.
 */

using System;
using G3D;

namespace Network_Demo
{
    public class Entity
    {
        public const int NO_ID = 0;

        public enum ModelType { NONE = 0, HELICOPTER }

        /**
          Time period over which positions are lerped between
          the client value and the server value when there
          is a discrepancy.
         */
        public static readonly double networkLerpTime = 0.2;

        public int id;

        public Color3 color;

        public Pose pose = new Pose();

        public ModelType modelType;

        public string name = "";

        public PhysicsFrame frame = PhysicsFrame.identity();

        /** The difference of the new and old values due to network
            synchronization.  Used in smoothCoordinateFrame.
            On the  client side, this is used to prevent jerky motion
            due to discrepancies from server information... we
            interpolate between old and new positions over a short
            period of time to hide the jump (although simulation
            proceeds with the new frame immediately).  We store the
            difference between old and new frames so that we aren't
            interpolating from a static frame.

            Not used on the server side.  Not sent when the Entity
            is serialized. */
        public PhysicsFrame oldDeltaFrame = PhysicsFrame.identity();

        /** System::getTick() time at which oldFrame was frozen */
        public double oldFrameTime;

        public Vector3 velocity;

        /** When the controls change, the orientation matrix can't change
            immediately.  Tracked only for visualization.*/
        public Vector3 oldDesiredVelocity;

        public double oldDesiredVelocityTime;

        /** Ephemeral.  Used for the 'tip' matrix */
        public Vector3 currentTiltVelocity;

        /** Rotation due to movement. Ephemeral; tracked only for visualization. */
        public Matrix3 tip = Matrix3.identity();

        public Controls controls = new Controls();

        public Entity()
        {
            id = NO_ID;
            oldFrameTime = -g3dmath.inf();
            velocity = Vector3.zero();
            oldDesiredVelocityTime = -100;
            tip = Matrix3.identity();
        }

        /** Up-to-date coordinate frame */
        public CoordinateFrame coordinateFrame()
        {
            CoordinateFrame c = frame.toCoordinateFrame();
            c.rotation = c.rotation * tip;
            return c;
        }

        /** Coordinate frame interpolated between frame and oldFrame.
            @param now System::getTick*/
        public CoordinateFrame smoothCoordinateFrame(double now)
        {
            return smoothCoordinateFrame(now, global::G3D.System.getTick());
        }

        public CoordinateFrame smoothCoordinateFrame()
        {
            return smoothCoordinateFrame(-1, global::G3D.System.getTick());
        }

        private CoordinateFrame smoothCoordinateFrame(double now, double actualNow)
        {
            double useNow = (now < 0) ? actualNow : now;

            // Amount of *old* time to blend in.
            double alpha = g3dmath.clamp(1.0 - (useNow - oldFrameTime) / networkLerpTime, 0.0, 1.0);

            if (alpha == 0.0)
            {
                // No lerping necessary; the lerp-period has expired
                CoordinateFrame c = frame.toCoordinateFrame();
                c.rotation = c.rotation * tip;
                return c;
            }
            else
            {
                // Compose: position = current + delta*scale;
                PhysicsFrame scaledDelta = PhysicsFrame.identity().lerp(oldDeltaFrame, (float)alpha);
                PhysicsFrame current = PhysicsFrame.identity();
                current.translation = frame.translation + scaledDelta.translation;
                current.rotation = frame.rotation * scaledDelta.rotation;

                CoordinateFrame c = current.toCoordinateFrame();
                c.rotation = c.rotation * tip;
                return c;
            }
        }

        /** Makes an update message that can be sent between client and server or vice versa. */
        public void makeStateMessage(EntityStateMessage msg)
        {
            msg.id = id;
            msg.frame = frame;
            msg.velocity = velocity;
            msg.controls = controls;
        }

        /** Update this entity's state from the message on the client side.
            (The server side only updates the controls, not the state.)
            If localID == id, the controls are not updated because this
            entity is being controlled locally. */
        public void clientUpdateFromStateMessage(EntityStateMessage msg, int localID)
        {
            Debug.debugAssert(id == msg.id);

            // oldDeltaFrame = correct - estimated
            oldDeltaFrame.translation = frame.translation - msg.frame.translation;
            oldDeltaFrame.rotation = frame.rotation * msg.frame.rotation.inverse();
            oldFrameTime = global::G3D.System.getTick();
            frame = msg.frame;
            velocity = msg.velocity;

            if (localID != id)
            {
                // Leave the local controls alone
                oldDesiredVelocityTime = oldFrameTime;
                oldDesiredVelocity = currentTiltVelocity;
                controls = msg.controls;
            }
        }

        public void doSimulation(SimTime dt)
        {
            // The simulation and control scheme for the demo is ridiculous-- it is
            // just for demonstration purposes and needs to be replaced for any
            // reasonable simulator.

            CoordinateFrame cframe = coordinateFrame();
            pose.rotorAngle = g3dmath.wrap(pose.rotorAngle - (double)dt * 20, -g3dmath.twoPi() * 100, g3dmath.twoPi() * 100);

            Vector3 acceleration = Vector3.zero();

            for (int i = 0; i < 3; ++i)
            {
                const double maxAccel = 10;
                double a = VectorComponent.get(controls.desiredVelocity, i) - VectorComponent.get(velocity, i);
                // Obey maximum acceleration and don't overshoot the target velocity
                double component = g3dmath.sign(a) * Math.Min(g3dmath.abs(a) / (double)dt, maxAccel);
                VectorComponent.set(ref acceleration, i, component);
            }

            velocity = velocity + acceleration * (float)dt;
            frame.translation += velocity;

            double dYaw = controls.desiredYawVelocity * (double)dt;
            frame.rotation = Quat.fromAxisAngleRotation(Vector3.unitY(), (float)dYaw) * frame.rotation;

            // Compute the orientation of the craft based on movement.
            {
                const double tiltLerpTime = 0.25;

                // World space velocity affecting tilt
                currentTiltVelocity = controls.desiredVelocity;

                double alpha = g3dmath.clamp((global::G3D.System.time() - oldDesiredVelocityTime) / tiltLerpTime, 0.0, 1.0);

                if (alpha < 1.0)
                {
                    // Use cosine interpolation rate
                    alpha = (1 - Math.Cos(alpha * g3dmath.pi())) * 0.5;
                    currentTiltVelocity = oldDesiredVelocity.lerp(controls.desiredVelocity, (float)alpha);
                }

                // Object space velocity affecting tilt

                Matrix3 localAxes = frame.rotation.toRotationMatrix();
                double dx = localAxes.getColumnVector(0).dot(currentTiltVelocity) * .1;
                double dz = localAxes.getColumnVector(2).dot(currentTiltVelocity) * .1;

                Vector3 Y = new Vector3((float)dx, 1.0f, (float)dz).direction();
                Vector3 X = (Vector3.unitX() - Y * (float)dx).direction();
                Vector3 Z = X.cross(Y);
                tip.setColumn(0, X);
                tip.setColumn(1, Y);
                tip.setColumn(2, Z);
            }
        }

        /** Called from CreateEntityMessage::serialize */
        public virtual void serialize(BinaryOutput b)
        {
            b.writeInt32(id);
            color.serialize(b);
            b.writeUInt32((uint)modelType);
            frame.serialize(b);
            b.writeString(name);
            velocity.serialize(b);
            controls.serialize(b);
        }

        public virtual void deserialize(BinaryInput b)
        {
            id = b.readInt32();
            color.deserialize(b);
            modelType = (ModelType)b.readUInt32();
            frame.deserialize(b);
            name = b.readString();
            velocity.deserialize(b);
            controls.deserialize(b);
            oldFrameTime = -g3dmath.inf();
        }
    }

    public class EntityTable : Table<int, Entity>
    {
    }

    /** Component access that the original wrote as Vector3::operator[]. */
    internal static class VectorComponent
    {
        internal static double get(Vector3 v, int i)
        {
            switch (i)
            {
                case 0: return v.x;
                case 1: return v.y;
                default: return v.z;
            }
        }

        internal static void set(ref Vector3 v, int i, double value)
        {
            float f = (float)value;
            switch (i)
            {
                case 0: v.x = f; break;
                case 1: v.y = f; break;
                default: v.z = f; break;
            }
        }
    }

    public static class EntitySimulation
    {
        public static void simulateEntities(EntityTable entityTable, SimTime dt)
        {
            Table<int, Entity>.Iterator end = entityTable.end();
            for (Table<int, Entity>.Iterator e = entityTable.begin(); e != end; e.MoveNext())
            {
                e.Current.value.doSimulation(dt);
            }
        }
    }
}
