using System.Globalization;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class AudioRecordingService : IDisposable
{
    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private string? currentFilePath;
    private bool disposed;

    public bool IsRecording { get; private set; }

    public string StartRecording()
    {
        ThrowIfDisposed();

        if (IsRecording && currentFilePath is not null)
        {
            return currentFilePath;
        }

        var filePath = CreateRecordingFilePath();
        var recorder = new WaveInEvent();
        WaveFileWriter? fileWriter = null;

        try
        {
            fileWriter = new WaveFileWriter(filePath, recorder.WaveFormat);
            recorder.DataAvailable += OnDataAvailable;
            recorder.StartRecording();
        }
        catch
        {
            recorder.DataAvailable -= OnDataAvailable;
            fileWriter?.Dispose();
            recorder.Dispose();
            SafeDeleteFile(filePath);
            throw;
        }

        waveIn = recorder;
        writer = fileWriter;
        currentFilePath = filePath;
        IsRecording = true;

        return filePath;
    }

    public string StopRecording()
    {
        ThrowIfDisposed();

        if (!IsRecording)
        {
            return currentFilePath ?? string.Empty;
        }

        var savedFilePath = currentFilePath ?? string.Empty;

        try
        {
            waveIn?.StopRecording();
        }
        finally
        {
            CleanupRecordingResources();
            IsRecording = false;
        }

        return savedFilePath;
    }

    public void SafeDeleteRecording(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        SafeDeleteFile(filePath);
    }

    public void CleanupOldRecordings()
    {
        var recordingsFolderPath = GetRecordingsFolderPath();
        var oldestAllowedWriteTimeUtc = DateTime.UtcNow.AddHours(-AudioConstants.TemporaryRecordingMaxAgeHours);

        try
        {
            if (!Directory.Exists(recordingsFolderPath))
            {
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(recordingsFolderPath, AudioConstants.WavSearchPattern))
            {
                if (IsCurrentRecordingFile(filePath) || !IsRecordingFileOld(filePath, oldestAllowedWriteTimeUtc))
                {
                    continue;
                }

                SafeDeleteFile(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors because temporary recording cleanup must not block app startup.
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        CleanupRecordingResources();
        IsRecording = false;
        disposed = true;
    }

    private static string CreateRecordingFilePath()
    {
        var recordingDirectory = GetRecordingsFolderPath();
        Directory.CreateDirectory(recordingDirectory);

        var timestamp = DateTime.Now.ToString(AudioConstants.RecordingTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = $"{AudioConstants.RecordingFilePrefix}{timestamp}{AudioConstants.WavFileExtension}";

        return Path.Combine(recordingDirectory, fileName);
    }

    private static string GetRecordingsFolderPath()
    {
        return Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.RecordingFolderName);
    }

    private static bool IsRecordingFileOld(string filePath, DateTime oldestAllowedWriteTimeUtc)
    {
        try
        {
            return File.GetLastWriteTimeUtc(filePath) < oldestAllowedWriteTimeUtc;
        }
        catch
        {
            return false;
        }
    }

    private bool IsCurrentRecordingFile(string filePath)
    {
        return IsRecording
            && currentFilePath is not null
            && string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(currentFilePath), StringComparison.OrdinalIgnoreCase);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
    {
        writer?.Write(args.Buffer, 0, args.BytesRecorded);
        writer?.Flush();
    }

    private void CleanupRecordingResources()
    {
        if (waveIn is not null)
        {
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.Dispose();
            waveIn = null;
        }

        writer?.Dispose();
        writer = null;
    }

    private static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors so recording and startup flows can continue.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
