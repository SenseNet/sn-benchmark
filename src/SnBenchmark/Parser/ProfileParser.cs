using SnBenchmark.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SnBenchmark.Parser
{
    internal class ProfileParser
    {
        private readonly Lexer _lexer;
        private readonly List<BenchmarkActionExpression> _actions;
        private readonly string _location;
        private readonly List<string> _speedItems;

        /// <summary>
        /// Initializes a new instance of the ProfileParser class.
        /// </summary>
        /// <param name="src">Profile script definition file contents.</param>
        /// <param name="location">Path of the profile.</param>
        /// <param name="speedItems">Speed names used in the profile.</param>
        public ProfileParser(string src, string location, List<string> speedItems)
        {
            _lexer = new Lexer(src);
            _lexer.NextToken();
            _location = location;
            _speedItems = speedItems;
            _actions = new List<BenchmarkActionExpression>();
        }

        /// <summary>
        /// Returns a list of benchmark actions in the profile script that was used to initialize the parser.
        /// </summary>
        public List<BenchmarkActionExpression> Parse()
        {
            _actions.Clear();
            while (true)
            {
                var token = _lexer.CurrentToken;
                BenchmarkActionExpression parsedAction;
                switch (token.Type)
                {
                    case TokenType.Comment: parsedAction = ParseComment(token); break;
                    case TokenType.Request: parsedAction = ParseRequest(token); break;
                    case TokenType.Wait: parsedAction = ParseWait(token); break;
                    case TokenType.Variable: parsedAction = ParseVariable(token); break;
                    case TokenType.PathSet: parsedAction = ParsePathSet(token); break;
                    case TokenType.Upload: parsedAction = ParseUpload(token); break;
                    case TokenType.Unparsed: throw new ApplicationException("Unparsed line: " + token.Value);
                    case TokenType.Eof: return _actions;
                    case TokenType.Data:
                    case TokenType.Speed:
                        throw new ApplicationException($"Unexpected token: {token.Type}: {token.Value}");
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (parsedAction != null)
                    _actions.Add(parsedAction);
            }
        }

        // ReSharper disable once UnusedParameter.Local
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

        private BenchmarkActionExpression ParseVariable(Token token)
        {
            var src = token.Value;

            var x = src.Split('=');
            if (x.Length != 2)
                throw new ApplicationException("Syntax error: " + src);

            var name = x[0].Trim();
            if (name.Length < 2)
                throw new ApplicationException("Syntax error: variable name must be at least 2 characters long: " + src);

            if (name[0] != VariableExpression.VariableStart)
                throw new ApplicationException("Syntax error: variable name must start with '" + VariableExpression.VariableStart + "': " + src);

            if (name.Substring(1).Contains(VariableExpression.VariableStart))
                throw new ApplicationException("Syntax error: invalid variable name: " + src);

            var y = x[1].Split('.');
            var objectName = y[0].Trim();
            var propertyPath = y.Skip(1).Select(p=>p.Trim()).ToArray();

            _lexer.NextToken();

            return new VariableExpression(name, objectName, propertyPath);
        }
        private BenchmarkActionExpression ParsePathSet(Token token)
        {
            var src = token.Value;

            var p = src.IndexOf(' ');
            if (p < 0)
                throw new ApplicationException("Syntax error: must starts with a name followed by a definition.");

            var name = src.Substring(0, p);
            var definition = src.Substring(p);

            _lexer.NextToken();

            return new PathSetExpression(name.Trim(), definition.Trim());
        }
        private BenchmarkActionExpression ParseUpload(Token token)
        {
            var src = token.Value;
            var p = src.IndexOf(" /Root", StringComparison.OrdinalIgnoreCase);
            if (p < 0)
                throw new ApplicationException("Syntax error: must starts with a global or local filesystem path followed by a repository path.");

            var source = src.Substring(0, p).Trim();
            var target = src.Substring(p).Trim();

            _lexer.NextToken();

            var speed = ParseSpeed(_lexer.CurrentToken);

            return new UploadExpression(source, target, _location, speed);
        }

        private BenchmarkActionExpression ParseRequest(Token token)
        {
            var src = token.Value;
            string httpMethod;
            string url;
            ParseRequestHead(src, out httpMethod, out url);

            _lexer.NextToken();

            var requestData = ParseRequestData(_lexer.CurrentToken);

            var speed = ParseSpeed(_lexer.CurrentToken);

            return new RequestExpression(url, httpMethod, requestData, speed);
        }

        private readonly string[] _httpVerbs = { "DELETE", "GET", "HEAD", "OPTIONS", "PATCH", "POST", "PUT" };

        private void ParseRequestHead(string src, out string httpMethod, out string url)
        {
            src = src.Trim();

            httpMethod = null;
            foreach (var httpVerb in _httpVerbs)
            {
                var verb = httpVerb + " ";
                if (!src.StartsWith(verb, StringComparison.OrdinalIgnoreCase))
                    continue;
                httpMethod = httpVerb;
                src = src.Substring(verb.Length).Trim();
                break;
            }
            url = src;
        }

        private string ParseSpeed(Token token)
        {
            if (token.Type != TokenType.Speed)
                return RequestExpression.NormalSpeed;

            var speed = token.Value.Trim();
            _lexer.NextToken();

            if (string.IsNullOrEmpty(speed))
                speed = RequestExpression.NormalSpeed;

            speed = speed.ToUpper();
            if (!_speedItems.Contains(speed))
                _speedItems.Add(speed);

            return speed;
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
                currentToken = _lexer.CurrentToken;
            }

            return sb.ToString();
        }
    }
}
