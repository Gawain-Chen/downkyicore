﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DownKyi.Core.BiliApi.Favorites;
using DownKyi.Core.BiliApi.VideoStream;
using DownKyi.CustomControl;
using DownKyi.Events;
using DownKyi.Images;
using DownKyi.PrismExtension.Dialog;
using DownKyi.Services;
using DownKyi.Services.Download;
using DownKyi.Utils;
using DownKyi.ViewModels.PageViewModels;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace DownKyi.ViewModels;

public class ViewMyFavoritesViewModel : ViewModelBase
{
    public const string Tag = "PageMyFavorites";

    //private readonly IDialogService dialogService;

    private CancellationTokenSource tokenSource1;
    private CancellationTokenSource tokenSource2;

    private long mid = -1;

    // 每页视频数量，暂时在此写死，以后在设置中增加选项
    private readonly int VideoNumberInPage = 40;

    #region 页面属性申明

    private string pageName = Tag;

    public string PageName
    {
        get => pageName;
        set => SetProperty(ref pageName, value);
    }

    private bool contentVisibility;

    public bool ContentVisibility
    {
        get => contentVisibility;
        set => SetProperty(ref contentVisibility, value);
    }

    private bool loading;

    public bool Loading
    {
        get => loading;
        set => SetProperty(ref loading, value);
    }

    private bool loadingVisibility;

    public bool LoadingVisibility
    {
        get => loadingVisibility;
        set => SetProperty(ref loadingVisibility, value);
    }

    private bool noDataVisibility;

    public bool NoDataVisibility
    {
        get => noDataVisibility;
        set => SetProperty(ref noDataVisibility, value);
    }

    private bool mediaLoading;

    public bool MediaLoading
    {
        get => mediaLoading;
        set => SetProperty(ref mediaLoading, value);
    }

    private bool mediaContentVisibility;

    public bool MediaContentVisibility
    {
        get => mediaContentVisibility;
        set => SetProperty(ref mediaContentVisibility, value);
    }

    private bool mediaLoadingVisibility;

    public bool MediaLoadingVisibility
    {
        get => mediaLoadingVisibility;
        set => SetProperty(ref mediaLoadingVisibility, value);
    }

    private bool mediaNoDataVisibility;

    public bool MediaNoDataVisibility
    {
        get => mediaNoDataVisibility;
        set => SetProperty(ref mediaNoDataVisibility, value);
    }

    private VectorImage arrowBack;

    public VectorImage ArrowBack
    {
        get => arrowBack;
        set => SetProperty(ref arrowBack, value);
    }

    private VectorImage downloadManage;

    public VectorImage DownloadManage
    {
        get => downloadManage;
        set => SetProperty(ref downloadManage, value);
    }

    private ObservableCollection<TabHeader> tabHeaders;

    public ObservableCollection<TabHeader> TabHeaders
    {
        get => tabHeaders;
        set => SetProperty(ref tabHeaders, value);
    }

    private int selectTabId;

    public int SelectTabId
    {
        get => selectTabId;
        set => SetProperty(ref selectTabId, value);
    }

    private bool isEnabled = true;

    public bool IsEnabled
    {
        get => isEnabled;
        set => SetProperty(ref isEnabled, value);
    }

    private CustomPagerViewModel pager;

    public CustomPagerViewModel Pager
    {
        get => pager;
        set => SetProperty(ref pager, value);
    }

    private ObservableCollection<FavoritesMedia> medias;

    public ObservableCollection<FavoritesMedia> Medias
    {
        get => medias;
        set => SetProperty(ref medias, value);
    }

    private bool isSelectAll;

    public bool IsSelectAll
    {
        get => isSelectAll;
        set => SetProperty(ref isSelectAll, value);
    }

    private int selectedCount;
    public int SelectedCount
    {
        get => selectedCount;
        set => SetProperty(ref selectedCount, value);
    }
    private void UpdateSelectedCount()
    {
        try
        {
            int count = Medias.Where(p => p.IsSelected).Count();
            if (count != selectedCount)
            {
                SelectedCount = count;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"额eMedias:[{e.Message}]");
        }
    }
    #endregion

