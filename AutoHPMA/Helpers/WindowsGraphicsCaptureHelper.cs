using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using OpenCvSharp;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace AutoHPMA.Helpers;

public class WindowsGraphicsCapture : IDisposable
{
    private IntPtr _hWnd;
    private Direct3D11CaptureFramePool _captureFramePool = null!;
    private GraphicsCaptureItem _captureItem = null!;
    private GraphicsCaptureSession _captureSession = null!;
    private IDirect3DDevice _d3dDevice = null!;
    private bool _isCapturing;

    // 最新帧的存储
    private Mat? _latestFrame;
    private readonly ReaderWriterLockSlim _frameAccessLock = new();

    // 用于获取帧数据的临时纹理
    private Texture2D? _stagingTexture;
    private long _lastFrameTime = 0;

    public bool IsCapturing => _isCapturing;

    public void Start(IntPtr hWnd)
    {
        if (_isCapturing)
            return;

        _hWnd = hWnd;
        _isCapturing = true;

        // 创建捕获项
        _captureItem = CreateItemForWindow(_hWnd);
        if (_captureItem == null)
        {
            throw new InvalidOperationException("无法创建捕获项，请确保窗口有效且系统支持Windows.Graphics.Capture API");
        }

        // 创建D3D设备
        _d3dDevice = CreateDevice();

        // 创建帧池
        _captureFramePool = Direct3D11CaptureFramePool.Create(
            _d3dDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            _captureItem.Size);

        _captureItem.Closed += (sender, args) => Stop();
        _captureFramePool.FrameArrived += OnFrameArrived;

        // 创建捕获会话
        _captureSession = _captureFramePool.CreateCaptureSession(_captureItem);

        // 关闭鼠标捕获和边框显示
        if (ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled"))
        {
            _captureSession.IsCursorCaptureEnabled = false;
        }

        if (ApiInformation.IsWriteablePropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired"))
        {
            _captureSession.IsBorderRequired = false;
        }

        // 开始捕获
        _captureSession.StartCapture();
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame == null)
            return;

        // 限制处理帧率为66fps
        var now = GetTickCount64();
        if (now - _lastFrameTime < 15)
            return;
        _lastFrameTime = now;

        // 从捕获的帧创建一个可以被访问的纹理
        using var surfaceTexture = CreateSharpDXTexture2D(frame.Surface);
        var d3dDevice = surfaceTexture.Device;

        // 创建或重用暂存纹理
        _stagingTexture ??= CreateStagingTexture(frame, d3dDevice);
        var stagingTexture = _stagingTexture;

        // 将捕获的纹理复制到暂存纹理
        d3dDevice.ImmediateContext.CopyResource(surfaceTexture, stagingTexture);

        // 映射纹理以便CPU读取
        var dataBox = d3dDevice.ImmediateContext.MapSubresource(
            stagingTexture,
            0,
            MapMode.Read,
            MapFlags.None);

        try
        {
            // 创建一个新的Mat
            var newFrame = new Mat(stagingTexture.Description.Height, stagingTexture.Description.Width,
                MatType.CV_8UC4, dataBox.DataPointer);

            // 使用写锁更新最新帧
            _frameAccessLock.EnterWriteLock();
            try
            {
                // 释放之前的帧
                _latestFrame?.Dispose();
                // 克隆新帧以保持对其的引用（因为dataBox.DataPointer将被释放）
                _latestFrame = newFrame.Clone();
            }
            finally
            {
                _frameAccessLock.ExitWriteLock();
            }

            newFrame.Dispose();
        }
        finally
        {
            // 取消映射纹理
            d3dDevice.ImmediateContext.UnmapSubresource(stagingTexture, 0);
        }
    }

    public Mat? Capture()
    {
        // 使用读锁获取最新帧
        _frameAccessLock.EnterReadLock();
        try
        {
            // 如果没有可用帧则返回null
            if (_latestFrame == null)
                return null;

            // 返回最新帧的副本
            return _latestFrame.Clone();
        }
        finally
        {
            _frameAccessLock.ExitReadLock();
        }
    }

    public void Stop()
    {
        if (!_isCapturing)
            return;

        _captureSession?.Dispose();
        _captureFramePool?.Dispose();
        _captureItem = null!;
        _stagingTexture?.Dispose();
        _d3dDevice?.Dispose();

        _hWnd = IntPtr.Zero;
        _isCapturing = false;

        // 释放最新帧
        _frameAccessLock.EnterWriteLock();
        try
        {
            _latestFrame?.Dispose();
            _latestFrame = null;
        }
        finally
        {
            _frameAccessLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        Stop();
        _frameAccessLock.Dispose();
    }

    #region Helper Methods

    private Texture2D CreateStagingTexture(Direct3D11CaptureFrame frame, Device device)
    {
        // 创建可以用于CPU读取的暂存纹理
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = frame.ContentSize.Width,
            Height = frame.ContentSize.Height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };

        return new Texture2D(device, textureDesc);
    }

    #region Win32/WinRT Interop

    [DllImport("kernel32.dll")]
    private static extern long GetTickCount64();

    [System.Runtime.InteropServices.Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(
            [In] IntPtr window,
            [In] ref Guid iid);

        IntPtr CreateForMonitor(
            [In] IntPtr monitor,
            [In] ref Guid iid);
    }

    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    private static GraphicsCaptureItem CreateItemForWindow(IntPtr hWnd)
    {
        var factory = WinRT.WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
        var interop = (IGraphicsCaptureItemInterop)factory;
        var guid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        var itemPointer = interop.CreateForWindow(hWnd, ref guid);
        var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
        Marshal.Release(itemPointer);
        return item;
    }

    private static IDirect3DDevice CreateDevice()
    {
        var d3dDevice = new Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
        IDirect3DDevice device = null;

        // 获取DXGI设备
        using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device3>())
        {
            // 使用WinRT互操作包装本机设备
            int hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);

            if (hr == 0)
            {
                device = WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);
                Marshal.Release(pUnknown);
            }
            else
            {
                throw new Exception($"创建Direct3D设备失败，错误码: {hr}");
            }
        }

        return device;
    }

    private static Texture2D CreateSharpDXTexture2D(IDirect3DSurface surface)
    {
        IDirect3DDxgiInterfaceAccess dxgiInterfaceAccess = WinRT.CastExtensions.As<IDirect3DDxgiInterfaceAccess>(surface);
        IntPtr pResource = dxgiInterfaceAccess.GetInterface(new Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d")); // IID_IDXGIResource

        using var dxgiResource = new SharpDX.DXGI.Resource(pResource);
        IntPtr texture2DPtr = dxgiResource.SharedHandle;

        using var dxgiFactory = new SharpDX.DXGI.Factory2();
        using var adapter = dxgiFactory.GetAdapter1(0);
        using var device = new Device(adapter);

        return device.OpenSharedResource<Texture2D>(texture2DPtr);
    }

    [System.Runtime.InteropServices.Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }

    #endregion

    #endregion
}