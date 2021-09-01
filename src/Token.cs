using System.Text;

public class Token 
{
	private TokenType type;
	public TokenType getType() {
		return type;
	}
	
	private string value = null;
	public string getValue() {
		return value;
	}

	private int line;
	public int getLine() {
		return line;
	}
	
	public Token(TokenType type, int line) 
    {
        this.type = type;
        this.line = line;
	}
	public Token(TokenType type, int line, string value) 
    {
		this.type = type;
		this.line = line;
		this.value = value;
	}

	public override string ToString() 
    {
        var s = new StringBuilder();
        s.Append($"{line:00000}");
        s.Append($" {type.ToString()}");
		if (value != null) 
        {
            s.Append($"[{value}]");
		}
		return s.ToString();
	}
}
