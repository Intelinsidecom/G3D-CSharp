/**
 @file G3DGameUnits.h

 @maintainer Morgan McGuire, matrix@graphics3d.com
 @created 2002-10-05
 @edited  2006-01-10
 */

namespace G3D
{
    public enum AMPM
    {
        AM,
        PM
    }

    public static class G3DGameUnits
    {
        public const int SECOND = 1;
        public const int MINUTE = 60;
        public const int HOUR = 60 * 60;
        public const int DAY = 24 * 60 * 60;
        public const int SUNRISE = 24 * 60 * 60 / 4;
        public const int SUNSET = 24 * 60 * 60 * 3 / 4;
        public const int MIDNIGHT = 0;
        public const int METER = 1;
        public const int KILOMETER = 1000;

        public const double CENTIMETER = 0.01;
        public const double DECIMETER = 0.1;

        public static double toSeconds(int hour, int minute, AMPM ap)
        {
            return toSeconds(hour, minute, 0, ap);
        }

        public static double toSeconds(int hour, int minute, double seconds, AMPM ap)
        {
            double t = ((hour % 12) * 60 + minute) * 60 + seconds;

            if (ap == AMPM.PM)
            {
                t += 12 * 60 * 60;
            }

            return t;
        }
    }
}
