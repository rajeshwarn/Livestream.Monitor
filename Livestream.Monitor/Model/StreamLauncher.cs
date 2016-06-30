﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Livestream.Monitor.Core;
using Livestream.Monitor.Core.Utility;
using Livestream.Monitor.Model.Monitoring;
using Livestream.Monitor.ViewModels;
using Action = System.Action;

namespace Livestream.Monitor.Model
{
    public class StreamLauncher
    {
        // used for 
        private static readonly object watchingStreamsLock = new object();

        private readonly ISettingsHandler settingsHandler;
        private readonly IWindowManager windowManager;
        private readonly List<LivestreamModel> watchingStreams = new List<LivestreamModel>();

        public StreamLauncher(ISettingsHandler settingsHandler, IWindowManager windowManager)
        {
            if (settingsHandler == null) throw new ArgumentNullException(nameof(settingsHandler));
            if (windowManager == null) throw new ArgumentNullException(nameof(windowManager));

            this.settingsHandler = settingsHandler;
            this.windowManager = windowManager;
        }

        public List<LivestreamModel> WatchingStreams
        {
            get
            {
                lock (watchingStreamsLock)
                {
                    // return a copy of the streams to prevent modification issues
                    return watchingStreams.ToList();
                }
            }
        }

        public async Task OpenChat(LivestreamModel livestreamModel, IViewAware fromScreen)
        {
            // guard against invalid/missing chrome path
            var chromeLocation = settingsHandler.Settings.ChromeFullPath;
            if (string.IsNullOrWhiteSpace(chromeLocation))
            {
                await fromScreen.ShowMessageAsync("No chrome locations specified",
                    $"Chrome location is not set in settings.{Environment.NewLine}Chat relies on chrome to function.");
                return;
            }
            if (!File.Exists(chromeLocation))
            {
                await fromScreen.ShowMessageAsync("Chrome not found",
                    $"Could not find chrome @ {chromeLocation}.{Environment.NewLine}Chat relies on chrome to function.");
                return;
            }
            // guard against stream provider not having chat support
            if (!livestreamModel.ApiClient.HasChatSupport)
            {
                await fromScreen.ShowMessageAsync("Chat not supported",
                    $"No external chat support for stream provider '{livestreamModel.ApiClient.ApiName}'");
                return;
            }
            
            string chromeArgs = $"--app={livestreamModel.ChatUrl} --window-size=350,758";

            await Task.Run(async () =>
            {
                try
                {
                    var proc = new Process()
                    {
                        StartInfo =
                        {
                            FileName = chromeLocation,
                            Arguments = chromeArgs,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }
                    };

                    proc.Start();
                }
                catch (Exception ex)
                {
                    await fromScreen.ShowMessageAsync("Error launching chat", ex.Message);
                }
            });
        }

        public void OpenStream(LivestreamModel livestreamModel)
        {
            if (livestreamModel?.ApiClient == null || !livestreamModel.Live) return;

            // TODO - move the stream quality into the IApiClient
            // Fall back to source stream quality for non-partnered Livestreams
            var streamQuality = (!livestreamModel.IsPartner &&
                                 settingsHandler.Settings.DefaultStreamQuality != StreamQuality.Best)
                                    ? StreamQuality.Best
                                    : settingsHandler.Settings.DefaultStreamQuality;

            string livestreamerArgs = $"{livestreamModel.StreamUrl} {streamQuality}";
            var messageBoxViewModel = ShowLivestreamerLoadMessageBox(
                title: $"Stream '{livestreamModel.DisplayName}'",
                messageText: $"Launching livestreamer....{Environment.NewLine}'livestreamer.exe {livestreamerArgs}'");

            // Notify the user if the quality has been swapped back to source due to the livestream not being partenered (twitch specific).
            if (!livestreamModel.IsPartner && streamQuality != StreamQuality.Best)
            {
                messageBoxViewModel.MessageText += Environment.NewLine + "[NOTE] Channel is not a twitch partner so falling back to Source quality";
            }

            lock (watchingStreamsLock)
            {
                watchingStreams.Add(livestreamModel);
            }

            StartLivestreamer(livestreamerArgs, messageBoxViewModel, onClose: () =>
            {
                lock (watchingStreamsLock)
                {
                    watchingStreams.Remove(livestreamModel);
                }
            });
        }

