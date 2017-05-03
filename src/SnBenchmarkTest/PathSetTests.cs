using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SnBenchmark;
using SnBenchmark.Expression;

namespace SnBenchmarkTest
{
    [TestClass]
    public class PathSetTests
    {
        private const string PathSet0 = "PathSet0";
        private const string ProfileName0 = "Profile0";

        [TestInitialize]
        public void InitializeTest()
        {
            Profile.ResetIdAndIndexes();
            PathSet.PathSets.Clear();
        }

        [TestMethod]
        public void PathSet_ProfileIdAndIndex()
        {
            var profileA0 = new Profile("ProfileA", new List<BenchmarkActionExpression>());
            var profileB0 = new Profile("ProfileB", new List<BenchmarkActionExpression>());
            var profileA1 = profileA0.Clone();
            var profileA2 = profileA0.Clone();
            var profileB1 = profileB0.Clone();
            var profileB2 = profileB0.Clone();
            var profileA3 = profileA2.Clone();
            var profileB3 = profileB2.Clone();
            var profileA4 = profileA1.Clone();
            var profileB4 = profileB1.Clone();

            var pa = new[] {profileA0, profileA1, profileA2, profileA3, profileA4};
            var pb = new[] {profileB0, profileB1, profileB2, profileB3, profileB4};

            var idStringA = string.Join(",", pa.Select(p => p.Id).ToArray());
            var idStringB = string.Join(",", pb.Select(p => p.Id).ToArray());

            Assert.AreEqual("1,3,4,7,9", idStringA);
            Assert.AreEqual("2,5,6,8,10", idStringB);

            var indexStringA = string.Join(",", pa.Select(p => p.GetIndex("A")).ToArray());
            var indexStringB = string.Join(",", pb.Select(p => p.GetIndex("A")).ToArray());

            Assert.AreEqual("0,1,2,3,4", indexStringA);
            Assert.AreEqual("0,1,2,3,4", indexStringB);
        }

        #region Parsing tests

