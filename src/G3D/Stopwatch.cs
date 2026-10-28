/**
 @file Stopwatch.h
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
  
 @created 2005-10-05
 @edited  2005-10-05

 Copyright 2000-2003, Morgan McGuire.
 All rights reserved.
 */

using System;

namespace G3D
{
    public sealed class Stopwatch
    {
        private bool inBetween;
        private long cycleStart;
        private double timeStart;
        private double lastTockTime;
        private double lastDuration;
        private long lastCycleCount;
        private double m_fps;
        private double emwaFPS;
        private double m_smoothFPS;
        private double emwaDuration;
        private long cycleOverhead;

        public Stopwatch()
        {
            inBetween = false;
            lastTockTime = -1;
            lastDuration = 0;
            lastCycleCount = 0;
            m_fps = 0;
            emwaFPS = 0;
            m_smoothFPS = 0;
            emwaDuration = 0;
            computeOverhead();
        }

        private void computeOverhead()
        {
            cycleOverhead = 0;
            tick();
            tock();
            cycleOverhead = (long)elapsedCycles();
        }

        public double FPS()
        {
            return m_fps;
        }

        public double elapsedTime()
        {
            return lastDuration;
        }

        public double smoothElapsedTime()
        {
            return emwaDuration;
        }

        public double smoothFPS()
        {
            return m_smoothFPS;
        }

        public ulong elapsedCycles()
        {
            return (ulong)lastCycleCount;
        }

        public void tick()
        {
            if (inBetween)
            {
                throw new InvalidOperationException("Stopwatch::tick() called twice in a row.");
            }
            inBetween = true;

            timeStart = currentTime();
            cycleStart = global::System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public void tock()
        {
            long cycles = global::System.Diagnostics.Stopwatch.GetTimestamp() - cycleStart;
            double now = currentTime();
            lastDuration = now - timeStart;
            if (Math.Abs(emwaDuration - lastDuration) > Math.Max(emwaDuration, lastDuration) * 0.50)
            {
                emwaDuration = lastDuration;
            }
            else
            {
                emwaDuration = lastDuration * 0.05 + emwaDuration * 0.95;
            }

            lastCycleCount = cycles - cycleOverhead;
            if (lastCycleCount < 0)
            {
                lastCycleCount = 0;
            }

            if (lastTockTime != -1.0)
            {
                m_fps = 1.0 / (now - lastTockTime);

                emwaFPS = m_fps * 0.01 + emwaFPS * 0.99;

                if (Math.Abs(emwaFPS - m_fps) > Math.Max(emwaFPS, m_fps) * 0.08)
                {
                    emwaFPS = m_fps;
                }

                if (m_smoothFPS == 0)
                {
                    m_smoothFPS = m_fps;
                }
                else if (emwaFPS <= 10)
                {
                    if (Math.Abs(m_smoothFPS - emwaFPS) > .75)
                    {
                        m_smoothFPS = Math.Floor(emwaFPS * 10.0 + 0.5) / 10.0;
                    }
                }
                else
                {
                    if (Math.Abs(m_smoothFPS - emwaFPS) > 1.25)
                    {
                        m_smoothFPS = Math.Floor(emwaFPS + 0.5);
                    }
                }
            }
            lastTockTime = now;

            if (!inBetween)
            {
                throw new InvalidOperationException("Stopwatch::tock() called without matching tick.");
            }
            inBetween = false;
        }

        private static double currentTime()
        {
            return (double)global::System.Diagnostics.Stopwatch.GetTimestamp() / (double)global::System.Diagnostics.Stopwatch.Frequency;
        }
    }
}
