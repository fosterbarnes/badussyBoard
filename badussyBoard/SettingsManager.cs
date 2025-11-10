using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace BadussyBoard
{
    public class SettingsManager
    {
        public string AppSettingsFile { get; set; }
        public ObservableCollection<SoundItem> SoundItems { get; set; }
        public string? CurrentSoundItemsJsonPath { get; set; }
        public bool HasUnsavedChanges { get; set; }

        public SettingsManager(ObservableCollection<SoundItem> soundItems, string appSettingsFile)
        {
            SoundItems = soundItems;
            AppSettingsFile = appSettingsFile;
        }

        public void ReadSoundItemsFromJson(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show($"Soundboard save file not found:\n{path}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<SoundItem>>(json);

                if (loaded == null)
                {
                    MessageBox.Show("Soundboard save file was invalid or empty.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SoundItems.Clear();
                foreach (var item in loaded)
                    SoundItems.Add(item);

                CurrentSoundItemsJsonPath = path;
                HasUnsavedChanges = false;

                Debug.WriteLine($"[SettingsManager] Loaded soundboard save file: {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load soundboard save file:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SaveAppSettings(string lastUsedPath)
        {
            try
            {
                var settingsDir = Path.GetDirectoryName(AppSettingsFile)!;
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);

                var settings = new AppSettings { LastUsedSoundItemsJson = lastUsedPath };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppSettingsFile, json);
            }
            catch
            {
                // silently ignore errors for now
            }
        }

        public void LoadAppSettings()
        {
            try
            {
                AppSettings settings = new AppSettings();

                if (File.Exists(AppSettingsFile))
                {
                    string json = File.ReadAllText(AppSettingsFile);
                    settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }

                string? lastUsedPath = settings.LastUsedSoundItemsJson;
                if (!string.IsNullOrWhiteSpace(lastUsedPath) && File.Exists(lastUsedPath))
                {
                    ReadSoundItemsFromJson(lastUsedPath);
                    CurrentSoundItemsJsonPath = lastUsedPath;
                    HasUnsavedChanges = false;
                }
                else
                {
                    Console.WriteLine("No previous save found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool SaveAllSettings(string path)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(SoundItems.ToList(), options);
                File.WriteAllText(path, json);

                CurrentSoundItemsJsonPath = path;
                HasUnsavedChanges = false;
                SaveAppSettings(path);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save all settings:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool PromptSaveAs()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save soundboard as...",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "BadussyBoard.json"
            };

            bool? result = dlg.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(dlg.FileName))
                return SaveAllSettings(dlg.FileName);

            return false;
        }

        public class AppSettings
        {
            public string? LastUsedSoundItemsJson { get; set; } = null;
        }
    }
}
