using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using OpenCvSharp;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Windows.Foundation; // 添加此命名空间
using System.Reflection;   // 添加此命名空间

namespace AutoHPMA.GameTask;

public class WindowsGraphicsCaptureHelper : IDisposable
{
    private IDirect3DDevice _d3dDevice;
    private GraphicsCaptureItem _captureItem;
    private Direct3D11CaptureFramePool _captureFramePool;
    private GraphicsCaptureSession _captureSession;
    private Texture2D _stagingTexture;
    private Mat _capturedFrame;
    private bool _frameReady;
    private readonly AutoResetEvent _frameEvent = new AutoResetEvent(false);
    private TypedEventHandler<Direct3D11CaptureFramePool, object> _frameArrivedHandler;

    #region Interop

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
    }

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
    static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

    static readonly Guid IID_ID3D11Device = new Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140");
    static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    #endregion

    public WindowsGraphicsCaptureHelper()
    {
        CreateD3DDevice();
    }

    private void CreateD3DDevice()
    {
        // 创建D3D设备
        var d3dDevice = new Device(SharpDX.Direct3D.DriverType.Hardware,
                                  DeviceCreationFlags.BgraSupport);

        // 获取DXGI设备
        using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device3>())
        {
            uint hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);
            if (hr == 0)
            {
                _d3dDevice = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                Marshal.Release(pUnknown);
            }
            else
            {
                throw new Exception("创建D3D设备失败");
            }
        }
    }

    private GraphicsCaptureItem CreateCaptureItemForWindow(IntPtr hwnd)
    {
        var factory = Marshal.GetTypeFromCLSID(new Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")) as IGraphicsCaptureItemInterop;

        Guid graphicsCaptureItemGuid = typeof(GraphicsCaptureItem).GUID;
        IntPtr itemPointer = factory.CreateForWindow(hwnd, ref graphicsCaptureItemGuid);

        GraphicsCaptureItem item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
        Marshal.Release(itemPointer);

        return item;
    }

    private Texture2D CreateStagingTexture(GraphicsCaptureItem item, Device device)
    {
        // 创建可被CPU读取的暂存纹理
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = item.Size.Width,
            Height = item.Size.Height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };

        return new Texture2D(device, textureDesc);
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using (var frame = sender.TryGetNextFrame())
        {
            if (frame == null) return;

            // 获取D3D设备
            var access = _d3dDevice as IDirect3DDxgiInterfaceAccess;
            IntPtr d3dPointer = access.GetInterface(IID_ID3D11Device);
            using (var d3dDevice = new Device(d3dPointer))
            {
                // 获取帧纹理
                access = frame.Surface as IDirect3DDxgiInterfaceAccess;
                IntPtr texturePointer = access.GetInterface(IID_ID3D11Texture2D);
                using (var surfaceTexture = new Texture2D(texturePointer))
                {
                    // 创建或获取暂存纹理
                    if (_stagingTexture == null)
                    {
                        _stagingTexture = CreateStagingTexture(_captureItem, d3dDevice);
                    }

                    // 复制纹理
                    d3dDevice.ImmediateContext.CopyResource(surfaceTexture, _stagingTexture);

                    // 映射纹理以便CPU读取
                    var dataBox = d3dDevice.ImmediateContext.MapSubresource(
                        _stagingTexture,
                        0,
                        MapMode.Read,
                        MapFlags.None);

                    try
                    {
                        // 创建OpenCV的Mat
                        if (_capturedFrame != null)
                        {
                            _capturedFrame.Dispose();
                        }

                        _capturedFrame = new Mat(_stagingTexture.Description.Height,
                                               _stagingTexture.Description.Width,
                                               MatType.CV_8UC4,
                                               dataBox.DataPointer);

                        // 克隆Mat以保持对数据的引用
                        _capturedFrame = _capturedFrame.Clone();

                        // 通知外部可以获取帧了
                        _frameReady = true;
                        _frameEvent.Set();
                    }
                    finally
                    {
                        // 解除映射
                        d3dDevice.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 捕获窗口并返回图像
    /// </summary>
    /// <param name="hwnd">窗口句柄</param>
    /// <param name="timeoutMs">超时时间(毫秒)</param>
    /// <returns>捕获的图像，如果超时返回null</returns>
    public Mat CaptureWindow(IntPtr hwnd, int timeoutMs = 1000)
    {
        try
        {
            // 如果已有捕获会话，先清理
            Cleanup();

            // 重置状态
            _frameReady = false;

            // 创建捕获项
            _captureItem = CreateCaptureItemForWindow(hwnd);
            if (_captureItem == null)
            {
                throw new InvalidOperationException("无法创建窗口的捕获项");
            }

            // 创建帧池
            _captureFramePool = Direct3D11CaptureFramePool.Create(
                _d3dDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                _captureItem.Size);

            // 注册帧到达事件
            // 注册帧到达事件（通过反射）
            _frameArrivedHandler = new TypedEventHandler<Direct3D11CaptureFramePool, object>(OnFrameArrived);
            var addMethod = typeof(Direct3D11CaptureFramePool).GetRuntimeMethod(
                "add_FrameArrived",
                new[] { typeof(TypedEventHandler<Direct3D11CaptureFramePool, object>) });
            addMethod.Invoke(_captureFramePool, new object[] { _frameArrivedHandler });

            // 创建捕获会话
            _captureSession = _captureFramePool.CreateCaptureSession(_captureItem);

            // 设置不捕获鼠标
            if (Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent(
                "Windows.Graphics.Capture.GraphicsCaptureSession", "IsCursorCaptureEnabled"))
            {
                _captureSession.IsCursorCaptureEnabled = false;
            }

            // 设置不需要边框
            if (Windows.Foundation.Metadata.ApiInformation.IsWriteablePropertyPresent(
                "Windows.Graphics.Capture.GraphicsCaptureSession", "IsBorderRequired"))
            {
                //_captureSession.IsBorderRequired = false;
            }

            // 开始捕获
            _captureSession.StartCapture();

            // 等待帧到达或超时
            if (_frameEvent.WaitOne(timeoutMs) && _frameReady)
            {
                // 返回捕获的帧
                return _capturedFrame.Clone();
            }
            else
            {
                return null; // 超时或未获取到帧
            }
        }
        finally
        {
            // 无论成功与否，都清理资源
            Cleanup();
        }
    }

    private void Cleanup()
    {
        // 停止捕获会话
        _captureSession?.Dispose();
        _captureSession = null;

        // 清理帧池
        if (_captureFramePool != null)
        {
            if (_captureFramePool != null && _frameArrivedHandler != null)
            {
                var removeMethod = typeof(Direct3D11CaptureFramePool).GetRuntimeMethod(
                    "remove_FrameArrived",
                    new[] { typeof(TypedEventHandler<Direct3D11CaptureFramePool, object>) });
                removeMethod.Invoke(_captureFramePool, new object[] { _frameArrivedHandler });
                _frameArrivedHandler = null;
            }
            _captureFramePool.Dispose();
            _captureFramePool = null;
        }

        // 清理捕获项
        _captureItem = null;

        // 清理暂存纹理
        _stagingTexture?.Dispose();
        _stagingTexture = null;
    }

    public void Dispose()
    {
        Cleanup();
        _capturedFrame?.Dispose();
        _d3dDevice?.Dispose();
        _frameEvent?.Dispose();
    }
}