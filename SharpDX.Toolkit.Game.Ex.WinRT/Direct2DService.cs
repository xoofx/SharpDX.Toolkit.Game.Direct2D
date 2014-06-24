using System;
using Windows.Graphics.Display;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Toolkit.Graphics;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct2D1.Device;
using Factory1 = SharpDX.DirectWrite.Factory1;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace SharpDX.Toolkit
{
    /// <summary>
    ///     Provides a service that offers Direct2D and DirectWrite contexts.
    /// </summary>
    public sealed class Direct2DService : Component, IDirect2DService
    {
        #region Private constants

        private const DebugLevel D2DDebugLevel = DebugLevel.Information;

        #endregion

        #region Private fields

        private readonly IGraphicsDeviceService _graphicsDeviceService;
        private GraphicsDevice graphicsDeviceCopy;
        private Device _device;
        private DeviceContext _deviceContext;
        private Factory1 _directWriteFactory;
        private Bitmap1 _target;

        #endregion

        #region Public constants

        /// <summary>
        ///     Initializes a new instance of <see cref="Direct2DService" />, subscribes to <see cref="GraphicsDevice" /> changes
        ///     events via
        ///     <see cref="IGraphicsDeviceService" />.
        /// </summary>
        /// <param name="graphicsDeviceService">The service responsible for <see cref="GraphicsDevice" /> management.</param>
        /// <exception cref="ArgumentNullException">Then either <paramref name="graphicsDeviceService" /> is null.</exception>
        public Direct2DService(IGraphicsDeviceService graphicsDeviceService)
        {
            if (graphicsDeviceService == null) throw new ArgumentNullException("graphicsDeviceService");
            _graphicsDeviceService = graphicsDeviceService;

            graphicsDeviceService.DeviceCreated += GraphicsDeviceServiceOnDeviceCreated;
            graphicsDeviceService.DeviceDisposing += GraphicsDeviceServiceOnDeviceDisposing;
            graphicsDeviceService.DeviceChangeBegin += GraphicsDeviceServiceOnDeviceChangeBegin;
            graphicsDeviceService.DeviceChangeEnd += GraphicsDeviceServiceOnDeviceChangeEnd;
            graphicsDeviceService.DeviceLost += GraphicsDeviceServiceOnDeviceLost;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Gets a reference to the Direct2D device.
        /// </summary>
        public Device Device
        {
            get { return _device; }
        }

        /// <summary>
        ///     Gets a reference to the default <see cref="Direct2D1.DeviceContext" />.
        /// </summary>
        public DeviceContext DeviceContext
        {
            get { return _deviceContext; }
        }

        /// <summary>
        ///     Gets a reference to the default <see cref="SharpDX.DirectWrite.Factory1" />.
        /// </summary>
        public Factory1 DirectWriteFactory
        {
            get { return _directWriteFactory; }
        }

        #endregion

        #region GraphicsDeviceService events handlers

        private void GraphicsDeviceServiceOnDeviceChangeBegin(object sender, EventArgs e)
        {
            // Dispose only the Direct2D bitmap
            if (_target != null)
            {
                if (_deviceContext != null)
                {
                    _deviceContext.Target = null;
                }
                RemoveAndDispose(ref _target);
            }
        }

        private void GraphicsDeviceServiceOnDeviceChangeEnd(object sender, EventArgs e)
        {
            CreateOrUpdateDirect2D();
        }

        private void GraphicsDeviceServiceOnDeviceLost(object sender, EventArgs e)
        {
        }

        /// <summary>
        ///     Handles the <see cref="IGraphicsDeviceService.DeviceCreated" /> event.
        ///     Initializes the <see cref="Direct2DService.Device" /> and <see cref="DeviceContext" />.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void GraphicsDeviceServiceOnDeviceCreated(object sender, EventArgs e)
        {
        }

        private void CreateOrUpdateDirect2D()
        {
            // Dispose and recreate all devices only if the GraphicsDevice changed
            if (graphicsDeviceCopy != _graphicsDeviceService.GraphicsDevice)
            {
                graphicsDeviceCopy = _graphicsDeviceService.GraphicsDevice;

                DisposeAll();

                var device = (Direct3D11.Device)_graphicsDeviceService.GraphicsDevice;
                using (var dxgiDevice = device.QueryInterface<DXGI.Device>())
                {
                    _device = ToDispose(new Device(dxgiDevice, new CreationProperties { DebugLevel = D2DDebugLevel }));
                    _deviceContext = ToDispose(new DeviceContext(_device, DeviceContextOptions.None));
                }

                _directWriteFactory = ToDispose(new Factory1());
            }

            // Dispose the Direct2D bitmap
            if (_target != null)
            {
                RemoveAndDispose(ref _target);
            }

            // Create a Bitmap1 from backbuffer and make it the D2D context's target
            GraphicsDevice graphicsDevice = _graphicsDeviceService.GraphicsDevice;
            if (graphicsDevice == null || graphicsDevice.Presenter == null ||
                graphicsDevice.Presenter.NativePresenter == null) return;
            var swapChain = (SwapChain1)graphicsDevice.Presenter.NativePresenter;
            using (var surface = swapChain.GetBackBuffer<Surface>(0))
            {
                var properties = new BitmapProperties1
                {
                    BitmapOptions = BitmapOptions.Target | BitmapOptions.CannotDraw,
                    DpiX = DisplayProperties.LogicalDpi,
                    DpiY = DisplayProperties.LogicalDpi,
                    PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)
                };

                // Resizing the application will produce:
                // DXGI ERROR: IDXGISwapChain::ResizeBuffers: Swapchain cannot be resized unless all outstanding buffer references have been released. [ MISCELLANEOUS ERROR #19: ]
                // http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx#Handling_Window_Resizing
                // http://sharpdx.org/forum/5-api-usage/228-d3d11-directwrite-d2d1-directdraw-directwrite-into-a-texture2d
                // The responsible of this is the Bitmap1 below, disposing it, releasing it does absolutely nothing
                // It seems that Dispose() method is not implemented for this type
                // If the following 3 lines are commented out, no error is produced but obviously no 2D content is drawn anymore
                // Now the interesting part is that things do work even though this is error is produced, still it'd be nice to have no error
                
                _target = ToDispose(new Bitmap1(DeviceContext, surface, properties));
                _deviceContext.Target = _target;
            } 
        }

        /// <summary>
        ///     Handles the <see cref="IGraphicsDeviceService.DeviceDisposing" /> event.
        ///     Disposes the <see cref="Direct2DService.Device" />, <see cref="DeviceContext" /> and its render target
        ///     associated with the current <see cref="Direct2DService" /> instance.
        /// </summary>
        /// <param name="sender">Ignored.</param>
        /// <param name="e">Ignored.</param>
        private void GraphicsDeviceServiceOnDeviceDisposing(object sender, EventArgs e)
        {
            DisposeAll();
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Disposes the <see cref="Direct2DService.Device" />, <see cref="DeviceContext" /> and its render target
        ///     associated with the current <see cref="Direct2DService" /> instance.
        /// </summary>
        private void DisposeAll()
        {
            if (_deviceContext != null)
            {
                _deviceContext.Target = null;
            }

            RemoveAndDispose(ref _target);
            RemoveAndDispose(ref _directWriteFactory);
            RemoveAndDispose(ref _deviceContext);
            RemoveAndDispose(ref _device);
        }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Diposes all resources associated with the current <see cref="Direct2DService" /> instance.
        /// </summary>
        /// <param name="disposeManagedResources">Indicates whether to dispose management resources.</param>
        protected override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);

            DisposeAll();
        }

        #endregion
    }
}