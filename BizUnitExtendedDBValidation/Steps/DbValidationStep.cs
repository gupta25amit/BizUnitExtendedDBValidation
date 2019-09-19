using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using BizUnit.Core.TestBuilder;
using BizUnit.TestSteps.Sql;

namespace BizUnitExtendedDBValidation.Steps 
{
    public class DbValidationStep : TestStepBase
    {
        private Collection<DbValidationStep> _subSteps = new Collection<DbValidationStep>();
        public string ConnectionString { get; set; }
        public SqlQuery SQLQuery { get; set; }
        public Collection<DbRowToValidate> DbRowsToValidate { get; set; }
        public int NumberOfRowsExpected { get; set; }
        public int DelayBeforeCheck { get; set; }
        public string ExpectedDbValue { get; set; }
        public string ActualDbValue { get; set; }
        public bool IsRowValidation { get; set; }
        public bool IsValueValidation { get; set; }
        public bool IsDbUpdate { get; set; }

        private bool _stepCompleted = false;
        private string _actualValue;

        public new Collection<DbValidationStep> SubSteps
        {
            set
            {
                _subSteps = value;
            }
            get
            {
                return _subSteps;
            }
        }

        public DbValidationStep()
        {
            DbRowsToValidate = new Collection<DbRowToValidate>();
            IsRowValidation = false;
            IsValueValidation = false;
            IsDbUpdate = false;
        }

        /// <summary>
        /// Execute()
        /// </summary>
        /// <param name='context'>The context for the test, this holds state that is passed beteen tests</param>
        public override void Execute(Context context)
        {
            context.LogInfo("Using database connection string: {0}", ConnectionString);
            string sqlQueryToExecute = SQLQuery.GetFormattedSqlQuery(context);

            // Sleep for delay seconds...
            if (0 < DelayBeforeCheck)
            {
                context.LogInfo("Sleeping for: {0} seconds", DelayBeforeCheck);
                System.Threading.Thread.Sleep(DelayBeforeCheck * 1000);
            }

            context.LogInfo("Executing database query: {0}", sqlQueryToExecute);
            DataSet ds = FillDataSet(ConnectionString, sqlQueryToExecute);

            if (IsDbUpdate)
            {
                context.LogInfo("DB Updated.");
                _stepCompleted = true;
            }

            if (IsRowValidation)
            {
                if (NumberOfRowsExpected != ds.Tables[0].Rows.Count)
                {
                    throw new ApplicationException(string.Format("Number of rows expected to be returned by the query does not match the value specified in the teststep. Number of rows the NnumberOfRowsExpected were: {0}, actual: {1}", NumberOfRowsExpected, ds.Tables[0].Rows.Count));
                }

                context.LogInfo("NumberOfRowsExpected: {0}, actual number returned: {1}", NumberOfRowsExpected, ds.Tables[0].Rows.Count);
                _stepCompleted = true;
            }

            if (IsValueValidation)
            {
                if (ds.Tables[0].Rows.Count == 1)
                {
                    _actualValue = ds.Tables[0].Rows[0][0].ToString().Trim();
                    if (ExpectedDbValue != _actualValue)
                    {
                        throw new ApplicationException(string.Format("The value returned by the query does not match the value specified in the teststep. Expected value was: {0}, Actual: {1}", ExpectedDbValue, _actualValue));
                    }
                    context.LogInfo("Expecte Value: {0}, Actual Value Returned: {1}", ExpectedDbValue, _actualValue);

                }
                else if (ds.Tables[0].Rows.Count > 1)
                {
                    throw new ApplicationException(string.Format("More than one rows returned!"));

                }
                else
                {
                    throw new ApplicationException(string.Format("No row returned!"));
                }
                _stepCompleted = true;
            }

            if (0 < DbRowsToValidate.Count)
            {
                int rowCount = 0;

                foreach (var dbRowToValidate in DbRowsToValidate)
                {
                    context.LogInfo("Validating row number: {0}", rowCount);

                    var resultRow = ds.Tables[0].Rows[rowCount];
                    var cells = dbRowToValidate.Cells;

                    foreach (DbCellToValidate cell in cells)
                    {
                        object dbData = resultRow[cell.ColumnName];
                        var dbDataStringValue = string.Empty;

                        if (0 == ValidateData(dbData, cell.ExpectedValue, ref dbDataStringValue))
                        {
                            context.LogInfo("Validation succeeded for field: {0}. Expected value: {1}", cell.ColumnName, dbDataStringValue);
                        }
                        else
                        {
                            throw new Exception(String.Format("Validation failed for field: {0}. Expected value: {1}, actual value: {2}", cell.ColumnName, cell.ExpectedValue, dbDataStringValue));
                        }
                    }

                    rowCount++;
                }
                _stepCompleted = true;
            }

            foreach (var subStep in _subSteps)
            {
                try
                {
                    // Try the validation and catch the exception
                    subStep.Execute(_actualValue, context);
                }
                catch (Exception ex)
                {
                    context.LogException(ex);
                    throw;
                }
            }

            if (!_stepCompleted)
            {
                throw new ApplicationException(string.Format("No Test Action Selected for DbValidationStep!"));
            }
        }

