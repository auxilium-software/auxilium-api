
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using AuxiliumSoftware.AuxiliumServices.Common.Enumerators;
using AuxiliumSoftware.AuxiliumServices.Common.Utilities;

namespace AuxiliumAPI.Tests
{
    public class UUIDUtilitiesTests
    {
        #region GenerateV5 Tests
        [Theory]
        [InlineData(DatabaseObjectType.Case)]
        [InlineData(DatabaseObjectType.CaseTimelineItem)]
        [InlineData(DatabaseObjectType.CaseTodoItem)]
        [InlineData(DatabaseObjectType.File)]
        [InlineData(DatabaseObjectType.Message)]
        [InlineData(DatabaseObjectType.User)]
        public void GenerateV5_WithCaseObjectType_ReturnsValidGuid(DatabaseObjectType objectType)
        {
            Guid uuid = UUIDUtilities.GenerateV5(objectType);

            uuid.Should().NotBeEmpty();
            uuid.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void GenerateV5_MultipleCallsSameType_ReturnsDifferentGuids()
        {
            Guid uuid1 = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            Guid uuid2 = UUIDUtilities.GenerateV5(DatabaseObjectType.User);

            uuid1.Should().NotBe(uuid2);
        }

        [Fact]
        public void GenerateV5_DifferentObjectTypes_ReturnsDifferentGuids()
        {
            Guid userUuid = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            Guid caseUuid = UUIDUtilities.GenerateV5(DatabaseObjectType.Case);

            userUuid.Should().NotBe(caseUuid);
        }
        #endregion

        #region Version 5 Validation Tests
        [Fact]
        public void GenerateV5_ReturnsVersion5Uuid()
        {
            Guid uuid = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            string uuidString = uuid.ToString();

            char versionChar = uuidString[14];
            versionChar.Should().Be('5');
        }

        [Fact]
        public void GenerateV5_ReturnsRfc4122Variant()
        {
            Guid uuid = UUIDUtilities.GenerateV5(DatabaseObjectType.User);
            byte[] bytes = uuid.ToByteArray();

            // rfc4122 variant has bits 10 in high 2 bits of byte 8
            var variantByte = bytes[8];
            (variantByte & 0xC0).Should().Be(0x80);
        }
        #endregion

        #region Edge Cases
        [Fact]
        public void GenerateV5_WithEmptyString_ShouldWork()
        {
            var namespaceGuid = Guid.NewGuid();

            var uuid = UUIDUtilities.GenerateV5(namespaceGuid, "");

            uuid.Should().NotBeEmpty();
        }

        [Fact]
        public void GenerateV5_WithUnicodeString_ShouldWork()
        {
            var namespaceGuid = Guid.NewGuid();
            var unicodeName = "測試";

            var uuid = UUIDUtilities.GenerateV5(namespaceGuid, unicodeName);

            uuid.Should().NotBeEmpty();
        }

        [Fact]
        public void GenerateV5_WithVeryLongString_ShouldWork()
        {
            var namespaceGuid = Guid.NewGuid();
            var longName = new string('A', 10000);

            var uuid = UUIDUtilities.GenerateV5(namespaceGuid, longName);

            uuid.Should().NotBeEmpty();
        }
        #endregion

        #region Format Tests
        [Fact]
        public void GenerateV5String_ReturnsCorrectFormat()
        {
            var uuidString = UUIDUtilities.GenerateV5(DatabaseObjectType.Case).ToString();

            uuidString.Should().HaveLength(36);
            uuidString.Should().Contain("-");
            uuidString.Split('-').Should().HaveCount(5);
            uuidString.Split('-')[0].Should().HaveLength(8);
            uuidString.Split('-')[1].Should().HaveLength(4);
            uuidString.Split('-')[2].Should().HaveLength(4);
            uuidString.Split('-')[3].Should().HaveLength(4);
            uuidString.Split('-')[4].Should().HaveLength(12);
        }
        #endregion
    }
}
