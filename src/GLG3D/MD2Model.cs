/**
  @file MD2Model.h

  Quake II MD2 file structure.

  @cite http://tfc.duke.free.fr/us/tutorials/models/md2.htm

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-02-21
  @edited 2005-11-20
*/

using System;
using System.IO;

namespace G3D
{
    /**
     Quake II model class.
     <P>
     See demos/MD2Model for an example of how to use this class.

     <P>
     Models are centered about their waist.  To figure out where the feet are you
     might want to look at the bounding box for the stand/walk animations.
     
     <P>
      When available, this class uses SSE instructions for fast vertex blending.
      This cuts the time for getGeometry by a factor of 2.

     <P>This class is not threadsafe; you cannot
     even call methods on two different instances on different threads.

      <P>
     The posed model supplies texture coordinates and normals when rendering
     but the caller must
     bind a texture, set the object to world matrix, and set up lighting.
     <P>
     If VAR has been initialized for the renderDevice, the first time
     this is called (for any Model) a 512k VAR area is allocated.  This
     memory is shared between all MD2 models.
     <P>
     Really huge MD2 models will not fit into this small VAR area and
     will revert to non-VAR rendering.

     <P>
     When getting geometry from the posed model, the normalArray 
     values are interpolated and may have slightly less than unit length.

     <P>
     <IMG SRC="md2000.jpg">
     <DT>
     <IMG SRC="md2001.jpg">
     <DT>
     <IMG SRC="md2002.jpg">
     <DT>
     <IMG SRC="md2003.jpg">
     <DT>

    */
    public class MD2Model : ReferenceCountedObject
    {
        /**
         Amount of time to blend between two animations.
         */
        public static readonly GameTime PRE_BLEND_TIME = new GameTime(1.0 / 8.0);

        /**
         These names are (mostly) from Quake II.
         FLIP, SALUTE, FALLBACK, WAVE, and POINT are all taunts.
         A negative number means to run the specified animation backwards.
         The JUMP animation is Quake's jump animation backwards followed
         by the same animation forwards.
         */
        public enum Animation
        {
            JUMP_UP = -6,
            CROUCH_WALK_BACKWARD = -13,
            RUN_BACKWARD = -1,
            STAND = 0,
            RUN = 1,
            ATTACK = 2,
            PAIN_A = 3,
            PAIN_B = 4,
            PAIN_C = 5,
            JUMP_DOWN = 6,
            FLIP = 7,
            SALUTE = 8,
            FALLBACK = 9,
            WAVE = 10,
            POINT = 11,
            CROUCH_STAND = 12,
            CROUCH_WALK = 13,
            CROUCH_ATTACK = 14,
            CROUCH_PAIN = 15,
            CROUCH_DEATH = 16, 
            DEATH_FALLBACK = 17,
            DEATH_FALLFORWARD = 18,
            DEATH_FALLBACKSLOW = 19,
            JUMP = 20,
            MAX_ANIMATIONS = 21
        }

        public class Pose
        {

            /**
             When time is negative, this frame is blended into the first
             frame of the animation (which will occur at time 0) over PRE_BLEND_TIME.  This allows disjoint animations to be smoothly connected. 
             MD2Model::choosePose will set time to -PRE_BLEND_TIME and set preFrame. If
             you are manually constructing a pose, MD2Model::getFrameNumber
             will return a value you can use.
             */
            public int preFrameNumber;

            public Animation animation;

            /**
             Time since the start of the animation. Animations
             loop, so times after the final animation frame time
             are allowed.
             */
            public GameTime time;

            public Pose()
            {
                animation = Animation.STAND;
                time = 0;
                preFrameNumber = 0;
            }

            public Pose(Animation a)
            {
                animation = a;
                time = 0;
                preFrameNumber = 0;
            }

            public Pose(Animation a, GameTime t)
            {
                animation = a;
                time = t;
                preFrameNumber = 0;
            }

            public static bool operator ==(Pose left, Pose right)
            {
                if (object.ReferenceEquals(left, right))
                {
                    return true;
                }
                if ((object.ReferenceEquals(left, null)) || (object.ReferenceEquals(right, null)))
                {
                    return false;
                }
                return (left.animation == right.animation) && g3dmath.fuzzyEq((double)left.time, (double)right.time);
            }

            public static bool operator !=(Pose left, Pose right)
            {
                return !(left == right);
            }

            public override bool Equals(object obj)
            {
                return this == (obj as Pose);
            }

            public override int GetHashCode()
            {
                return ((int)animation).GetHashCode() ^ time.GetHashCode();
            }

            /**
             Given a time and state flags indicating a character's desires,
             computes the new pose.
             <P>
             This may not be ideal for all applications; it is provided as a 
             helper function.
             <P>
             If any death is triggered while crouching, the crouch death will be
             played instead.
             <P>
             Game logic should generally not use the JUMP animation, or
             the jump parameter to choosePose that triggers it.  Instead, play
             the JUMP_UP animation when the character leaves the ground and
             the JUMP_DOWN animation when they hit it again.
             */
            public void doSimulation(
                GameTime deltaTime,
                bool crouching,
                bool movingForward,
                bool movingBackward,
                bool attack,
                bool jump,
                bool flip,
                bool salute,
                bool fallback,
                bool wave,
                bool point,
                bool death1,
                bool death2,
                bool death3,
                bool pain1,
                bool pain2,
                bool pain3)
            {
                Pose currentPose = new Pose(this.animation, this.time);
                currentPose.preFrameNumber = this.preFrameNumber;

                time = new GameTime((double)time + (double)deltaTime);

                if (animationDeath(animation))
                {
                    // Can't recover from a death pose
                    return;
                }

                if (death1 || death2 || death3)
                {
                    // Death interrupts anything
                    preFrameNumber = getFrameNumber(currentPose);
                    time = new GameTime(-(double)PRE_BLEND_TIME);
                    if (crouching)
                    {
                        animation = Animation.CROUCH_DEATH;
                    }
                    else if (death1)
                    {
                        animation = Animation.DEATH_FALLBACK;
                    }
                    else if (death2)
                    {
                        animation = Animation.DEATH_FALLFORWARD;
                    }
                    else if (death3)
                    {
                        animation = Animation.DEATH_FALLBACKSLOW;
                    }
                    return;
                }

                if ((pain1 || pain2 || pain3) && ! animationPain(animation))
                {
                    // Pain interrupts anything but death
                    preFrameNumber = getFrameNumber(currentPose);
                    time = new GameTime(-(double)PRE_BLEND_TIME);
                    if (crouching)
                    {
                        animation = Animation.CROUCH_PAIN;
                    }
                    else if (pain1)
                    {
                        animation = Animation.PAIN_A;
                    }
                    else if (pain2)
                    {
                        animation = Animation.PAIN_B;
                    }
                    else if (pain3)
                    {
                        animation = Animation.PAIN_C;
                    }
                    return;
                }

                // End of an animation
                if (! animationLoops(animation) &&
                    ((double)time >= (double)animationLength(animation)))
                {
                    animation = Animation.STAND;
                }


                // Run
                if (movingForward)
                {
                    if ((! animationRunForward(animation)) && animationInterruptible(animation))
                    {
                        // Start running
                        animation = Animation.RUN;
                    }
                }
                else if (movingBackward)
                {
                    if ((! animationRunBackward(animation)) && animationInterruptible(animation))
                    {
                        // Start running
                        animation = Animation.RUN_BACKWARD;
                    }
                }
                else
                {
                    if (animationRun(animation))
                    {
                        // Stop running
                        animation = Animation.STAND;
                    }
                }

                if (animationInterruptible(animation))
                {

                    if (attack)
                    {
                        animation = Animation.ATTACK;
                    }
                    else if (jump && ! animationJump(animation))
                    {
                        animation = Animation.JUMP;
                    }
                    else if (flip)
                    {
                        animation = Animation.FLIP;
                    }
                    else if (salute)
                    {
                        animation = Animation.SALUTE;
                    }
                    else if (fallback)
                    {
                        animation = Animation.FALLBACK;
                    }
                    else if (wave)
                    {
                        animation = Animation.WAVE;
                    }
                    else if (point)
                    {
                        animation = Animation.POINT;
                    }
                }

                // Crouch
                if (crouching)
                {
                    // Move to a crouch if necessary
                    switch (animation)
                    {
                    case Animation.STAND:
                        animation = Animation.CROUCH_STAND;
                        break;

                    case Animation.RUN:
                        animation = Animation.CROUCH_WALK;
                        break;

                    case Animation.RUN_BACKWARD:
                        animation = Animation.CROUCH_WALK_BACKWARD;
                        break;

                    case Animation.ATTACK:
                        animation = Animation.CROUCH_ATTACK;
                        break;

                    default:
                        // We don't allow crouch during pain or other actions
                        break;
                    }
                }
                else
                {

                    // Move to a crouch if necessary
                    switch (animation)
                    {
                    case Animation.CROUCH_STAND:
                        animation = Animation.STAND;
                        break;

                    case Animation.CROUCH_WALK:
                        animation = Animation.RUN;
                        break;

                    case Animation.CROUCH_WALK_BACKWARD:
                        animation = Animation.RUN_BACKWARD;
                        break;

                    case Animation.CROUCH_ATTACK:
                        animation = Animation.ATTACK;
                        break;

                    default:
                        // We don't allow standing up during pain or other actions
                        break;
                    }
                }

                // Blend in old animation
                if (animation != currentPose.animation)
                {
                    preFrameNumber = getFrameNumber(currentPose);
                    time = new GameTime(-(double)PRE_BLEND_TIME);
                }
            }
        }