        [TestMethod]
        public void PathSet_ExprName_Missing()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = string.Empty;
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprName_Unknown()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = "PathSet0", Paths = new string[0], ProfileName = ProfileName0 } };

            var src = "PathSet111.FIRST";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprName_CaseInsensitive()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0.ToUpperInvariant()}.FIRST";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.PathSet.Name);
        }


        [TestMethod]
        public void PathSet_ExprOperation_Missing()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = ProfileName0;
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprOperation_First()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.FIRST";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.PathSet.Name);
            Assert.AreEqual(PathSetOperation.First, pathSetExpr.Operation);
        }
        [TestMethod]
        public void PathSet_ExprOperation_Current()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.CURRENT";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.PathSet.Name);
            Assert.AreEqual(PathSetOperation.Current, pathSetExpr.Operation);
        }
        [TestMethod]
        public void PathSet_ExprOperation_Next()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.NEXT";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.PathSet.Name);
            Assert.AreEqual(PathSetOperation.Next, pathSetExpr.Operation);
        }

        [TestMethod]
        public void PathSet_ExprOperation_AbsIndex()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.42";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.PathSet.Name);
            Assert.AreEqual(PathSetOperation.Index, pathSetExpr.Operation);
            Assert.AreEqual(42, pathSetExpr.AbsoluteIndex);
        }

        [TestMethod]
        public void PathSet_ExprOperation_AbsIndex_Negative()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.-111";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprOperation_AbsIndex_TooBig()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.{(long)42 + int.MaxValue}";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }

        [TestMethod]
        public void PathSet_ExprTrasform_Entity()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.ODATAENTITY";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(1, pathSetExpr.TransformationSteps.Length);
            Assert.AreEqual(PathSetTransform.ODataEntity, pathSetExpr.TransformationSteps[0]);
        }
        [TestMethod]
        public void PathSet_ExprTrasform_Parent()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(1, pathSetExpr.TransformationSteps.Length);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[0]);
        }
        [TestMethod]
        public void PathSet_ExprTrasform_Unknown()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.UNKNOWN";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprTrasform_ParentParentEntity()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT.PARENT.ODATAENTITY";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(3, pathSetExpr.TransformationSteps.Length);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[0]);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[1]);
            Assert.AreEqual(PathSetTransform.ODataEntity, pathSetExpr.TransformationSteps[2]);
        }
        [TestMethod]
        public void PathSet_ExprTrasform_ParentParentParent()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT.PARENT.PARENT";
            var pathSetExpr = PathOperation.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(3, pathSetExpr.TransformationSteps.Length);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[0]);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[1]);
            Assert.AreEqual(PathSetTransform.Parent, pathSetExpr.TransformationSteps[2]);
        }
        [TestMethod]
        public void PathSet_ExprTrasform_ParentParentWrong()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT.PARENT.WRONG";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }
        [TestMethod]
        public void PathSet_ExprTrasform_ParentWrongParent()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT.WRONG.PARENT";
            try
            {
                PathOperation.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }

        #endregion

        #region Execution tests

        [TestMethod]
        public void PathSet_ExprExec_Current()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2" });

            var expectedUrl = "/OData.svc/Root/A/B/Path0?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current##?metadata=no", profile0);
            var actualUrl2 = PathSet.ResolveUrl("/OData.svc##PathSet0.current##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
            Assert.AreEqual(expectedUrl, actualUrl2);
        }
        [TestMethod]
        public void PathSet_ExprExec_Next()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2" });

            var expectedUrls = new []
            {
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path1?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path1?metadata=no"
            };
            var actualUrls = new []
            {
                PathSet.ResolveUrl("/OData.svc##PathSet0.current##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0)
            };
            Assert.AreEqual(expectedUrls[0], actualUrls[0]);
            Assert.AreEqual(expectedUrls[1], actualUrls[1]);
            Assert.AreEqual(expectedUrls[2], actualUrls[2]);
            Assert.AreEqual(expectedUrls[3], actualUrls[3]);
            Assert.AreEqual(expectedUrls[4], actualUrls[4]);
        }
        [TestMethod]
        public void PathSet_ExprExec_First()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2", "/Root/A/B/Path3" });

            var expectedUrls = new[]
            {
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path1?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path0?metadata=no",
            };
            var actualUrls = new[]
            {
                PathSet.ResolveUrl("/OData.svc##PathSet0.current##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.first##?metadata=no", profile0),
            };
            Assert.AreEqual(expectedUrls[0], actualUrls[0]);
            Assert.AreEqual(expectedUrls[1], actualUrls[1]);
            Assert.AreEqual(expectedUrls[2], actualUrls[2]);
            Assert.AreEqual(expectedUrls[3], actualUrls[3]);
        }
        [TestMethod]
        public void PathSet_ExprExec_Index4Index2FirstIndex3()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2", "/Root/A/B/Path3", "/Root/A/B/Path4" });

            var expectedUrls = new[]
            {
                "/OData.svc/Root/A/B/Path4?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path3?metadata=no",
            };
            var actualUrls = new[]
            {
                PathSet.ResolveUrl("/OData.svc##PathSet0.4##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.2##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.first##?metadata=no", profile0),
                PathSet.ResolveUrl("/OData.svc##PathSet0.3##?metadata=no", profile0),
            };

            Assert.AreEqual(expectedUrls[0], actualUrls[0]);
            Assert.AreEqual(expectedUrls[1], actualUrls[1]);
            Assert.AreEqual(expectedUrls[2], actualUrls[2]);
            Assert.AreEqual(expectedUrls[3], actualUrls[3]);
        }

        [TestMethod]
        public void PathSet_ExprExec_MoreInstances()
        {
            var profileA = new Profile("ProfileA", new List<BenchmarkActionExpression>());
            PathSet.Create("ProfileA", "PathSet0",
                new[] {"/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2", "/Root/A/B/Path3", "/Root/A/B/Path4"});

            var profiles = new[]
            {
                profileA,
                profileA.Clone(),
                profileA.Clone(),
                profileA.Clone(),
                profileA.Clone(),
                profileA.Clone(),
                profileA.Clone()
            };


            // Test #1
            var actualUrls = profiles.Select(p => PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", p)).ToArray();

            // Assert #1
            var expectedUrls = new[]
            {
                "/OData.svc/Root/A/B/Path1?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path3?metadata=no",
                "/OData.svc/Root/A/B/Path4?metadata=no",
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path1?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
            };
            for (int i = 0; i < actualUrls.Length; i++)
                Assert.AreEqual(expectedUrls[i], actualUrls[i]);

            // Test #2
            actualUrls = profiles.Select(p => PathSet.ResolveUrl("/OData.svc##PathSet0.next##?metadata=no", p)).ToArray();

            // Assert #2
            expectedUrls = new[]
            {
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path3?metadata=no",
                "/OData.svc/Root/A/B/Path4?metadata=no",
                "/OData.svc/Root/A/B/Path0?metadata=no",
                "/OData.svc/Root/A/B/Path1?metadata=no",
                "/OData.svc/Root/A/B/Path2?metadata=no",
                "/OData.svc/Root/A/B/Path3?metadata=no",
            };
            for (int i = 0; i < actualUrls.Length; i++)
                Assert.AreEqual(expectedUrls[i], actualUrls[i]);
        }

        [TestMethod]
        public void PathSet_ExprExec_Parent()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A0/B0/Path0", "/Root/A1/B1/Path1", "/Root/A2/B2/Path2" });

            var expectedUrl = "/OData.svc/Root/A0/B0?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current.PARENT##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
        }
        [TestMethod]
        public void PathSet_ExprExec_Entity()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A0/B0/Path0", "/Root/A1/B1/Path1", "/Root/A2/B2/Path2" });

            var expectedUrl = "/OData.svc/Root/A0/B0('Path0')?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current.ODATAENTITY##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
        }
        [TestMethod]
        public void PathSet_ExprExec_ParentParent()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A0/B0/Path0", "/Root/A1/B1/Path1", "/Root/A2/B2/Path2" });

            var expectedUrl = "/OData.svc/Root/A0?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current.PARENT.PARENT##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
        }
        [TestMethod]
        public void PathSet_ExprExec_ParentParentEntity()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A0/B0/Path0", "/Root/A1/B1/Path1", "/Root/A2/B2/Path2" });

            var expectedUrl = "/OData.svc/Root('A0')?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current.PARENT.PARENT.ODATAENTITY##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
        }
        [TestMethod]
        public void PathSet_ExprExec_ParentParentParentEntity()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A0/B0/Path0", "/Root/A1/B1/Path1", "/Root/A2/B2/Path2" });

            var expectedUrl = "/OData.svc/('Root')?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc##PathSet0.current.PARENT.PARENT.PARENT.ODATAENTITY##?metadata=no", profile0);

            Assert.AreEqual(expectedUrl, actualUrl1);
        }

        #endregion

        [TestMethod]
        public void PathSet_Initialization()
        {
            var speedItems = new List<string> { RequestExpression.NormalSpeed };
            var src =
                "PATHSET: BigFiles Size:>236000 AND TypeIs:File AND InTree:'/Root/Benchmark/Files' .AUTOFILTERS:OFF" + Environment.NewLine +
                "PATHSET: SmallFiles Size:<2360 AND TypeIs:File AND InTree:'/Root/Benchmark/Files' .AUTOFILTERS:OFF" + Environment.NewLine +
                "WAIT: 10042";
            var webAccess = new TestWebAccess();
            Web.WebAccess = webAccess;

            var profile = Profile.Parse("Profile0", src, speedItems);
            IEnumerable<PathSetExpression> psExprs;
            Profile.GetPathSets(profile, out psExprs);

            // test#1: PathSetExpressions must be removed from the profile's action list
            var actions = profile.Actions;
            Assert.AreEqual(1, actions.Count);
            var action = actions[0] as WaitExpression;
            Assert.IsNotNull(action);
            Assert.AreEqual(10042, action.Milliseconds);

            // test#2: Separated path set list contains two items
            var pathSetExpressions = psExprs.ToArray();
            Assert.AreEqual(2, pathSetExpressions.Length);
            Assert.AreEqual("BigFiles", pathSetExpressions[0].Name);
            Assert.AreEqual("SmallFiles", pathSetExpressions[1].Name);
        }

        [TestMethod]
        public void PathSet_ParsePaths()
        {
            var src = @"{
    ""d"": {
        ""__count"": 10,
        ""results"": [
            {
                ""Path"": ""/Root/Benchmark/doc1.doc""
            },
            {
                ""Path"": ""/Root/Benchmark/doc2.doc""
            },
            {
                ""Path"": ""/Root/Benchmark/doc3.doc""
            },
        ]
    }
}";
            var result = new BuiltInWebAccess().ParsePaths(src);

            var expected = string.Join(", ", result);
            Assert.AreEqual("/Root/Benchmark/doc1.doc, /Root/Benchmark/doc2.doc, /Root/Benchmark/doc3.doc", expected);
        }
    }
}
