using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class ParseException : Exception
{
    public ParseException(int line, string message) : base($"{line:00000} : {message}")
    {
    }
}

class Module
{
    struct Function
    {
        public string name;
        public int numInputs;
        public int numOutputs;
        public string translateAs;
    }

    Dictionary<string, Function> functions;
    Dictionary<string, Module> modules;

    readonly string originalFilePath;

    public Module(string fromFilePath)
    {
        functions=new Dictionary<string, Function>();
        modules=new Dictionary<string, Module>();
        linesThatNeedCommenting=new HashSet<int>();
        originalFilePath = fromFilePath;
        firstLine=1;
    }
    int firstLine;

    void RegisterFunction(string translate, int numInputs, int numOutputs, params string[] aliases)
    {
        foreach (var a in aliases)
        {
            functions.Add(a,new Function { name = a, numInputs=numInputs, numOutputs=numOutputs, translateAs=translate});
        }
    }

    public void RegisterModule(Module input, string originalName)
    {
        // Registers a translated module (ie. VID.NET uses the already translated CLOCK.NET etc)
        modules.Add(originalName, input);
    }
    
    public void RegisterModules(IEnumerable<Module> inputs)
    {
        foreach (var m in inputs)
        {
            if (m.name.getValue() != name.getValue())
            {
                RegisterModule(m, m.name.getValue());
            }
        }
    }

