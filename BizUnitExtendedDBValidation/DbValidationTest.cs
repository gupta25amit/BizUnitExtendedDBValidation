using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizUnit.TestSteps.Sql;
using BizUnit.Core.TestBuilder;
using BizUnit.Core;
using BizUnitExtendedDBValidation.Steps;

namespace BizUnitExtendedDBValidation
{
    [TestClass]
    public class DbValidationTest
    {
        [TestMethod]
        public void ValidateDbTest()
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
            dbQuery2.SQLQuery = new SqlQuery { RawSqlQuery = "Select * from [dbo].[Test1] where Name = '@param'" };

            dbQuery.SubSteps.Add(dbQuery2);


            var testCase = new TestCase();
            testCase.SetupSteps.Add(dbQuery1);
            testCase.SetupSteps.Add(dbQuery);
            //testCase.ExecutionSteps.Add(dbQuery2);

            var testRunner = new TestRunner(testCase);
            testRunner.Run();
        }
    }
}