        protected class PosedMD2Model : PosedModel
        {
            public ReferenceCountedPointer<MD2Model> model;
            public CoordinateFrame cframe;
            public Pose pose;
            public bool useMaterial;
            public GMaterial material;

            /** Filled out the first time objectSpaceGeometry is called */
            public MeshAlg.Geometry geometry = new MeshAlg.Geometry();

            /** Computed on the first call to objectSpaceFaceNormals */
            public Array<Vector3> faceNormals = new Array<Vector3>();

            public PosedMD2Model(
                ReferenceCountedPointer<MD2Model> _model,
                CoordinateFrame _cframe,
                Pose _pose,
                bool _useMat,
                GMaterial _mat)
            {
                model = _model;
                cframe = _cframe;
                pose = _pose;
                useMaterial = _useMat;
                material = _mat;
            }

            public override string name()
            {
                return model.pointer().name();
            }

            public override void getCoordinateFrame(out CoordinateFrame c)
            {
                c = cframe;
            }

            public override MeshAlg.Geometry objectSpaceGeometry()
            {
                if (geometry.vertexArray.size() == 0)
                {
                    model.pointer().getGeometry(pose, geometry);
                }

                return geometry;
            }

            public override Array<int> triangleIndices()
            {
                return model.pointer().indexArray;
            }

            public override Array<MeshAlg.Face> faces()
            {
                return model.pointer().faceArray;
            }

            public override Array<MeshAlg.Edge> edges()
            {
                return model.pointer().edgeArray;
            }

            public override Array<MeshAlg.Vertex> vertices()
            {
                return model.pointer().vertexArray;
            }

            public override Array<MeshAlg.Face> weldedFaces()
            {
                return model.pointer().weldedFaceArray;
            }

            public override Array<MeshAlg.Edge> weldedEdges()
            {
                return model.pointer().weldedEdgeArray;
            }

            public override Array<MeshAlg.Vertex> weldedVertices()
            {
                return model.pointer().weldedVertexArray;
            }

            public override void getObjectSpaceBoundingSphere(out Sphere s)
            {
                s = model.pointer().animationBoundingSphere[g3dmath.iAbs((int)pose.animation)];
            }

            public override void getObjectSpaceBoundingBox(out Box b)
            {
                b = model.pointer().animationBoundingBox[g3dmath.iAbs((int)pose.animation)];
            }

            public override int numBoundaryEdges()
            {
                return model.pointer().numBoundaryEdges;
            }

            public override int numWeldedBoundaryEdges()
            {
                return model.pointer().numWeldedBoundaryEdges;
            }

            public override void render(RenderDevice renderDevice)
            {
                renderDevice.pushState();
                    renderDevice.setObjectToWorldMatrix(coordinateFrame());
                    if (useMaterial && renderDevice.colorWrite())
                    {
                        material.configure(renderDevice);
                    }
                    model.pointer().render(renderDevice, pose);
                renderDevice.popState();
            }

            public override Array<Vector3> objectSpaceFaceNormals(bool normalize)
            {
                if (faceNormals.size() == 0)
                {
                    MeshAlg.computeFaceNormals(objectSpaceGeometry().vertexArray, faces(), faceNormals, true);
                }

                return faceNormals;
            }
        }

        protected class PackedGeometry
        {
            public Array<Vector3> vertexArray = new Array<Vector3>();

            /** Indices into the normalTable. */
            public Array<byte> normalArray = new Array<byte>();
        }

        /**
         Quake uses a set of canonical normal vectors.
         */
        protected static Vector3[] normalTable = new Vector3[162];

