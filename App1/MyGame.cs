using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.IO;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using SharpDX.WIC;
using BitmapInterpolationMode = SharpDX.Direct2D1.BitmapInterpolationMode;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;
using DeviceContext = SharpDX.Direct2D1.DeviceContext;
using Factory1 = SharpDX.DirectWrite.Factory1;
using PixelFormat = SharpDX.WIC.PixelFormat;

namespace App1 {
    internal class MyGame : Game {
        #region Private fields

        private readonly GraphicsDeviceManager _graphicsDeviceManager;
        private readonly Direct2DService _service;
        private BasicEffect _basicEffect;
        private Buffer<VertexPositionColor> _buffer;
        private VertexInputLayout _bufferLayout;
        private Bitmap1 _example1Bitmap;
        private SolidColorBrush _example2Brush1;
        private SolidColorBrush _example2Brush2;
        private Ellipse _example2Ellipse;
        private StrokeStyle _example2StrokeStyle;
        private SolidColorBrush _example3Brush1;
        private LinearGradientBrush _example3Brush2;
        private PathGeometry _example3Geometry;
        private SolidColorBrush _example4Brush;
        private TextFormat _example4TextFormat;

        #endregion

        public MyGame() {
            _graphicsDeviceManager = new GraphicsDeviceManager(this) {
                DeviceCreationFlags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug
            };

            _service = new Direct2DService(_graphicsDeviceManager);
            Services.AddService(typeof (IDirect2DService), _service);
        }


