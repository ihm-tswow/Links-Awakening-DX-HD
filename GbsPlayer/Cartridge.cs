using System;

namespace GbsPlayer
{
    public class Cartridge
    {
        public byte[] ROM;

        public byte SelectedRomBank = 1;

        // gbs
        public byte Version;
        public byte TrackCount;
        public byte FirstSong;

        public ushort LoadAddress;
        public ushort InitAddress;
        public ushort PlayAddress;

        public ushort StackPointer;

        public byte TimeModulo;
        public byte TimeControl;

        public string Title;
        public string Author;
        public string Copyright;

        public ushort RomOffset;

        public void Init()
        {
            // Reset values
            SelectedRomBank = 1;

            // GBS Header
            Version = ROM[0x03];
            TrackCount = ROM[0x04];
            FirstSong = ROM[0x05];

            LoadAddress = (ushort)(ROM[0x07] << 0x08 | ROM[0x06]);
            InitAddress = (ushort)(ROM[0x09] << 0x08 | ROM[0x08]);
            PlayAddress = (ushort)(ROM[0x0b] << 0x08 | ROM[0x0a]);

            StackPointer = (ushort)(ROM[0x0d] << 0x08 | ROM[0x0c]);

            TimeModulo = ROM[0x0e];
            TimeControl = ROM[0x0f];

            var charArray = new char[32];
            for (var i = 0; i < 32; i++)
                charArray[i] = (char)ROM[0x10 + i];
            Title = new string(charArray);

            for (var i = 0; i < 32; i++)
                charArray[i] = (char)ROM[0x30 + i];
            Author = new string(charArray);

            for (var i = 0; i < 32; i++)
                charArray[i] = (char)ROM[0x50 + i];
            Copyright = new string(charArray);

            RomOffset = (ushort)(LoadAddress - 0x70);
        }

        public byte this[int index]
        {
            get
            {
                // ROM Bank 00
                if (index < 0x4000)
                {
                    if (index - RomOffset >= 0)
                        return ROM[index - RomOffset];
                }
                // ROM Bank 01-..
                else if (index < 0x8000)
                {
                    var romIndex = (SelectedRomBank) * 0x4000 + (index - 0x4000) - RomOffset;
                    if (romIndex < ROM.Length)
                        return ROM[romIndex];
                }

                Console.WriteLine("Cartridge Index unavailable: {0:X}", index);
                return 0;
            }
            set
            {
                // normally enable RAM
                if (index < 0x2000)
                {
                    Console.WriteLine("Write to 0x{0:X} not supported", index);
                }
                // select ROM Bank
                else if (index < 0x4000)
                {
                    // GBS:
                    // A page is selected into Bank 1 by writing the page number as a byte value somewhere in the address range $2000 -$3fff.

                    SelectedRomBank = value;
                    // Console.WriteLine("select ROM bank 0x{0:X}:" + value, index);
                }
                // RAM Bank Number - or - Upper Bits of ROM Bank Number
                else if (index < 0x6000)
                {
                    // GBS:
                    // Player authors: you should disregard writes to $4000-$5fff and $ff70, and just implement main RAM from $a000 to $dfff.
                }
                else
                    Console.WriteLine("Write to 0x{0:X} not supported", index);
            }
        }
    }
}