        /** Loads data into the normalTable. */
        protected static void setNormalTable()
        {
            if (normalTable[0].y != 0)
            {
                // The table has already been initialized
                return;
            }

            // Initialize the global table
            normalTable[  0] = new Vector3(0.000000f, 0.850651f, 0.525731f);
            normalTable[  1] = new Vector3(-0.238856f, 0.864188f, 0.442863f);
            normalTable[  2] = new Vector3(0.000000f, 0.955423f, 0.295242f);
            normalTable[  3] = new Vector3(-0.500000f, 0.809017f, 0.309017f);
            normalTable[  4] = new Vector3(-0.262866f, 0.951056f, 0.162460f);
            normalTable[  5] = new Vector3(0.000000f, 1.000000f, 0.000000f);
            normalTable[  6] = new Vector3(-0.850651f, 0.525731f, 0.000000f);
            normalTable[  7] = new Vector3(-0.716567f, 0.681718f, 0.147621f);
            normalTable[  8] = new Vector3(-0.716567f, 0.681718f, -0.147621f);
            normalTable[  9] = new Vector3(-0.525731f, 0.850651f, 0.000000f);
            normalTable[ 10] = new Vector3(-0.500000f, 0.809017f, -0.309017f);
            normalTable[ 11] = new Vector3(0.000000f, 0.850651f, -0.525731f);
            normalTable[ 12] = new Vector3(0.000000f, 0.955423f, -0.295242f);
            normalTable[ 13] = new Vector3(-0.238856f, 0.864188f, -0.442863f);
            normalTable[ 14] = new Vector3(-0.262866f, 0.951056f, -0.162460f);
            normalTable[ 15] = new Vector3(-0.147621f, 0.716567f, 0.681718f);
            normalTable[ 16] = new Vector3(-0.309017f, 0.500000f, 0.809017f);
            normalTable[ 17] = new Vector3(-0.425325f, 0.688191f, 0.587785f);
            normalTable[ 18] = new Vector3(-0.525731f, 0.000000f, 0.850651f);
            normalTable[ 19] = new Vector3(-0.442863f, 0.238856f, 0.864188f);
            normalTable[ 20] = new Vector3(-0.681718f, 0.147621f, 0.716567f);
            normalTable[ 21] = new Vector3(-0.587785f, 0.425325f, 0.688191f);
            normalTable[ 22] = new Vector3(-0.809017f, 0.309017f, 0.500000f);
            normalTable[ 23] = new Vector3(-0.864188f, 0.442863f, 0.238856f);
            normalTable[ 24] = new Vector3(-0.688191f, 0.587785f, 0.425325f);
            normalTable[ 25] = new Vector3(-0.681718f, -0.147621f, 0.716567f);
            normalTable[ 26] = new Vector3(-0.809017f, -0.309017f, 0.500000f);
            normalTable[ 27] = new Vector3(-0.850651f, 0.000000f, 0.525731f);
            normalTable[ 28] = new Vector3(-0.850651f, -0.525731f, 0.000000f);
            normalTable[ 29] = new Vector3(-0.864188f, -0.442863f, 0.238856f);
            normalTable[ 30] = new Vector3(-0.955423f, -0.295242f, 0.000000f);
            normalTable[ 31] = new Vector3(-0.951056f, -0.162460f, 0.262866f);
            normalTable[ 32] = new Vector3(-1.000000f, 0.000000f, 0.000000f);
            normalTable[ 33] = new Vector3(-0.955423f, 0.295242f, 0.000000f);
            normalTable[ 34] = new Vector3(-0.951056f, 0.162460f, 0.262866f);
            normalTable[ 35] = new Vector3(-0.864188f, 0.442863f, -0.238856f);
            normalTable[ 36] = new Vector3(-0.951056f, 0.162460f, -0.262866f);
            normalTable[ 37] = new Vector3(-0.809017f, 0.309017f, -0.500000f);
            normalTable[ 38] = new Vector3(-0.864188f, -0.442863f, -0.238856f);
            normalTable[ 39] = new Vector3(-0.951056f, -0.162460f, -0.262866f);
            normalTable[ 40] = new Vector3(-0.809017f, -0.309017f, -0.500000f);
            normalTable[ 41] = new Vector3(-0.525731f, 0.000000f, -0.850651f);
            normalTable[ 42] = new Vector3(-0.681718f, 0.147621f, -0.716567f);
            normalTable[ 43] = new Vector3(-0.681718f, -0.147621f, -0.716567f);
            normalTable[ 44] = new Vector3(-0.850651f, 0.000000f, -0.525731f);
            normalTable[ 45] = new Vector3(-0.688191f, 0.587785f, -0.425325f);
            normalTable[ 46] = new Vector3(-0.442863f, 0.238856f, -0.864188f);
            normalTable[ 47] = new Vector3(-0.587785f, 0.425325f, -0.688191f);
            normalTable[ 48] = new Vector3(-0.309017f, 0.500000f, -0.809017f);
            normalTable[ 49] = new Vector3(-0.147621f, 0.716567f, -0.681718f);
            normalTable[ 50] = new Vector3(-0.425325f, 0.688191f, -0.587785f);
            normalTable[ 51] = new Vector3(-0.295242f, 0.000000f, -0.955423f);
            normalTable[ 52] = new Vector3(0.000000f, 0.000000f, -1.000000f);
            normalTable[ 53] = new Vector3(-0.162460f, 0.262866f, -0.951056f);
            normalTable[ 54] = new Vector3(0.525731f, 0.000000f, -0.850651f);
            normalTable[ 55] = new Vector3(0.295242f, 0.000000f, -0.955423f);
            normalTable[ 56] = new Vector3(0.442863f, 0.238856f, -0.864188f);
            normalTable[ 57] = new Vector3(0.162460f, 0.262866f, -0.951056f);
            normalTable[ 58] = new Vector3(0.309017f, 0.500000f, -0.809017f);
            normalTable[ 59] = new Vector3(0.147621f, 0.716567f, -0.681718f);
            normalTable[ 60] = new Vector3(0.000000f, 0.525731f, -0.850651f);
            normalTable[ 61] = new Vector3(-0.442863f, -0.238856f, -0.864188f);
            normalTable[ 62] = new Vector3(-0.309017f, -0.500000f, -0.809017f);
            normalTable[ 63] = new Vector3(-0.162460f, -0.262866f, -0.951056f);
            normalTable[ 64] = new Vector3(0.000000f, -0.850651f, -0.525731f);
            normalTable[ 65] = new Vector3(-0.147621f, -0.716567f, -0.681718f);
            normalTable[ 66] = new Vector3(0.147621f, -0.716567f, -0.681718f);
            normalTable[ 67] = new Vector3(0.000000f, -0.525731f, -0.850651f);
            normalTable[ 68] = new Vector3(0.309017f, -0.500000f, -0.809017f);
            normalTable[ 69] = new Vector3(0.442863f, -0.238856f, -0.864188f);
            normalTable[ 70] = new Vector3(0.162460f, -0.262866f, -0.951056f);
            normalTable[ 71] = new Vector3(-0.716567f, -0.681718f, -0.147621f);
            normalTable[ 72] = new Vector3(-0.500000f, -0.809017f, -0.309017f);
            normalTable[ 73] = new Vector3(-0.688191f, -0.587785f, -0.425325f);
            normalTable[ 74] = new Vector3(-0.238856f, -0.864188f, -0.442863f);
            normalTable[ 75] = new Vector3(-0.425325f, -0.688191f, -0.587785f);
            normalTable[ 76] = new Vector3(-0.587785f, -0.425325f, -0.688191f);
            normalTable[ 77] = new Vector3(-0.716567f, -0.681718f, 0.147621f);
            normalTable[ 78] = new Vector3(-0.500000f, -0.809017f, 0.309017f);
            normalTable[ 79] = new Vector3(-0.525731f, -0.850651f, 0.000000f);
            normalTable[ 80] = new Vector3(0.000000f, -0.850651f, 0.525731f);
            normalTable[ 81] = new Vector3(-0.238856f, -0.864188f, 0.442863f);
            normalTable[ 82] = new Vector3(0.000000f, -0.955423f, 0.295242f);
            normalTable[ 83] = new Vector3(-0.262866f, -0.951056f, 0.162460f);
            normalTable[ 84] = new Vector3(0.000000f, -1.000000f, 0.000000f);
            normalTable[ 85] = new Vector3(0.000000f, -0.955423f, -0.295242f);
            normalTable[ 86] = new Vector3(-0.262866f, -0.951056f, -0.162460f);
            normalTable[ 87] = new Vector3(0.238856f, -0.864188f, 0.442863f);
            normalTable[ 88] = new Vector3(0.500000f, -0.809017f, 0.309017f);
            normalTable[ 89] = new Vector3(0.262866f, -0.951056f, 0.162460f);
            normalTable[ 90] = new Vector3(0.850651f, -0.525731f, 0.000000f);
            normalTable[ 91] = new Vector3(0.716567f, -0.681718f, 0.147621f);
            normalTable[ 92] = new Vector3(0.716567f, -0.681718f, -0.147621f);
            normalTable[ 93] = new Vector3(0.525731f, -0.850651f, 0.000000f);
            normalTable[ 94] = new Vector3(0.500000f, -0.809017f, -0.309017f);
            normalTable[ 95] = new Vector3(0.238856f, -0.864188f, -0.442863f);
            normalTable[ 96] = new Vector3(0.262866f, -0.951056f, -0.162460f);
            normalTable[ 97] = new Vector3(0.864188f, -0.442863f, -0.238856f);
            normalTable[ 98] = new Vector3(0.809017f, -0.309017f, -0.500000f);
            normalTable[ 99] = new Vector3(0.688191f, -0.587785f, -0.425325f);
            normalTable[100] = new Vector3(0.681718f, -0.147621f, -0.716567f);
            normalTable[101] = new Vector3(0.587785f, -0.425325f, -0.688191f);
            normalTable[102] = new Vector3(0.425325f, -0.688191f, -0.587785f);
            normalTable[103] = new Vector3(0.955423f, -0.295242f, 0.000000f);
            normalTable[104] = new Vector3(1.000000f, 0.000000f, 0.000000f);
            normalTable[105] = new Vector3(0.951056f, -0.162460f, -0.262866f);
            normalTable[106] = new Vector3(0.850651f, 0.525731f, 0.000000f);
            normalTable[107] = new Vector3(0.955423f, 0.295242f, 0.000000f);
            normalTable[108] = new Vector3(0.864188f, 0.442863f, -0.238856f);
            normalTable[109] = new Vector3(0.951056f, 0.162460f, -0.262866f);
            normalTable[110] = new Vector3(0.809017f, 0.309017f, -0.500000f);
            normalTable[111] = new Vector3(0.681718f, 0.147621f, -0.716567f);
            normalTable[112] = new Vector3(0.850651f, 0.000000f, -0.525731f);
            normalTable[113] = new Vector3(0.864188f, -0.442863f, 0.238856f);
            normalTable[114] = new Vector3(0.809017f, -0.309017f, 0.500000f);
            normalTable[115] = new Vector3(0.951056f, -0.162460f, 0.262866f);
            normalTable[116] = new Vector3(0.525731f, 0.000000f, 0.850651f);
            normalTable[117] = new Vector3(0.681718f, 0.000000f, 0.716567f);
            normalTable[118] = new Vector3(0.681718f, 0.147621f, 0.716567f);
            normalTable[119] = new Vector3(0.850651f, 0.000000f, 0.525731f);
            normalTable[120] = new Vector3(0.809017f, 0.309017f, 0.500000f);
            normalTable[121] = new Vector3(0.864188f, 0.442863f, 0.238856f);
            normalTable[122] = new Vector3(0.951056f, 0.162460f, 0.262866f);
            normalTable[123] = new Vector3(0.442863f, 0.238856f, 0.864188f);
            normalTable[124] = new Vector3(0.309017f, 0.500000f, 0.809017f);
            normalTable[125] = new Vector3(0.587785f, 0.425325f, 0.688191f);
            normalTable[126] = new Vector3(0.147621f, 0.716567f, 0.681718f);
            normalTable[127] = new Vector3(0.238856f, 0.864188f, 0.442863f);
            normalTable[128] = new Vector3(0.425325f, 0.688191f, 0.587785f);
            normalTable[129] = new Vector3(0.500000f, 0.809017f, 0.309017f);
            normalTable[130] = new Vector3(0.716567f, 0.681718f, 0.147621f);
            normalTable[131] = new Vector3(0.688191f, 0.587785f, 0.425325f);
            normalTable[132] = new Vector3(0.262866f, 0.951056f, 0.162460f);
            normalTable[133] = new Vector3(0.238856f, 0.864188f, -0.442863f);
            normalTable[134] = new Vector3(0.262866f, 0.951056f, -0.162460f);
            normalTable[135] = new Vector3(0.500000f, 0.809017f, -0.309017f);
            normalTable[136] = new Vector3(0.716567f, 0.681718f, -0.147621f);
            normalTable[137] = new Vector3(0.525731f, 0.850651f, 0.000000f);
            normalTable[138] = new Vector3(0.688191f, 0.587785f, -0.425325f);
            normalTable[139] = new Vector3(0.425325f, 0.688191f, -0.587785f);
            normalTable[140] = new Vector3(0.587785f, 0.425325f, -0.688191f);
            normalTable[141] = new Vector3(-0.295242f, 0.000000f, 0.955423f);
            normalTable[142] = new Vector3(-0.162460f, 0.262866f, 0.951056f);
            normalTable[143] = new Vector3(0.000000f, 0.000000f, 1.000000f);
            normalTable[144] = new Vector3(0.000000f, 0.525731f, 0.850651f);
            normalTable[145] = new Vector3(0.295242f, 0.000000f, 0.955423f);
            normalTable[146] = new Vector3(0.162460f, 0.262866f, 0.951056f);
            normalTable[147] = new Vector3(-0.442863f, -0.238856f, 0.864188f);
            normalTable[148] = new Vector3(-0.162460f, -0.262866f, 0.951056f);
            normalTable[149] = new Vector3(-0.309017f, -0.500000f, 0.809017f);
            normalTable[150] = new Vector3(0.442863f, -0.238856f, 0.864188f);
            normalTable[151] = new Vector3(0.162460f, -0.262866f, 0.951056f);
            normalTable[152] = new Vector3(0.309017f, -0.500000f, 0.809017f);
            normalTable[153] = new Vector3(-0.147621f, -0.716567f, 0.681718f);
            normalTable[154] = new Vector3(0.147621f, -0.716567f, 0.681718f);
            normalTable[155] = new Vector3(0.000000f, -0.525731f, 0.850651f);
            normalTable[156] = new Vector3(-0.587785f, -0.425325f, 0.688191f);
            normalTable[157] = new Vector3(-0.425325f, -0.688191f, 0.587785f);
            normalTable[158] = new Vector3(-0.688191f, -0.587785f, 0.425325f);
            normalTable[159] = new Vector3(0.688191f, -0.587785f, 0.425325f);
            normalTable[160] = new Vector3(0.425325f, -0.688191f, 0.587785f);
            normalTable[161] = new Vector3(0.587785f, -0.425325f, 0.688191f);
        }

