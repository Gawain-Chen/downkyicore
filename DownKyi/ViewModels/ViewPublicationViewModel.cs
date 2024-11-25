﻿#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DownKyi.Core.BiliApi.VideoStream;
using DownKyi.Core.Storage;
using DownKyi.Core.Utils;
using DownKyi.CustomControl;
using DownKyi.Events;
using DownKyi.Images;
using DownKyi.PrismExtension.Dialog;
using DownKyi.Services;
using DownKyi.Services.Download;
using DownKyi.Utils;
using DownKyi.ViewModels.PageViewModels;
using DownKyi.ViewModels.UserSpace;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace DownKyi.ViewModels
{
    public class ViewPublicationViewModel : ViewModelBase
    {
        public const string Tag = "PagePublication";

        private CancellationTokenSource _tokenSource;

        private long _mid = -1;

        // 每页视频数量，暂时在此写死，以后在设置中增加选项
        private readonly int _videoNumberInPage = 30;

        #region 页面属性申明

        private string _pageName = Tag;

        public string PageName
        {
            get => _pageName;
            set => SetProperty(ref _pageName, value);
        }

        private bool _loading;

        public bool Loading
        {
            get => _loading;
            set => SetProperty(ref _loading, value);
        }

        private bool _loadingVisibility;

        public bool LoadingVisibility
        {
            get => _loadingVisibility;
            set => SetProperty(ref _loadingVisibility, value);
        }

        private bool _noDataVisibility;

        public bool NoDataVisibility
        {
            get => _noDataVisibility;
            set => SetProperty(ref _noDataVisibility, value);
        }

        private VectorImage _arrowBack;

        public VectorImage ArrowBack
        {
            get => _arrowBack;
            set => SetProperty(ref _arrowBack, value);
        }

        private VectorImage _downloadManage;

        public VectorImage DownloadManage
        {
            get => _downloadManage;
            set => SetProperty(ref _downloadManage, value);
        }

        private ObservableCollection<TabHeader> _tabHeaders;

        public ObservableCollection<TabHeader> TabHeaders
        {
            get => _tabHeaders;
            set => SetProperty(ref _tabHeaders, value);
        }

        private int _selectTabId;

        public int SelectTabId
        {
            get => _selectTabId;
            set => SetProperty(ref _selectTabId, value);
        }

        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        private CustomPagerViewModel _pager;

        public CustomPagerViewModel Pager
        {
            get => _pager;
            set => SetProperty(ref _pager, value);
        }

        private ObservableCollection<PublicationMedia> _medias;

        public ObservableCollection<PublicationMedia> Medias
        {
            get => _medias;
            set => SetProperty(ref _medias, value);
        }

        private bool _isSelectAll;

        public bool IsSelectAll
        {
            get => _isSelectAll;
            set => SetProperty(ref _isSelectAll, value);
        }

        #endregion

        public ViewPublicationViewModel(IEventAggregator eventAggregator, IDialogService dialogService) : base(
            eventAggregator)
        {
            DialogService = dialogService;

            #region 属性初始化

            // 初始化loading
            Loading = true;
            LoadingVisibility = false;
            NoDataVisibility = false;

            ArrowBack = NavigationIcon.Instance().ArrowBack;
            ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

            // 下载管理按钮
            DownloadManage = ButtonIcon.Instance().DownloadManage;
            DownloadManage.Height = 24;
            DownloadManage.Width = 24;
            DownloadManage.Fill = DictionaryResource.GetColor("ColorPrimary");

            TabHeaders = new ObservableCollection<TabHeader>();
            Medias = new ObservableCollection<PublicationMedia>();

            #endregion
        }

        #region 命令申明

        // 返回事件
        private DelegateCommand _backSpaceCommand;

        public DelegateCommand BackSpaceCommand => _backSpaceCommand ??= new DelegateCommand(ExecuteBackSpace);

        /// <summary>
        /// 返回事件
        /// </summary>
        private void ExecuteBackSpace()
        {
            ArrowBack.Fill = DictionaryResource.GetColor("ColorText");

            // 结束任务
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _tokenSource = null;
            var parameter = new NavigationParam
            {
                ViewName = ParentView,
                ParentViewName = null,
                Parameter = null
            };
            EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
        }

        // 前往下载管理页面
        private DelegateCommand _downloadManagerCommand;

        public DelegateCommand DownloadManagerCommand => _downloadManagerCommand ??= new DelegateCommand(ExecuteDownloadManagerCommand);

        /// <summary>
        /// 前往下载管理页面
        /// </summary>
        private void ExecuteDownloadManagerCommand()
        {
            var parameter = new NavigationParam
            {
                ViewName = ViewDownloadManagerViewModel.Tag,
                ParentViewName = Tag,
                Parameter = null
            };
            EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
        }

        // 左侧tab点击事件
        private DelegateCommand<object> _leftTabHeadersCommand;

        public DelegateCommand<object> LeftTabHeadersCommand =>
            _leftTabHeadersCommand ??= new DelegateCommand<object>(ExecuteLeftTabHeadersCommand, CanExecuteLeftTabHeadersCommand);

        /// <summary>
        /// 左侧tab点击事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteLeftTabHeadersCommand(object parameter)
        {
            if (parameter is not TabHeader tabHeader)
            {
                return;
            }

            // 页面选择
            Pager = new CustomPagerViewModel(1,
                (int)Math.Ceiling(double.Parse(tabHeader.SubTitle) / _videoNumberInPage));
            Pager.CurrentChanged += OnCurrentChanged_Pager;
            Pager.CountChanged += OnCountChanged_Pager;
            Pager.Current = 1;
        }

        /// <summary>
        /// 左侧tab点击事件是否允许执行
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private bool CanExecuteLeftTabHeadersCommand(object parameter)
        {
            return IsEnabled;
        }

        // 全选按钮点击事件
        private DelegateCommand<object> _selectAllCommand;

        public DelegateCommand<object> SelectAllCommand => _selectAllCommand ??= new DelegateCommand<object>(ExecuteSelectAllCommand);

        /// <summary>
        /// 全选按钮点击事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteSelectAllCommand(object parameter)
        {
            if (IsSelectAll)
            {
                foreach (var item in Medias)
                {
                    item.IsSelected = true;
                }
            }
            else
            {
                foreach (var item in Medias)
                {
                    item.IsSelected = false;
                }
            }
        }

        // 列表选择事件
        private DelegateCommand<object> _mediasCommand;

        public DelegateCommand<object> MediasCommand => _mediasCommand ??= new DelegateCommand<object>(ExecuteMediasCommand);

        /// <summary>
        /// 列表选择事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteMediasCommand(object parameter)
        {
            if (parameter is not IList selectedMedia)
            {
                return;
            }

            IsSelectAll = selectedMedia.Count == Medias.Count;
        }

        // 添加选中项到下载列表事件
        private DelegateCommand _addToDownloadCommand;

        public DelegateCommand AddToDownloadCommand => _addToDownloadCommand ??= new DelegateCommand(ExecuteAddToDownloadCommand);

        /// <summary>
        /// 添加选中项到下载列表事件
        /// </summary>
        private void ExecuteAddToDownloadCommand()
        {
            AddToDownload(true);
        }

        // 添加所有视频到下载列表事件
        private DelegateCommand _addAllToDownloadCommand;

        public DelegateCommand AddAllToDownloadCommand => _addAllToDownloadCommand ??= new DelegateCommand(ExecuteAddAllToDownloadCommand);

        /// <summary>
        /// 添加所有视频到下载列表事件
        /// </summary>
        private void ExecuteAddAllToDownloadCommand()
        {
            AddToDownload(false);
        }

        #endregion

        /// <summary>
        /// 添加到下载
        /// </summary>
        /// <param name="isOnlySelected"></param>
        private async void AddToDownload(bool isOnlySelected)
        {
            // 收藏夹里只有视频
            var addToDownloadService = new AddToDownloadService(PlayStreamType.VIDEO);

            // 选择文件夹
            var directory = await addToDownloadService.SetDirectory(DialogService);

            // 视频计数
            var i = 0;
            await Task.Run(async () =>
            {
                // 为了避免执行其他操作时，
                // Medias变化导致的异常
                var list = Medias.ToList();

                // 添加到下载
                foreach (var videoInfoService in from media in list where !isOnlySelected || media.IsSelected select new VideoInfoService(media.Bvid))
                {
                    addToDownloadService.SetVideoInfoService(videoInfoService);
                    addToDownloadService.GetVideo();
                    addToDownloadService.ParseVideo(videoInfoService);
                    // 下载
                    i += await addToDownloadService.AddToDownload(EventAggregator, DialogService, directory);
                }
            });

            if (directory == null)
            {
                return;
            }

            // 通知用户添加到下载列表的结果
            if (i <= 0)
            {
                EventAggregator.GetEvent<MessageEvent>().Publish(DictionaryResource.GetString("TipAddDownloadingZero"));
            }
            else
            {
                EventAggregator.GetEvent<MessageEvent>()
                    .Publish(
                        $"{DictionaryResource.GetString("TipAddDownloadingFinished1")}{i}{DictionaryResource.GetString("TipAddDownloadingFinished2")}");
            }
        }

        private void OnCountChanged_Pager(int count)
        {
        }

        private bool OnCurrentChanged_Pager(int old, int current)
        {
            if (!IsEnabled)
            {
                //Pager.Current = old;
                return false;
            }

            Medias.Clear();
            IsSelectAll = false;
            LoadingVisibility = true;
            NoDataVisibility = false;

            _ = UpdatePublication(current);

            return true;
        }
        
        private static string StringToUnicode(string s)
        {
            var charbuffers = s.ToCharArray();
            byte[] buffer;
            var sb = new StringBuilder();
            for (var i = 0; i < charbuffers.Length; i++)
            {
                buffer = Encoding.Unicode.GetBytes(charbuffers[i].ToString());
                sb.Append($"\\u{buffer[1]:X2}{buffer[0]:X2}");
            }
            return sb.ToString();
        }

        private async Task UpdatePublication(int current)
        {
            _tokenSource?.Cancel();
            // 是否正在获取数据
            // 在所有的退出分支中都需要设为true
            IsEnabled = false;
            _tokenSource = new CancellationTokenSource();
            var cancellationToken = _tokenSource.Token;
            var defaultPic = ImageHelper.LoadFromResource(new Uri("avares://DownKyi/Resources/video-placeholder.png"));
            var tab = TabHeaders[SelectTabId];

            await Task.Run(async () =>
            {
                var publications = Core.BiliApi.Users.UserSpace.GetPublication(_mid, current, _videoNumberInPage, tab.Id);
                if (publications == null)
                {
                    // 没有数据，UI提示
                    LoadingVisibility = false;
                    NoDataVisibility = true;
                    return;
                }

                var videos = publications.Vlist;
                if (videos == null)
                {
                    // 没有数据，UI提示
                    LoadingVisibility = false;
                    NoDataVisibility = true;
                    return;
                }

                foreach (var video in videos)
                {
                    // 查询、保存封面
                    var coverUrl = video.Pic;
                   
                    // 播放数
                    var play = string.Empty;
                    if (video.Play > 0)
                    {
                        play = Format.FormatNumber(video.Play);
                    }
                    else
                    {
                        play = "--";
                    }

                    var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
                    var dateCTime = startTime.AddSeconds(video.Created);
                    var ctime = dateCTime.ToString("yyyy-MM-dd");

                    App.PropertyChangeAsync(() =>
                    {
                        var media = new PublicationMedia(EventAggregator)
                        {
                            Avid = video.Aid,
                            Bvid = video.Bvid,
                            Cover = defaultPic,
                            Duration = video.Length,
                            Title = video.Title,
                            PlayNumber = play,
                            CreateTime = ctime,
                            CoverUrl = coverUrl
                        };
                        _medias.Add(media);

                        LoadingVisibility = false;
                        NoDataVisibility = false;
                    });

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
                IsEnabled = true;
                await UpdateMediaCovers(cancellationToken);
            }, cancellationToken).ContinueWith(t => { });
        }
        
        private async Task UpdateMediaCovers(CancellationToken cancellationToken)
        {
            var storageCover = new StorageCover();
            var currentMedias = _medias.ToList();
            foreach (var media in currentMedias)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                media.Cover = await storageCover.GetCoverThumbnailAsync(media.Avid, media.Bvid, -1, media.CoverUrl, 200, 125);
            }
        }

        /// <summary>
        /// 初始化页面数据
        /// </summary>
        private void InitView()
        {
            ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

            DownloadManage = ButtonIcon.Instance().DownloadManage;
            DownloadManage.Height = 24;
            DownloadManage.Width = 24;
            DownloadManage.Fill = DictionaryResource.GetColor("ColorPrimary");

            TabHeaders.Clear();
            Medias.Clear();
            SelectTabId = -1;
            IsSelectAll = false;
        }

        /// <summary>
        /// 导航到页面时执行
        /// </summary>
        /// <param name="navigationContext"></param>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 根据传入参数不同执行不同任务
            var parameter = navigationContext.Parameters.GetValue<Dictionary<string, object>>("Parameter");
            if (parameter == null)
            {
                return;
            }

            InitView();

            _mid = (long)parameter["mid"];
            var tid = (int)parameter["tid"];
            var zones = (List<PublicationZone>)parameter["list"];

            foreach (var item in zones)
            {
                TabHeaders.Add(new TabHeader
                {
                    Id = item.Tid,
                    Title = item.Name,
                    SubTitle = item.Count.ToString()
                });
            }

            // 初始选中项
            var selectTab = TabHeaders.FirstOrDefault(item => item.Id == tid);
            SelectTabId = TabHeaders.IndexOf(selectTab);

            // 页面选择
            Pager = new CustomPagerViewModel(1,
                (int)Math.Ceiling(double.Parse(selectTab.SubTitle) / _videoNumberInPage));
            Pager.CurrentChanged += OnCurrentChanged_Pager;
            Pager.CountChanged += OnCountChanged_Pager;
            Pager.Current = 1;
        }
    }
}