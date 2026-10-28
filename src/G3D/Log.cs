/**
  @file Log.h

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @cite Backtrace by Aaron Orenstein
  @created 2001-08-04
  @edited  2005-11-04
 */

using System;
using global::System.IO;

namespace G3D
{
    public sealed class Log : IDisposable
    {
        private StreamWriter logFile;
        private string filename;
        private static Log commonLog;
        private int stripFromStackBottom;

        public Log(string filename = "log.txt", int stripFromStackBottom = 0)
        {
            this.stripFromStackBottom = stripFromStackBottom;
            this.filename = filename;

            try
            {
                logFile = new StreamWriter(filename, false);
            }
            catch (IOException)
            {
                logFile = null;
            }
            catch (UnauthorizedAccessException)
            {
                logFile = null;
            }

            if (logFile == null)
            {
                string root;
                Array<string> path = new Array<string>();
                string baseName;
                string ext;
                FileUtils.parseFilename(filename, out root, path, out baseName, out ext);
                string logName = baseName + ((ext != "") ? ("." + ext) : "");

                if (FileUtils.fileExists("c:/tmp"))
                {
                    logName = "c:/tmp/" + logName;
                }
                else if (FileUtils.fileExists("c:/temp"))
                {
                    logName = "c:/temp/" + logName;
                }
                else
                {
                    logName = "c:/" + logName;
                }

                logFile = new StreamWriter(logName, false);
                this.filename = logName;
            }

            logFile.AutoFlush = true;

            logFile.Write("Application Log\n");
            logFile.Write("Start: " + DateTime.Now.ToString() + "\n");

            if (commonLog == null)
            {
                commonLog = this;
            }
        }

        public void Dispose()
        {
            section("Shutdown");
            println("Closing log file");

            if (object.ReferenceEquals(commonLog, this))
            {
                commonLog = null;
            }

            if (logFile != null)
            {
                logFile.Close();
                logFile = null;
            }
        }

        public StreamWriter getFile()
        {
            return logFile;
        }

        public static Log common()
        {
            if (commonLog == null)
            {
                commonLog = new Log();
            }
            return commonLog;
        }

        public static string getCommonLogFilename()
        {
            return common().filename;
        }

        public void section(string s)
        {
            logFile.Write("_____________________________________________________\n");
            logFile.Write("\n    ###    " + s + "    ###\n\n");
        }

        public void printf(string fmt, params object[] args)
        {
            printHeader();

            print(Format.vformat(fmt, args));
        }

        public void vprintf(string fmt, object[] argPtr)
        {
            logFile.Write(Format.vformat(fmt, argPtr));
        }

        public void print(string s)
        {
            printHeader();
            logFile.Write(s);
        }

        public void println(string s)
        {
            printHeader();
            logFile.Write(s + "\n");
        }

        private void printHeader()
        {
        }
    }
}