        protected class MD2AnimInfo
        {
            public int first;
            public int last;
            public int fps;
            public bool loops;

            public MD2AnimInfo(int first, int last, int fps, bool loops)
            {
                this.first = first;
                this.last = last;
                this.fps = fps;
                this.loops = loops;
            }
        }

        /**
         Information relating Animations to keyFrames.  Used by computeFrameNumbers().
         */
        protected static readonly MD2AnimInfo[] animationTable = new MD2AnimInfo[]
        {
            // first, last, fps
            new MD2AnimInfo(  0,  39,  9, true ),    // STAND
            new MD2AnimInfo( 40,  45, 10, true ),    // RUN
            new MD2AnimInfo( 46,  53, 10, false ),   // ATTACK
            new MD2AnimInfo( 54,  57,  7, false ),   // PAIN_A
            new MD2AnimInfo( 58,  61,  7, false ),   // PAIN_B
            new MD2AnimInfo( 62,  65,  7, false ),   // PAIN_C
            new MD2AnimInfo( 66,  71,  7, false ),   // JUMP_DOWN
            new MD2AnimInfo( 72,  83,  7, false ),   // FLIP
            new MD2AnimInfo( 84,  94,  7, false ),   // SALUTE
            new MD2AnimInfo( 95, 111, 10, false ),   // FALLBACK
            new MD2AnimInfo(112, 122,  7, false ),   // WAVE
            new MD2AnimInfo(123, 134,  6, false ),   // POINT
            new MD2AnimInfo(135, 153, 10, true ),    // CROUCH_STAND
            new MD2AnimInfo(154, 159,  7, true ),    // CROUCH_WALK
            new MD2AnimInfo(160, 168, 10, false ),   // CROUCH_ATTACK
            new MD2AnimInfo(169, 172,  7, false ),   // CROUCH_PAIN
            new MD2AnimInfo(173, 177,  5, false ),   // CROUCH_DEATH
            new MD2AnimInfo(178, 183,  7, false ),   // DEATH_FALLBACK
            new MD2AnimInfo(184, 189,  7, false ),   // DEATH_FALLFORWARD
            new MD2AnimInfo(190, 197,  7, false ),   // DEATH_FALLBACKSLOW
            // JUMP is not in the table; it is handled specially
        };

        /**
         Computes the previous and next frame indices and how far we are between them.
         */
        protected static void computeFrameNumbers(Pose pose, out int kf0, out int kf1, out double alpha)
        {

            if ((double)pose.time < 0)
            {
                Animation blendAnim = pose.animation;
                
                if (pose.animation == Animation.JUMP)
                {
                    blendAnim = Animation.JUMP_UP;
                }
                else
                {
                }

                Debug.debugAssert(g3dmath.iAbs((int)blendAnim) < (int)Animation.MAX_ANIMATIONS);

                if ((int)blendAnim >= 0)
                {
                    kf1 = animationTable[(int)blendAnim].first;
                }
                else
                {
                    kf1 = animationTable[g3dmath.iAbs((int)blendAnim)].last;
                }

                alpha = g3dmath.clamp(1 + ((double)pose.time / (double)PRE_BLEND_TIME), 0, 1);

                if ((pose.preFrameNumber >= 0) && (pose.preFrameNumber < 197))
                {
                    kf0 = pose.preFrameNumber;
                }
                else
                {
                    // Illegal pose number; just hold the first frame
                    kf0 = kf1;
                }

                return;
            }


            // Assume time is positive
            if (pose.animation == Animation.JUMP)
            {
                // Jump is special because it is two animations pasted together

                // Time to jump up (== time to jump down)
                GameTime upTime = animationLength(Animation.JUMP_UP);

                // Make the time on the right interval
                GameTime jumpTime = new GameTime(
                    g3dmath.iWrap(g3dmath.iRound((double)pose.time * 1000),
                                  g3dmath.iRound(1000 * (double)upTime * (2 + hangTimePct))) / 1000.0);

                if ((double)jumpTime < (double)upTime)
                {
                    // Jump up
                    computeFrameNumbers(new Pose(Animation.JUMP_UP, jumpTime), out kf0, out kf1, out alpha);
                }
                else if ((double)jumpTime < (double)upTime * (1 + hangTimePct))
                {
                    // Hold at the peak
                    computeFrameNumbers(new Pose(Animation.JUMP_UP, upTime), out kf0, out kf1, out alpha);
                }
                else
                {
                    // Jump down
                    computeFrameNumbers(new Pose(Animation.JUMP_DOWN, new GameTime((double)jumpTime - (double)upTime * (1 + hangTimePct))), out kf0, out kf1, out alpha);
                }

                return;
            }

            Animation a = (Animation)g3dmath.iAbs((int)pose.animation);
            Debug.debugAssert((int)a < (int)Animation.MAX_ANIMATIONS);

            // Figure out how far between frames we are
            MD2AnimInfo anim = animationTable[(int)a];
            GameTime time = pose.time;
            GameTime len = animationLength(a);

            if ((int)pose.animation < 0)
            {
                // Run the animation backwards
                time = new GameTime((double)len - (double)time);
            }

            int totalFrames = anim.last - anim.first + 1;

            // Number of frames into the animation
            double frames = (double)time * anim.fps;

            int iframes   = g3dmath.iFloor(frames);

            if (anim.loops)
            {
                kf0   = anim.first + g3dmath.iWrap(iframes, totalFrames);
                kf1   = anim.first + g3dmath.iWrap((iframes + 1), totalFrames);
                alpha = frames - iframes;
            }
            else
            {
                kf0   = anim.first + g3dmath.iMin(iframes, totalFrames - 1);
                kf1   = anim.first + g3dmath.iMin((iframes + 1), totalFrames - 1);
                alpha = frames - iframes;
            }
        }

        /** How long we hold in the air as a fraction of jump time. */
        protected const double hangTimePct = 0.1;

        /**
         The pose currently stored in interpolatedFrame.  When the animation is MAX_ANIMATIONS
            interpolatedFrame is not yet initialized. */
        protected static MD2Model interpolatedModel;
        protected static Pose interpolatedPose = new Pose();
        protected static MeshAlg.Geometry interpolatedFrame = new MeshAlg.Geometry();

        protected const int NUM_VAR_AREAS = 10;
        protected const int NONE_ALLOCATED = -1;