    public ViewMyFavoritesViewModel(IEventAggregator eventAggregator, IDialogService dialogService) : base(
        eventAggregator)
    {
        this.DialogService = dialogService;

        #region 属性初始化

        // 初始化loading gif
        Loading = true;
        LoadingVisibility = false;
        NoDataVisibility = false;

        MediaLoading = true;
        MediaLoadingVisibility = false;
        MediaNoDataVisibility = false;

        ArrowBack = NavigationIcon.Instance().ArrowBack;
        ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

        // 下载管理按钮
        DownloadManage = ButtonIcon.Instance().DownloadManage;
        DownloadManage.Height = 24;
        DownloadManage.Width = 24;
        DownloadManage.Fill = DictionaryResource.GetColor("ColorPrimary");

        TabHeaders = new ObservableCollection<TabHeader>();
        Medias = new ObservableCollection<FavoritesMedia>();

        #endregion
    }

    #region 命令申明

    // 返回事件
    private DelegateCommand backSpaceCommand;

    public DelegateCommand BackSpaceCommand =>
        backSpaceCommand ?? (backSpaceCommand = new DelegateCommand(ExecuteBackSpace));

    /// <summary>
    /// 返回事件
    /// </summary>
    private void ExecuteBackSpace()
    {
        InitView();

        ArrowBack.Fill = DictionaryResource.GetColor("ColorText");

        // 结束任务
        tokenSource1?.Cancel();
        tokenSource2?.Cancel();

        NavigationParam parameter = new NavigationParam
        {
            ViewName = ParentView,
            ParentViewName = null,
            Parameter = null
        };
        EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
    }

    // 前往下载管理页面
    private DelegateCommand downloadManagerCommand;

    public DelegateCommand DownloadManagerCommand => downloadManagerCommand ??
                                                     (downloadManagerCommand =
                                                         new DelegateCommand(ExecuteDownloadManagerCommand));

    /// <summary>
    /// 前往下载管理页面
    /// </summary>
    private void ExecuteDownloadManagerCommand()
    {
        NavigationParam parameter = new NavigationParam
        {
            ViewName = ViewDownloadManagerViewModel.Tag,
            ParentViewName = Tag,
            Parameter = null
        };
        EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
    }

    // 左侧tab点击事件
    private DelegateCommand<object> leftTabHeadersCommand;

    public DelegateCommand<object> LeftTabHeadersCommand => leftTabHeadersCommand ?? (leftTabHeadersCommand =
        new DelegateCommand<object>(ExecuteLeftTabHeadersCommand, CanExecuteLeftTabHeadersCommand));

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

        // tab点击后，隐藏MediaContent
        MediaContentVisibility = false;

        // 页面选择
        Pager = new CustomPagerViewModel(1, (int)Math.Ceiling(double.Parse(tabHeader.SubTitle) / VideoNumberInPage));
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
    private DelegateCommand<object> selectAllCommand;

