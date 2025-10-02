﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace grzyClothTool.Helpers;

public static class UpdateHelper
{
    private static readonly HttpClient _httpClient;
    private static readonly string _exeLocation;

    static UpdateHelper()
    {
        _httpClient = new HttpClient();
        _exeLocation = GetExeLocation();
    }

    private static string GetExeLocation()
    {
        string assemblyName = Assembly.GetEntryAssembly().GetName().Name;
        var assemblyLocation = Path.Join(AppContext.BaseDirectory, $"{assemblyName}.exe");

        return assemblyLocation;
    }

    public static string GetCurrentVersion()
    {
        return FileVersionInfo.GetVersionInfo(_exeLocation).FileVersion;
    }

    public async static Task CheckForUpdates()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args.Contains("--skipUpdate"))
        {
            App.splashScreen.AddMessage("Skipping update.");

            var removeTempFilesArg = args.FirstOrDefault(arg => arg.StartsWith("--removeTempFiles"));
            if (removeTempFilesArg != null)
            {
                var path = removeTempFilesArg.Split('=')[1].Trim('"');
                RemoveTempFiles(path);
            }

            return;
        }

        string currentVersion = GetCurrentVersion();
        var latestVersion = await GetLatestVersion();

        if (latestVersion is null)
        {
            App.splashScreen.AddMessage("Checking for update failed.");
            return;
        }

        if(latestVersion == currentVersion)
        {
            App.splashScreen.AddMessage("No new updates found.");
            return;
        }

        App.splashScreen.AddMessage("New update found. Downloading...");
        await DownloadUpdate(latestVersion);

    }

    private async static Task<string> GetLatestVersion()
    {
        try
        {
            string url = "https://raw.githubusercontent.com/grzybeek/grzyClothTool/master/grzyClothTool/grzyClothTool.csproj";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            // Parse the content as XML
            XDocument doc = XDocument.Parse(content);
            XElement variableElement = doc.Root.Element("PropertyGroup").Element("FileVersion");

            return variableElement.Value;
        }
        catch
        {
            return null;
        }
    }

    private static async Task DownloadUpdate(string version)
    {
        string url = $"https://github.com/grzybeek/grzyClothTool/releases/download/v{version}/grzyClothTool.zip";

        string tempPath = Path.GetTempPath();
        string downloadZip = Path.Combine(tempPath, "grzyClothTool.zip");

        //remove old zip and folder
        if (File.Exists(downloadZip))
        {
            File.Delete(downloadZip);
        }

        var extractFolder = Path.Combine(tempPath, "grzyClothTool");
        if (Directory.Exists(extractFolder))
        {
            Directory.Delete(extractFolder, true);
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var fileStream = new FileStream(downloadZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            var totalBytes = response.Content.Headers.ContentLength.Value;
            var buffer = await response.Content.ReadAsByteArrayAsync();
            await fileStream.WriteAsync(buffer);
        }
        catch(Exception ex)
        {
            File.WriteAllText("download_failed.log", ex.ToString());

            App.splashScreen.AddMessage("Downloading failed");
            return;
        }

        ExtractAndRunUpdatedApp();
    }

    private static void ExtractAndRunUpdatedApp()
    {
        string tempPath = Path.GetTempPath();
        string downloadZip = Path.Combine(tempPath, "grzyClothTool.zip");

        System.IO.Compression.ZipFile.ExtractToDirectory(downloadZip, tempPath);
        File.Delete(downloadZip);

        var newExeLocation = Path.Combine(tempPath, "grzyClothTool", "grzyClothTool.exe");
        //run exe with args
        ProcessStartInfo startInfo = new()
        {
            FileName = newExeLocation,
            ArgumentList = { "--skipUpdate", $"--removeTempFiles=\"{_exeLocation}\"" },
            UseShellExecute = true
        };
        Process.Start(startInfo);

        App.splashScreen.Shutdown();
        Application.Current.Shutdown();
    }

    public static void RemoveTempFiles(string oldExeLocation)
    {
        //remove .exe and .dll.config from oldExeLocation
        string[] fileExtensions = [".exe", ".dll.config"];
        foreach (var extension in fileExtensions)
        {
            string[] filesToDelete = Directory.GetFiles(Path.GetDirectoryName(oldExeLocation), $"grzyClothTool{extension}");
            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        //get all files from current exe location and move them to oldExeLocation
        string[] files = Directory.GetFiles(AppContext.BaseDirectory);
        foreach (var file in files)
        {
            File.Move(file, Path.Combine(Path.GetDirectoryName(oldExeLocation), Path.GetFileName(file)));
        }

        string tempPath = Path.GetTempPath();
        string extractFolder = Path.Combine(tempPath, "grzyClothTool");

        if (Directory.Exists(extractFolder))
        {
            Directory.Delete(extractFolder, true);
        }
    }

}
