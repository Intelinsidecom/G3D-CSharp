/**
  @file LightingParameters.h

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2002-10-05
  @edited  2005-06-01

  Copyright 2000-2005, Morgan McGuire.
  All rights reserved.
*/

using System;
using G3D;

namespace G3D
{
    public class LightingParameters
    {
        public const float BROWN_UNIVERSITY_LATITUDE = 41.7333f;
        public const float BROWN_UNIVERSITY_LONGITUDE = 71.4333f;

        /* Initital star offset on Jan 1 1970 midnight */
        /* Definition of a sidereal day */
        public const double SIDEREAL_DAY = ((23 * G3DGameUnits.HOUR) + (56 * G3DGameUnits.MINUTE) + (4.071f * G3DGameUnits.SECOND));

        private static readonly double sunRiseAndSetTime = G3DGameUnits.HOUR / 2.0;
        private static readonly double solarYear = 365.2564 * G3DGameUnits.DAY;
        private static readonly double halfSolarYear = 182.6282;
        private static readonly double moonPhaseInterval = G3DGameUnits.DAY * 29.53;

        // Tilt amount from the ecliptic
        private static readonly double earthTilt = g3dmath.toRadians(23.5);
        private static readonly double moonTilt = g3dmath.toRadians(5);

        // (very rough) Initial star offset on Jan 1 1970 midnight
        private static readonly double initialStarRot = 1;

        private static readonly Color3 dayAmbient = Color3.white() * .40f;
        private static readonly Color3 dayDiffuse = Color3.white() * .75f;

        private static readonly double[] lightColorTimes = new double[] { G3DGameUnits.MIDNIGHT, G3DGameUnits.SUNRISE - G3DGameUnits.HOUR, G3DGameUnits.SUNRISE, G3DGameUnits.SUNRISE + sunRiseAndSetTime / 4, G3DGameUnits.SUNRISE + sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime / 2, G3DGameUnits.SUNSET, G3DGameUnits.SUNSET + G3DGameUnits.HOUR / 2, G3DGameUnits.DAY };
        private static readonly Color3[] lightColorValues = new Color3[] { new Color3(.2f, .2f, .2f), new Color3(.1f, .1f, .1f), new Color3(0, 0, 0), new Color3(.6f, .6f, 0), dayDiffuse, dayDiffuse, new Color3(.1f, .1f, .075f), new Color3(.1f, .05f, .05f), new Color3(.1f, .1f, .1f), new Color3(.2f, .2f, .2f) };

        private static readonly double[] ambientTimes = new double[] { G3DGameUnits.MIDNIGHT, G3DGameUnits.SUNRISE - G3DGameUnits.HOUR, G3DGameUnits.SUNRISE, G3DGameUnits.SUNRISE + sunRiseAndSetTime / 4, G3DGameUnits.SUNRISE + sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime / 2, G3DGameUnits.SUNSET, G3DGameUnits.SUNSET + G3DGameUnits.HOUR / 2, G3DGameUnits.DAY };
        private static readonly Color3[] ambientValues = new Color3[] { new Color3(0, .1f, .3f), new Color3(0, .0f, .1f), new Color3(0, 0, 0), new Color3(0, 0, 0), dayAmbient, dayAmbient, new Color3(.5f, .2f, .2f), new Color3(.05f, .05f, .1f), new Color3(.0f, .0f, .1f), new Color3(.1f, .1f, .3f) };

        private static readonly double[] diffuseTimes = new double[] { G3DGameUnits.MIDNIGHT, G3DGameUnits.SUNRISE - G3DGameUnits.HOUR, G3DGameUnits.SUNRISE, G3DGameUnits.SUNRISE + sunRiseAndSetTime / 2, G3DGameUnits.SUNRISE + sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime / 2, G3DGameUnits.SUNSET, G3DGameUnits.SUNSET + G3DGameUnits.HOUR / 2, G3DGameUnits.DAY };
        private static readonly Color3[] diffuseValues = new Color3[] { new Color3(.2f, .2f, .3f), new Color3(.05f, .06f, .07f), new Color3(.08f, .08f, .01f), new Color3(1.0f, 1.0f, 1.0f) * .75f, new Color3(1.0f, 1.0f, 1.0f) * .75f, new Color3(1.0f, 1.0f, 1.0f) * .35f, new Color3(.5f, .2f, .2f), new Color3(.05f, .05f, .1f), new Color3(.06f, .06f, .07f), new Color3(.1f, .1f, .17f) };

