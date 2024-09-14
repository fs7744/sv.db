using SV.Db;
using System.Data.Common;

namespace UT.GeneratorTestCases
{
    internal class RecordTestCase
    {
        public void TestCase()
        {
            DbConnection? connection = default;
            connection.CreateCommand().SetParams(new Person(default, default));
            connection.CreateCommand().SetParams(new Point(default, default, default));
            connection.CreateCommand().SetParams(new Point2());
        }

        public void Check(string generatedCode)
        {
            Assert.NotEmpty(generatedCode);
            Assert.Contains("public class Person_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Person>(new Person_", generatedCode);
            Assert.Contains("p.ParameterName = \"FirstName\";", generatedCode);
            Assert.Contains("p.ParameterName = \"LastName\";", generatedCode);
            Assert.Contains("public class Point_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Point>(new Point_", generatedCode);
            Assert.Contains("p.ParameterName = \"X\";", generatedCode);
            Assert.DoesNotContain("d.X = reader.IsDBNull(j) ? default : reader.GetDouble(j);", generatedCode);
            Assert.Contains("public class Point2_", generatedCode);
            Assert.Contains("RecordFactory.RegisterRecordFactory<global::UT.GeneratorTestCases.Point2>(new Point2_", generatedCode);
            Assert.Contains("p.ParameterName = \"X2\";", generatedCode);
            Assert.Contains("d.X2 = reader.IsDBNull(j) ? default : reader.GetDouble(j);", generatedCode);
        }
    }

    public record Person(string FirstName, string LastName);

    public readonly record struct Point(double X, double Y, double Z);

    public record struct Point2
    {
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Z2 { get; set; }
    }
}