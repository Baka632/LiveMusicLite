﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace LiveMusicLite
{
    /// <summary>
    /// 为应用程序的设置提供属性
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// 访问本地设置的实例
        /// </summary>
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ApplicationDataCompositeValue SuspendedData = new ApplicationDataCompositeValue();
        /// <summary>
        /// 主题设置的值
        /// </summary>
        string _ThemeSettings;
        /// <summary>
        /// 音量大小的值
        /// </summary>
        double _MusicVolume;
        /// <summary>
        /// 指示是否在启动时加载音乐列表的值
        /// </summary>
        bool _IsLoadMusicOnStartUp;
        /// <summary>
        /// 指示在手动打开音乐时选择何种操作的值
        /// </summary>
        bool _MediaOpenOperation;

        /// <summary>
        /// 初始化Settings类的新实例
        /// </summary>
        public Settings()
        {
            switch ((string)localSettings.Values["ThemeSetting"])
            {
                case null:
                    _ThemeSettings = "Default";
                    localSettings.Values["ThemeSetting"] = "Default";
                    break;
                default:
                    _ThemeSettings = (string)localSettings.Values["ThemeSetting"];
                    break;
            }
            switch (localSettings.Values["MusicVolume"])
            {
                case null:
                    _MusicVolume = 1d;
                    localSettings.Values["MusicVolume"] = 1d;
                    break;
                default:
                    _MusicVolume = (double)localSettings.Values["MusicVolume"];
                    break;
            }
            switch (localSettings.Values["IsLoadMusicOnStartUp"])
            {
                case null:
                    _IsLoadMusicOnStartUp = false;
                    localSettings.Values["IsLoadMusicOnStartUp"] = false;
                    break;
                default:
                    _IsLoadMusicOnStartUp = (bool)localSettings.Values["IsLoadMusicOnStartUp"];
                    break;
            }
            switch (localSettings.Values["MediaOpenOperation"])
            {
                case null:
                    _MediaOpenOperation = true;
                    localSettings.Values["MediaOpenOperation"] = true;
                    break;
                default:
                    _MediaOpenOperation = (bool)localSettings.Values["MediaOpenOperation"];
                    break;
            }
        }

        /// <summary>
        /// 设置中主题设置的属性
        /// </summary>
        public string ThemeSettings
        {
            get => _ThemeSettings;
            set
            {
                _ThemeSettings = value;
                localSettings.Values["ThemeSetting"] = value;
            }
        }

        /// <summary>
        /// 设置中音乐大小的属性
        /// </summary>
        public double MusicVolume
        {
            get => _MusicVolume;
            set
            {
                _MusicVolume = value;
                localSettings.Values["MusicVolume"] = value;
            }
        }

        /// <summary>
        /// 设置中是否加载音乐列表的设置的属性
        /// </summary>
        public bool IsLoadMusicOnStartUp
        {
            get => _IsLoadMusicOnStartUp;
            set
            {
                _IsLoadMusicOnStartUp = value;
                localSettings.Values["IsLoadMusicOnStartUp"] = value;
            }
        }
        
        /// <summary>
        /// 设置中手动打开音乐文件操作的属性
        /// </summary>
        public bool MediaOpenOperation
        {
            get => _MediaOpenOperation;
            set
            {
                _MediaOpenOperation = value;
                localSettings.Values["MediaOpenOperation"] = value;
            }
        }
    }
}
