using System.Text;

public class Token 
{
	private TokenType type;
	public TokenType Type => type;
	
	private string value;
	public string Value => value;

	private int line;
	public int Line => line;
	
	public Token(TokenType type, int line, string value=null) 
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
