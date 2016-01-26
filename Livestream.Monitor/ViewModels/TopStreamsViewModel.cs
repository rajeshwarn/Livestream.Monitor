﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using ExternalAPIs.TwitchTv.Query;
using Livestream.Monitor.Core;
using Livestream.Monitor.Core.UI;
using Livestream.Monitor.Model;
using Livestream.Monitor.Model.ApiClients;
using Livestream.Monitor.Model.Monitoring;

namespace Livestream.Monitor.ViewModels
{
    public class TopStreamsViewModel : PagingConductor<TopStreamResult>
    {
        private const int STREAM_TILES_PER_PAGE = 15;

        private readonly IMonitorStreamsModel monitorStreamsModel;
        private readonly ISettingsHandler settingsHandler;
        private readonly StreamLauncher streamLauncher;
        private readonly INavigationService navigationService;
        private readonly IApiClientFactory apiClientFactory;
        
        private bool loadingItems;
        private string gameName;
        private BindableCollection<string> possibleGameNames = new BindableCollection<string>();
        private bool expandPossibleGames;
        private IApiClient selectedApiClient;

        #region Design time constructor

        public TopStreamsViewModel()
        {
            if (!Execute.InDesignMode)
                throw new InvalidOperationException("Constructor only accessible from design time");

            var designTimeItems = new List<TopStreamResult>(new[]
            {
                new TopStreamResult(new LivestreamModel
                    {
                        DisplayName = "Bob Ross",
                        Game = "Creative",
                        Description = "Beat the devil out of it",
                        Live = true,
                        StartTime = DateTimeOffset.Now.AddHours(-3),
                        Viewers = 50000
                    })
                {
                    IsMonitored = false,
                }
            });

            for (var i = 0; i < 9; i++)
            {
                var stream = new TopStreamResult(new LivestreamModel
                {
                    Description = "Design time item " + i,
                    DisplayName = "Display Name " + i,
                    Game = "Random Game " + i,
                    Live = true,
                    StartTime = DateTimeOffset.Now.AddMinutes(-29 - i),
                    Viewers = 30000 - i * 200
                });
                stream.IsMonitored = i % 3 == 0;
                designTimeItems.Add(stream);
            }

            Items.AddRange(designTimeItems);
            ItemsPerPage = STREAM_TILES_PER_PAGE;
        }

        #endregion

