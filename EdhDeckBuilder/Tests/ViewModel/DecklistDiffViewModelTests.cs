using EdhDeckBuilder.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests.ViewModel
{
    [TestFixture]
    public class DecklistDiffViewModelTests
    {
        [Test]
        public void Diff_WhenNumCopiesGoesDownButNotToZero_TreatsCardAsStillInDeck()
        {
            var deckModel = new DeckModel("Untitled");
            deckModel.AddCards(new List<CardModel> { new CardModel { Name = "Forest", NumCopies = 1 } });
            var diffDeckVm = new DecklistDiffViewModel("", deckModel);
            diffDeckVm.DiffDeck = "2 Forest";

            diffDeckVm.Diff();

            Assert.AreEqual($"1 Cut(s) (0 card(s) removed entirely from deck)", diffDeckVm.CutsHeader);
        }

        [Test]
        public void Diff_WhenNewCardsAdded_ShowsCountOfNewCards()
        {
            var deckModel = new DeckModel("Untitled");
            deckModel.AddCards(new List<CardModel>
                {
                    new CardModel { Name = "Forest", NumCopies = 2 },
                    new CardModel { Name = "Ghalta, Stampede Tyrant", NumCopies = 1 }
                });
            var diffDeckVm = new DecklistDiffViewModel("", deckModel);
            diffDeckVm.DiffDeck = "1 Forest";

            diffDeckVm.Diff();

            Assert.AreEqual($"2 Add(s) (1 card(s) entirely new to deck)", diffDeckVm.AddsHeader);
        }
    }
}
