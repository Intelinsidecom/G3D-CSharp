/**
  @file Sky.h

  @maintainer Morgan McGuire, morgan@cs.brown.edu

  @created 2002-10-04
  @edited  2006-02-11
*/

using System;
using System.IO;
using G3D;

namespace G3D
{
    /**
     A background cube with an appropriately warped texture to make it
     look like an infinite environment.  The sky also manages drawing
     the sun and moon, with lens flare effects for the sun.

     Copy the images from data/sky to your project directory to use this
     class or provide your own.

     If you already have a preloaded CubeMap texture, use Sky::fromCubeMap.

     Example:

     <PRE>
       // Showing the default filename mask for clarity
       ReferenceCountedPointer<Sky> sky = Sky.fromFile(renderDevice, "data/sky/", "plainsky/null_plainsky512_*.jpg");
       LightingParameters lighting = new LightingParameters(toSeconds(9, 00, 00, AM));

       ...

       // Rendering loop
           sky.render(lighting);

           // Draw the rest of the scene
           ...

           sky.renderLensFlare(lighting);
           ...
     </PRE>

    */
    public class Sky : ReferenceCountedObject
    {
        /** Indices into texture array */
        private enum Direction { UP = 0, LT = 1, RT = 2, BK = 3, FT = 4, DN = 5 };

        /**
         Only used if the render device does not support cube maps.
         */
        private ReferenceCountedPointer<Texture>[] texture = new ReferenceCountedPointer<Texture>[6];

        /**
         Used unless the render device does not support cube maps.
         */
        private ReferenceCountedPointer<Texture> cubeMap;

        private ReferenceCountedPointer<Texture> sun;
        private ReferenceCountedPointer<Texture> moon;
        private ReferenceCountedPointer<Texture> disk;
        private ReferenceCountedPointer<Texture> sunRays;

        private Array<Vector4> star = new Array<Vector4>();
        private Array<float> starIntensity = new Array<float>();

        private bool drawCelestialBodies;

        /** @deprecated To be removed in 7.00 */
        private RenderDevice renderDevice;

        private Vector3 sunDirectionValue;
        private Vector3 moonDirectionValue;
        private CoordinateFrame starFrameValue;
        private Color3 skyColorValue;
        private bool hasSunDirection;
        private bool hasMoonDirection;
        private bool hasStarFrame;

        /**
         Used to draw the sky, moon, and lens flare

         @param C center
         @param X a unit axis
         @param Y a unit axis perpendicular to X
         @param r radius of the sphere
        */
        private static void drawCelestialSphere(
            RenderDevice rd,
            Vector4 C,
            Vector4 X,
            Vector4 Y,
            double r,
            Color4 color)
        {
            rd.setColor(color);
            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            rd.setTexCoord(0, new Vector2(0, 0));
            rd.sendVertex(C + (X + Y) * (float)r);
            rd.setTexCoord(0, new Vector2(0, 1));
            rd.sendVertex(C + (X - Y) * (float)r);
            rd.setTexCoord(0, new Vector2(1, 1));
            rd.sendVertex(C + (-X - Y) * (float)r);
            rd.setTexCoord(0, new Vector2(1, 0));
            rd.sendVertex(C + (-X + Y) * (float)r);
            rd.endPrimitive();
        }

        private static Vector4 screenProject(RenderDevice rd, Vector4 v)
        {
            Vector4 clip = rd.project(v);
            Rect2D viewport = rd.getViewport();
            double rhw = 1.0 / clip.w;
            return new Vector4(
                (float)((1 + clip.x * rhw) * viewport.width() / 2 + viewport.x0()),
                (float)((1 - clip.y * rhw) * viewport.height() / 2 + viewport.y0()),
                (float)(clip.z * rhw),
                (float)rhw);
        }

        private static void hackProjectionMatrix(RenderDevice rd)
        {
            Matrix4 P = rd.getProjectionMatrix();

            // Set the 3rd row (2nd index) so the depth always is in the middle of the depth range.

            P.setElt(2, 0, 0);
            P.setElt(2, 1, 0);
            P.setElt(2, 2, -0.5f);
            P.setElt(2, 3, 0);

            rd.setProjectionMatrix(P);
        }

