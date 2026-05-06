using System.Globalization;
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
        var recordingDirectory = Path.Combine(Path.GetTempPath(), AudioConstants.AppTempFolderName, AudioConstants.RecordingFolderName);
        Directory.CreateDirectory(recordingDirectory);

        var timestamp = DateTime.Now.ToString(AudioConstants.RecordingTimestampFormat, CultureInfo.InvariantCulture);
        var fileName = $"{AudioConstants.RecordingFilePrefix}{timestamp}{AudioConstants.WavFileExtension}";

        return Path.Combine(recordingDirectory, fileName);
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
            // Ignore cleanup errors so the original recording error can be shown.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
