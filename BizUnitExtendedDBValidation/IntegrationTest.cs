using BizUnit.Core;
using BizUnit.Core.TestBuilder;
using BizUnit.TestSteps.Common;
using BizUnit.TestSteps.DataLoaders.File;
using BizUnit.TestSteps.File;
using BizUnit.TestSteps.ValidationSteps.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using MSTestExtensions;
using BizUnit.TestSteps.Sql;
using BizUnit.TestSteps.Http;
using BizUnitExtendedDBValidation.Steps;

namespace BizUnitExtendedDBValidation
{
    [TestClass]
    public class IntegrationTest
    {
        #region Test Cases

        #region End to End integration Test
        [TestMethod]
        public void Integration_Test()
        {

            #region Variable Intialization
            var sourceFilePath = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Testing\OrderSample.xml";
            var targetFilePath = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Testing\Order\OrderSample.xml";
            var DestinationDirSummary = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Testing\Summary";
            #endregion

            #region Initial CleanUp Step
            //Get rid of any files that are already there
            var deleteStep = new DeleteStep();
            deleteStep.FilePathsToDelete.Add(targetFilePath);
            #endregion

            #region Create step
            //Create the test step
            var createStep = new CreateStep();
            createStep.CreationPath = targetFilePath;   //Where are we going to create the file
            var dataLoader = new FileDataLoader();          //Where are we getting the file from?
            dataLoader.FilePath = sourceFilePath;
            createStep.DataSource = dataLoader;
            #endregion

            #region Execution steps

            #region File Validation Step
            //Create a validating read step //We should only have one file in the directory
            var ValidateFileStep = new FileReadMultipleStep();
            ValidateFileStep.DirectoryPath = DestinationDirSummary;
            ValidateFileStep.SearchPattern = "*.xml";
            ValidateFileStep.Timeout = 60000;
            ValidateFileStep.ExpectedNumberOfFiles = 1;
            ValidateFileStep.DeleteFiles = true;
            #endregion

            #region  #SubSteps - Schema and node value Validation Step
            //Create an XML Validation step //This will check the result against the XSD for the document
            var summarySchemaValidationStep = new XmlValidationStep();
            var schemaSummary = new SchemaDefinition();
            schemaSummary.XmlSchemaPath = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Schemas\SummarySchema.xsd";
            schemaSummary.XmlSchemaNameSpace = "http://Timewave.BizTalkUnit.Sample.DestinationSchema";

            summarySchemaValidationStep.XmlSchemas.Add(schemaSummary);

            //Create an XPath Validation.  This will check a value in the output.
            //The Xpath for the node can be grabbed from the Instance XPath property on the XSD.
            var xpathProductId = new XPathDefinition();
            xpathProductId.Description = "ItemsOrdered";
            xpathProductId.XPath = "/*[local-name()='CustomerSummary' and namespace-uri()='http://Timewave.BizTalkUnit.Sample.DestinationSchema']/*[local-name()='ItemsOrdered' and namespace-uri()='']";
            xpathProductId.Value = "1";
            summarySchemaValidationStep.XPathValidations.Add(xpathProductId);

            ValidateFileStep.SubSteps.Add(summarySchemaValidationStep);

            #endregion

            #endregion

            #region Test Case Execution
            var testCase = new TestCase();
            testCase.SetupSteps.Add(deleteStep);
            testCase.ExecutionSteps.Add(createStep);
            testCase.ExecutionSteps.Add(ValidateFileStep);
            testCase.CleanupSteps.Add(deleteStep);

            var testRunner = new TestRunner(testCase);
            testRunner.Run();


            #endregion

        }

        #endregion

        #region Other Validation Tests

        #region Xml Node Value Validation 
        [TestMethod]
        public void PickListTest_ValidatePickListQuantity()
        {
            var validation = new XmlValidationStep();
            var schemaPickList = new SchemaDefinition
            {
                XmlSchemaPath = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Schemas\PickListSchema.xsd",
                XmlSchemaNameSpace = "http://Timewave.BizTalkUnit.Sample.PickListSchema"
            };
            validation.XmlSchemas.Add(schemaPickList);

            var xpathProductId = new XPathDefinition();
            xpathProductId.Description = "Quantity";
            xpathProductId.XPath = "/*[local-name()='PickList' and namespace-uri()='http://Timewave.BizTalkUnit.Sample.PickListSchema']/*[local-name()='Items' and namespace-uri()='']/*[local-name()='Quantity' and namespace-uri()='']"; ;
            xpathProductId.Value = "3";
            validation.XPathValidations.Add(xpathProductId);

            var ctx = new Context();
            string[] filePaths = Directory.GetFiles(@"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Testing\Picklist", "*.xml");
            var data = StreamHelper.LoadFileToStream(filePaths[0]);
            validation.Execute(data, ctx);
        }
        #endregion

