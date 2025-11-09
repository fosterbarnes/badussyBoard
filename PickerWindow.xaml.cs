using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Interop;


namespace BadussyBoard
{
    /// <summary>
    /// Interaction logic for PickerWindow.xaml
    /// </summary>
    public partial class PickerWindow : Window
    {
        private MainWindow _mainWindow;
        private string? selectedFilePath;
        private string? selectedHotkey;
        private bool isCapturingHotkey = false;
        private HashSet<Key> pressedKeys = new HashSet<Key>();
        private SoundItem? _soundToEdit;

        public PickerWindow(MainWindow mainWindow) : this(mainWindow, null) { } //this is here so calling "new PickerWindow(this)" still works

        //Dark mode title bar setup
        //TODO Don't repeat logic from MainWindow
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref bool attrValue, int attrSize);

        public PickerWindow(MainWindow mainWindow, SoundItem? soundToEdit) //main constructor
        {
            InitializeComponent();
            _mainWindow = mainWindow;
             this.SourceInitialized += (s, e) => EnableDarkTitleBar();
            if (soundToEdit != null)
            {
                _soundToEdit = soundToEdit;
                PopulateFieldsFromItem(soundToEdit);
            }
        }

        private void EnableDarkTitleBar()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            bool useDarkMode = true;

            // Try both attribute IDs since they vary by Windows version
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, Marshal.SizeOf(useDarkMode));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, Marshal.SizeOf(useDarkMode));
        }
        
        private void PopulateFieldsFromItem(SoundItem item)
        {
            selectedFilePath = item.FilePath;
            selectedHotkey = item.Hotkey;
            //TODO MIDI hotkey

            SoundPickerText.Text = string.IsNullOrWhiteSpace(selectedFilePath) ? "Click to set sound file..." : selectedFilePath;
            HotkeyPickerText.Text = string.IsNullOrWhiteSpace(selectedHotkey) ? "Click to set hotkey..." : selectedHotkey;
            //TODO MIDI hotkey
        }

        private void SoundPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Left Click: Sound File Picker");

            //Pick Sound File
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Sound Files (*.mp3;*.wav;*.ogg)|*.mp3;*.wav;*.ogg|All Files (*.*)|*.*",
                Title = "Select a sound file"
            };
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                selectedFilePath = openFileDialog.FileName;
                SoundPickerText.Text = selectedFilePath;
            }
        }

        private void SoundPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Right Click: Sound File Picker");
            selectedFilePath = null;
            SoundPickerText.Text = "Click to set sound file...";
        }

        private void HotkeyPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Left Click: Hotkey Picker");

            //Pick Hotkey
            if (!isCapturingHotkey)
            {
                isCapturingHotkey = true;
                pressedKeys.Clear();
                HotkeyPickerText.Text = "Press your key(s)...";
                HotkeyPicker.Focus(); // capture keyboard input
            }
        }

        private void HotkeyPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Right Click: Hotkey Picker");
            selectedHotkey = null;
            HotkeyPickerText.Text = "Click to set hotkey...";
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!isCapturingHotkey)
                return;

            // Don't include modifier system keys like "System" or "ImeProcessed"
            if (e.Key == Key.System || e.Key == Key.ImeProcessed)
                return;

            pressedKeys.Add(e.Key);

            // Format the pressed keys
            selectedHotkey = string.Join(" + ", pressedKeys.Select(k => k.ToString()));
            HotkeyPickerText.Text = selectedHotkey;
        }
        
        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (!isCapturingHotkey)
                return;

            // If user releases all keys, stop capturing
            if (Keyboard.Modifiers == ModifierKeys.None && pressedKeys.Count > 0)
            {
                isCapturingHotkey = false;
                Debug.WriteLine($"[PickerWindow] Hotkey set: {selectedHotkey}");
            }
        }
        
        private void MIDIPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Left Click: MIDI Picker");
            //TODO Add logic
        }

        private void MIDIPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Right Click: MIDI Picker");
            //TODO Add logic
        }
        
        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow] Click: Done");

            if (string.IsNullOrWhiteSpace(selectedFilePath))
            {
                MessageBox.Show("Please select a sound file.", "Missing Sound File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedHotkey))
            {
                MessageBox.Show("Please set a hotkey.", "Missing Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_soundToEdit != null)
            {
                // update the existing item
                _soundToEdit.FilePath = selectedFilePath!;
                _soundToEdit.Hotkey = selectedHotkey!;
                //TODO MIDI hotkey

                //refresh the DataGrid
                _mainWindow.SoundDataGrid.Items.Refresh();
            }
            else
            {
                // Add item to MainWindow as new sound
                _mainWindow.SoundItems.Add(new SoundItem
                {
                    FilePath = selectedFilePath!,
                    Hotkey = selectedHotkey!
                    //TODO MIDI Hotkey
                });
            }
            

            this.Close();
        }
    }
}
