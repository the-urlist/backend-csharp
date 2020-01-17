using System;
using LinkyLink.Infrastructure;
using LinkyLink.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace LinkyLink.Tests
{
    public class HasherTests : TestBase
    {
        private readonly ITestOutputHelper output;

        public HasherTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("securestring")]
        [InlineData("secretdata")]
        [InlineData("Data")]
        public void HashString_Hashes_Provided_String(string dataToProtect)
        {
            // Arrange
            Environment.SetEnvironmentVariable("HASHER_KEY", "somekey");
            Environment.SetEnvironmentVariable("HASHER_SALT", "somesalt");

            Hasher hasher = new Hasher();

            //Act
            var hashedData = hasher.HashString(dataToProtect);
            output.WriteLine($"{dataToProtect} hashed into {hashedData}");

            //Assert
            Assert.NotEqual(dataToProtect, hashedData);
        }

        [Fact]
        public void HashString_Throws_When_Parameter_Is_Empty()
        {
            // Arrange
            Environment.SetEnvironmentVariable("HASHER_KEY", "somekey");
            Environment.SetEnvironmentVariable("HASHER_SALT", "somesalt");

            Hasher hasher = new Hasher();

            //Act
            var exp = Assert.Throws<ArgumentException>(() => hasher.HashString(string.Empty));
            Assert.Equal("Data parameter was null or empty (Parameter 'data')", exp.Message);
        }

        [Theory]
        [InlineData("securestring")]
        [InlineData("secretdata")]
        [InlineData("Data")]
        public void Verify_Matches_Hashed_Data(string dataToProtect)
        {
            // Arrange
            Environment.SetEnvironmentVariable("HASHER_KEY", "somekey");
            Environment.SetEnvironmentVariable("HASHER_SALT", "somesalt");

            Hasher hasher = new Hasher();

            //Act
            var hashedData = hasher.HashString(dataToProtect);

            //Assert
            Assert.True(hasher.Verify(dataToProtect, hashedData));

        }
    }
}