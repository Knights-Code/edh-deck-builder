using EdhDeckBuilder.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests
{
    [TestFixture]
    public class RoleModelTests
    {
        [Test]
        public void RoleModelConstructor_WhenValueMissing_DefaultsToOne()
        {
            var roleModel = new RoleModel("Draw");
            Assert.AreEqual(1, roleModel.Value);
        }

        [Test]
        public void RoleModelConstructor_WhenApplyMissing_DefaultsToTrue()
        {
            var roleModel = new RoleModel("Draw", 1);
            Assert.True(roleModel.Applies);
        }

        [Test]
        public void RoleModelConstructor_WhenValueIsEqualToOrLessThanZero_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new RoleModel("Draw", 0);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new RoleModel("Draw", -1);
            });

            Assert.DoesNotThrow(() =>
            {
                new RoleModel("Draw", 1);
            });
        }
    }
}
