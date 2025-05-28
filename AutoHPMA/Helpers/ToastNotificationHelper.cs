using Microsoft.Toolkit.Uwp.Notifications;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoHPMA.Helpers;

public class ToastNotificationHelper
{
    public static async void ShowToastWithImage(string title, string content, Mat image, string launchArgs = "")
    {
        try
        {
            string imageDir = CacheHelper.GetSubCacheDir("Images");
            string tempImagePath = Path.Combine(imageDir, $"toast_{Guid.NewGuid()}.png");

            image.SaveImage(tempImagePath);

            new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .AddInlineImage(new Uri(tempImagePath))
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
