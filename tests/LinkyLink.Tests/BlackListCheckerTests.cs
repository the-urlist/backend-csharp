using System;
using System.Threading.Tasks;
using LinkyLink.Infrastructure;
using LinkyLink.Tests.Helpers;
using Xunit;

namespace LinkyLink.Tests
{
    public class EnvironmentBlackListCheckerTests : TestBase
    {
        
        [Fact]
        public async Task Check_Returns_False_On_Empty_Key() {
            EnvironmentBlackListChecker checker =  new EnvironmentBlackListChecker(string.Empty);
            Assert.True(await checker.Check("somevalue"));
        }

        [Theory]
        [InlineData("value1")]
        [InlineData("key")]
        public async Task Check_Always_Returns_True_For_Empty_Setting(string value)
        {
            // Arrange        
            Environment.SetEnvironmentVariable("key", "value");
            EnvironmentBlackListChecker checker = new EnvironmentBlackListChecker();

            // Act
            bool result = await checker.Check(value);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("value1")]
        [InlineData("key")]
        public async Task Check_Returns_True_For_Missing_Environment_Variable(string value)
        {
            // Arrange            
            EnvironmentBlackListChecker checker = new EnvironmentBlackListChecker();

            // Act
            bool result = await checker.Check(value);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Check_Throws_Exception_On_Empty_BlackList_Value()
        {
            // Arrange
            string key = "key";
            Environment.SetEnvironmentVariable(key, "value");            
            EnvironmentBlackListChecker checker = new EnvironmentBlackListChecker(key);

            // Act
             await Assert.ThrowsAsync<ArgumentNullException>(() => checker.Check(string.Empty));
        }

        [Fact]
        public async Task Check_Compares_Input_To_Blacklist()
        {
            // Arrange
            string key = "key";
            Environment.SetEnvironmentVariable(key, "1,2,3,4,5,6");            
            EnvironmentBlackListChecker checker = new EnvironmentBlackListChecker(key);

            // Act
            bool result_1 = await checker.Check("1");
            bool result_2 = await checker.Check("10");

            // Assert
            Assert.True(result_1);
            Assert.False(result_2);
        }
    }
}