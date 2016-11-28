namespace SnBenchmark.Parser
{
    internal enum TokenType { Comment, Eof, Request, Data, Speed, Variable, Wait, Unparsed }
    internal class Token
    {
        public const string Req = "REQ:";
        public const string Data = "DATA:";
        public const string Speed = "SPEED:";
        public const string Var = "VAR:";
        public const string Wait = "WAIT:";
        public const string Comment = ";";

        public static Token Eof = new Token { Type = TokenType.Eof };

        public TokenType Type { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return $"{Type}: {Value}";
        }
    }
}
