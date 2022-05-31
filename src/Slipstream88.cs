
using System.Collections.Generic;
using System.Text;

struct Slipstream88 : IModification
{
    static readonly HashSet<string> excludeModules = new HashSet<string>(new [] {
        "MACZINVB1",
        "ZTLATCH1",
        "ZBUF1",
        "SR"
    });
    static readonly HashSet<string> bidirectionalDriver = new HashSet<string>(new [] {
        "MACZINVB1",
        "ZTLATCH1",
        "ZBUF1",
        "BTS4A", 
        "BTS4B", 
        "BTS4C",
        "BTS5A",
        "BTS5B"
    });

    public HashSet<string> ExcludeModules => excludeModules;
    public HashSet<string> BidirectionalDrivers => bidirectionalDriver;

    public string[] PreInputs(string moduleName)
    {
        // Inject MasterClocks on all modules except TMUX1&2
        if (moduleName != "TMUX2" && moduleName != "TMUX1")
        {
            return new[] { "    input    MasterClock," };
        }
        return new string[] { };
    }

    public string[] PostOutputs(string moduleName)
    {
        // Inject BLANKING output to allow direct use
        if (moduleName == "VID")
        {
            return new[] { "    ,output    BLANKING" };
        }
        // Inject DAC outputs to allow direct capture
        if (moduleName == "PWM")
        {
            return new[] { "    ,output    [13:0] DAC" };
        }
        if (moduleName == "DSP")
        {
            return new[] {
                "    ,output    [13:0] LEFTDAC",
                "    ,output    [13:0] RIGHTDAC"
            };
        }
        return new string[] { };
    }

    public bool DropWire(string moduleName, string wireName)
    {
        // Remove blanking wire, since its injected in outputs now
        return moduleName == "VID" && wireName == "BLANKING";
    }

    public void ModulePreInputs(string moduleName, Module.Code c, ref StringBuilder b)
    {
        if (moduleName!="TMUX2" && moduleName!="TMUX1")
        {
            b.Append(".MasterClock(MasterClock),");
        }
        if (moduleName=="PWM")
        {
            // Inject dac fetch
            if (c.instanceName.Value=="PWMLEFT_")
            {
                b.Append(".DAC(LEFTDAC),");
            }
            else
            {
                b.Append(".DAC(RIGHTDAC),");
            }
        }
    }