        #region BizUnit Test with Assert Methods
        [TestMethod]
        public void PickListTest_QuantityMisMatch()
        {
            var validation = new XmlValidationStep();
            var schemaPurchaseOrder = new SchemaDefinition
            {
                XmlSchemaPath = @"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Timewave.BizUnit.Sample\Schemas\PickListSchema.xsd",
                XmlSchemaNameSpace = "http://Timewave.BizTalkUnit.Sample.PickListSchema"
            };
            validation.XmlSchemas.Add(schemaPurchaseOrder);

            var xpathProductId = new XPathDefinition();
            xpathProductId.Description = "Quantity";
            xpathProductId.XPath = "/*[local-name()='PickList' and namespace-uri()='http://Timewave.BizTalkUnit.Sample.PickListSchema']/*[local-name()='Items' and namespace-uri()='']/*[local-name()='Quantity' and namespace-uri()='']"; ;
            xpathProductId.Value = "8";
            validation.XPathValidations.Add(xpathProductId);

            var ctx = new Context();
            string[] filePaths = Directory.GetFiles(@"C:\Users\amigupta7\Desktop\Amit\Project\POC\Timewave.BizUnit.Sample\Testing\Picklist", "*.xml");
            var data = StreamHelper.LoadFileToStream(filePaths[0]);

            try
            {
                MSTestExtensions.BaseTest.Assert.Throws<ValidationStepExecutionException>(() => validation.Execute(data, ctx));
            }
            catch (ValidationStepExecutionException vsee)
            {
                Assert.AreEqual("Failed to validate document instance", vsee.Message);
                Assert.AreEqual("The expected value for qauntity is 3", vsee.InnerException.Message);
            }
        }
        #endregion

        #region DB Validation Steps
        [TestMethod]
        public void Test_DbTestStep()
        {

            string con = "Data Source=(local);Initial Catalog=AmiTest;Integrated Security=SSPI;";
            var dbQuery1 = new DbValidationStep();
            dbQuery1.ConnectionString = con;
            dbQuery1.IsDbUpdate = true;
            dbQuery1.SQLQuery = new SqlQuery { RawSqlQuery = "UPDATE [dbo].[Test1] SET IsActive = 1 WHERE [Name] = 'Amit'" };

            var dbQuery = new DbValidationStep();
            dbQuery.ConnectionString = con;
            dbQuery.ExpectedDbValue = "Amit";
            dbQuery.IsValueValidation = true;
            dbQuery.SQLQuery = new SqlQuery { RawSqlQuery = "Select top 1 Name from [dbo].[Test1] where City = 'Delhi' and IsActive = 1" };

            var dbQuery2 = new DbValidationStep();
            dbQuery2.ConnectionString = con;
            dbQuery2.ActualDbValue = dbQuery.ActualDbValue;
            dbQuery2.NumberOfRowsExpected = 2;
            dbQuery2.IsRowValidation = true;
            dbQuery2.SQLQuery = new SqlQuery { RawSqlQuery = "Select * from [dbo].[Test1] where Name = '@param'" };  // @param will be replaced by the value extracted from DB in th eparen step.

            dbQuery.SubSteps.Add(dbQuery2);


            var testCase = new TestCase();
            testCase.SetupSteps.Add(dbQuery1);
            testCase.SetupSteps.Add(dbQuery);
            //testCase.ExecutionSteps.Add(dbQuery2);

            var testRunner = new TestRunner(testCase);
            testRunner.Run();

            //var ctx = new Context();
            //dbQuery.Execute(ctx);

            //try
            //{
            //    Assert.Throws<Exception>(() => dbQuery.Execute(testContextInstance));
            //}
            //catch (Exception ex)
            //{
            //    Assert.AreEqual("Failed to validate document instance", vsee.Message);
            //    Assert.AreEqual("The expected value for qauntity is 3", vsee.InnerException.Message);
            //}

        }
        #endregion

        #endregion
        #endregion
    }
}