        /** Shared dynamic vertex arrays. Allocated by allocateVertexArrays.
            We cycle through multiple VARAreas because the models are so small
            that we can send data to the card faster than it can be rendered
            and we end up spending all of our time waiting on the GPU.*/
        protected static ReferenceCountedPointer<VARArea>[] varArea = new ReferenceCountedPointer<VARArea>[NUM_VAR_AREAS];

        /**
         When NONE_ALLOCATED, the vertex arrays have not been allocated. 
         */
        protected static int nextVarArea = NONE_ALLOCATED;

        protected Array<string> _textureFilenames = new Array<string>();

        /**
         One RenderDevice primitive
         */
        protected class Primitive
        {
            /** RenderDevice::TRIANGLE_STRIP or RenderDevice::TRIANGLE_FAN */
            public RenderDevice.Primitive type;

            public class PVertex
            {
                /** Indices into a MeshAlg::Geometry's vertexArray */
                public int index;

                /** One texture coordinate for each index */
                public Vector2 texCoord;
            }

            public Array<PVertex> pvertexArray = new Array<PVertex>();
        }

        protected Array<PackedGeometry> keyFrame = new Array<PackedGeometry>();

        protected Array<Primitive> primitiveArray = new Array<Primitive>();

        /**
         Texture array that parallels vertex and normal arrays.
         Set up by computeTexCoords
        */
        protected Array<Vector2int16> _texCoordArray = new Array<Vector2int16>();

        /**
         Matrix mapping int16 texture coordinates to floats.
         */
        protected CoordinateFrame texFrame = CoordinateFrame.identity();

        protected Sphere[] animationBoundingSphere = new Sphere[(int)Animation.MAX_ANIMATIONS]; 
        protected Box[] animationBoundingBox = new Box[(int)Animation.MAX_ANIMATIONS]; 

        protected void loadTextureFilenames(BinaryInput b, int num, int offset)
        {
            _textureFilenames.resize(num);
            b.setPosition(offset);
            for (int t = 0; t < num; ++t)
            {
                _textureFilenames[t] = b.readString();
            }
        }

        /**
         Creates a texCoordArray to parallel the vertex and normal arrays,
         duplicating vertices in the keyframes as needed. Called from load().
         @param inCoords Texture coords corresponding to the index array
         */
        protected void computeTexCoords(Array<Vector2int16> inCoords)
        {
            int numVertices = keyFrame[0].vertexArray.size();

            // Table mapping original vertex indices to alternative locations
            // for that vertex (corresponding to different texture coords).
            // A zero length array means a vertex that was not yet seen.
            Array<Array<int>> cloneListArray = new Array<Array<int>>();
            cloneListArray.resize(numVertices);
            for (int i = 0; i < numVertices; ++i)
            {
                cloneListArray[i] = new Array<int>();
            }

            _texCoordArray.resize(numVertices);

            // Walk through the index array and inCoords array
            for (int i = 0; i < indexArray.size(); ++i)
            {
                // Texture coords
                Vector2int16 coords = inCoords[i];

                // Vertex index
                int v = indexArray[i];

                // cloneList[vertex index] = vertex index of clone
                Array<int> cloneList = cloneListArray[v];

                if (cloneList.size() == 0)
                {
                    // We've never seen this vertex before, so assign it
                    // the texture coordinates we already have.
                    cloneList.append(v);
                    _texCoordArray[v] = coords;

                }
                else
                {
                    bool foundMatch = false;

                    // Walk through the clones and see if one has the same tex coords
                    for (int c = 0; c < cloneList.size(); ++c)
                    {
                        int clone = cloneList[c];

                        if (_texCoordArray[clone] == coords)
                        {
                            // Found a match
                            foundMatch = true;

                            // Replace the index with the clone's index
                            indexArray[i] = clone;

                            // The texture coordinates are already assigned
                            break;
                        }
                    }

                    if (! foundMatch)
                    {
                        // We have a new vertex.  Add it to the list of vertices
                        // cloned from the original.
                        int clone = numVertices;
                        cloneList.append(clone);

                        // Overwrite the index array entry
                        indexArray[i] = clone;

                        // Add the texture coordinates
                        _texCoordArray.append(coords);

                        // Clone the vertex in every key pose.
                        for (int k = 0; k < keyFrame.size(); ++k)
                        {
                            keyFrame[k].vertexArray.append(keyFrame[k].vertexArray[v]);
                            keyFrame[k].normalArray.append(keyFrame[k].normalArray[v]);
                        }

                        ++numVertices;
                    }
                }
            }
        }

        /**
          Called from render() to create the vertex arrays.  Assumes VAR is
          available and the arrays are not initialized.
         */
        protected void allocateVertexArrays(RenderDevice renderDevice)
        {
            long size = maxVARVerts * (24 + 12 * 2 + 4);

            for (int i = 0; i < NUM_VAR_AREAS; ++i)
            {
                if (varArea[i] != null)
                {
                    varArea[i].Dispose();
                }
                varArea[i] = new ReferenceCountedPointer<VARArea>(VARArea.create(size));
            }

            if ((varArea[NUM_VAR_AREAS - 1] == null) || varArea[NUM_VAR_AREAS - 1].isNull())
            {
                nextVarArea = NONE_ALLOCATED;
                Log.common().println("\n*******\nCould not allocate vertex arrays.");
                return;
            }

            nextVarArea = 0;
        }

        /**
         Triangle list array useful for generating all of the triangles,
         e.g. for collision detection.  Not used for rendering.
         */
        protected Array<int> indexArray = new Array<int>();

        /** Called from create */
        private MD2Model()
        {
        }

