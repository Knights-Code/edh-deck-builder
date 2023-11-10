using EdhDeckBuilder.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests.Model
{
    [TestFixture]
    public class TemplateModelTests
    {
        [Test]
        public void TemplateModelConstructor_WhenTemplateMinGreaterThanTemplateMax_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new TemplateModel("Draw", 10, 9);
            });
        }
    }
}
