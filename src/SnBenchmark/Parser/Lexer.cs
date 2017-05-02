using System;

namespace SnBenchmark.Parser
{
    internal class Lexer
    {
        private readonly string[] _src;
        private int _lineIndex;

        public Lexer(string src)
        {
            _src = src.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public Token CurrentToken { get; private set; }
        public void NextToken()
        {
            if (_lineIndex >= _src.Length)
            {
                CurrentToken = Token.Eof;
                return;
            }

            var line = _src[_lineIndex++];
            var trimmed = line.Trim();

            if (trimmed.StartsWith(Token.Comment))
            {
                CurrentToken = new Token { Type = TokenType.Comment, Value = trimmed.Substring(Token.Comment.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.Wait))
            {
                CurrentToken = new Token { Type = TokenType.Wait, Value = trimmed.Substring(Token.Wait.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.Req))
            {
                CurrentToken = new Token { Type = TokenType.Request, Value = trimmed.Substring(Token.Req.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.Data))
            {
                CurrentToken = new Token { Type = TokenType.Data, Value = trimmed.Substring(Token.Data.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.Speed))
            {
                CurrentToken = new Token { Type = TokenType.Speed, Value = trimmed.Substring(Token.Speed.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.Var))
            {
                CurrentToken = new Token { Type = TokenType.Variable, Value = trimmed.Substring(Token.Var.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(Token.PathSet))
            {
                CurrentToken = new Token { Type = TokenType.PathSet, Value = trimmed.Substring(Token.PathSet.Length).TrimStart() };
                return;
            }

            CurrentToken = new Token { Type = TokenType.Unparsed, Value = line.Substring(Token.Data.Length) };
        }
    }
}