        /** Called from create */
        private void load(string filename, float scale)
        {

            // If models are being reloaded it is dangerous to trust the interpolation cache.
            interpolatedModel = null;

            if (! FileUtils.fileExists(filename))
            {
                throw new FileNotFoundException("Can't find \"" + filename + "\"", filename);
            }

            setNormalTable();

            // Clear out
            reset();

            BinaryInput b = new BinaryInput(filename, G3DEndian.G3D_LITTLE_ENDIAN);

            MD2ModelHeader header = new MD2ModelHeader();

            header.deserialize(b);
            Debug.debugAssert(header.version == 8);
            Debug.debugAssert(header.numVertices <= 4096);

            keyFrame.resize(header.numFrames);
            for (int f = 0; f < keyFrame.size(); ++f)
            {
                keyFrame[f] = new PackedGeometry();
            }
            Array<Vector3> frameMin = new Array<Vector3>(header.numFrames); 
            Array<Vector3> frameMax = new Array<Vector3>(header.numFrames);
            Array<double> frameRad = new Array<double>(header.numFrames);

            Vector3 min  = Vector3.inf();
            Vector3 max  = -Vector3.inf();
            double  rad  = 0;

            if (header.numVertices < 3)
            {
                Log.common().printf("\n*****************\nWarning: \"%s\" is corrupted and is not being loaded.\n", filename);
                return;
            }

            loadTextureFilenames(b, header.numSkins, header.offsetSkins);

            for (int f = 0; f < keyFrame.size(); ++f)
            {
                MD2Frame md2Frame = new MD2Frame();

                b.setPosition(header.offsetFrames + f * header.frameSize);
                md2Frame.deserialize(b);

                // Read the vertices for the frame
                keyFrame[f].vertexArray.resize(header.numVertices);
                keyFrame[f].normalArray.resize(header.numVertices);

                // Per-pose bounds
                Vector3 min_1  = Vector3.inf();
                Vector3 max_1  = -Vector3.inf();
                double  rad_1  = 0;

                // Quake's axes are permuted and scaled
                double[] vertexScale   = {-.07, .07, -.07};
                int[] permute = {2, 0, 1};
                int v, i;
                for (v = 0; v < header.numVertices; ++v)
                {

                    Vector3 vertex = new Vector3();
                    for (i = 0; i < 3; ++i)
                    {
                        vertex[permute[i]] = (float)((b.readUInt8() * md2Frame.scale[i] + md2Frame.translate[i]) * vertexScale[permute[i]]);
                    }

                    vertex *= scale;

                    byte normalIndex = b.readUInt8();
                    Debug.debugAssert(normalIndex < 162);
                    keyFrame[f].vertexArray[v] = vertex;
                    keyFrame[f].normalArray[v] = normalIndex;

                    min_1 = min_1.min(vertex);
                    max_1 = max_1.max(vertex);

                    if (vertex.squaredMagnitude() > rad_1)
                    {
                        rad_1 = vertex.squaredMagnitude();
                    }
                }

                frameMin[f] = min_1;
                frameMax[f] = max_1;
                frameRad[f] = Math.Sqrt(rad_1);

                min = min.min(min_1);
                max = max.max(max_1);

                if (rad_1 > rad)
                {
                    rad = rad_1;
                }
            }

            // Compute per-animation bounds based on frame bounds
            for (int a = 0; a < (int)Animation.JUMP; ++a)
            {
                int first = animationTable[a].first;
                int last  = animationTable[a].last;

                if ((first < header.numFrames) && (last < header.numFrames))
                {
                    Vector3 animMin = frameMin[first];
                    Vector3 animMax = frameMax[first];
                    double  animRad = frameRad[first];

                    for (int i = first + 1; i <= last; ++i)
                    {
                        animMin = animMin.min(frameMin[i]);
                        animMax = animMax.max(frameMax[i]);
                        animRad = Math.Max(animRad, frameRad[i]);
                    }

                    animationBoundingBox[a] = new Box(animMin, animMax);

					// Sometimes the sphere bounding the box is tighter than the one we calculated.
					double boxRadSq = (animMax - animMin).squaredMagnitude() * 0.25;

					if (boxRadSq >= animRad * animRad)
                    {
						animationBoundingSphere[a] = new Sphere(Vector3.zero(), (float)animRad);
					}
                    else
                    {
						animationBoundingSphere[a] = new Sphere((animMax + animMin) * 0.5, (float)Math.Sqrt(boxRadSq));
					}

                } else
                {
                    // This animation is not supported by this model
                    animationBoundingBox[a] = new Box(Vector3.zero(), Vector3.zero());
                    animationBoundingSphere[a] = new Sphere(Vector3.zero(), 0);
                }
            }

            animationBoundingBox[(int)Animation.JUMP] = animationBoundingBox[(int)Animation.JUMP_DOWN];
            animationBoundingSphere[(int)Animation.JUMP] = animationBoundingSphere[(int)Animation.JUMP_DOWN];

            boundingBox    = new Box(min, max);
            boundingSphere = new Sphere(Vector3.zero(), (float)Math.Sqrt(rad));

            // Load the texture coords
            texFrame.rotation.m00 = (float)(1.0 / header.skinWidth); 
            texFrame.rotation.m11 = (float)(1.0 / header.skinHeight);

            Array<Vector2int16> fileTexCoords = new Array<Vector2int16>(header.numTexCoords);
            b.setPosition(header.offsetTexCoords);
            for (int t = 0; t < fileTexCoords.size(); ++t)
            {
                Vector2int16 texCoord = new Vector2int16();
                texCoord.x = (short)b.readUInt16();
                texCoord.y = (short)b.readUInt16();
                fileTexCoords[t] = texCoord;
            }

            // The indices for the texture coords (which don't match the
            // vertex indices originally).
            indexArray.resize(header.numTriangles * 3);
            Array<Vector2int16> index_texCoordArray = new Array<Vector2int16>(indexArray.size());

            // Read the triangles, reversing them to get triangle list order
            b.setPosition(header.offsetTriangles);
            for (int t = header.numTriangles - 1; t >= 0; --t)
            {

                for (int i = 2; i >= 0; --i)
                {
                    indexArray[t * 3 + i] = b.readUInt16();
                }

                for (int i = 2; i >= 0; --i)
                {
                    index_texCoordArray[t * 3 + i] = fileTexCoords[b.readUInt16()];
                }
            }

            computeTexCoords(index_texCoordArray);

            // Read the primitives
            {
                primitiveArray.clear();
                b.setPosition(header.offsetGlCommands);
                
                int n = b.readInt32();

                while (n != 0)
                {
                    primitiveArray.append(new Primitive());
                    Primitive primitive = primitiveArray[primitiveArray.size() - 1];

                    if (n > 0)
                    {
                        primitive.type = RenderDevice.Primitive.TRIANGLE_STRIP;
                    }
                    else
                    {
                        primitive.type = RenderDevice.Primitive.TRIANGLE_FAN;
                        n = -n;
                    }

                    primitive.pvertexArray.resize(n);

                    Array<Primitive.PVertex> pvertex = primitive.pvertexArray;

                    for (int i = 0; i < pvertex.size(); ++i)
                    {
                        Primitive.PVertex vertex = new Primitive.PVertex();
                        vertex.texCoord.x = b.readFloat32();
                        vertex.texCoord.y = b.readFloat32();
                        vertex.index      = b.readInt32();
                        pvertex[i] = vertex;
                    }

                    n = b.readInt32();
                }
            }


            MeshAlg.computeAdjacency(keyFrame[0].vertexArray, indexArray, faceArray, edgeArray, vertexArray);
            weldedFaceArray = copyFaces(faceArray);
            weldedEdgeArray = copyEdges(edgeArray);
            weldedVertexArray = copyVertices(vertexArray);
            MeshAlg.weldAdjacency(keyFrame[0].vertexArray, weldedFaceArray, weldedEdgeArray, weldedVertexArray);

            numBoundaryEdges = MeshAlg.countBoundaryEdges(edgeArray);
            numWeldedBoundaryEdges = MeshAlg.countBoundaryEdges(weldedEdgeArray);
        }

        /**
         Wipe all data structures.  Called from load.
         */
        protected virtual void reset()
        {
            _textureFilenames.clear();
            keyFrame.clear();
            primitiveArray.clear();
            indexArray.clear();
            _texCoordArray.clear();
            faceArray.clear();
            vertexArray.clear();
            edgeArray.clear();
        }

        private class MD2ModelHeader
        {
            public int magic; 
            public int version; 
            public int skinWidth; 
            public int skinHeight; 
            public int frameSize; 
            public int numSkins; 
            public int numVertices; 
            public int numTexCoords; 
            public int numTriangles; 
            public int numGlCommands; 
            public int numFrames; 
            public int offsetSkins; 
            public int offsetTexCoords; 
            public int offsetTriangles; 
            public int offsetFrames; 
            public int offsetGlCommands; 
            public int offsetEnd;

            public void deserialize(BinaryInput b)
            {
                magic               = b.readInt32();
                version             = b.readInt32(); 
                skinWidth           = b.readInt32(); 
                skinHeight          = b.readInt32(); 
                frameSize           = b.readInt32(); 
                numSkins            = b.readInt32(); 
                numVertices         = b.readInt32(); 
                numTexCoords        = b.readInt32(); 
                numTriangles        = b.readInt32(); 
                numGlCommands       = b.readInt32(); 
                numFrames           = b.readInt32(); 
                offsetSkins         = b.readInt32(); 
                offsetTexCoords     = b.readInt32(); 
                offsetTriangles     = b.readInt32(); 
                offsetFrames        = b.readInt32(); 
                offsetGlCommands    = b.readInt32(); 
                offsetEnd           = b.readInt32();
            }
        }

        private class MD2Frame
        {
            public Vector3 scale;
            public Vector3 translate;
            public string name;

            public void deserialize(BinaryInput b)
            {
                scale = new Vector3(b);
                translate = new Vector3(b);
                name = b.readString(16);
            }
        }

        /**
         Called from PosedMD2Model::render.
         */
        protected void render(RenderDevice renderDevice, Pose pose)
        {

            bool tooBig = ((int)maxVARVerts < keyFrame[0].vertexArray.size());
            bool useVAR = (nextVarArea != NONE_ALLOCATED) && ! tooBig;
           
            if (! useVAR && ! tooBig)
            {
                // Try to allocate some memory
                allocateVertexArrays(renderDevice);
                useVAR = (nextVarArea != NONE_ALLOCATED);
            }

            getGeometry(pose, interpolatedFrame);

            renderDevice.pushState();
                renderDevice.setShadeMode(RenderDevice.ShadeMode.SHADE_SMOOTH);

                // Quake's triangles are backwards of OpenGL in our coordinate
                // system, so cull front faces instead of back faces.

                Array<Vector3> vertexArray   = interpolatedFrame.vertexArray;
                Array<Vector3> normalArray   = interpolatedFrame.normalArray;

                if (useVAR) {

                    // Upload the arrays (System::memcpy is no faster than memcpy for VAR memory)
                    Debug.debugAssert((varArea[nextVarArea] != null) && varArea[nextVarArea].notNull());
                    varArea[nextVarArea].pointer().reset();

                    VAR varTexCoord = createTexCoordVAR(texCoordFloatArray(), varArea[nextVarArea].pointer());
                    VAR varNormal   = createNormalVAR(normalArray.ToArray(), varArea[nextVarArea].pointer());
                    VAR varVertex   = createVertexVAR(vertexArray.ToArray(), varArea[nextVarArea].pointer());

                    renderDevice.setTextureMatrix(0, texFrame);

                    renderDevice.beginIndexedPrimitives();
                        renderDevice.setTexCoordArray(0, varTexCoord);
                        renderDevice.setNormalArray(varNormal);
                        renderDevice.setVertexArray(varVertex);
                        renderDevice.sendIndices(RenderDevice.Primitive.TRIANGLES, indexArray);
                    renderDevice.endIndexedPrimitives();
                    
                    nextVarArea = (nextVarArea + 1) % NUM_VAR_AREAS;

                } else {

                    // No VAR available; use the default rendering path
                    for (int p = 0; p < primitiveArray.size(); ++p) {
    
                        Primitive primitive = primitiveArray[p];
                        int n = primitive.pvertexArray.size();

                        renderDevice.beginPrimitive(primitive.type);

                            if (primitive.type == RenderDevice.Primitive.TRIANGLE_FAN) {
                                // Reverse the winding of the fan by keeping the
                                // center vertex first and reversing the rim.
                                sendPrimitiveVertex(renderDevice, primitive, normalArray, vertexArray, 0);
                                for (int i = n - 1; i >= 1; --i) {
                                    sendPrimitiveVertex(renderDevice, primitive, normalArray, vertexArray, i);
                                }
                            } else {
                                // Reverse the winding of the strip.
                                for (int i = n - 1; i >= 0; --i) {
                                    sendPrimitiveVertex(renderDevice, primitive, normalArray, vertexArray, i);
                                }
                            }
                        renderDevice.endPrimitive();
                    }
                }

            renderDevice.popState();
            
        }

