using System;

namespace GbsPlayer
{
    public partial class GameBoyCPU
    {
        private readonly GeneralMemory _memory;
        private readonly Cartridge _cartridge;
        private readonly Sound _gbSound;

        // Interrupt Master Enable Flag (write only)
        public bool IME;

        // registers
        public byte reg_A, reg_F;
        public byte reg_B, reg_C;
        public byte reg_D, reg_E;
        public byte reg_H, reg_L;

        public ushort reg_AF { get => (ushort)(reg_A << 8 | reg_F); set { reg_A = (byte)(value >> 8); reg_F = (byte)(value & 0xF0); } }
        public ushort reg_BC { get => (ushort)(reg_B << 8 | reg_C); set { reg_B = (byte)(value >> 8); reg_C = (byte)(value & 0xFF); } }
        public ushort reg_DE { get => (ushort)(reg_D << 8 | reg_E); set { reg_D = (byte)(value >> 8); reg_E = (byte)(value & 0xFF); } }
        public ushort reg_HL { get => (ushort)(reg_H << 8 | reg_L); set { reg_H = (byte)(value >> 8); reg_L = (byte)(value & 0xFF); } }

        // flags (reg_F)
        public bool flag_Z { get => (reg_F & 0x80) == 0x80; set { reg_F = (byte)((reg_F & 0x70) | (value ? 0x80 : 0x00)); } }
        public bool flag_N { get => (reg_F & 0x40) == 0x40; set { reg_F = (byte)((reg_F & 0xB0) | (value ? 0x40 : 0x00)); } }
        public bool flag_H { get => (reg_F & 0x20) == 0x20; set { reg_F = (byte)((reg_F & 0xD0) | (value ? 0x20 : 0x00)); } }
        public bool flag_C { get => (reg_F & 0x10) == 0x10; set { reg_F = (byte)((reg_F & 0xE0) | (value ? 0x10 : 0x00)); } }

        private byte flag_CBit => (byte)(flag_C ? 1 : 0);

        // SP - stack point
        public ushort reg_SP;
        // PC - program counter
        public ushort reg_PC;

        public byte data8 => _memory[reg_PC - 1];
        public ushort data16 => (ushort)((_memory[reg_PC - 1] << 8) | _memory[reg_PC - 2]);

        private bool CPUHalt;

        // list of functions
        public Action[] ops;
        public Action[] op_cb;

        //  4194304Hz / 59.73fps = 70221
        public const float UpdateRate = 59.73f;
        public const int Clockrate = 4194304;
        
        private const float maxPlayCycles60 = Clockrate / UpdateRate; // Clockrate / 59.73;

        public int cycleCount;  // 69905 70224
        public int lastCycleCount;  // 69905 70224

        private const float UpdateStepSize = 10;
        private const int CycleTime = (int)(Clockrate / 1000.0f * UpdateStepSize);

        private float soundCount;
        private float maxSoundCycles = 95.1089f; // 4194304 / 44100

        private float updateCycleCounter;
        private float maxPlayCycles = maxPlayCycles60;

        private int divCounter;
        private int timerCounter;

        private int currentInstruction;

        private double updateCount;

        private double soundDebugCounter;

        private bool _finishedInit;
        private bool _calledPlay;

        // init stack pointer
        public ushort IdleAddress = 0xF00D;

        public bool IsRunning;

