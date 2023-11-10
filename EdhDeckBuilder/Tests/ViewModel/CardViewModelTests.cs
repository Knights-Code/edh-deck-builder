using EdhDeckBuilder.Model;
using EdhDeckBuilder.ViewModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests.ViewModel
{
    [TestFixture]
    public class CardViewModelTests
    {
        [Test]
        public void CardViewModelConstructor_WhenModelHasNumCopiesOfZero_SetsNumCopiesToOne()
        {
            var cardVm = new CardViewModel(new CardModel { Name = "Skullclamp" });

            Assert.AreEqual(1, cardVm.NumCopies);
        }

        [Test]
        public void CardViewModelConstructor_WhenModelHasRoles_SetsRoleAccordingly()
        {
            var drawRole = new RoleModel("Draw");
            var cardModel = new CardModel { Name = "Skullclamp" };
            cardModel.Roles.Add(drawRole);
            var cardVm = new CardViewModel(cardModel);

            AssertHasRoleVm(cardVm, "Draw", true);
        }

        [Test]
        public void CardViewModelConstructor_CreatesDefaultRoleViewModels()
        {
            var cardVm = new CardViewModel(new CardModel { Name = "Skullclamp" });

            AssertHasRoleVm(cardVm, "Ramp", false);
            AssertHasRoleVm(cardVm, "Draw", false);
            AssertHasRoleVm(cardVm, "Removal", false);
            AssertHasRoleVm(cardVm, "Wipe", false);
            AssertHasRoleVm(cardVm, "Land", false);
            AssertHasRoleVm(cardVm, "Standalone", false);
            AssertHasRoleVm(cardVm, "Enhancer", false);
            AssertHasRoleVm(cardVm, "Enabler", false);
            AssertHasRoleVm(cardVm, "Tapland", false);
        }

        [Test]
        public void CardViewModelConstructor_WhenModelHasRole_UpdatesRole()
        {
            var drawRole = new RoleModel("Draw");
            var cardModel = new CardModel { Name = "Skullclamp" };
            cardModel.Roles.Add(drawRole);
            var cardVm = new CardViewModel(cardModel);

            AssertHasRoleVm(cardVm, "Ramp", false);
            AssertHasRoleVm(cardVm, "Draw", true);
            AssertHasRoleVm(cardVm, "Removal", false);
            AssertHasRoleVm(cardVm, "Wipe", false);
            AssertHasRoleVm(cardVm, "Land", false);
            AssertHasRoleVm(cardVm, "Standalone", false);
            AssertHasRoleVm(cardVm, "Enhancer", false);
            AssertHasRoleVm(cardVm, "Enabler", false);
            AssertHasRoleVm(cardVm, "Tapland", false);
        }

        private void AssertHasRoleVm(CardViewModel cardVm, string expectedRoleName, bool expectedApplies)
        {
            var roleVm = cardVm.RoleVms.FirstOrDefault(vm => vm.Name == expectedRoleName);
            Assert.NotNull(roleVm);
            Assert.AreEqual(expectedApplies, roleVm.Applies);
        }
    }
}
