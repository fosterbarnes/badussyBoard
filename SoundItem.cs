using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BadussyBoard
{
    public class SoundItem
    {
        public required string SoundFile { get; set; }
        public required string Hotkey { get; set; }

        //TODO MIDI hotkeys
    }
}
