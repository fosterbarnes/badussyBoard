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
        
        public PickerWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private void SoundPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Left Click: Sound File Picker");

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
            Debug.WriteLine("Right Click: Sound File Picker");
            selectedFilePath = null;
        }

        private void HotkeyPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Left Click: Hotkey Picker");

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
            Debug.WriteLine("Right Click: Hotkey Picker");
            selectedHotkey = null;
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
                Debug.WriteLine($"Hotkey set: {selectedHotkey}");
            }
        }
        
        private void MIDIPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Left Click: MIDI Picker");
            //TODO Add logic
        }

        private void MIDIPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Right Click: MIDI Picker");
            //TODO Add logic
        }
        
        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Click: Done");

             Debug.WriteLine("Click: Done");

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

            // Add item to MainWindow
            _mainWindow.SoundItems.Add(new SoundItem
            {
                SoundFile = selectedFilePath!,
                Hotkey = selectedHotkey!
            });

            this.Close();
        }
    }
}
