/**
  @file global::System.h

  OS and processor abstraction.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-01-25
  @edited  2006-04-26
 */

using System;
using global::System.Diagnostics;
using global::System.IO;
using global::System.Text;
using global::System.Threading;

namespace G3D
{
    public partial class System
    {
        public delegate bool OutOfMemoryCallback(int size, bool recoverable);

        public static OutOfMemoryCallback outOfMemoryCallback;

        public static void init()
        {
        }

        public static bool hasMMX()
        {
            return true;
        }

        public static bool hasCPUID()
        {
            return true;
        }

        public static bool hasSSE()
        {
            return true;
        }

        public static bool hasSSE2()
        {
            return true;
        }

        public static bool has3DNow()
        {
            return false;
        }

        public static bool hasRDTSC()
        {
            return true;
        }

        public static string cpuVendor()
        {
            string vendor = ReadRegistryString(
                "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0",
                "VendorIdentifier");
            return vendor == "" ? "Unknown" : vendor;
        }

        public static string operatingSystem()
        {
            Version v = Environment.OSVersion.Version;
            string servicePack = ReadRegistryString(
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
                "CSDVersion");
            return Format.format("Windows %d.%d build %d Platform %d %s",
                v.Major, v.Minor, v.Build, 2, servicePack);
        }

        public static string cpuArchitecture()
        {
            int maxAddr = Environment.Is64BitOperatingSystem ? 64 : 32;
            int mhz = ReadRegistryInt32(
                "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", "~MHz");
            string arch = maxAddr == 64 ? "Intel" : "Intel";
            return Format.format("%d x %d-bit %s processor @ %4.1f GHz",
                Environment.ProcessorCount, maxAddr, arch, mhz / 1024.0);
        }

