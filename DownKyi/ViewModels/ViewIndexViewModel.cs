using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DownKyi.Core.BiliApi.Users;
using DownKyi.Core.BiliApi.Users.Models;
using DownKyi.Core.Logging;
using DownKyi.Core.Settings;
using DownKyi.Core.Settings.Models;
using DownKyi.Core.Storage;
using DownKyi.Images;
using DownKyi.Services;
using DownKyi.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.ViewModels;

public class ViewIndexViewModel : ViewModelBase
{
    public const string Tag = "PageIndex";

    private bool _loginPanelVisibility;

    public bool LoginPanelVisibility
    {
        get => _loginPanelVisibility;
        set => SetProperty(ref _loginPanelVisibility, value);
    }

    private string? _userName;

    public string? UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    private Bitmap _header;

    public Bitmap Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }


    private VectorImage textLogo;

    public VectorImage TextLogo
    {
        get => textLogo;
        set => SetProperty(ref textLogo, value);
    }

    private string _inputText;

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    private VectorImage _generalSearch;

    public VectorImage GeneralSearch
    {
        get => _generalSearch;
        set => SetProperty(ref _generalSearch, value);
    }

    private VectorImage _settings;

    public VectorImage Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    private VectorImage _downloadManager;

    public VectorImage DownloadManager
    {
        get => _downloadManager;
        set => SetProperty(ref _downloadManager, value);
    }

    private VectorImage _toolbox;

    public VectorImage Toolbox
    {
        get => _toolbox;
        set => SetProperty(ref _toolbox, value);
    }


    public ViewIndexViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
    {
        _loginPanelVisibility = true;
        Header = ImageHelper.LoadFromResource(new Uri("avares://DownKyi/Resources/default_header.jpg"));

        TextLogo = LogoIcon.Instance().TextLogo;
        TextLogo.Fill = DictionaryResource.GetColor("ColorPrimary");

        GeneralSearch = ButtonIcon.Instance().GeneralSearch;
        GeneralSearch.Fill = DictionaryResource.GetColor("ColorPrimary");

        Settings = ButtonIcon.Instance().Settings;
        Settings.Fill = DictionaryResource.GetColor("ColorPrimary");

        DownloadManager = ButtonIcon.Instance().DownloadManage;
        DownloadManager.Fill = DictionaryResource.GetColor("ColorPrimary");

        Toolbox = ButtonIcon.Instance().Toolbox;
        Toolbox.Fill = DictionaryResource.GetColor("ColorPrimary");

        UpdateUserInfo();
    }

    // 输入确认事件
    public DelegateCommand<object> inputCommand;
    public DelegateCommand<object> InputCommand => inputCommand ?? (inputCommand = new DelegateCommand<object>(ExecuteInput));

    /// <summary>
    /// 处理输入事件
    /// </summary>
    private void ExecuteInput(object param)
    {
        EnterBili();
    }


    // 登录事件
    private DelegateCommand? _loginCommand;
    public DelegateCommand LoginCommand => _loginCommand ??= new DelegateCommand(ExecuteLogin);

    /// <summary>
    /// 进入登录页面
    /// </summary>
    private void ExecuteLogin()
    {
        if (UserName == null)
        {
            NavigateToView.NavigationView(EventAggregator, ViewLoginViewModel.Tag, Tag, null);
        }
        else
        {
            // 进入用户空间
            var userInfo = SettingsManager.GetInstance().GetUserInfo();
            if (userInfo != null && userInfo.Mid != -1)
            {
                NavigateToView.NavigationView(EventAggregator, ViewMySpaceViewModel.Tag, Tag, userInfo.Mid);
            }
        }
    }

    // 进入设置页面
    private DelegateCommand settingsCommand;

    public DelegateCommand SettingsCommand =>
        settingsCommand ?? (settingsCommand = new DelegateCommand(ExecuteSettingsCommand));

    /// <summary>
    /// 进入设置页面
    /// </summary>
    private void ExecuteSettingsCommand()
    {
        NavigateToView.NavigationView(EventAggregator, ViewSettingsViewModel.Tag, Tag, null);
    }

    // 进入下载管理页面
    private DelegateCommand downloadManagerCommand;

    public DelegateCommand DownloadManagerCommand => downloadManagerCommand ??
                                                     (downloadManagerCommand =
                                                         new DelegateCommand(ExecuteDownloadManagerCommand));

    /// <summary>
    /// 进入下载管理页面
    /// </summary>
    private void ExecuteDownloadManagerCommand()
    {
        NavigateToView.NavigationView(EventAggregator, ViewDownloadManagerViewModel.Tag, Tag, null);
    }

