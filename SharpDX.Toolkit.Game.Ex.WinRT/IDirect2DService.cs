using System;
using SharpDX.Direct2D1;
using Factory1 = SharpDX.DirectWrite.Factory1;

namespace SharpDX.Toolkit {
    /// <summary>
    ///     Provides Direct2DService support for drawing on D3D11.1 SwapChain
    /// </summary>
    public interface IDirect2DService : IDisposable
    {
        /// <summary>
        ///     Gets a reference to the Direct2DService device. Can be used to create additional <see cref="Direct2D1.DeviceContext" />.
        /// </summary>
        Device Device { get; }

        /// <summary>
        ///     Gets a reference to the default <see cref="Direct2D1.DeviceContext" /> which will draw directly over SwapChain.
        ///     The developer is responsible to restore default render target states.
        /// </summary>
        DeviceContext DeviceContext { get; }

        /// <summary>
        ///     Gets a reference to the default <see cref="SharpDX.DirectWrite.Factory1" /> used to create all DirectWrite objects.
        /// </summary>
        Factory1 DirectWriteFactory { get; }
    }
}