        private static readonly double[] skyTimes = new double[] { G3DGameUnits.MIDNIGHT, G3DGameUnits.SUNRISE - G3DGameUnits.HOUR, G3DGameUnits.SUNRISE - G3DGameUnits.HOUR / 2, G3DGameUnits.SUNRISE, G3DGameUnits.SUNRISE + sunRiseAndSetTime, G3DGameUnits.SUNSET - sunRiseAndSetTime, G3DGameUnits.SUNSET, G3DGameUnits.SUNSET + G3DGameUnits.HOUR / 3, G3DGameUnits.DAY };
        private static readonly Color3[] skyValues = new Color3[] { new Color3(0, 0, 0), new Color3(0, 0, 0), new Color3(.2f, .15f, .01f), new Color3(.2f, .15f, .01f), new Color3(1.0f, 1.0f, 1.0f), new Color3(1.0f, 1.0f, 1.0f), new Color3(.4f, .2f, .05f), new Color3(0, 0, 0), new Color3(0, 0, 0) };

        // Initial moon phase on Jan 1 1970 midnight
        private static readonly double initialMoonPhase = 0.75;

        /**
         Multiply this by all emissive values when rendering.  
         Some algorithms (e.g. contrib/ArticulatedModel/ToneMap) scale
         down light intensity to preserve dynamic range.
        */
        public Color3 emissiveScale;

        /** Modulate sky box color */
        public Color3 skyAmbient;

        /**
         Use this for objects that do not receive directional lighting
         (e.g. billboards).
        */
        public Color3 diffuseAmbient;

        /**
         Directional light color.
        */
        public Color3 lightColor;
        public Color3 ambient;

        /** Only one light source, the sun or moon, is active at a given time. */
        public Vector3 lightDirection;
        public enum Source { SUN, MOON }
        public Source source;

        /** Using physically correct parameters.  When false, the sun and moon
            travel in a perfectly east-west arc where +x = east and -x = west. */
        public bool physicallyCorrect;

        /** The vector <B>to</B> the sun */
        public Vector3 trueSunPosition;
        public Vector3 sunPosition;

        /** The vector <B>to</B> the moon */
        public Vector3 trueMoonPosition;
        public Vector3 moonPosition;
        public double moonPhase;    

        /** The coordinate frame and vector related to the starfield */
        public CoordinateFrame starFrame;
        public CoordinateFrame trueStarFrame;
        public Vector3 starVec;

        /* Geographic position */
        public float geoLatitude;
        //public float geoLongitude;
        
        public LightingParameters()
        {
            emissiveScale = Color3.white();
            skyAmbient = Color3.white();
            diffuseAmbient = Color3.white();
            lightColor = Color3.white();
            ambient = Color3.black();
            lightDirection = Vector3.unitY();
            source = Source.SUN;
            physicallyCorrect = true;
            trueSunPosition = Vector3.zero();
            sunPosition = Vector3.zero();
            trueMoonPosition = Vector3.zero();
            moonPosition = Vector3.zero();
            moonPhase = 0;
            starFrame = CoordinateFrame.identity();
            trueStarFrame = CoordinateFrame.identity();
            starVec = Vector3.zero();
            geoLatitude = BROWN_UNIVERSITY_LATITUDE;
            setTime(0);
        }

        /**
         Sets light parameters for the sun/moon based on the
         specified time since midnight, as well as geographic 
         latitude for starfield orientation (positive for north 
         of the equator and negative for south) and geographic 
         longitude for sun positioning (postive for east of 
         Greenwich, and negative for west). The latitude and 
         longitude is set by default to that of Providence, RI, 
         USA.
        */
        public LightingParameters(GameTime _time)
            : this(_time, true, BROWN_UNIVERSITY_LATITUDE)
        {
        }

        public LightingParameters(GameTime _time, bool _physicallyCorrect)
            : this(_time, _physicallyCorrect, BROWN_UNIVERSITY_LATITUDE)
        {
        }

        public LightingParameters(
             GameTime _time,
             bool _physicallyCorrect,
             float _latitude)
        {
            physicallyCorrect = _physicallyCorrect;
            geoLatitude = _latitude;
            setTime(_time);
        }

