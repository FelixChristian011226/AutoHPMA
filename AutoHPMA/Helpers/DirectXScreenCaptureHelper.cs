using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AutoHPMA.Helpers
{
    public class DirectXScreenCaptureHelper
    {
        public static Bitmap CaptureScreen()
        {
            // 创建工厂和设备
            var factory = new Factory1();
            var adapter = new SharpDX.DXGI.Factory1().GetAdapter1(0);
            var device = new SharpDX.Direct3D11.Device(adapter);
            var output = adapter.GetOutput(0);
            var output1 = output.QueryInterface<Output1>();

            // 获取桌面分辨率
            var bounds = output.Description.DesktopBounds;
            int width = bounds.Right - bounds.Left;
            int height = bounds.Bottom - bounds.Top;

            // 创建纹理2D
            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            };

            var screenTexture = new Texture2D(device, textureDesc);

            // 复制屏幕内容到纹理
            using (var duplication = output1.DuplicateOutput(device))
            {
                OutputDuplicateFrameInformation frameInfo;
                SharpDX.DXGI.Resource screenResource;

                duplication.AcquireNextFrame(500, out frameInfo, out screenResource);

                using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                {
                    device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                }
                duplication.ReleaseFrame();
            }

            // 从纹理获取位图
            var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var boundsRect = new Rectangle(0, 0, width, height);
            var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            var sourcePtr = mapSource.DataPointer;
            var destPtr = mapDest.Scan0;

            for (int y = 0; y < height; y++)
            {
                // 拷贝每行的像素
                Utilities.CopyMemory(destPtr, sourcePtr, width * 4);
                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                destPtr = IntPtr.Add(destPtr, mapDest.Stride);
            }

            bitmap.UnlockBits(mapDest);
            device.ImmediateContext.UnmapSubresource(screenTexture, 0);

            // 清理资源
            screenTexture.Dispose();
            device.Dispose();
            adapter.Dispose();
            factory.Dispose();

            return bitmap;
        }
    }
}