    public DelegateCommand<object> SelectAllCommand =>
        selectAllCommand ?? (selectAllCommand = new DelegateCommand<object>(ExecuteSelectAllCommand));

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
        UpdateSelectedCount();
    }

    // 列表选择事件
    private DelegateCommand<object> mediasCommand;

    public DelegateCommand<object> MediasCommand =>
        mediasCommand ?? (mediasCommand = new DelegateCommand<object>(ExecuteMediasCommand));

    /// <summary>
    /// 列表选择事件
    /// </summary>
    /// <param name="parameter"></param>
    private void ExecuteMediasCommand(object parameter)
    {
        if (!(parameter is IList selectedMedia))
        {
            return;
        }

        if (selectedMedia.Count == Medias.Count)
        {
            IsSelectAll = true;
        }
        else
        {
            IsSelectAll = false;
        }
        UpdateSelectedCount();
    }

    // 添加选中项到下载列表事件
    private DelegateCommand addToDownloadCommand;

    public DelegateCommand AddToDownloadCommand => addToDownloadCommand ??
                                                   (addToDownloadCommand =
                                                       new DelegateCommand(ExecuteAddToDownloadCommand));

    /// <summary>
    /// 添加选中项到下载列表事件
    /// </summary>
    private void ExecuteAddToDownloadCommand()
    {
        AddToDownload(true);
    }

    // 添加所有视频到下载列表事件
    private DelegateCommand addAllToDownloadCommand;

    public DelegateCommand AddAllToDownloadCommand => addAllToDownloadCommand ??
                                                      (addAllToDownloadCommand =
                                                          new DelegateCommand(ExecuteAddAllToDownloadCommand));

    /// <summary>
    /// 添加所有视频到下载列表事件
    /// </summary>
    private void ExecuteAddAllToDownloadCommand()
    {
        AddToDownload(false);
    }

    // 加载收藏夹内所有视频事件
    private DelegateCommand loadAllVideoCommand;
    public DelegateCommand LoadAllVideoCommand => loadAllVideoCommand ??= new DelegateCommand(ExecuteLoadAllVideoCommand);
    /// <summary>
    /// 加载收藏夹内所有视频事件定义
    /// </summary>
    private void ExecuteLoadAllVideoCommand()
    {
        for (int i = 1; i <= Pager.Count; i++)
        {
            UpdateFavoritesMediaList(i, i > 1);
        }

        UpdateSelectedCount();
        Pager = new CustomPagerViewModel(1, 1);
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
            foreach (var media in list)
            {
                // 只下载选中项，跳过未选中项
                if (isOnlySelected && !media.IsSelected)
                {
                    continue;
                }

                /// 有分P的就下载全部

                // 开启服务
                var videoInfoService = new VideoInfoService(media.Bvid);

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
            EventAggregator.GetEvent<MessageEvent>().Publish($"{DictionaryResource.GetString("TipAddDownloadingFinished1")}{i}{DictionaryResource.GetString("TipAddDownloadingFinished2")}");
        }
    }

    private void OnCountChanged_Pager(int count)
    {
        UpdateSelectedCount();
    }

    private bool OnCurrentChanged_Pager(int old, int current)
    {
        if (!IsEnabled)
        {
            //Pager.Current = old;
            return false;
        }

        UpdateFavoritesMediaList(current);

        return true;
    }

    private async void UpdateFavoritesMediaList(int current, bool isLoadAll = false)
    {
        if (!isLoadAll)
        {
            Medias.Clear();
        }
        IsSelectAll = false;

        MediaLoadingVisibility = true;
        MediaNoDataVisibility = false;

        // 是否正在获取数据
        // 在所有的退出分支中都需要设为true
        IsEnabled = false;

        var tab = TabHeaders[SelectTabId];

        await Task.Run(new Action(() =>
        {
            CancellationToken cancellationToken = tokenSource2.Token;

            List<Core.BiliApi.Favorites.Models.FavoritesMedia> medias =
                FavoritesResource.GetFavoritesMedia(tab.Id, current, VideoNumberInPage);
            if (medias == null || medias.Count == 0)
            {
                MediaContentVisibility = true;
                MediaLoadingVisibility = false;
                MediaNoDataVisibility = true;
                return;
            }

            MediaContentVisibility = true;
            MediaLoadingVisibility = false;
            MediaNoDataVisibility = false;

            var service = new FavoritesService();
            service.GetFavoritesMediaList(medias, Medias, EventAggregator, cancellationToken);


        }), (tokenSource2 = new CancellationTokenSource()).Token);

        UpdateSelectedCount();
        IsEnabled = true;
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

        ContentVisibility = false;
        LoadingVisibility = true;
        NoDataVisibility = false;
        MediaLoadingVisibility = false;
        MediaNoDataVisibility = false;

        TabHeaders.Clear();
        Medias.Clear();
        SelectTabId = -1;
        IsSelectAll = false;
    }

    /// <summary>
    /// 导航到页面时执行
    /// </summary>
    /// <param name="navigationContext"></param>
    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 根据传入参数不同执行不同任务
        mid = navigationContext.Parameters.GetValue<long>("Parameter");
        if (mid == 0)
        {
            return;
        }

        InitView();

        await Task.Run(() =>
        {
            CancellationToken cancellationToken = tokenSource1.Token;

            var service = new FavoritesService();
            service.GetCreatedFavorites(mid, TabHeaders, cancellationToken);
            service.GetCollectedFavorites(mid, TabHeaders, cancellationToken);
        }, (tokenSource1 = new CancellationTokenSource()).Token);

        if (TabHeaders.Count == 0)
        {
            ContentVisibility = false;
            LoadingVisibility = false;
            NoDataVisibility = true;

            return;
        }

        ContentVisibility = true;
        LoadingVisibility = false;
        NoDataVisibility = false;

        // 初始选中项
        SelectTabId = 0;

        // 页面选择
        Pager = new CustomPagerViewModel(1, (int)Math.Ceiling(double.Parse(TabHeaders[SelectTabId].SubTitle) / VideoNumberInPage));
        Pager.CurrentChanged += OnCurrentChanged_Pager;
        Pager.CountChanged += OnCountChanged_Pager;
        Pager.Current = 1;
    }
}