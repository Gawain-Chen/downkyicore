﻿using System.Collections.Generic;
using DownKyi.Events;
using DownKyi.Images;
using DownKyi.Utils;
using DownKyi.ViewModels.PageViewModels;
using DownKyi.ViewModels.Toolbox;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace DownKyi.ViewModels
{
    public class ViewToolboxViewModel : ViewModelBase
    {
        public const string Tag = "PageToolbox";

        private readonly IRegionManager regionManager;

        #region 页面属性申明

        private VectorImage arrowBack;

        public VectorImage ArrowBack
        {
            get => arrowBack;
            set => SetProperty(ref arrowBack, value);
        }

        private List<TabHeader> tabHeaders;

        public List<TabHeader> TabHeaders
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

        #endregion

        public ViewToolboxViewModel(IRegionManager regionManager, IEventAggregator eventAggregator) : base(
            eventAggregator)
        {
            this.regionManager = regionManager;

            #region 属性初始化

            ArrowBack = NavigationIcon.Instance().ArrowBack;
            ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");

            TabHeaders = new List<TabHeader>
            {
                new() { Id = 0, Title = DictionaryResource.GetString("BiliHelper") },
                new() { Id = 1, Title = DictionaryResource.GetString("Delogo") },
                new() { Id = 2, Title = DictionaryResource.GetString("ExtractMedia") }
            };

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
            NavigationParam parameter = new NavigationParam
            {
                ViewName = ParentView,
                ParentViewName = null,
                Parameter = null
            };
            EventAggregator.GetEvent<NavigationEvent>().Publish(parameter);
        }

        // 左侧tab点击事件
        private DelegateCommand<object> leftTabHeadersCommand;

        public DelegateCommand<object> LeftTabHeadersCommand => leftTabHeadersCommand ??
                                                                (leftTabHeadersCommand =
                                                                    new DelegateCommand<object>(
                                                                        ExecuteLeftTabHeadersCommand));

        /// <summary>
        /// 左侧tab点击事件
        /// </summary>
        /// <param name="parameter"></param>
        private void ExecuteLeftTabHeadersCommand(object parameter)
        {
            if (!(parameter is TabHeader tabHeader))
            {
                return;
            }

            NavigationParameters param = new NavigationParameters();

            switch (tabHeader.Id)
            {
                case 0:
                    regionManager.RequestNavigate("ToolboxContentRegion", ViewBiliHelperViewModel.Tag, param);
                    break;
                case 1:
                    regionManager.RequestNavigate("ToolboxContentRegion", ViewDelogoViewModel.Tag, param);
                    break;
                case 2:
                    regionManager.RequestNavigate("ToolboxContentRegion", ViewExtractMediaViewModel.Tag, param);
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 导航到页面时执行
        /// </summary>
        /// <param name="navigationContext"></param>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 进入设置页面时显示的设置项
            SelectTabId = 0;
            PropertyChangeAsync(() =>
            {
                regionManager.RequestNavigate("ToolboxContentRegion", ViewBiliHelperViewModel.Tag,
                    new NavigationParameters());
            });

            ArrowBack.Fill = DictionaryResource.GetColor("ColorTextDark");
        }
    }
}