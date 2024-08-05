using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service.Clipboard;
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
    public class DeckBuilderViewModelTests
    {
        [Test]
        public void DeckBuilderVmConstructor_SetsUpDefaultTemplatesAndRolesCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();

            var rampRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Ramp");
            var drawRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Draw");
            var removalRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Removal");
            var wipeRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Wipe");
            var landRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Land");
            var standaloneRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Standalone");
            var enhancerRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Enhancer");
            var enablerRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Enabler");
            var taplandRoleTemplate = deckBuilderVm.TemplateVms.FirstOrDefault(template => template.Role == "Tapland");

            AssertRoleTemplateValues(rampRoleTemplate, "Ramp", 10, 12);
            AssertRoleTemplateValues(drawRoleTemplate, "Draw", 10, 100);
            AssertRoleTemplateValues(removalRoleTemplate, "Removal", 10, 12);
            AssertRoleTemplateValues(wipeRoleTemplate, "Wipe", 2, 2);
            AssertRoleTemplateValues(landRoleTemplate, "Land", 35, 38);
            AssertRoleTemplateValues(standaloneRoleTemplate, "Standalone", 25, 100);
            AssertRoleTemplateValues(enhancerRoleTemplate, "Enhancer", 10, 12);
            AssertRoleTemplateValues(enablerRoleTemplate, "Enabler", 7, 8);
            AssertRoleTemplateValues(taplandRoleTemplate, "Tapland", 0, 4);
        }

        private void AssertRoleTemplateValues(TemplateViewModel actual, string expectedRoleName, int expectedMinimum, int expectedMaximum)
        {
            Assert.NotNull(actual);
            Assert.AreEqual(expectedRoleName, actual.Role);
            Assert.AreEqual(expectedMinimum, actual.Minimum);
            Assert.AreEqual(expectedMaximum, actual.Maximum);
        }

        [Test]
        public void CalculateTotal_WhenOneCardWithOneCopy_CalculatesTotalCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            deckBuilderVm.CardVms.Add(new CardViewModel(new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 1,
            }));

            var totalCards = deckBuilderVm.TotalCards;

            Assert.AreEqual(1, totalCards);
        }

        [Test]
        public void CalculateTotal_WhenOneCardWithTwoCopies_CalculatesTotalCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            deckBuilderVm.CardVms.Add(new CardViewModel(new EdhDeckBuilder.Model.CardModel
            {
                Name = "Skullclamp",
                NumCopies = 2,
            }));

            var totalCards = deckBuilderVm.TotalCards;

            Assert.AreEqual(2, totalCards);
        }

        [Test]
        public void CalculateTotal_WhenTwoCardsWithOneCopyEach_CalculatesTotalCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            deckBuilderVm.CardVms.Add(new CardViewModel(new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 1,
            }));
            deckBuilderVm.CardVms.Add(new CardViewModel(new CardModel
            {
                Name = "Plains",
                NumCopies = 1,
            }));

            var totalCards = deckBuilderVm.TotalCards;

            Assert.AreEqual(2, totalCards);
        }

        [Test]
        public void CalculateTotal_WhenTwoCardsWithTwoCopiesEach_CalculatesTotalCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            deckBuilderVm.CardVms.Add(new CardViewModel(new CardModel
            {
                Name = "Skullclamp",
                NumCopies = 2,
            }));
            deckBuilderVm.CardVms.Add(new CardViewModel(new CardModel
            {
                Name = "Plains",
                NumCopies = 2,
            }));

            var totalCards = deckBuilderVm.TotalCards;

            Assert.AreEqual(4, totalCards);
        }

        [Test]
        public async void AddCard_WhenNoCards_AddsCard()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            await deckBuilderVm.AddCardAsync("Skullclamp");

            Assert.AreEqual(1, deckBuilderVm.CardVms.Count);
        }

        [Test]
        public async void AddCard_WhenCardAlreadyAdded_DoesNotAddCard()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            await deckBuilderVm.AddCardAsync("Skullclamp");
            await deckBuilderVm.AddCardAsync("Skullclamp");

            Assert.AreEqual(1, deckBuilderVm.CardVms.Count);
        }

        [Test]
        public async void DeckBuilderVm_WhenCardRoleUpdated_UpdatesTemplatesCurrentsCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            await deckBuilderVm.AddCardAsync("Skullclamp");
            deckBuilderVm.CardVms.First().RoleVms.First(vm => vm.Name == "Draw").Applies = true;

            var drawRoleHeader = deckBuilderVm.TemplateVms.First(vm => vm.Role == "Draw");

            Assert.AreEqual(1, drawRoleHeader.Current);
        }

        [Test]
        public async void DeckBuilderVm_WhenCardCopiesIncreasedWhileRoleApplies_UpdatesTemplatesCurrentsCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            await deckBuilderVm.AddCardAsync("Skullclamp");

            var cardVm = deckBuilderVm.CardVms.First();
            cardVm.RoleVms.First(vm => vm.Name == "Draw").Applies = true;
            cardVm.NumCopies++;

            var drawRoleHeader = deckBuilderVm.TemplateVms.First(vm => vm.Role == "Draw");

            Assert.AreEqual(2, drawRoleHeader.Current);
        }

        [Test]
        public async void DeckBuilderVm_WhenCardCopiesReducedWhileRoleApplies_UpdatesTemplatesCurrentsCorrectly()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            await deckBuilderVm.AddCardAsync("Skullclamp");

            var cardVm = deckBuilderVm.CardVms.First();
            cardVm.NumCopies++;
            cardVm.RoleVms.First(vm => vm.Name == "Draw").Applies = true;
            cardVm.NumCopies--;

            var drawRoleHeader = deckBuilderVm.TemplateVms.First(vm => vm.Role == "Draw");

            Assert.AreEqual(1, drawRoleHeader.Current);
        }

        [Test]
        public async void AddCard__WhenCustomRoleHeadersExist_CreatesRoleVmsForCustomRoles()
        {
            var deckBuilderVm = new DeckBuilderViewModel();
            deckBuilderVm.AddCustomRole();

            await deckBuilderVm.AddCardAsync("Skullclamp");

            var skullclamp = deckBuilderVm.CardVms.First();
            Assert.AreEqual(deckBuilderVm.TemplateVms.Count, skullclamp.RoleVms.Count);
        }

        [Test]
        public async void ImportFromClipboard_WhenCustomRoleHeadersExist_CreatesRoleVmsForCustomRoles()
        {
            var fakeClipboard = new FakeClipboard();
            var deckBuilderVm = new DeckBuilderViewModel(fakeClipboard);
            fakeClipboard.SetClipboardText("1 Bronzebeak Foragers");
            deckBuilderVm.AddCustomRole();

            await deckBuilderVm.ImportFromClipboardAsync();

            var foragers = deckBuilderVm.CardVms.First();
            Assert.AreEqual(deckBuilderVm.TemplateVms.Count, foragers.RoleVms.Count);
        }
    }
}
