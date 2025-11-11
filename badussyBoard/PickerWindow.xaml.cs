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

            Debug.WriteLine($"[PickerWindow.PopulateFieldsFromItem] PopulateFieldsFromItem - Initial values:");
            Debug.WriteLine($"[PickerWindow.PopulateFieldsFromItem] FilePath: {item.FilePath ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.PopulateFieldsFromItem] FileName: {item.FileName ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.PopulateFieldsFromItem] Hotkey: {item.Hotkey ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.PopulateFieldsFromItem] MIDIHotkey: {item.MIDIHotkey ?? "<null>"}\n");
        }

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin;
        }
        
        private string NormalizeHotkey(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

            // Split tokens, trim them
            var tokens = raw.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToList();

            // Normalize tokens into canonical token list (modifiers -> key)
            bool hasCtrl = tokens.Any(t => t.IndexOf("CTRL", StringComparison.OrdinalIgnoreCase) >= 0
                                   || t.Equals("CONTROL", StringComparison.OrdinalIgnoreCase));
            bool hasAlt  = tokens.Any(t => t.IndexOf("ALT", StringComparison.OrdinalIgnoreCase) >= 0);
            bool hasShift= tokens.Any(t => t.IndexOf("SHIFT", StringComparison.OrdinalIgnoreCase) >= 0);
            bool hasWin  = tokens.Any(t => t.IndexOf("WIN", StringComparison.OrdinalIgnoreCase) >= 0);

            // Find first non-modifier token (prefer last token if multiple)
            string? mainKey = tokens
            .Where(t => !(t.IndexOf("CTRL", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("ALT", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("SHIFT", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("WIN", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.Equals("CONTROL", StringComparison.OrdinalIgnoreCase)))
            .LastOrDefault();

            var parts = new List<string>();
            if (hasCtrl) parts.Add("Ctrl");
            if (hasAlt)  parts.Add("Alt");
            if (hasShift)parts.Add("Shift");
            if (hasWin)  parts.Add("Win");
            if (!string.IsNullOrWhiteSpace(mainKey))
            parts.Add(mainKey!);

            return string.Join(" + ", parts);
        }

        private void SoundPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.SoundPicker_LeftClick] Left Click: Sound File Picker");

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

                Debug.WriteLine($"[PickerWindow.SoundPicker_LeftClick] Sound file selected:");
                Debug.WriteLine($"[PickerWindow.SoundPicker_LeftClick] FilePath: {selectedFilePath}");
                Debug.WriteLine($"[PickerWindow.SoundPicker_LeftClick] FileName: {System.IO.Path.GetFileName(selectedFilePath)}\n");
            }
        }

        private void SoundPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.SoundPicker_RightClick] Right Click: Sound File Picker");
            selectedFilePath = null;
            SoundPickerText.Text = "Click to set sound file...";
            Debug.WriteLine("[PickerWindow.SoundPicker_RightClick] FilePath cleared -> <null>\n");
        }

        private void HotkeyPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.HotkeyPicker_LeftClick] Left Click: Hotkey Picker");

            //Pick Hotkey
            if (!isCapturingHotkey)
            {
                isCapturingHotkey = true;
                pressedKeys.Clear();
                HotkeyPickerText.Text = "Press your key(s)...";
                HotkeyPicker.Focus(); // capture keyboard input

                Debug.WriteLine("[PickerWindow.HotkeyPicker_LeftClick] Hotkey capture started");
            }
        }

        private void HotkeyPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.HotkeyPicker_RightClick] Right Click: Hotkey Picker");
            selectedHotkey = null;
            HotkeyPickerText.Text = "Click to set hotkey...";
            Debug.WriteLine("[PickerWindow.HotkeyPicker_RightClick] Hotkey cleared -> <null>\n");
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (!isCapturingHotkey)
            return;

            if (e.Key == Key.System || e.Key == Key.ImeProcessed) // Don't include modifier system keys like "System" or "ImeProcessed"
            return;

            pressedKeys.Add(e.Key); // Keep track of pressed keys

            // Build canonical string: modifiers (from Keyboard.Modifiers) then the primary non-modifier key
            var parts = new List<string>();
            var mods = Keyboard.Modifiers;
            if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((mods & ModifierKeys.Alt) != 0)     parts.Add("Alt");
            if ((mods & ModifierKeys.Shift) != 0)   parts.Add("Shift");
            if ((mods & ModifierKeys.Windows) != 0) parts.Add("Win");

            // Find a non-modifier key from pressedKeys
            var mainKey = pressedKeys
                .Where(k => !IsModifierKey(k))
                .Select(k => k.ToString())
                .LastOrDefault();

            if (!string.IsNullOrWhiteSpace(mainKey))
            parts.Add(mainKey);

            selectedHotkey = string.Join(" + ", parts);
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
                Debug.WriteLine($"[PickerWindow.OnPreviewKeyUp] Hotkey set: {selectedHotkey ?? "<null>"}");
                Debug.WriteLine("[PickerWindow.OnPreviewKeyUp] Final Hotkey string: " + (selectedHotkey ?? "<null>")); Debug.WriteLine("");
            }
        }
        
        private void MIDIPicker_LeftClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.MIDIPicker_LeftClick] Left Click: MIDI Picker");
            //TODO Add logic
        }

        private void MIDIPicker_RightClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.MIDIPicker_RightClick] Right Click: MIDI Picker");
            //TODO Add logic
        }
        
        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[PickerWindow.Done_Click] Click: Done");

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

            // Normalize the hotkey before saving or registering
            selectedHotkey = NormalizeHotkey(selectedHotkey);

            SoundItem item;

            if (_soundToEdit != null)
            {
                // Update existing item
                _soundToEdit.FilePath = selectedFilePath!;
                _soundToEdit.Hotkey = selectedHotkey!;
                item = _soundToEdit;

                Debug.WriteLine("[PickerWindow.Done_Click] Updating existing SoundItem with these values:");
            }
            else
            {
            // Create new item
            item = new SoundItem
            {
                FilePath = selectedFilePath!,
                Hotkey = selectedHotkey!
            };

            _mainWindow.SoundItems.Add(item);

            Debug.WriteLine("[PickerWindow.Done_Click] Creating new SoundItem with these values:");
            }

            Debug.WriteLine($"[PickerWindow.Done_Click] FilePath: {item.FilePath ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.Done_Click] FileName: {item.FileName ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.Done_Click] Hotkey: {item.Hotkey ?? "<null>"}");
            Debug.WriteLine($"[PickerWindow.Done_Click] MIDIHotkey: {item.MIDIHotkey ?? "<null>"}\n");

            _mainWindow.SoundDataGrid.Items.Refresh();

            Debug.WriteLine("[PickerWindow.Done_Click] Registering hotkey with SoundManager...");
            _mainWindow.Sound.RegisterHotkey(item);

            this.Close();
        }
    }
}
