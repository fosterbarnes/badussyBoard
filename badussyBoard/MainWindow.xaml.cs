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
        private SettingsManager settingsManager;
        public SoundManager Sound;

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

            // Initialize SettingsManager
            string appSettingsFile = System.IO.Path.Combine
            (
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "BadussyBoard",
                "settings.json"
            );
            settingsManager = new SettingsManager(SoundItems, appSettingsFile);
            settingsManager.LoadAppSettings();

            // Initialize SoundManager with the window handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Sound = new SoundManager(hwnd);
            // Register hotkeys for any already-loaded SoundItems
            foreach (var item in SoundItems)
            {
                Sound.RegisterHotkey(item);
            }

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

        /* ------- Event Handlers -------*/
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // If there are unsaved changes, prompt the user
            if (settingsManager.HasUnsavedChanges)
            {
                if (string.IsNullOrWhiteSpace(settingsManager.CurrentSoundItemsJsonPath))
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
                        bool saved = settingsManager.PromptSaveAs();
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
                        bool ok = settingsManager.SaveAllSettings(settingsManager.CurrentSoundItemsJsonPath!);
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
                if (!string.IsNullOrWhiteSpace(settingsManager.CurrentSoundItemsJsonPath))
                { settingsManager.SaveAppSettings(settingsManager.CurrentSoundItemsJsonPath); }
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
            Debug.WriteLine("[MainWindow] (Add Button): Click ↓"); AnimateButton.Press(AddAnimation);
        }
        private void Add_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Add Button): Click ↑"); AnimateButton.Release(AddAnimation);

            PickerWindow picker = new PickerWindow(this);
            picker.Owner = this;
            bool? result = picker.ShowDialog(); // blocks until window is closed
            settingsManager.HasUnsavedChanges = true;
        }

        // Remove
        private void Remove_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Remove Button): Click ↓"); AnimateButton.Press(RemoveAnimation);
        }
        private void Remove_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Remove Button): Click ↑"); AnimateButton.Release(RemoveAnimation);

            var selectedItem = SoundDataGrid.SelectedItem as SoundItem; // Get the currently selected item in the DataGrid
            if (selectedItem != null)
            {
                SoundItems.Remove(selectedItem);
                settingsManager.HasUnsavedChanges = true;
            }
        }

        // Edit
        private void Edit_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Edit Button): Click ↓"); AnimateButton.Press(EditAnimation);
        }
        private void Edit_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Edit Button): Click ↑"); AnimateButton.Release(EditAnimation);

            var selectedItem = SoundDataGrid.SelectedItem as SoundItem;
            if (selectedItem != null)
            {
                var picker = new PickerWindow(this, selectedItem);
                picker.ShowDialog();
                settingsManager.HasUnsavedChanges = true;
            }
            else
            {
                MessageBox.Show("Select a sound/hotkey to edit. You look like Boo Boo the Fool right now :(", "No Sound Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Play
        private void Play_Click_Down(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Play Button): Click ↓"); AnimateButton.Press(PlayAnimation);
        }
        private void Play_Click_Up(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Play Button): Click ↑"); AnimateButton.Release(PlayAnimation);

            if (SoundDataGrid.SelectedItem is SoundItem selectedItem)
            {
                Sound.Play(selectedItem.FilePath); 
            }
        }

        // Stop
        private void Stop_Click_Down(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Stop Button): Click ↓"); AnimateButton.Press(StopAnimation);
        }
        private void Stop_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Stop Button): Click ↑"); AnimateButton.Release(StopAnimation);

            Sound.StopAll();
        }

        // Save
        private void Save_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save Button): Click ↓"); AnimateButton.Press(SaveAnimation);
        }
        private void Save_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save Button): Click ↑"); AnimateButton.Release(SaveAnimation);
            
            if (string.IsNullOrWhiteSpace(settingsManager.CurrentSoundItemsJsonPath))
            {
                // No current file -> behave like Save As
                SaveAs_Click_Up(sender, e);
                return;
            }

            settingsManager.SaveAllSettings(settingsManager.CurrentSoundItemsJsonPath);
        }

        // Save As
        private void SaveAs_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save As Button): Click ↓"); AnimateButton.Press(SaveAsAnimation);
        }
        private void SaveAs_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Save As Button): Click ↑"); AnimateButton.Release(SaveAsAnimation);

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
                bool saved = settingsManager.SaveAllSettings(dlg.FileName);
                if (!saved)
                    MessageBox.Show("Failed to save the soundboard.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Open
        private void Open_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Open Button): Click ↓"); AnimateButton.Press(OpenAnimation);
        }
        private void Open_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Open Button): Click ↑"); AnimateButton.Release(OpenAnimation);
            
            var dlg = new OpenFileDialog
            {
                Title = "Open saved soundboard",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                settingsManager.ReadSoundItemsFromJson(dlg.FileName); 
            }
        }

        // Levels
        private void Levels_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Levels Button): Click ↓"); AnimateButton.Press(LevelsAnimation);
            //TODO Add logic
        }
        private void Levels_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Levels Button): Click ↑"); AnimateButton.Release(LevelsAnimation);
            //TODO Add logic
        }

        // Settings
        private void Settings_Click_Down(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Settings Button): Click ↓"); AnimateButton.Press(SettingsAnimation);
            //TODO Add logic
        }
        private void Settings_Click_Up(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] (Settings Button): Click ↑"); AnimateButton.Release(SettingsAnimation);
            //TODO Add logic
        }
    }
}