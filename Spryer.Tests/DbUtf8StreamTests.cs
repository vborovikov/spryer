namespace Spryer.Tests;

using System;
using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DbUtf8StreamTests
{
    [TestMethod]
    public async Task DbUtf8Stream_10Kb_Parsed()
    {
        await using var stream = new DbUtf8Stream(new JsonDataReader("employees_10KB.json"));
        var employees = await JsonSerializer.DeserializeAsync<Employee[]>(stream);

        Assert.IsNotNull(employees);
        Assert.AreEqual(6, employees.Length);
    }

    [TestMethod]
    public async Task DbUtf8Stream_100Kb_Parsed()
    {
        await using var stream = new DbUtf8Stream(new JsonDataReader("employees_100KB.json"));
        var employees = await JsonSerializer.DeserializeAsync<Employee[]>(stream);

        Assert.IsNotNull(employees);
        Assert.AreEqual(55, employees.Length);
    }

    [TestMethod]
    public void DbUtf8Stream_10KbSync_Parsed()
    {
        using var stream = new DbUtf8Stream(new JsonDataReader("employees_10KB.json"));
        var employees = JsonSerializer.Deserialize<Employee[]>(stream);

        Assert.IsNotNull(employees);
        Assert.AreEqual(6, employees.Length);
    }

    [TestMethod]
    public void DbUtf8Stream_100KbSync_Parsed()
    {
        using var stream = new DbUtf8Stream(new JsonDataReader("employees_100KB.json"));
        var employees = JsonSerializer.Deserialize<Employee[]>(stream);

        Assert.IsNotNull(employees);
        Assert.AreEqual(55, employees.Length);
    }

    record Employee
    {
        public int employee_id { get; init; }
        public string name { get; init; }
        public int age { get; init; }
        public string position { get; init; }
        public string department { get; init; }
        public string manager { get; init; }
        public Contact contact { get; init; }
        public Address address { get; init; }
        public Family[] family { get; init; }
        public WorkHistory[] work_history { get; init; }
        public Project[] projects { get; init; }
    }

    record Contact
    {
        public string email { get; init; }
        public string phone { get; init; }
    }

    record Address
    {
        public string street { get; init; }
        public string city { get; init; }
        public string zip_code { get; init; }
        public string country { get; init; }
    }

    record Family
    {
        public string relation { get; init; }
        public string name { get; init; }
        public int age { get; init; }
        public Contact contact { get; init; }
    }

    record WorkHistory
    {
        public string company { get; init; }
        public string role { get; init; }
        public string duration { get; init; }
    }

    record Project
    {
        public string project_id { get; init; }
        public string name { get; init; }
        public string status { get; init; }
        public ProjectTask[] tasks { get; init; }
    }

    record ProjectTask
    {
        public string task_id { get; init; }
        public string description { get; init; }
        public string status { get; init; }
        public ProjectSubtask[] subtasks { get; init; }
    }

    record ProjectSubtask
    {
        public string subtask_id { get; init; }
        public string name { get; init; }
        public string status { get; init; }
    }

    sealed class JsonDataReader : DbDataReader
    {
        private const int ChunkSize = 2033;

        private readonly StreamReader streamReader;
        private readonly char[] buffer;
        private int dataLength;

        public JsonDataReader(string sampleName)
        {
            this.streamReader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream($"Spryer.Tests.Samples.{sampleName}") ??
                throw new InvalidOperationException(), Encoding.Unicode);
            this.buffer = new char[ChunkSize];
        }

        public override bool Read()
        {
            this.dataLength = this.streamReader.Read(buffer, 0, buffer.Length);
            return this.dataLength > 0;
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));

            var copyLength = Math.Min(length, this.dataLength - (int)dataOffset);
            Array.Copy(this.buffer, (int)dataOffset, buffer, bufferOffset, copyLength);
            return copyLength;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.streamReader.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            this.streamReader.Dispose();
            await base.DisposeAsync();
        }

        public override bool NextResult() => false;

        #region Unused

        public override object this[int ordinal] => throw new NotImplementedException();
        public override object this[string name] => throw new NotImplementedException();

        public override int Depth { get; }
        public override int FieldCount => 1;
        public override bool HasRows => true;
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }

        public override bool GetBoolean(int ordinal) => throw new NotImplementedException();

        public override byte GetByte(int ordinal) => throw new NotImplementedException();

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();

        public override char GetChar(int ordinal) => throw new NotImplementedException();

        public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();

        public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();

        public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();

        public override double GetDouble(int ordinal) => throw new NotImplementedException();

        public override IEnumerator GetEnumerator() => throw new NotImplementedException();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal) => throw new NotImplementedException();

        public override float GetFloat(int ordinal) => throw new NotImplementedException();

        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();

        public override short GetInt16(int ordinal) => throw new NotImplementedException();

        public override int GetInt32(int ordinal) => throw new NotImplementedException();

        public override long GetInt64(int ordinal) => throw new NotImplementedException();

        public override string GetName(int ordinal) => throw new NotImplementedException();

        public override int GetOrdinal(string name) => throw new NotImplementedException();

        public override string GetString(int ordinal) => throw new NotImplementedException();

        public override object GetValue(int ordinal) => throw new NotImplementedException();

        public override int GetValues(object[] values) => throw new NotImplementedException();

        public override bool IsDBNull(int ordinal) => throw new NotImplementedException();
        #endregion Unused
    }
}
