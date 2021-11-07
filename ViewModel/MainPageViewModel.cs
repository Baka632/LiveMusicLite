﻿using LiveMusicLite.Commands;
using LiveMusicLite.Helper;
using LiveMusicLite.Models;
using LiveMusicLite.Services;
using LiveMusicLite.Views;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace LiveMusicLite.ViewModel
{
    public class MainPageViewModel : NotificationObject
    {
        /// <summary>
        /// 循环播放的状态
        /// </summary>
        private string RepeatingMusicState = "循环播放:关";
        private string _RepeatButtonIconString = "\uE1CD";
        private string ShufflingMusicState = "随机播放:关";
        private string NowPlayingState = "暂停";
        private bool IsMediaPlayingFailed = false;
        /// <summary>
        /// 指示鼠标指针是否在拖动进度条的值
        /// </summary>
        private bool PointerEntered = false;
        /// <summary>
        /// 新的进度条值
        /// </summary>
        private double SliderNewValue;
        /// <summary>
        /// 支持的音频格式数组
        /// </summary>
        private static readonly string[] SupportedAudioFormats = new string[]
        {
            ".mp3", ".wav", ".wma", ".aac", ".adt", ".adts", ".ac3", ".ec3",
        };

        public MusicService MusicService { get; }
        public MusicInfomation MusicInfomation { get; }
        public MediaCommand RepeatMusicCommand { get; }
        public MediaCommand StopMusicCommand { get; set; }
        public MediaCommand PlayPauseMusicCommand { get; }
        public MediaCommand NextMusicCommand { get; }
        public MediaCommand PreviousMusicCommand { get; }
        public MediaCommand MuteCommand { get; set; }
        public MediaCommand ShuffleMusicCommand { get; }
        public MediaCommand ChangePlayRateCommand { get; }
        public DelegateCommand OpenAndPlayMusicCommand { get; }
        public DelegateCommand OpenSettingsCommand { get; } = new DelegateCommand()
        {
            ExecuteAction = async (object obj) =>
            {
                SettingsContentDialog settingsContentDialog = new SettingsContentDialog();
                _ = await settingsContentDialog.ShowAsync();
            }
        };
        public VolumeGlyphState VolumeGlyphState { get; }
        public TileHelper TileHelper { get; } = new TileHelper();
        private FileService FileService { get; }

        /// <summary>
        /// 播放器"重复播放"状态的属性
        /// </summary>
        public string RepeatingMusicProperties
        {
            get => RepeatingMusicState;
            set
            {
                RepeatingMusicState = value;
                OnPropertiesChanged();
            }
        }

        /// <summary>
        /// 播放器"循环播放"状态的属性
        /// </summary>
        public string ShufflingMusicProperties
        {
            get => ShufflingMusicState;
            set
            {
                ShufflingMusicState = value;
                OnPropertiesChanged();
            }
        }

        /// <summary>
        /// 播放器"正在播放"状态的属性
        /// </summary>
        public string NowPlayingProperties
        {
            get => NowPlayingState;
            set
            {
                NowPlayingState = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        public string RepeatButtonIconString
        {
            get => _RepeatButtonIconString;
            set 
            { 
                _RepeatButtonIconString = value;
                OnPropertiesChanged();
            }
        }

        private bool _IsMediaControlShow;

        public bool IsMediaControlShow
        {
            get => _IsMediaControlShow;
            set
            {
                _IsMediaControlShow = value;
                OnPropertiesChanged();
                OnPropertiesChanged(nameof(IsMediaControlShowReverse));
            }
        }

        public bool IsMediaControlShowReverse => !IsMediaControlShow;

        private string _PausePlayingButtonIcon = "\uE102";

        public string PausePlayingButtonIcon
        {
            get => _PausePlayingButtonIcon;
            set
            {
                _PausePlayingButtonIcon = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        private string _TimeTextBlockText;

        public string TimeTextBlockText
        {
            get => _TimeTextBlockText;
            set
            {
                _TimeTextBlockText = value;
                OnPropertiesChangedUsingMainThread();
            }
        }

        private double _ProcessSliderValue;
        private bool IsMediaPlayingFailedDialogShow = false;

        public double ProcessSliderValue
        {
            get => _ProcessSliderValue;
            set
            {
                _ProcessSliderValue = value;
                if (PointerEntered != true)
                {
                    OnPropertiesChangedUsingMainThread();
                }
            }
        }


        public MainPageViewModel()
        {
            MusicService = new MusicService();
            MusicInfomation = new MusicInfomation();
            MuteCommand = new MediaCommand(MusicService, MediaCommandType.Mute);
            RepeatMusicCommand = new MediaCommand(MusicService, MediaCommandType.Repeat);
            RepeatMusicCommand.CommandExecuted += OnRepeatMusicCommandExecuted;
            PlayPauseMusicCommand = new MediaCommand(MusicService, MediaCommandType.PlayAndPause);
            NextMusicCommand = new MediaCommand(MusicService, MediaCommandType.Next);
            PreviousMusicCommand = new MediaCommand(MusicService, MediaCommandType.Previous);
            ShuffleMusicCommand = new MediaCommand(MusicService, MediaCommandType.Shuffle);
            StopMusicCommand = new MediaCommand(MusicService, MediaCommandType.Stop)
            {
                ExecuteAction = (object obj) =>
                {
                    IsMediaControlShow = false;
                    MusicService.StopMusic();
                    MusicInfomation.ResetAllMusicProperties();
                    TileHelper.DeleteTile();
                },
                CanExecuteFunc = (object obj) =>
                {
                    return IsMediaControlShow;
                }
            };
            ChangePlayRateCommand = new MediaCommand(MusicService, MediaCommandType.ChangePlayRate);
            ShuffleMusicCommand.CommandExecuted += OnShuffleMusicCommandExecuted;
            OpenAndPlayMusicCommand = new DelegateCommand() { ExecuteAction = async (object obj) => await OpenAndPlayMusicFile(obj)};
            FileService = new FileService();
            VolumeGlyphState = new VolumeGlyphState(MusicService, MusicInfomation);

            MusicService.MediaPlaybackList.CurrentItemChanged += OnCurrentItemChanged;
            MusicService.MediaPlaybackList.ItemFailed += OnMediaPlaybackListItemFailed;
            MusicService.MediaPlayer.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;
            MusicService.MediaPlayer.PlaybackSession.NaturalDurationChanged += OnNaturalDurationChanged;
            MusicService.MediaPlayer.PlaybackSession.PositionChanged += OnPositionChanged;
        }

        private void OnMediaPlaybackListItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            IsMediaPlayingFailed = true;
        }

        private async Task ShowPlayingErrorDialog()
        {
            await RunOnMainThread(async () =>
            {
                if (IsMediaPlayingFailedDialogShow)
                {
                    return;
                }
                IsMediaPlayingFailedDialogShow = true;
                MusicPlayingErrorDialog dialog = new MusicPlayingErrorDialog();
                _ = await dialog.ShowAsync();
                IsMediaPlayingFailedDialogShow = false;
            });
        }

        private void OnPositionChanged(MediaPlaybackSession sender, object args)
        {
            ProcessSliderValue = MusicService.MediaPlayer.PlaybackSession.Position.TotalSeconds;
            if (PointerEntered)
            {
                return;
            }
            if (MusicService.MediaPlayer.PlaybackSession.Position.TotalSeconds >= 3600)
            {
                TimeTextBlockText = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"h\:mm\:ss");
            }
            else
            {
                TimeTextBlockText = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
            }
        }

        private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
        {
            if (sender.NaturalDuration.TotalSeconds >= 3600)
            {
                MusicInfomation.MusicLenthProperties = sender.NaturalDuration.ToString(@"h\:mm\:ss");
            }
            else
            {
                MusicInfomation.MusicLenthProperties = sender.NaturalDuration.ToString(@"m\:ss");
            }
            MusicInfomation.MusicDurationProperties = sender.NaturalDuration.TotalSeconds;
        }

        private async void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    NowPlayingProperties = "暂停";
                    PausePlayingButtonIcon = "\uE103";
                    break;
                case MediaPlaybackState.Paused:
                    NowPlayingProperties = "播放";
                    PausePlayingButtonIcon = "\uE102";
                    if (IsMediaPlayingFailed)
                    {
                        await ShowPlayingErrorDialog();
                    }
                    break;
                case MediaPlaybackState.None:
                case MediaPlaybackState.Opening:
                case MediaPlaybackState.Buffering:
                default:
                    break;
            }
        }

        private async void OnCurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            if (MusicService.MediaPlaybackList.CurrentItem != null)
            {
                MusicDisplayProperties currentMusicInfomation = MusicService.MediaPlaybackList.CurrentItem.GetDisplayProperties().MusicProperties;
                RandomAccessStreamReference CurrentItemMusicThumbnail = sender.CurrentItem.GetDisplayProperties().Thumbnail;
                IRandomAccessStream musicThumbnail = await CurrentItemMusicThumbnail.OpenReadAsync();
                MusicInfomation.MusicAlbumArtistProperties = currentMusicInfomation.AlbumArtist;
                MusicInfomation.MusicTitleProperties = currentMusicInfomation.Title;
                MusicInfomation.MusicAlbumProperties = currentMusicInfomation.AlbumTitle;
                await RunOnMainThread(async () => await WorkOnMainThread(musicThumbnail));
                _ = await FileService.CreateMusicAlbumCoverFile(currentMusicInfomation.AlbumTitle, musicThumbnail);
                await SetTileSourceAsync();
            }
            else
            {
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            }

            async Task WorkOnMainThread(IRandomAccessStream musicThumbnail)
            {
                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(musicThumbnail);
                MusicInfomation.MusicImageProperties = image;
            }
        }

        private void OnShuffleMusicCommandExecuted(MediaCommandExecutedEventArgs obj)
        {
            ShufflingMusicProperties = MusicService.MediaPlaybackList.ShuffleEnabled ? "随机播放:开" : "随机播放:关";
        }

        private void OnRepeatMusicCommandExecuted(MediaCommandExecutedEventArgs args)
        {
            switch ((bool?)args.Parameter)
            {
                case true:
                    RepeatingMusicProperties = "循环播放:全部循环";
                    break;
                case false:
                    RepeatButtonIconString = "\uE1CD";
                    RepeatingMusicProperties = "循环播放:关闭循环";
                    break;
                case null:
                    RepeatButtonIconString = "\uE1CC";
                    RepeatingMusicProperties = "循环播放:单曲循环";
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 开始拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnProgressSliderEnterPressedReleased()
        {
            PointerEntered = true;
        }

        /// <summary>
        /// 结束拖拽进度条时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnProgressSliderPointerReleased()
        {
            MusicService.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SliderNewValue);
            TimeTextBlockText = MusicService.MediaPlayer.PlaybackSession.Position.ToString(@"m\:ss");
            PointerEntered = false;
        }

        /// <summary>
        /// 当进度条的值改变时调用的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnProcessSliderValueChanged(RangeBaseValueChangedEventArgs e)
        {
            SliderNewValue = e.NewValue;
            if (PointerEntered)
            {
                TimeTextBlockText = e.NewValue >= 3600
                    ? TimeSpan.FromSeconds(e.NewValue).ToString(@"h\:mm\:ss")
                    : TimeSpan.FromSeconds(e.NewValue).ToString(@"m\:ss");
            }
        }

        private async Task OpenAndPlayMusicFile(object storageFile = null)
        {
            bool IsUsingFilePicker = false;
            IReadOnlyList<StorageFile> fileList;
            if (storageFile is IReadOnlyList<StorageFile> storageFiles)
            {
                fileList = storageFiles;
            }
            else
            {
                IsUsingFilePicker = true;
                Windows.Storage.Pickers.FileOpenPicker MusicPicker = new Windows.Storage.Pickers.FileOpenPicker
                {
                    ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                    SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
                };
                foreach (string fileExtension in SupportedAudioFormats)
                {
                    MusicPicker.FileTypeFilter.Add(fileExtension);
                }

                fileList = await MusicPicker.PickMultipleFilesAsync();
            }

            if (fileList.Count > 0)
            {
                if (IsMediaControlShow != true)
                {
                    IsMediaControlShow = true;
                }

                if (Settings.MediaOpenOperation && IsUsingFilePicker != true)
                {
                    MusicInfomation.ResetAllMusicProperties();
                    if (MusicService.MediaPlaybackList.Items != null)
                    {
                        MusicService.StopMusic();
                    }
                }
                await FileService.GetMusicPropertiesAndPlayAysnc(fileList, MusicService.MediaPlaybackList);
            }
        }

        /// <summary>
        /// 设置磁贴的源
        /// </summary>
        private async Task SetTileSourceAsync()
        {
            if (MusicService.MediaPlaybackList.Items.Count > MusicService.MediaPlaybackList.CurrentItemIndex + 1)
            {
                MusicDisplayProperties MusicProps;
                if (MusicService.MediaPlaybackList.ShuffleEnabled)
                {
                    IReadOnlyList<MediaPlaybackItem> shuffledItems = MusicService.MediaPlaybackList.ShuffledItems;
                    int index = await Task.Run(() => shuffledItems.IndexOf(MusicService.MediaPlaybackList.CurrentItem) + 1);

                    if (shuffledItems.Count > index)
                    {
                        MusicProps = shuffledItems[index].GetDisplayProperties().MusicProperties;
                    }
                    else
                    {
                        if (MusicService.MediaPlaybackList.AutoRepeatEnabled)
                        {
                            MusicProps = shuffledItems.First().GetDisplayProperties().MusicProperties;
                        }
                        else
                        {
                            TileHelper.ShowTitle(MusicInfomation.MusicTitleProperties,
                                     MusicInfomation.MusicAlbumArtistProperties,
                                     MusicInfomation.MusicAlbumProperties,
                                     false);
                            return;
                        }
                    }
                }
                else
                {
                    int index = (int)(MusicService.MediaPlaybackList.CurrentItemIndex + 1);
                    MusicProps = MusicService.MediaPlaybackList.Items[index].GetDisplayProperties().MusicProperties;
                }
                TileHelper.ShowTitle(MusicInfomation.MusicTitleProperties,
                                     MusicInfomation.MusicAlbumArtistProperties,
                                     MusicInfomation.MusicAlbumProperties,
                                     true,
                                     MusicProps.Title,
                                     MusicProps.AlbumArtist,
                                     MusicProps.AlbumTitle);
            }
            else
            {
                if (MusicService.MediaPlaybackList.AutoRepeatEnabled)
                {
                    IReadOnlyList<MediaPlaybackItem> shuffledItems = MusicService.MediaPlaybackList.ShuffledItems;
                    MusicDisplayProperties MusicProps = shuffledItems.First().GetDisplayProperties().MusicProperties;
                    TileHelper.ShowTitle(MusicInfomation.MusicTitleProperties,
                                     MusicInfomation.MusicAlbumArtistProperties,
                                     MusicInfomation.MusicAlbumProperties,
                                     true,
                                     MusicProps.Title,
                                     MusicProps.AlbumArtist,
                                     MusicProps.AlbumTitle);
                }
                else
                {
                    TileHelper.ShowTitle(MusicInfomation.MusicTitleProperties,
                                     MusicInfomation.MusicAlbumArtistProperties,
                                     MusicInfomation.MusicAlbumProperties,
                                     false);
                }
            }
        }

        private async Task RunOnMainThread(DispatchedHandler handler, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            await Dispatcher.RunAsync(priority, handler);
        }
    }

    internal static class IReadOnlyListExtension
    {
        public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind)
        {
            int i = 0;
            foreach (T element in self)
            {
                if (Equals(element, elementToFind))
                {
                    return i;
                }

                i++;
            }
            return -1;
        }
    }
}