    static readonly HashSet<string> excludeModules = new HashSet<string>(new [] {
        "MACZINVB1",
        "ZTLATCH1",
        "ZBUF1",
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


    void RegisterFunctions()
    {
        RegisterFunction("assign @O0 = @I0;", 1, 1, "B3A");                                 // B3A is non inverting power buffer
        RegisterFunction("assign @O0 = ~@I0;", 1, 1, "B1A", "N1A", "N1B", "N1C", "N1D");    // B1A is an inverting power buffer
        RegisterFunction("assign @O0 = @I0 ^ @I1;", 2, 1, "EOA", "EOB");
        RegisterFunction("assign @O0 = ~(@I0 ^ @I1);", 2, 1, "ENA");
        RegisterFunction("assign @O0 = @I0 | @I1;", 2, 1, "OR2A", "OR2B", "OR2C");
        RegisterFunction("assign @O0 = @I0 & @I1;", 2, 1, "AND2A", "AND2B", "AND2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1);", 2, 1, "NR2A", "NR2B", "NR2C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1);", 2, 1, "ND2A", "ND2B", "ND2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2);", 3, 1, "NR3A", "NR3B", "NR3C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2);", 3, 1, "ND3A", "ND3B", "ND3C");
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2;", 3, 1, "OR3A");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2;", 3, 1, "AND3A", "AND3B", "AND3C");
        RegisterFunction("assign @O0 = ~((@I0 & @I1)|(@I2 & @I3));", 4, 1, "AO2A", "AO2B", "AO2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3);", 4, 1, "NR4A", "NR4B", "NR4C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3);", 4, 1, "ND4A", "ND4C");
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3;", 4, 1, "OR4A");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3;", 4, 1, "AND4A", "AND4B");
        RegisterFunction("assign @O0 = ~(@I0 & ~(@I1 & @I2 & @I3 & @I4));", 5, 1, "N4AND");   //translated from MODULE in IODEC
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4;", 5, 1, "OR5A");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4;", 5, 1, "AND5A", "AND5B");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4);", 5, 1, "ND5A", "ND5B", "ND5C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4);", 5, 1, "NR5A", "NR5B");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5);", 6, 1, "ND6A", "ND6B", "ND6C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4 | @I5);", 6, 1, "NR6A", "NR6B");
        RegisterFunction("assign @O0 = ~((@I0 & @I1) | (@I2 & @I3) | (@I4 & @I5));", 6, 1, "AO11A");    // translated from MACROS.HDL
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7);", 8, 1, "ND8A");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4 | @I5 | @I6 | @I7);", 8, 1, "NR8A");
        //RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4 | @I5 | @I6 | @I7;", 8, 1, "MACAND8");
        //RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7 & @I8 & @I9;", 10, 1, "AND10");
        //RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7 & @I8 & @I9 & @I10;", 11, 1, "AND11");
        //RegisterFunction("assign @O0 = (@I0 ~^ @I9) & (@I1 ~^ @I10) & (@I2 ~^ @I11) & (@I3 ~^ @I12) & (@I4 ~^ @I13) & (@I5 ~^ @I14) & (@I6 ~^ @I15) & (@I7 ~^ @I16) & (@I8 ~^ @I17) & @I18;", 9+9+1, 1, "EQU9");

        //RegisterFunction("assign @O0 = ~(~((@I0 & @I1)|(@I2 & @I3)));", 4, 1, "MUX");
        //RegisterFunction("assign @O0 = ~((~(@I0 & @I1)) & (~(@I2 & @I3)));", 4, 1, "MUX2");

        // Multiple out, but short enough to not need function
        //RegisterFunction("assign @O0 = ~@I0; assign @O1 = ~@I1;", 2, 2, "MACINV2");
        //RegisterFunction("assign @O0 = @I0 ^ @I1; assign @O1 = @I0 & @I1;", 2, 2, "HALFADD");

        // Not 100%

        RegisterFunction("assign @O0 = ~(@I2 ? @I1 : @I0);", 3, 1, "MX21LA", "MX21LB");   // Inverting output  (based on MUX21l)



        // Not synthesizable (will need to be replaced)
        //RegisterFunction("assign @O0 = ~@I1 ? @I0 : 1'bZ;", 2, 1, "MACZINVB1");
        //RegisterFunction("assign @O0 = @I1 ? @I0 : 1'bZ;", 2, 1, "BTS4A", "BTS4B", "BTS4C");         // 5 is inverting output,4 is normal?
        //RegisterFunction("assign @O0 = @I1 ? (~@I0) : 1'bZ;", 2, 1, "BTS5A");         // 5 is inverting output,4 is normal?
        //RegisterFunction($"ZTLATCH1 @N_inst (.QB(@O0),.D(@I1),.CLK(@I2),.ENL(@I3));", 4,1, "ZTLATCH1"); // TODO add verify that I0==O0
        
        // Ported to non tristate bus style
        RegisterFunction("assign @TO0 = ~@I0; assign @TE0 = ~@I1;@T+0 ", 2, 1, "MACZINVB1");
        RegisterFunction("assign @TO0 = @I0; assign @TE0 = @I1;@T+0 ", 2, 1, "BTS4A", "BTS4B", "BTS4C", "ZBUF1");         // 5 is inverting output,4 is normal?
        RegisterFunction("assign @TO0 = ~@I0; assign @TE0 = @I1;@T+0 ", 2, 1, "BTS5A", "BTS5B");         // 5 is inverting output,4 is normal?
        RegisterFunction($"wire @N_@TO0,@N_@TO0L; LD1A @N_inst (.q(@N_@TO0),.qL(@N_@TO0L),.d(@I1),.en(@I2)); assign @TO0 = ~@I0; assign @TE0 = ~@I3;@T+0 ", 4,1, "ZTLATCH1");

        // Requires Module Implementations
        //RegisterFunction($"SR @N_inst (.Q(@O0),.QL(@O1),.S(@I0),.R(@I1));", 2,2, "SR");
        RegisterFunction($"LD1A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD1A");
        RegisterFunction($"LD2A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD2A");
        RegisterFunction($"FD1A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1));", 2,2, "FD1A");
        //RegisterFunction($"FULLADD @N_inst (.Q(@O0),.CO(@O1),.A(@I0),.B(@I1),.CI(@I2));", 3,2, "FULLADD");
        RegisterFunction($"FD2A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2));", 3,2, "FD2A");
        RegisterFunction($"FD3A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2),.sL(@I3));", 4,2, "FD3A");
        RegisterFunction($"FD4A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.sL(@I2));", 3,2, "FD4A");
        //RegisterFunction($"JK @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.r(@I2),.clk(@I3));", 4,2, "JK");
        RegisterFunction($"FJK1A @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.clk(@I2));", 3,2, "FJK1A");
        RegisterFunction($"FJK2A @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.clk(@I2),.rL(@I3));", 4,2, "FJK2A");
        //RegisterFunction($"SYNCNT0 @N_inst (.Q(@O0),.QB(@O1),.D(@I0),.CLK(@I1),.CLR(@I2),.LDL(@I3));", 4,2, "SYNCNT0");
        //RegisterFunction($"SYNCNT @N_inst (.Q(@O0),.QB(@O1),.CO(@O2),.D(@I0),.CLK(@I1),.CLR(@I2),.LDL(@I3),.CI(@I4));", 5,3, "SYNCNT");
        //RegisterFunction($"MUX4 @N_inst (.Q(@O0),.A(@I0),.B(@I1),.D_0(@I2),.D_1(@I3),.D_2(@I4),.D_3(@I5));", 6,1, "MUX4");
        //RegisterFunction($"LFUBIT @N_inst (.DOUT(@O0),.SRCD(@I0),.DSTD(@I1),.LFUC_0(@I2),.LFUC_1(@I3),.LFUC_2(@I4),.LFUC_3(@I5));", 6,1, "LFUBIT");
        //RegisterFunction($"LSCNTEL @N_inst (.Q(@O0),.QL(@O1),.CO(@O2),.D(@I0),.LD(@I1),.LDL(@I2),.CLK(@I3),.CI(@I4),.RSTL(@I5));", 6,3, "LSCNTEL");

        // Could be packed to make more readable e.g. .A({A_3,A_2,A_1,A_0})
        RegisterFunction($@"FA4C @N_inst (.CO(@O0),.SUM_3(@O1),.SUM_2(@O2),.SUM_1(@O3),.SUM_0(@O4),.CI(@I0),.A_3(@I1),.A_2(@I2),.A_1(@I3),.A_0(@I4),.B_3(@I5),.B_2(@I6),.B_1(@I7),.B_0(@I8));", 9,5, "FA4C");
        RegisterFunction($@"FA8 @N_inst (.CO(@O0),.SUM_7(@O1),.SUM_6(@O2),.SUM_5(@O3),.SUM_4(@O4),.SUM_3(@O5),.SUM_2(@O6),.SUM_1(@O7),.SUM_0(@O8),.CI(@I0),.A_7(@I1),.A_6(@I2),.A_5(@I3),.A_4(@I4),.A_3(@I5),.A_2(@I6),.A_1(@I7),.A_0(@I8),.B_7(@I9),.B_6(@I10),.B_5(@I11),.B_4(@I12),.B_3(@I13),.B_2(@I14),.B_1(@I15),.B_0(@I16));", 17,9, "FA8");

    }

    string Translate(Code code)
    {
        if (functions.TryGetValue(code.functionName.getValue(), out var func))
        {
            return TranslateFunction(code, func);
        }
        else if (modules.TryGetValue(code.functionName.getValue(), out var module))
        {
            return TranslateModule(code, module);
        }
        else
        {
            throw new ParseException(code.instanceName.getLine(), $"Unknown Function for {code.functionName.getValue()}!");
        }
    }

    private string TranslateModule(Code code, Module module)
    {
        // Translation would be simple here except for the caveat of modules with bidirectional pins

        // So first we need to verify the number of inputs and outputs
        if (code.inputs.Count != module.inputs.Count)
            throw new ParseException(code.instanceName.getLine(), $"Wrong Number Inputs for {code.functionName.getValue()}!");
        if (code.outputs.Count != module.outputs.Count)
            throw new ParseException(code.instanceName.getLine(), $"Wrong Number Outputs for {code.functionName.getValue()}!");

        // So now, if the module has bidirection pins, we should validate that the input and output names match - we do this as we translate

        var b = new StringBuilder();

        b.Append($"m_{module.name.getValue()} {code.instanceName.getValue()} (");
        for (int i1 = 0; i1 < code.inputs.Count; i1++)
        {
            Token i = code.inputs[i1];

            if (module.InputIsBidirectional(module.inputs[i1]))
            {
                if (InputIsBidirectional(i))
                {
                    b.Append($".in{module.inputs[i1].getValue()}(in{i.getValue()}),");
                }
                else
                {
                    b.Append($".in{module.inputs[i1].getValue()}({i.getValue()}),");
                }
            }
            else
            {
                if (InputIsBidirectional(i))
                {
                    b.Append($".{module.inputs[i1].getValue()}(in{i.getValue()}),");
                }
                else
                {
                    b.Append($".{module.inputs[i1].getValue()}({i.getValue()}),");
                }
            }
        }
        for (int o1 = 0; o1 < code.outputs.Count; o1++)
        {
            Token o = code.outputs[o1];

            if (module.OutputIsBidirectional(module.outputs[o1], out _))
            {
                if (OutputIsBidirectional(o,out var idx))
                {
                    // some modules don't have inputs for bidirectional outputs, these are marked with NO_INPUT_PIN
                    if (module.outputIsBi[o1]!=NO_INPUT_PIN)
                    {
                        if (code.inputs[module.outputIsBi[o1]].getValue() != code.outputs[o1].getValue())
                        {
                            throw new ParseException(code.instanceName.getLine(), "bidirectional pin with different input than output!");
                        }
                    }
                    b.Append($".out{module.outputs[o1].getValue()}(drv{tristateOutputUse[idx]}_out{o.getValue()}),");
                    b.Append($".en{module.outputs[o1].getValue()}(drv{tristateOutputUse[idx]}_en{o.getValue()})");
                    tristateOutputUse[idx]++;
                    if (o1 != code.outputs.Count - 1)
                        b.Append(",");
                }
                else
                {
                    throw new NotImplementedException($"ERMM..");
                }
            }
            else
            {
                b.Append($".{module.outputs[o1].getValue()}({o.getValue()})");
                if (o1 != code.outputs.Count - 1)
                    b.Append(",");
            }
        }

        b.Append(");");

        return b.ToString();
    }

    private string TranslateFunction(Code code, Function func)
    {
        if (code.inputs.Count != func.numInputs)
            throw new ParseException(code.instanceName.getLine(), $"Wrong Number Inputs for {code.functionName.getValue()}!");
        if (code.outputs.Count != func.numOutputs)
            throw new ParseException(code.instanceName.getLine(), $"Wrong Number Outputs for {code.functionName.getValue()}!");

        var b = new StringBuilder();

        int special = 0;
        int value = 0;
        foreach (var s in func.translateAs)
        {
            switch (special)
            {
                case 0:
                    if (s == '@')
                    {
                        special = 1;
                        continue;
                    }
                    b.Append(s);
                    continue;
                case 1:
                    if (s == 'I')
                    {
                        special = 2;
                        value = 0;
                        continue;
                    }
                    if (s == 'O')
                    {
                        special = 3;
                        value = 0;
                        continue;
                    }
                    if (s == 'N')
                    {
                        b.Append(code.instanceName.getValue());
                        special = 0;
                        continue;
                    }
                    if (s == 'T')
                    {
                        special = 4;
                        value = 0;
                        continue;
                    }

                    throw new NotImplementedException($"Unhandled special");
                case 2:
                case 3:
                    if (s >= '0' && s <= '9')
                    {
                        value = value * 10;
                        value += s - '0';
                    }
                    else
                    {
                        if (special == 2)
                        {
                            if (InputIsBidirectional(code.inputs[value]))
                            {
                                b.Append($"in{code.inputs[value].getValue()}");
                            }
                            else
                            {
                                b.Append($"{code.inputs[value].getValue()}");
                            }
                        }
                        else if (special == 3)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out _))
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to bidirectional output via non tristate driver");
                                //b.Append($"{code.outputs[value].getValue()}");
                            }
                            else
                            {
                                b.Append($"{code.outputs[value].getValue()}");
                            }
                        }
                        else
                            throw new NotImplementedException($"Unexpected special");
                        b.Append(s);
                        special = 0;
                    }
                    continue;
                case 4:
                    if (s == 'O')
                    {
                        special = 5;
                    }
                    else if (s == 'E')
                    {
                        special = 6;
                    }
                    else if (s == '+')
                    {
                        special = 7;
                    }
                    else
                        throw new NotImplementedException($"Unexpected special");
                    value=0;
                    continue;
                case 5:
                case 6:
                case 7:
                    if (s >= '0' && s <= '9')
                    {
                        value = value * 10;
                        value += s - '0';
                    }
                    else
                    {
                        if (special == 5)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out var idx))
                            {
                                b.Append($"drv{tristateOutputUse[idx]}_out{code.outputs[value].getValue()}");
                            }
                            else
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to non bidirectional output is unexpected!");
                                //b.Append($"{code.outputs[value].getValue()}");
                            }
                        }
                        else if (special == 6)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out var idx))
                            {
                                b.Append($"drv{tristateOutputUse[idx]}_en{code.outputs[value].getValue()}");
                            }
                            else
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to non bidirectional enable is unexpected!");
                                //b.Append($"{code.outputs[value].getValue()}");
                            }
                        }
                        else if (special == 7)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out var idx))
                            {
                                tristateOutputUse[idx]++;
                            }
                            else
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to non bidirectional enable is unexpected!");
                                //b.Append($"{code.outputs[value].getValue()}");
                            }
                        }
                        else
                            throw new NotImplementedException($"Unexpected special");
                        b.Append(s);
                        special = 0;
                    }
                    continue;
            }
        }

        return b.ToString();
    }

    struct Code
    {
        public Token instanceName;
        public List<Token> outputs;
        public Token functionName;
        public List<Token> inputs;
    }

    Token name;
    int[] inputIsBi;
    int[] outputIsBi;
    List<Token> inputs;
    List<Token> outputs;
    Token define;               // Used to decide where to place wires

    List<Token> wires;
    List<Code> codeLines;

    Token end;

    HashSet<int> linesThatNeedCommenting;

    private enum FileState
    {
        ExpectingCompile,
        ExpectingDirectory,
        ExpectingModuleOrEndCompile,
        ExpectingEnd
    }
    
    private enum ModuleState
    {
        ExpectingModule,
        ExpectingInputs,
        ExpectingOutputs,
        ExpectingLevel,
        ExpectingDefine,
        ExpectingCode
    }

    private void MarkAsCodeLine(Tokenizer tokenizer, int tokenCount)
    {
        for (int a=0;a<tokenCount;a++)
        {
            linesThatNeedCommenting.Add(tokenizer.nextToken(a).getLine());
        }
    }

    private bool InputIsBidirectional(Token token)
    {
        for (int i = 0; i < inputs.Count; i++)
        {
            Token s = inputs[i];

            if (s.getValue() == token.getValue())
            {
                return inputIsBi[i]>=0;
            }
        }
        return false;
    }
    private bool OutputIsBidirectional(Token token, out int idx)
    {
        idx=0;
        for (int i = 0; i < outputs.Count; i++)
        {
            Token s = outputs[i];

            if (s.getValue() == token.getValue())
            {
                idx = i;
                return outputIsBi[i]>=0;
            }
        }
        return false;
    }

    public static List<Module> ParseFile(string filepath)
    {
        Tokenizer tokenizer = new Tokenizer();
        try 
        {
            using (var input = new StreamReader(filepath))
            {
                tokenizer.tokenize(input);
            }
        } 
        catch (Exception ex) 
        {
            Console.WriteLine(ex.StackTrace);
        }

        var modules = new List<Module>();

        tokenizer.reset();

        var state = FileState.ExpectingCompile;

        var module = new Module(filepath);
        
        while (!tokenizer.matchTokens(TokenType.END, TokenType.SEMICOLON))
        {
            switch (state)
            {
                case FileState.ExpectingCompile:
                    if (!tokenizer.matchTokens(TokenType.COMPILE,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected COMPILE;");
                    module.MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    state=FileState.ExpectingDirectory;
                    break;
                case FileState.ExpectingDirectory:
                    if (!tokenizer.matchTokens(TokenType.DIRECTORY,TokenType.MASTER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected DIRECTORY MASTER;");
                    module.MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=FileState.ExpectingModuleOrEndCompile;
                    break;
                case FileState.ExpectingModuleOrEndCompile:
                    if (tokenizer.matchTokens(TokenType.MODULE,TokenType.IDENTIFIER,TokenType.SEMICOLON))
                    {
                        module.Parse(tokenizer);
                        if (!excludeModules.Contains(module.name.getValue()))
                        {
                            modules.Add(module);
                        }
                        module = new Module(filepath);
                        module.SetFirstLine(tokenizer.nextToken().getLine());
                    }
                    else if (tokenizer.matchTokens(TokenType.END, TokenType.COMPILE, TokenType.SEMICOLON))
                    {
                        module.MarkAsCodeLine(tokenizer,3);
                        tokenizer.consumeToken(3);
                        state=FileState.ExpectingEnd;
                    }
                    else
                    {
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected MODULE <name>; or END COMPILE;");
                    }
                    break;
                case FileState.ExpectingEnd:
                    break;
            }
        }

        module.MarkAsCodeLine(tokenizer,3);
        tokenizer.consumeToken(3);

        return modules;
    }

    public void Parse(Tokenizer tokenizer)
    {
        var state = ModuleState.ExpectingModule;
        codeLines=new List<Code>();
        
        while (!tokenizer.matchTokens(TokenType.END, TokenType.MODULE, TokenType.SEMICOLON))
        {
            switch (state)
            {
                case ModuleState.ExpectingModule:
                    if (!tokenizer.matchTokens(TokenType.MODULE,TokenType.IDENTIFIER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected MODULE <name>;");
                    this.name = tokenizer.nextToken(1);
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=ModuleState.ExpectingInputs;
                    break;
                case ModuleState.ExpectingInputs:
                    if (!tokenizer.matchTokens(TokenType.INPUTS,TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected INPUTS <name>[,<name>]*;");
                    inputs = new List<Token>();
                    inputs.Add(tokenizer.nextToken(1));
                    MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    while (tokenizer.matchTokens(TokenType.COMMA, TokenType.IDENTIFIER))
                    {
                        inputs.Add(tokenizer.nextToken(1));
                        MarkAsCodeLine(tokenizer,2);
                        tokenizer.consumeToken(2);
                    }
                    if (!tokenizer.matchTokens(TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected INPUTS <name>[,<name>]*;");
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingOutputs;
                    break;
                case ModuleState.ExpectingOutputs:
                    if (!tokenizer.matchTokens(TokenType.OUTPUTS,TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected OUTPUTS <name>[,<name>]*;");
                    outputs = new List<Token>();
                    outputs.Add(tokenizer.nextToken(1));
                    MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    while (tokenizer.matchTokens(TokenType.COMMA, TokenType.IDENTIFIER))
                    {
                        outputs.Add(tokenizer.nextToken(1));
                        MarkAsCodeLine(tokenizer,2);
                        tokenizer.consumeToken(2);
                    }
                    if (!tokenizer.matchTokens(TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected OUTPUTS <name>[,<name>]*;");
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingLevel;
                    break;
                case ModuleState.ExpectingLevel:
                    if (!tokenizer.matchTokens(TokenType.LEVEL,TokenType.FUNCTION,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected LEVEL FUNCTION;");
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=ModuleState.ExpectingDefine;
                    break;
                case ModuleState.ExpectingDefine:
                    if (!tokenizer.matchTokens(TokenType.DEFINE))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected DEFINE");
                    define=tokenizer.nextToken();
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingCode;
                    break;
                case ModuleState.ExpectingCode:
                    if (!tokenizer.matchTokens(TokenType.IDENTIFIER, TokenType.LPAREN, TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
                    var code = new Code();
                    code.instanceName = tokenizer.nextToken(0);
                    code.outputs=new List<Token>();
                    code.outputs.Add(tokenizer.nextToken(2));
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    while (tokenizer.matchTokens(TokenType.COMMA, TokenType.IDENTIFIER))
                    {
                        code.outputs.Add(tokenizer.nextToken(1));
                        MarkAsCodeLine(tokenizer,2);
                        tokenizer.consumeToken(2);
                    }
                    if (!tokenizer.matchTokens(TokenType.RPAREN, TokenType.ASSIGN, TokenType.IDENTIFIER, TokenType.LPAREN, TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
                    code.functionName = tokenizer.nextToken(2);
                    code.inputs = new List<Token>();
                    code.inputs.Add(tokenizer.nextToken(4));
                    MarkAsCodeLine(tokenizer,5);
                    tokenizer.consumeToken(5);
                    while (tokenizer.matchTokens(TokenType.COMMA, TokenType.IDENTIFIER) || tokenizer.matchTokens(TokenType.COMMA, TokenType.NUMBER))
                    {
                        code.inputs.Add(tokenizer.nextToken(1));
                        MarkAsCodeLine(tokenizer,2);
                        tokenizer.consumeToken(2);
                    }
                    if (!tokenizer.matchTokens(TokenType.RPAREN, TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
                    MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    codeLines.Add(code);
                    state=ModuleState.ExpectingCode;
                    break;
            }
        }
        end = tokenizer.nextToken();
        MarkAsCodeLine(tokenizer,3);
        tokenizer.consumeToken(3);

        // Now do the work we need to translate the modules etc.

        RegisterFunctions();

        PassMakeBidirectional();
        PassPickupBidirectionalOutputOnly();
        PassConstructWireList();
        PassConstructOutputTristateMuxWires();
    }

    const int NO_INPUT_PIN=99999999;

    private void PassPickupBidirectionalOutputOnly()
    {
        // The dsp is full of modules that produce tristate outputs without definining the inputs...
        //so for now, we also convert outputs to bidirectional if they are driven by a bidirectional driver
        foreach (var c in codeLines)
        {
            if (bidirectionalDriver.Contains(c.functionName.getValue()))
            {
                foreach (var co in c.outputs)
                {
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        Token o = outputs[i];
                        if (o.getValue() == co.getValue())
                        {
                            outputIsBi[i] = NO_INPUT_PIN;    // no input to connect to
                        }
                    }
                }
            }
        }
    }

    private void PassMakeBidirectional()
    {
        // just do it the lazy way
        inputIsBi = new int [inputs.Count];
        outputIsBi = new int [outputs.Count];

        for (int i=0;i<inputs.Count;i++)
            inputIsBi[i]=-1;
        for (int o=0;o<outputs.Count;o++)
            outputIsBi[o]=-1;

        for (int i1 = 0; i1 < inputs.Count; i1++)
        {
            Token i = inputs[i1];
            for (int i2 = 0; i2 < outputs.Count; i2++)
            {
                Token o = outputs[i2];
                if (i.getValue() == o.getValue())
                {
                    if (inputIsBi[i1]!=-1 || outputIsBi[i2]!=-1)
                        throw new NotImplementedException($"Whoops");

                    inputIsBi[i1]=i2;
                    outputIsBi[i2]=i1;
                    break;
                }
            }
        }
    }

    private void PassConstructWireList()
    {
        // Construct list of wires we need to declare
        wires = new List<Token>();
        var uniqueWire = new HashSet<string>();
        uniqueWire.Add("1");
        uniqueWire.Add("0");
        foreach (var i in inputs)
        {
            uniqueWire.Add(i.getValue());
        }
        foreach (var o in outputs)
        {
            uniqueWire.Add(o.getValue());
        }
        foreach (var cl in codeLines)
        {
            foreach (var i in cl.inputs)
            {
                if (!uniqueWire.Contains(i.getValue()))
                {
                    uniqueWire.Add(i.getValue());
                    wires.Add(i);
                }
            }
            foreach (var o in cl.outputs)
            {
                if (!uniqueWire.Contains(o.getValue()))
                {
                    uniqueWire.Add(o.getValue());
                    wires.Add(o);
                }
            }
        }
    }

    int[] tristateOutputUsages;
    int[] tristateOutputUse;

    private void PassConstructOutputTristateMuxWires()
    {
        tristateOutputUsages = new int [outputs.Count];
        tristateOutputUse = new int [outputs.Count];
        foreach (var line in codeLines)
        {
            foreach (var o in line.outputs)
            {
                if (OutputIsBidirectional(o, out var idx))
                {
                    tristateOutputUsages[idx]++;
                }
            }
        }

        // Reloop through outputs and construct wires for all tristates
        foreach (var o in outputs)
        {
            if (OutputIsBidirectional(o, out var idx))
            {
                for (int a = 0; a < tristateOutputUsages[idx]; a++)
                {
                    wires.Add(new Token(o.getType(), o.getLine(), $"drv{a}_out{o.getValue()}"));
                    wires.Add(new Token(o.getType(), o.getLine(), $"drv{a}_en{o.getValue()}"));
                }
            }
        }
    }

    Dictionary<int, string> originalFileLines;
    int currentLine;

    private void CreateOriginalFileLinesDictionary(string originalFilePath)
    {
        originalFileLines=new Dictionary<int, string>();
        using (var input = new StreamReader(originalFilePath))
        {
            int line=0;
            currentLine=1;
            string s;
            while ((s = input.ReadLine()) != null)
            {
                line++;
                originalFileLines.Add(line,s);
            }
        }
    }

    const int COMMENTPAD=80;

    TextWriter output=Console.Out;

    public void SetFirstLine(int linenum)
    {
        firstLine=linenum;
    }

    private void DumpLinesUpto(int line)
    {
        while (currentLine<line)
        {
            if (currentLine>=firstLine)
            {
                if (linesThatNeedCommenting.Contains(currentLine))
                {
                    output.WriteLine($"{' ',COMMENTPAD}{FetchTokenLine(currentLine)}");
                }
                else
                {
                    output.WriteLine(originalFileLines[currentLine]);
                }
            }
            currentLine++;
        }
        if (currentLine == line)
            currentLine++;
    }

    private string FetchTokenLine(int lineNum)
    {
        return $"//[{Path.GetFileName(originalFilePath)}:{lineNum:00000}] {originalFileLines[lineNum]}";
    }
    private string FetchTokenLine(Token token)
    {
        return FetchTokenLine(token.getLine());
    }

    public void DumpStringWithOriginalLine(string line, Token root)
    {
        // Pad to fixed width (i choose 60 spaces for now)
        var s = new StringBuilder();
        s.Append(line);
        if (line.Length<COMMENTPAD)
        {
            s.Append(' ', COMMENTPAD-line.Length);
        }
        s.Append(FetchTokenLine(root));
        output.WriteLine(s.ToString());
    }
    
    public void DumpString(string line)
    {
        output.WriteLine(line);
    }

    public void Dump(string outputPath=null)
    {
        if (outputPath!=null)
        {
            output = new StreamWriter(outputPath);
        }
        else
        {
            output = Console.Out;
        }

        CreateOriginalFileLinesDictionary(originalFilePath);

        int moduleLine = name.getLine();
        DumpLinesUpto(moduleLine);

        DumpStringWithOriginalLine($"module m_{name.getValue()}", name);
        DumpStringWithOriginalLine($"(", name);
        int a;
        DumpLinesUpto(inputs[0].getLine());
        for (a=0;a<inputs.Count;a++)
        {
            if (inputIsBi[a]>=0)
            {
                DumpStringWithOriginalLine($"    input    in{inputs[a].getValue()},", inputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    input    {inputs[a].getValue()},", inputs[a]);
            }
        }
        DumpLinesUpto(outputs[0].getLine());
        for (a=0;a<outputs.Count-1;a++)
        {
            if (outputIsBi[a]>=0)
            {
                DumpStringWithOriginalLine($"    output    out{outputs[a].getValue()}, en{outputs[a].getValue()},", outputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    output    {outputs[a].getValue()},", outputs[a]);
            }
        }
        if (outputIsBi[outputs.Count-1]>=0)
        {
            DumpStringWithOriginalLine($"    output    out{outputs[outputs.Count-1].getValue()}, en{outputs[outputs.Count-1].getValue()}", outputs[outputs.Count-1]);
        }
        else
        {
            DumpStringWithOriginalLine($"    output    {outputs[outputs.Count-1].getValue()}", outputs[outputs.Count-1]);
        }
        DumpStringWithOriginalLine($");", name);
        currentLine = outputs[outputs.Count - 1].getLine()+1;

        DumpLinesUpto(define.getLine());

        foreach (var w in wires)
        {
            DumpStringWithOriginalLine($"wire {w.getValue()};",w);
        }

        foreach (var cl in codeLines)
        {
            DumpLinesUpto(cl.instanceName.getLine());
            DumpStringWithOriginalLine(Translate(cl), cl.instanceName);
        }

        DumpLinesUpto(end.getLine());


        for (a=0;a<tristateOutputUse.Length;a++)
        {
            if (tristateOutputUsages[a]!=tristateOutputUse[a])
            {
                throw new NotImplementedException($"Something went wrong and we haven't written the expected number of drivers");
            }
        }

        // The last thing we do in the conversion is to glue all the tristates together into the output drivers
        for (a=0;a<outputs.Count;a++)
        {
            if (outputIsBi[a]>=0)
            {
                // We need to glue all the signals together that make up this output and enable pair
                // Output signal
                var s = new StringBuilder();
                s.Append($"assign out{outputs[a].getValue()} = ");
                for (int c=0;c<tristateOutputUsages[a];c++)
                {
                    s.Append($"(drv{c}_out{outputs[a].getValue()} & drv{c}_en{outputs[a].getValue()})");
                    if (c == tristateOutputUsages[a]-1)
                    {
                        s.Append(";");
                    }
                    else
                    {
                        s.Append(" | ");
                    }
                }
                DumpString(s.ToString());
                // Enable signal
                s = new StringBuilder();
                s.Append($"assign en{outputs[a].getValue()} = ");
                for (int c=0;c<tristateOutputUsages[a];c++)
                {
                    s.Append($"drv{c}_en{outputs[a].getValue()}");
                    if (c == tristateOutputUsages[a]-1)
                    {
                        s.Append(";");
                    }
                    else
                    {
                        s.Append(" | ");
                    }
                }
                DumpString(s.ToString());
            }
        }

        DumpStringWithOriginalLine("endmodule", end);

        output.Flush();
    }

    public string FileName => $"m_{name.getValue()}.sv";
}