        private void sendPrimitiveVertex(
            RenderDevice renderDevice,
            Primitive primitive,
            Array<Vector3> normalArray,
            Array<Vector3> vertexArray,
            int i)
        {
            Primitive.PVertex pvertex = primitive.pvertexArray[i];
            int v = pvertex.index;
            renderDevice.setTexCoord(0, pvertex.texCoord);
            renderDevice.setNormal(normalArray[v]);
            renderDevice.sendVertex(vertexArray[v]);
        }

        /**
         Render the wireframe mesh.
         */
        public void debugRenderWireframe(RenderDevice renderDevice, Pose pose)
        {
            getGeometry(pose, interpolatedFrame);

            renderDevice.pushState();
                renderDevice.setDepthTest(RenderDevice.DepthTest.DEPTH_LEQUAL);
                renderDevice.setRenderMode(RenderDevice.RenderMode.RENDER_WIREFRAME);
                renderDevice.setPolygonOffset(-.1);
                renderDevice.setColor(Color3.black());
                
                renderDevice.beginPrimitive(RenderDevice.Primitive.TRIANGLES);
                for (int i = 0; i < indexArray.size(); ++i)
                {
                    renderDevice.sendVertex(interpolatedFrame.vertexArray[indexArray[i]]);
                }
                renderDevice.endPrimitive();

                renderDevice.setPolygonOffset(0.0);
                renderDevice.setRenderMode(RenderDevice.RenderMode.RENDER_SOLID);
            renderDevice.popState();
        }

        /**
         Fills the geometry out from the pose. 

         Called from PosedMD2Model::getGeometry
         */
        protected void getGeometry(Pose pose, MeshAlg.Geometry geometry)
        {
            
            int numVertices = keyFrame[0].vertexArray.size();

            geometry.vertexArray.resize(numVertices, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);
            geometry.normalArray.resize(numVertices, Array<Vector3>.DONT_SHRINK_UNDERLYING_ARRAY);

            if ((interpolatedModel == this) && (pose == interpolatedPose))
            {
                // We're being asked to recompute a pose we have cached.
                if (! object.ReferenceEquals(geometry, interpolatedFrame))
                {
                    // Copy from the cache
                    geometry.vertexArray = new Array<Vector3>(interpolatedFrame.vertexArray);
                    geometry.normalArray = new Array<Vector3>(interpolatedFrame.normalArray);
                }

                return;
            }

            if (object.ReferenceEquals(geometry, interpolatedFrame))
            {
                // Make a note about what the cache contains
                interpolatedPose  = pose;
                interpolatedModel = this;
            }

            double alpha;
            int i0, i1;

            computeFrameNumbers(pose, out i0, out i1, out alpha);

            if ((i0 >= keyFrame.size()) || (i1 >= keyFrame.size()))
            {
                // This animation is not supported by this model.
                i0 = 0;
                i1 = 0;
                alpha = 0;
            }

            PackedGeometry frame0 = keyFrame[i0];
            PackedGeometry frame1 = keyFrame[i1];

            for (int v = numVertices - 1; v >= 0; --v)
            {
                geometry.vertexArray[v] = frame0.vertexArray[v].lerp(frame1.vertexArray[v], (float)alpha);
                geometry.normalArray[v] = normalTable[frame0.normalArray[v]].lerp(normalTable[frame1.normalArray[v]], (float)alpha);
            }
        }

        /**
         The texture coordinates of the model, converted to floats.  The
         int16 texture coordinates are scaled by textureMatrix() just like
         they were in the original OpenGL implementation.
         */
        private Vector3[] texCoordFloatArray()
        {
            Vector3[] result = new Vector3[_texCoordArray.size()];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = new Vector3(_texCoordArray[i].x, _texCoordArray[i].y, 0.0f);
            }
            return result;
        }

        /**
         Creates the vertex array used by the indexed rendering path.
         */
        private static VAR createVertexVAR(Vector3[] values, VARArea area)
        {
            return new VAR(values, area);
        }

        /**
         Creates the normal array used by the indexed rendering path.
         */
        private static VAR createNormalVAR(Vector3[] values, VARArea area)
        {
            VAR var = new VAR(values, area);
            var.setType(VAR.Type.NORMAL);
            return var;
        }

        /**
         Creates the texture coordinate array used by the indexed rendering
         path.  The managed VAR uses a three component stride for texture
         coordinates, so the two component texture coordinates are uploaded
         with a zero third component.
         */
        private static VAR createTexCoordVAR(Vector3[] values, VARArea area)
        {
            VAR var = new VAR(values, area);
            var.setType(VAR.Type.TEXCOORD0);
            return var;
        }