        public void OpenVod(VodDetails vodDetails)
        {
            if (string.IsNullOrWhiteSpace(vodDetails.Url) || !Uri.IsWellFormedUriString(vodDetails.Url, UriKind.Absolute)) return;

            string livestreamerArgs = $"--player-passthrough hls {vodDetails.Url} best";
            const int maxTitleLength = 70;
            var title = vodDetails.Title?.Length > maxTitleLength ? vodDetails.Title.Substring(0, maxTitleLength) + "..." : vodDetails.Title;

            var messageBoxViewModel = ShowLivestreamerLoadMessageBox(
                title: title,
                messageText: $"Launching livestreamer....{Environment.NewLine}'livestreamer.exe {livestreamerArgs}'");

            StartLivestreamer(livestreamerArgs, messageBoxViewModel);
        }

        private void StartLivestreamer(string livestreamerArgs, MessageBoxViewModel messageBoxViewModel, Action onClose = null)
        {
            if (!CheckLivestreamerExists()) return;

            // the process needs to be launched from its own thread so it doesn't lockup the UI
            Task.Run(() =>
            {
                var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = settingsHandler.Settings.LivestreamerFullPath,
                        Arguments = livestreamerArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };

                bool preventClose = false;

                // see below for output handler
                proc.ErrorDataReceived +=
                    (sender, args) =>
                    {
                        if (args.Data == null) return;

                        preventClose = true;
                        messageBoxViewModel.MessageText += Environment.NewLine + args.Data;
                    };
                proc.OutputDataReceived +=
                    (sender, args) =>
                    {
                        if (args.Data == null) return;
                        if (args.Data.StartsWith("[cli][info] Starting player") && settingsHandler.Settings.HideStreamOutputMessageBoxOnLoad)
                        {
                            messageBoxViewModel.TryClose();
                            // can continue adding messages, the view model still exists so it doesn't really matter
                        }

                        messageBoxViewModel.MessageText += Environment.NewLine + args.Data;
                    };

                try
                {
                    proc.Start();

                    proc.BeginErrorReadLine();
                    proc.BeginOutputReadLine();

                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        preventClose = true;
                        // open the message box if it was somehow closed prior to the error being displayed
                        if (!messageBoxViewModel.IsActive) windowManager.ShowWindow(messageBoxViewModel, null, new WindowSettingsBuilder().SizeToContent().NoResizeBorderless().Create());
                    }

                    onClose?.Invoke();
                }
                catch (Exception)
                {
                    // TODO log errors opening stream
                }

                if (preventClose)
                {
                    messageBoxViewModel.MessageText += Environment.NewLine + Environment.NewLine +
                                                       "ERROR occured in Livestreamer: Manually close this window when you've finished reading the livestreamer output.";
                }
                else
                    messageBoxViewModel.TryClose();
            });
        }

        private MessageBoxViewModel ShowLivestreamerLoadMessageBox(string title, string messageText)
        {
            var messageBoxViewModel = new MessageBoxViewModel
            {
                DisplayName = title,
                MessageText = messageText,
            };
            messageBoxViewModel.ShowHideOnLoadCheckbox(settingsHandler);

            var settings = new WindowSettingsBuilder().SizeToContent()
                                                      .NoResizeBorderless()
                                                      .Create();

            windowManager.ShowWindow(messageBoxViewModel, null, settings);
            return messageBoxViewModel;
        }

        private bool CheckLivestreamerExists()
        {
            if (File.Exists(settingsHandler.Settings.LivestreamerFullPath)) return true;

            var msgBox = new MessageBoxViewModel()
            {
                DisplayName = "Livestreamer not found",
                MessageText =
                    $"Could not find livestreamer @ {settingsHandler.Settings.LivestreamerFullPath}.{Environment.NewLine} Please download and install livestreamer from 'http://docs.livestreamer.io/install.html#windows-binaries'"
            };

            var settings = new WindowSettingsBuilder().SizeToContent()
                                                      .NoResizeBorderless()
                                                      .Create();

            windowManager.ShowWindow(msgBox, null, settings);
            return false;
        }
    }
}
