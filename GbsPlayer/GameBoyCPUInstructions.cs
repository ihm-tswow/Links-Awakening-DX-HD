namespace GbsPlayer
{
    public partial class GameBoyCPU
    {
        // control
        public void NOP() { }
        public void STOP() { }
        public void HALT() { CPUHalt = true; }
        public void DI() { IME = false; }
        public void EI() { IME = true; }

        public void LDH_a8_A() { _memory[0xFF00 + data8] = reg_A; }
        // LD/LDH
        public void LDH_A_a8() { reg_A = _memory[0xFF00 + data8]; }

        public void LD_A_a16() { reg_A = _memory[data16]; }

        public void LD_BC_d16() { reg_BC = data16; }
        public void LD_DE_d16() { reg_DE = data16; }
        public void LD_HL_d16() { reg_HL = data16; }
        public void LD_SP_d16() { reg_SP = data16; }

        public void LD_aHLp_A() { LD_aHL_A(); INC_HL(); }
        public void LD_aHLm_A() { LD_aHL_A(); DEC_HL(); }
        public void LD_aHL_d8() { _memory[reg_HL] = data8; }
        public void LD_aBC_A() { _memory[reg_BC] = reg_A; }
        public void LD_aDE_A() { _memory[reg_DE] = reg_A; }
        public void LD_a16_A() { _memory[data16] = reg_A; }

        public void LD_A_aC() { reg_A = _memory[0xFF00 + reg_C]; }
        public void LD_A_aBC() { reg_A = _memory[reg_BC]; }
        public void LD_A_aDE() { reg_A = _memory[reg_DE]; }
        public void LD_A_aHLp() { LD_A_aHL(); INC_HL(); }
        public void LD_A_aHLm() { LD_A_aHL(); DEC_HL(); }

        public void LD_aC_A() { _memory[0xFF00 + reg_C] = reg_A; }

        public void LD_a16_SP() { _memory[data16] = (byte)(reg_SP & 0xFF); _memory[data16 + 1] = (byte)(reg_SP >> 8); }

        public void LD_HL_SPr8()
        {
            reg_HL = (ushort)(reg_SP + (sbyte)data8); flag_Z = false; flag_N = false;
            flag_H = (((reg_SP ^ (sbyte)data8 ^ ((reg_SP + (sbyte)data8) & 0xFFFF)) & 0x10) == 0x10);
            flag_C = (((reg_SP ^ (sbyte)data8 ^ ((reg_SP + (sbyte)data8) & 0xFFFF)) & 0x100) == 0x100);
        }

        public void LD_SP_HL() { reg_SP = reg_HL; }

        public void LD_A_d8() { reg_A = data8; }
        public void LD_B_d8() { reg_B = data8; }
        public void LD_C_d8() { reg_C = data8; }
        public void LD_D_d8() { reg_D = data8; }
        public void LD_E_d8() { reg_E = data8; }
        public void LD_H_d8() { reg_H = data8; }
        public void LD_L_d8() { reg_L = data8; }

        public void LD_aHL_A() { _memory[reg_HL] = reg_A; }
        public void LD_aHL_B() { _memory[reg_HL] = reg_B; }
        public void LD_aHL_C() { _memory[reg_HL] = reg_C; }
        public void LD_aHL_D() { _memory[reg_HL] = reg_D; }
        public void LD_aHL_E() { _memory[reg_HL] = reg_E; }
        public void LD_aHL_H() { _memory[reg_HL] = reg_H; }
        public void LD_aHL_L() { _memory[reg_HL] = reg_L; }

        public void LD_A_A() { }
        public void LD_A_B() { reg_A = reg_B; }
        public void LD_A_C() { reg_A = reg_C; }
        public void LD_A_D() { reg_A = reg_D; }
        public void LD_A_E() { reg_A = reg_E; }
        public void LD_A_H() { reg_A = reg_H; }
        public void LD_A_L() { reg_A = reg_L; }
        public void LD_A_aHL() { reg_A = _memory[reg_HL]; }

        public void LD_B_A() { reg_B = reg_A; }
        public void LD_B_B() { }
        public void LD_B_C() { reg_B = reg_C; }
        public void LD_B_D() { reg_B = reg_D; }
        public void LD_B_E() { reg_B = reg_E; }
        public void LD_B_H() { reg_B = reg_H; }
        public void LD_B_L() { reg_B = reg_L; }
        public void LD_B_aHL() { reg_B = _memory[reg_HL]; }

        public void LD_C_A() { reg_C = reg_A; }
        public void LD_C_B() { reg_C = reg_B; }
        public void LD_C_C() { }
        public void LD_C_D() { reg_C = reg_D; }
        public void LD_C_E() { reg_C = reg_E; }
        public void LD_C_H() { reg_C = reg_H; }
        public void LD_C_L() { reg_C = reg_L; }
        public void LD_C_aHL() { reg_C = _memory[reg_HL]; }

        public void LD_D_A() { reg_D = reg_A; }
        public void LD_D_B() { reg_D = reg_B; }
        public void LD_D_C() { reg_D = reg_C; }
        public void LD_D_D() { }
        public void LD_D_E() { reg_D = reg_E; }
        public void LD_D_H() { reg_D = reg_H; }
        public void LD_D_L() { reg_D = reg_L; }
        public void LD_D_aHL() { reg_D = _memory[reg_HL]; }

        public void LD_E_A() { reg_E = reg_A; }
        public void LD_E_B() { reg_E = reg_B; }
        public void LD_E_C() { reg_E = reg_C; }
        public void LD_E_D() { reg_E = reg_D; }
        public void LD_E_E() { }
        public void LD_E_H() { reg_E = reg_H; }
        public void LD_E_L() { reg_E = reg_L; }
        public void LD_E_aHL() { reg_E = _memory[reg_HL]; }

        public void LD_H_A() { reg_H = reg_A; }
        public void LD_H_B() { reg_H = reg_B; }
        public void LD_H_C() { reg_H = reg_C; }
        public void LD_H_D() { reg_H = reg_D; }
        public void LD_H_E() { reg_H = reg_E; }
        public void LD_H_H() { }
        public void LD_H_L() { reg_H = reg_L; }
        public void LD_H_aHL() { reg_H = _memory[reg_HL]; }

        public void LD_L_A() { reg_L = reg_A; }
        public void LD_L_B() { reg_L = reg_B; }
        public void LD_L_C() { reg_L = reg_C; }
        public void LD_L_D() { reg_L = reg_D; }
        public void LD_L_E() { reg_L = reg_E; }
        public void LD_L_H() { reg_L = reg_H; }
        public void LD_L_L() { }
        public void LD_L_aHL() { reg_L = _memory[reg_HL]; }

        // INC 8bit
        public void INC_A() { reg_A++; flag_Z = (reg_A == 0x00); flag_N = false; flag_H = ((reg_A & 0x0F) == 0x00); }
        public void INC_B() { reg_B++; flag_Z = (reg_B == 0x00); flag_N = false; flag_H = ((reg_B & 0x0F) == 0x00); }
        public void INC_C() { reg_C++; flag_Z = (reg_C == 0x00); flag_N = false; flag_H = ((reg_C & 0x0F) == 0x00); }
        public void INC_D() { reg_D++; flag_Z = (reg_D == 0x00); flag_N = false; flag_H = ((reg_D & 0x0F) == 0x00); }
        public void INC_E() { reg_E++; flag_Z = (reg_E == 0x00); flag_N = false; flag_H = ((reg_E & 0x0F) == 0x00); }
        public void INC_H() { reg_H++; flag_Z = (reg_H == 0x00); flag_N = false; flag_H = ((reg_H & 0x0F) == 0x00); }
        public void INC_L() { reg_L++; flag_Z = (reg_L == 0x00); flag_N = false; flag_H = ((reg_L & 0x0F) == 0x00); }
        public void INC_aHL() { _memory[reg_HL]++; flag_Z = (_memory[reg_HL] == 0x00); flag_N = false; flag_H = ((_memory[reg_HL] & 0x0F) == 0x00); }

        // INC 16bit
        public void INC_BC() { reg_BC++; }
        public void INC_DE() { reg_DE++; }
        public void INC_HL() { reg_HL++; }
        public void INC_SP() { reg_SP++; }

        // DEC 8bit todo flag h right?
        public void DEC_A() { reg_A--; flag_Z = (reg_A == 0x00); flag_N = true; flag_H = ((reg_A & 0x0F) == 0x0F); }
        public void DEC_B() { reg_B--; flag_Z = (reg_B == 0x00); flag_N = true; flag_H = ((reg_B & 0x0F) == 0x0F); }
        public void DEC_C() { reg_C--; flag_Z = (reg_C == 0x00); flag_N = true; flag_H = ((reg_C & 0x0F) == 0x0F); }
        public void DEC_D() { reg_D--; flag_Z = (reg_D == 0x00); flag_N = true; flag_H = ((reg_D & 0x0F) == 0x0F); }
        public void DEC_E() { reg_E--; flag_Z = (reg_E == 0x00); flag_N = true; flag_H = ((reg_E & 0x0F) == 0x0F); }
        public void DEC_H() { reg_H--; flag_Z = (reg_H == 0x00); flag_N = true; flag_H = ((reg_H & 0x0F) == 0x0F); }
        public void DEC_L() { reg_L--; flag_Z = (reg_L == 0x00); flag_N = true; flag_H = ((reg_L & 0x0F) == 0x0F); }
        public void DEC_aHL() { _memory[reg_HL]--; flag_Z = (_memory[reg_HL] == 0x00); flag_N = true; flag_H = ((_memory[reg_HL] & 0x0F) == 0x0F); }

        // DEC 16bit
        public void DEC_BC() { reg_BC--; }
        public void DEC_DE() { reg_DE--; }
        public void DEC_HL() { reg_HL--; }
        public void DEC_SP() { reg_SP--; }

        // ADD/ADC 8bit
        public void ADD_A_A() { flag_H = ((reg_A & 0x0F) + (reg_A & 0x0F)) > 0x0F; flag_C = (reg_A + reg_A) > 0xFF; reg_A += reg_A; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_B() { flag_H = ((reg_A & 0x0F) + (reg_B & 0x0F)) > 0x0F; flag_C = (reg_A + reg_B) > 0xFF; reg_A += reg_B; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_C() { flag_H = ((reg_A & 0x0F) + (reg_C & 0x0F)) > 0x0F; flag_C = (reg_A + reg_C) > 0xFF; reg_A += reg_C; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_D() { flag_H = ((reg_A & 0x0F) + (reg_D & 0x0F)) > 0x0F; flag_C = (reg_A + reg_D) > 0xFF; reg_A += reg_D; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_E() { flag_H = ((reg_A & 0x0F) + (reg_E & 0x0F)) > 0x0F; flag_C = (reg_A + reg_E) > 0xFF; reg_A += reg_E; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_H() { flag_H = ((reg_A & 0x0F) + (reg_H & 0x0F)) > 0x0F; flag_C = (reg_A + reg_H) > 0xFF; reg_A += reg_H; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_L() { flag_H = ((reg_A & 0x0F) + (reg_L & 0x0F)) > 0x0F; flag_C = (reg_A + reg_L) > 0xFF; reg_A += reg_L; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_d8() { flag_H = ((reg_A & 0x0F) + (data8 & 0x0F)) > 0x0F; flag_C = (reg_A + data8) > 0xFF; reg_A += data8; flag_Z = reg_A == 0x00; flag_N = false; }
        public void ADD_A_aHL()
        {
            flag_H = ((reg_A & 0x0F) + (_memory[reg_HL] & 0x0F)) > 0x0F;
            flag_C = (reg_A + _memory[reg_HL]) > 0xFF; reg_A += _memory[reg_HL]; flag_Z = reg_A == 0x00; flag_N = false;
        }

        public void ADD_SP_r8()
        {
            flag_Z = false; flag_N = false; flag_H = (((reg_SP ^ (sbyte)data8 ^ ((reg_SP + (sbyte)data8) & 0xFFFF)) & 0x10) == 0x10);
            flag_C = (((reg_SP ^ (sbyte)data8 ^ ((reg_SP + (sbyte)data8) & 0xFFFF)) & 0x100) == 0x100); reg_SP = (ushort)(reg_SP + (sbyte)data8);
        }
        
        // ADC todo: make it nicer
        public void ADC_A_A()
        {
            var sum = reg_A + reg_A + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_A & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_B()
        {
            var sum = reg_A + reg_B + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_B & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_C()
        {
            var sum = reg_A + reg_C + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_C & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_D()
        {
            var sum = reg_A + reg_D + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_D & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_E()
        {
            var sum = reg_A + reg_E + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_E & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_H()
        {
            var sum = reg_A + reg_H + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_H & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_L()
        {
            var sum = reg_A + reg_L + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (reg_L & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_d8()
        {
            var sum = reg_A + data8 + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (data8 & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }
        public void ADC_A_aHL()
        {
            var sum = reg_A + _memory[reg_HL] + (flag_C ? 1 : 0);
            flag_H = ((reg_A & 0x0F) + (_memory[reg_HL] & 0x0F) + (flag_C ? 0x01 : 0x00)) > 0x0F; flag_C = sum > 0xFF;
            reg_A = (byte)sum; flag_Z = reg_A == 0x00; flag_N = false;
        }

        // ADD 16bit
        public void ADD_HL_BC() { flag_N = false; flag_H = ((reg_HL & 0x0FFF) + (reg_BC & 0x0FFF)) > 0x0FFF; flag_C = (reg_HL + reg_BC) > 0xFFFF; reg_HL += reg_BC; }
        public void ADD_HL_DE() { flag_N = false; flag_H = ((reg_HL & 0x0FFF) + (reg_DE & 0x0FFF)) > 0x0FFF; flag_C = (reg_HL + reg_DE) > 0xFFFF; reg_HL += reg_DE; }
        public void ADD_HL_HL() { flag_N = false; flag_H = ((reg_HL & 0x0FFF) + (reg_HL & 0x0FFF)) > 0x0FFF; flag_C = (reg_HL + reg_HL) > 0xFFFF; reg_HL += reg_HL; }
        public void ADD_HL_SP() { flag_N = false; flag_H = ((reg_HL & 0x0FFF) + (reg_SP & 0x0FFF)) > 0x0FFF; flag_C = (reg_HL + reg_SP) > 0xFFFF; reg_HL += reg_SP; }

        // SUB 8bit
        public void SUB_A() { flag_H = (reg_A & 0x0F) < (reg_A & 0x0F); flag_C = false; reg_A -= reg_A; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_B() { flag_H = (reg_A & 0x0F) < (reg_B & 0x0F); flag_C = reg_A < reg_B; reg_A -= reg_B; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_C() { flag_H = (reg_A & 0x0F) < (reg_C & 0x0F); flag_C = reg_A < reg_C; reg_A -= reg_C; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_D() { flag_H = (reg_A & 0x0F) < (reg_D & 0x0F); flag_C = reg_A < reg_D; reg_A -= reg_D; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_E() { flag_H = (reg_A & 0x0F) < (reg_E & 0x0F); flag_C = reg_A < reg_E; reg_A -= reg_E; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_H() { flag_H = (reg_A & 0x0F) < (reg_H & 0x0F); flag_C = reg_A < reg_H; reg_A -= reg_H; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_L() { flag_H = (reg_A & 0x0F) < (reg_L & 0x0F); flag_C = reg_A < reg_L; reg_A -= reg_L; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_d8() { flag_H = (reg_A & 0x0F) < (data8 & 0x0F); flag_C = reg_A < data8; reg_A -= data8; flag_Z = reg_A == 0x00; flag_N = true; }
        public void SUB_aHL()
        {
            flag_H = (reg_A & 0x0F) < (_memory[reg_HL] & 0x0F);
            flag_C = reg_A < _memory[reg_HL]; reg_A -= _memory[reg_HL]; flag_Z = reg_A == 0x00; flag_N = true;
        }

        // SBC
        public void SBC_A_A() { var temp = reg_A + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_A & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_B() { var temp = reg_B + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_B & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_C() { var temp = reg_C + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_C & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_D() { var temp = reg_D + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_D & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_E() { var temp = reg_E + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_E & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_H() { var temp = reg_H + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_H & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_L() { var temp = reg_L + flag_CBit; flag_H = (reg_A & 0x0F) < ((reg_L & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_d8() { var temp = data8 + flag_CBit; flag_H = (reg_A & 0x0F) < ((data8 & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A -= (byte)temp; flag_Z = (reg_A == 0x00); flag_N = true; }
        public void SBC_A_aHL()
        {
            var temp = _memory[reg_HL] + flag_CBit;
            flag_H = (reg_A & 0x0F) < ((_memory[reg_HL] & 0x0F) + flag_CBit); flag_C = reg_A < temp; reg_A = (byte)(reg_A - temp); flag_Z = (reg_A == 0x00); flag_N = true;
        }

        // AND 8bit
        public void AND_A() { reg_A &= reg_A; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_B() { reg_A &= reg_B; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_C() { reg_A &= reg_C; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_D() { reg_A &= reg_D; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_E() { reg_A &= reg_E; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_H() { reg_A &= reg_H; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_L() { reg_A &= reg_L; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_d8() { reg_A &= data8; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }
        public void AND_aHL() { reg_A &= _memory[reg_HL]; flag_Z = reg_A == 0x00; flag_N = false; flag_H = true; flag_C = false; }

        // OR 8bit
        public void OR_A() { reg_A |= reg_A; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_B() { reg_A |= reg_B; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_C() { reg_A |= reg_C; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_D() { reg_A |= reg_D; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_E() { reg_A |= reg_E; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_H() { reg_A |= reg_H; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_L() { reg_A |= reg_L; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_d8() { reg_A |= data8; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void OR_aHL() { reg_A |= _memory[reg_HL]; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }

        // XOR 8bit
        public void XOR_A() { reg_A ^= reg_A; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_B() { reg_A ^= reg_B; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_C() { reg_A ^= reg_C; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_D() { reg_A ^= reg_D; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_E() { reg_A ^= reg_E; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_H() { reg_A ^= reg_H; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_L() { reg_A ^= reg_L; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_d8() { reg_A ^= data8; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void XOR_aHL() { reg_A ^= _memory[reg_HL]; flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }

        // CP
        public void CP_A() { flag_Z = true; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_A & 0x0F); flag_C = false; }
        public void CP_B() { flag_Z = reg_A == reg_B; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_B & 0x0F); flag_C = reg_A < reg_B; }
        public void CP_C() { flag_Z = reg_A == reg_C; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_C & 0x0F); flag_C = reg_A < reg_C; }
        public void CP_D() { flag_Z = reg_A == reg_D; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_D & 0x0F); flag_C = reg_A < reg_D; }
        public void CP_E() { flag_Z = reg_A == reg_E; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_E & 0x0F); flag_C = reg_A < reg_E; }
        public void CP_H() { flag_Z = reg_A == reg_H; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_H & 0x0F); flag_C = reg_A < reg_H; }
        public void CP_L() { flag_Z = reg_A == reg_L; flag_N = true; flag_H = (reg_A & 0x0F) < (reg_L & 0x0F); flag_C = reg_A < reg_L; }
        public void CP_d8() { flag_Z = (reg_A == data8); flag_N = true; flag_H = (reg_A & 0x0F) < (data8 & 0x0F); flag_C = reg_A < data8; }
        public void CP_aHL() { flag_Z = reg_A == _memory[reg_HL]; flag_N = true; flag_H = (reg_A & 0x0F) < (_memory[reg_HL] & 0x0F); flag_C = reg_A < _memory[reg_HL]; }

        // DAA
        public void DAA()
        {
            int temp = reg_A;

            // ADD/ADC
            if (!flag_N)
            {
                if (flag_H || ((temp & 0xF) > 9))
                    temp += 0x06;

                if (flag_C || (temp > 0x9F))
                    temp += 0x60;
            }
            // SUB/SBC
            else
            {
                if (flag_H)
                    temp = (temp - 6) & 0xFF;

                if (flag_C)
                    temp -= 0x60;
            }

            flag_H = false;
            flag_Z = (temp & 0xFF) == 0x00;

            if ((temp & 0x100) == 0x100)
                flag_C = true;

            temp &= 0xFF;

            reg_A = (byte)temp;
        }
        public void CPL() { reg_A = (byte)(~reg_A); flag_N = true; flag_H = true; }
        public void SCF() { flag_N = false; flag_H = false; flag_C = true; }
        public void CCF() { flag_N = false; flag_H = false; flag_C = !flag_C; }

        // JP/JR
        public void JP_a16() { reg_PC = data16; }
        public void JP_aHL() { reg_PC = reg_HL; }
        public void JP_NZ_a16() { if (!flag_Z) { JP_a16(); cycleCount += 4; } }
        public void JP_Z_a16() { if (flag_Z) { JP_a16(); cycleCount += 4; } }
        public void JP_NC_a16() { if (!flag_C) { JP_a16(); cycleCount += 4; } }
        public void JP_C_a16() { if (flag_C) { JP_a16(); cycleCount += 4; } }

        public void JR_d8() { reg_PC = (ushort)(reg_PC + (sbyte)data8); }
        public void JR_NZ_a8() { if (!flag_Z) { JR_d8(); cycleCount += 4; } }
        public void JR_Z_a8() { if (flag_Z) { JR_d8(); cycleCount += 4; } }
        public void JR_NC_a8() { if (!flag_C) { JR_d8(); cycleCount += 4; } }
        public void JR_C_a8() { if (flag_C) { JR_d8(); cycleCount += 4; } }

        // POP/PUSH
        public void POP_AF() { reg_F = (byte)(_memory[reg_SP++] & 0xF0); reg_A = _memory[reg_SP++]; }
        public void POP_BC() { reg_C = _memory[reg_SP++]; reg_B = _memory[reg_SP++]; }
        public void POP_DE() { reg_E = _memory[reg_SP++]; reg_D = _memory[reg_SP++]; }
        public void POP_HL() { reg_L = _memory[reg_SP++]; reg_H = _memory[reg_SP++]; }

        public void PUSH_AF() { _memory[--reg_SP] = reg_A; _memory[--reg_SP] = reg_F; }
        public void PUSH_BC() { _memory[--reg_SP] = reg_B; _memory[--reg_SP] = reg_C; }
        public void PUSH_DE() { _memory[--reg_SP] = reg_D; _memory[--reg_SP] = reg_E; }
        public void PUSH_HL() { _memory[--reg_SP] = reg_H; _memory[--reg_SP] = reg_L; }

        // Returns
        public void RET() { reg_PC = (ushort)(_memory[reg_SP++] | (_memory[reg_SP++] << 8)); }
        public void RET_NZ() { if (!flag_Z) { RET(); cycleCount += 12; } }
        public void RET_Z() { if (flag_Z) { RET(); cycleCount += 12; } }
        public void RET_NC() { if (!flag_C) { RET(); cycleCount += 12; } }
        public void RET_C() { if (flag_C) { RET(); cycleCount += 12; } }

        public void RETI() { RET(); EI(); }

        // CALL
        public void CALL_a16() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = data16; }
        public void CALL_NZ_a16() { if (!flag_Z) { CALL_a16(); cycleCount += 12; } }
        public void CALL_Z_a16() { if (flag_Z) { CALL_a16(); cycleCount += 12; } }
        public void CALL_NC_a16() { if (!flag_C) { CALL_a16(); cycleCount += 12; } }
        public void CALL_C_a16() { if (flag_C) { CALL_a16(); cycleCount += 12; } }

        // RST - Restart
        public void RST_00H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0000 + _cartridge.LoadAddress); }
        public void RST_08H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0008 + _cartridge.LoadAddress); }
        public void RST_10H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0010 + _cartridge.LoadAddress); }
        public void RST_18H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0018 + _cartridge.LoadAddress); }
        public void RST_20H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0020 + _cartridge.LoadAddress); }
        public void RST_28H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0028 + _cartridge.LoadAddress); }
        public void RST_30H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0030 + _cartridge.LoadAddress); }
        public void RST_38H() { _memory[--reg_SP] = (byte)(reg_PC >> 8); _memory[--reg_SP] = (byte)(reg_PC & 0xFF); reg_PC = (ushort)(0x0038 + _cartridge.LoadAddress); }

        // RLCA
        public void RLCA() { flag_C = (reg_A & 0x80) == 0x80; reg_A = (byte)((reg_A << 1) | (reg_A >> 7)); flag_Z = false; flag_N = false; flag_H = false; }
        public void RRCA() { flag_C = (reg_A & 0x01) == 0x01; reg_A = (byte)((reg_A >> 1) | ((reg_A & 0x01) << 7)); flag_Z = false; flag_N = false; flag_H = false; }
        public void RLA() { var temp = reg_A; reg_A = (byte)((reg_A << 1) | (flag_C ? 0x01 : 0x00)); flag_Z = false; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RRA() { var temp = reg_A; reg_A = (byte)((reg_A >> 1) | (flag_C ? 0x80 : 0x00)); flag_Z = false; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }

        // CB instructions
        // RLC
        public void RLC_A() { flag_C = (reg_A & 0x80) == 0x80; reg_A = (byte)(reg_A << 1 | (reg_A >> 7)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; }
        public void RLC_B() { flag_C = (reg_B & 0x80) == 0x80; reg_B = (byte)(reg_B << 1 | (reg_B >> 7)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; }
        public void RLC_C() { flag_C = (reg_C & 0x80) == 0x80; reg_C = (byte)(reg_C << 1 | (reg_C >> 7)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; }
        public void RLC_D() { flag_C = (reg_D & 0x80) == 0x80; reg_D = (byte)(reg_D << 1 | (reg_D >> 7)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; }
        public void RLC_E() { flag_C = (reg_E & 0x80) == 0x80; reg_E = (byte)(reg_E << 1 | (reg_E >> 7)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; }
        public void RLC_H() { flag_C = (reg_H & 0x80) == 0x80; reg_H = (byte)(reg_H << 1 | (reg_H >> 7)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; }
        public void RLC_L() { flag_C = (reg_L & 0x80) == 0x80; reg_L = (byte)(reg_L << 1 | (reg_L >> 7)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; }
        public void RLC_aHL()
        {
            flag_C = (_memory[reg_HL] & 0x80) == 0x80;
            _memory[reg_HL] = (byte)(_memory[reg_HL] << 1 | (_memory[reg_HL] >> 7)); flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false;
        }

        // RRC
        public void RRC_A() { flag_C = (reg_A & 0x01) == 0x01; reg_A = (byte)((reg_A >> 1) | ((reg_A & 0x01) << 7)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; }
        public void RRC_B() { flag_C = (reg_B & 0x01) == 0x01; reg_B = (byte)((reg_B >> 1) | ((reg_B & 0x01) << 7)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; }
        public void RRC_C() { flag_C = (reg_C & 0x01) == 0x01; reg_C = (byte)((reg_C >> 1) | ((reg_C & 0x01) << 7)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; }
        public void RRC_D() { flag_C = (reg_D & 0x01) == 0x01; reg_D = (byte)((reg_D >> 1) | ((reg_D & 0x01) << 7)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; }
        public void RRC_E() { flag_C = (reg_E & 0x01) == 0x01; reg_E = (byte)((reg_E >> 1) | ((reg_E & 0x01) << 7)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; }
        public void RRC_H() { flag_C = (reg_H & 0x01) == 0x01; reg_H = (byte)((reg_H >> 1) | ((reg_H & 0x01) << 7)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; }
        public void RRC_L() { flag_C = (reg_L & 0x01) == 0x01; reg_L = (byte)((reg_L >> 1) | ((reg_L & 0x01) << 7)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; }
        public void RRC_aHL()
        {
            flag_C = (_memory[reg_HL] & 0x01) == 0x01;
            _memory[reg_HL] = (byte)((_memory[reg_HL] >> 1) | ((_memory[reg_HL] & 0x01) << 7));
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false;
        }

        // RL
        public void RL_A() { var temp = reg_A; reg_A = (byte)((reg_A << 1) | (flag_C ? 1 : 0)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_B() { var temp = reg_B; reg_B = (byte)((reg_B << 1) | (flag_C ? 1 : 0)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_C() { var temp = reg_C; reg_C = (byte)((reg_C << 1) | (flag_C ? 1 : 0)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_D() { var temp = reg_D; reg_D = (byte)((reg_D << 1) | (flag_C ? 1 : 0)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_E() { var temp = reg_E; reg_E = (byte)((reg_E << 1) | (flag_C ? 1 : 0)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_H() { var temp = reg_H; reg_H = (byte)((reg_H << 1) | (flag_C ? 1 : 0)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_L() { var temp = reg_L; reg_L = (byte)((reg_L << 1) | (flag_C ? 1 : 0)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80; }
        public void RL_aHL()
        {
            var temp = _memory[reg_HL]; _memory[reg_HL] = (byte)((_memory[reg_HL] << 1) | (flag_C ? 1 : 0));
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x80) == 0x80;
        }

        // RR
        public void RR_A() { var temp = reg_A; reg_A = (byte)((reg_A >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_B() { var temp = reg_B; reg_B = (byte)((reg_B >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_C() { var temp = reg_C; reg_C = (byte)((reg_C >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_D() { var temp = reg_D; reg_D = (byte)((reg_D >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_E() { var temp = reg_E; reg_E = (byte)((reg_E >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_H() { var temp = reg_H; reg_H = (byte)((reg_H >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_L() { var temp = reg_L; reg_L = (byte)((reg_L >> 1) | (flag_C ? 0x80 : 0)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01; }
        public void RR_aHL()
        {
            var temp = _memory[reg_HL]; _memory[reg_HL] = (byte)((_memory[reg_HL] >> 1) | (flag_C ? 0x80 : 0));
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false; flag_C = (temp & 0x01) == 0x01;
        }

        // SLA
        public void SLA_A() { flag_C = (reg_A & 0x80) == 0x80; reg_A = (byte)(reg_A << 1); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; }
        public void SLA_B() { flag_C = (reg_B & 0x80) == 0x80; reg_B = (byte)(reg_B << 1); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; }
        public void SLA_C() { flag_C = (reg_C & 0x80) == 0x80; reg_C = (byte)(reg_C << 1); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; }
        public void SLA_D() { flag_C = (reg_D & 0x80) == 0x80; reg_D = (byte)(reg_D << 1); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; }
        public void SLA_E() { flag_C = (reg_E & 0x80) == 0x80; reg_E = (byte)(reg_E << 1); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; }
        public void SLA_H() { flag_C = (reg_H & 0x80) == 0x80; reg_H = (byte)(reg_H << 1); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; }
        public void SLA_L() { flag_C = (reg_L & 0x80) == 0x80; reg_L = (byte)(reg_L << 1); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; }
        public void SLA_aHL()
        {
            flag_C = (_memory[reg_HL] & 0x80) == 0x80; _memory[reg_HL] = (byte)(_memory[reg_HL] << 1);
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false;
        }

        // SRA
        public void SRA_A() { flag_C = (reg_A & 0x01) == 0x01; reg_A = (byte)((reg_A >> 1) | (reg_A & 0x80)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; }
        public void SRA_B() { flag_C = (reg_B & 0x01) == 0x01; reg_B = (byte)((reg_B >> 1) | (reg_B & 0x80)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; }
        public void SRA_C() { flag_C = (reg_C & 0x01) == 0x01; reg_C = (byte)((reg_C >> 1) | (reg_C & 0x80)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; }
        public void SRA_D() { flag_C = (reg_D & 0x01) == 0x01; reg_D = (byte)((reg_D >> 1) | (reg_D & 0x80)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; }
        public void SRA_E() { flag_C = (reg_E & 0x01) == 0x01; reg_E = (byte)((reg_E >> 1) | (reg_E & 0x80)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; }
        public void SRA_H() { flag_C = (reg_H & 0x01) == 0x01; reg_H = (byte)((reg_H >> 1) | (reg_H & 0x80)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; }
        public void SRA_L() { flag_C = (reg_L & 0x01) == 0x01; reg_L = (byte)((reg_L >> 1) | (reg_L & 0x80)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; }
        public void SRA_aHL()
        {
            flag_C = (_memory[reg_HL] & 0x01) == 0x01;
            _memory[reg_HL] = (byte)((_memory[reg_HL] >> 1) | (_memory[reg_HL] & 0x80));
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false;
        }

        // SWAP
        public void SWAP_A() { reg_A = (byte)((reg_A >> 4) | (reg_A << 4)); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_B() { reg_B = (byte)((reg_B >> 4) | (reg_B << 4)); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_C() { reg_C = (byte)((reg_C >> 4) | (reg_C << 4)); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_D() { reg_D = (byte)((reg_D >> 4) | (reg_D << 4)); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_E() { reg_E = (byte)((reg_E >> 4) | (reg_E << 4)); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_H() { reg_H = (byte)((reg_H >> 4) | (reg_H << 4)); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_L() { reg_L = (byte)((reg_L >> 4) | (reg_L << 4)); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; flag_C = false; }
        public void SWAP_aHL()
        {
            _memory[reg_HL] = (byte)((_memory[reg_HL] >> 4) | (_memory[reg_HL] << 4));
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false; flag_C = false;
        }

        // SRL
        public void SRL_A() { flag_C = (reg_A & 0x01) == 0x01; reg_A = (byte)(reg_A >> 1); flag_Z = reg_A == 0x00; flag_N = false; flag_H = false; }
        public void SRL_B() { flag_C = (reg_B & 0x01) == 0x01; reg_B = (byte)(reg_B >> 1); flag_Z = reg_B == 0x00; flag_N = false; flag_H = false; }
        public void SRL_C() { flag_C = (reg_C & 0x01) == 0x01; reg_C = (byte)(reg_C >> 1); flag_Z = reg_C == 0x00; flag_N = false; flag_H = false; }
        public void SRL_D() { flag_C = (reg_D & 0x01) == 0x01; reg_D = (byte)(reg_D >> 1); flag_Z = reg_D == 0x00; flag_N = false; flag_H = false; }
        public void SRL_E() { flag_C = (reg_E & 0x01) == 0x01; reg_E = (byte)(reg_E >> 1); flag_Z = reg_E == 0x00; flag_N = false; flag_H = false; }
        public void SRL_H() { flag_C = (reg_H & 0x01) == 0x01; reg_H = (byte)(reg_H >> 1); flag_Z = reg_H == 0x00; flag_N = false; flag_H = false; }
        public void SRL_L() { flag_C = (reg_L & 0x01) == 0x01; reg_L = (byte)(reg_L >> 1); flag_Z = reg_L == 0x00; flag_N = false; flag_H = false; }
        public void SRL_aHL()
        {
            flag_C = (_memory[reg_HL] & 0x01) == 0x01; _memory[reg_HL] = (byte)(_memory[reg_HL] >> 1);
            flag_Z = _memory[reg_HL] == 0x00; flag_N = false; flag_H = false;
        }

        // RES
        public void RES_0_A() { reg_A &= 0xFE; }
        public void RES_0_B() { reg_B &= 0xFE; }
        public void RES_0_C() { reg_C &= 0xFE; }
        public void RES_0_D() { reg_D &= 0xFE; }
        public void RES_0_E() { reg_E &= 0xFE; }
        public void RES_0_H() { reg_H &= 0xFE; }
        public void RES_0_L() { reg_L &= 0xFE; }
        public void RES_0_aHL() { _memory[reg_HL] &= 0xFE; }

        public void RES_1_A() { reg_A &= 0xFD; }
        public void RES_1_B() { reg_B &= 0xFD; }
        public void RES_1_C() { reg_C &= 0xFD; }
        public void RES_1_D() { reg_D &= 0xFD; }
        public void RES_1_E() { reg_E &= 0xFD; }
        public void RES_1_H() { reg_H &= 0xFD; }
        public void RES_1_L() { reg_L &= 0xFD; }
        public void RES_1_aHL() { _memory[reg_HL] &= 0xFD; }

        public void RES_2_A() { reg_A &= 0xFB; }
        public void RES_2_B() { reg_B &= 0xFB; }
        public void RES_2_C() { reg_C &= 0xFB; }
        public void RES_2_D() { reg_D &= 0xFB; }
        public void RES_2_E() { reg_E &= 0xFB; }
        public void RES_2_H() { reg_H &= 0xFB; }
        public void RES_2_L() { reg_L &= 0xFB; }
        public void RES_2_aHL() { _memory[reg_HL] &= 0xFB; }

        public void RES_3_A() { reg_A &= 0xF7; }
        public void RES_3_B() { reg_B &= 0xF7; }
        public void RES_3_C() { reg_C &= 0xF7; }
        public void RES_3_D() { reg_D &= 0xF7; }
        public void RES_3_E() { reg_E &= 0xF7; }
        public void RES_3_H() { reg_H &= 0xF7; }
        public void RES_3_L() { reg_L &= 0xF7; }
        public void RES_3_aHL() { _memory[reg_HL] &= 0xF7; }

        public void RES_4_A() { reg_A &= 0xEF; }
        public void RES_4_B() { reg_B &= 0xEF; }
        public void RES_4_C() { reg_C &= 0xEF; }
        public void RES_4_D() { reg_D &= 0xEF; }
        public void RES_4_E() { reg_E &= 0xEF; }
        public void RES_4_H() { reg_H &= 0xEF; }
        public void RES_4_L() { reg_L &= 0xEF; }
        public void RES_4_aHL() { _memory[reg_HL] &= 0xEF; }

        public void RES_5_A() { reg_A &= 0xDF; }
        public void RES_5_B() { reg_B &= 0xDF; }
        public void RES_5_C() { reg_C &= 0xDF; }
        public void RES_5_D() { reg_D &= 0xDF; }
        public void RES_5_E() { reg_E &= 0xDF; }
        public void RES_5_H() { reg_H &= 0xDF; }
        public void RES_5_L() { reg_L &= 0xDF; }
        public void RES_5_aHL() { _memory[reg_HL] &= 0xDF; }

        public void RES_6_A() { reg_A &= 0xBF; }
        public void RES_6_B() { reg_B &= 0xBF; }
        public void RES_6_C() { reg_C &= 0xBF; }
        public void RES_6_D() { reg_D &= 0xBF; }
        public void RES_6_E() { reg_E &= 0xBF; }
        public void RES_6_H() { reg_H &= 0xBF; }
        public void RES_6_L() { reg_L &= 0xBF; }
        public void RES_6_aHL() { _memory[reg_HL] &= 0xBF; }

        public void RES_7_A() { reg_A &= 0x7F; }
        public void RES_7_B() { reg_B &= 0x7F; }
        public void RES_7_C() { reg_C &= 0x7F; }
        public void RES_7_D() { reg_D &= 0x7F; }
        public void RES_7_E() { reg_E &= 0x7F; }
        public void RES_7_H() { reg_H &= 0x7F; }
        public void RES_7_L() { reg_L &= 0x7F; }
        public void RES_7_aHL() { _memory[reg_HL] &= 0x7F; }

        // SET
        public void SET_0_A() { reg_A |= 0x01; }
        public void SET_0_B() { reg_B |= 0x01; }
        public void SET_0_C() { reg_C |= 0x01; }
        public void SET_0_D() { reg_D |= 0x01; }
        public void SET_0_E() { reg_E |= 0x01; }
        public void SET_0_H() { reg_H |= 0x01; }
        public void SET_0_L() { reg_L |= 0x01; }
        public void SET_0_aHL() { _memory[reg_HL] |= 0x01; }

        public void SET_1_A() { reg_A |= 0x02; }
        public void SET_1_B() { reg_B |= 0x02; }
        public void SET_1_C() { reg_C |= 0x02; }
        public void SET_1_D() { reg_D |= 0x02; }
        public void SET_1_E() { reg_E |= 0x02; }
        public void SET_1_H() { reg_H |= 0x02; }
        public void SET_1_L() { reg_L |= 0x02; }
        public void SET_1_aHL() { _memory[reg_HL] |= 0x02; }

        public void SET_2_A() { reg_A |= 0x04; }
        public void SET_2_B() { reg_B |= 0x04; }
        public void SET_2_C() { reg_C |= 0x04; }
        public void SET_2_D() { reg_D |= 0x04; }
        public void SET_2_E() { reg_E |= 0x04; }
        public void SET_2_H() { reg_H |= 0x04; }
        public void SET_2_L() { reg_L |= 0x04; }
        public void SET_2_aHL() { _memory[reg_HL] |= 0x04; }

        public void SET_3_A() { reg_A |= 0x08; }
        public void SET_3_B() { reg_B |= 0x08; }
        public void SET_3_C() { reg_C |= 0x08; }
        public void SET_3_D() { reg_D |= 0x08; }
        public void SET_3_E() { reg_E |= 0x08; }
        public void SET_3_H() { reg_H |= 0x08; }
        public void SET_3_L() { reg_L |= 0x08; }
        public void SET_3_aHL() { _memory[reg_HL] |= 0x08; }

        public void SET_4_A() { reg_A |= 0x10; }
        public void SET_4_B() { reg_B |= 0x10; }
        public void SET_4_C() { reg_C |= 0x10; }
        public void SET_4_D() { reg_D |= 0x10; }
        public void SET_4_E() { reg_E |= 0x10; }
        public void SET_4_H() { reg_H |= 0x10; }
        public void SET_4_L() { reg_L |= 0x10; }
        public void SET_4_aHL() { _memory[reg_HL] |= 0x10; }

        public void SET_5_A() { reg_A |= 0x20; }
        public void SET_5_B() { reg_B |= 0x20; }
        public void SET_5_C() { reg_C |= 0x20; }
        public void SET_5_D() { reg_D |= 0x20; }
        public void SET_5_E() { reg_E |= 0x20; }
        public void SET_5_H() { reg_H |= 0x20; }
        public void SET_5_L() { reg_L |= 0x20; }
        public void SET_5_aHL() { _memory[reg_HL] |= 0x20; }

        public void SET_6_A() { reg_A |= 0x40; }
        public void SET_6_B() { reg_B |= 0x40; }
        public void SET_6_C() { reg_C |= 0x40; }
        public void SET_6_D() { reg_D |= 0x40; }
        public void SET_6_E() { reg_E |= 0x40; }
        public void SET_6_H() { reg_H |= 0x40; }
        public void SET_6_L() { reg_L |= 0x40; }
        public void SET_6_aHL() { _memory[reg_HL] |= 0x40; }

        public void SET_7_A() { reg_A |= 0x80; }
        public void SET_7_B() { reg_B |= 0x80; }
        public void SET_7_C() { reg_C |= 0x80; }
        public void SET_7_D() { reg_D |= 0x80; }
        public void SET_7_E() { reg_E |= 0x80; }
        public void SET_7_H() { reg_H |= 0x80; }
        public void SET_7_L() { reg_L |= 0x80; }
        public void SET_7_aHL() { _memory[reg_HL] |= 0x80; }

        // BIT
        public void BIT_0_A() { flag_Z = (reg_A & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_B() { flag_Z = (reg_B & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_C() { flag_Z = (reg_C & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_D() { flag_Z = (reg_D & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_E() { flag_Z = (reg_E & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_H() { flag_Z = (reg_H & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_L() { flag_Z = (reg_L & 0x01) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_0_aHL() { flag_Z = (_memory[reg_HL] & 0x01) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_1_A() { flag_Z = (reg_A & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_B() { flag_Z = (reg_B & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_C() { flag_Z = (reg_C & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_D() { flag_Z = (reg_D & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_E() { flag_Z = (reg_E & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_H() { flag_Z = (reg_H & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_L() { flag_Z = (reg_L & 0x02) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_1_aHL() { flag_Z = (_memory[reg_HL] & 0x02) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_2_A() { flag_Z = (reg_A & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_B() { flag_Z = (reg_B & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_C() { flag_Z = (reg_C & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_D() { flag_Z = (reg_D & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_E() { flag_Z = (reg_E & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_H() { flag_Z = (reg_H & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_L() { flag_Z = (reg_L & 0x04) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_2_aHL() { flag_Z = (_memory[reg_HL] & 0x04) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_3_A() { flag_Z = (reg_A & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_B() { flag_Z = (reg_B & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_C() { flag_Z = (reg_C & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_D() { flag_Z = (reg_D & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_E() { flag_Z = (reg_E & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_H() { flag_Z = (reg_H & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_L() { flag_Z = (reg_L & 0x08) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_3_aHL() { flag_Z = (_memory[reg_HL] & 0x08) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_4_A() { flag_Z = (reg_A & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_B() { flag_Z = (reg_B & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_C() { flag_Z = (reg_C & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_D() { flag_Z = (reg_D & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_E() { flag_Z = (reg_E & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_H() { flag_Z = (reg_H & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_L() { flag_Z = (reg_L & 0x10) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_4_aHL() { flag_Z = (_memory[reg_HL] & 0x10) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_5_A() { flag_Z = (reg_A & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_B() { flag_Z = (reg_B & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_C() { flag_Z = (reg_C & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_D() { flag_Z = (reg_D & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_E() { flag_Z = (reg_E & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_H() { flag_Z = (reg_H & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_L() { flag_Z = (reg_L & 0x20) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_5_aHL() { flag_Z = (_memory[reg_HL] & 0x20) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_6_A() { flag_Z = (reg_A & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_B() { flag_Z = (reg_B & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_C() { flag_Z = (reg_C & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_D() { flag_Z = (reg_D & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_E() { flag_Z = (reg_E & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_H() { flag_Z = (reg_H & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_L() { flag_Z = (reg_L & 0x40) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_6_aHL() { flag_Z = (_memory[reg_HL] & 0x40) == 0x00; flag_N = false; flag_H = true; }

        public void BIT_7_A() { flag_Z = (reg_A & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_B() { flag_Z = (reg_B & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_C() { flag_Z = (reg_C & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_D() { flag_Z = (reg_D & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_E() { flag_Z = (reg_E & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_H() { flag_Z = (reg_H & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_L() { flag_Z = (reg_L & 0x80) == 0x00; flag_N = false; flag_H = true; }
        public void BIT_7_aHL() { flag_Z = (_memory[reg_HL] & 0x80) == 0x00; flag_N = false; flag_H = true; }
    }
}