        public GameBoyCPU(GeneralMemory memory, Cartridge cartridge, Sound gbSound)
        {
            _memory = memory;
            _cartridge = cartridge;
            _gbSound = gbSound;

            ops = new Action[] {
                NOP, LD_BC_d16, LD_aBC_A, INC_BC, INC_B, DEC_B, LD_B_d8, RLCA, LD_a16_SP, ADD_HL_BC, LD_A_aBC, DEC_BC, INC_C, DEC_C, LD_C_d8, RRCA,
                STOP, LD_DE_d16, LD_aDE_A, INC_DE, INC_D, DEC_D, LD_D_d8, RLA, JR_d8, ADD_HL_DE, LD_A_aDE, DEC_DE, INC_E, DEC_E, LD_E_d8, RRA,
                JR_NZ_a8, LD_HL_d16, LD_aHLp_A, INC_HL, INC_H, DEC_H, LD_H_d8, DAA, JR_Z_a8, ADD_HL_HL, LD_A_aHLp, DEC_HL, INC_L, DEC_L, LD_L_d8, CPL,
                JR_NC_a8, LD_SP_d16, LD_aHLm_A, INC_SP, INC_aHL, DEC_aHL, LD_aHL_d8, SCF, JR_C_a8, ADD_HL_SP, LD_A_aHLm, DEC_SP, INC_A, DEC_A, LD_A_d8, CCF,
                LD_B_B, LD_B_C, LD_B_D, LD_B_E, LD_B_H, LD_B_L, LD_B_aHL, LD_B_A, LD_C_B, LD_C_C, LD_C_D, LD_C_E, LD_C_H, LD_C_L, LD_C_aHL, LD_C_A,
                LD_D_B, LD_D_C, LD_D_D, LD_D_E, LD_D_H, LD_D_L, LD_D_aHL, LD_D_A, LD_E_B, LD_E_C, LD_E_D, LD_E_E, LD_E_H, LD_E_L, LD_E_aHL, LD_E_A,
                LD_H_B, LD_H_C, LD_H_D, LD_H_E, LD_H_H, LD_H_L, LD_H_aHL, LD_H_A, LD_L_B, LD_L_C, LD_L_D, LD_L_E, LD_L_H, LD_L_L, LD_L_aHL, LD_L_A,
                LD_aHL_B, LD_aHL_C, LD_aHL_D, LD_aHL_E, LD_aHL_H, LD_aHL_L, HALT, LD_aHL_A, LD_A_B, LD_A_C, LD_A_D, LD_A_E, LD_A_H, LD_A_L, LD_A_aHL, LD_A_A,
                ADD_A_B, ADD_A_C, ADD_A_D, ADD_A_E, ADD_A_H, ADD_A_L, ADD_A_aHL, ADD_A_A, ADC_A_B, ADC_A_C, ADC_A_D, ADC_A_E, ADC_A_H, ADC_A_L, ADC_A_aHL, ADC_A_A,
                SUB_B, SUB_C, SUB_D, SUB_E, SUB_H, SUB_L, SUB_aHL, SUB_A, SBC_A_B, SBC_A_C, SBC_A_D, SBC_A_E, SBC_A_H, SBC_A_L, SBC_A_aHL, SBC_A_A,
                AND_B, AND_C, AND_D, AND_E, AND_H, AND_L, AND_aHL, AND_A, XOR_B, XOR_C, XOR_D, XOR_E, XOR_H, XOR_L, XOR_aHL, XOR_A,
                OR_B, OR_C, OR_D, OR_E, OR_H, OR_L, OR_aHL, OR_A, CP_B, CP_C, CP_D, CP_E, CP_H, CP_L, CP_aHL, CP_A,
                RET_NZ, POP_BC, JP_NZ_a16, JP_a16, CALL_NZ_a16, PUSH_BC, ADD_A_d8, RST_00H, RET_Z, RET, JP_Z_a16, null, CALL_Z_a16, CALL_a16, ADC_A_d8, RST_08H,
                RET_NC, POP_DE, JP_NC_a16, null, CALL_NC_a16, PUSH_DE, SUB_d8, RST_10H, RET_C, RETI, JP_C_a16, null, CALL_C_a16, null, SBC_A_d8, RST_18H,
                LDH_a8_A, POP_HL, LD_aC_A, null, null, PUSH_HL, AND_d8, RST_20H, ADD_SP_r8, JP_aHL, LD_a16_A, null, null, null, XOR_d8, RST_28H,
                LDH_A_a8, POP_AF, LD_A_aC, DI, null, PUSH_AF, OR_d8, RST_30H, LD_HL_SPr8, LD_SP_HL, LD_A_a16, EI, null, null, CP_d8, RST_38H};

            op_cb = new Action[] {
                RLC_B, RLC_C, RLC_D, RLC_E, RLC_H, RLC_L, RLC_aHL, RLC_A, RRC_B, RRC_C, RRC_D, RRC_E, RRC_H, RRC_L, RRC_aHL, RRC_A,
                RL_B, RL_C, RL_D, RL_E, RL_H, RL_L, RL_aHL, RL_A, RR_B, RR_C, RR_D, RR_E, RR_H, RR_L, RR_aHL, RR_A ,
                SLA_B, SLA_C, SLA_D, SLA_E, SLA_H, SLA_L, SLA_aHL, SLA_A, SRA_B, SRA_C, SRA_D, SRA_E, SRA_H, SRA_L, SRA_aHL, SRA_A,
                SWAP_B, SWAP_C, SWAP_D, SWAP_E, SWAP_H, SWAP_L, SWAP_aHL, SWAP_A, SRL_B, SRL_C, SRL_D, SRL_E, SRL_H, SRL_L, SRL_aHL, SRL_A,
                BIT_0_B, BIT_0_C, BIT_0_D, BIT_0_E, BIT_0_H, BIT_0_L, BIT_0_aHL, BIT_0_A, BIT_1_B, BIT_1_C, BIT_1_D, BIT_1_E, BIT_1_H, BIT_1_L, BIT_1_aHL, BIT_1_A,
                BIT_2_B, BIT_2_C, BIT_2_D, BIT_2_E, BIT_2_H, BIT_2_L, BIT_2_aHL, BIT_2_A, BIT_3_B, BIT_3_C, BIT_3_D, BIT_3_E, BIT_3_H, BIT_3_L, BIT_3_aHL, BIT_3_A,
                BIT_4_B, BIT_4_C, BIT_4_D, BIT_4_E, BIT_4_H, BIT_4_L, BIT_4_aHL, BIT_4_A, BIT_5_B, BIT_5_C, BIT_5_D, BIT_5_E, BIT_5_H, BIT_5_L, BIT_5_aHL, BIT_5_A,
                BIT_6_B, BIT_6_C, BIT_6_D, BIT_6_E, BIT_6_H, BIT_6_L, BIT_6_aHL, BIT_6_A, BIT_7_B, BIT_7_C, BIT_7_D, BIT_7_E, BIT_7_H, BIT_7_L, BIT_7_aHL, BIT_7_A,
                RES_0_B, RES_0_C, RES_0_D, RES_0_E, RES_0_H, RES_0_L, RES_0_aHL, RES_0_A, RES_1_B, RES_1_C, RES_1_D, RES_1_E, RES_1_H, RES_1_L, RES_1_aHL, RES_1_A,
                RES_2_B, RES_2_C, RES_2_D, RES_2_E, RES_2_H, RES_2_L, RES_2_aHL, RES_2_A, RES_3_B, RES_3_C, RES_3_D, RES_3_E, RES_3_H, RES_3_L, RES_3_aHL, RES_3_A,
                RES_4_B, RES_4_C, RES_4_D, RES_4_E, RES_4_H, RES_4_L, RES_4_aHL, RES_4_A, RES_5_B, RES_5_C, RES_5_D, RES_5_E, RES_5_H, RES_5_L, RES_5_aHL, RES_5_A,
                RES_6_B, RES_6_C, RES_6_D, RES_6_E, RES_6_H, RES_6_L, RES_6_aHL, RES_6_A, RES_7_B, RES_7_C, RES_7_D, RES_7_E, RES_7_H, RES_7_L, RES_7_aHL, RES_7_A,
                SET_0_B, SET_0_C, SET_0_D, SET_0_E, SET_0_H, SET_0_L, SET_0_aHL, SET_0_A, SET_1_B, SET_1_C, SET_1_D, SET_1_E, SET_1_H, SET_1_L, SET_1_aHL, SET_1_A,
                SET_2_B, SET_2_C, SET_2_D, SET_2_E, SET_2_H, SET_2_L, SET_2_aHL, SET_2_A, SET_3_B, SET_3_C, SET_3_D, SET_3_E, SET_3_H, SET_3_L, SET_3_aHL, SET_3_A,
                SET_4_B, SET_4_C, SET_4_D, SET_4_E, SET_4_H, SET_4_L, SET_4_aHL, SET_4_A, SET_5_B, SET_5_C, SET_5_D, SET_5_E, SET_5_H, SET_5_L, SET_5_aHL, SET_5_A,
                SET_6_B, SET_6_C, SET_6_D, SET_6_E, SET_6_H, SET_6_L, SET_6_aHL, SET_6_A, SET_7_B, SET_7_C, SET_7_D, SET_7_E, SET_7_H, SET_7_L, SET_7_aHL, SET_7_A };
        }

