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

    void RegisterFunction(string translate, int numInputs, int numOutputs, params string[] aliases)
    {
        foreach (var a in aliases)
        {
            functions.Add(a,new Function { name = a, numInputs=numInputs, numOutputs=numOutputs, translateAs=translate});
        }
    }

    void RegisterFunctions()
    {
        functions=new Dictionary<string, Function>();

        RegisterFunction("assign @O0 = @I0;", 1, 1, "B1A", "B3A");
        RegisterFunction("assign @O0 = ~@I0;", 1, 1, "N1A", "N1B", "N1C");
        RegisterFunction("assign @O0 = @I0 | @I1;", 2, 1, "OR2A");
        RegisterFunction("assign @O0 = @I0 & @I1;", 2, 1, "AND2A");
        RegisterFunction("assign @O0 = ~(@I0 | @I1);", 2, 1, "NR2A", "NR2C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1);", 2, 1, "ND2A", "ND2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2);", 3, 1, "NR3A");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2);", 3, 1, "ND3A", "ND3C");
        RegisterFunction("assign @O0 = @I0 & @I1 & @I2;", 3, 1, "AND3A");
        RegisterFunction("assign @O0 = ~((@I0 & @I1)|(@I2 & @I3));", 4, 1, "AO2A", "AO2C");
        RegisterFunction("assign @O0 = ~(@I0 | @I1 | @I2 | @I3);", 4, 1, "NR4A", "NR4C");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3);", 4, 1, "ND4A", "ND4C");
        RegisterFunction("assign @O0 = ~(@I0 & ~(@I1 & @I2 & @I3 & @I4));", 5, 1, "N4AND");   //translated from MODULE in IODEC
        RegisterFunction("assign @O0 = @I0 | @I1 | @I2 | @I3 | @I4;", 5, 1, "OR5A");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4);", 5, 1, "ND5A");
        RegisterFunction("assign @O0 = ~(@I0 & @I1 & @I2 & @I3 & @I4 & @I5);", 6, 1, "ND6A");

        // Not synthesizable (will need to be replaced)
        RegisterFunction("assign @O0 = ~@I1 ? @I0 : 1'bZ;", 2, 1, "MACZINVB1");
        

        // Requires Module Implementations
        RegisterFunction($"LD1A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.en(@I1));", 2,2, "LD1A");
        RegisterFunction($"FD2A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.rL(@I2));", 3,2, "FD2A");
        RegisterFunction($"FD4A @N_inst (.q(@O0),.qL(@O1),.d(@I0),.clk(@I1),.sL(@I2));", 3,2, "FD4A");
        RegisterFunction($"JK @N_inst (.q(@O0),.qL(@O1),.j(@I0),.k(@I1),.r(@I2),.clk(@I3));", 4,2, "JK");
    }

    string Translate(Code code)
    {
        if (functions.TryGetValue(code.functionName.getValue(), out var func))
        {
            if (code.inputs.Count!=func.numInputs)
                throw new ParseException(code.instanceName.getLine(), $"Wrong Number Inputs for {code.functionName.getValue()}!");
            if (code.outputs.Count!=func.numOutputs)
                throw new ParseException(code.instanceName.getLine(), $"Wrong Number Outputs for {code.functionName.getValue()}!");

            var b = new StringBuilder();

            int special=0;
            int value=0;
            foreach (var s in func.translateAs)
            {
                switch (special)
                {
                    case 0:
                        if (s=='@')
                        {
                            special=1;
                            continue;
                        }
                        b.Append(s);
                        continue;
                    case 1:
                        if (s=='I')
                        {
                            special=2;
                            value=0;
                            continue;
                        }
                        if (s=='O')
                        {
                            special=3;
                            value=0;
                            continue;
                        }
                        if (s=='N')
                        {
                            b.Append(code.instanceName.getValue());
                            special=0;
                            continue;
                        }
                        throw new NotImplementedException($"Unhandled special");
                    case 2:
                    case 3:
                        if (s>='0' && s<='9')
                        {
                            value=value*10;
                            value+=s-'0';
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
                            special=0;
                        }
                        continue;
                }
            }

            return b.ToString();
        }
        else
        {
            throw new ParseException(code.instanceName.getLine(), $"Unknown Function for {code.functionName.getValue()}!");
        }
    }

    struct Code
    {
        public Token instanceName;
        public List<Token> outputs;
        public Token functionName;
        public List<Token> inputs;
    }

    Token name;
    List<Token> inputs;
    bool[] inputIsBi;
    List<Token> outputs;
    bool[] outputIsBi;
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
    }

    private void PassMakeBidirectional()
    {
        // just do it the lazy way
        inputIsBi = new bool [inputs.Count];
        outputIsBi = new bool [outputs.Count];

        for (int i1 = 0; i1 < inputs.Count; i1++)
        {
            Token i = inputs[i1];
            for (int i2 = 0; i2 < outputs.Count; i2++)
            {
                Token o = outputs[i2];
                if (i.getValue() == o.getValue())
                {
                    inputIsBi[i1]=true;
                    outputIsBi[i2]=true;
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
        RegisterFunctions();

        PassMakeBidirectional();
        PassConstructWireList();

        CreateOriginalFileLinesDictionary(originalFilePath);

        int moduleLine = name.getLine();
        DumpLinesUpto(moduleLine);

        DumpStringWithOriginalLine($"module m_{name.getValue()}", name);
        DumpStringWithOriginalLine($"(", name);
        int a;
        DumpLinesUpto(inputs[0].getLine());
        for (a=0;a<inputs.Count;a++)
        {
            if (inputIsBi[a])
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
            if (outputIsBi[a])
            {
                DumpStringWithOriginalLine($"//    output    {outputs[a].getValue()},", outputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    output    {outputs[a].getValue()},", outputs[a]);
            }
        }
        if (outputIsBi[outputs.Count-1])
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
