//
// SqlCommandTest.cs - NUnit Test Cases for testing the
//                          SqlCommand class
// Author:
//      Umadevi S (sumadevi@novell.com)
//	Sureshkumar T (tsureshkumar@novell.com)
//	Senganal T (tsenganal@novell.com)
//
// Copyright (c) 2004 Novell Inc., and the individuals listed
// on the ChangeLog entries.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
#if NET_2_0
using System.Data.Sql;
using System.Xml;
#endif

using NUnit.Framework;

namespace MonoTests.System.Data.SqlClient
{
	[TestFixture]
	[Category ("sqlserver")]
	public class SqlCommandTest
	{
		SqlConnection conn;
		SqlCommand cmd;
		string connectionString = ConnectionManager.Singleton.ConnectionString;

		[TearDown]
		public void TearDown ()
		{
			if (cmd != null) {
				cmd.Dispose ();
				cmd = null;
			}

			if (conn != null) {
				conn.Close ();
				conn = null;
			}
		}

		[Test] // ctor (String, SqlConnection, SqlTransaction)
		public void Constructor4 ()
		{
			string cmdText = "select @@version";

			SqlTransaction trans = null;
			SqlConnection connA = null;
			SqlConnection connB = null;

			// transaction from same connection
			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				trans = connA.BeginTransaction ();
				cmd = new SqlCommand (cmdText, connA, trans);

				Assert.AreEqual (cmdText, cmd.CommandText, "#A1");
				Assert.AreEqual (30, cmd.CommandTimeout, "#A2");
				Assert.AreEqual (CommandType.Text, cmd.CommandType, "#A3");
				Assert.AreSame (connA, cmd.Connection, "#A4");
				Assert.IsNull (cmd.Container, "#A5");
				Assert.IsTrue (cmd.DesignTimeVisible, "#A6");
#if NET_2_0
				Assert.IsNull (cmd.Notification, "#A7");
				Assert.IsTrue (cmd.NotificationAutoEnlist, "#A8");
#endif
				Assert.IsNotNull (cmd.Parameters, "#A9");
				Assert.AreEqual (0, cmd.Parameters.Count, "#A10");
				Assert.IsNull (cmd.Site, "#A11");
				Assert.AreSame (trans, cmd.Transaction, "#A12");
				Assert.AreEqual (UpdateRowSource.Both, cmd.UpdatedRowSource, "#A13");
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
			}

			// transaction from other connection
			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();
				connB = new SqlConnection (connectionString);
				connB.Open ();

				trans = connB.BeginTransaction ();
				cmd = new SqlCommand (cmdText, connA, trans);