        protected override void LoadContent() {
            VertexPositionColor[] vertices = {
                new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0), Color.Cyan),
                new VertexPositionColor(new Vector3(0.5f, -0.5f, 0), Color.Magenta),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0), Color.Black),
                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0), Color.Yellow),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0), Color.Black),
                new VertexPositionColor(new Vector3(0.5f, -0.5f, 0), Color.Magenta)
            };

            _buffer = Buffer.Vertex.New(GraphicsDevice, vertices);
            _bufferLayout = VertexInputLayout.FromBuffer(0, _buffer);
            _basicEffect = new BasicEffect(GraphicsDevice) {VertexColorEnabled = true};

            // from http://msdn.microsoft.com/en-us/library/windows/desktop/dd756671(v=vs.85).aspx

            // example 1
            DeviceContext context = _service.DeviceContext;
            using (var factory = new ImagingFactory())
            using (var decoder = new PngBitmapDecoder(factory))
            using (var fileStream =
                new NativeFileStream("Content\\sample.png", NativeFileMode.Open, NativeFileAccess.Read))
            using (var wicStream = new WICStream(factory, fileStream)) {
                decoder.Initialize(wicStream, DecodeOptions.CacheOnLoad);
                using (BitmapFrameDecode decode = decoder.GetFrame(0))
                using (var converter = new FormatConverter(factory)) {
                    converter.Initialize(decode, PixelFormat.Format32bppPBGRA);
                    DeviceContext direct2DContext = context;
                    _example1Bitmap = Bitmap1.FromWicBitmap(direct2DContext, converter);
                }
            }

            // example 2
            _example2Brush2 = new SolidColorBrush(context, Color.Black);
            _example2Brush1 = new SolidColorBrush(context, Color.Silver);
            _example2Ellipse = new Ellipse(new Vector2(100.0f, 100.0f), 75.0f, 50.0f);
            var styleProperties = new StrokeStyleProperties {
                StartCap = CapStyle.Flat,
                EndCap = CapStyle.Flat,
                DashCap = CapStyle.Triangle,
                LineJoin = LineJoin.Miter,
                MiterLimit = 10.0f,
                DashStyle = DashStyle.DashDotDot,
                DashOffset = 0.0f
            };
            _example2StrokeStyle = new StrokeStyle(context.Factory, styleProperties);

            // example 3
            _example3Geometry = new PathGeometry(context.Factory);
            using (GeometrySink sink = _example3Geometry.Open()) {
                sink.BeginFigure(new Vector2(0.0f, 0.0f), FigureBegin.Filled);
                sink.AddLine(new Vector2(200.0f, 0.0f));
                sink.AddBezier(new BezierSegment {
                    Point1 = new Vector2(150.0f, 50.0f),
                    Point2 = new Vector2(150.0f, 150.0f),
                    Point3 = new Vector2(200.0f, 200.0f),
                });
                sink.AddLine(new Vector2(0.0f, 200.0f));
                sink.AddBezier(new BezierSegment {
                    Point1 = new Vector2(50.0f, 150.0f),
                    Point2 = new Vector2(50.0f, 50.0f),
                    Point3 = new Vector2(0.0f, 0.0f),
                });
                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            _example3Brush1 = new SolidColorBrush(context, Color4.Black);

            using (var gradientStopCollection = new GradientStopCollection(context, new[] {
                new GradientStop {Color = new Color4(0.0f, 1.0f, 1.0f, 0.25f), Position = 0.0f},
                new GradientStop {Color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), Position = 1.0f}
            })) {
                _example3Brush2 = new LinearGradientBrush(context,
                    new LinearGradientBrushProperties {
                        StartPoint = new Vector2(100.0f, 0.0f),
                        EndPoint = new Vector2(100.0f, 200.0f)
                    }, gradientStopCollection);
            }

            // example 4
            Factory1 directWriteFactory = _service.DirectWriteFactory;
            _example4TextFormat = new TextFormat(directWriteFactory, "Segoe UI", 32.0f);
            _example4Brush = new SolidColorBrush(context, Color4.Black);

            base.LoadContent();
        }

        protected override void Update(GameTime time) {
            var t = (float) time.TotalGameTime.TotalMilliseconds;
            float width = GraphicsDevice.Viewport.Width;
            float height = GraphicsDevice.Viewport.Height;
            float cx = width/2.0f;
            float cy = height/2.0f;
            float scaleX = 100.0f*(float) (1 + Math.Abs(0.5 + 0.5*Math.Sin(t/1000.0f*MathUtil.Pi)));
            float scaleY = 100.0f*(float) (1 + Math.Abs(0.5 + 0.5*Math.Sin((t + 600.0f)/1000.0f*MathUtil.Pi)));
            float angle = t/1000.0f%MathUtil.TwoPi;
            Matrix world =
                // Rotate around a point but keep head up
                Matrix.RotationZ(angle)*(Matrix.Translation(-0.5f, -0.5f, 0.0f)*Matrix.RotationZ(MathUtil.Pi - angle))
                    // Swimming effect
                *Matrix.Scaling(scaleX, scaleY, 1.0f)
                    // Center
                *Matrix.Translation(cx, cy, 0.0f);
            _basicEffect.World = world;
            _basicEffect.View = Matrix.Identity;
            _basicEffect.Projection = Matrix.OrthoOffCenterRH(0, width, height, 0, 0, 1);
            base.Update(time);
        }

        protected override void Draw(GameTime time) {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            /* Direct2D / DirectWrite example */
            DeviceContext context = _service.DeviceContext;
            if (context != null && context.Target != null) {
                context.BeginDraw();
                // example 1
                context.Transform = Matrix3x2.Translation(new Vector2(20.0f, 20.0f));
                context.DrawBitmap(_example1Bitmap, 0.75f, BitmapInterpolationMode.NearestNeighbor);
                // example 2
                context.Transform = Matrix3x2.Translation(new Vector2(220.0f, 20.0f));
                context.FillEllipse(_example2Ellipse, _example2Brush1);
                context.DrawEllipse(_example2Ellipse, _example2Brush2, 10.0f, _example2StrokeStyle);
                // example 3
                context.Transform = Matrix3x2.Translation(new Vector2(20.0f, 220.0f));
                context.DrawGeometry(_example3Geometry, _example3Brush1, 10.0f);
                context.FillGeometry(_example3Geometry, _example3Brush2);
                // example 4
                context.Transform = Matrix3x2.Translation(new Vector2(220.0f, 220.0f));
                var rect = new RectangleF(20.0f, 20.0f, 200.0f, 200.0f);
                context.DrawText("Hello, DirectWrite !", _example4TextFormat, rect, _example4Brush);
                context.EndDraw();
            }


            /* Direct3D example */
            GraphicsDevice.SetVertexBuffer(_buffer);
            GraphicsDevice.SetVertexInputLayout(_bufferLayout);
            _basicEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.Draw(PrimitiveType.TriangleList, _buffer.ElementCount);

            base.Draw(time);
        }
    }
}