        /**
         Copies the faces so that welding the adjacency information does not
         modify the unwelded copy.
         */
        private static Array<MeshAlg.Face> copyFaces(Array<MeshAlg.Face> source)
        {
            Array<MeshAlg.Face> result = new Array<MeshAlg.Face>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Face face = new MeshAlg.Face();
                for (int j = 0; j < 3; ++j)
                {
                    face.vertexIndex[j] = source[i].vertexIndex[j];
                    face.edgeIndex[j] = source[i].edgeIndex[j];
                }
                result[i] = face;
            }
            return result;
        }

        /**
         Copies the edges so that welding the adjacency information does not
         modify the unwelded copy.
         */
        private static Array<MeshAlg.Edge> copyEdges(Array<MeshAlg.Edge> source)
        {
            Array<MeshAlg.Edge> result = new Array<MeshAlg.Edge>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Edge edge = new MeshAlg.Edge();
                for (int j = 0; j < 2; ++j)
                {
                    edge.vertexIndex[j] = source[i].vertexIndex[j];
                    edge.faceIndex[j] = source[i].faceIndex[j];
                }
                result[i] = edge;
            }
            return result;
        }

        /**
         Copies the vertices so that welding the adjacency information does not
         modify the unwelded copy.
         */
        private static Array<MeshAlg.Vertex> copyVertices(Array<MeshAlg.Vertex> source)
        {
            Array<MeshAlg.Vertex> result = new Array<MeshAlg.Vertex>(source.size());
            for (int i = 0; i < source.size(); ++i)
            {
                MeshAlg.Vertex vertex = new MeshAlg.Vertex();
                vertex.faceIndex = new Array<int>(source[i].faceIndex);
                vertex.edgeIndex = new Array<int>(source[i].edgeIndex);
                result[i] = vertex;
            }
            return result;
        }

        protected Array<Vector3>              faceNormalArray = new Array<Vector3>();
        protected Array<MeshAlg.Face>        faceArray = new Array<MeshAlg.Face>();
        protected Array<MeshAlg.Vertex>      vertexArray = new Array<MeshAlg.Vertex>();
        protected Array<MeshAlg.Edge>        edgeArray = new Array<MeshAlg.Edge>();
        protected Array<MeshAlg.Face>        weldedFaceArray = new Array<MeshAlg.Face>();
        protected Array<MeshAlg.Vertex>      weldedVertexArray = new Array<MeshAlg.Vertex>();
        protected Array<MeshAlg.Edge>        weldedEdgeArray = new Array<MeshAlg.Edge>();
        protected Sphere                      boundingSphere = new Sphere(Vector3.zero(), 0);
        protected Box                         boundingBox = new Box(Vector3.zero(), Vector3.zero());
        protected int                         numBoundaryEdges;
        protected int                         numWeldedBoundaryEdges;
        protected string                      _name = "";

        private const int maxVARVerts = 1600;

        public string name()
        {
            return _name;
        }

        public virtual void setName(string n)
        {
            _name = n;
        }

        /**
         @filename The tris.md2 file.  Note that most MD2 
          files are stored in two files, tris.md2 and weapon.md2.  
          You will have to load both as separate models.

         @param scale Optional scale factor to apply while loading.
         */
        public static ReferenceCountedPointer<MD2Model> create(string filename)
        {
            return create(filename, 1.0f);
        }

        public static ReferenceCountedPointer<MD2Model> create(string filename, float scale)
        {
            MD2Model model = new MD2Model();
            try
            {
                model.load(filename, scale);
            }
            catch
            {
                model.Dispose();
                throw;
            }
            return new ReferenceCountedPointer<MD2Model>(model);
        }

        public ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe, Pose pose)
        {
            return new ReferenceCountedPointer<PosedModel>(
                new PosedMD2Model(new ReferenceCountedPointer<MD2Model>(this), cframe, pose, false, new GMaterial()));
        }

        /** You can also pose a model without a material.  A good example of why this is useful
            is the MD2Model_Demo, in which shadows are rendered by flattening a pose of the model.
            The shadow should not be textured!*/
        public ReferenceCountedPointer<PosedModel> pose(CoordinateFrame cframe, Pose pose, GMaterial mat)
        {
            return new ReferenceCountedPointer<PosedModel>(
                new PosedMD2Model(new ReferenceCountedPointer<MD2Model>(this), cframe, pose, true, mat));
        }

        public Array<Vector2int16> texCoordArray()
        {
            return _texCoordArray;
        }

        public Array<MeshAlg.Face> faces()
        {
            return faceArray;
        }

        public Array<MeshAlg.Face> weldedFaces()
        {
            return weldedFaceArray;
        }

        public Array<MeshAlg.Edge> edges()
        {
            return edgeArray;
        }

        public Array<MeshAlg.Edge> weldedEdges()
        {
            return weldedEdgeArray;
        }

        /** You must get the geometry for the vertex positions-- this only specifies adjacency */
        public Array<MeshAlg.Vertex> vertices()
        {
            return vertexArray;
        }

        public Array<MeshAlg.Vertex> weldedVertices()
        {
            return weldedVertexArray;
        }

        /**
         Matrix mapping int16 texture coordinates to floats for this model.
         Useful if you are using custom rendering code instead of pose()->render()
         */    
        public CoordinateFrame textureMatrix()
        {
            return texFrame;
        }

        /**
         A bounding sphere on the model.  Covers all vertices in all animations.
         */
        public Sphere objectSpaceBoundingSphere()
        {
            return boundingSphere;
        }

        /**
         An oriented bounding box on the model.  Covers all vertices in all animations.
         */
        public Box objectSpaceBoundingBox()
        {
            return boundingBox;
        }

        /**
         Returns the total time of the animation.  If the animation loops (e.g. walking)
         this is the time from the first frame until that frame repeats.  If the
         animation does not loop (e.g. death) this is the time from the first frame
         until the last frame.
         */
        public static GameTime animationLength(Animation a)
        {
            if (a == Animation.JUMP)
            {
                return new GameTime((double)animationLength(Animation.JUMP_DOWN) * (2 + hangTimePct));
            }

            a = (Animation)g3dmath.iAbs((int)a);
            Debug.debugAssert((int)a < (int)Animation.MAX_ANIMATIONS);

            MD2AnimInfo info = animationTable[(int)a];

            if (info.loops)
            {
                return new GameTime((info.last - info.first + 1) / (double)info.fps);
            }
            else
            {
                return new GameTime((info.last - info.first) / (double)info.fps);
            }
        }

        /**
         Returns true for standing, running, crouching, and crouch walking animations.
         */
        public static bool animationLoops(Animation a)
        {
            if (a == Animation.JUMP)
            {
                return false;
            }

            a = (Animation)g3dmath.iAbs((int)a);
            Debug.debugAssert((int)a < (int)Animation.MAX_ANIMATIONS);
            return animationTable[(int)a].loops;
        }

        /**
         Returns true for the crouching set of animations.
         */
        public static bool animationCrouch(Animation a)
        {
            return ((int)a >= (int)Animation.CROUCH_STAND) && ((int)a <= (int)Animation.CROUCH_DEATH);
        }

        /**
         Returns true for the death animations
         */
        public static bool animationDeath(Animation a)
        {
            return ((int)a >= (int)Animation.CROUCH_DEATH) && ((int)a <= (int)Animation.DEATH_FALLBACKSLOW);
        }

        public static bool animationAttack(Animation a)
        {
            return (a == Animation.ATTACK) || (a == Animation.CROUCH_ATTACK);
        }

        public static bool animationJump(Animation a)
        {
            return (a == Animation.JUMP) || (a == Animation.JUMP_UP) || (a == Animation.JUMP_DOWN);
        }

        public static bool animationPain(Animation a)
        {
            return (a == Animation.CROUCH_PAIN) || (((int)a >= (int)Animation.PAIN_A) && ((int)a <= (int)Animation.PAIN_C));
        }

        /**
         STAND or CROUCH_STAND.
         */
        public static bool animationStand(Animation a)
        {
            return (a == Animation.STAND) || (a == Animation.CROUCH_STAND);
        }

        /**
         running, forward or backward, standing or crouching
         */
        public static bool animationRun(Animation a)
        {
            return (g3dmath.iAbs((int)a) == (int)Animation.RUN) || (g3dmath.iAbs((int)a) == (int)Animation.CROUCH_WALK);
        }

        public static bool animationRunForward(Animation a)
        {
            return (a == Animation.RUN) || (a == Animation.CROUCH_WALK);
        }

        public static bool animationRunBackward(Animation a)
        {
            return (a == Animation.RUN_BACKWARD) || (a == Animation.CROUCH_WALK_BACKWARD);
        }

        /**
         True for actions that can be interrupted, like running or saluting.
         Jumping (which is really more of a falling animation) is considered
         interruptible.
         */
        public static bool animationInterruptible(Animation a)
        {
            return (! animationAttack(a)) && (! animationDeath(a)) && (! animationPain(a));
        }

        /** Filenames of textures this model can use. */
        public Array<string> textureFilenames()
        {
            return _textureFilenames;
        }

        /**
         Returns a value for MD2Model::Pose::preFrameNumber that will
         smoothly blend from this animation to the next one.
         */
        public static int getFrameNumber(Pose pose)
        {
            // Return the frame we're about to go to.
            int kf0, kf1;
            double alpha;
            computeFrameNumbers(pose, out kf0, out kf1, out alpha);
            return kf1;
        }

        /**
         Returns the approximate amount of main memory, not counting the texture,
         occupied by this data structure.
         */
        public virtual ulong mainMemorySize()
        {
            const int managedArrayHeaderBytes = 24;
            const int managedMD2ModelHeaderBytes = 64;
            const int managedPackedGeometryBytes = 24;
            const int managedVector3Bytes = 12;
            const int managedByteBytes = 1;
            const int managedIntBytes = 4;
            const int managedVector2Bytes = 8;
            const int managedFaceBytes = 24;
            const int managedEdgeBytes = 16;
            const int managedVertexBytes = 24;
            const int managedPrimitiveBytes = 24;
            const int managedPVertexBytes = 12;

            ulong frameSize = (ulong)(keyFrame.size() * (managedPackedGeometryBytes +
                (managedVector3Bytes + managedByteBytes) * keyFrame[0].vertexArray.size()));
            ulong indexSize = (ulong)(indexArray.size() * managedIntBytes);
            ulong faceSize = (ulong)(faceArray.size() * managedFaceBytes);
            ulong texSize = (ulong)(_texCoordArray.size() * managedVector2Bytes);
            ulong valentSize = (ulong)(vertexArray.size() * managedVertexBytes);
            for (int i = 0; i < vertexArray.size(); ++i)
            {
                valentSize += (ulong)(vertexArray[i].faceIndex.size() * managedIntBytes);
                valentSize += (ulong)(vertexArray[i].edgeIndex.size() * managedIntBytes);
            }

            ulong primitiveSize  = (ulong)(primitiveArray.size() * managedPrimitiveBytes);
            for (int i = 0; i < primitiveArray.size(); ++i)
            {
                primitiveSize += (ulong)(primitiveArray[i].pvertexArray.size() * managedPVertexBytes);
            }

            ulong edgeSize    = (ulong)(edgeArray.size() * managedEdgeBytes);

            return (ulong)managedMD2ModelHeaderBytes + (ulong)managedArrayHeaderBytes + frameSize + indexSize + faceSize + valentSize + primitiveSize + texSize + edgeSize;
        }
    }
}