// 进入工具箱页面
    private DelegateCommand toolboxCommand;

    public DelegateCommand ToolboxCommand =>
        toolboxCommand ?? (toolboxCommand = new DelegateCommand(ExecuteToolboxCommand));

    /// <summary>
    /// 进入工具箱页面
    /// </summary>
    private async void ExecuteToolboxCommand()
    {
        NavigateToView.NavigationView(EventAggregator, ViewToolboxViewModel.Tag, Tag, null);
    }


    /// <summary>
    /// 进入B站链接的处理逻辑，
    /// 只负责处理输入，并跳转到视频详情页。<para/>
    /// 不是支持的格式，则进入搜索页面。
    /// </summary>
    private void EnterBili()
    {
        if (InputText == null || InputText == string.Empty)
        {
            return;
        }

        LogManager.Debug(Tag, $"InputText: {InputText}");
        InputText = Regex.Replace(InputText, @"[【]*[^【]*[^】]*[】 ]", "");
        SearchService searchService = new SearchService();
        bool isSupport = searchService.BiliInput(InputText, Tag, EventAggregator);
        if (!isSupport)
        {
            // 关键词搜索
            searchService.SearchKey(InputText, Tag, EventAggregator);
        }

        InputText = string.Empty;
    }


    private async Task<UserInfoForNavigation> GetUserInfo()
    {
        UserInfoForNavigation userInfo = null;
        await Task.Run(() =>
        {
            // 获取用户信息
            userInfo = UserInfo.GetUserInfoForNavigation();
            if (userInfo != null)
            {
                SettingsManager.GetInstance().SetUserInfo(new UserInfoSettings
                {
                    Mid = userInfo.Mid,
                    Name = userInfo.Name,
                    IsLogin = userInfo.IsLogin,
                    IsVip = userInfo.VipStatus == 1,
                    ImgKey = userInfo.Wbi.ImgUrl.Split('/').ToList().Last().Split('.')[0],
                    SubKey = userInfo.Wbi.SubUrl.Split('/').ToList().Last().Split('.')[0],
                });
            }
            else
            {
                SettingsManager.GetInstance().SetUserInfo(new UserInfoSettings
                {
                    Mid = -1,
                    Name = "",
                    IsLogin = false,
                    IsVip = false,
                });
            }
        });
        return userInfo;
    }

    /// <summary>
    /// 更新用户登录信息
    /// </summary>
    private async void UpdateUserInfo(bool isBackgroud = false)
    {
        try
        {
            if (isBackgroud)
            {
                // 获取用户信息
                await GetUserInfo();
                return;
            }

            LoginPanelVisibility = false;

            // 获取用户信息
            var userInfo = await GetUserInfo();

            // 检查本地是否存在login文件，没有则说明未登录
            if (!File.Exists(StorageManager.GetLogin()))
            {
                LoginPanelVisibility = true;
                Header = ImageHelper.LoadFromResource(new Uri("avares://DownKyi/Resources/default_header.jpg"));
                UserName = null;
                return;
            }

            LoginPanelVisibility = true;

            if (userInfo != null)
            {
                if (userInfo.Face != null)
                {
                    Header = new StorageHeader().GetHeaderThumbnail(userInfo.Mid, userInfo.Name, userInfo.Face, 36, 36);
                }
                else
                {
                    Header = ImageHelper.LoadFromResource(new Uri("avares://DownKyi/Resources/default_header.jpg"));
                }

                UserName = userInfo.Name;
            }
            else
            {
                Header = ImageHelper.LoadFromResource(new Uri("avares://DownKyi/Resources/default_header.jpg"));
                UserName = null;
            }
        }
        catch (Exception e)
        {
            Console.PrintLine("UpdateUserInfo()发生异常: {0}", e);
            LogManager.Error(Tag, e);
        }
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        DownloadManager = ButtonIcon.Instance().DownloadManage;
        DownloadManager.Height = 27;
        DownloadManager.Width = 32;
        DownloadManager.Fill = DictionaryResource.GetColor("ColorPrimary");

        // 根据传入参数不同执行不同任务
        string parameter = navigationContext.Parameters.GetValue<string>("Parameter");
        if (parameter == null)
        {
            // 其他情况只更新设置的用户信息，不更新UI
            UpdateUserInfo(true);
            return;
        }

        // 启动
        if (parameter == "start")
        {
            UpdateUserInfo();
        }
        // 从登录页面返回
        else if (parameter == "login")
        {
            UpdateUserInfo();
        }
        // 注销
        else if (parameter == "logout")
        {
            UpdateUserInfo();
        }
        else
        {
            // 其他情况只更新设置的用户信息，不更新UI
            UpdateUserInfo(true);
        }
    }
}