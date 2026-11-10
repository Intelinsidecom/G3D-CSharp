/**
  @file Collision_Demo/Scene.cs

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-02-07
  @edited  2003-11-18
*/

using System;
using G3D;

namespace Collision_Demo
{
    /**
     
    */
    public class Scene
    {
        /** Static objects */
        public Array<Object> @object = new Array<Object>();

        /** Dynamic objects */
        public Array<SimSphere> sim = new Array<SimSphere>();

    public ReferenceCountedPointer<Sky> sky;

    public ReferenceCountedPointer<Texture> shadowMap;
    private Framebuffer shadowFramebuffer;

    public App app;

    private static readonly Vector3 gravity = new Vector3(0, -60, 0);

    /** Reused per-frame lighting state. */
    private CoordinateFrame lightCFrame = new CoordinateFrame();
    private GLight ambientLight = new GLight();
    private GLight sunLight = new GLight();

        public Scene(App _app)
        {
            app = _app;
            if (app != null && app.renderDevice != null)
            {
                sky = G3D.Sky.fromFile(app.renderDevice, app.dataDir + "sky/");
                if (app.renderDevice.supportsShadows())
                {
                    shadowMap = new ReferenceCountedPointer<Texture>(Texture.createEmpty(
                        "Shadow map", 512, 512, TextureFormat.depth(),
                        Texture.Dimension.DIM_2D, Texture.Settings.shadow()));
                    app.renderDevice.allocateTexture(shadowMap.pointer());
                    shadowFramebuffer = Framebuffer.create(512, 512);
                    shadowFramebuffer.setTexture(Framebuffer.AttachmentPoint.Depth, shadowMap.pointer());
                }
            }
        }

        /** Draws all objects using current lighting and blending. */
        private void renderingPass(RenderDevice rd)
        {
            for (int i = 0; i < @object.size(); ++i)
            {
                @object[i].render(rd);
            }

            for (int i = 0; i < sim.size(); ++i)
            {
                sim[i].render(rd);
            }
        }

        public virtual void render(LightingParameters lighting)
        {
            lightCFrame.rotation = Matrix3.identity();
            lightCFrame.translation = Vector3.zero();
            lightCFrame.lookAt(-lighting.lightDirection, Vector3.unitY());
            lightCFrame.translation = lighting.lightDirection * 20;
            Matrix4 lightProjectionMatrix = Matrix4.orthogonalProjection(-17, 17, -17, 17, 1, 40);
            Matrix4 lightMVP = lightProjectionMatrix * lightCFrame.inverse();

            if (shadowMap != null)
            {
                generateShadowMap(lightCFrame);
            }

            app.renderDevice.clear(sky == null, true, false);

            if (sky != null)
            {
                sky.pointer().render(lighting);
            }

            app.renderDevice.pushState();

            // Ambient and detail light pass.
            app.renderDevice.enableLighting();
            ambientLight = GLight.directional(-lighting.lightDirection, Color3.white() * .25f);
            app.renderDevice.setLight(0, ambientLight);
            app.renderDevice.setAmbientLightColor(lighting.ambient);
            app.renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);
            renderingPass(app.renderDevice);

            // Sun light pass
            app.renderDevice.setAmbientLightColor(Color3.black());
            app.renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
            app.renderDevice.setDepthWrite(false);
            sunLight = GLight.directional(lighting.lightDirection, lighting.lightColor);
            app.renderDevice.setLight(0, sunLight);
            app.renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
            if (shadowMap != null)
            {
                app.renderDevice.configureShadowMap(1, lightMVP, shadowMap.pointer());
            }
            renderingPass(app.renderDevice);

            app.renderDevice.popState();

            if (sky != null)
            {
                sky.pointer().renderLensFlare(lighting);
            }
        }

        /** Updates the shadow map texture. */
        private void generateShadowMap(    
            CoordinateFrame lightViewMatrix)
        {
            const int shadowMapSize = 512;
            const float lightProjX = 17;
            const float lightProjY = 17;
            const float lightProjNear = 1;
            const float lightProjFar = 40;

            RenderDevice rd = app.renderDevice;
            rd.pushState();
            rd.setFramebuffer(shadowFramebuffer);
            rd.clear(false, true, false);
            rd.setViewport(Rect2D.xywh(0, 0, shadowMapSize, shadowMapSize));
            rd.setProjectionMatrix(Matrix4.orthogonalProjection(-lightProjX, lightProjX, -lightProjY, lightProjY, lightProjNear, lightProjFar));
            rd.setCameraToWorldMatrix(lightViewMatrix);
            rd.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
            rd.setDepthWrite(true);
            rd.setColorWrite(false);
            rd.setPolygonOffset(4);
            renderingPass(rd);
            rd.popState();
        }

