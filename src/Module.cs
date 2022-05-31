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

    public void RegisterFunction(string translate, int numInputs, int numOutputs, params string[] aliases)
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
            if (m.name.Value != name.Value)
            {
                RegisterModule(m, m.name.Value);
            }
        }
    }

    string Translate(Code code, IModification extensions)
    {
        if (functions.TryGetValue(code.functionName.Value, out var func))
        {
            return TranslateFunction(code, func);
        }
        else if (modules.TryGetValue(code.functionName.Value, out var module))
        {
            return TranslateModule(code, module, extensions);
        }
        else
        {
            throw new ParseException(code.instanceName.Line, $"Unknown Function for {code.functionName.Value}!");
        }
    }

    private string TranslateModule(Code code, Module module, IModification extensions)
    {
        // Translation would be simple here except for the caveat of modules with bidirectional pins

        // So first we need to verify the number of inputs and outputs
        if (code.inputs.Count != module.inputs.Count)
            throw new ParseException(code.instanceName.Line, $"Wrong Number Inputs for {code.functionName.Value}!");
        if (code.outputs.Count != module.outputs.Count)
            throw new ParseException(code.instanceName.Line, $"Wrong Number Outputs for {code.functionName.Value}!");

        // So now, if the module has bidirection pins, we should validate that the input and output names match - we do this as we translate

        var b = new StringBuilder();

        b.Append($"m_{module.name.Value} {code.instanceName.Value} (");

        extensions.ModulePreInputs(module.name.Value,code,ref b);

        for (int i1 = 0; i1 < code.inputs.Count; i1++)
        {
            Token i = code.inputs[i1];

            if (module.InputIsBidirectional(module.inputs[i1]))
            {
                if (InputIsBidirectional(i))
                {
                    b.Append($".in{module.inputs[i1].Value}(in{i.Value}),");
                }
                else
                {
                    b.Append($".in{module.inputs[i1].Value}({i.Value}),");
                }
            }
            else
            {
                if (InputIsBidirectional(i))
                {
                    b.Append($".{module.inputs[i1].Value}(in{i.Value}),");
                }
                else
                {
                    b.Append($".{module.inputs[i1].Value}({i.Value}),");
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
                        if (code.inputs[module.outputIsBi[o1]].Value != code.outputs[o1].Value)
                        {
                            throw new ParseException(code.instanceName.Line, "bidirectional pin with different input than output!");
                        }
                    }
                    b.Append($".out{module.outputs[o1].Value}(drv{tristateOutputUse[idx]}_out{o.Value}),");
                    b.Append($".en{module.outputs[o1].Value}(drv{tristateOutputUse[idx]}_en{o.Value})");
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
                b.Append($".{module.outputs[o1].Value}({o.Value})");
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
            throw new ParseException(code.instanceName.Line, $"Wrong Number Inputs for {code.functionName.Value}!");
        if (code.outputs.Count != func.numOutputs)
            throw new ParseException(code.instanceName.Line, $"Wrong Number Outputs for {code.functionName.Value}!");

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
                        b.Append(code.instanceName.Value);
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
                                b.Append($"in{code.inputs[value].Value}");
                            }
                            else
                            {
                                b.Append($"{code.inputs[value].Value}");
                            }
                        }
                        else if (special == 3)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out _))
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to bidirectional output via non tristate driver");
                                //b.Append($"{code.outputs[value].Value}");
                            }
                            else
                            {
                                b.Append($"{code.outputs[value].Value}");
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
                                b.Append($"drv{tristateOutputUse[idx]}_out{code.outputs[value].Value}");
                            }
                            else
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to non bidirectional output is unexpected!");
                                //b.Append($"{code.outputs[value].Value}");
                            }
                        }
                        else if (special == 6)
                        {
                            if (OutputIsBidirectional(code.outputs[value], out var idx))
                            {
                                b.Append($"drv{tristateOutputUse[idx]}_en{code.outputs[value].Value}");
                            }
                            else
                            {
                                // Would need to assign to out and en , but this should be a tristate operation
                                throw new NotImplementedException($"Assign to non bidirectional enable is unexpected!");
                                //b.Append($"{code.outputs[value].Value}");
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
                                //b.Append($"{code.outputs[value].Value}");
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

    public struct Code
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
            linesThatNeedCommenting.Add(tokenizer.nextToken(a).Line);
        }
    }

    private bool InputIsBidirectional(Token token)
    {
        for (int i = 0; i < inputs.Count; i++)
        {
            Token s = inputs[i];

            if (s.Value == token.Value)
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

            if (s.Value == token.Value)
            {
                idx = i;
                return outputIsBi[i]>=0;
            }
        }
        return false;
    }

    public static List<Module> ParseFile(string filepath,IModification extensions)
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
                        throw new ParseException(tokenizer.nextToken().Line, "Expected COMPILE;");
                    module.MarkAsCodeLine(tokenizer,2);
                    tokenizer.consumeToken(2);
                    state=FileState.ExpectingDirectory;
                    break;
                case FileState.ExpectingDirectory:
                    if (!tokenizer.matchTokens(TokenType.DIRECTORY,TokenType.MASTER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected DIRECTORY MASTER;");
                    module.MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=FileState.ExpectingModuleOrEndCompile;
                    break;
                case FileState.ExpectingModuleOrEndCompile:
                    if (tokenizer.matchTokens(TokenType.MODULE,TokenType.IDENTIFIER,TokenType.SEMICOLON))
                    {
                        module.Parse(tokenizer, extensions);
                        if (!extensions.ExcludeModules.Contains(module.name.Value))
                        {
                            modules.Add(module);
                        }
                        module = new Module(filepath);
                        module.SetFirstLine(tokenizer.nextToken().Line);
                    }
                    else if (tokenizer.matchTokens(TokenType.END, TokenType.COMPILE, TokenType.SEMICOLON))
                    {
                        module.MarkAsCodeLine(tokenizer,3);
                        tokenizer.consumeToken(3);
                        state=FileState.ExpectingEnd;
                    }
                    else
                    {
                        throw new ParseException(tokenizer.nextToken().Line, "Expected MODULE <name>; or END COMPILE;");
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

    public void Parse(Tokenizer tokenizer,IModification extensions)
    {
        var state = ModuleState.ExpectingModule;
        codeLines=new List<Code>();
        
        while (!tokenizer.matchTokens(TokenType.END, TokenType.MODULE, TokenType.SEMICOLON))
        {
            switch (state)
            {
                case ModuleState.ExpectingModule:
                    if (!tokenizer.matchTokens(TokenType.MODULE,TokenType.IDENTIFIER,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected MODULE <name>;");
                    this.name = tokenizer.nextToken(1);
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=ModuleState.ExpectingInputs;
                    break;
                case ModuleState.ExpectingInputs:
                    if (!tokenizer.matchTokens(TokenType.INPUTS,TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected INPUTS <name>[,<name>]*;");
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
                        throw new ParseException(tokenizer.nextToken().Line, "Expected INPUTS <name>[,<name>]*;");
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingOutputs;
                    break;
                case ModuleState.ExpectingOutputs:
                    if (!tokenizer.matchTokens(TokenType.OUTPUTS,TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected OUTPUTS <name>[,<name>]*;");
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
                        throw new ParseException(tokenizer.nextToken().Line, "Expected OUTPUTS <name>[,<name>]*;");
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingLevel;
                    break;
                case ModuleState.ExpectingLevel:
                    if (!tokenizer.matchTokens(TokenType.LEVEL,TokenType.FUNCTION,TokenType.SEMICOLON))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected LEVEL FUNCTION;");
                    MarkAsCodeLine(tokenizer,3);
                    tokenizer.consumeToken(3);
                    state=ModuleState.ExpectingDefine;
                    break;
                case ModuleState.ExpectingDefine:
                    if (!tokenizer.matchTokens(TokenType.DEFINE))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected DEFINE");
                    define=tokenizer.nextToken();
                    MarkAsCodeLine(tokenizer,1);
                    tokenizer.consumeToken(1);
                    state=ModuleState.ExpectingCode;
                    break;
                case ModuleState.ExpectingCode:
                    if (!tokenizer.matchTokens(TokenType.IDENTIFIER, TokenType.LPAREN, TokenType.IDENTIFIER))
                        throw new ParseException(tokenizer.nextToken().Line, "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
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
                        throw new ParseException(tokenizer.nextToken().Line, "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
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
                        throw new ParseException(tokenizer.nextToken().Line, "Expected <instance>(<output>[,<output>]*) = <function>(<input>[,<input>]*);");
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

        extensions.RegisterFunctions(this);

        PassMakeBidirectional();
        PassPickupBidirectionalOutputOnly(extensions);
        PassConstructWireList();
        PassConstructOutputTristateMuxWires();
    }

    const int NO_INPUT_PIN=99999999;

    private void PassPickupBidirectionalOutputOnly(IModification extensions)
    {
        // The dsp is full of modules that produce tristate outputs without definining the inputs...
        //so for now, we also convert outputs to bidirectional if they are driven by a bidirectional driver
        foreach (var c in codeLines)
        {
            if (extensions.BidirectionalDrivers.Contains(c.functionName.Value))
            {
                foreach (var co in c.outputs)
                {
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        Token o = outputs[i];
                        if (o.Value == co.Value)
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
                if (i.Value == o.Value)
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
            uniqueWire.Add(i.Value);
        }
        foreach (var o in outputs)
        {
            uniqueWire.Add(o.Value);
        }
        foreach (var cl in codeLines)
        {
            foreach (var i in cl.inputs)
            {
                if (!uniqueWire.Contains(i.Value))
                {
                    uniqueWire.Add(i.Value);
                    wires.Add(i);
                }
            }
            foreach (var o in cl.outputs)
            {
                if (!uniqueWire.Contains(o.Value))
                {
                    uniqueWire.Add(o.Value);
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
                    wires.Add(new Token(o.Type, o.Line, $"drv{a}_out{o.Value}"));
                    wires.Add(new Token(o.Type, o.Line, $"drv{a}_en{o.Value}"));
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
        return FetchTokenLine(token.Line);
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
        var originalLine = FetchTokenLine(root);
        if (originalLine.Contains("/*") && !originalLine.Contains("*/"))    // Handle a hiccup if the original line contained only the start of the comment
        {
            s.Append(originalLine.Substring(0,originalLine.IndexOf("/*")));
            output.WriteLine(s.ToString());
            output.WriteLine(originalLine.Substring(originalLine.IndexOf("/*")));
        }
        else
        {
            s.Append(FetchTokenLine(root));
            output.WriteLine(s.ToString());
        }
    }
    
    public void DumpString(string line)
    {
        output.WriteLine(line);
    }

    public void Dump(string outputPath, IModification extensions)
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

        int moduleLine = name.Line;
        DumpLinesUpto(moduleLine);

        DumpStringWithOriginalLine($"module m_{name.Value}", name);
        DumpStringWithOriginalLine($"(", name);
        int a;
        DumpLinesUpto(inputs[0].Line);

        var preInputMods = extensions.PreInputs(name.Value);
        foreach(var str in preInputMods)
        {
            DumpString(str);
        }

        for (a=0;a<inputs.Count;a++)
        {
            if (inputIsBi[a]>=0)
            {
                DumpStringWithOriginalLine($"    input    in{inputs[a].Value},", inputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    input    {inputs[a].Value},", inputs[a]);
            }
        }
        DumpLinesUpto(outputs[0].Line);
        for (a=0;a<outputs.Count-1;a++)
        {
            if (outputIsBi[a]>=0)
            {
                DumpStringWithOriginalLine($"    output    out{outputs[a].Value}, en{outputs[a].Value},", outputs[a]);
            }
            else
            {
                DumpStringWithOriginalLine($"    output    {outputs[a].Value},", outputs[a]);
            }
        }
        if (outputIsBi[outputs.Count-1]>=0)
        {
            DumpStringWithOriginalLine($"    output    out{outputs[outputs.Count-1].Value}, en{outputs[outputs.Count-1].Value}", outputs[outputs.Count-1]);
        }
        else
        {
            DumpStringWithOriginalLine($"    output    {outputs[outputs.Count-1].Value}", outputs[outputs.Count-1]);
        }
        var postOutputMods = extensions.PostOutputs(name.Value);
        foreach(var str in postOutputMods)
        {
            DumpString(str);
        }
        DumpStringWithOriginalLine($");", name);
        currentLine = outputs[outputs.Count - 1].Line+1;

        DumpLinesUpto(define.Line);

        foreach (var w in wires)
        {
            // Allow dropping of wires (useful if a wire is promoted to an output for instance)
            if (extensions.DropWire(name.Value, w.Value))
            {
                continue;
            }
            DumpStringWithOriginalLine($"wire {w.Value};",w);
        }

        var preCodeLines = extensions.PreCodeLines(name.Value);
        foreach(var str in preCodeLines)
        {
            DumpString(str);
        }

        foreach (var cl in codeLines)
        {
            DumpLinesUpto(cl.instanceName.Line);
            var codeLine = extensions.ReplaceCodeLine(name.Value,cl);
            DumpStringWithOriginalLine(Translate(codeLine, extensions), codeLine.instanceName);
        }

        DumpLinesUpto(end.Line);


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
                s.Append($"assign out{outputs[a].Value} = ");
                for (int c=0;c<tristateOutputUsages[a];c++)
                {
                    s.Append($"(drv{c}_out{outputs[a].Value} & drv{c}_en{outputs[a].Value})");
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
                s.Append($"assign en{outputs[a].Value} = ");
                for (int c=0;c<tristateOutputUsages[a];c++)
                {
                    s.Append($"drv{c}_en{outputs[a].Value}");
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
        
        var beforeEndModule = extensions.BeforeEndModule(name.Value);
        foreach (var str in beforeEndModule)
        {
            DumpString(str);
        }

        DumpStringWithOriginalLine("endmodule", end);

        output.Flush();
    }

    public string FileName => $"m_{name.Value}.sv";
}
