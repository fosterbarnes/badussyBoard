using BadussyBoard;
using NAudio.Wave;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public class SoundManager
{
    private readonly List<(WaveOutEvent player, AudioFileReader audio)> currentlyPlaying = new(); // Track currently playing sounds
    private readonly Dictionary<int, SoundItem> hotkeyMap = new(); // hotkeyId → SoundItem
    private int nextHotkeyId = 0;

    private IntPtr _windowHandle;

    public SoundManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessage;
    }

    public void Play(string filePath)
    {
        try
        {
            var audioFile = new AudioFileReader(filePath);
            var output = new WaveOutEvent();

            output.Init(audioFile);
            output.Play();
            currentlyPlaying.Add((output, audioFile));

            // Dispose objects when playback stops
            output.PlaybackStopped += (s, args) =>
            {
                output.Dispose();
                audioFile.Dispose();
                currentlyPlaying.RemoveAll(x => x.player == output);
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error playing sound:\n{ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    public void StopAll()
    {
        foreach (var (player, audio) in currentlyPlaying.ToList())
        {
            player.Stop();
            // Disposal handled in PlaybackStopped event
        }
    }

    public void RegisterHotkey(SoundItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Hotkey))
            return;

        ParseHotkey(item.Hotkey, out uint modifiers, out uint key);

        // Check if this hotkey is already registered and unregister if necessary
        int existingId = hotkeyMap.FirstOrDefault(kv => kv.Value.Hotkey == item.Hotkey).Key;
        if (existingId != 0)
        {
            UnregisterHotKey(_windowHandle, existingId);
            hotkeyMap.Remove(existingId);
        }

        int id = nextHotkeyId++;
        if (RegisterHotKey(_windowHandle, id, modifiers, key))
        {
            hotkeyMap[id] = item;
            Debug.WriteLine($"[SoundManager] Registered hotkey: {item.Hotkey} (mods={modifiers}, key={key})");
        }
        else
        {
            Debug.WriteLine($"[SoundManager] Failed to register hotkey: {item.Hotkey}");
        }
    }

    public void UnregisterAllHotkeys()
    {
        foreach (var id in hotkeyMap.Keys)
        {
            UnregisterHotKey(_windowHandle, id);
        }
        hotkeyMap.Clear();
    }

    private void ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg.message == WM_HOTKEY)
        {
            int id = msg.wParam.ToInt32();
            if (hotkeyMap.TryGetValue(id, out var item))
            {
                Play(item.FilePath);
                handled = true;
            }
        }
    }
    
    #region Win32 Interop
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private void ParseHotkey(string hotkey, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;

        string[] parts = hotkey.Split('+', StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                    modifiers |= 0x0002;  break; // MOD_CONTROL
                case "ALT":
                    modifiers |= 0x0001; break; // MOD_ALT 
                case "SHIFT":
                    modifiers |= 0x0004; break; // MOD_SHIFT
                case "WIN":
                    modifiers |= 0x0008; break; // MOD_WIN
                default:
                    try
                    {
                        key = (uint)KeyInterop.VirtualKeyFromKey(
                            (Key)Enum.Parse(typeof(Key), part, true));
                    }
                    catch
                    {
                        Debug.WriteLine($"[ParseHotkey] Invalid key: {part}");
                    }
                    break;
                }
        }

        Debug.WriteLine($"[ParseHotkey] Parsed => Modifiers: {modifiers}, Key: {key}");
    }
    #endregion
}
