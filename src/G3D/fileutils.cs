/**
 @file fileutils.h
 
 @maintainer Morgan McGuire, graphics3d.com
 
 @author  2002-06-06
 @edited  2006-02-04

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Diagnostics;
using global::System.IO;
using global::System.Text;

namespace G3D
{
    public static class FileUtils
    {
        private static readonly Array<string> currentFilesUsed = new Array<string>();

        public static Array<string> filesUsed()
        {
            return currentFilesUsed;
        }

        public static string resolveFilename(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            if (filename.Length >= 1)
            {
                if ((filename[0] == '/') || (filename[0] == '\\'))
                {
                    return filename;
                }
                else
                {
                    if ((filename.Length >= 2) && (filename[1] == ':'))
                    {
                        if ((filename.Length >= 3) && ((filename[2] == '\\') ||
                                                       (filename[2] == '/')))
                        {
                            return filename;
                        }
                        else
                        {
                            global::System.Diagnostics.Debug.Assert(false, "Files of the form d:path are not supported (use a fully qualified name).");
                            return filename;
                        }
                    }
                }
            }

            string buffer = Directory.GetCurrentDirectory();
            return Format.format("%s/%s", buffer, filename);
        }

        public static string readFileAsString(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            currentFilesUsed.append(filename);

            long length = fileLength(filename);

            if (length == -1)
            {
                return "";
            }

            return Encoding.UTF8.GetString(File.ReadAllBytes(filename));
        }

        public static long fileLength(string filename)
        {
            if (File.Exists(filename))
            {
                return new FileInfo(filename).Length;
            }

            if (Directory.Exists(filename))
            {
                return 0;
            }

            return -1;
        }

        public static FileStream createTempFile()
        {
            string path = Path.GetTempFileName();
            return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        }

        public static void writeStringToFile(string str, string filename, bool flush = true)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            string root;
            Array<string> pathArray = new Array<string>();
            string baseName;
            string ext;
            parseFilename(filename, out root, pathArray, out baseName, out ext);

            string path = root + StringUtils.stringJoin(pathArray, '/');
            if (!fileExists(path))
            {
                createDirectory(path);
            }

            File.WriteAllBytes(filename, Encoding.UTF8.GetBytes(str));
        }

        public static void createDirectory(string dir)
        {
            if (dir == "")
            {
                return;
            }

            if (fileExists(dir))
            {
                return;
            }

            Directory.CreateDirectory(dir);
        }

        public static bool fileExists(string filename)
        {
            if (filename == "")
            {
                return true;
            }

            return File.Exists(filename) || Directory.Exists(filename);
        }

        public static void copyFile(string source, string dest)
        {
            File.Copy(source, dest, true);
        }

        public static void parseFilename(
            string filename,
            out string root,
            Array<string> path,
            out string baseName,
            out string ext)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string f = filename;

            root = "";
            path.clear();
            baseName = "";
            ext = "";

            if (f == "")
            {
                return;
            }

            if ((f.Length >= 2) && (f[1] == ':'))
            {
                if ((f.Length > 2) && StringUtils.isSlash(f[2]))
                {
                    root = f.Substring(0, 3);
                    f = f.Substring(3, f.Length - 3);
                }
                else
                {
                    root = f.Substring(2);
                    f = f.Substring(2, f.Length - 2);
                }
            }
            else if (((f.Length >= 2) & StringUtils.isSlash(f[0])) && StringUtils.isSlash(f[1]))
            {
                root = f.Substring(0, 2);
                f = f.Substring(2, f.Length - 2);
            }
            else if (StringUtils.isSlash(f[0]))
            {
                root = f.Substring(0, 1);
                f = f.Substring(1, f.Length - 1);
            }

            {
                int i = f.LastIndexOf('.');

                int j = g3dmath.iMax(f.LastIndexOf('/'), f.LastIndexOf('\\'));
                if ((i != -1) && (i > j))
                {
                    ext = f.Substring(i + 1, f.Length - i - 1);
                    f = f.Substring(0, i);
                }
            }

            {
                int i = g3dmath.iMax(f.LastIndexOf('/'), f.LastIndexOf('\\'));

                if (i == -1)
                {
                    baseName = f;
                    f = "";
                }
                else if ((i != -1) && (i < f.Length - 1))
                {
                    baseName = f.Substring(i + 1, f.Length - i - 1);
                    f = f.Substring(0, i);
                }
            }

            int prev;
            int cur = 0;

            while (cur < f.Length)
            {
                prev = cur;

                int i = f.IndexOf('/', prev + 1);
                int j = f.IndexOf('\\', prev + 1);
                if (i == -1)
                {
                    i = f.Length;
                }

                if (j == -1)
                {
                    j = f.Length;
                }

                cur = g3dmath.iMin(i, j);

                path.append(f.Substring(prev, cur - prev));
                ++cur;
            }
        }

        private static void getFileOrDirList(
            string filespec,
            Array<string> files,
            bool wantFiles,
            bool includePath)
        {
            if (filespec == null)
            {
                throw new ArgumentNullException("filespec");
            }
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            string path = Path.GetDirectoryName(filespec);
            string pattern = Path.GetFileName(filespec);

            if (path == null)
            {
                path = "";
            }

            string prefix = path;
            if (path.Length > 0)
            {
                prefix += "/";
            }

            string searchPath = (path.Length == 0) ? "." : path;

            FileSystemInfo[] entries = wantFiles
                ? (FileSystemInfo[])(new DirectoryInfo(searchPath)).GetFiles(pattern)
                : (FileSystemInfo[])(new DirectoryInfo(searchPath)).GetDirectories(pattern);

            for (int i = 0; i < entries.Length; ++i)
            {
                string name = entries[i].Name;
                if ((name != ".") && (name != ".."))
                {
                    if (includePath)
                    {
                        files.append(prefix + name);
                    }
                    else
                    {
                        files.append(name);
                    }
                }
            }
        }

        public static void getFiles(
            string filespec,
            Array<string> files,
            bool includePath = false)
        {
            getFileOrDirList(filespec, files, true, includePath);
        }

        public static void getDirs(
            string filespec,
            Array<string> files,
            bool includePath = false)
        {
            getFileOrDirList(filespec, files, false, includePath);
        }

        public static string filenameBaseExt(string filename)
        {
            int i = filename.LastIndexOf("/");
            int j = filename.LastIndexOf("\\");

            if ((j > i) && (j >= 0))
            {
                i = j;
            }

            j = filename.LastIndexOf(":");
            if ((i == -1) && (j >= 0))
            {
                i = j;
            }

            if (i == -1)
            {
                return filename;
            }
            else
            {
                return filename.Substring(i + 1, filename.Length - i - 1);
            }
        }

        public static string filenameExt(string filename)
        {
            int i = filename.LastIndexOf(".");
            if (i >= 0)
            {
                return filename.Substring(i + 1, filename.Length - i - 1);
            }
            else
            {
                return "";
            }
        }

        public static string filenamePath(string filename)
        {
            int i = filename.LastIndexOf("/");
            int j = filename.LastIndexOf("\\");

            if ((j > i) && (j >= 0))
            {
                i = j;
            }

            j = filename.LastIndexOf(":");
            if ((i == -1) && (j >= 0))
            {
                i = j;
            }

            if (i == -1)
            {
                return "";
            }
            else
            {
                return filename.Substring(0, i + 1);
            }
        }

        public static bool isDirectory(string filename)
        {
            return Directory.Exists(filename);
        }

        public static bool filenameContainsWildcards(string filename)
        {
            return (filename.IndexOf('*') != -1) || (filename.IndexOf('?') != -1);
        }

        public static bool fileIsNewer(string src, string dst)
        {
            bool sexists = File.Exists(src) || Directory.Exists(src);
            bool dexists = File.Exists(dst) || Directory.Exists(dst);

            if (!sexists)
            {
                return false;
            }

            if (!dexists)
            {
                return true;
            }

            DateTime stime = FileSystemInfoTime(src);
            DateTime dtime = FileSystemInfoTime(dst);
            return stime > dtime;
        }

        private static DateTime FileSystemInfoTime(string path)
        {
            FileSystemInfo info = File.Exists(path)
                ? (FileSystemInfo)new FileInfo(path)
                : (FileSystemInfo)new DirectoryInfo(path);
            return info.LastWriteTimeUtc;
        }
    }
}
