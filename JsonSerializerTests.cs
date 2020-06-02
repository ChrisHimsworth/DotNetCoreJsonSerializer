using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using CoreJsonSerializer = System.Text.Json.JsonSerializer;
//using NewtonJsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace UTF8BOMJsonSerializerTests
{
	public class JsonSerializerTests : IDisposable
	{
		private readonly TestObject testObject;

		private readonly MemoryStream stream;

		public JsonSerializerTests()
		{
			testObject = new TestObject { CorrelationId = "Test123" };

			stream = new MemoryStream();

			CoreSerializeWithBOM(testObject, stream);
		}

		[Fact]
		public void DeserializeFromStringWithBOM()
		{
			var messageAsString = ConvertToString(stream);

			var returnTestObject = CoreJsonSerializer.Deserialize<TestObject>(messageAsString);

			Assert.Equal(testObject, returnTestObject);
		}

		[Fact]
		public void DeserializeFromStringWithoutBOM()
		{
			var messageAsString = ConvertToString(stream, true);

			var returnTestObject = CoreJsonSerializer.Deserialize<TestObject>(messageAsString);

			Assert.Equal(testObject, returnTestObject);
		}

		[Fact]
		public void DeserializeFromBytesWithBOM()
		{
			var returnTestObject = CoreJsonSerializer.Deserialize<TestObject>(stream.ToArray());

			Assert.Equal(testObject, returnTestObject);
		}

		[Fact]
		public void DeserializeFromBytesWithoutBOM()
		{
			var bytes = RemoveByteOrderMarkIfFound(stream.ToArray());

			var returnTestObject = CoreJsonSerializer.Deserialize<TestObject>(bytes);

			Assert.Equal(testObject, returnTestObject);
		}

		[Fact]
		public async void DeserializeFromStreamWithBOM()
		{
			var returnTestObject = await CoreJsonSerializer.DeserializeAsync<TestObject>(stream);

			Assert.Equal(testObject, returnTestObject);
		}

		[Fact]
		public async void DeserializeFromStreamWithBOMThenString()
		{
			var messageAsString = ConvertToString(stream);
			var messageBodyInBytes = Encoding.UTF8.GetBytes(messageAsString);
			await using var convertedStream = new MemoryStream(messageBodyInBytes);

			var returnTestObject = await CoreJsonSerializer.DeserializeAsync<TestObject>(convertedStream);

			Assert.Equal(testObject, returnTestObject);
		}

		private static string ConvertToString(MemoryStream stream, bool removeBOM = false)
		{
			var bytes = stream.ToArray();

			var enc = new UTF8Encoding(true);

			if (!removeBOM)
			{
				return enc.GetString(bytes);
			}

			return enc.GetString(RemoveByteOrderMarkIfFound(bytes));
		}

		private static byte[] RemoveByteOrderMarkIfFound(byte[] bytes)
		{
			var enc = new UTF8Encoding(true);
			var preamble = enc.GetPreamble();
			if (preamble.Where((p, i) => p != bytes[i]).Any())
			{
				return bytes;
			}

			return bytes.Skip(preamble.Length).ToArray();
		}

		private static void CoreSerializeWithBOM(object message, Stream stream)
		{
			var inputMessage = CoreJsonSerializer.Serialize(message);
			var encoding = new UTF8Encoding(true);
			stream.Write(encoding.GetPreamble());

			var bytes = encoding.GetBytes(inputMessage);
			stream.Write(bytes);
			stream.Position = 0;
		}

		/*
		 //Original method of creating stream extracted from live system

		 private static void NewtonSerializeWithBOM(object message, Stream stream)
		{
			var jsonWriter = CreateJsonWriter(stream);
			var serializer = NewtonJsonSerializer.Create();
			serializer.Serialize(jsonWriter, message);
			jsonWriter.Flush();
			stream.Position = 0;
		}

		private static JsonWriter CreateJsonWriter(Stream stream)
		{
			var streamWriter = new StreamWriter(stream, Encoding.UTF8);
			return new JsonTextWriter(streamWriter);
		}
		*/

		public void Dispose()
		{
			stream?.Dispose();
		}
	}

	public class TestObject : IEquatable<TestObject>
	{
		public string CorrelationId { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as TestObject);
		}

		public override int GetHashCode() => CorrelationId != null ? CorrelationId.GetHashCode() : 0;

		public static bool operator ==(TestObject left, TestObject right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(TestObject left, TestObject right)
		{
			return !Equals(left, right);
		}

		public bool Equals(TestObject other)
		{
			return other != null &&
				   CorrelationId == other.CorrelationId;
		}
	}
}
