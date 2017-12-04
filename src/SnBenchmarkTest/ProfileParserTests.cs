using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnBenchmark.Expression;
using SnBenchmark.Parser;

namespace SnBenchmarkTest
{
    [TestClass]
    public class ProfileParserTests
    {
        private const string ProfileLocation = "c:\\fakelocation";

        #region Tokenizer tests

        [TestMethod]
        public void Profile_TokenizeComment()
        {
            var value = "three word comment";
            var src = $"; {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Comment, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
            
            Assert.AreEqual($"Comment: {value}", lexer.CurrentToken.ToString());
        }
        [TestMethod]
        public void Profile_TokenizeRequest()
        {
            var value = "GET /odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var src = $"REQ: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Request, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeRequestData()
        {
            var value = "models=[{\"__ContentType\":\"Memo\",\"Description\":\"asdf qwer\"}]";
            var src = $"DATA: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Data, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeRequestSpeed()
        {
            var value = "Slow";
            var src = $"SPEED: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Speed, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeVariable()
        {
            var value = "@Name = @Response.d.Name";
            var src = $"VAR: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Variable, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeWait()
        {
            var value = "10000";
            var src = $"WAIT: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Wait, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizePathSet()
        {
            var value = "BigFiles Size:>236000 AND TypeIs:File AND InTree:'/Root/Benchmark/Files' .AUTOFILTERS:OFF";
            var src = $"PATHSET: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.PathSet, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeUpload()
        {
            var value = "files\\Test.txt /Root/Benchmark/UploadTest";
            var src = $"UPLOAD: {value}";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Upload, lexer.CurrentToken.Type);
            Assert.AreEqual(value, lexer.CurrentToken.Value);
        }
        [TestMethod]
        public void Profile_TokenizeUnparsed()
        {

            var src = $"Unparsed unchanged text";
            var lexer = new Lexer(src);

            lexer.NextToken();

            Assert.AreEqual(TokenType.Unparsed, lexer.CurrentToken.Type);
            Assert.AreEqual(src, lexer.CurrentToken.Value);
        }

        #endregion

        [TestMethod]
        public void Profile_ParseComment()
        {
            var value = "three word comment";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"; {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(0, result.Count);
        }
        [TestMethod]
        public void Profile_ParseGetRequest()
        {
            var method = "GET";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var value = $"{method} {url}";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"REQ: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.IsNull(reqExp.RequestData);
            Assert.AreEqual("NORMAL", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParsePostRequest()
        {
            var method = "POST";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var value = $"{method} {url}";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"REQ: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.IsNull(reqExp.RequestData);
            Assert.AreEqual("NORMAL", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParseRequestData()
        {
            var method = "POST";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var reqValue = $"{method} {url}";
            var dataValue = @"models=[{""__ContentType"":""Memo"",""Description"":""asdf qwer""}]";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $@"REQ: {reqValue}" + Environment.NewLine
                      + $@"DATA: {dataValue}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.AreEqual(dataValue, reqExp.RequestData);
            Assert.AreEqual("NORMAL", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParseRequestData_MultiLine()
        {
            var method = "POST";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var reqValue = $"{method} {url}";
            var dataValue = @"models=[{""__ContentType"":""Memo"",""Description"":""asdfqwer""}]";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $@"REQ: {reqValue}" + Environment.NewLine
                      + @"DATA: models=[{" + Environment.NewLine
                      + @"    ""__ContentType"":""Memo""," + Environment.NewLine
                      + @"    ""Description"":""asdfqwer""" + Environment.NewLine
                      + @"}]" + Environment.NewLine
                      + @"SPEED: Medium" + Environment.NewLine;
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.AreEqual(dataValue, reqExp.RequestData.Replace(Environment.NewLine, "").Replace(" ", ""));
            Assert.AreEqual("MEDIUM", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParseRequestSpeed()
        {
            var method = "POST";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var reqValue = $"{method} {url}";
            var dataValue = @"models=[{""__ContentType"":""Memo"",""Description"":""asdf qwer""}]";
            var speedValue = "Slow";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $@"REQ: {reqValue}" + Environment.NewLine
                      + $@"DATA: {dataValue}" + Environment.NewLine
                      + $@"SPEED: {speedValue}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.AreEqual(dataValue, reqExp.RequestData);
            Assert.AreEqual("SLOW", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParseRequestSpeedEmpty()
        {
            var method = "GET";
            var url = "/odata.svc/Root/Benchmark?metadata=no&$select=Name";
            var reqValue = $"{method} {url}";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $@"REQ: {reqValue}" + Environment.NewLine
                      + $@"SPEED:";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is RequestExpression);
            var reqExp = (RequestExpression)result[0];
            Assert.AreEqual(method, reqExp.HttpMethod);
            Assert.AreEqual(url, reqExp.Url);
            Assert.AreEqual("NORMAL", reqExp.Speed);
        }
        [TestMethod]
        public void Profile_ParseVariable()
        {
            var value = "@Name = @Response.d.Name";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"VAR: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is VariableExpression);
            var varExp = (VariableExpression)result[0];
            Assert.AreEqual("@Name", varExp.Name);
            Assert.AreEqual("@Response", varExp.ObjectName);
            Assert.AreEqual(2, varExp.PropertyPath.Length);
            Assert.AreEqual("d", varExp.PropertyPath[0]);
            Assert.AreEqual("Name", varExp.PropertyPath[1]);
        }
        [TestMethod]
        public void Profile_ParseWait()
        {
            var value = "10000";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"WAIT: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is WaitExpression);
            Assert.AreEqual(10000, ((WaitExpression)result[0]).Milliseconds);
        }
        [TestMethod]
        public void Profile_ParsePathSet()
        {
            var name = "BigFiles";
            var definition = "Size:>236000 AND TypeIs:File AND InTree:'/Root/Benchmark/Files' .AUTOFILTERS:OFF";
            var value = $"{name} {definition}";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"PATHSET: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is PathSetExpression);
            var psetExp = (PathSetExpression)result[0];
            Assert.AreEqual(name, psetExp.Name);
            Assert.AreEqual(definition, psetExp.Definition);
        }
        [TestMethod]
        public void Profile_ParseUpload_File()
        {
            var source = "files\\Test.txt";
            var target = "/Root/Benchmark/UploadTest";
            var value = $"{source} {target}";
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src = $"UPLOAD: {value}";
            var parser = new ProfileParser(src, ProfileLocation, speedItems);

            var result = parser.Parse();

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0] is UploadExpression);
            var uploadExp = (UploadExpression)result[0];
            Assert.AreEqual(source, uploadExp.Source);
            Assert.AreEqual(target, uploadExp.Target);
        }

    }
}
