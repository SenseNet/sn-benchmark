using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnBenchmark
{
    class Lexer
    {
        private const string TOKEN_REQ = "REQ:";
        private const string TOKEN_DATA = "DATA:";
        private const string TOKEN_WAIT = "WAIT:";
        private const string TOKEN_COMMENT = ";";

        string[] _src;
        int _lineIndex;

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

            if (trimmed.StartsWith(TOKEN_COMMENT))
            {
                CurrentToken = new Token { Type = TokenType.Comment, Value = trimmed.Substring(TOKEN_COMMENT.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(TOKEN_WAIT))
            {
                CurrentToken = new Token { Type = TokenType.Wait, Value = trimmed.Substring(TOKEN_WAIT.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(TOKEN_REQ))
            {
                CurrentToken = new Token { Type = TokenType.Request, Value = trimmed.Substring(TOKEN_REQ.Length).TrimStart() };
                return;
            }

            if (trimmed.StartsWith(TOKEN_DATA))
            {
                CurrentToken = new Token { Type = TokenType.Data, Value = trimmed.Substring(TOKEN_DATA.Length).TrimStart() };
                return;
            }

            CurrentToken = new Token { Type = TokenType.Unparsed, Value = line.Substring(TOKEN_DATA.Length) };
        }
    }

    enum TokenType { Comment, Eof, Request, Data, Wait, Unparsed }
    class Token
    {
        public static Token Eof = new Token { Type = TokenType.Eof };

        public TokenType Type { get; set; }
        public string Value { get; set; }
        public override string ToString()
        {
            return string.Format("{0}: {1}", Type, Value);
        }
    }

    class ProfileParser
    {
        Lexer _lexer;
        List<BenchmarkActionExpression> _actions;

        public ProfileParser(string src)
        {
            _lexer = new Lexer(src);
            _lexer.NextToken();
        }

        public List<BenchmarkActionExpression> Parse()
        {
            _actions = new List<BenchmarkActionExpression>();
            BenchmarkActionExpression parsedAction;
            while (true)
            {
                //_lexer.NextToken();
                var token = _lexer.CurrentToken;
                parsedAction = null;
                switch (token.Type)
                {
                    case TokenType.Comment: parsedAction = ParseComment(token); break;
                    case TokenType.Request: parsedAction = ParseRequest(token); break;
                    case TokenType.Wait: parsedAction = ParseWait(token); break;
                    case TokenType.Unparsed: throw new ApplicationException("Unparsed line: " + token.Value);
                    case TokenType.Eof: return _actions;
                }

                if (parsedAction != null)
                    _actions.Add(parsedAction);
            }
        }

        private BenchmarkActionExpression ParseComment(Token token)
        {
            _lexer.NextToken();
            return null;
        }

        private BenchmarkActionExpression ParseWait(Token token)
        {
            var src = token.Value;
            int milliseconds;
            if (!int.TryParse(src.Trim(), out milliseconds))
                throw new ApplicationException("Invalid wait: " + src);
            _lexer.NextToken();
            return new WaitExpression(milliseconds);
        }

        private BenchmarkActionExpression ParseRequest(Token token)
        {
            var src = token.Value;
            string httpMethod;
            string url;
            ParseRequestHead(src, out httpMethod, out url);

            _lexer.NextToken();

            var requestData = ParseRequestData(_lexer.CurrentToken);
            if(requestData != null)
                _lexer.NextToken();

            return new RequestExpression(url, httpMethod, requestData);
        }

        string[] _httpVerbs = new[] { "DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT" };

        private void ParseRequestHead(string src, out string httpMethod, out string url)
        {
            src = src.Trim();

            httpMethod = null;
            foreach (var httpVerb in _httpVerbs)
            {
                var verb = httpVerb + " ";
                if (src.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                {
                    httpMethod = httpVerb;
                    src = src.Substring(verb.Length).Trim();
                    break;
                }
            }
            url = src;
        }

        private string ParseRequestData(Token token)
        {
            if (token.Type != TokenType.Data)
                return null;

            var sb = new StringBuilder(token.Value);

            _lexer.NextToken();
            var currentToken = _lexer.CurrentToken;
            while (currentToken.Type == TokenType.Unparsed)
            {
                sb.Append(currentToken.Value);
                _lexer.NextToken();
            }

            return sb.ToString();
        }

    }
}