        public void Init()
        {
            _memory.Init();
            _gbSound.Init();

            updateCount = 0;
            cycleCount = 0;
            lastCycleCount = 0;

            soundCount = 0;
            updateCycleCounter = 0;

            CPUHalt = false;

            _finishedInit = false;
            _calledPlay = false;
        }
        
        public void Update()
        {
            if (!IsRunning)
                return;

            // create a 5 buffer puffer
            while (!_gbSound.WasStopped && _gbSound._soundOutput.GetPendingBufferCount() < 10)
            {
                while (cycleCount < CycleTime)
                    CPUCycle();

                cycleCount -= CycleTime;
            }
            
            // start playing music
            if (_calledPlay && !_gbSound.IsPlaying())
                _gbSound.Play();
        }

        private void CPUCycle()
        {
            lastCycleCount = cycleCount;

            // execute next instruction
            if (!CPUHalt)
                ExecuteInstruction();
            else
                cycleCount += 4;

            if (_finishedInit)
                updateCycleCounter += (cycleCount - lastCycleCount);

            if (_calledPlay && !_gbSound.WasStopped)
            {
                soundCount += (cycleCount - lastCycleCount);

                while (soundCount >= maxSoundCycles)
                {
                    soundCount -= maxSoundCycles;
                    _gbSound.UpdateBuffer();
                }
            }
        }