				Assert.AreEqual (cmdText, cmd.CommandText, "#B1");
				Assert.AreEqual (30, cmd.CommandTimeout, "#B2");
				Assert.AreEqual (CommandType.Text, cmd.CommandType, "#B3");
				Assert.AreSame (connA, cmd.Connection, "#B4");
				Assert.IsNull (cmd.Container, "#B5");
				Assert.IsTrue (cmd.DesignTimeVisible, "#B6");
#if NET_2_0
				Assert.IsNull (cmd.Notification, "#B7");
				Assert.IsTrue (cmd.NotificationAutoEnlist, "#B8");
#endif
				Assert.IsNotNull (cmd.Parameters, "#B9");
				Assert.AreEqual (0, cmd.Parameters.Count, "#B10");
				Assert.IsNull (cmd.Site, "#B11");
				Assert.AreSame (trans, cmd.Transaction, "#B12");
				Assert.AreEqual (UpdateRowSource.Both, cmd.UpdatedRowSource, "#B13");
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
			}
		}

		[Test]
		public void ExecuteScalar ()
		{
			conn = new SqlConnection (connectionString);
			cmd = new SqlCommand ("" , conn);
			cmd.CommandText = "Select count(*) from numeric_family where id<=4";

			// Check the Return value for a Correct Query 
			object result = 0;
			conn.Open ();
			result = cmd.ExecuteScalar ();
			Assert.AreEqual (4, (int)result, "#A1 Query Result returned is incorrect");

			cmd.CommandText = "select id , type_bit from numeric_family order by id asc";
			result = Convert.ToInt32 (cmd.ExecuteScalar ());
			Assert.AreEqual (1, result,
				"#A2 ExecuteScalar Should return (1,1) the result set" );

			cmd.CommandText = "select id from numeric_family where id=-1";
			result = cmd.ExecuteScalar ();
			Assert.IsNull (result, "#A3 Null should be returned if result set is empty");

			// Check SqlException is thrown for Invalid Query 
			cmd.CommandText = "select count* from numeric_family";
			try {
				result = cmd.ExecuteScalar ();
				Assert.Fail ("#B1");
			} catch (SqlException ex) {
				// Incorrect syntax near the keyword 'from'
				Assert.AreEqual (typeof (SqlException), ex.GetType (), "#B2");
				Assert.AreEqual ((byte) 15, ex.Class, "#B3");
				Assert.IsNull (ex.InnerException, "#B4");
				Assert.IsNotNull (ex.Message, "#B5");
				Assert.IsTrue (ex.Message.IndexOf ("'from'") != -1, "#B6");
				Assert.AreEqual (156, ex.Number, "#B7");
				Assert.AreEqual ((byte) 1, ex.State, "#B8");
			}

			// Parameterized stored procedure calls

			int int_value = 20;
			string string_value = "output value changed";
			string return_value = "first column of first rowset";
			
			cmd.CommandText = 
				"create procedure #tmp_executescalar_outparams "+
				" (@p1 int, @p2 int out, @p3 varchar(200) out) "+
				"as " +
				"select '" + return_value + "' as 'col1', @p1 as 'col2' "+
				"set @p2 = @p2 * 2 "+
				"set @p3 = N'" + string_value + "' "+
				"select 'second rowset' as 'col1', 2 as 'col2' "+
				"return 1";
			
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#tmp_executescalar_outparams";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.Input;
			p1.DbType = DbType.Int32;
			p1.Value = int_value;
			cmd.Parameters.Add (p1);

			SqlParameter p2 = new SqlParameter ();
			p2.ParameterName = "@p2";
			p2.Direction = ParameterDirection.InputOutput;
			p2.DbType = DbType.Int32;
			p2.Value = int_value;
			cmd.Parameters.Add (p2);

			SqlParameter p3 = new SqlParameter ();
			p3.ParameterName = "@p3";
			p3.Direction = ParameterDirection.Output;
			p3.DbType = DbType.String;
			p3.Size = 200;
			cmd.Parameters.Add (p3);

			result = cmd.ExecuteScalar ();
			Assert.AreEqual (return_value, result, "#C1 ExecuteScalar Should return 'first column of first rowset'");
			Assert.AreEqual (int_value * 2, p2.Value, "#C2 ExecuteScalar should fill the parameter collection with the outputted values");
			Assert.AreEqual (string_value, p3.Value, "#C3 ExecuteScalar should fill the parameter collection with the outputted values");

			p3.Size = 0;
			p3.Value = null;
			try {
				cmd.ExecuteScalar ();
				Assert.Fail ("#D1 Query should throw System.InvalidOperationException due to size = 0 and value = null");
			} catch (InvalidOperationException ex) {
				// String[2]: the Size property has an invalid
				// size of 0
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#D2");
				Assert.IsNull (ex.InnerException, "#D3");
				Assert.IsNotNull (ex.Message, "#D4");
			} finally {
				conn.Close ();
			}
		}

		[Test]
		public void ExecuteScalar_Connection_PendingTransaction ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			using (SqlTransaction trans = conn.BeginTransaction ()) {
				cmd = new SqlCommand ("select @@version", conn);

				try {
					cmd.ExecuteScalar ();
					Assert.Fail ("#1");
				} catch (InvalidOperationException ex) {
					// Execute requires the command to have a
					// transaction object when the connection
					// assigned to the command is in a pending
					// local transaction.  The Transaction
					// property of the command has not been
					// initialized
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
					Assert.IsNull (ex.InnerException, "#3");
					Assert.IsNotNull (ex.Message, "#4");
				}
			}
		}

		[Test]
		public void ExecuteScalar_Query_Invalid ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			cmd = new SqlCommand ("InvalidQuery", conn);
			try {
				cmd.ExecuteScalar ();
				Assert.Fail ("#1");
			} catch (SqlException ex) {
				// Could not find stored procedure 'InvalidQuery'
				Assert.AreEqual (typeof (SqlException), ex.GetType (), "#2");
				Assert.AreEqual ((byte) 16, ex.Class, "#3");
				Assert.IsNull (ex.InnerException, "#4");
				Assert.IsNotNull (ex.Message, "#5");
				Assert.IsTrue (ex.Message.IndexOf ("'InvalidQuery'") != -1, "#6");
				Assert.AreEqual (2812, ex.Number, "#7");
				Assert.AreEqual ((byte) 62, ex.State, "#8");
			}
		}

		[Test]
		public void ExecuteScalar_Transaction_NotAssociated ()
		{
			Assert.Ignore ("NotWorking");

			SqlTransaction trans = null;
			SqlConnection connA = null;
			SqlConnection connB = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				trans = connA.BeginTransaction ();

				cmd = new SqlCommand ("select @@version", connB, trans);

				try {
					cmd.ExecuteScalar ();
					Assert.Fail ("#A1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
					Assert.IsNull (ex.InnerException, "#A3");
					Assert.IsNotNull (ex.Message, "#A4");
				} finally {
					cmd.Dispose ();
				}

				cmd = new SqlCommand ("select @@version", connB);
				cmd.Transaction = trans;

				try {
					cmd.ExecuteScalar ();
					Assert.Fail ("#B1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
					Assert.IsNull (ex.InnerException, "#B3");
					Assert.IsNotNull (ex.Message, "#B4");
				} finally {
					cmd.Dispose ();
				}
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void ExecuteScalar_Transaction_Only ()
		{
			SqlTransaction trans = null;

			conn = new SqlConnection (connectionString);
			conn.Open ();
			trans = conn.BeginTransaction ();

			cmd = new SqlCommand ("select @@version");
			cmd.Transaction = trans;

			try {
				cmd.ExecuteScalar ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// ExecuteScalar: Connection property has not
				// been initialized
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
#if NET_2_0
				Assert.IsTrue (ex.Message.StartsWith ("ExecuteScalar:"), "#5");
#else
				Assert.IsTrue (ex.Message.StartsWith ("ExecuteReader:"), "#5");
#endif
			} finally {
				trans.Dispose ();
			}
		}

		[Test]
		public void ExecuteNonQuery ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			SqlTransaction trans = conn.BeginTransaction ();

			cmd = conn.CreateCommand ();
			cmd.Transaction = trans;

			int result = 0;

			try {
				cmd.CommandText = "Select id from numeric_family where id=1";
				result = cmd.ExecuteNonQuery ();
				Assert.AreEqual (-1, result, "#A1");

				cmd.CommandText = "Insert into numeric_family (id,type_int) values (100,200)";
				result = cmd.ExecuteNonQuery ();
				Assert.AreEqual (1, result, "#A2 One row shud be inserted");

				cmd.CommandText = "Update numeric_family set type_int=300 where id=100";
				result = cmd.ExecuteNonQuery ();
				Assert.AreEqual (1, result, "#A3 One row shud be updated");

				// Test Batch Commands 
				cmd.CommandText = "Select id from numeric_family where id=1;";
				cmd.CommandText += "update numeric_family set type_int=10 where id=1000";
				cmd.CommandText += "update numeric_family set type_int=10 where id=100";
				result = cmd.ExecuteNonQuery ();
				Assert.AreEqual (1, result, "#A4 One row shud be updated");
				
				cmd.CommandText = "Delete from numeric_family where id=100";
				result = cmd.ExecuteNonQuery ();
				Assert.AreEqual (1, result, "#A5 One row shud be deleted");
			}finally {
				trans.Dispose ();
			}

			// Parameterized stored procedure calls

			int int_value = 20;
			string string_value = "output value changed";

			cmd.CommandText =
				"create procedure #tmp_executescalar_outparams " +
				" (@p1 int, @p2 int out, @p3 varchar(200) out) " +
				"as " +
				"select 'test' as 'col1', @p1 as 'col2' " +
				"set @p2 = @p2 * 2 " +
				"set @p3 = N'" + string_value + "' " +
				"select 'second rowset' as 'col1', 2 as 'col2' " +
				"return 1";

			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#tmp_executescalar_outparams";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.Input;
			p1.DbType = DbType.Int32;
			p1.Value = int_value;
			cmd.Parameters.Add (p1);

			SqlParameter p2 = new SqlParameter ();
			p2.ParameterName = "@p2";
			p2.Direction = ParameterDirection.InputOutput;
			p2.DbType = DbType.Int32;
			p2.Value = int_value;
			cmd.Parameters.Add (p2);

			SqlParameter p3 = new SqlParameter ();
			p3.ParameterName = "@p3";
			p3.Direction = ParameterDirection.Output;
			p3.DbType = DbType.String;
			p3.Size = 200;
			cmd.Parameters.Add (p3);

			cmd.ExecuteNonQuery ();
			Assert.AreEqual (int_value * 2, p2.Value, "#B1 ExecuteNonQuery should fill the parameter collection with the outputted values");
			Assert.AreEqual (string_value, p3.Value, "#B2 ExecuteNonQuery should fill the parameter collection with the outputted values");
		}

		[Test]
		public void ExecuteNonQuery_Connection_PendingTransaction ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			using (SqlTransaction trans = conn.BeginTransaction ()) {
				cmd = new SqlCommand ("select @@version", conn);

				try {
					cmd.ExecuteNonQuery ();
					Assert.Fail ("#1");
				} catch (InvalidOperationException ex) {
					// Execute requires the command to have a
					// transaction object when the connection
					// assigned to the command is in a pending
					// local transaction.  The Transaction
					// property of the command has not been
					// initialized
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
					Assert.IsNull (ex.InnerException, "#3");
					Assert.IsNotNull (ex.Message, "#4");
				}
			}
		}

		[Test]
		public void ExecuteNonQuery_Query_Invalid ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();
			cmd = new SqlCommand ("select id1 from numeric_family", conn);

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (SqlException ex) {
				// Invalid column name 'id1'
				Assert.AreEqual (typeof (SqlException), ex.GetType (), "#2");
				Assert.AreEqual ((byte) 16, ex.Class, "#3");
				Assert.IsNull (ex.InnerException, "#4");
				Assert.IsNotNull (ex.Message, "#5");
				Assert.IsTrue (ex.Message.IndexOf ("'id1'") != -1, "#6");
				Assert.AreEqual (207, ex.Number, "#7");
				Assert.AreEqual ((byte) 1, ex.State, "#8");
			} finally {
				conn.Close ();
			}
		}

		[Test]
		public void ExecuteNonQuery_Transaction_NotAssociated ()
		{
			Assert.Ignore ("NotWorking");

			SqlTransaction trans = null;
			SqlConnection connA = null;
			SqlConnection connB = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				trans = connA.BeginTransaction ();

				cmd = new SqlCommand ("select @@version", connB, trans);

				try {
					cmd.ExecuteNonQuery ();
					Assert.Fail ("#A1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
					Assert.IsNull (ex.InnerException, "#A3");
					Assert.IsNotNull (ex.Message, "#A4");
				} finally {
					cmd.Dispose ();
				}

				cmd = new SqlCommand ("select @@version", connB);
				cmd.Transaction = trans;

				try {
					cmd.ExecuteNonQuery ();
					Assert.Fail ("#B1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
					Assert.IsNull (ex.InnerException, "#B3");
					Assert.IsNotNull (ex.Message, "#B4");
				} finally {
					cmd.Dispose ();
				}
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void ExecuteNonQuery_Transaction_Only ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			SqlTransaction trans = conn.BeginTransaction ();

			cmd = new SqlCommand ("select @@version");
			cmd.Transaction = trans;

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// ExecuteNonQuery: Connection property has not
				// been initialized
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
				Assert.IsTrue (ex.Message.StartsWith ("ExecuteNonQuery:"), "#5");
			} finally {
				trans.Dispose ();
			}
		}

		[Test] // bug #412569
		public void ExecuteReader ()
		{
			Assert.Ignore ("bug 412569");

			// Test for command behaviors
			DataTable schemaTable = null;
			SqlDataReader reader = null;

			conn = new SqlConnection (connectionString);
			conn.Open ();
			cmd = new SqlCommand ("", conn);
			cmd.CommandText = "Select id from numeric_family where id <=4 order by id asc;";
			cmd.CommandText += "Select type_bit from numeric_family where id <=4 order by id asc";

			// Test for default command behavior
			reader = cmd.ExecuteReader ();
			int rows = 0;
			int results = 0;
			do {
				while (reader.Read ())
					rows++;
				Assert.AreEqual (4, rows, "#1 Multiple rows shud be returned");
				results++;
				rows = 0;
			} while (reader.NextResult ());
			Assert.AreEqual (2, results, "#2 Multiple result sets shud be returned");
			reader.Close ();

			// Test if closing reader, closes the connection
			reader = cmd.ExecuteReader (CommandBehavior.CloseConnection);
			reader.Close ();
			Assert.AreEqual (ConnectionState.Closed, conn.State,
				"#3 Command Behavior is not followed");
			conn.Open ();

			// Test if row info and primary Key info is returned
			reader = cmd.ExecuteReader (CommandBehavior.KeyInfo);
			schemaTable = reader.GetSchemaTable ();
			Assert.IsTrue (reader.HasRows, "#4 Data Rows shud also be returned");
			Assert.IsTrue ((bool) schemaTable.Rows [0] ["IsKey"],
				"#5 Primary Key info shud be returned");
			reader.Close ();

			// Test only column information is returned 
			reader = cmd.ExecuteReader (CommandBehavior.SchemaOnly);
			schemaTable = reader.GetSchemaTable ();
			Assert.IsFalse (reader.HasRows, "#6 row data shud not be returned");
			Assert.AreEqual (DBNull.Value, schemaTable.Rows [0] ["IsKey"],
				"#7 Primary Key info shud not be returned");
			Assert.AreEqual ("id", schemaTable.Rows [0] ["ColumnName"],
				"#8 Schema Data is Incorrect");
			reader.Close ();

			// Test only one result set (first) is returned 
			reader = cmd.ExecuteReader (CommandBehavior.SingleResult);
			schemaTable = reader.GetSchemaTable ();
			Assert.IsFalse (reader.NextResult (),
				"#9 Only one result set shud be returned");
			Assert.AreEqual ("id", schemaTable.Rows [0] ["ColumnName"],
				"#10 The result set returned shud be the first result set");
			reader.Close ();

			// Test only one row is returned for all result sets 
			// msdotnet doesnt work correctly.. returns only one result set
			reader = cmd.ExecuteReader (CommandBehavior.SingleRow);
			rows = 0;
			results = 0;
			do {
				while (reader.Read ())
					rows++;
				Assert.AreEqual (1, rows, "#11 Only one row shud be returned");
				results++;
				rows = 0;
			} while (reader.NextResult ());

			// LAMESPEC:
			// https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=357085
			Assert.AreEqual (1, results, "#12 Multiple result sets shud be returned");
			reader.Close ();
		}

		[Test]
		public void ExecuteReader_Connection_PendingTransaction ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			using (SqlTransaction trans = conn.BeginTransaction ()) {
				cmd = new SqlCommand ("select @@version", conn);

				try {
					cmd.ExecuteReader ();
					Assert.Fail ("#1");
				} catch (InvalidOperationException ex) {
					// Execute requires the command to have a
					// transaction object when the connection
					// assigned to the command is in a pending
					// local transaction.  The Transaction
					// property of the command has not been
					// initialized
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
					Assert.IsNull (ex.InnerException, "#3");
					Assert.IsNotNull (ex.Message, "#4");
				}
			}
		}

		[Test]
		public void ExecuteReader_Query_Invalid ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();

			cmd = new SqlCommand ("InvalidQuery", conn);
			try {
				cmd.ExecuteReader ();
				Assert.Fail ("#1");
			} catch (SqlException ex) {
				// Could not find stored procedure 'InvalidQuery'
				Assert.AreEqual (typeof (SqlException), ex.GetType (), "#2");
				Assert.AreEqual ((byte) 16, ex.Class, "#3");
				Assert.IsNull (ex.InnerException, "#4");
				Assert.IsNotNull (ex.Message, "#5");
				Assert.IsTrue (ex.Message.IndexOf ("'InvalidQuery'") != -1, "#6");
				Assert.AreEqual (2812, ex.Number, "#7");
				Assert.AreEqual ((byte) 62, ex.State, "#8");
			}
		}

		[Test]
		public void ExecuteReader_Transaction_NotAssociated ()
		{
			Assert.Ignore ("NotWorking");

			SqlTransaction trans = null;
			SqlConnection connA = null;
			SqlConnection connB = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				trans = connA.BeginTransaction ();

				cmd = new SqlCommand ("select @@version", connB, trans);

				try {
					cmd.ExecuteReader ();
					Assert.Fail ("#A1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
					Assert.IsNull (ex.InnerException, "#A3");
					Assert.IsNotNull (ex.Message, "#A4");
				} finally {
					cmd.Dispose ();
				}

				cmd = new SqlCommand ("select @@version", connB);
				cmd.Transaction = trans;

				try {
					cmd.ExecuteReader ();
					Assert.Fail ("#B1");
				} catch (InvalidOperationException ex) {
					// The transaction object is not associated
					// with the connection object
					Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
					Assert.IsNull (ex.InnerException, "#B3");
					Assert.IsNotNull (ex.Message, "#B4");
				} finally {
					cmd.Dispose ();
				}
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void ExecuteReader_Transaction_Only ()
		{
			SqlTransaction trans = null;

			conn = new SqlConnection (connectionString);
			conn.Open ();
			trans = conn.BeginTransaction ();

			cmd = new SqlCommand ("select @@version");
			cmd.Transaction = trans;

			try {
				cmd.ExecuteReader ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// ExecuteReader: Connection property has not
				// been initialized
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
				Assert.IsTrue (ex.Message.StartsWith ("ExecuteReader:"), "#5");
			} finally {
				trans.Dispose ();
			}
		}


		[Test]
		public void PrepareTest_CheckValidStatement ()
		{
			cmd = new SqlCommand ();
			conn = new SqlConnection (connectionString);
			conn.Open ();
			
			cmd.CommandText = "Select id from numeric_family where id=@ID";
			cmd.Connection = conn;

			// Test if Parameters are correctly populated 
			cmd.Parameters.Clear ();
			cmd.Parameters.Add ("@ID", SqlDbType.TinyInt);
			cmd.Parameters["@ID"].Value = 2;
			cmd.Prepare ();
			Assert.AreEqual (2, cmd.ExecuteScalar (), "#3 Prepared Stmt not working");

			cmd.Parameters[0].Value = 3;
			Assert.AreEqual (3, cmd.ExecuteScalar (), "#4 Prep Stmt not working");
			conn.Close ();
		}

		[Test]
		public void PrepareTest ()
		{
			cmd = new SqlCommand ();
			conn = new SqlConnection (connectionString);
			conn.Open ();
			
			cmd.CommandText = "Select id from numeric_family where id=@ID";
			cmd.Connection = conn;

			// Test InvalidOperation Exception is thrown if Parameter Type
			// is not explicitly set
#if NET_2_0
			cmd.Parameters.AddWithValue ("@ID", 2);
#else
			cmd.Parameters.Add ("@ID", 2);
#endif
			try {
				cmd.Prepare ();
				Assert.Fail ("#A1");
			} catch (InvalidOperationException ex) {
				// SqlCommand.Prepare method requires all parameters
				// to have an explicitly set type
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
				Assert.IsNull (ex.InnerException, "#A3");
				Assert.IsNotNull (ex.Message, "#A4");
			}

			// Test Exception is thrown for variable size data  if precision/scale
			// is not set
			cmd.CommandText = "select type_varchar from string_family where type_varchar=@p1";
			cmd.Parameters.Clear ();
			cmd.Parameters.Add ("@p1", SqlDbType.VarChar);
			cmd.Parameters["@p1"].Value = "afasasadadada";
			try {
				cmd.Prepare ();
				Assert.Fail ("#B1");
			} catch (InvalidOperationException ex) {
				// SqlCommand.Prepare method requires all variable
				// length parameters to have an explicitly set
				// non-zero Size
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
				Assert.IsNull (ex.InnerException, "#B3");
				Assert.IsNotNull (ex.Message, "#B4");
			}

 			// Test Exception is not thrown for Stored Procs 
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = "ABFSDSFSF";
			cmd.Prepare ();

			cmd.CommandType = CommandType.Text;
			conn.Close ();
		}

		[Test] // bug #412576
		public void Connection ()
		{
			SqlConnection connA = null;
			SqlConnection connB = null;
			SqlTransaction trans = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				cmd = connA.CreateCommand ();
				cmd.Connection = connB;
				Assert.AreSame (connB, cmd.Connection, "#A1");
				Assert.IsNull (cmd.Transaction, "#A2");
				cmd.Dispose ();

				trans = connA.BeginTransaction ();
				cmd = new SqlCommand ("select @@version", connA, trans);
				cmd.Connection = connB;
				Assert.AreSame (connB, cmd.Connection, "#B1");
				Assert.AreSame (trans, cmd.Transaction, "#B2");
				trans.Dispose ();

				trans = connA.BeginTransaction ();
				cmd = new SqlCommand ("select @@version", connA, trans);
				trans.Rollback ();
				Assert.AreSame (connA, cmd.Connection, "#C1");
				Assert.IsNull (cmd.Transaction, "#C2");
				cmd.Connection = connB;
				Assert.AreSame (connB, cmd.Connection, "#C3");
				Assert.IsNull (cmd.Transaction, "#C4");

				trans = connA.BeginTransaction ();
				cmd = new SqlCommand ("select @@version", connA, trans);
				cmd.Connection = null;
				Assert.IsNull (cmd.Connection, "#D1");
				Assert.AreSame (trans, cmd.Transaction, "#D2");
			}finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void Connection_Reader_Open ()
		{
			SqlConnection connA = null;
			SqlConnection connB = null;
			SqlTransaction trans = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				trans = connA.BeginTransaction ();
				SqlCommand cmdA = new SqlCommand ("select @@version", connA, trans);

				SqlCommand cmdB = new SqlCommand ("select @@version", connA, trans);
				using (SqlDataReader reader = cmdB.ExecuteReader ()) {
#if NET_2_0
					cmdA.Connection = connA;
					Assert.AreSame (connA, cmdA.Connection, "#A1");
					Assert.AreSame (trans, cmdA.Transaction, "#A2");
#else
					try {
						cmdA.Connection = connA;
						Assert.Fail ("#A1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
						Assert.IsNull (ex.InnerException, "#A3");
						Assert.IsNotNull (ex.Message, "#A4");

						Assert.AreSame (connA, cmdA.Connection, "#A5");
						Assert.AreSame (trans, cmdA.Transaction, "#A6");
					}
#endif

#if NET_2_0
					cmdA.Connection = connB;
					Assert.AreSame (connB, cmdA.Connection, "#B1");
					Assert.AreSame (trans, cmdA.Transaction, "#B2");
#else
					try {
						cmdA.Connection = connB;
						Assert.Fail ("#B1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
						Assert.IsNull (ex.InnerException, "#B3");
						Assert.IsNotNull (ex.Message, "#B4");

						Assert.AreSame (connA, cmdA.Connection, "#B5");
						Assert.AreSame (trans, cmdA.Transaction, "#B6");
					}
#endif

#if NET_2_0
					cmdA.Connection = null;
					Assert.IsNull (cmdA.Connection, "#C1");
					Assert.AreSame (trans, cmdA.Transaction, "#C2");
#else
					try {
						cmdA.Connection = null;
						Assert.Fail ("#C1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#C2");
						Assert.IsNull (ex.InnerException, "#C3");
						Assert.IsNotNull (ex.Message, "#C4");

						Assert.AreSame (connA, cmdA.Connection, "#C5");
						Assert.AreSame (trans, cmdA.Transaction, "#C6");
					}
#endif
				}
			} finally {
				if (trans != null)
					trans.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void Transaction ()
		{
			SqlConnection connA = null;
			SqlConnection connB = null;

			SqlTransaction transA = null;
			SqlTransaction transB = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				transA = connA.BeginTransaction ();
				transB = connB.BeginTransaction ();

				SqlCommand cmd = new SqlCommand ("select @@version", connA, transA);
				cmd.Transaction = transA;
				Assert.AreSame (connA, cmd.Connection, "#A1");
				Assert.AreSame (transA, cmd.Transaction, "#A2");
				cmd.Transaction = transB;
				Assert.AreSame (connA, cmd.Connection, "#B1");
				Assert.AreSame (transB, cmd.Transaction, "#B2");
				cmd.Transaction = null;
				Assert.AreSame (connA, cmd.Connection, "#C1");
				Assert.IsNull (cmd.Transaction, "#C2");
			} finally {
				if (transA != null)
					transA.Dispose ();
				if (transB != null)
					transA.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test] // bug #412579
		public void Transaction_Reader_Open ()
		{
			SqlConnection connA = null;
			SqlConnection connB = null;

			SqlTransaction transA = null;
			SqlTransaction transB = null;

			try {
				connA = new SqlConnection (connectionString);
				connA.Open ();

				connB = new SqlConnection (connectionString);
				connB.Open ();

				transA = connA.BeginTransaction ();
				transB = connB.BeginTransaction ();

				SqlCommand cmdA = new SqlCommand ("select * from employee", connA, transA);

				SqlCommand cmdB = new SqlCommand ("select * from employee", connA, transA);
				using (SqlDataReader reader = cmdB.ExecuteReader ()) {
#if NET_2_0
					cmdA.Transaction = transA;
					Assert.AreSame (transA, cmdA.Transaction, "#A1");
#else
					try {
						cmdA.Transaction = transA;
						Assert.Fail ("#A1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#A2");
						Assert.IsNull (ex.InnerException, "#A3");
						Assert.IsNotNull (ex.Message, "#A4");

						Assert.AreSame (transA, cmdA.Transaction, "#A5");
					}
#endif

#if NET_2_0
					cmdA.Transaction = transB;
					Assert.AreSame (transB, cmdA.Transaction, "#B1");
#else
					try {
						cmdA.Transaction = transB;
						Assert.Fail ("#B1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#B2");
						Assert.IsNull (ex.InnerException, "#B3");
						Assert.IsNotNull (ex.Message, "#B4");

						Assert.AreSame (transA, cmdA.Transaction, "#B5");
					}
#endif

#if NET_2_0
					cmdA.Transaction = null;
					Assert.IsNull (cmdA.Transaction, "#C1");
#else
					try {
						cmdA.Transaction = null;
						Assert.Fail ("#C1");
					} catch (InvalidOperationException ex) {
						// The SqlCommand is currently busy
						// Open, Fetching
						Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#C2");
						Assert.IsNull (ex.InnerException, "#C3");
						Assert.IsNotNull (ex.Message, "#C4");

						Assert.AreSame (transA, cmdA.Transaction, "#C5");
					}
#endif
				}

				cmdA.Transaction = transA;
				Assert.AreSame (transA, cmdA.Transaction, "#D1");
				cmdA.Transaction = transB;
				Assert.AreSame (transB, cmdA.Transaction, "#D2");
			} finally {
				if (transA != null)
					transA.Dispose ();
				if (transB != null)
					transA.Dispose ();
				if (connA != null)
					connA.Close ();
				if (connB != null)
					connB.Close ();
			}
		}

		[Test]
		public void ExecuteNonQueryTempProcedureTest ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			try {
				ConnectionManager.Singleton.OpenConnection ();
				// create temp sp here, should normally be created in Setup of test 
				// case, but cannot be done right now because of ug #68978
				DBHelper.ExecuteNonQuery (conn, CREATE_TMP_SP_TEMP_INSERT_PERSON);
				SqlCommand cmd = new SqlCommand();
				cmd.Connection = conn;
				cmd.CommandText = "#sp_temp_insert_employee";
				cmd.CommandType = CommandType.StoredProcedure;
				Object TestPar = "test";
				cmd.Parameters.Add("@fname", SqlDbType.VarChar);
				cmd.Parameters ["@fname"].Value = TestPar;
				Assert.AreEqual(1,cmd.ExecuteNonQuery());
			} finally {
				DBHelper.ExecuteNonQuery (conn, DROP_TMP_SP_TEMP_INSERT_PERSON);
				DBHelper.ExecuteSimpleSP (conn, "sp_clean_person_table");
				ConnectionManager.Singleton.CloseConnection ();
			}
		}

		[Test] // bug #319598
		public void LongQueryTest ()
		{
			SqlConnection conn = new SqlConnection (
							connectionString + ";Pooling=false");
			using (conn) {
				conn.Open ();
				SqlCommand cmd = conn.CreateCommand ();
				String value =  new String ('a', 10000);
				cmd.CommandText = String.Format ("Select '{0}'", value);
				cmd.ExecuteNonQuery ();
			}
		}

		[Test] // bug #319598
		public void LongStoredProcTest()
		{
			SqlConnection conn = new SqlConnection (
							connectionString + ";Pooling=false");
			using (conn) {
				conn.Open ();
				/*int size = conn.PacketSize;*/
				SqlCommand cmd = conn.CreateCommand ();
				// create a temp stored proc
				cmd.CommandText  = "Create Procedure #sp_tmp_long_params ";
				cmd.CommandText += "@p1 nvarchar (4000), ";
				cmd.CommandText += "@p2 nvarchar (4000), ";
				cmd.CommandText += "@p3 nvarchar (4000), ";
				cmd.CommandText += "@p4 nvarchar (4000) out ";
				cmd.CommandText += "As ";
				cmd.CommandText += "Begin ";
				cmd.CommandText += "Set @p4 = N'Hello' ";
				cmd.CommandText += "Return 2 ";
				cmd.CommandText += "End";
				cmd.ExecuteNonQuery ();

				//execute the proc 
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "#sp_tmp_long_params";

				String value =  new String ('a', 4000);
				SqlParameter p1 = new SqlParameter ("@p1",
							SqlDbType.NVarChar,4000);
				p1.Value = value;

				SqlParameter p2 = new SqlParameter ("@p2",
							SqlDbType.NVarChar,4000);
				p2.Value = value;

				SqlParameter p3 = new SqlParameter ("@p3",
							SqlDbType.NVarChar,4000);
				p3.Value = value;

				SqlParameter p4 = new SqlParameter ("@p4",
							SqlDbType.NVarChar,4000);
				p4.Direction = ParameterDirection.Output;

				// for now, name shud be @RETURN_VALUE  
				// can be changed once RPC is implemented 
				SqlParameter p5 = new SqlParameter ("@RETURN_VALUE", SqlDbType.Int);
				p5.Direction = ParameterDirection.ReturnValue;

				cmd.Parameters.Add (p1);
				cmd.Parameters.Add (p2);
				cmd.Parameters.Add (p3);
				cmd.Parameters.Add (p4);
				cmd.Parameters.Add (p5);

				cmd.ExecuteNonQuery ();
				Assert.AreEqual ("Hello", p4.Value, "#1");
				Assert.AreEqual (2, p5.Value, "#2");
			}
		}

		[Test] // bug #319694
		public void DateTimeParameterTest ()
		{
			SqlConnection conn = new SqlConnection (connectionString);
			using (conn) {
				conn.Open ();
				SqlCommand cmd = conn.CreateCommand ();
				cmd.CommandText = "select * from datetime_family where type_datetime=@p1";
				cmd.Parameters.Add ("@p1", SqlDbType.DateTime).Value= "10-10-2005";
				// shudnt cause and exception
				SqlDataReader rdr = cmd.ExecuteReader ();
				rdr.Close ();
			}
		}

		/**
		 * Verifies whether an enum value is converted to a numeric value when
		 * used as value for a numeric parameter (bug #66630)
		 */
		[Test]
		public void EnumParameterTest ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			try {
				ConnectionManager.Singleton.OpenConnection ();
				// create temp sp here, should normally be created in Setup of test 
				// case, but cannot be done right now because of ug #68978
				DBHelper.ExecuteNonQuery (conn, "CREATE PROCEDURE #Bug66630 (" 
							  + "@Status smallint = 7"
							  + ")"
							  + "AS" + Environment.NewLine
							  + "BEGIN" + Environment.NewLine
							  + "SELECT CAST(5 AS int), @Status" + Environment.NewLine
							  + "END");
				
				SqlCommand cmd = new SqlCommand("#Bug66630", conn);
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.Parameters.Add("@Status", SqlDbType.Int).Value = Status.Error;

				using (SqlDataReader dr = cmd.ExecuteReader()) {
					// one record should be returned
					Assert.IsTrue(dr.Read(), "EnumParameterTest#1");
					// we should get two field in the result
					Assert.AreEqual(2, dr.FieldCount, "EnumParameterTest#2");
					// field 1
					Assert.AreEqual("int", dr.GetDataTypeName(0), "EnumParameterTest#3");
					Assert.AreEqual(5, dr.GetInt32(0), "EnumParameterTest#4");
					// field 2
					Assert.AreEqual("smallint", dr.GetDataTypeName(1), "EnumParameterTest#5");
					Assert.AreEqual((short) Status.Error, dr.GetInt16(1), "EnumParameterTest#6");
					// only one record should be returned
					Assert.IsFalse(dr.Read(), "EnumParameterTest#7");
				}
			} finally {
				DBHelper.ExecuteNonQuery (conn, "if exists (select name from sysobjects " +
							  " where name like '#temp_Bug66630' and type like 'P') " +
							  " drop procedure #temp_Bug66630; ");
				ConnectionManager.Singleton.CloseConnection ();
			}
		}

		[Test]
		public void CloneTest ()
		{
			conn = new SqlConnection (connectionString);
			conn.Open ();
			
			SqlTransaction trans = conn.BeginTransaction ();

			cmd = new SqlCommand ();
			cmd.Connection = conn;
			cmd.Transaction = trans;

			SqlCommand clone = (((ICloneable) (cmd)).Clone ()) as SqlCommand;
			Assert.AreSame (conn, clone.Connection);
			Assert.AreSame (trans, clone.Transaction);
		}

		[Test]
		public void StoredProc_NoParameterTest ()
		{
			string query = "create procedure #tmp_sp_proc as begin";
			query += " select 'data' end";
			SqlConnection conn = new SqlConnection (connectionString);
			SqlCommand cmd = conn.CreateCommand ();
			cmd.CommandText = query;
			conn.Open ();
			cmd.ExecuteNonQuery ();
	
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.CommandText = "#tmp_sp_proc";
			using (SqlDataReader reader = cmd.ExecuteReader()) {
				if (reader.Read ())
					Assert.AreEqual ("data", reader.GetString(0),"#1");
				else
					Assert.Fail ("#2 Select shud return data");
			}
			conn.Close ();
		}
	
		[Test]
		public void StoredProc_ParameterTest ()
		{
			string create_query  = CREATE_TMP_SP_PARAM_TEST;
			string drop_query = DROP_TMP_SP_PARAM_TEST;

			SqlConnection conn = new SqlConnection (connectionString);
			
			conn.Open ();
			SqlCommand cmd = conn.CreateCommand ();
			int label = 0;
			string error = string.Empty;
			while (label != -1) {
				try {
					switch (label) {
						case 0 :
							// Test BigInt Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "bigint"));
							rpc_helper_function (cmd, SqlDbType.BigInt, 0,
								Int64.MaxValue, Int64.MaxValue,
								Int64.MaxValue);
							rpc_helper_function (cmd, SqlDbType.BigInt, 0,
								Int64.MinValue, Int64.MinValue,
								Int64.MinValue);
							rpc_helper_function (cmd, SqlDbType.BigInt, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 1 :
							// Test Binary Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "binary(5)"));
							rpc_helper_function (cmd, SqlDbType.Binary, 5,
								new byte [] { 1, 2, 3, 4, 5 },
								new byte [] { 1, 2, 3, 4, 5 },
								new byte [] { 1, 2, 3, 4, 5 });
							/*
							rpc_helper_function (cmd, SqlDbType.Binary, 5,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							*/
							rpc_helper_function (cmd, SqlDbType.Binary, 2,
								new byte [0],
								new byte [] { 0, 0, 0, 0, 0 },
								new byte [] { 0, 0 });
							break;
						case 2 :
							// Test Bit Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "bit"));
							rpc_helper_function (cmd, SqlDbType.Bit, 0,
								true, true, true);
							rpc_helper_function (cmd, SqlDbType.Bit, 0,
								false, false, false);
							rpc_helper_function (cmd, SqlDbType.Bit, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 3 :
							// Testing Char
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "char(10)"));
							rpc_helper_function (cmd, SqlDbType.Char, 10,
								"characters", "characters",
								"characters");
							/*
							rpc_helper_function (cmd, SqlDbType.Char, 3,
								"characters", "cha       ",
								"cha");
							rpc_helper_function (cmd, SqlDbType.Char, 3,
								string.Empty, "          ",
								"   ");
							*/
							rpc_helper_function (cmd, SqlDbType.Char, 5,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 4 :
							// Testing DateTime
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "datetime"));
							rpc_helper_function (cmd, SqlDbType.DateTime, 0, "2079-06-06 23:59:00",
								new DateTime (2079, 6, 6, 23, 59, 0),
								new DateTime (2079, 6, 6, 23, 59, 0));
							rpc_helper_function (cmd, SqlDbType.DateTime, 0, "2009-04-12 10:39:45",
								new DateTime (2009, 4, 12, 10, 39, 45),
								new DateTime (2009, 4, 12, 10, 39, 45));
							rpc_helper_function (cmd, SqlDbType.DateTime, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 5 :
							// Test Decimal Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "decimal(10,2)"));
							rpc_helper_function (cmd, SqlDbType.Decimal, 0,
								10.665, 10.67, 11);
							// FIXME: NUnit 2.2.0 bug
							/*
							rpc_helper_function (cmd, SqlDbType.Decimal, 0,
								0m, 0m, 0m);
							*/
							rpc_helper_function (cmd, SqlDbType.Decimal, 0,
								-5.657, -5.66m, -6);
							rpc_helper_function (cmd, SqlDbType.Decimal, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 6 :
							// Test Float Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "float"));
							rpc_helper_function (cmd, SqlDbType.Float, 0,
								10.0, 10.0, 10.0);
							rpc_helper_function (cmd, SqlDbType.Float, 0,
								10.54, 10.54, 10.54);
							rpc_helper_function (cmd, SqlDbType.Float, 0,
								0, 0, 0);
							rpc_helper_function (cmd, SqlDbType.Float, 0,
								-5.34, -5.34, -5.34);
							rpc_helper_function (cmd, SqlDbType.Float, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 7 :
							// Testing Image
							/* NOT WORKING
							   DBHelper.ExecuteNonQuery (conn,
							   String.Format(create_query, "image"));
							   rpc_helper_function (cmd, SqlDbType.Image, 0, );
							   rpc_helper_function (cmd, SqlDbType.Image, 0, );
							   rpc_helper_function (cmd, SqlDbType.Image, 0, );
							   /* NOT WORKING*/
							break;
						case 8 :
							// Test Integer Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "int"));
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								10, 10, 10);
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								0, 0, 0);
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								-5, -5, -5);
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								int.MaxValue, int.MaxValue,
								int.MaxValue);
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								int.MinValue, int.MinValue,
								int.MinValue);
							rpc_helper_function (cmd, SqlDbType.Int, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 9 :
							// Test Money Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "money"));
							// FIXME: NUnit 2.2.0 bug
							/*
							rpc_helper_function (cmd, SqlDbType.Money, 0,
								10m, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.Money, 0,
								10.54, 10.54m, 10.54m);
							rpc_helper_function (cmd, SqlDbType.Money, 0,
								0, 0m, 0m);
							*/
							/*
							rpc_helper_function (cmd, SqlDbType.Money, 0,
								-5.34, -5.34m, -5.34m);
							*/
							rpc_helper_function (cmd, SqlDbType.Money, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 23 :
							// Test NChar Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "nchar(10)"));
							rpc_helper_function (cmd, SqlDbType.NChar, 10,
								"characters", "characters",
								"characters");
							rpc_helper_function (cmd, SqlDbType.NChar, 3,
								"characters", "cha       ",
								"cha");
							rpc_helper_function (cmd, SqlDbType.NChar, 3,
								string.Empty, "          ",
								"   ");
							/*
							rpc_helper_function (cmd, SqlDbType.NChar, 5,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							*/
							break;
						case 10 :
							// Test NText Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "ntext"));
							/*
							rpc_helper_function (cmd, SqlDbType.NText, 0, "ntext");
							rpc_helper_function (cmd, SqlDbType.NText, 0, "");
							rpc_helper_function (cmd, SqlDbType.NText, 0, null);
							*/
							break;
						case 11 :
							// Test NVarChar Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "nvarchar(10)"));
							rpc_helper_function (cmd, SqlDbType.NVarChar, 10,
								"nvarchar", "nvarchar", "nvarchar");
							rpc_helper_function (cmd, SqlDbType.NVarChar, 3,
								"nvarchar", "nva", "nva");
							/*
							rpc_helper_function (cmd, SqlDbType.NVarChar, 10,
								string.Empty, string.Empty, string.Empty);
							rpc_helper_function (cmd, SqlDbType.NVarChar, 10,
								DBNull.Value, DBNull.Value, DBNull.Value);
							*/
							break;
						case 12 :
							// Test Real Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "real"));
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								10m, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								10d, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								0, 0m, 0m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								3.54d, 3.54m, 3.54m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								10, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								10.5f, 10.5m, 10.5m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								3.5d, 3.5m, 3.5m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								4.54m, 4.54m, 4.54m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								-4.54m, -4.54m, -4.54m);
							rpc_helper_function (cmd, SqlDbType.Real, 0,
								DBNull.Value, DBNull.Value, DBNull.Value);
							break;
						case 13 :
							// Test SmallDateTime Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "smalldatetime"));
							rpc_helper_function (cmd, SqlDbType.SmallDateTime, 0,
								"6/6/2079 11:59:00 PM",
								new DateTime (2079, 6, 6, 23, 59, 0),
								new DateTime (2079, 6, 6, 23, 59, 0));
							rpc_helper_function (cmd, SqlDbType.SmallDateTime, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 14 :
							// Test SmallInt Param
							DBHelper.ExecuteNonQuery (conn,
								String.Format (create_query, "smallint"));
							rpc_helper_function (cmd, SqlDbType.SmallInt, 0,
								10, 10, 10);
							rpc_helper_function (cmd, SqlDbType.SmallInt, 0,
								-10, -10, -10);
							rpc_helper_function (cmd, SqlDbType.SmallInt, 0,
								short.MaxValue, short.MaxValue,
								short.MaxValue);
							rpc_helper_function (cmd, SqlDbType.SmallInt, 0,
								short.MinValue, short.MinValue,
								short.MinValue);
							rpc_helper_function (cmd, SqlDbType.SmallInt, 0,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							break;
						case 15 :
							// Test SmallMoney Param
							DBHelper.ExecuteNonQuery (conn,
									String.Format (create_query, "smallmoney"));
							// FIXME: NUnit 2.2.0 bug
							/*
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								10.0d, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								0, 0m, 0m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								3.54d, 3.54m, 3.54m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								10, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								10.5f, 10.5m, 10.5m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								3.5d, 3.5m, 3.5m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								4.54m, 4.54m, 4.54m);
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								-4.54m, -4.54m, -4.54m);
							*/
							rpc_helper_function (cmd, SqlDbType.SmallMoney, 0,
								DBNull.Value, DBNull.Value, DBNull.Value);
							break;
						case 16 :
							// Test Text Param
							DBHelper.ExecuteNonQuery (conn,
									String.Format (create_query, "text"));
							/*
							rpc_helper_function (cmd, SqlDbType.Text, 0, "text");
							rpc_helper_function (cmd, SqlDbType.Text, 0, "");
							rpc_helper_function (cmd, SqlDbType.Text, 0, null);
							*/
							break;
						case 17 :
							// Test TimeStamp Param
							/* NOT WORKING
							   DBHelper.ExecuteNonQuery (conn,
							   String.Format(create_query,"timestamp"));
							   rpc_helper_function (cmd, SqlDbType.TimeStamp, 0, "");
							   rpc_helper_function (cmd, SqlDbType.TimeStamp, 0, "");
							   rpc_helper_function (cmd, SqlDbType.TimeStamp, 0, null);
							 */
							break;
						case 18 :
							// Test TinyInt Param
							DBHelper.ExecuteNonQuery (conn,
									String.Format(create_query,"tinyint"));
							rpc_helper_function (cmd, SqlDbType.TinyInt, 0,
								10.0d, 10m, 10m);
							rpc_helper_function (cmd, SqlDbType.TinyInt, 0,
								0, 0, 0);
							rpc_helper_function (cmd, SqlDbType.TinyInt, 0,
								byte.MaxValue, byte.MaxValue, byte.MaxValue);
							rpc_helper_function (cmd, SqlDbType.TinyInt, 0,
								byte.MinValue, byte.MinValue, byte.MinValue);
							break;
						case 19 :
							// Test UniqueIdentifier Param
							/*
							DBHelper.ExecuteNonQuery (conn,
									String.Format(create_query,"uniqueidentifier"));
							rpc_helper_function (cmd, SqlDbType.UniqueIdentifier, 0, "0f159bf395b1d04f8c2ef5c02c3add96");
							rpc_helper_function (cmd, SqlDbType.UniqueIdentifier, 0, null);
							*/
							break;
						case 20 :
							// Test VarBinary Param
							/* NOT WORKING
							   DBHelper.ExecuteNonQuery (conn,
							   String.Format(create_query,"varbinary (10)"));
							   rpc_helper_function (cmd, SqlDbType.VarBinary, 0,);
							   rpc_helper_function (cmd, SqlDbType.VarBinary, 0,);
							   rpc_helper_function (cmd, SqlDbType.VarBinary, 0, null);
							 */
							break;
						case 21 :
							// Test Varchar Param
							DBHelper.ExecuteNonQuery (conn,
									String.Format(create_query,"varchar(10)"));
							rpc_helper_function (cmd, SqlDbType.VarChar, 7,
								"VarChar", "VarChar", "VarChar");
							rpc_helper_function (cmd, SqlDbType.VarChar, 5,
								"Var", "Var", "Var");
							/*
							rpc_helper_function (cmd, SqlDbType.VarChar, 3,
								"Varchar", "Var", "Var");
							rpc_helper_function (cmd, SqlDbType.VarChar, 10,
								string.Empty, string.Empty, string.Empty);
							rpc_helper_function (cmd, SqlDbType.VarChar, 10,
								DBNull.Value, DBNull.Value,
								DBNull.Value);
							*/
							break;
						case 22 :
							// Test Variant Param
							/* NOT WORKING
							   DBHelper.ExecuteNonQuery (conn,
							   String.Format(create_query,"variant"));
							   rpc_helper_function (cmd, SqlDbType.Variant, 0, );
							   rpc_helper_function (cmd, SqlDbType.Variant, 0, );
							   rpc_helper_function (cmd, SqlDbType.Variant, 0, null);
							 */
							break;
						default :
							label = -2;
							break;
					}
				}catch (AssertionException ex) {
					error += String.Format (" Case {0} INCORRECT VALUE : {1}\n", label, ex.ToString ());
				}catch (Exception ex) {
					error += String.Format (" Case {0} NOT WORKING : {1}\n", label, ex.ToString ());
				}

				label++;
				if (label != -1)
					DBHelper.ExecuteNonQuery (conn, drop_query);
			}

			if (error.Length != 0)
				Assert.Fail (error);
		}

		private void rpc_helper_function (SqlCommand cmd, SqlDbType type, int size, object input, object expectedRead, object expectedOut)
		{
			cmd.Parameters.Clear ();
			SqlParameter param1;
			SqlParameter param2;
			if (size != 0) {
				param1 = new SqlParameter ("@param1", type, size);
				param2 = new SqlParameter ("@param2", type, size);
			} else {
				param1 = new SqlParameter ("@param1", type);
				param2 = new SqlParameter ("@param2", type);
			}

			SqlParameter retval = new SqlParameter ("retval", SqlDbType.Int);
			param1.Value = input;
			param1.Direction = ParameterDirection.Input;
			param2.Direction = ParameterDirection.Output;
			retval.Direction = ParameterDirection.ReturnValue;
			cmd.Parameters.Add (param1);
			cmd.Parameters.Add (param2);
			cmd.Parameters.Add (retval);
			cmd.CommandText = "#tmp_sp_param_test";
			cmd.CommandType = CommandType.StoredProcedure;
			using (SqlDataReader reader = cmd.ExecuteReader ()) {
				Assert.IsTrue (reader.Read (), "#1");
				Assert.AreEqual (expectedRead, reader.GetValue (0), "#2");
				Assert.IsFalse (reader.Read (), "#3");
			}
			Assert.AreEqual (expectedOut, param2.Value, "#4");
			Assert.AreEqual (5, retval.Value, "#5");
		}

		[Test]
		public void OutputParamSizeTest1 ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			ConnectionManager.Singleton.OpenConnection ();
			cmd = new SqlCommand ();
			cmd.Connection = conn;

			cmd.CommandText = "create procedure #testsize (@p1 as varchar(10) output) as return";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#testsize";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.InputOutput;
			p1.DbType = DbType.String;
			p1.IsNullable = false;
			cmd.Parameters.Add (p1);

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// String[0]: the Size property has an invalid
				// size of 0
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
			}
		}

		[Test]
		public void OutputParamSizeTest2 ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			ConnectionManager.Singleton.OpenConnection ();
			cmd = new SqlCommand ();
			cmd.Connection = conn;

			cmd.CommandText = "create procedure #testsize (@p1 as varchar(10) output) as return";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#testsize";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.Output;
			p1.DbType = DbType.String;
			p1.IsNullable = false;
			cmd.Parameters.Add (p1);

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// String[0]: the Size property has an invalid
				// size of 0
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
			}
		}

		[Test]
		public void OutputParamSizeTest3 ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			ConnectionManager.Singleton.OpenConnection ();
			cmd = new SqlCommand ();
			cmd.Connection = conn;

			cmd.CommandText = "create procedure #testsize (@p1 as varchar(10) output) as return";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#testsize";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.InputOutput;
			p1.DbType = DbType.String;
			p1.IsNullable = true;
			cmd.Parameters.Add (p1);

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// String[0]: the Size property has an invalid
				// size of 0
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
			}
		}

		[Test]
		public void OutputParamSizeTest4 ()
		{
			conn = (SqlConnection) ConnectionManager.Singleton.Connection;
			ConnectionManager.Singleton.OpenConnection ();
			cmd = new SqlCommand ();
			cmd.Connection = conn;

			cmd.CommandText = "create procedure #testsize (@p1 as varchar(10) output) as return";
			cmd.CommandType = CommandType.Text;
			cmd.ExecuteNonQuery ();

			cmd.CommandText = "#testsize";
			cmd.CommandType = CommandType.StoredProcedure;

			SqlParameter p1 = new SqlParameter ();
			p1.ParameterName = "@p1";
			p1.Direction = ParameterDirection.Output;
			p1.DbType = DbType.String;
			p1.IsNullable = true;
			cmd.Parameters.Add (p1);

			try {
				cmd.ExecuteNonQuery ();
				Assert.Fail ("#1");
			} catch (InvalidOperationException ex) {
				// String[0]: the Size property has an invalid
				// size of 0
				Assert.AreEqual (typeof (InvalidOperationException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
			}
		}

#if NET_2_0
		[Test]
		public void NotificationTest ()
		{
			cmd = new SqlCommand ();
			SqlNotificationRequest notification = new SqlNotificationRequest("MyNotification","MyService",15);
			Assert.AreEqual (null, cmd.Notification, "#1 The default value for this property should be null");
			cmd.Notification = notification;
			Assert.AreEqual ("MyService", cmd.Notification.Options, "#2 The value should be MyService as the constructor is initiated with this value");
			Assert.AreEqual (15, cmd.Notification.Timeout, "#2 The value should be 15 as the constructor is initiated with this value");
		}

		[Test]
		public void NotificationAutoEnlistTest ()
		{
			cmd = new SqlCommand ();
			Assert.AreEqual (true, cmd.NotificationAutoEnlist, "#1 Default value of the property should be true");
			cmd.NotificationAutoEnlist = false;
			Assert.AreEqual (false, cmd.NotificationAutoEnlist, "#2 The value of the property should be false after setting it to false");
		}

		[Test]
		public void BeginExecuteXmlReaderTest ()
		{
			cmd = new SqlCommand ();
			string connectionString1 = null;
			connectionString1 = ConnectionManager.Singleton.ConnectionString + "Asynchronous Processing=true";
			try {
				SqlConnection conn1 = new SqlConnection (connectionString1);
				conn1.Open ();
				cmd.CommandText = "Select lname from employee where id<2 FOR XML AUTO, XMLDATA";
				cmd.Connection = conn1;
			
				IAsyncResult result = cmd.BeginExecuteXmlReader ();
				XmlReader reader = cmd.EndExecuteXmlReader (result);
				while (reader.Read ()) {
					if (reader.LocalName.ToString () == "employee")
						Assert.AreEqual ("kumar", reader["lname"], "#1 ");
				}
			} finally {
				ConnectionManager.Singleton.CloseConnection ();
			}
		}
		
		[Test]
		public void BeginExecuteXmlReaderExceptionTest ()
		{
			cmd = new SqlCommand ();
			try {
				SqlConnection conn = new SqlConnection (connectionString);
				conn.Open ();
				cmd.CommandText = "Select lname from employee where id<2 FOR XML AUTO, XMLDATA";
				cmd.Connection = conn;
				
				try {
					/*IAsyncResult result = */cmd.BeginExecuteXmlReader ();
				} catch (InvalidOperationException) {
					Assert.AreEqual (ConnectionManager.Singleton.ConnectionString, connectionString, "#1 Connection string has changed");
					return;
				}
				Assert.Fail ("Expected Exception InvalidOperationException not thrown");
			} finally {
				ConnectionManager.Singleton.CloseConnection ();
			}
		}
#endif

		[Test]
		public void SqlCommandDisposeTest ()
		{
			IDataReader reader = null;
			try {
				conn = (SqlConnection) ConnectionManager.Singleton.Connection;
				ConnectionManager.Singleton.OpenConnection ();

				IDbCommand command = conn.CreateCommand ();
				try {
					string sql = "SELECT * FROM employee";
					command.CommandText = sql;
					reader = command.ExecuteReader();
				} finally {
					command.Dispose();
				}
				while (reader.Read());
			} finally {
				reader.Dispose();
				ConnectionManager.Singleton.CloseConnection ();
			}
		}

		private void bug326182_OutputParamMixupTestCommon (int paramOrder, 
		                                                   out int param0Val,
		                                                   out int param1Val,
		                                                   out int param2Val,
		                                                   out int param3Val,
		                                                   out int rvalVal)
		{
			try {
				conn = (SqlConnection) ConnectionManager.Singleton.Connection;
				ConnectionManager.Singleton.OpenConnection ();
				string create_proc = "CREATE procedure #sp_326182 ( " + Environment.NewLine +
						"@param0 int out," + Environment.NewLine +
						"@param1 int out," + Environment.NewLine +
						"@param2 int out," + Environment.NewLine +
						"@param3 int out" + Environment.NewLine + 
						")" + Environment.NewLine + 
						"as" + Environment.NewLine +
						"set @param0 = 100" + Environment.NewLine +
						"set @param1 = 101" + Environment.NewLine +
						"set @param2 = 102" + Environment.NewLine +
						"set @param3 = 103" + Environment.NewLine +
						"return 2";

				try {
					SqlParameter param0 = new SqlParameter ("@param0", SqlDbType.Int);
					SqlParameter param1 = new SqlParameter ("@param1", SqlDbType.Int);
					SqlParameter param2 = new SqlParameter ("@param2", SqlDbType.Int);
					SqlParameter param3 = new SqlParameter ("@param3", SqlDbType.Int);
					SqlParameter rval = new SqlParameter ("@RETURN_VALUE", SqlDbType.Int);
					
					cmd = new SqlCommand();
					cmd.CommandText = create_proc;
					cmd.CommandType = CommandType.Text;
					cmd.Connection = conn;
					cmd.CommandTimeout = 90;
					cmd.ExecuteNonQuery ();
					
					cmd.CommandText = "dbo.[#sp_326182]";
					cmd.CommandType = CommandType.StoredProcedure;
					
					param0.Direction = ParameterDirection.Output;
					param1.Direction = ParameterDirection.Output;
					param2.Direction = ParameterDirection.Output;
					param3.Direction = ParameterDirection.Output;
					rval.Direction = ParameterDirection.ReturnValue;
					
					switch (paramOrder) {
					case 1: cmd.Parameters.Add (param0);
						cmd.Parameters.Add (param1);
						cmd.Parameters.Add (rval);
						cmd.Parameters.Add (param2);
						cmd.Parameters.Add (param3);
						break;
					case 2: cmd.Parameters.Add (rval);
						cmd.Parameters.Add (param1);
						cmd.Parameters.Add (param0);
						cmd.Parameters.Add (param2);
						cmd.Parameters.Add (param3);
						break;
					default: cmd.Parameters.Add (param0);
						cmd.Parameters.Add (param1);
						cmd.Parameters.Add (param2);
						cmd.Parameters.Add (param3);
						cmd.Parameters.Add (rval);
						break;
					}
					
					cmd.ExecuteNonQuery ();
					
					/* Copy the param values to variables, just in case if 
					 * tests fail, we don't want the created sp to exist */
					param3Val = (int)cmd.Parameters["@param3"].Value;
					param1Val = (int)cmd.Parameters["@param1"].Value;
					rvalVal = (int)cmd.Parameters["@RETURN_VALUE"].Value;
					param2Val = (int)cmd.Parameters["@param2"].Value;
					param0Val = (int)cmd.Parameters["@param0"].Value;
				} finally {
					/* Delete the created stored procedure */
					cmd = conn.CreateCommand ();
					cmd.CommandText = "drop procedure #sp_326182";
					cmd.ExecuteNonQuery ();

					cmd.Dispose();
					cmd = null;
				}
			} finally {
				ConnectionManager.Singleton.CloseConnection ();
				conn = null;
			}
		}
		
		[Test]
		public void bug326182_OutputParamMixupTest_Normal ()
		{
			int param0Val, param1Val, param2Val, param3Val, rvalVal;
			
			//param0Val = param1Val = param2Val = param3Val = rvalVal = 0;
			
			bug326182_OutputParamMixupTestCommon (0, out param0Val, out param1Val, 
			                                      out param2Val, out param3Val, out rvalVal);
			Assert.AreEqual (103, param3Val);
			Assert.AreEqual (101, param1Val);
			Assert.AreEqual (2, rvalVal);
			Assert.AreEqual (102, param2Val);
			Assert.AreEqual (100, param0Val);
		}

		[Test]
		public void bug326182_OutputParamMixupTest_RValInBetween ()
		{
			int param0Val, param1Val, param2Val, param3Val, rvalVal;
			
			bug326182_OutputParamMixupTestCommon (1, out param0Val, out param1Val, 
			                                      out param2Val, out param3Val, out rvalVal);
			Assert.AreEqual (103, param3Val);
			Assert.AreEqual (101, param1Val);
			Assert.AreEqual (2, rvalVal);
			Assert.AreEqual (102, param2Val);
			Assert.AreEqual (100, param0Val);
		}

		[Test]
		public void bug326182_OutputParamMixupTest_RValFirst ()
		{
			int param0Val, param1Val, param2Val, param3Val, rvalVal;
			
			bug326182_OutputParamMixupTestCommon (2, out param0Val, out param1Val, 
			                                      out param2Val, out param3Val, out rvalVal);
			Assert.AreEqual (103, param3Val);
			Assert.AreEqual (101, param1Val);
			Assert.AreEqual (2, rvalVal);
			Assert.AreEqual (102, param2Val);
			Assert.AreEqual (100, param0Val);
		}

		private enum Status
		{
			OK = 0,
			Error = 3
		}

		private readonly string CREATE_TMP_SP_PARAM_TEST = "create procedure #tmp_sp_param_test (@param1 {0}, @param2 {0} output) as begin select @param1 set @param2=@param1 return 5 end";
		private readonly string DROP_TMP_SP_PARAM_TEST = "drop procedure #tmp_sp_param_test";

		private readonly string CREATE_TMP_SP_TEMP_INSERT_PERSON = ("create procedure #sp_temp_insert_employee ( " + Environment.NewLine + 
									    "@fname varchar (20)) " + Environment.NewLine + 
									    "as " + Environment.NewLine + 
									    "begin" + Environment.NewLine + 
									    "declare @id int;" + Environment.NewLine + 
									    "select @id = max (id) from employee;" + Environment.NewLine + 
									    "set @id = @id + 6000 + 1;" + Environment.NewLine + 
									    "insert into employee (id, fname, dob, doj) values (@id, @fname, '1980-02-11', getdate ());" + Environment.NewLine + 
									    "return @id;" + Environment.NewLine + 
									    "end");

		private readonly string DROP_TMP_SP_TEMP_INSERT_PERSON = ("if exists (select name from sysobjects where " + Environment.NewLine + 
									  "name = '#sp_temp_insert_employee' and type = 'P') " + Environment.NewLine + 
									  "drop procedure #sp_temp_insert_employee; ");
	}
}

