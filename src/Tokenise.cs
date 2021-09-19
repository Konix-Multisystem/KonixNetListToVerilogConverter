using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class Tokenizer 
{
	private List<Token> tokens = new List<Token>();
	private int position = 0;
	
    public Tokenizer()
    {
    }

	public Token nextToken(int offset) 
    {
		int p = position + offset;
		if ( (p >= 0) && (p < tokens.Count) ) 
        {
			return tokens[p];
		}
		return new Token(TokenType.EOF, -1);
	}

	public Token nextToken() 
    {
		return nextToken(0);
	}

	public void consumeToken(int count=1) 
    {
		position += count;
	}
	public void reset() 
    {
		position = 0;
	}
	
	public bool matchTokens(params TokenType[] args) 
    {
		return matchTokens(0, args);
	}

	public bool matchTokens(int offset, params TokenType[] args) 
    {
		for(int k = 0; k < args.Length; k++) 
        {
			if (!(args[k] == nextToken(offset + k).getType()))
				return false;
		}
		return true;
	}

	private bool inComment = false;
	private bool inPreproc = false;
	
	public void tokenize(StreamReader input)
    {
		string line;
		int lineNo = 0;
		
		inComment = false;
		inPreproc = false;
		
		try 
        {
			while( (line = input.ReadLine()) != null ) 
            {
				lineNo++;
				tokenizeLine(lineNo, line);
			}
		} 
        catch (Exception ex) 
        {
			throw ex;
		} 
	}

	private TokenType StringToTokenType(string s)
	{
		switch (s)
		{
			case "COMPILE":
				return TokenType.COMPILE;
			case "DIRECTORY":
				return TokenType.DIRECTORY;
			case "MASTER":
				return TokenType.MASTER;
			case "MODULE":
				return TokenType.MODULE;
			case "INPUTS":
				return TokenType.INPUTS;
			case "OUTPUTS":
				return TokenType.OUTPUTS;
			case "LEVEL":
				return TokenType.LEVEL;
			case "FUNCTION":
				return TokenType.FUNCTION;
			case "DEFINE":
				return TokenType.DEFINE;
			case "END":
				return TokenType.END;
		}
		return TokenType.NONE;
	}
	
	private TokenType SubStringToTokenType(string s)
	{
		switch (s[0])
		{
			case '=':
				return TokenType.ASSIGN;
			case ';':
				return TokenType.SEMICOLON;
			case '(':
				return TokenType.LPAREN;
			case ')':
				return TokenType.RPAREN;
			case ',':
				return TokenType.COMMA;
		}
		return TokenType.NONE;
	}

	private void tokenizeLine(int lineNo, string line) 
    {
		Regex pIdentifier = new Regex("^[a-zA-Z][a-zA-Z0-9\\\\_]*");
		Regex pNumber = new Regex("^[0-9]+");
		
		while(line.Length > 0) 
        {
			// Strip blank characters
			if ( (line[0] == ' ') || (line[0] == '\t') ) 
            {
				line = line.Substring(1);
				continue;
			}
			// Preprocessor directives, done the quick and dirty way
			if (line.StartsWith("#if")) 
            {
				if (line.IndexOf("verilog") < 0) 
                {
					inPreproc = true;
				}
				return;
			}
			if (line.StartsWith("#endif")) 
            {
				inPreproc = false;
				return;
			}
			if (inPreproc)
				return;
			
			// Comments
			if (line.StartsWith("/*") || line.StartsWith("(*") || inComment) 
            {
				int p = line.IndexOf("*/");
				if (p < 0)
					p = line.IndexOf("*)");
				if (p < 0) 
                {
					inComment = true;
					return;
				} 
                else 
                {
					line = line.Substring(p + 2);
					inComment = false;
					continue;
				}
			}

			// Special cases
			if (line.StartsWith("NC/1/"))
			{
				Token number = new Token(TokenType.NUMBER, lineNo, "1");
				tokens.Add(number);
				line = line.Substring("NC/1/".Length);
				continue;
			}

			
			// Identifiers
            var match = pIdentifier.Match(line);
			if (match.Success) 
            {
				string identifier = match.Value;
				line = line.Substring(identifier.Length);
				
				// Search for keywords
				var tokenKind = StringToTokenType(identifier);
				if (tokenKind != TokenType.NONE)
				{
					Token token = new Token(tokenKind, lineNo);
					tokens.Add(token);
				}
				else
                {
                    Token token = new Token(TokenType.IDENTIFIER, lineNo, identifier);
					tokens.Add(token);
				}
			}
            else
            {
                var mNumber = pNumber.Match(line);
				if (mNumber.Success) 
                {
					Token number = new Token(TokenType.NUMBER, lineNo, match.Value);
					tokens.Add(number);
					line = line.Substring(match.Value.Length);
					continue;
				} 
                else 
                {
					// Search for remaining tokens
					var tokenKind = SubStringToTokenType(line);
					if (tokenKind!=TokenType.NONE)
					{
						Token token = new Token(tokenKind, lineNo);
						tokens.Add(token);
						line=line.Substring(1);
					}
					else
						throw new Exception("Invalid expression at line " + lineNo);
				}
			}
		}
	}
	
	public void dumpTokens() 
    {
		foreach (var t in tokens) 
        {
			Console.WriteLine(t);
		}
	}
	
	public void dumpRemainingTokens() 
    {
		for(int k = position; k < tokens.Count; k++) 
        {
            Console.WriteLine(tokens[k]);
		}
	}
	
	public static void Test(string path) 
    {
		Tokenizer tokenizer = new Tokenizer();
		try 
        {
		    var input = new StreamReader(path);
			tokenizer.tokenize(input);
		} 
        catch (Exception ex) 
        {
            Console.WriteLine(ex.StackTrace);
		}
		tokenizer.dumpTokens();
	}
	
}
