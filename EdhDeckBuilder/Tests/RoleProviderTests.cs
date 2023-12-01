using EdhDeckBuilder.Model;
using EdhDeckBuilder.Service;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdhDeckBuilder.Tests
{
    [TestFixture]
    public class RoleProviderTests
    {
        private RoleProvider _roleProvider = new RoleProvider();
        private string _testRolesFilename = "test_roles.csv";

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(_testRolesFilename))
            {
                File.Delete(_testRolesFilename);
            }
        }

        [Test]
        public void GetRolesForCard_WhenNoRolesExistForCard_ReturnsNull()
        {
            var rolesForCard = _roleProvider.GetRolesForCard("Skullclamp");

            Assert.IsNull(rolesForCard);
        }

        [Test]
        public void GetRolesForCard_WhenRolesExistForCard_ReturnsListOfRoleModels()
        {
            using (var writer = new StreamWriter(new FileStream(_testRolesFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland");
                writer.WriteLine("Skullclamp,,1");
                writer.WriteLine("\"Ghalta, Primal Hunger\",,,,,,1");
            }

            _roleProvider.Initialise(_testRolesFilename);

            var rolesForGhalta = _roleProvider.GetRolesForCard("Ghalta, Primal Hunger");

            Assert.NotNull(rolesForGhalta);
            Assert.IsNotEmpty(rolesForGhalta);

            var role = rolesForGhalta.First();
            Assert.AreEqual("Standalone", role.Name);
        }

        [Test]
        public void SaveRoles_UpdatesRoleDataWithoutDeletingExistingData()
        {
            using (var writer = new StreamWriter(new FileStream(_testRolesFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland");
                writer.WriteLine("Skullclamp,,1");
                writer.WriteLine("\"Ghalta, Primal Hunger\",,,,,,1");
            }

            _roleProvider.Initialise(_testRolesFilename);

            var deckModel = new DeckModel("Test Deck");
            deckModel.AddCards(new List<CardModel>
            {
                new CardModel
                {
                    Name = "Forest",
                    NumCopies = 10,
                    Roles = new List<RoleModel> { new RoleModel("Land") }
                }
            });

            _roleProvider.SaveRoles(deckModel, _testRolesFilename);

            using (var reader = new StreamReader(_testRolesFilename))
            {
                var fileText = reader.ReadToEnd();
                Assert.AreEqual(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland\r\nSkullclamp,,1\r\n\"Ghalta, Primal Hunger\",,,,,,1\r\nForest,,,,,1\r\n", fileText);
            }
        }

        [Test]
        public void SaveRoles_WhenDeckModelIncludesCustomRoles_CreatesColumnsForNewRoles()
        {
            using (var writer = new StreamWriter(new FileStream(_testRolesFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland");
                writer.WriteLine("Skullclamp,,1");
                writer.WriteLine("\"Ghalta, Primal Hunger\",,,,,,1");
            }

            _roleProvider.Initialise(_testRolesFilename);

            var deckModel = new DeckModel("Test Deck");
            deckModel.CustomRoles.Add("Unplayables");
            deckModel.AddCards(new List<CardModel>
            {
                new CardModel
                {
                    Name = "Forest",
                    NumCopies = 10,
                    Roles = new List<RoleModel> { new RoleModel("Land") }
                },
                new CardModel
                {
                    Name = "Salt Marsh",
                    NumCopies = 1,
                    Roles = new List<RoleModel> {new RoleModel("Land"), new RoleModel("Tapland"), new RoleModel("Unplayables") }
                }
            });

            _roleProvider.SaveRoles(deckModel, _testRolesFilename);

            using (var reader = new StreamReader(_testRolesFilename))
            {
                var fileText = reader.ReadToEnd();
                Assert.AreEqual(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland,Unplayables\r\nSkullclamp,,1\r\n\"Ghalta, Primal Hunger\",,,,,,1\r\nForest,,,,,1\r\nSalt Marsh,,,,,1,,,,1,1\r\n", fileText);
            }
        }

        [Test]
        public void GetRolesForCard_WhenRolesExistForCardAndIncludeCustomRoles_ReturnsListOfRoleModels()
        {
            using (var writer = new StreamWriter(new FileStream(_testRolesFilename, FileMode.OpenOrCreate)))
            {
                writer.WriteLine(",Ramp,Draw,Removal,Wipe,Land,Standalone,Enhancer,Enabler,Tapland,Unplayables");
                writer.WriteLine("Skullclamp,,1");
                writer.WriteLine("\"Ghalta, Primal Hunger\",,,,,,1");
                writer.WriteLine("Salt Marsh,,,,,1,,,,1,1");
            }

            _roleProvider.Initialise(_testRolesFilename);

            var rolesForSaltMarsh = _roleProvider.GetRolesForCard("Salt Marsh");

            Assert.NotNull(rolesForSaltMarsh);
            Assert.IsNotEmpty(rolesForSaltMarsh);

            var customRole = rolesForSaltMarsh.FirstOrDefault(r => r.Name == "Unplayables");
            Assert.NotNull(customRole);
        }
    }
}
