using System.Diagnostics;
using System.Globalization;
using System.IO;
using EnglishVoiceTutor.Desktop.Constants;
using NAudio.Wave;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class AudioRecordingService : IDisposable
{
    private readonly object writerLock = new();
    private readonly AudioInputDeviceService audioInputDeviceService = new();
    private WaveInEvent? waveIn;
    private WaveFileWriter? writer;
    private string? currentFilePath;
    private DateTimeOffset? recordingStartedAt;
    private bool disposed;

    public bool IsRecording { get; private set; }

    public TimeSpan LastRecordingDuration { get; private set; }

    public TimeSpan CurrentRecordingDuration => recordingStartedAt is null
        ? TimeSpan.Zero
        : DateTimeOffset.UtcNow - recordingStartedAt.Value;

    public string StartRecording(string? audioInputDeviceId = null)
    {
        ThrowIfDisposed();

        if (IsRecording)
        {
            throw new InvalidOperationException(AudioConstants.RecordingAlreadyInProgressMessage);
        }

        CleanupRecordingResources();

        var filePath = CreateRecordingFilePath();
        var recorder = CreateRecorder(audioInputDeviceId);
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
        recordingStartedAt = DateTimeOffset.UtcNow;
        Debug.WriteLine($"Voice recording started: StartedAtUtc={recordingStartedAt.Value:O}; FileName={Path.GetFileName(filePath)}.");
        LastRecordingDuration = TimeSpan.Zero;
        IsRecording = true;

        return filePath;
    }

    public string StopRecording()
    {
        ThrowIfDisposed();

        if (!IsRecording)
        {
            return string.Empty;
        }

        var savedFilePath = currentFilePath ?? string.Empty;
        LastRecordingDuration = CurrentRecordingDuration;

        try
        {
            waveIn?.StopRecording();
        }
        finally
        {
            CleanupRecordingResources();
            var stoppedAt = DateTimeOffset.UtcNow;
            var savedFileInfo = string.IsNullOrWhiteSpace(savedFilePath) ? null : new FileInfo(savedFilePath);
            Debug.WriteLine($"Voice recording stopped: StoppedAtUtc={stoppedAt:O}; FileName={Path.GetFileName(savedFilePath)}; FileSizeBytes={(savedFileInfo?.Exists == true ? savedFileInfo.Length : 0)}; DurationMs={LastRecordingDuration.TotalMilliseconds:F0};");
            recordingStartedAt = null;
            currentFilePath = null;
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
        recordingStartedAt = null;
        currentFilePath = null;
        IsRecording = false;
        disposed = true;
    }

    private WaveInEvent CreateRecorder(string? audioInputDeviceId)
    {
        var recorder = new WaveInEvent();
        var deviceNumber = audioInputDeviceService.ResolveDeviceNumber(audioInputDeviceId);

        if (deviceNumber.HasValue)
        {
            recorder.DeviceNumber = deviceNumber.Value;
        }

        return recorder;
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
        lock (writerLock)
        {
            writer?.Write(args.Buffer, 0, args.BytesRecorded);
            writer?.Flush();
        }
    }

    private void CleanupRecordingResources()
    {
        if (waveIn is not null)
        {
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.Dispose();
            waveIn = null;
        }

        lock (writerLock)
        {
            writer?.Dispose();
            writer = null;
        }
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
