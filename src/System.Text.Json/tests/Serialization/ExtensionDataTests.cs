﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ExtensionDataTests
    {
        [Fact]
        public static void ExtensionPropertyNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionPropertyRoundTrip()
        {
            ClassWithExtensionProperty obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = JsonSerializer.Serialize(obj);
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
                Verify();

                // The json should not contain the dictionary name.
                Assert.DoesNotContain(nameof(ClassWithExtensionProperty.MyOverflow), json);
            }

            void Verify()
            {
                Assert.NotNull(obj.MyOverflow);
                Assert.NotNull(obj.MyOverflow["MyIntMissing"]);
                Assert.Equal(1, obj.MyInt);
                Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

                JsonProperty[] properties = obj.MyOverflow["MyNestedClassMissing"].EnumerateObject().ToArray();

                // Verify a couple properties
                Assert.Equal(1, properties.Where(prop => prop.Name == "MyInt16").First().Value.GetInt32());
                Assert.True(properties.Where(prop => prop.Name == "MyBooleanTrue").First().Value.GetBoolean());
            }
        }

        [Fact]
        public static void ExtensionPropertyAlreadyInstantiated()
        {
            Assert.NotNull(new ClassWithExtensionPropertyAlreadyInstantiated().MyOverflow);

            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyAsObject()
        {
            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(2, ((JsonElement)obj.MyOverflow["MyIntMissing"]).GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyCamelCasing()
        {
            // Currently we apply no naming policy. If we do (such as a ExtensionPropertyNamingPolicy), we'd also have to add functionality to the JsonDocument.

            ClassWithExtensionProperty obj;
            const string jsonWithProperty = @"{""MyIntMissing"":1}";
            const string jsonWithPropertyCamelCased = @"{""myIntMissing"":1}";

            {
                // Baseline Pascal-cased json + no casing option.
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithProperty);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Pascal-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithProperty, options);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj, options);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + no casing option.
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithPropertyCamelCased);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""myIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithPropertyCamelCased, options);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj, options);
                Assert.Contains(@"""myIntMissing"":1", json);
            }
        }

        [Fact]
        public static void NullValuesIgnored()
        {
            const string json = @"{""MyNestedClass"":null}";
            const string jsonMissing = @"{""MyNestedClassMissing"":null}";

            {
                // Baseline with no missing.
                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
                Assert.Null(obj.MyOverflow);

                string outJson = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""MyNestedClass"":null", outJson);
            }

            {
                // Baseline with missing.
                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonMissing);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }

            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.IgnoreNullValues = true;

                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonMissing, options);

                // Currently we do not ignore nulls in the extension data. The JsonDocument would also need to support this mode
                // for any lower-level nulls.
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }
        }

        private class ClassWithInvalidExtensionProperty
        {
            [JsonExtensionData]
            public Dictionary<string, int> MyOverflow { get; set; }
        }

        private class ClassWithTwoExtensionProperties
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow1 { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow2 { get; set; }
        }

        [Fact]
        public static void InvalidExtensionPropertyFail()
        {
            // Baseline
            JsonSerializer.Deserialize<ClassWithExtensionProperty>(@"{}");
            JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(@"{}");

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithInvalidExtensionProperty>(@"{}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithTwoExtensionProperties>(@"{}"));
        }

        private class ClassWithIgnoredData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            [JsonIgnore]
            public int MyInt { get; set; }
        }

        [Fact]
        public static void IgnoredDataShouldNotBeExtensionData()
        {
            ClassWithIgnoredData obj = JsonSerializer.Deserialize<ClassWithIgnoredData>(@"{""MyInt"":1}");

            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_Empty()
        {
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            Assert.Equal(@"{}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_SameAsExtensionPropertyName()
        {
            const string json = @"{""MyOverflow"":{""Key1"":""V""}}";

            // Deserializing directly into the overflow is not supported by design.
            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);

            // The JSON is treated as normal overflow.
            Assert.NotNull(obj.MyOverflow["MyOverflow"]);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        private class ClassWithExtensionPropertyAsObjectAndNameProperty
        {
            public string Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        [Fact]
        public static void ExtensionPropertyDuplicateNames()
        {
            var obj = new ClassWithExtensionPropertyAsObjectAndNameProperty();
            obj.Name = "Name1";

            obj.MyOverflow = new Dictionary<string, object>();
            obj.MyOverflow["Name"] = "Name2";

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Name"":""Name1"",""Name"":""Name2""}", json);

            // The overflow value comes last in the JSOn so it overwrites the original value.
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObjectAndNameProperty>(json);
            Assert.Equal("Name2", obj.Name);

            // Since there was no overflow, this should be null.
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void NullAsNullObjectOrJsonValueKindNull()
        {
            const string json = @"{""MissingProperty"":null}";

            {
                ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);

                // A null value maps to <object>, so the value is null.
                object elem = obj.MyOverflow["MissingProperty"];
                Assert.Null(elem);
            }

            {
                ClassWithExtensionPropertyAsJsonElement obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);

                // Since JsonElement is a struct, it treats null as JsonValueKind.Null.
                object elem = obj.MyOverflow["MissingProperty"];
                Assert.IsType<JsonElement>(elem);
                Assert.Equal(JsonValueKind.Null, ((JsonElement)elem).ValueKind);
            }
        }

        [Fact]
        public static void ExtensionPropertyObjectValue()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);

            Assert.Equal(@"{""test"":{},""test1"":1}", JsonSerializer.Serialize(obj));
        }

        private class DummyObj
        {
            public string Prop { get; set; }
        }

        private struct DummyStruct
        {
            public string Prop { get; set; }
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_RoundTrip()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);
            obj.MyOverflow.Add("test2", "text");
            obj.MyOverflow.Add("test3", new DummyObj() { Prop = "ObjectProp" });
            obj.MyOverflow.Add("test4", new DummyStruct() { Prop = "StructProp" });
            obj.MyOverflow.Add("test5", new Dictionary<string, object>() { { "Key", "Value" }, { "Key1", "Value1" }, });

            string json = JsonSerializer.Serialize(obj);
            ClassWithExtensionPropertyAlreadyInstantiated roundTripObj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(json);

            Assert.Equal(6, roundTripObj.MyOverflow.Count);

            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test1"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test2"]);
            Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test3"]);

            Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test"]).ValueKind);

            Assert.Equal(JsonValueKind.Number, ((JsonElement)roundTripObj.MyOverflow["test1"]).ValueKind);
            Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt32());
            Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt64());

            Assert.Equal(JsonValueKind.String, ((JsonElement)roundTripObj.MyOverflow["test2"]).ValueKind);
            Assert.Equal("text", ((JsonElement)roundTripObj.MyOverflow["test2"]).GetString());

            Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test3"]).ValueKind);
            Assert.Equal("ObjectProp", ((JsonElement)roundTripObj.MyOverflow["test3"]).GetProperty("Prop").GetString());

            Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test4"]).ValueKind);
            Assert.Equal("StructProp", ((JsonElement)roundTripObj.MyOverflow["test4"]).GetProperty("Prop").GetString());

            Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test5"]).ValueKind);
            Assert.Equal("Value", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key").GetString());
            Assert.Equal("Value1", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key1").GetString());
        }

        private class ClassWithReference
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }

            public ClassWithExtensionProperty MyReference { get; set; }
        }

        [Fact]
        public static void ObjectTree()
        {
            string json = @"{""MyIntMissing"":2, ""MyReference"":{""MyIntMissingChild"":3}}";

            ClassWithReference obj = JsonSerializer.Deserialize<ClassWithReference>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

            ClassWithExtensionProperty child = obj.MyReference;
            Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
            Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
            Assert.Equal(1, child.MyOverflow.Count);
            Assert.Equal(3, child.MyOverflow["MyIntMissingChild"].GetInt32());
        }

        private class ClassWithInvalidExtensionPropertyStringString
        {
            [JsonExtensionData]
            public Dictionary<string, string> MyOverflow { get; set; }
        }

        private class ClassWithInvalidExtensionPropertyObjectString
        {
            [JsonExtensionData]
            public Dictionary<DummyObj, string> MyOverflow { get; set; }
        }

        [Fact]
        public static void ExtensionProperty_InvalidDictionary()
        {
            ClassWithInvalidExtensionPropertyStringString obj1 = new ClassWithInvalidExtensionPropertyStringString();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj1));

            ClassWithInvalidExtensionPropertyObjectString obj2 = new ClassWithInvalidExtensionPropertyObjectString();
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(obj2));
        }

        private class ClassWithExtensionPropertyAlreadyInstantiated
        {
            public ClassWithExtensionPropertyAlreadyInstantiated()
            {
                MyOverflow = new Dictionary<string, object>();
            }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyAsObject
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyAsJsonElement
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }
        }
    }
}
