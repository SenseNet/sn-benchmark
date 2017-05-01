using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [TestMethod]
        public void PathSet_ExprName_Missing()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = string.Empty;
            try
            {
                PathSetExpression.Parse(src, ProfileName0, pathSets);
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
                PathSetExpression.Parse(src, ProfileName0, pathSets);
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
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.Name);
        }


        [TestMethod]
        public void PathSet_ExprOperation_Missing()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = ProfileName0;
            try
            {
                PathSetExpression.Parse(src, ProfileName0, pathSets);
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
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.Name);
            Assert.AreEqual(PathSetOperation.First, pathSetExpr.Operation);
        }
        [TestMethod]
        public void PathSet_ExprOperation_Current()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.CURRENT";
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.Name);
            Assert.AreEqual(PathSetOperation.Current, pathSetExpr.Operation);
        }
        [TestMethod]
        public void PathSet_ExprOperation_Next()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.NEXT";
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.Name);
            Assert.AreEqual(PathSetOperation.Next, pathSetExpr.Operation);
        }

        [TestMethod]
        public void PathSet_ExprOperation_AbsIndex()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.42";
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(PathSet0, pathSetExpr.Name);
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
                var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException e)
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
                PathSetExpression.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException e)
            {
            }
        }

        [TestMethod]
        public void PathSet_ExprTrasform_Entity()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.ODATAENTITY";
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

            Assert.AreEqual(1, pathSetExpr.TransformationSteps.Length);
            Assert.AreEqual(PathSetTransform.ODataEntity, pathSetExpr.TransformationSteps[0]);
        }
        [TestMethod]
        public void PathSet_ExprTrasform_Parent()
        {
            var pathSets = new List<PathSet> { new PathSet { Name = PathSet0, Paths = new string[0], ProfileName = ProfileName0 } };

            var src = $"{PathSet0}.current.PARENT";
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

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
                PathSetExpression.Parse(src, ProfileName0, pathSets);
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
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

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
            var pathSetExpr = PathSetExpression.Parse(src, ProfileName0, pathSets);

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
                PathSetExpression.Parse(src, ProfileName0, pathSets);
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
                PathSetExpression.Parse(src, ProfileName0, pathSets);
                Assert.Fail("Exception was not thrown");
            }
            catch (ApplicationException)
            {
            }
        }


        /*
        [TestMethod]
        public void PathSet_Exec_current()
        {
            var profile0 = new Profile("Profile0", new List<BenchmarkActionExpression>());
            var profile1 = profile0.Clone();
            var profile2 = new Profile("Profile2", new List<BenchmarkActionExpression>());

            PathSet.Create("Profile0", "PathSet0", new[] { "/Root/A/B/Path0", "/Root/A/B/Path1", "/Root/A/B/Path2" });
            PathSet.Create("Profile1", "PathSet0", new[] { "/Root/A/B/Path3", "/Root/A/B/Path4", "/Root/A/B/Path5" });
            PathSet.Create("Profile2", "PathSet0", new[] { "/Root/A/B/Path6", "/Root/A/B/Path7", "/Root/A/B/Path8" });

            var expectedUrl = "/OData.svc/Root/A/B/Path4?metadata=no";
            var actualUrl1 = PathSet.ResolveUrl("/OData.svc/##PathSet0.current##?metadata=no", profile1);
            var actualUrl2 = PathSet.ResolveUrl("/OData.svc/##PathSet0.current##?metadata=no", profile1);

            Assert.AreEqual(expectedUrl, actualUrl1);
            Assert.AreEqual(expectedUrl, actualUrl2);
        }
        */
    }
}
