using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simulator1
{
    class Instruction
    {
        public uint originalBits { get; set; }

        public uint rd { get; set; }
        public uint rn { get; set; }

        public uint cond { get; set; }
        public uint type { get; set; }

        public bool S { get; set; }

      //  public string commandStr { get; set; }

        public bool N, Z, C, F;


        public Instruction()
        {
            ;
        }
        public virtual void parse(Memory command)
        {
            Logger.Instance.writeLog("CMD: UNDISCOVERED");
        }

        
        public uint rm { get; set; }

        public virtual string run(ref Register[] reg, ref Memory RAM)
        {
            return "CMD: UNDISCOVERERD";
        }


        public ShifterOperand figureOutShift(bool I, ShifterOperand shiftOp, uint RmVal, Register[] reg)
        {
            if (!I)
            {
                //it's a register!
                if (shiftOp.bit4 && !shiftOp.bit7)
                {
                    //shifted by a register!
                    shiftOp.shiftRM(RmVal, reg[shiftOp.Rs].ReadWord(0, true));

                }
                else
                {
                    //shifted by an immediate value!
                    shiftOp.shiftRM(RmVal, shiftOp.shift_imm);
                }
            }
            return shiftOp;
        }



        internal string checkCond(bool[] flagsNZCF)
        {
            bool N = flagsNZCF[0];
            bool Z = flagsNZCF[1];
            bool C = flagsNZCF[2];
            bool F = flagsNZCF[3];
            switch (cond)
            {
                case 0x0:
                    if (Z) { return "EQ"; }
                    break;
                case 0x1:
                    if (!Z) { return "NE"; }
                    break;
                case 0x2:
                    if (C) { return "CS"; }
                    break;
                case 0x3:
                    if (!C) { return "CC"; }
                    break;
                case 0x4:
                    if (N) { return "MI"; }
                    break;
                case 0x5:
                    if (!N) { return "PL"; }
                    break;
                case 0x6:
                    if (F) { return "VS"; }
                    break;
                case 0x7:
                    if (!F) { return "VC"; }
                    break;
                case 0x8:
                    if ((C && !F)) { return "HI"; }
                    break;
                case 0x9:
                    if ((!C && F)) { return "LS"; }
                    break;
                case 0xa:
                    if ((N == F)) { return "GE"; }
                    break;
                case 0xb:
                    if ((N != F)) { return "LT"; }
                    break;
                case 0xc:
                    if ((!Z && N == F)) { return "GT"; }
                    break;
                case 0xd:
                    if ((Z || N != F)) { return "LE"; }
                    break;
                case 0xe:
                    return "";
                    break;
                case 0xf:
                    return "Dont execute";
                    break;
                default:
                    return "Dont execute";
                    break;
            }



            return "Dont execute";
        }
    }


    class dataMovement : Instruction
    {
        public bool R { get; set; }
        public bool P { get; set; }
        public bool U { get; set; }
        public bool B { get; set; }
        public bool W { get; set; }
        public bool L { get; set; }

        public ShifterOperand shiftOp { get; set; }

        public override void parse(Memory command)
        {
            //PUBWL
            bool R = command.TestFlag(0, 25);
            rm = (command.ReadWord(0) & 0x0000000F);
            this.shiftOp = new ShifterOperand(command);


            if (!(command.TestFlag(0, 25) && command.TestFlag(0, 4)))
            {
                this.R = command.TestFlag(0, 25);
                this.P = command.TestFlag(0, 24);
                this.U = command.TestFlag(0, 23);
                this.B = command.TestFlag(0, 22);
                this.W = command.TestFlag(0, 21);
                this.L = command.TestFlag(0, 20);



            }

        }

        public override string run(ref Register[] reg, ref Memory RAM)
        {
            //base.run(ref reg, ref RAM);
            Logger.Instance.writeLog(string.Format("CMD: Data Movement : 0x{0}", Convert.ToString(this.originalBits, 16)));
           
                //from register info to memory!!!
                // --->
                uint RdValue = reg[this.rd].ReadWord(0, true);
                uint RnValue = reg[this.rn].ReadWord(0, true);
                uint RmValue = reg[this.rm].ReadWord(0, true);

                this.shiftOp = loadStoreShift(this.R, this.shiftOp, RmValue, reg);
                //addressing mode
                uint addr = figureOutAddressing(ref reg);
                string cmd = "";
                if (this.L)
                {
                    cmd = "ldr";
                    if (this.B)
                    {
                        byte inpu = RAM.ReadByte(addr);
                        //clear it out first
                        reg[this.rd].WriteWord(0, 0);

                        reg[this.rd].WriteByte(0, inpu);

                    }
                    else
                    {
                        uint inpu = RAM.ReadWord(addr);
                        reg[this.rd].WriteWord(0, inpu);
                    }
                }
                else
                {
                    cmd = "str";
                    if (this.B)
                    {
                        byte inpu = reg[this.rd].ReadByte(0);
                        RAM.WriteByte(addr, inpu);
                    }
                    else
                    {
                        RAM.WriteWord(addr, RdValue);
                    }
                }
                return string.Format("CMD: {0} {1}, 0x{2} : 0x{3} ", cmd, RdValue,
                    Convert.ToString(addr, 16), Convert.ToString(this.originalBits, 16));
            }


        



        public ShifterOperand loadStoreShift(bool R, ShifterOperand shiftOp, uint RmValue, Register[] reg)
        {
            if (R)
            {
                //it's a register
                shiftOp = figureOutShift(!R, shiftOp, RmValue, reg);
            }
            else
            {
                //it's an immediate 12 bit value
                shiftOp.offset = shiftOp.immed_12;
            }

            return shiftOp;
        }



        private uint figureOutAddressing(ref Register[] reg)
        {

            uint RdValue = reg[this.rd].ReadWord(0, true);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            uint addr = 0;
            if (this.P)
            {
                if (this.U)
                {
                    addr = RnValue + this.shiftOp.offset;
                }
                else
                {
                    addr = RnValue - this.shiftOp.offset;
                }

                //offset addressing
                if (this.W)
                {
                    //pre-indexed
                    reg[this.rn].WriteWord(0, addr);
                }

            }
            else
            {
                //post-indexed addressing
                addr = RnValue;
                if (this.U)
                {
                    reg[this.rn].WriteWord(0, RnValue + this.shiftOp.offset);
                }
                else
                {
                    reg[this.rn].WriteWord(0, RnValue - this.shiftOp.offset);
                }
            }
            return addr;
        }


    }


    class dataManipulation : Instruction
    {


        public uint opcode { get; set; }
        public bool I { get; set; }
        public ShifterOperand shiftOp { get; set; }
        public bool bit4 { get; set; }
        public bool bit7 { get; set; }

        public override void parse(Memory command)
        {

            //Get S Byte
            this.I = command.TestFlag(0, 25);
            this.S = command.TestFlag(0, 20);
            this.bit4 = command.TestFlag(0, 4);
            this.bit7 = command.TestFlag(0, 7);
            //dataManipulation dataManinstruct = new dataManipulation();
            this.shiftOp = new ShifterOperand(command);

            if (!(!I && bit4 && bit7))
            {
                //it's data man

                //get OpCode
                uint c = command.ReadWord(0, true);
                this.opcode = (uint)((c & 0x01E00000) >> 21);
                return;

            }
            else
            {
                //it's a multpiply
                this.opcode = 0x1F;

                return;
            }
            


        }


        public override string run(ref Register[] reg, ref Memory RAM)
        {
            Logger.Instance.writeLog(string.Format("CMD: Data Manipulation 0x{0}", Convert.ToString(this.originalBits, 16)));

                switch (this.opcode)
                {
                    case 0:
                        //and
                        return this.and(ref reg, ref RAM);
                        break;
                    case 1: //EOR
                        return this.eor(ref reg, ref RAM);
                        break;
                    case 2: //SUb
                        return this.sub(ref reg, ref RAM);
                        break;
                    case 3: //RSB
                        return this.rsb(ref reg, ref RAM);
                        break;
                    case 4: //ADD
                        return this.add(ref reg, ref RAM);
                        break;
                    case 5: //ADC
                        break;
                    case 6: //SBC
                        break;
                    case 7: //RSC
                        break;
                    case 8: //TST
                        break;
                    case 9: //teq
                        break;
                    case 10: //cmp
                        return this.cmp(ref reg, ref RAM);
                        break;
                    case 11: //cmn
                        break;
                    case 12: //oor
                        return this.oor(ref reg, ref RAM);
                        break;
                    case 13: //mov
                        return this.mov(ref reg, ref RAM);
                        break;
                    case 14: //bic
                        return this.bic(ref reg, ref RAM);
                        break;
                    case 15: //mvn
                        return this.mvn(ref reg, ref RAM);
                        break;
                    case 0x1F:
                        return this.mul(ref reg, ref RAM);
                        break;

                    default:
                        //something bad
                        break;
                }//switch
                return string.Format("Invalid opCode not yet implemented {0}", this.opcode);
            
        }

        private string mul(ref Register[] reg, ref Memory RAM)
        {
            uint RmValue = reg[this.shiftOp.Rm].ReadWord(0, true);
            uint RsValue = reg[this.shiftOp.Rs].ReadWord(0, true);
            uint product = RmValue * RsValue;
            if (product > 0xFFFFFFFF) { 
                Logger.Instance.writeLog("ERR: Multiply to large");
            }
            reg[this.rn].WriteWord(0, product);
            return String.Format("CMD: MUL__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RmValue, RsValue, Convert.ToString(this.originalBits, 16));

        }   

        private string bic(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);

            reg[this.rd].WriteWord(0, (RnValue & (~ this.shiftOp.offset)));

            return (String.Format("CMD: BIC__ R{0},{1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }


        // This can be refactored
        //maybe pass in two values in the order you need them and an operator....
        private string eor(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (RnValue ^ this.shiftOp.offset));
            return (String.Format("CMD: EOR__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }

        private string oor(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (RnValue | this.shiftOp.offset));
            return (String.Format("CMD: OOR__ R{0},{1},{2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }

        private string and(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (RnValue & this.shiftOp.offset));
            return (String.Format("CMD: AND__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }

        private string rsb(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (this.shiftOp.offset - RnValue));
            return (String.Format("CMD: rsb__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }

        private string mvn(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);

            reg[this.rd].WriteWord(0, ~ this.shiftOp.offset);
            return (String.Format("CMD: mvn__ R{0}, {1} : 0x{2}",
                this.rd, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }

        private string cmp(ref Register[] reg, ref Memory RAM)
        {

            S = true;
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnVal = reg[this.rn].ReadWord(0, true);

            uint cmpVal = RnVal - this.shiftOp.offset;

            Memory alu = new Memory(4);
            alu.WriteWord(0, cmpVal);
            //set N flag
            uint biggest32BitUint = 4294967295;
            N = alu.TestFlag(0,31);
            Z = alu.ReadWord(0, true) == 0;

            Logger.Instance.writeLog("****\n\tFix compare C and V flags\n");
            C = (RnVal > biggest32BitUint - this.shiftOp.offset);

            F = false;
            return (String.Format("CMD: cmp__ R{0}, {1} : 0x{2}",
                this.rn, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
   

        }


        public string add(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (RnValue + this.shiftOp.offset));
            return (String.Format("CMD: ADD__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }


        public string sub(ref Register[] reg, ref Memory RAM)
        {
            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);
            uint RnValue = reg[this.rn].ReadWord(0, true);
            reg[this.rd].WriteWord(0, (RnValue - this.shiftOp.offset));
            return (String.Format("CMD: sub__ R{0}, {1}, {2} : 0x{3}",
                this.rd, RnValue, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));

        }

        private string mov(ref Register[] reg, ref Memory RAM)
        {

            this.shiftOp = figureOutShift(this.I, this.shiftOp, reg[this.shiftOp.Rm].ReadWord(0, true), reg);

            reg[this.rd].WriteWord(0, this.shiftOp.offset);
            return (String.Format("CMD: mov__ R{0}, {1} : 0x{2}",
                this.rd, this.shiftOp.offset, Convert.ToString(this.originalBits, 16)));
        }



    }

    class Branch : Instruction
    {
        public bool LN { get; set; }
        //23bit long offset
        public int offset { get; set; }
        bool immediate = true;
        uint regNum;

        public override void parse(Memory command)
        {
            // not immediate
            if ((0x012FFF10 & command.ReadWord(0)) == 0x012FFF10)
            {
                immediate = false;
                this.regNum = (command.ReadWord(0) & 0xF);
            }
            else
            {
                this.LN = command.TestFlag(0, 24);
                this.offset = ((int)command.ReadWord(0, true) & 0x00FFFFFF) << 2;
            }
        }



        public override string run(ref Register[] reg, ref Memory RAM)
        {
            uint newAddress = 0;
            if (immediate)
            {
                uint curAddr = reg[15].ReadWord(0, true);
                if (this.LN)
                {
                    //store a return address
                    reg[14].WriteWord(0, curAddr - 8);
                }
                newAddress = (uint)(curAddr + this.offset);
            }
            else
            {
                //not immediate
                newAddress = (reg[this.regNum].ReadWord(0, true) & 0xFFFFFFFE);
            }
            reg[15].WriteWord(0, newAddress);
            return (string.Format("CMD: B__ #{0} : 0x{1}", newAddress, Convert.ToString(this.originalBits, 16)));

        }


    }//branch class


    class Swap : Instruction
    {

    }

    class MRS : Instruction
    {

    }

    class MSR : Instruction
    {

    }

    class dataMoveMultiple : dataMovement
    {
        public bool[] regFlags { get; set; }


        public override void parse(Memory command)
        {
           // dataMoveMultiple this = (dataMoveMultiple)parseLoadStore(command);
            this.regFlags = new bool[16];
            this.R = command.TestFlag(0, 25); ;
            this.P = command.TestFlag(0, 24);
            this.U = command.TestFlag(0, 23);
            this.B = command.TestFlag(0, 22);
            this.W = command.TestFlag(0, 21);
            this.L = command.TestFlag(0, 20);
            for (byte i = 0; i < 16; ++i)
            {
                this.regFlags[i] = command.TestFlag(0, i);
            }
        }


        public override string run(ref Register[] reg, ref Memory RAM)
        {
            Logger.Instance.writeLog(string.Format("CMD: Data Move Multiple : 0x{0}", Convert.ToString(this.originalBits, 16)));
            
                int RnVal = (int)reg[this.rn].ReadWord(0, true);
                uint numReg = 0;
                string Scom = "";
                string registers = "";
    

                if (this.U)
                {
                    //go up in memory!
                    if (this.P)
                    {
                        //RnVal excluded
                        RnVal += 4;
                    }


                    for (int i = 0; i < 16; ++i)
                    {
                        if (this.regFlags[i])
                        {
                            if (this.L)
                            {
                                reg[i].WriteWord(0, RAM.ReadWord((uint)RnVal));

                                Scom = "ldm";
                            }
                            else
                            {
                                RAM.WriteWord((uint)RnVal, reg[i].ReadWord(0, true));

                                Scom = "stm";
                            }
                            RnVal += 4;
                            registers += string.Format(", r{0}", i);
                            ++numReg;
                        }
                    }



                }
                else
                {
                    //go down in memory
                    if (this.P)
                    {
                        //RnVal excluded
                        RnVal -= 4;
                    }

                    for (int i = 15; i > -1; --i)
                    {
                        if (this.regFlags[i])
                        {
                            if (this.L)
                            {
                                reg[i].WriteWord(0, RAM.ReadWord((uint)RnVal));

                                Scom = "ldm";
                            }
                            else
                            {
                                RAM.WriteWord((uint)RnVal, reg[i].ReadWord(0, true));

                                Scom = "stm";
                            }
                            RnVal -= 4;
                            registers += string.Format(", r{0}", i);
                            ++numReg;
                        }
                    }


                }



                if (this.W)
                {
                    uint n;
                    if (this.U)
                    {
                        n = reg[this.rn].ReadWord(0, true) + (4 * numReg);
                    }
                    else
                    {
                        n = reg[this.rn].ReadWord(0, true) - (4 * numReg);
                    }
                    reg[this.rn].WriteWord(0, n);
                }

                return (string.Format("CMD: {0}__ r{1}{2}", Scom, this.rn, registers));
      

        }//LoadMultStoreMult
    }

    class CoProcessorInstruction : Instruction
    {
        coProcessorOperand operand;
    }

    class Transfer : CoProcessorInstruction
    {

    }

    class Op : CoProcessorInstruction
    {

    }

    class RTransfer : CoProcessorInstruction
    {

    }

    class SWI : CoProcessorInstruction
    {

    }


}