        private void ExecuteInstruction()
        {
            // gbs: finished init or play function?
            if (reg_PC == IdleAddress)
            {
                _calledPlay = _finishedInit;
                _finishedInit = true;

                if (updateCycleCounter >= maxPlayCycles)
                {
                    updateCycleCounter -= maxPlayCycles;

                    reg_PC = _cartridge.PlayAddress;

                    if (reg_SP != _cartridge.StackPointer)
                        Console.WriteLine("StackPointer error");

                    // push the idleAddress on the stack
                    _memory[--reg_SP] = (byte)(IdleAddress >> 0x8);
                    _memory[--reg_SP] = (byte)(IdleAddress & 0xFF);
                }
                else
                {
                    // skip cycles until the next relevant event
                    var instructionDiff = CycleTime - cycleCount;
                    var updateDiff = maxPlayCycles - updateCycleCounter;

                    var minDiff = Math.Min(instructionDiff, updateDiff);

                    cycleCount += (int)(maxSoundCycles * (int)(minDiff / maxSoundCycles));
                    soundCount -= maxSoundCycles * (int)(minDiff / maxSoundCycles);

                    while (minDiff >= maxSoundCycles && !_gbSound.WasStopped)
                    {
                        minDiff -= maxSoundCycles;
                        _gbSound.UpdateBuffer();
                    }

                    cycleCount += (int)minDiff + 1;
                }

                return;
            }

            currentInstruction = reg_PC;

            // update cycle count
            cycleCount += OpCycle.CycleArray[_memory[reg_PC]];
            reg_PC += OpLength.LengthTable[_memory[reg_PC]];

            // execute
            if (ops[_memory[currentInstruction]] != null)
            {
                ops[_memory[currentInstruction]]();
            }
            // cb instruction
            else if (_memory[currentInstruction] == 0xCB)
            {
                cycleCount += OpCycle.CycleCbArray[_memory[reg_PC]];
                op_cb[_memory[reg_PC]]();
                reg_PC++;
            }
            else
            {
                reg_PC++;
            }
        }

        public void SkipBootROM()
        {
            reg_AF = 0x01B0;
            reg_BC = 0x0013;
            reg_DE = 0x00D8;
            reg_HL = 0x014D;

            reg_PC = 0x0100;
            reg_SP = 0xFFFE;

            _memory[0xFF06] = 0x00;
            _memory[0xFF07] = 0x00;
            _memory[0xFF10] = 0x80;
            _memory[0xFF11] = 0xBF;
            _memory[0xFF12] = 0xF3;
            _memory[0xFF14] = 0xBF;
            _memory[0xFF16] = 0x3F;
            _memory[0xFF17] = 0x00;
            _memory[0xFF19] = 0xBF;
            _memory[0xFF1A] = 0x7F;
            _memory[0xFF1B] = 0xFF;
            _memory[0xFF1C] = 0x9F;
            _memory[0xFF1E] = 0xBF;
            _memory[0xFF20] = 0xFF;
            _memory[0xFF21] = 0x00;
            _memory[0xFF22] = 0x00;
            _memory[0xFF23] = 0xBF;
            _memory[0xFF24] = 0x77;
            _memory[0xFF25] = 0xF3;
            _memory[0xFF26] = 0xF0;
            //_memory[0xFF40] = 0x91;
            //_memory[0xFF42] = 0x00;
            //_memory[0xFF43] = 0x00;
            //_memory[0xFF45] = 0x00;
            //_memory[0xFF47] = 0xFC;
            //_memory[0xFF48] = 0xFF;
            //_memory[0xFF49] = 0xFF;
            //_memory[0xFF4A] = 0x00;
            //_memory[0xFF4B] = 0x00;
            _memory[0xFFFF] = 0x00;
        }

        public void SetPlaybackSpeed(float multiplier)
        {
            maxPlayCycles = maxPlayCycles60 / multiplier;
        }
    }
}
