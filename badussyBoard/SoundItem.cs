using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadussyBoard
{
    public class SoundItem
    {
        public required string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string ?Hotkey { get; set; }
        public string ?MIDIHotkey { get; set; }
    }
}