        /**
         Renders the sky box.
         */
        private void renderBox(RenderDevice rd)
        {
            rd.pushState();

            bool cube = (cubeMap != null);

            if (cube)
            {
                rd.setTexture(0, cubeMap.pointer());

                // On Radeon Mobility, explicit cube map coordinates don't work right.
                // We instead put cube map coords in the normals and use texgen to copy
                // them over
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                backend.ConfigureCubeMapTexGen(0, CubeMapTexGenMode.TEXGEN_NORMAL_MAP);

                CoordinateFrame cframe = rd.getCameraToWorldMatrix();
                cframe.translation = Vector3.zero();
                rd.setTextureMatrix(0, cframe);
            }
            else
            {
                // In the 6-texture case, the sky box is rotated 90 degrees
                // (this is because the textures are loaded incorrectly)
                CoordinateFrame cframe = CoordinateFrame.identity();
                cframe.rotation = Matrix3.fromAxisAngle(Vector3.unitY(), (float)g3dmath.toRadians(-90));
                rd.setObjectToWorldMatrix(cframe);
                rd.setTexture(0, texture[(int)Direction.BK].pointer());
            }

            float s = 1;
            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, -s, +s, -s, 0, 0);
            vertex(rd, -s, -s, -s, 0, 1);
            vertex(rd, +s, -s, -s, 1, 1);
            vertex(rd, +s, +s, -s, 1, 0);
            rd.endPrimitive();

            if (!cube)
            {
                rd.setTexture(0, texture[(int)Direction.LT].pointer());
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, -s, +s, +s, 0, 0);
            vertex(rd, -s, -s, +s, 0, 1);
            vertex(rd, -s, -s, -s, 1, 1);
            vertex(rd, -s, +s, -s, 1, 0);
            rd.endPrimitive();


            if (!cube)
            {
                rd.setTexture(0, texture[(int)Direction.FT].pointer());
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, +s, +s, +s, 0, 0);
            vertex(rd, +s, -s, +s, 0, 1);
            vertex(rd, -s, -s, +s, 1, 1);
            vertex(rd, -s, +s, +s, 1, 0);
            rd.endPrimitive();

            if (!cube)
            {
                rd.setTexture(0, texture[(int)Direction.RT].pointer());
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, +s, +s, +s, 1, 0);
            vertex(rd, +s, +s, -s, 0, 0);
            vertex(rd, +s, -s, -s, 0, 1);
            vertex(rd, +s, -s, +s, 1, 1);
            rd.endPrimitive();

            if (!cube)
            {
                rd.setTexture(0, texture[(int)Direction.UP].pointer());
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, +s, +s, +s, 1, 1);
            vertex(rd, -s, +s, +s, 1, 0);
            vertex(rd, -s, +s, -s, 0, 0);
            vertex(rd, +s, +s, -s, 0, 1);
            rd.endPrimitive();

            if (!cube)
            {
                rd.setTexture(0, texture[(int)Direction.DN].pointer());
            }

            rd.beginPrimitive(RenderDevice.Primitive.QUADS);
            vertex(rd, +s, -s, -s, 0, 0);
            vertex(rd, -s, -s, -s, 0, 1);
            vertex(rd, -s, -s, +s, 1, 1);
            vertex(rd, +s, -s, +s, 1, 0);
            rd.endPrimitive();

            if (cube)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                backend.ConfigureCubeMapTexGen(0, CubeMapTexGenMode.TEXGEN_OFF);
            }