        public Scene()
        {
            // Initialize sky and shadow map
        }

        public void clear()
        {
            @object.deleteAll();
            sim.deleteAll();
        }

        public void insertStatic(Object obj)
        {
            @object.append(obj);
        }

        public int objectCount
        {
            get { return @object.size(); }
        }

        public int simCount
        {
            get { return sim.size(); }
        }

        /**
         Insert an object to be simulated.
        */
        public void insertDynamic(SimSphere s)
        {
            sim.append(s);
        }

        public void simulate(GameTime deltaTime)
        {
            double duration = deltaTime;

            const bool RESTING_CONTACT = true;
            const bool ROLLING_CONTACT = true;
            const bool SUPER_DAMPING = true;

            for (int i = 0; i < sim.size(); ++i)
            {
                SimSphere obj = sim[i];

                double time = 0.0;

                int count = 0;
                while ((time < duration) && (count < 10))
                {
                    Vector3 collisionLocation = Vector3.zero();
                    Vector3 collisionNormal = Vector3.zero();
                    Vector3 acceleration = gravity;

                    Vector3 outLocation = Vector3.zero();
                    Vector3 outNormal = Vector3.zero();
                    GameTime delta = timeUntilCollisionWithMovingSphere(obj.sphere, obj.velocity, duration - time, ref outLocation, ref outNormal);
                    collisionLocation = outLocation;
                    collisionNormal = outNormal;

                    if (RESTING_CONTACT)
                    {
                        if (g3dmath.fuzzyEq((double)delta, 0.0))
                        {
                            const double restingInterpenetration = 0.005;

                            if ((collisionLocation - obj.sphere.center).length() > obj.sphere.radius - restingInterpenetration)
                            {
                                Sphere bumpedSphere = new Sphere(obj.sphere.center + collisionNormal * restingInterpenetration, obj.sphere.radius);
                                Vector3 bumpedLocation = Vector3.zero();
                                Vector3 bumpedNormal = Vector3.zero();
                                GameTime bumpedDelta = timeUntilCollisionWithMovingSphere(bumpedSphere, obj.velocity, duration - time, ref bumpedLocation, ref bumpedNormal);

                                if (!g3dmath.fuzzyEq((double)bumpedDelta, 0.0))
                                {
                                    Vector3 deltaPosition = bumpedSphere.center - obj.sphere.center;
                                    obj.sphere.center = bumpedSphere.center;

                                    double v0 = obj.velocity.length();
                                    if (!g3dmath.fuzzyEq(v0, 0.0))
                                    {
                                        double v1 = Math.Sqrt(deltaPosition.dot(acceleration) + v0 * v0);
                                        obj.velocity = obj.velocity * (float)(v1 / v0);
                                    }
                                }

                                if (g3dmath.fuzzyEq((double)bumpedDelta, 0.0))
                                {
                                    // stuck
                                    if (obj.velocity.squaredLength() < 1)
                                    {
                                        obj.velocity = collisionNormal;
                                    }
                                    else
                                    {
                                        obj.velocity = obj.velocity.length() * collisionNormal;
                                    }
                                    delta = g3dmath.inf();
                                    acceleration = Vector3.zero();
                                }
                                else
                                {
                                    delta = bumpedDelta;
                                    collisionLocation = bumpedLocation;
                                    collisionNormal = bumpedNormal;
                                }
                            }
                        }
                    }

                    if (g3dmath.fuzzyLt(time + (double)delta, duration))
                    {
                        Vector3 bounceDirection = CollisionDetection.bounceDirection(obj.sphere, obj.velocity, (float)delta, collisionLocation, collisionNormal);

                        if (!bounceDirection.isFinite())
                        {
                            bounceDirection = Vector3.zero();
                        }

                        delta = Math.Max(0.0, (double)delta - 0.0005);
                        obj.sphere.center += obj.velocity * (float)delta;

                        if ((bounceDirection.dot(collisionNormal) < -.9))
                        {
                            Vector3 tangent = (obj.sphere.center - collisionLocation).direction();
                            bounceDirection = (bounceDirection + tangent * 0.5f).direction();
                        }

                        Vector3 velAccel = obj.velocity + acceleration * (float)delta;
                        obj.velocity = bounceDirection * velAccel.length();

                        double nv = obj.velocity.dot(collisionNormal);
                        double an = acceleration.dot(collisionNormal);
                        double av = acceleration.dot(obj.velocity);

                        if (!acceleration.isZero())
                        {
                            av /= duration * Math.Pow(acceleration.squaredLength(), 0.75);
                        }

                        if ((an < 0) && (nv * duration <= 0.05) && ROLLING_CONTACT)
                        {
                            Vector3 parallel = obj.velocity - (float)nv * collisionNormal;
                            double len = parallel.length();
                            if (len < 0.00001)
                            {
                                obj.velocity = Vector3.zero();
                            }
                            else
                            {
                                obj.velocity = parallel * (float)(obj.velocity.length() / len);
                            }
                        }
                        else if (SUPER_DAMPING)
                        {
                            double d = 0;
                            if (acceleration.length() > 0 && obj.velocity.length() > 0)
                            {
                                d = -acceleration.dot(obj.velocity) / (acceleration.length() * obj.velocity.length());
                            }
                            double alpha = 1;

                            if ((d > .85) && (obj.velocity.length() < 5))
                            {
                                if (d > .95)
                                {
                                    alpha = .01;
                                }
                                else if (d > .9)
                                {
                                    alpha = .05;
                                }
                                else
                                {
                                    alpha = .1;
                                }
                            }

                            obj.velocity *= (float)(alpha * 0.9);
                        }
                        else
                        {
                            obj.velocity *= 0.9f;
                        }
                    }
                    else
                    {
                        double remaining = duration - time;
                        delta = remaining;
                        obj.sphere.center += (float)delta * obj.velocity;

                        if (RESTING_CONTACT && obj.velocity.isZero() && !acceleration.isZero())
                        {
                            Vector3 bumpedLocation = Vector3.zero();
                            Vector3 bumpedNormal = Vector3.zero();
                            GameTime t = timeUntilCollisionWithMovingSphere(obj.sphere, acceleration * (float)duration, duration - time, ref bumpedLocation, ref bumpedNormal);
                            if ((double)t < 0.01)
                            {
                                Vector3 bounceDirection = CollisionDetection.bounceDirection(obj.sphere, acceleration * (float)duration, (float)t, bumpedLocation, bumpedNormal);
                                double align = bounceDirection.dot(acceleration.direction());
                                if (align < -.95)
                                {
                                    acceleration = Vector3.zero();
                                }
                            }
                        }

                        obj.velocity += acceleration * (float)delta;
                    }

                    if (!obj.velocity.isFinite())
                    {
                        obj.velocity = Vector3.zero();
                    }
                    if (!obj.sphere.center.isFinite())
                    {
                        obj.sphere.center = Vector3.zero();
                    }

                    double old = time;
                    time += (double)delta;
                    if (time <= old)
                    {
                        time += duration / 20.0;
                    }
                    ++count;
                }

                if (obj.sphere.center.squaredLength() > 40 * 40)
                {
                    obj.sphere.center = new Vector3(0, obj.sphere.radius * 2, 0);
                    obj.velocity = Vector3.random() * 25;
                }
            }
        }



        /** Returns the time until collision with a moving sphere,
            inf if no collision will ever occur.  Only <I>entering</I>
            collisions are detected, not ones where the sphere
            is leaving an object it is trapped inside. */
        public virtual GameTime timeUntilCollisionWithMovingSphere(
            Sphere sphere,
            Vector3 velocity,
            GameTime timeLimit,
            ref Vector3 outLocation,
            ref Vector3 outNormal)
        {
            GameTime outTime = g3dmath.inf();

            for (int i = 0; i < @object.size(); ++i)
            {
                Vector3 testLocation = Vector3.zero();
                Vector3 testNormal = Vector3.zero();
                GameTime testTime;

                testTime = @object[i].timeUntilCollisionWithMovingSphere(sphere, velocity, timeLimit, ref testLocation, ref testNormal);

                if (testTime < outTime)
                {
                    outTime = testTime;
                    outLocation = testLocation;
                    outNormal = testNormal;
                }
            }

            return outTime;
        }
    }
}