        public TopStreamsViewModel(
            IMonitorStreamsModel monitorStreamsModel,
            ISettingsHandler settingsHandler,
            StreamLauncher streamLauncher,
            INavigationService navigationService,
            IApiClientFactory apiClientFactory)
        {
            if (monitorStreamsModel == null) throw new ArgumentNullException(nameof(monitorStreamsModel));
            if (settingsHandler == null) throw new ArgumentNullException(nameof(settingsHandler));
            if (streamLauncher == null) throw new ArgumentNullException(nameof(streamLauncher));
            if (navigationService == null) throw new ArgumentNullException(nameof(navigationService));
            if (apiClientFactory == null) throw new ArgumentNullException(nameof(apiClientFactory));
            
            this.monitorStreamsModel = monitorStreamsModel;
            this.settingsHandler = settingsHandler;
            this.streamLauncher = streamLauncher;
            this.navigationService = navigationService;
            this.apiClientFactory = apiClientFactory;

            ItemsPerPage = STREAM_TILES_PER_PAGE;
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(Page))
                {
                    NotifyOfPropertyChange(() => CanPrevious);
                }
            };
        }

        public override bool CanPrevious => Page > 1 && !LoadingItems;

        public override bool CanNext => !LoadingItems && Items.Count == STREAM_TILES_PER_PAGE;

        public bool LoadingItems
        {
            get { return loadingItems; }
            set
            {
                if (value == loadingItems) return;
                loadingItems = value;
                NotifyOfPropertyChange(() => LoadingItems);
                NotifyOfPropertyChange(() => CanPrevious);
                NotifyOfPropertyChange(() => CanNext);
            }
        }

        public string GameName
        {
            get { return gameName; }
            set
            {
                if (value == gameName) return;
                gameName = value;
                NotifyOfPropertyChange(() => GameName);
                if (!PossibleGameNames.Any(x => x.IsEqualTo(gameName))) UpdatePossibleGameNames();
                MovePage();
            }
        }

        public BindableCollection<string> PossibleGameNames
        {
            get { return possibleGameNames; }
            set
            {
                if (Equals(value, possibleGameNames)) return;
                possibleGameNames = value;
                NotifyOfPropertyChange(() => PossibleGameNames);
            }
        }

        public bool ExpandPossibleGames
        {
            get { return expandPossibleGames; }
            set
            {
                if (value == expandPossibleGames) return;
                expandPossibleGames = value;
                NotifyOfPropertyChange(() => ExpandPossibleGames);
            }
        }

        public BindableCollection<IApiClient> ApiClients { get; set; }

        public IApiClient SelectedApiClient
        {
            get { return selectedApiClient; }
            set
            {
                if (Equals(value, selectedApiClient)) return;
                selectedApiClient = value;
                NotifyOfPropertyChange(() => SelectedApiClient);
                MovePage();
            }
        }

        public void OpenStream(TopStreamResult stream)
        {
            if (stream == null) return;

            streamLauncher.OpenStream(stream.LivestreamModel);
        }

        public async Task OpenChat(TopStreamResult stream)
        {
            if (stream == null) return;

            await streamLauncher.OpenChat(stream.LivestreamModel, this);
        }

        public void GotoVodViewer(TopStreamResult stream)
        {
            if (stream?.LivestreamModel == null) return;

            navigationService.NavigateTo<VodListViewModel>(vm =>
            {
                vm.StreamId = stream.LivestreamModel.Id;
                vm.SelectedApiClient = stream.LivestreamModel.ApiClient;
            });
        }

        public void ToggleNotify(TopStreamResult stream)
        {
            if (stream == null) return;

            stream.LivestreamModel.DontNotify = !stream.LivestreamModel.DontNotify;
            var excludeNotify = stream.LivestreamModel.ToExcludeNotify();
            if (settingsHandler.Settings.ExcludeFromNotifying.Any(x => Equals(x, excludeNotify)))
            {
                settingsHandler.Settings.ExcludeFromNotifying.Remove(excludeNotify);
            }
            else
            {
                settingsHandler.Settings.ExcludeFromNotifying.Add(excludeNotify);
            }
        }

        public async Task StreamClicked(TopStreamResult topStreamResult)
        {
            if (topStreamResult.IsBusy) return;
            topStreamResult.IsBusy = true;

            if (topStreamResult.IsMonitored)
            {
                await UnmonitorStream(topStreamResult);
            }
            else
            {
                await MonitorStream(topStreamResult);
            }

            topStreamResult.IsBusy = false;
        }

        protected override void OnInitialize()
        {
            ApiClients = new BindableCollection<IApiClient>(apiClientFactory.GetAll().Where(x => x.HasTopStreamsSupport));
            if (SelectedApiClient == null)
                SelectedApiClient = apiClientFactory.Get<TwitchApiClient>();
            base.OnInitialize();
        }

        protected override async void OnViewLoaded(object view)
        {
            if (Execute.InDesignMode) return;

            await EnsureItems();
            base.OnViewLoaded(view);
        }

        private async Task UnmonitorStream(TopStreamResult topStreamResult)
        {
            try
            {
                var livestreamModel = monitorStreamsModel.Livestreams.FirstOrDefault(x => Equals(x, topStreamResult.LivestreamModel));

                if (livestreamModel != null)
                {
                    monitorStreamsModel.RemoveLivestream(livestreamModel);
                }
                topStreamResult.IsMonitored = false;
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Error", "An error occured removing the stream from monitoring:" + ex.Message);
            }
        }

        private async Task MonitorStream(TopStreamResult topStreamResult)
        {
            try
            {
                await monitorStreamsModel.AddLivestream(topStreamResult.LivestreamModel);
                topStreamResult.IsMonitored = true;
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Error", "An error occured adding the stream for monitoring: " + ex.Message);
            }
        }

        protected override async void MovePage()
        {
            if (!IsActive) return;

            await EnsureItems();
            base.MovePage();
        }

        // Makes sure the items collection is populated with items for the current page
        private async Task EnsureItems()
        {
            if (!IsActive) return;

            LoadingItems = true;

            try
            {
                Items.Clear();

                var topStreamsQuery = new TopStreamQuery
                {
                    GameName = GameName,
                    Skip = (Page - 1) * ItemsPerPage,
                    Take = ItemsPerPage,
                };
                var topStreams = await SelectedApiClient.GetTopStreams(topStreamsQuery);
                var monitoredStreams = monitorStreamsModel.Livestreams;

                var topStreamResults = new List<TopStreamResult>();
                foreach (var topLivestream in topStreams)
                {
                    var topStreamResult = new TopStreamResult(topLivestream);
                    topStreamResult.IsMonitored = monitoredStreams.Any(x => Equals(x, topLivestream));
                    topStreamResult.LivestreamModel.SetLivestreamNotifyState(settingsHandler.Settings);

                    topStreamResults.Add(topStreamResult);
                }

                Items.AddRange(topStreamResults);
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("Error",
                    $"An error occured attempting to get top streams from '{SelectedApiClient.ApiName}'.{Environment.NewLine}{Environment.NewLine}{ex}");
            }

            LoadingItems = false;
        }

        private async void UpdatePossibleGameNames()
        {
            var game = GameName; // store local variable in case GameName changes while this is running
            if (string.IsNullOrWhiteSpace(game)) return;

            try
            {
                var games = await SelectedApiClient.GetKnownGameNames(game);
                PossibleGameNames.Clear();
                PossibleGameNames.AddRange(games.Select(x => x.GameName));
                ExpandPossibleGames = true;
            }
            catch
            {
                // make sure we dont crash just updating auto-completion options
            }
        }
    }
}