using System;

namespace GbsPlayer
{
    public class GeneralMemory
    {
        private readonly Cartridge _cartridge;
        private readonly Sound _gbSound;

        public byte[] Memory;

        public GeneralMemory(Cartridge cartridge, Sound gbSound)
        {
            _cartridge = cartridge;
            _gbSound = gbSound;
        }

        public void Init()
        {
            Memory = new byte[65536]; // 0x0000-0xFFFF
        }

        public byte this[int index]
        {
            get
            {
                // Cartridge
                if (index < 0x8000)
                    return _cartridge[index];
                if (0xA000 <= index && index < 0xE000)
                {
                    // GBS:
                    // Player authors: you should disregard writes to $4000-$5fff and $ff70, and just implement main RAM from $a000 to $dfff.
                    return Memory[index];
                }
                // shadow ram
                if (0xE000 <= index && index < 0xFE00)
                    return Memory[index - 0x2000];
                // Sound registries
                if (0xFF10 <= index && index <= 0xFF3F)
                    return _gbSound[index];
                // High RAM (HRAM)
                if (0xFF80 <= index && index <= 0xFFFF)
                    return Memory[index];

                Console.WriteLine("Read at 0x{0:X} not supported", index);
                return 0;
            }
            set
            {
                // ROM
                if (index < 0x8000)
                {
                    _cartridge[index] = value;
                }
                // 8KB Video RAM (VRAM)
                else if (index < 0xA000)
                {
                    Console.WriteLine("VRAM not supported 0x{0:X}", index);
                }
                // Unit Working RAM
                else if (index < 0xE000)
                {
                    // GBS:
                    // Player authors: you should disregard writes to $4000-$5fff and $ff70, and just implement main RAM from $a000 to $dfff.
                    Memory[index] = value;
                }
                // shadow memory
                else if (index < 0xFE00)
                {
                    Memory[index - 0x2000] = value;
                }
                else if (index < 0xFF05)
                {
                    Console.WriteLine("Write to 0x{0:X} not supported", index);
                }
                else if (index == 0xFF05)
                {
                    // TIMA Timer Counter
                    Console.WriteLine("TIMA currently not supported");
                }
                else if (index == 0xFF06)
                {
                    // TMA Timer Modulo
                    Console.WriteLine("TMA currently not supported");
                }
                // TAC
                else if (index == 0xFF07)
                {
                    // Bit 1 & 0, counter rate
                    //  00: 4096 Hz
                    //  01: 262144 Hz
                    //  10: 65536 Hz
                    //  11: 16384 Hz

                    // Bit 2, interrupt type
                    //  0: Use v-blank
                    //  1: Use timer

                    // Bit 6 - 3, reserved for expansion
                    //  Set them to 0

                    // Bit 7, CPU clock rate
                    //  0: Use normal rate
                    //  1: Use 2x(fast) rate

                    Console.WriteLine("TAC currently not supported");
                }
                else if (index < 0xFF10)
                {
                    Console.WriteLine("Write to 0x{0:X} not supported", index);
                }
                // Sound stuff
                else if (0xFF10 <= index && index <= 0xFF3F)
                {
                    _gbSound[index] = value;
                }
                else if (index < 0xFF80)
                {
                    Console.WriteLine("Write to 0x{0:X} not supported", index);
                }
                // High RAM (HRAM)
                else if (index < 0xFFFF)
                {
                    Memory[index] = value;
                }
                // Interrupt Enable Register
                else if (index == 0xFFFF)
                {
                    Memory[index] = value;
                }
                // ERROR
                else
                {
                    Console.WriteLine("Error writing to high address");
                }
            }
        }
    }
}
