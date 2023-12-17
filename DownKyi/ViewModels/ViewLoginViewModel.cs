using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DownKyi.Core.BiliApi.Login;
using DownKyi.Core.Logging;
using DownKyi.Events;
using DownKyi.Images;
using DownKyi.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Console = DownKyi.Core.Utils.Debugging.Console;

namespace DownKyi.ViewModels;

public class ViewLoginViewModel : ViewModelBase
{
    public const string Tag = "PageLogin";

    private CancellationTokenSource _tokenSource;

    #region 页面属性申明

    private VectorImage _arrowBack;

    public VectorImage ArrowBack
    {
        get => _arrowBack;
        set => SetProperty(ref _arrowBack, value);
    }

    private Bitmap? _loginQrCode;

    public Bitmap? LoginQrCode
    {
        get => _loginQrCode;
        set => SetProperty(ref _loginQrCode, value);
    }

    private double _loginQrCodeOpacity;

    public double LoginQrCodeOpacity
    {
        get => _loginQrCodeOpacity;
        set => SetProperty(ref _loginQrCodeOpacity, value);
    }

    private bool _loginQrCodeStatus;

    public bool LoginQrCodeStatus
    {
        get => _loginQrCodeStatus;
        set => SetProperty(ref _loginQrCodeStatus, value);
    }

    #endregion

    public ViewLoginViewModel(IEventAggregator eventAggregator) : base(eventAggregator)
    {
        #region 属性初始化

        ArrowBack = NavigationIcon.Instance().ArrowBack;
        ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

        #endregion
    }

    private DelegateCommand? _backSpaceCommand;

    public DelegateCommand BackSpaceCommand =>
        _backSpaceCommand ??= new DelegateCommand(ExecuteBackSpace);

    private void ExecuteBackSpace()
    {
        // 初始化状态
        InitStatus();

        // 结束任务
        _tokenSource.Cancel();
        NavigationParam parameter = new NavigationParam
        {
            ViewName = ParentView,
            ParentViewName = null,
            Parameter = "login"
        };
        EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
    }

    /// <summary>
    /// 登录
    /// </summary>
    private void Login()
    {
        try
        {
            var loginUrl = LoginQR.GetLoginUrl();
            if (loginUrl == null)
            {
                return;
            }

            if (loginUrl.Code != 0)
            {
                ExecuteBackSpace();
                return;
            }

            if (loginUrl.Data == null || loginUrl.Data.Url == null)
            {
                EventAggregator.GetEvent<MessageEvent>().Publish(DictionaryResource.GetString("GetLoginUrlFailed"));
                return;
            }

            PropertyChangeAsync(() => { LoginQrCode = LoginQR.GetLoginQRCode(loginUrl.Data.Url); });
            Console.PrintLine(loginUrl.Data.Url + "\n");
            LogManager.Debug(Tag, loginUrl.Data.Url);

            GetLoginStatus(loginUrl.Data.QrcodeKey);
        }
        catch (Exception e)
        {
            Console.PrintLine("Login()发生异常: {0}", e);
            LogManager.Error(Tag, e);
        }
    }

    /// <summary>
    /// 循环查询登录状态
    /// </summary>
    /// <param name="oauthKey"></param>
    private void GetLoginStatus(string oauthKey)
    {
        CancellationToken cancellationToken = _tokenSource.Token;
        while (true)
        {
            Thread.Sleep(1000);
            var loginStatus = LoginQR.GetLoginStatus(oauthKey);
            if (loginStatus == null)
            {
                continue;
            }

            Console.PrintLine(loginStatus.Data.Code + "\n" + loginStatus.Data.Message + "\n" +
                              loginStatus.Data.Url + "\n");

            switch (loginStatus.Data.Code)
            {
                // case -1:
                //     // 没有这个oauthKey
                //
                //     // 发送通知
                //     eventAggregator.GetEvent<MessageEvent>().Publish(DictionaryResource.GetString("LoginKeyError"));
                //     LogManager.Info(Tag, DictionaryResource.GetString("LoginKeyError"));
                //
                //     // 取消任务
                //     tokenSource.Cancel();
                //
                //     // 创建新任务
                //     PropertyChangeAsync(new Action(() => { Task.Run(Login, (tokenSource = new CancellationTokenSource()).Token); }));
                //     break;
                case 86038:
                    // 不匹配的oauthKey，超时或已确认的oauthKey

                    // 发送通知
                    EventAggregator.GetEvent<MessageEvent>().Publish(DictionaryResource.GetString("LoginTimeOut"));
                    LogManager.Info(Tag, DictionaryResource.GetString("LoginTimeOut"));

                    // 取消任务
                    _tokenSource.Cancel();

                    // 创建新任务
                    PropertyChangeAsync(() =>
                    {
                        Task.Run(Login, (_tokenSource = new CancellationTokenSource()).Token);
                    });
                    break;
                case 86101:
                    // 未扫码
                    break;
                case 86090:
                    // 已扫码，未确认
                    PropertyChangeAsync(() =>
                    {
                        LoginQrCodeStatus = true;
                        LoginQrCodeOpacity = 0.3;
                    });
                    break;
                case 0:
                    // 确认登录

                    // 发送通知
                    EventAggregator.GetEvent<MessageEvent>().Publish("登陆成功");
                    // LogManager.Info(Tag, DictionaryResource.GetString("LoginSuccessful"));

                    // 保存登录信息
                    try
                    {
                        bool isSucceed = LoginHelper.SaveLoginInfoCookies(loginStatus.Data.Url);
                        if (!isSucceed)
                        {
                            EventAggregator.GetEvent<MessageEvent>()
                                .Publish(DictionaryResource.GetString("LoginFailed"));
                            LogManager.Error(Tag, DictionaryResource.GetString("LoginFailed"));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.PrintLine("PageLogin 保存登录信息发生异常: {0}", e);
                        LogManager.Error(e);
                        EventAggregator.GetEvent<MessageEvent>().Publish(DictionaryResource.GetString("LoginFailed"));
                    }

                    // TODO 其他操作


                    // 取消任务
                    Thread.Sleep(3000);
                    PropertyChange(ExecuteBackSpace);
                    break;
            }

            // 判断是否该结束线程，若为true，跳出while循环
            if (cancellationToken.IsCancellationRequested)
            {
                Console.PrintLine("停止Login线程，跳出while循环");
                LogManager.Debug(Tag, "登录操作结束");
                break;
            }
        }
    }


    /// <summary>
    /// 初始化状态
    /// </summary>
    private void InitStatus()
    {
        ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

        LoginQrCode = null;
        LoginQrCodeOpacity = 1;
        LoginQrCodeStatus = false;
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        InitStatus();

        Task.Run(Login, (_tokenSource = new CancellationTokenSource()).Token);
    }
}