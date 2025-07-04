﻿using Microsoft.Toolkit.Uwp.Notifications;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoHPMA.Config;
using AutoHPMA.Helpers.DataHelper;

namespace AutoHPMA.Helpers;

public class ToastNotificationHelper
{
    public static void ShowToast(string title, string content, string launchArgs = "")
    {
        var settings = AppSettings.Load();
        if (!settings.NotificationEnabled)
            return;

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logoPath = Path.Combine(baseDir, "Assets", "logo.png");

            new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .AddAppLogoOverride(new Uri(logoPath), ToastGenericAppLogoCrop.Circle)
                .AddArgument("action", "viewImage")
                .AddArgument("launchArgs", launchArgs)
                .Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送通知失败: {ex.Message}");
        }
    }

    public static async void ShowToastWithImage(string title, string content, Mat image, string launchArgs = "")
    {
        var settings = AppSettings.Load();
        if (!settings.NotificationEnabled)
            return;

        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string logoPath = Path.Combine(baseDir, "Assets", "logo.png");
            string imageDir = CacheHelper.GetSubCacheDir("Images");
            string tempImagePath = Path.Combine(imageDir, $"toast_{Guid.NewGuid()}.png");

            image.SaveImage(tempImagePath);

            new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .AddInlineImage(new Uri(tempImagePath))
                .AddAppLogoOverride(new Uri(logoPath), ToastGenericAppLogoCrop.Circle)
                .AddArgument("action", "viewImage")
                .AddArgument("launchArgs", launchArgs)
                .Show();

            // 延时清理该图像文件
            await Task.Delay(5000);
            if (File.Exists(tempImagePath))
                File.Delete(tempImagePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送通知失败: {ex.Message}");
        }
    }
}
