using grzyClothTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

/// <summary>
/// Manages automatic file backup and recovery for crash protection
/// </summary>
public static class AutoRecoveryHelper
{
    private static string _recoveryPath;
    private static string _currentSessionId;
    private static Dictionary<string, RecoveryFileInfo> _fileMapping = new();
    private static readonly object _lock = new();

    public static string RecoveryPath
    {
        get
        {
            if (string.IsNullOrEmpty(_recoveryPath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _recoveryPath = Path.Combine(appData, "grzyClothTool", "recovery");
                Directory.CreateDirectory(_recoveryPath);
            }
            return _recoveryPath;
        }
    }

    /// <summary>
    /// Initializes a new recovery session
    /// </summary>
    public static void StartSession()
    {
        lock (_lock)
        {
            _currentSessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            _fileMapping.Clear();
            
            var sessionPath = GetSessionPath();
            Directory.CreateDirectory(sessionPath);
            
            LogHelper.Log($"Started auto-recovery session: {_currentSessionId}");
        }
    }

    /// <summary>
    /// Backs up a file to the recovery cache
    /// </summary>
    public static async Task<string> BackupFileAsync(string originalPath, string displayName, string addonName)
    {
        if (string.IsNullOrEmpty(_currentSessionId))
        {
            StartSession();
        }

        lock (_lock)
        {
            var cacheFileName = $"{Guid.NewGuid():N}{Path.GetExtension(originalPath)}";
            var cachePath = Path.Combine(GetSessionPath(), cacheFileName);

            try
            {
                // Copy the file to cache
                File.Copy(originalPath, cachePath, true);

                // Store the mapping
                _fileMapping[cacheFileName] = new RecoveryFileInfo
                {
                    CacheFileName = cacheFileName,
                    OriginalPath = originalPath,
                    DisplayName = displayName,
                    AddonName = addonName,
                    BackupTime = DateTime.Now
                };

                // Save the mapping file
                SaveMappingFile();

                LogHelper.Log($"Backed up file: {displayName} -> {cacheFileName}");
                return cachePath;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to backup file {displayName}: {ex.Message}");
                return originalPath; // Return original if backup fails
            }
        }
    }

    /// <summary>
    /// Closes the current session - deletes if empty, marks as closed if has files
    /// </summary>
    public static void CloseSession()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_currentSessionId)) return;

            try
            {
                var sessionPath = GetSessionPath();
                
                // Check if session has any backed up files
                if (_fileMapping == null || _fileMapping.Count == 0)
                {
                    // Empty session - delete it entirely to save space
                    if (Directory.Exists(sessionPath))
                    {
                        Directory.Delete(sessionPath, true);
                        LogHelper.Log($"Deleted empty recovery session: {_currentSessionId}");
                    }
                }
                else
                {
                    // Session has files - mark as properly closed
                    var closedMarker = Path.Combine(sessionPath, ".closed");
                    File.WriteAllText(closedMarker, DateTime.Now.ToString("o"));
                    LogHelper.Log($"Closed auto-recovery session: {_currentSessionId} with {_fileMapping.Count} files");
                }
                
                _currentSessionId = null;
                _fileMapping.Clear();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to close recovery session: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Cleans up the current session files
    /// </summary>
    public static void CleanupSession()
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(_currentSessionId)) return;

            try
            {
                var sessionPath = GetSessionPath();
                if (Directory.Exists(sessionPath))
                {
                    Directory.Delete(sessionPath, true);
                    LogHelper.Log($"Cleaned up recovery session: {_currentSessionId}");
                }

                _currentSessionId = null;
                _fileMapping.Clear();
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to cleanup recovery session: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks for unclosed recovery sessions on startup
    /// </summary>
    public static RecoverySession FindUnclosedSession()
    {
        var debugInfo = new System.Text.StringBuilder();
        debugInfo.AppendLine($"=== AUTO-RECOVERY DEBUG LOG ===");
        debugInfo.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        debugInfo.AppendLine($"Recovery Path: {RecoveryPath}\n");
        
        try
        {
            var sessions = Directory.GetDirectories(RecoveryPath, "session_*");
            debugInfo.AppendLine($"Total sessions found: {sessions.Length}\n");
            
            foreach (var sessionPath in sessions)
            {
                var sessionName = Path.GetFileName(sessionPath);
                var closedMarker = Path.Combine(sessionPath, ".closed");
                var hasClosed = File.Exists(closedMarker);
                var mappingFile = Path.Combine(sessionPath, "mapping.json");
                var hasMapping = File.Exists(mappingFile);
                
                debugInfo.AppendLine($"Session: {sessionName}");
                debugInfo.AppendLine($"  Has .closed: {hasClosed}");
                debugInfo.AppendLine($"  Has mapping.json: {hasMapping}");
                
                // If no closed marker, this is an unclosed session
                if (!hasClosed)
                {
                    if (hasMapping)
                    {
                        var json = File.ReadAllText(mappingFile);
                        debugInfo.AppendLine($"  JSON size: {json.Length} bytes");
                        
                        var fileMapping = JsonSerializer.Deserialize<Dictionary<string, RecoveryFileInfo>>(json);
                        debugInfo.AppendLine($"  Files in mapping: {(fileMapping != null ? fileMapping.Count.ToString() : "NULL")}");
                        
                        if (fileMapping != null && fileMapping.Count > 0)
                        {
                            var sessionId = Path.GetFileName(sessionPath);
                            var oldestFile = fileMapping.Values.OrderBy(f => f.BackupTime).First();
                            
                            debugInfo.AppendLine($"\n✓ FOUND UNCLOSED SESSION TO RECOVER!");
                            debugInfo.AppendLine($"  Session ID: {sessionId}");
                            debugInfo.AppendLine($"  File count: {fileMapping.Count}");
                            debugInfo.AppendLine($"  Created: {oldestFile.BackupTime}");
                            
                            // Write debug log
                            WriteDebugLog(debugInfo.ToString());
                            
                            return new RecoverySession
                            {
                                SessionId = sessionId,
                                SessionPath = sessionPath,
                                FileCount = fileMapping.Count,
                                CreatedTime = oldestFile.BackupTime,
                                FileMapping = fileMapping
                            };
                        }
                        else
                        {
                            debugInfo.AppendLine($"  ✗ Mapping is null or empty");
                        }
                    }
                    else
                    {
                        debugInfo.AppendLine($"  ✗ No mapping file (empty session)");
                    }
                }
                
                debugInfo.AppendLine();
            }
            
            debugInfo.AppendLine("Result: No unclosed sessions with files found.");
            
            // Write debug log
            WriteDebugLog(debugInfo.ToString());
        }
        catch (Exception ex)
        {
            debugInfo.AppendLine($"\n✗ EXCEPTION: {ex.Message}");
            debugInfo.AppendLine($"Stack: {ex.StackTrace}");
            WriteDebugLog(debugInfo.ToString());
            LogHelper.Log($"Error checking for recovery sessions: {ex.Message}");
        }
        
        return null;
    }
    
    private static void WriteDebugLog(string content)
    {
        try
        {
            var logPath = Path.Combine(RecoveryPath, "recovery_debug.txt");
            File.WriteAllText(logPath, content);
        }
        catch
        {
            // Ignore if can't write debug log
        }
    }

    /// <summary>
    /// Recovers files from a session
    /// </summary>
    public static async Task<bool> RecoverSessionAsync(RecoverySession session)
    {
        try
        {
            LogHelper.Log($"Recovering session: {session.SessionId}");
            
            // Set this as the current session
            _currentSessionId = session.SessionId;
            _fileMapping = session.FileMapping;

            // Create a new project with recovery name
            MainWindow.AddonManager.Addons = [];
            MainWindow.AddonManager.ProjectName = $"Recovery_{session.CreatedTime:yyyyMMdd_HHmmss}";

            // Collect all YDD files from the session, grouped by sex
            var maleFiles = new List<string>();
            var femaleFiles = new List<string>();
            
            foreach (var fileInfo in session.FileMapping.Values)
            {
                var cachePath = Path.Combine(session.SessionPath, fileInfo.CacheFileName);
                
                if (File.Exists(cachePath) && Path.GetExtension(cachePath).ToLower() == ".ydd")
                {
                    // Create a temporary copy with the original name for loading
                    var tempPath = Path.Combine(Path.GetTempPath(), fileInfo.DisplayName);
                    File.Copy(cachePath, tempPath, true);
                    
                    // Determine sex from original file path/name
                    // Assuming files with "_m_" or ending with "_u" are male, "_f_" or "_r" are female
                    var fileName = fileInfo.DisplayName.ToLower();
                    if (fileName.Contains("_m_") || fileName.EndsWith("_u.ydd"))
                    {
                        maleFiles.Add(tempPath);
                    }
                    else if (fileName.Contains("_f_") || fileName.EndsWith("_r.ydd"))
                    {
                        femaleFiles.Add(tempPath);
                    }
                    else
                    {
                        // Default to male if can't determine
                        maleFiles.Add(tempPath);
                    }
                }
            }

            // Add male drawables
            if (maleFiles.Count > 0)
            {
                await MainWindow.AddonManager.AddDrawables(maleFiles.ToArray(), Enums.SexType.male, null);
            }

            // Add female drawables
            if (femaleFiles.Count > 0)
            {
                await MainWindow.AddonManager.AddDrawables(femaleFiles.ToArray(), Enums.SexType.female, null);
            }

            LogHelper.Log($"Successfully recovered {session.FileCount} files");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Failed to recover session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Discards a recovery session
    /// </summary>
    public static void DiscardSession(RecoverySession session)
    {
        try
        {
            if (Directory.Exists(session.SessionPath))
            {
                Directory.Delete(session.SessionPath, true);
                LogHelper.Log($"Discarded recovery session: {session.SessionId}");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Failed to discard recovery session: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up old sessions:
    /// - Removes empty unclosed sessions (no files)
    /// - Keeps last 3 closed sessions
    /// - Limits total sessions to 20
    /// </summary>
    public static void CleanupOldSessions()
    {
        try
        {
            var allSessions = Directory.GetDirectories(RecoveryPath, "session_*")
                .Select(path => new
                {
                    Path = path,
                    IsClosed = File.Exists(Path.Combine(path, ".closed")),
                    HasMapping = File.Exists(Path.Combine(path, "mapping.json")),
                    CreatedTime = Directory.GetCreationTime(path)
                })
                .ToList();

            int deletedCount = 0;

            // 1. Delete empty unclosed sessions (no mapping file = no files backed up)
            var emptySessions = allSessions
                .Where(s => !s.IsClosed && !s.HasMapping)
                .ToList();

            foreach (var session in emptySessions)
            {
                try
                {
                    Directory.Delete(session.Path, true);
                    deletedCount++;
                }
                catch { /* Ignore individual failures */ }
            }

            // Refresh the list after deleting empty sessions
            allSessions = Directory.GetDirectories(RecoveryPath, "session_*")
                .Select(path => new
                {
                    Path = path,
                    IsClosed = File.Exists(Path.Combine(path, ".closed")),
                    HasMapping = File.Exists(Path.Combine(path, "mapping.json")),
                    CreatedTime = Directory.GetCreationTime(path)
                })
                .ToList();

            // 2. Keep only last 3 closed sessions, delete older ones
            var oldClosedSessions = allSessions
                .Where(s => s.IsClosed)
                .OrderByDescending(s => s.CreatedTime)
                .Skip(3)
                .ToList();

            foreach (var session in oldClosedSessions)
            {
                try
                {
                    Directory.Delete(session.Path, true);
                    deletedCount++;
                }
                catch { /* Ignore individual failures */ }
            }

            // 3. If still over 20 total sessions, delete oldest sessions (keep unclosed with files)
            var remainingSessions = Directory.GetDirectories(RecoveryPath, "session_*")
                .Select(path => new
                {
                    Path = path,
                    IsClosed = File.Exists(Path.Combine(path, ".closed")),
                    HasMapping = File.Exists(Path.Combine(path, "mapping.json")),
                    CreatedTime = Directory.GetCreationTime(path)
                })
                .ToList();

            if (remainingSessions.Count > 20)
            {
                // Delete oldest sessions, but keep unclosed sessions with files (potential recovery)
                var sessionsToDelete = remainingSessions
                    .Where(s => s.IsClosed || !s.HasMapping) // Only delete closed or empty sessions
                    .OrderBy(s => s.CreatedTime)
                    .Take(remainingSessions.Count - 20)
                    .ToList();

                foreach (var session in sessionsToDelete)
                {
                    try
                    {
                        Directory.Delete(session.Path, true);
                        deletedCount++;
                    }
                    catch { /* Ignore individual failures */ }
                }
            }

            if (deletedCount > 0)
            {
                LogHelper.Log($"Cleaned up {deletedCount} old recovery session(s)");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Failed to cleanup old sessions: {ex.Message}");
        }
    }

    private static string GetSessionPath()
    {
        return Path.Combine(RecoveryPath, _currentSessionId);
    }

    private static void SaveMappingFile()
    {
        try
        {
            var mappingFile = Path.Combine(GetSessionPath(), "mapping.json");
            var json = JsonSerializer.Serialize(_fileMapping, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(mappingFile, json);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Failed to save mapping file: {ex.Message}");
        }
    }
}

/// <summary>
/// Information about a backed up file
/// </summary>
public class RecoveryFileInfo
{
    public string CacheFileName { get; set; }
    public string OriginalPath { get; set; }
    public string DisplayName { get; set; }
    public string AddonName { get; set; }
    public DateTime BackupTime { get; set; }
}

/// <summary>
/// Information about a recovery session
/// </summary>
public class RecoverySession
{
    public string SessionId { get; set; }
    public string SessionPath { get; set; }
    public int FileCount { get; set; }
    public DateTime CreatedTime { get; set; }
    public Dictionary<string, RecoveryFileInfo> FileMapping { get; set; }
}
