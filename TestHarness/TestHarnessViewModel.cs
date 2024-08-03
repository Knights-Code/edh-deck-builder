using EdhDeckBuilder.Service;
using EdhDeckBuilder.ViewModel;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace TestHarness
{
    public class TestHarnessViewModel : ViewModelBase
    {
        private DeckBuilderViewModel _deckBuilderVm;
        public DeckBuilderViewModel DeckBuilderVm
        {
            get { return _deckBuilderVm; }
            set { SetProperty(ref _deckBuilderVm, value); }
        }


        private ObservableCollection<CardViewModel> _cards = new ObservableCollection<CardViewModel>();
        public ObservableCollection<CardViewModel> Cards
        {
            get { return _cards; }
            set { SetProperty(ref _cards, value); }
        }

        public async void TryPreview()
        {
            TextBoxHovered = true;
            await DeckBuilderVm.TryPreviewAsync(NewCardName);
        }

        public void ClearPreview()
        {
            TextBoxHovered = false;
            DeckBuilderVm.ClearPreview();
        }

        private CardViewModel _selectedCard;
        public CardViewModel SelectedCard
        {
            get { return _selectedCard; }
            set { SetProperty(ref _selectedCard, value); }
        }

        private string _newCardName;
        public string NewCardName
        {
            get { return _newCardName; }
            set { SetProperty(ref _newCardName, value); }
        }

        private bool _textBoxHovered;
        public bool TextBoxHovered
        {
            get { return _textBoxHovered; }
            set
            {
                SetProperty(ref _textBoxHovered, value);
                RaisePropertyChanged(nameof(TextBoxBackground));
            }
        }

        public string WindowTitle
        {
            get
            {
                var fileName = Path.GetFileName(SettingsProvider.DeckFilePath());
                return string.IsNullOrEmpty(fileName) ? "Test Harness" : $"Test Harness - {fileName}";
            }
        }

        public SolidColorBrush TextBoxBackground
        {
            get
            {
                return TextBoxHovered ? new SolidColorBrush(Colors.LightGray) : new SolidColorBrush(Colors.White);
            }
        }

        #region Menu Commands
        public ICommand NewDeckCommand { get; set; }
        public ICommand SaveDeckCommand { get; set; }
        public ICommand SaveDeckAsCommand { get; set; }
        public ICommand OpenDeckCommand { get; set; }
        public ICommand ImportFromClipboardCommand { get; set; }
        public ICommand ExportToClipboardCommand { get; set; }
        public ICommand CustomRoleCommand { get; set; }
        #endregion

        public ICommand NewCardEnterCommand { get; set; }

        public ICommand SortCardsCommand { get; set; }
        public ICommand SortByRoleCommand { get; set; }

        public ICommand CleanUpCommand { get; set; }

        public ICommand ManageTagsCommand { get; set; }

        public ICommand DecklistDiffCommand { get; set; }

        public ICommand DecklistDiffFromFileCommand { get; set; }

        public ICommand RoleRankingsCommand { get; set; }

        public TestHarnessViewModel()
        {
            DeckBuilderVm = new DeckBuilderViewModel();
            NewCardEnterCommand = new DelegateCommand(AddNewCard, () => !string.IsNullOrEmpty(NewCardName));

            // Menu Commands //
            NewDeckCommand = new DelegateCommand(NewDeck);
            SaveDeckCommand = new DelegateCommand(SaveDeck);
            SaveDeckAsCommand = new DelegateCommand(SaveDeckAs);
            OpenDeckCommand = new DelegateCommand(OpenDeck);
            ImportFromClipboardCommand = new DelegateCommand(ImportFromClipboard);
            ExportToClipboardCommand = new DelegateCommand(ExportToClipboard);
            CustomRoleCommand = new DelegateCommand(AddCustomRole);
            SortCardsCommand = new DelegateCommand(SortCards, () => DeckBuilderVm.CardVms.Any());
            SortByRoleCommand = new DelegateCommand(SortCardsByRoleRanking);
            CleanUpCommand = new DelegateCommand(CleanUp, () => DeckBuilderVm.CardVms.Any());
            ManageTagsCommand = new DelegateCommand(ManageTags, () => DeckBuilderVm.CardVms.Any());
            DecklistDiffCommand = new DelegateCommand(DecklistDiff, () => DeckBuilderVm.CardVms.Any());
            DecklistDiffFromFileCommand = new DelegateCommand(DecklistDiffFromFile);
            RoleRankingsCommand = new DelegateCommand(RoleRankings);

            if (!string.IsNullOrEmpty(SettingsProvider.DeckFilePath()))
            {
                LoadDeck();
            }
        }

        public async void LoadDeck()
        {
            await DeckBuilderVm.LoadDeck(SettingsProvider.DeckFilePath());
        }

        public void NewDeck()
        {
            DeckBuilderVm.NewDeck();
            RaisePropertyChanged(nameof(WindowTitle));
        }

        public void SaveDeck()
        {
            DeckBuilderVm.SaveDeck();
            RaisePropertyChanged(nameof(WindowTitle));
        }

        public async void OpenDeck()
        {
            await DeckBuilderVm.OpenDeck();
            RaisePropertyChanged(nameof(WindowTitle));
        }

        public async void AddNewCard()
        {
            var addSuccessful = await DeckBuilderVm.AddCardAsync(NewCardName);
            if (addSuccessful) NewCardName = string.Empty;
        }

        public void SaveDeckAs()
        {
            DeckBuilderVm.SaveDeckAs();
            RaisePropertyChanged(nameof(WindowTitle));
        }

        public async void ImportFromClipboard()
        {
            await DeckBuilderVm.ImportFromClipboardAsync();
        }

        public void ExportToClipboard()
        {
            DeckBuilderVm.ExportToClipboard();
        }

        public void AddCustomRole()
        {
            DeckBuilderVm.AddCustomRole();
        }

        public void SortCards()
        {
            DeckBuilderVm.SortCards();
        }

        public void SortCardsByRoleRanking()
        {
            DeckBuilderVm.SortByRole();
        }

        public void CleanUp()
        {
            DeckBuilderVm.CleanUp();
        }

        public void ManageTags()
        {
            DeckBuilderVm.ManageTags();
        }

        public void DecklistDiff()
        {
            DeckBuilderVm.DecklistDiff();
        }

        public void DecklistDiffFromFile()
        {
            DeckBuilderVm.DecklistDiffFromFile();
        }

        public void RoleRankings()
        {
            DeckBuilderVm.RoleRankings();
        }
    }
}
