/**
  @file GLight.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-11-12
  @edited  2006-02-08
*/

using System;

namespace G3D
{
    /**
     A light representation that closely follows the OpenGL light format.
     */
    public class GLight
    {
        /** World space position (for a directional light, w = 0 */
        public Vector4 position;

        public Vector3 spotDirection;

        /** In <B>degrees</B>.  180 = no cutoff (point/dir) >90 = spot light */
        public double spotCutoff;

        /** Constant, linear, quadratic */
        public double[] attenuation = new double[3];

        /** May be outside the range [0, 1] */
        public Color3 color;

        /** If false, this light is ignored */
        public bool enabled;

        /** If false, this light does not create specular highlights (useful when using negative lights). */
        public bool specular;

        /** If false, this light does not create diffuse illumination (useful when rendering a specular-only pass). */
        public bool diffuse;

        public GLight()
        {
            position = new Vector4(0, 0, 0, 0);
            color = Color3.white();
            spotDirection = new Vector3(0, 0, -1);
            spotCutoff = 180;
            enabled = false;
            attenuation[0] = 1.0;
            attenuation[1] = 0.0;
            attenuation[2] = 0.0;
            specular = true;
            diffuse = true;
        }

        public GLight(GLight other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            position = other.position;
            spotDirection = other.spotDirection;
            spotCutoff = other.spotCutoff;
            attenuation = new double[3];
            Array.Copy(other.attenuation, attenuation, 3);
            color = other.color;
            enabled = other.enabled;
            specular = other.specular;
            diffuse = other.diffuse;
        }

        /** @param toLight will be normalized */
        public static GLight directional(Vector3 toLight, Color3 color)
        {
            return directional(toLight, color, true, true);
        }

        public static GLight directional(Vector3 toLight, Color3 color, bool specular)
        {
            return directional(toLight, color, specular, true);
        }

        public static GLight directional(Vector3 toLight, Color3 color, bool specular, bool diffuse)
        {
            GLight L = new GLight();
            L.position = new Vector4(toLight.direction(), 0);
            L.color = color;
            L.specular = specular;
            L.diffuse = diffuse;
            return L;
        }

        public static GLight point(Vector3 pos, Color3 color)
        {
            return point(pos, color, 1, 0, 0, true, true);
        }

        public static GLight point(Vector3 pos, Color3 color, double constAtt)
        {
            return point(pos, color, constAtt, 0, 0, true, true);
        }

        public static GLight point(Vector3 pos, Color3 color, double constAtt, double linAtt)
        {
            return point(pos, color, constAtt, linAtt, 0, true, true);
        }

        public static GLight point(Vector3 pos, Color3 color, double constAtt, double linAtt, double quadAtt)
        {
            return point(pos, color, constAtt, linAtt, quadAtt, true, true);
        }

        public static GLight point(Vector3 pos, Color3 color, double constAtt, double linAtt, double quadAtt, bool specular)
        {
            return point(pos, color, constAtt, linAtt, quadAtt, specular, true);
        }

        public static GLight point(Vector3 pos, Color3 color, double constAtt, double linAtt, double quadAtt, bool specular, bool diffuse)
        {
            GLight L = new GLight();
            L.position = new Vector4(pos, 1);
            L.color = color;
            L.attenuation[0] = constAtt;
            L.attenuation[1] = linAtt;
            L.attenuation[2] = quadAtt;
            L.specular = specular;
            L.diffuse = diffuse;
            return L;
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color)
        {
            return spot(pos, pointDirection, cutOffAngleDegrees, color, 1, 0, 0, true, true);
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color, double constAtt)
        {
            return spot(pos, pointDirection, cutOffAngleDegrees, color, constAtt, 0, 0, true, true);
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color, double constAtt, double linAtt)
        {
            return spot(pos, pointDirection, cutOffAngleDegrees, color, constAtt, linAtt, 0, true, true);
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color, double constAtt, double linAtt, double quadAtt)
        {
            return spot(pos, pointDirection, cutOffAngleDegrees, color, constAtt, linAtt, quadAtt, true, true);
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color, double constAtt, double linAtt, double quadAtt, bool specular)
        {
            return spot(pos, pointDirection, cutOffAngleDegrees, color, constAtt, linAtt, quadAtt, specular, true);
        }

        public static GLight spot(Vector3 pos, Vector3 pointDirection, double cutOffAngleDegrees, Color3 color, double constAtt, double linAtt, double quadAtt, bool specular, bool diffuse)
        {
            GLight L = new GLight();
            L.position = new Vector4(pos, 1);
            L.spotDirection = pointDirection;
            L.spotCutoff = cutOffAngleDegrees;
            L.color = color;
            L.attenuation[0] = constAtt;
            L.attenuation[1] = linAtt;
            L.attenuation[2] = quadAtt;
            L.specular = specular;
            L.diffuse = diffuse;
            return L;
        }

        public static bool operator ==(GLight left, GLight right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            if (ReferenceEquals(right, null))
            {
                return false;
            }

            return (left.position == right.position) &&
                (left.spotDirection == right.spotDirection) &&
                (left.spotCutoff == right.spotCutoff) &&
                (left.attenuation[0] == right.attenuation[0]) &&
                (left.attenuation[1] == right.attenuation[1]) &&
                (left.attenuation[2] == right.attenuation[2]) &&
                (left.color == right.color) &&
                (left.enabled == right.enabled) &&
                (left.specular == right.specular) &&
                (left.diffuse == right.diffuse);
        }

        public static bool operator !=(GLight left, GLight right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is GLight && this == (GLight)obj;
        }

        public override int GetHashCode()
        {
            int hash = position.GetHashCode();
            hash = (hash * 31) + spotDirection.GetHashCode();
            hash = (hash * 31) + spotCutoff.GetHashCode();
            hash = (hash * 31) + attenuation[0].GetHashCode();
            hash = (hash * 31) + attenuation[1].GetHashCode();
            hash = (hash * 31) + attenuation[2].GetHashCode();
            hash = (hash * 31) + color.GetHashCode();
            hash = (hash * 31) + enabled.GetHashCode();
            hash = (hash * 31) + specular.GetHashCode();
            hash = (hash * 31) + diffuse.GetHashCode();
            return hash;
        }
    }
}