        /// <summary>
        /// Overloaded Execute method for SubSteps.
        /// </summary>
        /// <param name="value">Value from Parent step needed to update substep query if required</param>
        /// <param name="context">Context</param>
        public void Execute(string value, Context context)
        {
            context.LogInfo("Using database connection string: {0}", ConnectionString);
            string sqlQueryToExecute = SQLQuery.GetFormattedSqlQuery(context);
            sqlQueryToExecute = sqlQueryToExecute.Replace("@param", value);

            // Sleep for delay seconds...
            if (0 < DelayBeforeCheck)
            {
                context.LogInfo("Sleeping for: {0} seconds", DelayBeforeCheck);
                System.Threading.Thread.Sleep(DelayBeforeCheck * 1000);
            }

            context.LogInfo("Executing database query: {0}", sqlQueryToExecute);
            DataSet ds = FillDataSet(ConnectionString, sqlQueryToExecute);

            if (IsRowValidation)
            {
                if (NumberOfRowsExpected != ds.Tables[0].Rows.Count)
                {
                    throw new ApplicationException(string.Format("Number of rows expected to be returned by the query does not match the value specified in the teststep. Number of rows the NnumberOfRowsExpected were: {0}, actual: {1}", NumberOfRowsExpected, ds.Tables[0].Rows.Count));
                }

                context.LogInfo("NumberOfRowsExpected: {0}, actual number returned: {1}", NumberOfRowsExpected, ds.Tables[0].Rows.Count);
                _stepCompleted = true;
            }

        }
        private static int ValidateData(object dbData, string targetValue, ref string dbDataStringValue)
        {
            dbDataStringValue = Convert.ToString(dbData);

            switch (dbData.GetType().ToString())
            {
                case ("System.DateTime"):
                    var dbDt = (DateTime)dbData;
                    DateTime targetDt = Convert.ToDateTime(targetValue);
                    return targetDt.CompareTo(dbDt);

                case ("System.DBNull"):
                    dbDataStringValue = "null";
                    return targetValue.CompareTo("null");

                case ("System.String"):
                    dbDataStringValue = (string)dbData;
                    return targetValue.CompareTo((string)dbData);

                case ("System.Int16"):
                    var dbInt16 = (System.Int16)dbData;
                    var targetInt16 = Convert.ToInt16(targetValue);
                    return targetInt16.CompareTo(dbInt16);

                case ("System.Int32"):
                    var dbInt32 = (System.Int32)dbData;
                    var targetInt32 = Convert.ToInt32(targetValue);
                    return targetInt32.CompareTo(dbInt32);

                case ("System.Int64"):
                    var dbInt64 = (System.Int64)dbData;
                    var targetInt64 = Convert.ToInt64(targetValue);
                    return targetInt64.CompareTo(dbInt64);

                case ("System.Double"):
                    var dbDouble = (System.Double)dbData;
                    var targetDouble = Convert.ToDouble(targetValue);
                    return targetDouble.CompareTo(dbDouble);

                case ("System.Decimal"):
                    var dbDecimal = (System.Decimal)dbData;
                    var targetDecimal = Convert.ToDecimal(targetValue);
                    return targetDecimal.CompareTo(dbDecimal);

                case ("System.Boolean"):
                    var dbBoolean = (System.Boolean)dbData;
                    var targetBoolean = Convert.ToBoolean(targetValue);
                    return targetBoolean.CompareTo(dbBoolean);

                case ("System.Byte"):
                    var dbByte = (System.Byte)dbData;
                    var targetByte = Convert.ToByte(targetValue);
                    return targetByte.CompareTo(dbByte);

                case ("System.Char"):
                    var dbChar = (System.Char)dbData;
                    var targetChar = Convert.ToChar(targetValue);
                    return targetChar.CompareTo(dbChar);

                case ("System.SByte"):
                    var dbSByte = (System.SByte)dbData;
                    var targetSByte = Convert.ToSByte(targetValue);
                    return targetSByte.CompareTo(dbSByte);

                default:
                    throw new ApplicationException(string.Format("Unsupported type: {0}", dbData.GetType()));
            }
        }

        private static DataSet FillDataSet(string connectionString, string sqlQuery)
        {
            var connection = new SqlConnection(connectionString);
            var ds = new DataSet();

            using (connection)
            {
                var adapter = new SqlDataAdapter
                {
                    SelectCommand = new SqlCommand(sqlQuery, connection)
                };
                adapter.Fill(ds);
                return ds;
            }
        }

        public override void Validate(Context context)
        {
            // _delayBeforeCheck - optional

            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new ArgumentNullException("ConnectionString is either null or of zero length");
            }
            ConnectionString = context.SubstituteWildCards(ConnectionString);

            if (null == SQLQuery)
            {
                throw new ArgumentNullException("ConnectionString is either null or of zero length");
            }

            SQLQuery.Validate(context);
        }
    }
}
