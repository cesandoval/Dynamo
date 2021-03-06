﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Dynamo.Interfaces;
using Dynamo.Utilities;
using Dynamo.ViewModels;
using DynamoUnits;
using Dynamo.UpdateManager;
using NUnit.Framework;
using ProtoCore.Mirror;
using RevitServices.Persistence;
using RevitServices.Transactions;
using ModelCurve = Autodesk.Revit.DB.ModelCurve;
using Plane = Autodesk.Revit.DB.Plane;
using SketchPlane = Autodesk.Revit.DB.SketchPlane;
using Transaction = Autodesk.Revit.DB.Transaction;

namespace Dynamo.Tests
{
    [TestFixture]
    public abstract class DynamoRevitUnitTestBase
    {
        protected Transaction _trans;
        protected string _testPath;
        protected string _samplesPath;
        protected string _defsPath;
        protected string _emptyModelPath1;
        protected string _emptyModelPath;
        protected DynamoController Controller;

        //Called before each test method
        
        [SetUp]
        public void Setup()
        {
            StartDynamo();

            //it doesn't make sense to do these steps before every test
            //but when running from the revit plugin we are not loading the 
            //fixture, so the initfixture method is not called.

            //get the test path
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string assDir = fi.DirectoryName;
            string testsLoc = Path.Combine(assDir, @"..\..\..\test\System\revit\");
            _testPath = Path.GetFullPath(testsLoc);

            //get the samples path
            string samplesLoc = Path.Combine(assDir, @"..\..\..\doc\distrib\Samples\");
            _samplesPath = Path.GetFullPath(samplesLoc);

            //set the custom node loader search path
            string defsLoc = Path.Combine(assDir, @".\dynamo_packages\Dynamo Sample Custom Nodes\dyf\");
            _defsPath = Path.GetFullPath(defsLoc);

            _emptyModelPath = Path.Combine(_testPath, "empty.rfa");

            if (DocumentManager.Instance.CurrentUIApplication.Application.VersionNumber.Contains("2014") &&
                DocumentManager.Instance.CurrentUIApplication.Application.VersionName.Contains("Vasari"))
            {
                _emptyModelPath = Path.Combine(_testPath, "emptyV.rfa");
                _emptyModelPath1 = Path.Combine(_testPath, "emptyV1.rfa");
            }
            else
            {
                _emptyModelPath = Path.Combine(_testPath, "empty.rfa");
                _emptyModelPath1 = Path.Combine(_testPath, "empty1.rfa");
            }
        }

        private void StartDynamo()
        {
            try
            {
                SIUnit.HostApplicationInternalAreaUnit = DynamoAreaUnit.SquareFoot;
                SIUnit.HostApplicationInternalLengthUnit = DynamoLengthUnit.DecimalFoot;
                SIUnit.HostApplicationInternalVolumeUnit = DynamoVolumeUnit.CubicFoot;

                var logger = new DynamoLogger();
                var updateManager = new UpdateManager.UpdateManager(logger);

                //create a new instance of the ViewModel
                Controller = new DynamoController(Context.NONE, updateManager, logger, 
                    new DefaultWatchHandler(), new PreferenceSettings());
                DynamoController.IsTestMode = true;
                Controller.DynamoViewModel = new DynamoViewModel(Controller, null);
                Controller.VisualizationManager = new VisualizationManager();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            //create the transaction manager object
            TransactionManager.SetupManager(new AutomaticTransactionStrategy());

            //tests do not run from idle thread
            TransactionManager.Instance.DoAssertInIdleThread = false;
        }

        /// <summary>
        /// Creates two model curves separated in Z.
        /// </summary>
        /// <param name="mc1"></param>
        /// <param name="mc2"></param>
        protected void CreateTwoModelCurves(out ModelCurve mc1, out ModelCurve mc2)
        {
            //create two model curves 
            using (_trans = _trans = new Transaction(DocumentManager.Instance.CurrentUIDocument.Document, "CreateTwoModelCurves"))
            {
                _trans.Start();

                var p1 = new Plane(XYZ.BasisZ, XYZ.Zero);
                var p2 = new Plane(XYZ.BasisZ, new XYZ(0, 0, 5));

                SketchPlane sp1 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewSketchPlane(p1);
                SketchPlane sp2 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewSketchPlane(p2);
                Curve c1 = DocumentManager.Instance.CurrentUIApplication.Application.Create.NewLineBound(XYZ.Zero, new XYZ(1, 0, 0));
                Curve c2 = DocumentManager.Instance.CurrentUIApplication.Application.Create.NewLineBound(new XYZ(0, 0, 5), new XYZ(1, 0, 5));
                mc1 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewModelCurve(c1, sp1);
                mc2 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewModelCurve(c2, sp2);

                _trans.Commit();
            }
        }

        /// <summary>
        /// Creates one model curve on a plane with an origin at 0,0,0
        /// </summary>
        /// <param name="mc1"></param>
        protected void CreateOneModelCurve(out ModelCurve mc1)
        {
            //create two model curves 
            using (_trans = _trans = new Transaction(DocumentManager.Instance.CurrentUIDocument.Document, "CreateTwoModelCurves"))
            {
                _trans.Start();

                var p1 = new Plane(XYZ.BasisZ, XYZ.Zero);

                SketchPlane sp1 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewSketchPlane(p1);
                Curve c1 = DocumentManager.Instance.CurrentUIApplication.Application.Create.NewLineBound(XYZ.Zero, new XYZ(1, 0, 0));
                mc1 = DocumentManager.Instance.CurrentUIDocument.Document.FamilyCreate.NewModelCurve(c1, sp1);

                _trans.Commit();
            }
        }

        /// <summary>
        /// Opens and activates a new model, and closes the old model.
        /// </summary>
        protected void SwapCurrentModel(string modelPath)
        {
            Document initialDoc = DocumentManager.Instance.CurrentUIDocument.Document;
            DocumentManager.Instance.CurrentUIApplication.OpenAndActivateDocument(modelPath);
            initialDoc.Close(false);
        }

        protected void OpenAndRun(string subPath)
        {
            var model = dynSettings.Controller.DynamoModel;

            string samplePath = Path.Combine(_testPath, subPath);
            string testPath = Path.GetFullPath(samplePath);

            Assert.IsTrue(File.Exists(testPath), string.Format("Could not find file: {0} for testing.", testPath));

            model.Open(testPath);

            Assert.DoesNotThrow(() => dynSettings.Controller.RunExpression());
        }

        #region Revit unit test helper methods

        public void OpenModel(string relativeFilePath)
        {
            var model = dynSettings.Controller.DynamoModel;

            string samplePath = Path.Combine(_samplesPath, relativeFilePath);
            string testPath = Path.GetFullPath(samplePath);

            model.Open(testPath);
        }

        public void RunCurrentModel()
        {
            Assert.DoesNotThrow(() => Controller.RunExpression(null));
        }

        public void AssertNoDummyNodes()
        {
            var nodes = Controller.DynamoModel.Nodes;

            double dummyNodesCount = nodes.OfType<DSCoreNodesUI.DummyNode>().Count();
            if (dummyNodesCount >= 1)
            {
                Assert.Fail("Number of dummy nodes found in Sample: " + dummyNodesCount);
            }
        }

        public void AssertPreviewCount(string guid, int count)
        {
            string varname = GetVarName(guid);
            var mirror = GetRuntimeMirror(varname);
            Assert.IsNotNull(mirror);

            var data = mirror.GetData();
            Assert.IsTrue(data.IsCollection);
            Assert.AreEqual(count, data.GetElements().Count);
        }

        public object GetPreviewValue(string guid)
        {
            string varname = GetVarName(guid);
            var mirror = GetRuntimeMirror(varname);
            Assert.IsNotNull(mirror);

            return mirror.GetData().Data;
        }

        public object GetPreviewValueAtIndex(string guid, int index)
        {
            string varname = GetVarName(guid);
            var mirror = GetRuntimeMirror(varname);
            Assert.IsNotNull(mirror);

            return mirror.GetData().GetElements()[index].Data;
        }

        public void AssertClassName(string guid, string className)
        {
            string varname = GetVarName(guid);
            var mirror = GetRuntimeMirror(varname);
            Assert.IsNotNull(mirror);
            var classInfo = mirror.GetData().Class;
            Assert.AreEqual(classInfo.ClassName, className);
        }

        private string GetVarName(string guid)
        {
            var model = Controller.DynamoModel;
            var node = model.CurrentWorkspace.NodeFromWorkspace(guid);
            Assert.IsNotNull(node);
            return node.VariableToPreview;
        }

        private RuntimeMirror GetRuntimeMirror(string varName)
        {
            RuntimeMirror mirror = null;
            Assert.DoesNotThrow(() => mirror = Controller.EngineController.GetMirror(varName));
            return mirror;
        }

        #endregion

    }
}
