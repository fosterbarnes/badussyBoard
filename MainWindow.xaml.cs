using Microsoft.VisualBasic;
using Microsoft.Win32;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BadussyBoard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<SoundItem> SoundItems { get; set; }
        private List<(WaveOutEvent player, AudioFileReader reader)> currentlyPlaying = new List<(WaveOutEvent, AudioFileReader)>(); //list of currently playing sounds
        private string? currentSoundItemsJsonPath = null;
        private bool hasUnsavedChanges = false;
        private readonly string settingsFile = System.IO.Path.Combine
        (
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BadussyBoard",
            "settings.json"
        );
        
        //Dark mode title bar setup
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);

        public MainWindow()
        {
            InitializeComponent();
            SoundItems = new ObservableCollection<SoundItem>();
            SoundDataGrid.ItemsSource = SoundItems;
            this.SourceInitialized += (s, e) => EnableDarkTitleBar();
            LoadAppSettings();
            this.Closing += Window_Closing;
        }

        private void EnableDarkTitleBar()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            bool useDarkMode = true;

            // Try both attribute IDs since they vary by Windows version
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, Marshal.SizeOf(useDarkMode));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, Marshal.SizeOf(useDarkMode));
        }

        private void AnimateButtonPress(ScaleTransform transform)
        {
            if (transform != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.90,
                    Duration = TimeSpan.FromMilliseconds(50),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void AnimateButtonRelease(ScaleTransform transform)
        {
            if (transform != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        private void WriteSoundItemsToJson(string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var listToSave = SoundItems.ToList(); // Serialize the ObservableCollection to a List for cleaner JSON

            var json = JsonSerializer.Serialize(listToSave, options);
            File.WriteAllText(path, json);
        }

        private void LoadSoundItemsFromJson(string path) 
        {
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show($"Save file not found:\n{path}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<SoundItem>>(json);

                if (loaded == null)
                {
                    MessageBox.Show("Save file was invalid or empty.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SoundItems.Clear();
                foreach (var item in loaded)
                {
                    SoundItems.Add(item);
                }

                currentSoundItemsJsonPath = path;
                hasUnsavedChanges = false;

                Debug.WriteLine($"[MainWindow] Loaded save file: {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load save file:\n{ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAppSettings()
        {
            try
            {
                AppSettings settings = new AppSettings();

                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }

                string? lastUsedPath = settings.LastUsedSoundItemsJson;
                if (!string.IsNullOrWhiteSpace(lastUsedPath) && File.Exists(lastUsedPath))
                {
                    LoadSoundItemsFromJson(lastUsedPath);
                    currentSoundItemsJsonPath = lastUsedPath;
                    hasUnsavedChanges = false;
                }
                else
                {
                    Console.WriteLine("No previous save found.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load recent save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool SaveSoundItemsToFile(string path)
        {
            try
            {
                WriteSoundItemsToJson(path); // Save the sound items themselves
                currentSoundItemsJsonPath = path; // Update the current save path and unsaved changes
                hasUnsavedChanges = false;
                SaveAppSettings(currentSoundItemsJsonPath); // Persist last-used path in settings.json
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool PromptSaveSoundItemsAs()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save soundboard as...",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "BadussyBoard.json"
            };

            bool? result = dlg.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(dlg.FileName))
            {
                return SaveSoundItemsToFile(dlg.FileName); // returns true if save succeeded
            }

            return false; // user cancelled or save failed
        }

        public class AppSettings
        {
            public string? LastUsedSoundItemsJson { get; set; } = null;
        }

        private void SaveAppSettings(string lastUsedPath)
        {
            try
            {
                var settingsDir = System.IO.Path.GetDirectoryName(settingsFile)!;
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);

                var settings = new AppSettings { LastUsedSoundItemsJson = lastUsedPath };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);
            }
            catch
            {
                // silently ignore errors for now
            }
        }


        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // If there are unsaved changes, prompt the user
            if (hasUnsavedChanges)
            {
                if (string.IsNullOrWhiteSpace(currentSoundItemsJsonPath))
                {
                    // Never saved to a file before -> prompt Save As
                    var choice = MessageBox.Show(
                        "You have unsaved changes and haven't saved this project to a file. Save now?",
                        "Save before exit?",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true; // abort close
                        return;
                    }
                    else if (choice == MessageBoxResult.Yes)
                    {
                        bool saved = PromptSaveSoundItemsAs();
                        if (!saved)
                        {
                            e.Cancel = true; // user cancelled save-as or save failed
                            return;
                        }
                    }
                    // No -> continue closing without saving
                }
                else
                {
                    // Already has a current save path -> try to save
                    var choice = MessageBox.Show(
                        "You have unsaved changes. Save before exiting?",
                        "Save changes?",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (choice == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else if (choice == MessageBoxResult.Yes)
                    {
                        bool ok = SaveSoundItemsToFile(currentSoundItemsJsonPath);
                        if (!ok)
                        {
                            e.Cancel = true; // save failed
                            return;
                        }
                    }
                    // No -> continue closing without saving
                }
            }

            // If no unsaved changes or save completed successfully, make sure last-used path is stored
            try
            {
                if (!string.IsNullOrWhiteSpace(currentSoundItemsJsonPath))
                    SaveAppSettings(currentSoundItemsJsonPath);
            }
            catch
            {
                // ignore any settings saving errors on close
            }
        }

        /* ------- Buttons -------*/
        // Add
        private void Add_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Add Button): Click ↓"); AnimateButtonPress(AddAnimation);
        }
        private void Add_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Add Button): Click ↑"); AnimateButtonRelease(AddAnimation);

            PickerWindow picker = new PickerWindow(this);
            picker.Owner = this;
            bool? result = picker.ShowDialog(); // blocks until window is closed
            hasUnsavedChanges = true;
        }

        // Remove
        private void Remove_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Remove Button): Click ↓"); AnimateButtonPress(RemoveAnimation);
        }
        private void Remove_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Remove Button): Click ↑"); AnimateButtonRelease(RemoveAnimation);

            var selectedItem = SoundDataGrid.SelectedItem as SoundItem; // Get the currently selected item in the DataGrid
            if (selectedItem != null)
                SoundItems.Remove(selectedItem);
                hasUnsavedChanges = true;
        }

        // Edit
        private void Edit_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Edit Button): Click ↓"); AnimateButtonPress(EditAnimation);
        }
        private void Edit_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Edit Button): Click ↑"); AnimateButtonRelease(EditAnimation);

            var selectedItem = SoundDataGrid.SelectedItem as SoundItem;
            if (selectedItem != null)
            {
                var picker = new PickerWindow(this, selectedItem);
                picker.ShowDialog();
                hasUnsavedChanges = true;
            } else
            {
                MessageBox.Show("Select a sound/hotkey to edit. You look like Boo Boo the Fool right now :(", "No Sound Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Play
        private void Play_Click_Down(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Play Button): Click ↓"); AnimateButtonPress(PlayAnimation);
        }
        private void Play_Click_Up(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Play Button): Click ↑"); AnimateButtonRelease(PlayAnimation);

            // simple way to play a sound while testing. we will need something more complex to be able to stop or layer sounds
            var selectedItem = SoundDataGrid.SelectedItem as SoundItem;
            if (selectedItem != null)
            {
                try
                {
                    var audioFile = new AudioFileReader(selectedItem.FilePath);
                    var output = new WaveOutEvent();

                    output.Init(audioFile); output.Play();
                    currentlyPlaying.Add((output, audioFile)); //add to list of playing sounds

                    // dispose objects after playback
                    output.PlaybackStopped += (s, args) =>
                    {
                        output.Dispose(); audioFile.Dispose();
                        currentlyPlaying.RemoveAll(x => x.player == output);
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing sound:\n{ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Stop
        private void Stop_Click_Down(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Stop Button): Click ↓"); AnimateButtonPress(StopAnimation);
        }
        private void Stop_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Stop Button): Click ↑"); AnimateButtonRelease(StopAnimation);

            foreach (var (player, reader) in currentlyPlaying)
            {
                player.Stop(); player.Dispose(); reader.Dispose();
            }
            currentlyPlaying.Clear();
        }

        // Save
        private void Save_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save Button): Click ↓"); AnimateButtonPress(SaveAnimation);
        }
        private void Save_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save Button): Click ↑"); AnimateButtonRelease(SaveAnimation);
            
            if (string.IsNullOrWhiteSpace(currentSoundItemsJsonPath))
            {
                // No current file -> behave like Save As
                SaveAs_Click_Up(sender, e);
                return;
            }

            SaveSoundItemsToFile(currentSoundItemsJsonPath);
        }

        // Save As
        private void SaveAs_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save As Button): Click ↓"); AnimateButtonPress(SaveAsAnimation);
        }
        private void SaveAs_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save As Button): Click ↑"); AnimateButtonRelease(SaveAsAnimation);

            var dlg = new SaveFileDialog
            {
                Title = "Save as...",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "BadussyBoard.json"
            };

            bool? result = dlg.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(dlg.FileName))
            {
                bool saved = SaveSoundItemsToFile(dlg.FileName);
                if (!saved)
                {
                    MessageBox.Show("Failed to save the soundboard.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Open
        private void Open_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Open Button): Click ↓"); AnimateButtonPress(OpenAnimation);
        }
        private void Open_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Open Button): Click ↑"); AnimateButtonRelease(OpenAnimation);
            
            var dlg = new OpenFileDialog
            {
                Title = "Open saved soundboard",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                LoadSoundItemsFromJson(dlg.FileName);
            }
        }

        // Levels
        private void Levels_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Levels Button): Click ↓"); AnimateButtonPress(LevelsAnimation);
            //TODO Add logic
        }
        private void Levels_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Levels Button): Click ↑"); AnimateButtonRelease(LevelsAnimation);
            //TODO Add logic
        }

        // Settings
        private void Settings_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Settings Button): Click ↓"); AnimateButtonPress(SettingsAnimation);
            //TODO Add logic
        }
        private void Settings_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Settings Button): Click ↑"); AnimateButtonRelease(SettingsAnimation);
            //TODO Add logic
        }
    }
}