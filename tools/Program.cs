using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.IO.Compression;

public class App
{
    /** Repository root: the folder that contains tools\ and src\.
        Equivalent to the script's (Split-Path -Parent $PSScriptRoot), but
        resolved from the assembly location so the tool does not depend on the
        current working directory. */
    static string FindRepoRoot()
    {
        string[] starts = { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };

        foreach (string start in starts)
        {
            DirectoryInfo? dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "tools")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "src")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (a folder containing tools\\ and src\\).");
    }

    static string root = FindRepoRoot();
    static string lib = Path.Combine(root, "lib");
    static string cache = Path.Combine(Path.GetTempPath(), "g3d6-deps");

    static void Main(string[] args)
    {
        Directory.CreateDirectory(lib);
        Directory.CreateDirectory(cache);

        var openTk = GetNuGetPackage("OpenTK", "3.1.0");
        CopyPackageAssembly(openTk, @"lib\net20\OpenTK.dll", "OpenTK.dll");

        var openTkGlControl = GetNuGetPackage("OpenTK.GLControl", "3.1.0");
        CopyPackageAssembly(openTkGlControl, @"lib\net20\OpenTK.GLControl.dll", "OpenTK.GLControl.dll");

        var sharpDx = GetNuGetPackage("SharpDX", "4.2.0");
        CopyPackageAssembly(sharpDx, @"lib\net40\SharpDX.dll", "SharpDX.dll");

        var sharpDxDxgi = GetNuGetPackage("SharpDX.DXGI", "4.2.0");
        CopyPackageAssembly(sharpDxDxgi, @"lib\net40\SharpDX.DXGI.dll", "SharpDX.DXGI.dll");

        var sharpDxD3D11 = GetNuGetPackage("SharpDX.Direct3D11", "4.2.0");
        CopyPackageAssembly(sharpDxD3D11, @"lib\net40\SharpDX.Direct3D11.dll", "SharpDX.Direct3D11.dll");

        var sharpDxD3DCompiler = GetNuGetPackage("SharpDX.D3DCompiler", "4.2.0");
        CopyPackageAssembly(sharpDxD3DCompiler, @"lib\net40\SharpDX.D3DCompiler.dll", "SharpDX.D3DCompiler.dll");

        // Get-ChildItem -LiteralPath $lib -Filter '*.dll' | Select-Object Name, Length
        foreach (var file in new DirectoryInfo(lib).GetFiles("*.dll").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine("{0,-26} {1,10} bytes", file.Name, file.Length);
        }
    }

    static string GetNuGetPackage(string Id, string Version)
    {
        var lower = Id.ToLowerInvariant();
        var nupkg = Path.Combine(cache, lower + "." + Version + ".nupkg");
        var destination = Path.Combine(cache, lower + "." + Version);

        if (!File.Exists(nupkg))
        {
            var uri = $"https://api.nuget.org/v3-flatcontainer/{lower}/{Version}/{lower}.{Version}.nupkg";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage res = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result)
                    {
                        using (HttpContent content = res.Content)
                        {
                            res.EnsureSuccessStatusCode();

                            byte[] data = content.ReadAsByteArrayAsync().Result;

                            if (data != null && data.Length > 0)
                            { File.WriteAllBytes(nupkg, data); }

                            else
                            { throw new InvalidOperationException("Empty response for " + uri); }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception Hit------------");
                Console.WriteLine(exception);
                throw;
            }
        }
        else
        {
            Console.WriteLine("Cached " + Path.GetFileName(nupkg));
        }

        if (!File.Exists(nupkg))
        {
            throw new FileNotFoundException("The .nupkg was never downloaded.", nupkg);
        }

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, true);
        }
        ZipFile.ExtractToDirectory(nupkg, destination, true);
        return destination;
    }

    public static void CopyPackageAssembly(string PackageDirectory, string RelativePath, string FileName)
    {
        var source = Path.Combine(PackageDirectory, RelativePath);
        if (!File.Exists(source))
        {
            throw new ArgumentException($"Missing assembly: {source}");
        }

        // Copy-Item -LiteralPath $source -Destination (Join-Path $lib $FileName) -Force
        File.Copy(source, Path.Combine(lib, FileName), true);
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
