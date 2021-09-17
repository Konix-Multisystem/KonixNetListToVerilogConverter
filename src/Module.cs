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

    public Module()
    {
        functions=new Dictionary<string, Function>();
        modules=new Dictionary<string, Module>();
    }

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

    void RegisterFunctions()
    {
        // ENA 2 input exclusive nor
        RegisterFunction("assign @O0 = @I0;", 1, 1, "B3A");                                 // B3A is non inverting power buffer
        RegisterFunction("assign @O0 = ~@I0;", 1, 1, "B1A", "N1A", "N1B", "N1C", "N1D");    // B1A is an inverting power buffer
        RegisterFunction("assign @O0 = @I0 ^ @I1;", 2, 1, "EOA", "EOB");
        RegisterFunction("assign @O0 = @I0 | @I1;", 2, 1, "OR2A", "OR2C");
        RegisterFunction("assign @O0 = @I0 & @I1;", 2, 1, "AND2A", "AND2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1);", 2, 1, "NR2A", "NR2B", "NR2C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1);", 2, 1, "ND2A", "ND2B", "ND2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2);", 3, 1, "NR3A", "NR3C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2);", 3, 1, "ND3A", "ND3B", "ND3C");
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2;", 3, 1, "OR3A");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2;", 3, 1, "AND3A", "AND3C");
        RegisterFunction("assign @O0 = ~((@I0 & @I1)|(@I2 & @I3));", 4, 1, "AO2A", "AO2B", "AO2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3);", 4, 1, "NR4A", "NR4B", "NR4C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3);", 4, 1, "ND4A", "ND4C");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3;", 4, 1, "AND4A");
        RegisterFunction("assign @O0 = ~(@I0 & ~(@I1 & @I2 & @I3 & @I4));", 5, 1, "N4AND");   //translated from MODULE in IODEC
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4;", 5, 1, "OR5A");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4;", 5, 1, "AND5A");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4);", 5, 1, "ND5A");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5);", 6, 1, "ND6A", "ND6B", "ND6C");
        RegisterFunction("assign @O0 = ~((@I0 & @I1) | (@I2 & @I3) | (@I4 & @I5));", 6, 1, "AO11A");    // translated from MACROS.HDL
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7);", 8, 1, "ND8A");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3 | @I4 | @I5 | @I6 | @I7);", 8, 1, "NR8A");
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4 | @I5 | @I6 | @I7;", 8, 1, "MACAND8");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7 & @I8 & @I9;", 10, 1, "AND10");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2 & @I3 & @I4 & @I5 & @I6 & @I7 & @I8 & @I9 & @I10;", 11, 1, "AND11");
        RegisterFunction("assign @O0 = (@I0 ~^ @I9) & (@I1 ~^ @I10) & (@I2 ~^ @I11) & (@I3 ~^ @I12) & (@I4 ~^ @I13) & (@I5 ~^ @I14) & (@I6 ~^ @I15) & (@I7 ~^ @I16) & (@I8 ~^ @I17) & @I18;", 9+9+1, 1, "EQU9");

        RegisterFunction("assign @O0 = ~((~(@I0 & @I1)) & (~(@I2 & @I3)));", 4, 1, "MUX2");

        // Multiple out, but short enough to not need function
        RegisterFunction("assign @O0 = ~@I0; assign @O1 = ~@I1;", 2, 2, "MACINV2");
        RegisterFunction("assign @O0 = @I0 ^ @I1; assign @O1 = @I0 & @I1;", 2, 2, "HALFADD");

        // Not 100%

        RegisterFunction("assign @O0 = ~(@I2 ? @I1 : @I0);", 3, 1, "MX21LB");   // Inverting output?



        // Not synthesizable (will need to be replaced)
        RegisterFunction("assign @O0 = ~@I1 ? @I0 : 1'bZ;", 2, 1, "MACZINVB1");
        RegisterFunction("assign @O0 = @I1 ? @I0 : 1'bZ;", 2, 1, "BTS4A", "BTS4B", "BTS4C");         // 5 is inverting output,4 is normal?
        RegisterFunction("assign @O0 = @I1 ? (~@I0) : 1'bZ;", 2, 1, "BTS5A");         // 5 is inverting output,4 is normal?
        RegisterFunction($"ZTLATCH1 @N_inst (.QB(@O0),.D(@I1),.CLK(@I2),.ENL(@I3));", 4,1, "ZTLATCH1"); // TODO add verify that I0==O0
        

        // Requires Module Implementations
        RegisterFunction($"SR @N_inst (.Q(@O0),.QL(@O1),.S(@I0),.R(@I1));", 2,2, "SR");
        RegisterFunction($"LD1A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD1A");
        RegisterFunction($"LD2A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD2A");
        RegisterFunction($"FD1A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1));", 2,2, "FD1A");
        RegisterFunction($"FULLADD @N_inst (.Q(@O0),.CO(@O1),.A(@I0),.B(@I1),.CI(@I2));", 3,2, "FULLADD");
        RegisterFunction($"FD2A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2));", 3,2, "FD2A");
        RegisterFunction($"FD4A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.sL(@I2));", 3,2, "FD4A");
        RegisterFunction($"JK @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.r(@I2),.clk(@I3));", 4,2, "JK");
        RegisterFunction($"FJK2A @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.clk(@I2),.rL(@I3));", 4,2, "FJK2A");
        RegisterFunction($"SYNCNT0 @N_inst (.Q(@O0),.QB(@O1),.D(@I0),.CLK(@I1),.CLR(@I2),.LDL(@I3));", 4,2, "SYNCNT0");
        RegisterFunction($"SYNCNT @N_inst (.Q(@O0),.QB(@O1),.CO(@O2),.D(@I0),.CLK(@I1),.CLR(@I2),.LDL(@I3),.CI(@I4));", 5,3, "SYNCNT");
        RegisterFunction($"MUX4 @N_inst (.Q(@O0),.A(@I0),.B(@I1),.D_0(@I2),.D_1(@I3),.D_2(@I4),.D_3(@I5));", 6,1, "MUX4");
        RegisterFunction($"LFUBIT @N_inst (.DOUT(@O0),.SRCD(@I0),.DSTD(@I1),.LFUC_0(@I2),.LFUC_1(@I3),.LFUC_2(@I4),.LFUC_3(@I5));", 6,1, "LFUBIT");
        RegisterFunction($"LSCNTEL @N_inst (.Q(@O0),.QL(@O1),.CO(@O2),.D(@I0),.LD(@I1),.LDL(@I2),.CLK(@I3),.CI(@I4),.RSTL(@I5));", 6,3, "LSCNTEL");

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

    private static string TranslateModule(Code code, Module module)
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

            b.Append($".{module.inputs[i1].getValue()}({i.getValue()}),");
        }
        for (int o1 = 0; o1 < code.outputs.Count; o1++)
        {
            Token o = code.outputs[o1];

            if (module.outputIsBi[o1]>=0)
            {
                if (code.inputs[module.outputIsBi[o1]].getValue() != code.outputs[o1].getValue())
                {
                    throw new ParseException(code.instanceName.getLine(), "bidirectional pin with different input than output!");
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

    private static string TranslateFunction(Code code, Function func)
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
                            b.Append($"{code.inputs[value].getValue()}");
                        else if (special == 3)
                            b.Append($"{code.outputs[value].getValue()}");
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

    private enum State
    {
        ExpectingCompile,
        ExpectingDirectory,
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

    public void Parse(Tokenizer tokenizer)
    {
        // For now we only parse the FIRST module
        tokenizer.reset();

        var state = State.ExpectingCompile;
        linesThatNeedCommenting=new HashSet<int>();
        codeLines=new List<Code>();
        
        while (!tokenizer.matchTokens(TokenType.END, TokenType.MODULE, TokenType.SEMICOLON))
        {
            switch (state)
            {
                case State.ExpectingCompile:
                    if (!tokenizer.matchTokens(TokenType.COMPILE,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected COMPILE;");
                    MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    state=State.ExpectingDirectory;
                    break;
                case State.ExpectingDirectory:
                    if (!tokenizer.matchTokens(TokenType.DIRECTORY,TokenType.MASTER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected DIRECTORY MASTER;");
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=State.ExpectingModule;
                    break;
                case State.ExpectingModule:
                    if (!tokenizer.matchTokens(TokenType.MODULE,TokenType.IDENTIFIER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected MODULE <name>;");
                    this.name = tokenizer.nextToken(1);
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=State.ExpectingInputs;
                    break;
                case State.ExpectingInputs:
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
                    state=State.ExpectingOutputs;
                    break;
                case State.ExpectingOutputs:
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
                    state=State.ExpectingLevel;
                    break;
                case State.ExpectingLevel:
                    if (!tokenizer.matchTokens(TokenType.LEVEL,TokenType.FUNCTION,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected LEVEL FUNCTION;");
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=State.ExpectingDefine;
                    break;
                case State.ExpectingDefine:
                    if (!tokenizer.matchTokens(TokenType.DEFINE))
                        throw new ParseException(tokenizer.nextToken().getLine(), "Expected DEFINE");
                    define=tokenizer.nextToken();
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=State.ExpectingCode;
                    break;
                case State.ExpectingCode:
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
                    while (tokenizer.matchTokens(TokenType.COMMA, TokenType.IDENTIFIER))
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
                    state=State.ExpectingCode;
                    break;
            }
        }
        end = tokenizer.nextToken();
        MarkAsCodeLine(tokenizer,3);
        tokenizer.consumeToken(3);

        // Now do the work we need to translate the modules etc.

        RegisterFunctions();

        PassMakeBidirectional();
        PassConstructWireList();
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

    private void DumpLinesUpto(int line)
    {
        while (currentLine<line)
        {
            if (linesThatNeedCommenting.Contains(currentLine))
            {
                Console.WriteLine($"{' ',COMMENTPAD}//[{currentLine:00000}] {originalFileLines[currentLine]}");
            }
            else
            {
                Console.WriteLine(originalFileLines[currentLine]);
            }
            currentLine++;
        }
        if (currentLine == line)
            currentLine++;
    }

    private string FetchTokenLine(Token token)
    {
        return $"//[{token.getLine():00000}] {originalFileLines[token.getLine()]}";
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
        Console.WriteLine(s.ToString());
    }

    public void Dump(string originalFilePath)
    {
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
                DumpStringWithOriginalLine($"    inout    {inputs[a].getValue()},", inputs[a]);
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
                DumpStringWithOriginalLine($"//    output    {outputs[a].getValue()},", outputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    output    {outputs[a].getValue()},", outputs[a]);
            }
        }
        if (outputIsBi[outputs.Count-1]>=0)
        {
            DumpStringWithOriginalLine($"//    output    {outputs[outputs.Count-1].getValue()}", outputs[outputs.Count-1]);
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
        DumpStringWithOriginalLine("endmodule", end);
    }
}