            rd.popState();
        }

        /** Draw the sun, called by render() */
        private void drawSun(RenderDevice rd, LightingParameters lighting)
        {
            Vector3 sunPosition = hasSunDirection ? sunDirectionValue :
                (lighting.physicallyCorrect ? lighting.trueSunPosition : lighting.sunPosition);

            // Sun vector
            Vector4 L = new Vector4(sunPosition, 0);
            Vector3 LcrossZ = sunPosition.cross(Vector3.unitZ()).direction();
            Vector4 X = new Vector4(LcrossZ, 0);
            Vector4 Y = new Vector4(sunPosition.cross(LcrossZ), 0);

            rd.setTexture(0, sun.pointer());
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE, RenderDevice.BlendFunc.BLEND_ONE);
            Color3 c = lighting.emissiveScale * .8f;

            if (sunPosition.y < 0)
            {
                // Fade out the sun as it goes below the horizon
                c *= Math.Max(0.0f, (sunPosition.y + 0.1f) * 10.0f);
            }

            drawCelestialSphere(rd, L, X, Y, 0.12f, new Color4(c, 1.0f));
        }

        /** Draw the night sky, called by render() */
        private void drawMoonAndStars(RenderDevice rd, LightingParameters lighting)
        {
            Vector3 moonPosition = hasMoonDirection ? moonDirectionValue :
                (lighting.physicallyCorrect ? lighting.trueMoonPosition : lighting.moonPosition);

            Vector4 L = new Vector4(moonPosition, 0);
            Vector3 LcrossZ = moonPosition.cross(Vector3.unitZ()).direction();
            Vector4 X = new Vector4(LcrossZ, 0);
            Vector4 Y = new Vector4(moonPosition.cross(LcrossZ), 0);

            // Draw stars
            if (lighting.moonPosition.y > -0.3f)
            {
                float k = (3.001f - lighting.skyAmbient.length() * lighting.skyAmbient.length());
                float s = k;
                k *= (float)rd.getBrightScale();
                rd.pushState();
                // Rotate stars
                rd.setObjectToWorldMatrix(hasStarFrame ? starFrameValue :
                    (lighting.physicallyCorrect ? lighting.trueStarFrame : lighting.starFrame));
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE);

                rd.beginPrimitive(RenderDevice.Primitive.POINTS);
                for (int i = star.size() - 1; i >= 0; --i)
                {
                    float b = starIntensity[i] * k;
                    rd.setPointSize(starIntensity[i] * s);
                    rd.setColor(new Color3(b * lighting.emissiveScale.r, b * lighting.emissiveScale.g, b * lighting.emissiveScale.b));
                    rd.sendVertex(star[i]);
                }
                rd.endPrimitive();

                rd.popState();
            }

            rd.setTexture(0, moon.pointer());
            rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
            rd.setAlphaTest(RenderDevice.AlphaTest.ALPHA_GEQUAL, 0.05);
            drawCelestialSphere(rd, L, X, Y, .06, new Color4(lighting.emissiveScale, Math.Min(1.0f, Math.Max(0.0f, moonPosition.y * 4.0f))));
        }

        private Sky(
            RenderDevice renderDevice,
            ReferenceCountedPointer<Texture>[] textures,
            string directory,
            bool useCubeMap,
            bool drawCelestialBodies,
            double quality)
        {
            this.renderDevice = renderDevice;
            this.drawCelestialBodies = drawCelestialBodies;

            if (useCubeMap)
            {
                cubeMap = textures[0];
            }
            else
            {
                for (int i = 0; i < 6; ++i)
                {
                    texture[i] = textures[i];
                }
            }

            TextureFormat format;
            TextureFormat alphaFormat;

            if (quality > .66)
            {
                format = TextureFormat.RGB8;
                alphaFormat = TextureFormat.RGBA8;
            }
            else if (quality > .33)
            {
                format = TextureFormat.RGB_DXT1;
                alphaFormat = TextureFormat.RGBA_DXT1;
            }
            else
            {
                format = TextureFormat.RGBA_DXT5;
                alphaFormat = TextureFormat.RGBA_DXT5;
            }

            if (drawCelestialBodies)
            {
                moon = new ReferenceCountedPointer<Texture>(Texture.fromTwoFiles(directory + "moon.jpg", directory + "moon-alpha.jpg",
                    alphaFormat, Texture.WrapMode.TRANSPARENT_BORDER, Texture.InterpolateMode.BILINEAR_NO_MIPMAP, Texture.Dimension.DIM_2D,
                    Texture.DepthReadMode.DEPTH_NORMAL, 1.0f));

                sun = new ReferenceCountedPointer<Texture>(Texture.fromFile(directory + "sun.jpg", format, Texture.Dimension.DIM_2D,
                    new Texture.Settings() { wrapMode = Texture.WrapMode.TRANSPARENT_BORDER, interpolateMode = Texture.InterpolateMode.BILINEAR_NO_MIPMAP, depthReadMode = Texture.DepthReadMode.DEPTH_NORMAL, maxAnisotropy = 1.0f }));

                disk = new ReferenceCountedPointer<Texture>(Texture.fromFile(directory + "lensflare.jpg", format, Texture.Dimension.DIM_2D,
                    new Texture.Settings() { wrapMode = Texture.WrapMode.TRANSPARENT_BORDER, interpolateMode = Texture.InterpolateMode.BILINEAR_NO_MIPMAP, depthReadMode = Texture.DepthReadMode.DEPTH_NORMAL, maxAnisotropy = 1.0f }));

                sunRays = new ReferenceCountedPointer<Texture>(Texture.fromFile(directory + "sun-rays.jpg", format, Texture.Dimension.DIM_2D,
                    new Texture.Settings() { wrapMode = Texture.WrapMode.TRANSPARENT_BORDER, interpolateMode = Texture.InterpolateMode.BILINEAR_NO_MIPMAP, depthReadMode = Texture.DepthReadMode.DEPTH_NORMAL, maxAnisotropy = 1.0f }));

                int i = 0;

                // If file exists, load the real starfield
                if (File.Exists(directory + "real.str"))
                {
                    BinaryInput inFile = new BinaryInput(directory + "real.str", G3DEndian.G3D_LITTLE_ENDIAN, true);
                    {
                        short numStars;
                        float x, y, z;

                        string header = inFile.readString(5);
                        Debug.debugAssert(header == "STARS");

                        numStars = inFile.readInt16();
                        star.resize(numStars);
                        starIntensity.resize(numStars);

                        // Read X, Y, Z, and intensity
                        for (i = 0; i < numStars; i++)
                        {
                            x = shortToFloat(inFile.readInt16());
                            y = shortToFloat(inFile.readInt16());
                            z = shortToFloat(inFile.readInt16());

                            star[i] = new Vector4(x, y, z, 0);

                            float intensity = shortToFloat(inFile.readInt16());
                            starIntensity[i] = intensity * intensity + .3f;
                        }
                    }
                }
                else
                {
                    // Create a random starfield
                    star.resize(3000);
                    starIntensity.resize(star.size());
                    for (i = star.size() - 1; i >= 0; --i)
                    {
                        star[i] = new Vector4(Vector3.random(), 0);
                        starIntensity[i] = uniformRandom() * uniformRandom() + .3f;
                    }
                }
            }
        }

        private static float shortToFloat(short x)
        {
            return (float)((2.0f * x + 1.0f) * (1.0f / 65535.0f));
        }

        private static float uniformRandom()
        {
            return (float)g3dmath.random(0.0, 1.0);
        }

        /** Draws a single sky-box vertex.  Called from renderBox. (s,t) are
            texture coordinates for the case where the cube map is not used.*/
        private void vertex(RenderDevice renderDevice, float x, float y, float z, float s, float t)
        {
            double w = 0;
            bool cube = (cubeMap != null);

            if (cube)
            {
                // Texcoord generation will move this normal to tex coord 0
                renderDevice.setNormal(new Vector3(x, y, z));
            }
            else
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                if (backend == null || !backend.SupportsExtension("GL_EXT_texture_edge_clamp"))
                {
                    // Move the edge coordinates towards the center just
                    // enough that the black clamped border isn't sampled.
                    if (s == 0)
                    {
                        s += (0.6f / (float)texture[0].pointer().texelWidth());
                    }
                    else if (s == 1)
                    {
                        s -= (0.6f / (float)texture[0].pointer().texelWidth());
                    }
                    if (t == 0)
                    {
                        t += (0.6f / (float)texture[0].pointer().texelHeight());
                    }
                    else if (t == 1)
                    {
                        t -= (0.6f / (float)texture[0].pointer().texelHeight());
                    }
                }
                renderDevice.setTexCoord(0, new Vector2(s, t));
            }

            renderDevice.sendVertex(new Vector4(x, y, z, (float)w));
        }

        /**
         @param directory If directory is not "" it should
           end in a trailing slash.  This is the location of the real.str file
           and the sun, moon, etc. files.

         @param filename A filename with "*" in place of {up, lt, rt, bk, ft, dn}.
           The filename can be either fully qualified, relative to the current
           directory, or relative to <I>directory</I>.

         @param drawCelestialBodies If true, draw the sun, moon, and stars. Requires
           moon.jpg, moon-alpha.jpg, sun.jpg, lensflare.jpg, sun-rays.jpg be present
           in given directory. Defaults to true.

         @param quality Trade image quality for texture memory:
           .5 -> 1/8  the texture memory of 1.0,
            0 -> 1/16 the texture memory of 1.0.
           Color banding will occur at low quality settings.

         @param renderDevice May be NULL, if a non-NULL argument is provided to G3D::Sky::render.

         @param scaleDownFactor Resize the texture by 1/scaleDownFactor in each dimension.  Should be a power of two.
                  Useful for creating a skybox that occupies less memory on low-end cards.
        */
        public static ReferenceCountedPointer<Sky> fromFile(
            RenderDevice renderDevice,
            string directory,
            string filename = "plainsky/null_plainsky512_*.jpg",
            bool drawCelestialBodies = true,
            double quality = 1.0,
            int scaleDownFactor = 1)
        {
            string[] f = new string[6];
            f[0] = filename;
            for (int i = 1; i < 6; ++i)
            {
                f[i] = "";
            }
            return fromFile(renderDevice, directory, f, drawCelestialBodies, quality, scaleDownFactor);
        }

        public static ReferenceCountedPointer<Sky> fromFile(
            RenderDevice renderDevice,
            string directory,
            string[] filename,
            bool drawCelestialBodies = true,
            double quality = 1.0,
            int scaleDownFactor = 1)
        {
            Debug.debugAssertM(
                (directory == "") ||
                (directory[directory.Length - 1] == '/') ||
                (directory[directory.Length - 1] == '\\'),
                "Directory must end in a slash");

            if (filename == null || filename.Length != 6)
            {
                throw new ArgumentException("Exactly six filenames must be provided.", "filename");
            }

            if (scaleDownFactor < 1)
            {
                throw new ArgumentOutOfRangeException("scaleDownFactor", "scaleDownFactor must be at least 1.");
            }

            TextureFormat format;
            TextureFormat alphaFormat;
            string fullFilename = filename[0];

            if (quality > .66)
            {
                format = TextureFormat.RGB8;
                alphaFormat = TextureFormat.RGBA8;
            }
            else if (quality > .33)
            {
                format = TextureFormat.RGB_DXT1;
                alphaFormat = TextureFormat.RGBA_DXT1;
            }
            else
            {
                format = TextureFormat.RGBA_DXT5;
                alphaFormat = TextureFormat.RGBA_DXT5;
            }

            // Look for the filename
            // Parse the filename into a base name and extension
            string filenameBase = "";
            string filenameExt = "";

            if (filename[1] == "")
            {
                // First look relative to the current directory
                Texture.splitFilenameAtWildCard(fullFilename, out filenameBase, out filenameExt);

                if (!File.Exists(filenameBase + "up" + filenameExt))
                {
                    // Look relative to the specified directory
                    filenameBase = directory + filenameBase;
                }
            }

            ReferenceCountedPointer<Texture>[] faceTextures = new ReferenceCountedPointer<Texture>[6];
            bool useCubeMap;

            // The cube map path requires texture coordinate generation, which is
            // a backend specific feature that is not ported yet.  The six texture
            // path produces the same image.
            useCubeMap = false;

            // This card doesn't support cube maps; it probably has low memory as well, so we
            // switch to bilinear instead of trilinear to save space on the mipmaps.

            string[] ext = new string[] { "up", "lf", "rt", "bk", "ft", "dn" };

            if (filename[1] == "")
            {
                // Specified one cube map
                for (int t = 0; t < 6; ++t)
                {
                    faceTextures[t] = new ReferenceCountedPointer<Texture>(Texture.fromFile(filenameBase + ext[t] + filenameExt,
                        format, Texture.WrapMode.CLAMP, Texture.InterpolateMode.BILINEAR_NO_MIPMAP, Texture.Dimension.DIM_2D,
                        1.0, Texture.DepthReadMode.DEPTH_NORMAL, 1.0f, 1.0f / scaleDownFactor));
                }
            }
            else
            {
                // Specified six textures
                for (int t = 0; t < 6; ++t)
                {
                    faceTextures[t] = new ReferenceCountedPointer<Texture>(Texture.fromFile(filename[t],
                        format, Texture.WrapMode.CLAMP, Texture.InterpolateMode.BILINEAR_NO_MIPMAP, Texture.Dimension.DIM_2D,
                        1.0, Texture.DepthReadMode.DEPTH_NORMAL, 1.0f, 1.0f / scaleDownFactor));
                }
            }

            return new ReferenceCountedPointer<Sky>(new Sky(renderDevice, faceTextures, directory, useCubeMap, drawCelestialBodies, quality));
        }

        /** @deprecated Converted to Sky::fromFile */
        public static ReferenceCountedPointer<Sky> create(
            RenderDevice renderDevice,
            string directory,
            string filename = "plainsky/null_plainsky512_*.jpg",
            bool drawCelestialBodies = true,
            double quality = 1.0,
            int scaleDownFactor = 1)
        {
            return fromFile(renderDevice, directory, filename, drawCelestialBodies, quality, scaleDownFactor);
        }

        /** @deprecated Converted to Sky::fromFile */
        public static ReferenceCountedPointer<Sky> create(
            RenderDevice renderDevice,
            string directory,
            string[] filename,
            bool drawCelestialBodies = true,
            double quality = 1.0,
            int scaleDownFactor = 1)
        {
            return fromFile(renderDevice, directory, filename, drawCelestialBodies, quality, scaleDownFactor);
        }

        /**
         @param _cubeMap This must be a Texture of dimension - DIM_CUBE_MAP.
         */
        public static ReferenceCountedPointer<Sky> fromCubeMap(
            RenderDevice rd,
            ReferenceCountedPointer<Texture> _cubeMap,
            string directory,
            bool _drawCelestialBodies = true,
            double quality = 1.0)
        {
            Debug.debugAssertM(
                (directory == "") ||
                (directory[directory.Length - 1] == '/') ||
                (directory[directory.Length - 1] == '\\'),
                "Directory must end in a slash");

            Debug.debugAssert(_cubeMap != null);
            Debug.debugAssert(_cubeMap.pointer().getDimension() == Texture.Dimension.DIM_CUBE_MAP);

            if (_cubeMap == null)
            {
                throw new ArgumentNullException("_cubeMap");
            }

            if (_cubeMap.pointer().getDimension() != Texture.Dimension.DIM_CUBE_MAP)
            {
                throw new ArgumentException("The texture must have dimension DIM_CUBE_MAP.", "_cubeMap");
            }

            ReferenceCountedPointer<Texture>[] t = new ReferenceCountedPointer<Texture>[6];
            t[0] = _cubeMap;

            return new ReferenceCountedPointer<Sky>(new Sky(rd, t, directory, true, _drawCelestialBodies, quality));
        }

        /**
         Call at the very beginning of your rendering routine.
         Will restore all state it changes.
         */
        public void render(
            RenderDevice renderDevice,
            LightingParameters lighting)
        {
            Debug.debugAssert(renderDevice != null);

            if (renderDevice == null)
            {
                throw new ArgumentNullException("renderDevice");
            }

            if (lighting == null)
            {
                throw new ArgumentNullException("lighting");
            }

            update(lighting);

            renderDevice.pushState();
            // Ignore depth, make sure we're not clipped by the far plane
            hackProjectionMatrix(renderDevice);

            // Eliminate the translation of the camera
            CoordinateFrame matrix = new CoordinateFrame(Vector3.zero());
            matrix.rotation = renderDevice.getCameraToWorldMatrix().rotation;
            renderDevice.setCameraToWorldMatrix(matrix);
            renderDevice.setObjectToWorldMatrix(CoordinateFrame.identity());

            renderDevice.setColor(lighting.skyAmbient * (float)renderDevice.getBrightScale());
            renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
            renderDevice.setDepthWrite(false);
            renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_ALWAYS_PASS);


            // Draw the sky box
            renderDevice.resetTextureUnit(0);
            renderBox(renderDevice);

            if (drawCelestialBodies)
            {
                drawMoonAndStars(renderDevice, lighting);

                drawSun(renderDevice, lighting);
            }

            renderDevice.popState();
        }

        /** @deprecated Use render(RenderDevice, LightingParameters) */
        public void render(LightingParameters lighting)
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("This Sky was not created with a RenderDevice.");
            }
            render(renderDevice, lighting);
        }

        /**
         Call at the very end of your rendering routine.
         Will restore all state it changes.
         */
        public void renderLensFlare(
            RenderDevice renderDevice,
            LightingParameters lighting)
        {
            Debug.debugAssert(renderDevice != null);

            if (renderDevice == null)
            {
                throw new ArgumentNullException("renderDevice");
            }

            if (lighting == null)
            {
                throw new ArgumentNullException("lighting");
            }

            if (!drawCelestialBodies)
            {
                return;
            }

            Vector3 sunPosition = hasSunDirection ? sunDirectionValue :
                (lighting.physicallyCorrect ? lighting.trueSunPosition : lighting.sunPosition);

            if (sunPosition.y < -.1)
            {
                return;
            }

            renderDevice.pushState();

                CoordinateFrame camera = renderDevice.getCameraToWorldMatrix();
                CoordinateFrame matrix = CoordinateFrame.identity();
                matrix.rotation = camera.rotation;
                renderDevice.setCameraToWorldMatrix(matrix);
                renderDevice.setObjectToWorldMatrix(CoordinateFrame.identity());

                renderDevice.setColor(lighting.skyAmbient);
                renderDevice.setCullFace(RenderDevice.CullFace.CULL_BACK);
                renderDevice.setDepthWrite(false);
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_ALWAYS_PASS);
                renderDevice.resetTextureUnit(0);

                // Compute the sun's position using the 3D transformation
                Vector4 pos = screenProject(renderDevice, new Vector4(sunPosition, 0));

                if (sunPosition.dot(camera.getLookVector()) > 0)
                {
                    // Number of visible points on the sun
                    int visible = 0;
                    try
                    {
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        for (int dy = -1; dy <= 1; ++dy)
                        {
                            double distanceToSun =
                                renderDevice.getDepthBufferValue(
                                 g3dmath.iRound(pos.x + dx * 15.0),
                                 g3dmath.iRound(pos.y + dy * 15.0));
                            visible += (distanceToSun >= .99999) ? 1 : 0;
                        }
                    }
                    }
                    catch (NotSupportedException)
                    {
                        // The backend cannot read back the depth buffer, so
                        // the sun is assumed to be fully visible.
                        visible = 9;
                    }

                    double fractionOfSunVisible = visible / 9.0;

                    if (fractionOfSunVisible > 0.0)
                    {
                        // We need to switch to an infinite projection matrix
                        // to draw the flares.  Note that we must make this
                        // change *after* the depth buffer values have been
                        // read back.
                        hackProjectionMatrix(renderDevice);

                        renderDevice.setBlendFunc(RenderDevice.BlendFunc.BLEND_ONE,
                                                   RenderDevice.BlendFunc.BLEND_ONE);

                        // Make flares fade out near sunset and sunrise
                        float flareBrightness = (float)Math.Sqrt(Math.Max(sunPosition.y * 4.0f, 0.0f));

                        // Sun position
                        Vector4 L = new Vector4(sunPosition, 0);
                        Vector3 LcrossZ = sunPosition.cross(Vector3.unitZ()).direction();
                        Vector4 X = new Vector4(LcrossZ, 0);
                        Vector4 Y = new Vector4(sunPosition.cross(LcrossZ), 0);

                        // Sun rays at dawn
                        if ((sunPosition.x > 0) &&
                            (sunPosition.y >= -.1))
                        {
                            renderDevice.setTexture(0, sunRays.pointer());
                            double occlusionAttenuation =
                                (1 - (2 * fractionOfSunVisible - 1) * (2 * fractionOfSunVisible - 1));

                            Color4 col =
                                new Color4(1, 1, 1, 1) * (float)(occlusionAttenuation *
                                    0.4f * Math.Max(0.0f, Math.Min(1.0f, 1.0f - sunPosition.y * 2.0f / (float)Math.Sqrt(2.0f))));
                            drawCelestialSphere(renderDevice, L, X, Y, 0.6, col);
                        }

                        renderDevice.setTexture(0, sun.pointer());
                        drawCelestialSphere(renderDevice, L, X, Y, .13f,
                                            new Color4(lighting.emissiveScale * (float)fractionOfSunVisible * .5f, 1.0f));

                        // Lens flare
                        Vector4 C = new Vector4(camera.getLookVector(), 0);
                        double[] position = new double[] { .5, .5, -.25, -.75, .45, .6, -.5, -.1, .55, -1.5, -2, 1 };
                        double[] size = new double[] { .12, .05, .02, .02, .02, .02, .01, .01, .01, .01, .01, 0.05 };
                        Color3[] color = new Color3[] { new Color3(6, 4, 0) / 255, new Color3(6, 4, 0) / 255, new Color3(0, 12, 0) / 255, new Color3(0, 12, 0) / 255, new Color3(0, 12, 0) / 255, new Color3(0, 12, 0) / 255, new Color3(10, 0, 0) / 255, new Color3(0, 12, 0) / 255, new Color3(10, 0, 0) / 255, Color3.fromARGB(0x192125) / 10, Color3.fromARGB(0x1F2B1D) / 10, Color3.fromARGB(0x1F2B1D) / 10 };
                        int numFlares = 12;

                        renderDevice.setTexture(0, disk.pointer());
                        for (int i = 0; i < numFlares; ++i)
                        {
                            drawCelestialSphere(renderDevice,
                                 C + (C - L) * (float)position[i], X, Y, (float)size[i],
                                 new Color4(color[i] * lighting.emissiveScale * flareBrightness, 1));
                        }
                    }
                }

            renderDevice.popState();
        }

        /** @deprecated Use renderLensFlare(RenderDevice, LightingParameters) */
        public void renderLensFlare(LightingParameters lighting)
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("This Sky was not created with a RenderDevice.");
            }
            renderLensFlare(renderDevice, lighting);
        }

        /**
         Returns an environment cube map (or the front 2D texture if cube maps are not supported
         on this machine).
         */
        public ReferenceCountedPointer<Texture> getEnvironmentMap()
        {
            if (cubeMap == null)
            {
                return texture[(int)Direction.FT];
            }
            else
            {
                return cubeMap;
            }
        }

        /** True when this sky renders the six individual face textures. */
        public bool usesCubeMap()
        {
            return cubeMap != null;
        }

        /**
         Updates the cached celestial state from the lighting parameters.
         */
        public void update(LightingParameters lighting)
        {
            if (lighting == null)
            {
                throw new ArgumentNullException("lighting");
            }

            if (!hasSunDirection)
            {
                sunDirectionValue = lighting.physicallyCorrect ? lighting.trueSunPosition : lighting.sunPosition;
            }

            if (!hasMoonDirection)
            {
                moonDirectionValue = lighting.physicallyCorrect ? lighting.trueMoonPosition : lighting.moonPosition;
            }

            if (!hasStarFrame)
            {
                starFrameValue = lighting.physicallyCorrect ? lighting.trueStarFrame : lighting.starFrame;
            }

            skyColorValue = lighting.skyAmbient;
        }

        /** Overrides the direction to the sun used when rendering. */
        public void setSun(Vector3 direction)
        {
            sunDirectionValue = direction;
            hasSunDirection = true;
        }

        /** Overrides the direction to the moon used when rendering. */
        public void setMoon(Vector3 direction)
        {
            moonDirectionValue = direction;
            hasMoonDirection = true;
        }

        /** Overrides the star field coordinate frame used when rendering. */
        public void setStar(CoordinateFrame frame)
        {
            starFrameValue = frame;
            hasStarFrame = true;
        }

        /** The direction to the sun used by the last update or setSun call. */
        public Vector3 sunDirection()
        {
            return sunDirectionValue;
        }

        /** The direction to the moon used by the last update or setMoon call. */
        public Vector3 moonDirection()
        {
            return moonDirectionValue;
        }

        /** The direction of the star field used by the last update or setStar call. */
        public Vector3 starDirection()
        {
            return starFrameValue.getLookVector();
        }

        /** The sky color used by the last update call. */
        public Color3 skyColor()
        {
            return skyColorValue;
        }
    }
}