    public void RegisterFunctions(Module module)
    {
        module.RegisterFunction("assign @O0 = @I0;", 1, 1, "B3A");                                 // B3A is non inverting power buffer
        module.RegisterFunction("assign @O0 = ~@I0;", 1, 1, "B1A", "N1A", "N1B", "N1C", "N1D");    // B1A is an inverting power buffer
        module.RegisterFunction("assign @O0 = @I0 ^ @I1;", 2, 1, "EOA", "EOB");
        module.RegisterFunction("assign @O0 = ~(@I0 ^ @I1);", 2, 1, "ENA");
        module.RegisterFunction("assign @O0 = @I0 | @I1;", 2, 1, "OR2A", "OR2B", "OR2C");
        module.RegisterFunction("assign @O0 = @I0 & @I1;", 2, 1, "AND2A", "AND2B", "AND2C");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1);", 2, 1, "NR2A", "NR2B", "NR2C");
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1);", 2, 1, "ND2A", "ND2B", "ND2C");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2);", 3, 1, "NR3A", "NR3B", "NR3C");
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2);", 3, 1, "ND3A", "ND3B", "ND3C");
        module.RegisterFunction("assign @O0 = @I0 | @I1 | @I2;", 3, 1, "OR3A");
        module.RegisterFunction("assign @O0 = @I0 & @I1 & @I2;", 3, 1, "AND3A", "AND3B", "AND3C");
        module.RegisterFunction("assign @O0 = ~((@I0 & @I1)|(@I2 & @I3));", 4, 1, "AO2A", "AO2B", "AO2C");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3);", 4, 1, "NR4A", "NR4B", "NR4C");
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3);", 4, 1, "ND4A", "ND4C");
        module.RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3;", 4, 1, "OR4A");
        module.RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3;", 4, 1, "AND4A", "AND4B");
        module.RegisterFunction("assign @O0 = ~(@I0 & ~(@I1 & @I2 & @I3 & @I4));", 5, 1, "N4AND");   //translated from MODULE in IODEC
        module.RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4;", 5, 1, "OR5A");
        module.RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4;", 5, 1, "AND5A", "AND5B");
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4);", 5, 1, "ND5A", "ND5B", "ND5C");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4);", 5, 1, "NR5A", "NR5B");
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5);", 6, 1, "ND6A", "ND6B", "ND6C");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4 | @I5);", 6, 1, "NR6A", "NR6B");
        module.RegisterFunction("assign @O0 = ~((@I0 & @I1) | (@I2 & @I3) | (@I4 & @I5));", 6, 1, "AO11A");    // translated from MACROS.HDL
        module.RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7);", 8, 1, "ND8A");
        module.RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4 | @I5 | @I6 | @I7);", 8, 1, "NR8A");

        module.RegisterFunction("assign @O0 = ~(@I2 ? @I1 : @I0);", 3, 1, "MX21LA", "MX21LB");   // Inverting output  (based on MUX21l)

        // Ported to non tristate bus style
        module.RegisterFunction("assign @TO0 = ~@I0; assign @TE0 = ~@I1;@T+0 ", 2, 1, "MACZINVB1");
        module.RegisterFunction("assign @TO0 = @I0; assign @TE0 = @I1;@T+0 ", 2, 1, "BTS4A", "BTS4B", "BTS4C", "ZBUF1");         // 5 is inverting output,4 is normal?
        module.RegisterFunction("assign @TO0 = ~@I0; assign @TE0 = @I1;@T+0 ", 2, 1, "BTS5A", "BTS5B");         // 5 is inverting output,4 is normal?
        module.RegisterFunction($"wire @N_@TO0,@N_@TO0L; LD1A @N_inst (.MasterClock(MasterClock), .q(@N_@TO0),.qL(@N_@TO0L),.d(@I1),.en(@I2)); assign @TO0 = ~@N_@TO0; assign @TE0 = ~@I3;@T+0 ", 4,1, "ZTLATCH1");

        // Requires Module Implementations - requires injected clock
        module.RegisterFunction($"LD1A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD1A");
        module.RegisterFunction($"LD2A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD2A");
        module.RegisterFunction($"FD1A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.clk(@I1));", 2,2, "FD1A");
        module.RegisterFunction($"FD2A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2));", 3,2, "FD2A");
        module.RegisterFunction($"FD3A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2),.sL(@I3));", 4,2, "FD3A");
        module.RegisterFunction($"FD4A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.sL(@I2));", 3,2, "FD4A");
        module.RegisterFunction($"FJK1A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.clk(@I2));", 3,2, "FJK1A");
        module.RegisterFunction($"FJK2A @N_inst (.MasterClock(MasterClock),.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.clk(@I2),.rL(@I3));", 4,2, "FJK2A");
        module.RegisterFunction($"SR @N_inst (.MasterClock(MasterClock),.Q(@O0),.QL(@O1),.S(@I0),.R(@I1));", 2, 2, "SR");

        // Could be packed to make more readable e.g. .A({A_3,A_2,A_1,A_0})
        module.RegisterFunction($@"FA4C @N_inst (.CO(@O0),.SUM_3(@O1),.SUM_2(@O2),.SUM_1(@O3),.SUM_0(@O4),.CI(@I0),.A_3(@I1),.A_2(@I2),.A_1(@I3),.A_0(@I4),.B_3(@I5),.B_2(@I6),.B_1(@I7),.B_0(@I8));", 9,5, "FA4C");
        module.RegisterFunction($@"FA8 @N_inst (.CO(@O0),.SUM_7(@O1),.SUM_6(@O2),.SUM_5(@O3),.SUM_4(@O4),.SUM_3(@O5),.SUM_2(@O6),.SUM_1(@O7),.SUM_0(@O8),.CI(@I0),.A_7(@I1),.A_6(@I2),.A_5(@I3),.A_4(@I4),.A_3(@I5),.A_2(@I6),.A_1(@I7),.A_0(@I8),.B_7(@I9),.B_6(@I10),.B_5(@I11),.B_4(@I12),.B_3(@I13),.B_2(@I14),.B_1(@I15),.B_0(@I16));", 17,9, "FA8");
    }

    public Module.Code ReplaceCodeLine(string moduleName, Module.Code cl)
    {
        Module.Code retVal = cl;
        if (moduleName=="VCNT" && cl.outputs.Count==1 && cl.outputs[0].Value=="VSYNCDL")
        {
            // Replace VSYNC so that its a constant low for entire vsync time, rather than only active when hsync not
            if (retVal.inputs[0].Value=="HVSYNC")
            {
                retVal.inputs[0]=new Token(TokenType.NUMBER, cl.inputs[0].Line, "1");
            }
        }
        if (moduleName=="MEM" && cl.outputs.Count==1 && (cl.outputs[0].Value=="VRASL_0" || cl.outputs[0].Value=="VRASL_1"))
        {
            // Replace DRAM RAS signal since at present we are targeting internal RAM blocks and the muxing complicated the integration
            if (retVal.inputs[0].Value=="VRAS")
            {
                retVal.inputs[0]=new Token(TokenType.NUMBER, cl.inputs[0].Line, "0");
            }
        }
        if (moduleName == "BM" && cl.outputs.Count == 1 && (cl.outputs[0].Value == "VA_0" || cl.outputs[0].Value == "VA_1" || cl.outputs[0].Value == "VA_2" || cl.outputs[0].Value == "VA_3" || cl.outputs[0].Value == "VA_4" || cl.outputs[0].Value == "VA_5" || cl.outputs[0].Value == "VA_6" || cl.outputs[0].Value == "VA_7" || cl.outputs[0].Value == "VA_16"))
        {
            // Replace Multiplexing on DRAM signals
            if (retVal.inputs[1].Value=="MUXBL")
            {
                retVal.inputs[1]=new Token(TokenType.NUMBER, cl.inputs[0].Line, "1");
            }
            if (retVal.inputs[3].Value=="MUXB")
            {
                retVal.inputs[3]=new Token(TokenType.NUMBER, cl.inputs[0].Line, "0");
            }
        }

        return retVal;
    }

    public string[] PreCodeLines(string moduleName)
    {
        // These are currently just used to inject some variables for verilator
        if (moduleName=="VCNT")
        {
            return new [] {
                "",
                "/* Capture Vertical Counter For Verilator Debugger */",
                "wire [8:0] verilatorVID_VC /* verilator public */;",
                "assign verilatorVID_VC = {VC_8,VC_7,VC_6,VC_5,VC_4,VC_3,VC_2,VC_1,VC_0};"
            };
        }
        if (moduleName=="HCNT")
        {
            return new [] {
                "",
                "/* Capture Horizontal Counter For Verilator Debugger */",
                "wire [9:0] verilatorVID_HC /* verilator public */;",
                "assign verilatorVID_HC = {HC_9,HC_8,HC_7,HC_6,HC_5,HC_4,HC_3,HC_2,HC_1,HC_0};"
            };
        }
        if (moduleName=="PC")
        {
            return new [] {
                "",
                "/* Capture DSP Program Counter For Verilator Debugger */",
                "wire [7:0] verilatorDSP_PC /* verilator public */;",
                "assign verilatorDSP_PC = {PC_7,PC_6,PC_5,PC_4,PC_3,PC_2,PC_1,PC_0};"
            };
        }
        if (moduleName=="INSTRUCT")
        {
            return new [] {
                "",
                "/* Capture DSP Pipelined Instruction For Verilator Debugger */",
                "wire [5:0] verilatorDSP_PDKU /* verilator public */;",
                "assign verilatorDSP_PDKU = {PDKU_15,PDKU_14,PDKU_13,PDKU_12,PDKU_11,PDKU_10};"
            };
        }
        if (moduleName=="ADDRESS")
        {
            return new [] {
                "",
                "/* Capture DSP Index Register For Verilator Debugger */",
                "wire [8:0] verilatorDSP_IX /* verilator public */;",
                "assign verilatorDSP_IX = {IX_8,IX_7,IX_6,IX_5,IX_4,IX_3,IX_2,IX_1,IX_0};",
                "",
                "/* Capture DSP Intrude Address Register For Verilator Debugger */",
                "wire [8:0] verilatorDSP_INTRA /* verilator public */;",
                "assign verilatorDSP_INTRA = {INTRA_8,INTRA_7,INTRA_6,INTRA_5,INTRA_4,INTRA_3,INTRA_2,INTRA_1,INTRA_0};",
                "",
                "/* Capture DSP DataAddress Register For Verilator Debugger */",
                "wire [8:0] verilatorDSP_DA /* verilator public */;",
                "assign verilatorDSP_DA = {DA_8,DA_7,DA_6,DA_5,DA_4,DA_3,DA_2,DA_1,DA_0};",
            };
        }
        if (moduleName=="DMA")
        {
            return new [] {
                "",
                "/* Capture DSP DMA Data Register For Verilator Debugger */",
                "wire [15:0] verilatorDSP_DMD /* verilator public */;",
                "assign verilatorDSP_DMD = {DMD_15,DMD_14,DMD_13,DMD_12,DMD_11,DMD_10,DMD_9,DMD_8,DMD_7,DMD_6,DMD_5,DMD_4,DMD_3,DMD_2,DMD_1,DMD_0};",
                "",
                "/* Capture DSP DMA0 Register For Verilator Debugger */",
                "wire [15:0] verilatorDSP_DMA0 /* verilator public */;",
                "assign verilatorDSP_DMA0 = {DMA_15,DMA_14,DMA_13,DMA_12,DMA_11,DMA_10,DMA_9,DMA_8,DMA_7,DMA_6,DMA_5,DMA_4,DMA_3,DMA_2,DMA_1,DMA_0};",
                "",
                "/* Capture DSP DMA1 Register For Verilator Debugger */",
                "wire [15:0] verilatorDSP_DMA1 /* verilator public */;",
                "assign verilatorDSP_DMA1 = {1'b0,1'b0,1'b0,1'b0,HOLD,RDWR,BYTE,LOHI,1'b0,1'b0,1'b0,1'b0,DMA_19,DMA_18,DMA_17,DMA_16};",
            };
        }

        if (moduleName=="ALU")
        {
            return new [] {
                "",
                "/* Capture DSP ALU Carry Register For Verilator Debugger */",
                "wire verilatorDSP_CARRY /* verilator public */;",
                "assign verilatorDSP_CARRY = CARRY;",
                "",
                "/* Capture DSP X Register For Verilator Debugger */",
                "wire [15:0] verilatorDSP_X /* verilator public */;",
                "assign verilatorDSP_X = {XU_15,XU_14,XU_13,XU_12,XU_11,XU_10,XU_9,XU_8,XU_7,XU_6,XU_5,XU_4,XU_3,XU_2,XU_1,XU_0};",
                "",
                "/* Capture DSP AZ Register For Verilator Debugger */",
                "wire [15:0] verilatorDSP_AZ /* verilator public */;",
                "assign verilatorDSP_AZ = {AZR_15,AZR_14,AZR_13,AZR_12,AZR_11,AZR_10,AZR_9,AZR_8,AZR_7,AZR_6,AZR_5,AZR_4,AZR_3,AZR_2,AZR_1,AZR_0};",
                "",
                "/* Capture DSP MZ Register For Verilator Debugger */",
                "wire [35:0] verilatorDSP_MZ /* verilator public */;",
                "assign verilatorDSP_MZ = {MZR_35,MZR_34,MZR_33,MZR_32,MZR_31,MZR_30,MZR_29,MZR_28,MZR_27,MZR_26,MZR_25,MZR_24,MZR_23,MZR_22,MZR_21,MZR_20,MZR_19,MZR_18,MZR_17,MZR_16,MZR_15,MZR_14,MZR_13,MZR_12,MZR_11,MZR_10,MZR_9,MZR_8,MZR_7,MZR_6,MZR_5,MZR_4,MZR_3,MZR_2,MZR_1,MZR_0};",
                "",
                "/* Capture DSP MODE Register For Verilator Debugger */",
                "wire [6:0] verilatorDSP_MODE /* verilator public */;",
                "assign verilatorDSP_MODE = {MOD_6,MOD_5,MOD_4,MOD_3,MOD_2,MOD_1,MOD_0};",
            };
        }

        return new string[] { };
    }

    public string[] BeforeEndModule(string moduleName)
    {
        if (moduleName == "PWM")
        {
            // Inject Sampling the DAC
            return new [] {
                "",
                "reg [13:0] iDAC;",
                "",
                "always @(posedge MasterClock)",
                "begin",
                "    if (~DACWRL)",
                "    begin",
                "        iDAC[13] <= DL_15;",
                "        iDAC[12] <= D_14;",
                "        iDAC[11] <= D_13;",
                "        iDAC[10] <= D_12;",
                "        iDAC[9] <= D_11;",
                "        iDAC[8] <= D_10;",
                "        iDAC[7] <= D_9;",
                "        iDAC[6] <= D_8;",
                "        iDAC[5] <= D_7;",
                "        iDAC[4] <= D_6;",
                "        iDAC[3] <= D_5;",
                "        iDAC[2] <= D_4;",
                "        iDAC[1] <= D_3;",
                "        iDAC[0] <= D_2;",
                "    end",
                "end",
                "",
                "assign DAC = iDAC;",
                ""
            };
        }
        return new string[] { };
    }
}