        public void setTime(GameTime _time)
        {
            double time = (double)_time - Math.Floor((double)_time / G3DGameUnits.DAY) * G3DGameUnits.DAY;

            // Calculate starfield coordinate frame
            double starRot = initialStarRot - (2 * g3dmath.G3D_PI * ((double)_time - ((double)_time * Math.Floor((double)_time / SIDEREAL_DAY))) / SIDEREAL_DAY);
            float aX, aY, aZ;
            starVec.x = (float)Math.Cos(starRot);
            starVec.y = 0;
            starVec.z = (float)Math.Sin(starRot);

            starFrame.lookAt(starVec, Vector3.unitY());
            trueStarFrame.lookAt(starVec, Vector3.unitY());
            trueStarFrame.rotation.toEulerAnglesXYZ(out aX, out aY, out aZ);
            aX -= geoLatitude;
            trueStarFrame.rotation = Matrix3.fromEulerAnglesXYZ(aX, aY, aZ);

            // sunAngle = 0 at midnight
            float sourceAngle = (float)(2 * (float)g3dmath.G3D_PI * time / G3DGameUnits.DAY);

            // Calculate fake solar and lunar positions
            sunPosition.x = (float)Math.Sin(sourceAngle);
            sunPosition.y = (float)-Math.Cos(sourceAngle);
            sunPosition.z = 0;

            moonPosition.x = (float)Math.Sin(sourceAngle + (float)g3dmath.G3D_PI);
            moonPosition.y = (float)-Math.Cos(sourceAngle + (float)g3dmath.G3D_PI);
            moonPosition.z = 0;

            // Calculate "true" solar and lunar positions
            // These positions will always be somewhat wrong
            // unless _time is equal to real world GMT time,
            // and the current longitude is equal to zero. Also,
            // I'm assuming that the equinox-solstice interval
            // occurs exactly every 90 days, which isn't exactly
            // correct.
            // In addition, the precession of the moon's orbit is
            // not taken into account, but this should only account
            // for a 5 degree margin of error at most.

            float dayOfYearOffset = (float)((((double)_time - ((double)_time * Math.Floor((double)_time / solarYear))) / G3DGameUnits.DAY));
            moonPhase = Math.Floor((double)_time / moonPhaseInterval) + initialMoonPhase;

            float latRad = (float)g3dmath.toRadians(geoLatitude);
            float sunOffset = (float)(-earthTilt * Math.Cos(g3dmath.G3D_PI * (dayOfYearOffset - halfSolarYear) / halfSolarYear) - latRad);
            float moonOffset = (float)(((-earthTilt + moonTilt) * Math.Sin(moonPhase * 4)) - latRad);
            float curMoonPhase = (float)(moonPhase * g3dmath.G3D_PI * 2);

            Matrix3 rotMat = Matrix3.fromAxisAngle(Vector3.unitZ().cross(sunPosition), sunOffset);
            trueSunPosition = rotMat * sunPosition;

            Vector3 trueMoon = new Vector3((float)Math.Sin(curMoonPhase + sourceAngle),
                                           (float)-Math.Cos(curMoonPhase + sourceAngle),
                                           0);
            rotMat = Matrix3.fromAxisAngle(Vector3.unitZ().cross(trueMoon), moonOffset);
            trueMoonPosition = rotMat * trueMoon;

            // Determine which light source we observe.
            if (!physicallyCorrect)
            {
                if ((sourceAngle < (g3dmath.G3D_PI / 2)) || (sourceAngle > (3 * g3dmath.G3D_PI / 2)))
                {
                    source = Source.MOON;
                    sourceAngle += (float)g3dmath.G3D_PI;
                }
                else
                {
                    source = Source.SUN;
                }

                lightDirection.x = (float)Math.Sin(sourceAngle);
                lightDirection.y = (float)-Math.Cos(sourceAngle);
                lightDirection.z = 0;
            }
            else if (trueSunPosition.y > -.3f)
            {
                // The sun is always the stronger light source. When using
                // physically correct parameters, the sun and moon will
                // occasionally be in the visible sky at the same time.
                source = Source.SUN;
                lightDirection = trueSunPosition;
            }
            else
            {
                source = Source.MOON;
                lightDirection = trueMoonPosition;
            }

            lightColor = Spline.linearSpline(time, lightColorTimes, lightColorValues, 10);
            ambient = Spline.linearSpline(time, ambientTimes, ambientValues, 10);
            diffuseAmbient = Spline.linearSpline(time, diffuseTimes, diffuseValues, 10);
            skyAmbient = Spline.linearSpline(time, skyTimes, skyValues, 8);

            emissiveScale = Color3.white();
        }

        public void setLatitude(float _latitude)
        {
            geoLatitude = _latitude;
        }

        /**
         Returns a directional light composed from the light direction
         and color.
        */
        public GLight directionalLight()
        {
            return GLight.directional(lightDirection, lightColor);
        }
    }


    /** A rich environment lighting model that contains both global and local sources.
        See also LightingParameters, a class that describes a sun and moon lighting 
        model. */
    public class Lighting : ReferenceCountedObject
    {
        private Lighting()
        {
            emissiveScale = Color3.white();
        }

        /** Multiply this by all emissive values when rendering.  
            Some algorithms (e.g. contrib/ArticulatedModel/ToneMap) scale
            down light intensity to preserve dynamic range.*/
        public Color3 emissiveScale;

        /** Light reflected from the sky (usually slightly blue) */
        public Color3 ambientTop;

        /** Light reflected from the ground.  A simpler code path is taken
            if identical to ambientTop. */
        public Color3 ambientBottom;

        /** Cube map of surrounding environment (often just the skybox, although 
             it may be rendered per-object). */
        public ReferenceCountedPointer<Texture> environmentMap;

        /** Color to modulate environment map by */
        public Color3 environmentMapColor;

        /** Local illumination sources that do not cast shadows. */
        public Array<GLight> lightArray = new Array<GLight>();

        /** Local illumination sources that cast shadows. */
        public Array<GLight> shadowedLightArray = new Array<GLight>();

        /** Creates a (dark) environment. */
        public static ReferenceCountedPointer<Lighting> create()
        {
            return new ReferenceCountedPointer<Lighting>(new Lighting());
        }
    }
}