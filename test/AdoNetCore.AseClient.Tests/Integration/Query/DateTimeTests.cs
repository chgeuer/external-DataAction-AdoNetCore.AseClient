﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using AdoNetCore.AseClient.Tests.ConnectionProvider;
using Dapper;
using NUnit.Framework;

namespace AdoNetCore.AseClient.Tests.Integration.Query
{
    [Category("basic")]
#if NET_FRAMEWORK
    [TestFixture(typeof(SapConnectionProvider))]
#endif
    [TestFixture(typeof(CoreFxConnectionProvider))]
    public class DateTimeTests<T> where T : IConnectionProvider
    {
        private DbConnection GetConnection()
        {
            return Activator.CreateInstance<T>().GetConnection(ConnectionStrings.PooledUtf8);
        }

        [TestCaseSource(nameof(SelectLiteral_Cases))]
        public void SelectLiteral_Dapper(string input, DateTime? expected)
        {
            using (var connection = GetConnection())
            {
                Assert.AreEqual(expected, connection.ExecuteScalar<DateTime>($"select convert(datetime, {input})"));
            }
        }

        [TestCaseSource(nameof(SelectLiteral_Cases))]
        public void SelectLiteral_ExecuteScalar(string input, object expected)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"select convert(datetime, {input})";
                    Assert.AreEqual(expected ?? DBNull.Value, command.ExecuteScalar());
                }
            }
        }

        public static IEnumerable<TestCaseData> SelectLiteral_Cases()
        {
            //46 2e ff ff 00 00 00 00; p1: -53690, p2: 0
            yield return new TestCaseData("'1753-01-01 00:00:00'", new DateTime(1753, 1, 1, 0, 0, 0));
            //46 2e ff ff d4 80 8b 01; p1: -53690, p2: 25919700
            yield return new TestCaseData("'1753-01-01 23:59:59'", new DateTime(1753, 1, 1, 23, 59, 59));
            //46 2e ff ff ff 81 8b 01; p1: -53690, p2: 25919999
            yield return new TestCaseData("'1753-01-01 23:59:59.997'", new DateTime(1753, 1, 1, 23, 59, 59, 996));
            //b2 2f ff ff 00 00 00 00; p1: -53326, p2: 0
            yield return new TestCaseData("'1753-12-31 00:00:00'", new DateTime(1753, 12, 31, 0, 0, 0));
            //b2 2f ff ff d4 80 8b 01; p1: -53326, p2: 25919700
            yield return new TestCaseData("'1753-12-31 23:59:59'", new DateTime(1753, 12, 31, 23, 59, 59));
            //b2 2f ff ff ff 81 8b 01; p1: -53326, p2: 25919999
            yield return new TestCaseData("'1753-12-31 23:59:59.997'", new DateTime(1753, 12, 31, 23, 59, 59, 996));

            //00 00 00 00 00 00 00 00; p1: 0, p2: 0
            yield return new TestCaseData("'1900-01-01 00:00:00'", new DateTime(1900, 1, 1, 0, 0, 0));
            //00 00 00 00 d4 80 8b 01; p1: 0, p2: 25919700
            yield return new TestCaseData("'1900-01-01 23:59:59'", new DateTime(1900, 1, 1, 23, 59, 59));
            //p2: 25919999
            yield return new TestCaseData("'1900-01-01 23:59:59.997'", new DateTime(1900, 1, 1, 23, 59, 59, 996));
            //6c 01 00 00 00 00 00 00; p1: 364, p2: 0
            yield return new TestCaseData("'1900-12-31 00:00:00'", new DateTime(1900, 12, 31, 0, 0, 0));
            //6c 01 00 00 d4 80 8b 01; p1: 364, p2: 25919700
            yield return new TestCaseData("'1900-12-31 23:59:59'", new DateTime(1900, 12, 31, 23, 59, 59));
            //p2: 25919999
            yield return new TestCaseData("'1900-12-31 23:59:59.997'", new DateTime(1900, 12, 31, 23, 59, 59, 996));

            //13 23 2d 00 00 00 00 00; p1: 2958099, p2: 0
            //https://www.wolframalpha.com/input/?i=2958099+days+before+9999-01-01 = 1900-01-01
            yield return new TestCaseData("'9999-01-01 00:00:00'", new DateTime(9999, 01, 01, 0, 0, 0));
            //13 23 2d 00 d4 80 8b 01; p1: 2958099, p2: 25919700
            yield return new TestCaseData("'9999-01-01 23:59:59'", new DateTime(9999, 01, 01, 23, 59, 59));
            //p2: 25919999
            yield return new TestCaseData("'9999-01-01 23:59:59.997'", new DateTime(9999, 01, 01, 23, 59, 59, 996));
            //7f 24 2d 00 00 00 00 00; p1: 2958463, p2: 0
            yield return new TestCaseData("'9999-12-31 00:00:00'", new DateTime(9999, 12, 31, 0, 0, 0));
            //7f 24 2d 00 d4 80 8b 01; p1: 2958463, p2: 25919700
            yield return new TestCaseData("'9999-12-31 23:59:59'", new DateTime(9999, 12, 31, 23, 59, 59));
            //p2: 25919999
            yield return new TestCaseData("'9999-12-31 23:59:59.997'", new DateTime(9999, 12, 31, 23, 59, 59, 996));
        }

        [TestCaseSource(nameof(TestParameter_Cases))]
        public void SelectParameter_Dapper(string _, object expected)
        {
            using (var connection = GetConnection())
            {
                var p = new DynamicParameters();
                p.Add("@expected", expected, DbType.DateTime);
                Assert.AreEqual(expected, connection.ExecuteScalar<DateTime?>("select @expected", p));
            }
        }

        [TestCaseSource(nameof(TestParameter_Cases))]
        public void SelectParameter_ExecuteScalar(string _, object expected)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select @expected";

                    var p = command.CreateParameter();
                    p.DbType = DbType.DateTime;
                    p.ParameterName = "@expected";
                    p.Value = expected ?? DBNull.Value;
                    command.Parameters.Add(p);

                    Assert.AreEqual(expected ?? DBNull.Value, command.ExecuteScalar());
                }
            }
        }

        public static IEnumerable<TestCaseData> TestParameter_Cases()
        {
            yield return new TestCaseData("null", null);
            yield return new TestCaseData("1753_1", new DateTime(1753, 1, 1, 0, 0, 0));
            yield return new TestCaseData("1753_2", new DateTime(1753, 1, 1, 23, 59, 59));
            yield return new TestCaseData("1753_3", new DateTime(1753, 1, 1, 23, 59, 59, 996));
            yield return new TestCaseData("1753_4", new DateTime(1753, 12, 31, 0, 0, 0));
            yield return new TestCaseData("1753_5", new DateTime(1753, 12, 31, 23, 59, 59));
            yield return new TestCaseData("1753_6", new DateTime(1753, 12, 31, 23, 59, 59, 996));
            yield return new TestCaseData("1900_1", new DateTime(1900, 1, 1, 0, 0, 0));
            yield return new TestCaseData("1900_2", new DateTime(1900, 1, 1, 23, 59, 59));
            yield return new TestCaseData("1900_3", new DateTime(1900, 1, 1, 23, 59, 59, 996));
            yield return new TestCaseData("1900_4", new DateTime(1900, 12, 31, 0, 0, 0));
            yield return new TestCaseData("1900_5", new DateTime(1900, 12, 31, 23, 59, 59));
            yield return new TestCaseData("1900_6", new DateTime(1900, 12, 31, 23, 59, 59, 996));
            yield return new TestCaseData("9999_1", new DateTime(9999, 01, 01, 0, 0, 0));
            yield return new TestCaseData("9999_2", new DateTime(9999, 01, 01, 23, 59, 59));
            yield return new TestCaseData("9999_3", new DateTime(9999, 01, 01, 23, 59, 59, 996));
            yield return new TestCaseData("9999_4", new DateTime(9999, 12, 31, 0, 0, 0));
            yield return new TestCaseData("9999_5", new DateTime(9999, 12, 31, 23, 59, 59));
            yield return new TestCaseData("9999_6", new DateTime(9999, 12, 31, 23, 59, 59, 996));
        }

        [Test]
        public void SelectDateTime_Parameter_Now_ShouldWork()
        {
            using (var connection = GetConnection())
            {
                var now = DateTime.Now;
                var expected = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Kind);
                Assert.AreEqual(expected, connection.ExecuteScalar<DateTime?>("select @expected", new { expected }));
            }
        }
    }
}