        private static string ReadRegistryString(string keyPath, string valueName)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null)
                    {
                        return "";
                    }
                    object value = key.GetValue(valueName);
                    return value == null ? "" : value.ToString();
                }
            }
            catch
            {
                return "";
            }
        }

        private static int ReadRegistryInt32(string keyPath, string valueName)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null)
                    {
                        return 0;
                    }
                    object value = key.GetValue(valueName);
                    if (value == null)
                    {
                        return 0;
                    }
                    return Convert.ToInt32(value);
                }
            }
            catch
            {
                return 0;
            }
        }

        public static G3DEndian machineEndian()
        {
            return BitConverter.IsLittleEndian ? G3DEndian.G3D_LITTLE_ENDIAN : G3DEndian.G3D_BIG_ENDIAN;
        }

        public static byte[] alignedMalloc(int bytes, int alignment)
        {
            return malloc(bytes);
        }

        public static byte[] malloc(int bytes)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException("bytes");
            }

            countMalloc(bytes);

            try
            {
                return new byte[bytes];
            }
            catch (OutOfMemoryException)
            {
                if (outOfMemoryCallback != null && outOfMemoryCallback(bytes, true))
                {
                    return malloc(bytes);
                }
                throw;
            }
        }

        /**
          Only store buffers up to these sizes (in bytes) in each pool->
          Different pools have different management strategies.
          A large block is preallocated for tiny buffers; they are used with
          tremendous frequency.  Other buffers are allocated as demanded.
         */
        public const int TINY_BUFFER_SIZE = 128;
        public const int SMALL_BUFFER_SIZE = 1024;
        public const int MED_BUFFER_SIZE = 4096;

        private static int mallocTinyCount;
        private static int mallocSmallCount;
        private static int mallocMedCount;
        private static int mallocTotalCount;

        private static void countMalloc(int bytes)
        {
            if (bytes <= TINY_BUFFER_SIZE)
            {
                ++mallocTinyCount;
            }
            else if (bytes <= SMALL_BUFFER_SIZE)
            {
                ++mallocSmallCount;
            }
            else if (bytes <= MED_BUFFER_SIZE)
            {
                ++mallocMedCount;
            }
            ++mallocTotalCount;
        }

        public static string mallocPerformance()
        {
            if (mallocTotalCount > 0)
            {
                int pooled = mallocTinyCount +
                             mallocSmallCount +
                             mallocMedCount;

                int total = mallocTotalCount;

                return Format.format("malloc performance: %5.1f%% <= %db, %5.1f%% <= %db, " +
                                     "%5.1f%% <= %db, %5.1f%% > %db",
                          100.0 * mallocTinyCount / total,
                          TINY_BUFFER_SIZE,
                          100.0 * mallocSmallCount / total,
                          SMALL_BUFFER_SIZE,
                          100.0 * mallocMedCount / total,
                          MED_BUFFER_SIZE,
                          100.0 * (1.0 - (double)pooled / total),
                          MED_BUFFER_SIZE);
            }
            else
            {
                return "No System::malloc calls made yet.";
            }
        }

        public static void resetMallocPerformanceCounters()
        {
            mallocTinyCount = 0;
            mallocSmallCount = 0;
            mallocMedCount = 0;
            mallocTotalCount = 0;
        }

        public static byte[] calloc(int n, int x)
        {
            if (n < 0 || x < 0)
            {
                throw new ArgumentOutOfRangeException("n");
            }

            long bytes = (long)n * (long)x;
            if (bytes > int.MaxValue)
            {
                throw new OutOfMemoryException("Allocation size exceeds Int32.MaxValue.");
            }

            return new byte[bytes];
        }

        public static byte[] realloc(byte[] block, int bytes)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException("bytes");
            }

            byte[] result = new byte[bytes];
            if (block != null)
            {
                Array.Copy(block, result, Math.Min(block.Length, bytes));
            }
            return result;
        }

        public static string mallocStatus()
        {
            return GC.GetTotalMemory(false).ToString();
        }

        public static void free(byte[] p)
        {
        }

        public static void alignedFree(byte[] ptr)
        {
        }

        public static void memcpy(byte[] dst, byte[] src, int numBytes)
        {
            memcpy(dst, 0, src, 0, numBytes);
        }

        public static void memcpy(byte[] dst, int dstOffset, byte[] src, int srcOffset, int numBytes)
        {
            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }
            Array.Copy(src, srcOffset, dst, dstOffset, numBytes);
        }

        public static void memset(byte[] dst, byte value, int numBytes)
        {
            memset(dst, 0, value, numBytes);
        }

        public static void memset(byte[] dst, int dstOffset, byte value, int numBytes)
        {
            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }
            for (int i = 0; i < numBytes; ++i)
            {
                dst[dstOffset + i] = value;
            }
        }

        public static string currentProgramFilename()
        {
            try
            {
                return Process.GetCurrentProcess().MainModule.FileName;
            }
            catch
            {
                return "";
            }
        }

        public static string version()
        {
            const int ver = 60900;
            int beta = ver % 100;
            if (beta != 0)
            {
                return Format.format("G3D %d.%02d beta %d",
                    ver / 10000, (ver / 100) % 100, beta);
            }
            return Format.format("G3D %d.%02d", ver / 10000, (ver / 100) % 100);
        }

        public static string build()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        public static void sleep(double t)
        {
            if (t > 0.0)
            {
                Thread.Sleep((int)(t * 1000.0));
            }
        }

        public static void consoleClearScreen()
        {
            Console.Clear();
        }

        public static bool consoleKeyPressed()
        {
            return Console.KeyAvailable;
        }

        public static int consoleReadKey()
        {
            return Console.Read();
        }

        public static double getTick()
        {
            return (double)global::System.Diagnostics.Stopwatch.GetTimestamp() / (double)global::System.Diagnostics.Stopwatch.Frequency;
        }

        private static readonly double timeOrigin = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
        private static readonly double tickOrigin = getTick();

        public static double getLocalTime()
        {
            return timeOrigin + (getTick() - tickOrigin);
        }

        public static double time()
        {
            return getLocalTime();
        }

        public static void beginCycleCount(ref ulong cycleCount)
        {
            cycleCount = (ulong)global::System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public static void endCycleCount(ref ulong cycleCount)
        {
            cycleCount = (ulong)global::System.Diagnostics.Stopwatch.GetTimestamp() - cycleCount;
        }

        public static ulong getCycleCount()
        {
            return (ulong)global::System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public static void setEnv(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        public static string getEnv(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        public static void describeSystem(TextOutput t)
        {
            t.writeSymbol("OS");
            t.writeSymbol("{");
            t.writeNewline();
            t.pushIndent();
            t.writeSymbol("Name");
            t.writeSymbol("=");
            t.writeString(operatingSystem());
            t.writeNewline();
            t.popIndent();
            t.writeSymbol("}");
            t.writeNewline();
            t.writeNewline();

            t.writeSymbol("CPU");
            t.writeSymbol("{");
            t.writeNewline();
            t.pushIndent();
            t.writeSymbol("Vendor");
            t.writeSymbol("=");
            t.writeString(cpuVendor());
            t.writeNewline();
            t.writeSymbol("Architecture");
            t.writeSymbol("=");
            t.writeString(cpuArchitecture());
            t.writeNewline();
            t.writeSymbol("hasCPUID");
            t.writeSymbol("=");
            t.writeSymbol(hasCPUID() ? "Yes" : "No");
            t.writeNewline();
            t.writeSymbol("hasMMX");
            t.writeSymbol("=");
            t.writeSymbol(hasMMX() ? "Yes" : "No");
            t.writeNewline();
            t.writeSymbol("hasSSE");
            t.writeSymbol("=");
            t.writeSymbol(hasSSE() ? "Yes" : "No");
            t.writeNewline();
            t.writeSymbol("hasSSE2");
            t.writeSymbol("=");
            t.writeSymbol(hasSSE2() ? "Yes" : "No");
            t.writeNewline();
            t.writeSymbol("has3DNow");
            t.writeSymbol("=");
            t.writeSymbol(has3DNow() ? "Yes" : "No");
            t.writeNewline();
            t.writeSymbol("hasRDTSC");
            t.writeSymbol("=");
            t.writeSymbol(hasRDTSC() ? "Yes" : "No");
            t.writeNewline();
            t.popIndent();
            t.writeSymbol("}");
            t.writeNewline();
            t.writeNewline();

            t.writeSymbol("G3D");
            t.writeSymbol("{");
            t.writeNewline();
            t.pushIndent();
            t.writeSymbol("Link version");
            t.writeSymbol("=");
            t.writeNumber(60900);
            t.writeNewline();
            t.writeSymbol("Compile version");
            t.writeSymbol("=");
            t.writeString(version());
            t.writeNewline();
            t.popIndent();
            t.writeSymbol("}");
            t.writeNewline();
            t.writeNewline();
        }

        public static void describeSystem(ref string s)
        {
            TextOutput t = new TextOutput(new TextOutput.Options());
            describeSystem(t);
            s = t.commitString();
        }

        public static int cpuSpeedMHz()
        {
            return 0;
        }

        public static string demoFindData(bool errorIfNotFound)
        {
            Array<string> potential = new Array<string>();

            string x = "../";
            string f = "";
            for (int i = 0; i < 6; ++i)
            {
                potential.append(f);
                f = f + x;
            }

            int ver = 60900;
            string lname = Format.format("g3d-%d_%02d", ver / 10000, (ver / 100) % 100);

            if (ver % 10 != 0)
            {
                lname = lname + Format.format("-b%02d/", ver % 100);
            }
            else
            {
                lname = lname + "/";
            }

            string lpath = "libraries/" + lname;
            potential.append("c:/" + lpath);
            potential.append("d:/" + lpath);
            potential.append("e:/" + lpath);
            potential.append("f:/" + lpath);
            potential.append("g:/" + lpath);
            potential.append("x:/" + lpath);
            potential.append("c:/users/morgan/data/");

            for (int p = 0; p < potential.size(); ++p)
            {
                string path = potential[p];
                if (FileUtils.fileExists(path + "data") && FileUtils.fileExists(path + "data/font"))
                {
                    return path + "data/";
                }
            }

            if (errorIfNotFound)
            {
                throw new InvalidOperationException("Could not find G3D data directory.");
            }

            return "";
        }

        public static uint crc32(byte[] bytes, int numBytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            uint crc = 0xFFFFFFFFU;
            for (int i = 0; i < numBytes; ++i)
            {
                byte b = bytes[i];
                crc ^= b;
                for (int j = 0; j < 8; ++j)
                {
                    if ((crc & 1) != 0)
                    {
                        crc = (crc >> 1) ^ 0xEDB88320U;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return ~crc;
        }
    }
}
