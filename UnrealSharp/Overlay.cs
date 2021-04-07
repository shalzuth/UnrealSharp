using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Windows;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using FactoryType = SharpDX.Direct2D1.FactoryType;
using FontFactory = SharpDX.DirectWrite.Factory;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;
using Color = System.Drawing.Color;

namespace UnrealSharp
{
    public class OverlayRenderForm : RenderForm
    {
        public OverlayRenderForm()
        {
            ShowInTaskbar = false;
            BackColor = TransparencyKey = Color.AntiqueWhite;
        }
        [DllImport("user32")] static extern IntPtr SetActiveWindow(IntPtr handle);
        protected override void WndProc(ref Message m)
        {
            if (FormBorderStyle == FormBorderStyle.Sizable) base.WndProc(ref m);
            else if (m.Msg == 0x21)
            {
                m.Result = (IntPtr)4;
                return;
            }
            else if (m.Msg == 6)
            {
                if (((int)m.WParam & 0xFFFF) != 0)
                    if (m.LParam != IntPtr.Zero) SetActiveWindow(m.LParam);
                    else SetActiveWindow(IntPtr.Zero);
            }
            else
                base.WndProc(ref m);
        }
        protected override bool ShowWithoutActivation { get { return FormBorderStyle == FormBorderStyle.Sizable ? base.ShowWithoutActivation : true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                if (FormBorderStyle == FormBorderStyle.Sizable) return base.CreateParams;
                var param = base.CreateParams;
                param.ExStyle |= 0x08000000;
                return param;
            }
        }
    }
    public class Overlay
    {
        WindowRenderTarget GraphicsDevice;
        TextFormat tf;
        SolidColorBrush br;
        FontFactory font;
        Process process;
        RenderLoop Loop;
        OverlayRenderForm overlayWindowForm;
        public Overlay(Process proc)
        {
            overlayWindowForm = new OverlayRenderForm();
            Control.CheckForIllegalCrossThreadCalls = false;
            process = proc;
            CreateDx();
            SetToTransparentChild();
            Loop = new RenderLoop(overlayWindowForm);
            overlayWindowForm.Show();
        }
        public void Begin()
        {
            if (overlayWindowForm.FormBorderStyle != FormBorderStyle.Sizable) overlayWindowForm.TopMost = process.MainWindowHandle == GetForegroundWindow();
            TickFps();
            GraphicsDevice.BeginDraw();
            GraphicsDevice.Clear(((overlayWindowForm.FormBorderStyle == FormBorderStyle.Sizable) ? Color.Blue : Color.AntiqueWhite).ToSharpDx().ToColor4());
        }
        public void End()
        {
            GraphicsDevice.EndDraw();
        }
        void CreateDx()
        {
            var deviceProperties = new HwndRenderTargetProperties()
            {
                Hwnd = overlayWindowForm.Handle,
                PixelSize = new Size2(1920, 1080),
                PresentOptions = PresentOptions.Immediately
            };
            var _factory = new Factory(FactoryType.SingleThreaded);
            font = new FontFactory();
            tf = new TextFormat(font, "Arial", 12);
            var renderProperties = new RenderTargetProperties(RenderTargetType.Default, new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied), 96.0f, 96.0f, RenderTargetUsage.None, FeatureLevel.Level_DEFAULT);
            try
            {
                GraphicsDevice = new WindowRenderTarget(_factory, renderProperties, deviceProperties);
            }
            catch (SharpDXException)
            {
                try
                {
                    renderProperties.PixelFormat = new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied);
                    GraphicsDevice = new WindowRenderTarget(_factory, renderProperties, deviceProperties);
                }
                catch (SharpDXException)
                {
                    renderProperties.PixelFormat = new PixelFormat(Format.Unknown, AlphaMode.Premultiplied);
                    GraphicsDevice = new WindowRenderTarget(_factory, renderProperties, deviceProperties);
                }
            }
            br = new SolidColorBrush(GraphicsDevice, Color.Green.ToSharpDx().ToColor4());
            GraphicsDevice.AntialiasMode = AntialiasMode.Aliased;
            GraphicsDevice.TextAntialiasMode = TextAntialiasMode.Aliased;
        }
        public void SetToTransparentChild(Boolean topMost = true)
        {
            overlayWindowForm.FormBorderStyle = FormBorderStyle.None;
            SetWindowLong(overlayWindowForm.Handle, -20, GetWindowLong(overlayWindowForm.Handle, -20) | 0x80000 | 0x20);
            GetClientRect(process.MainWindowHandle, out Rect c);
            GraphicsDevice.Resize(new Size2(c.Right, c.Bottom));
            if (topMost)
            {
                GetWindowRect(process.MainWindowHandle, out Rect w);
                var border = (w.Right - w.Left - c.Right) / 2;
                var toolbar = w.Bottom - w.Top - c.Bottom - border;
                SetWindowPos(overlayWindowForm.Handle, 0, w.Left + border, w.Top + toolbar, c.Right, c.Bottom, 0);
                overlayWindowForm.TopMost = true;
            }
            else
            {
                SetParent(overlayWindowForm.Handle, (IntPtr)process.MainWindowHandle);
                SetWindowPos(overlayWindowForm.Handle, 0, 0, 0, c.Right, c.Bottom, 0);
            }
        }
        public void SetToRegularWindow()
        {
            overlayWindowForm.TopMost = false;
            SetWindowPos(overlayWindowForm.Handle, 0, 0, 0, 500, 500, 0);
            GraphicsDevice.Resize(new Size2(500, 500));
            //ShowInTaskbar = true;
            overlayWindowForm.FormBorderStyle = FormBorderStyle.Sizable;
            //SetWindowLong(Handle, -20, GetWindowLong(Handle, -20) | 0x80000 | 0x20);
            //SetParent(Handle, IntPtr.Zero);
        }
        Vector3 LastRotation = Vector3.Zero;
        Vector3 vAxisX = Vector3.Zero;
        Vector3 vAxisY = Vector3.Zero;
        Vector3 vAxisZ = Vector3.Zero;
        public Vector2 WorldToScreen(Vector3 worldLocation, Vector3 cameraLocation, Vector3 cameraRotation, Single fieldOfView)
        {
            if (LastRotation != cameraRotation)
            {
                cameraRotation.GetAxes(out vAxisX, out vAxisY, out vAxisZ);
                LastRotation = cameraRotation;
            }
            var vDelta = worldLocation - cameraLocation;
            var vTransformed = new Vector3(vDelta.Mult(vAxisY), vDelta.Mult(vAxisZ), vDelta.Mult(vAxisX));
            if (vTransformed.Z < 1f) vTransformed.Z = 1f;
            var ScreenCenterX = overlayWindowForm.ClientSize.Width / 2;
            var ScreenCenterY = overlayWindowForm.ClientSize.Height / 2;
            var fullScreen = new Vector2(ScreenCenterX + vTransformed.X * (ScreenCenterX / (float)Math.Tan(fieldOfView * (float)Math.PI / 360)) / vTransformed.Z,
                ScreenCenterY - vTransformed.Y * (ScreenCenterX / (float)Math.Tan(fieldOfView * (float)Math.PI / 360)) / vTransformed.Z);
            return new Vector2(fullScreen.X, fullScreen.Y);
        }
        Single LastYRotation = 0;
        Single CameraSinTheta = 0;
        Single CameraCosTheta = 0;
        Vector2 WorldToWindow(Vector3 targetLocation, Vector3 playerLocation, Vector3 cameraRotation, Single maxRange, Single radarSize)
        {
            if (LastYRotation != cameraRotation.Y)
            {
                var CameraRadians = (Single)Math.PI * (-cameraRotation.Y - 90.0f) / 180.0f;
                CameraSinTheta = (Single)Math.Sin(CameraRadians);
                CameraCosTheta = (Single)Math.Cos(CameraRadians);
                LastYRotation = cameraRotation.Y;
            }
            radarSize /= 2;
            var diff = targetLocation - playerLocation;
            var radarLoc = new Vector2(radarSize * diff.X / maxRange, radarSize * diff.Y / maxRange);
            radarLoc = new Vector2(CameraCosTheta * radarLoc.X - CameraSinTheta * radarLoc.Y, CameraSinTheta * radarLoc.X + CameraCosTheta * radarLoc.Y);
            radarLoc += new Vector2(radarSize, radarSize);
            return radarLoc;
        }
        public void DrawLines(Color color, Vector2[] points)
        {
            for (int i = 0; i < points.Length - 1; i++)
                DrawLine(color, points[i], points[i + 1]);
        }
        public void DrawLine(Color color, Vector2 start, Vector2 end)
        {
            var dist = Vector2.Distance(start, end);
            var angle = -Math.PI / 2 - Math.Atan2(-(end.Y - start.Y), end.X - start.X);
            using (var brush = new SolidColorBrush(GraphicsDevice, color.ToSharpDx().ToColor4()))
                GraphicsDevice.DrawLine(start.ToSharpDx(), end.ToSharpDx(), brush);
        }
        public void DrawBox(Vector3 targetPosition, Vector3 targetRotation, Vector3 cameraLocation, Vector3 cameraRotation, Single fieldOfView, System.Drawing.Color color)
        {
            var targetTest = WorldToScreen(targetPosition, cameraLocation, cameraRotation, fieldOfView);
            if (targetTest.X < 0 || targetTest.Y < 0 || targetTest.X > overlayWindowForm.Width || targetTest.Y > overlayWindowForm.Height)
                return;

            Single l = 60f, w = 60f, h = 80f, o = 50f;

            var zOffset = -40f;
            var xOffset = -20f;
            var yOffset = -20f;

            var p02 = new Vector3(o - l, w / 2, 0f);
            var p03 = new Vector3(o - l, -w / 2, 0f);
            var p00 = new Vector3(o, -w / 2, 0f);
            var p01 = new Vector3(o, w / 2, 0f);

            var theta1 = 2.0f * (targetRotation.FromRotator().Y);

            var cos = (float)Math.Cos(theta1);
            var sin = (float)Math.Sin(theta1);

            Single[] rotMVals =
                {cos, sin, 0, 0,
                -sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1 };
            var rotM = new Matrix(rotMVals);

            var curPos = new Vector3(targetPosition.X + xOffset, targetPosition.Y + yOffset, targetPosition.Z + zOffset);
            p01 = SharpDX.Vector3.TransformCoordinate(p01.ToSharpDx(), rotM).ToNative() + curPos;
            p03 = SharpDX.Vector3.TransformCoordinate(p03.ToSharpDx(), rotM).ToNative() + curPos;
            p00 = SharpDX.Vector3.TransformCoordinate(p00.ToSharpDx(), rotM).ToNative() + curPos;
            p02 = SharpDX.Vector3.TransformCoordinate(p02.ToSharpDx(), rotM).ToNative() + curPos;

            var s03 = WorldToScreen(p03, cameraLocation, cameraRotation, fieldOfView);
            var s00 = WorldToScreen(p00, cameraLocation, cameraRotation, fieldOfView);
            var s02 = WorldToScreen(p02, cameraLocation, cameraRotation, fieldOfView);
            var s01 = WorldToScreen(p01, cameraLocation, cameraRotation, fieldOfView);

            p03.Z += h; var s032 = WorldToScreen(p03, cameraLocation, cameraRotation, fieldOfView);
            p00.Z += h; var s002 = WorldToScreen(p00, cameraLocation, cameraRotation, fieldOfView);
            p02.Z += h; var s022 = WorldToScreen(p02, cameraLocation, cameraRotation, fieldOfView);
            p01.Z += h; var s012 = WorldToScreen(p01, cameraLocation, cameraRotation, fieldOfView);

            DrawLines(color, new Vector2[] { s00, s01, s02, s03, s00 });
            DrawLines(color, new Vector2[] { s002, s012, s022, s032, s002 });
            DrawLine(color, s03, s032);
            DrawLine(color, s00, s002);
            DrawLine(color, s02, s022);
            DrawLine(color, s01, s012);
        }
        public void DrawArrow(Vector3 targetPosition, Vector3 targetRotation, Vector3 playerLocation, Vector3 cameraRotation)
        {
            if (targetPosition == playerLocation)
            {
                var playerLoc = WorldToWindow(playerLocation, playerLocation, cameraRotation, 3000, 200);
                DrawLine(Color.Green, playerLoc, new Vector2(playerLoc.X, playerLoc.Y - 100));
                return;
            }
            var radarLoc = WorldToWindow(targetPosition, playerLocation, cameraRotation, 3000, 200);
            if (radarLoc.X > 0 && radarLoc.X < 200 && radarLoc.Y > 0 && radarLoc.Y < 200)
            {
                targetRotation = targetRotation.FromRotator();
                targetRotation.Z = 0;
                targetRotation = Vector3.Normalize(targetRotation);
                var endLoc = targetPosition + 400 * targetRotation;
                var endRadarLoc = WorldToWindow(endLoc, playerLocation, cameraRotation, 3000, 200);
                DrawLine(Color.Yellow, radarLoc, endRadarLoc);
            }
        }
        public void DrawText(String text, Vector2 loc, Color color)
        {
            using (var size = new TextLayout(font, text, tf, 1920, 1080))
            using (var brush = new SolidColorBrush(GraphicsDevice, color.ToSharpDx().ToColor4()))
                GraphicsDevice.DrawText(text, tf, new SharpDX.Mathematics.Interop.RawRectangleF(loc.X, loc.Y, loc.X + size.Metrics.Width + 5, loc.Y + size.Metrics.Height + 5), brush);
        }
        Stopwatch clock = new Stopwatch();
        UInt64 frameCount;
        public Double MeasuredFps { get; set; }
        void TickFps()
        {
            if (!clock.IsRunning) clock.Start();
            frameCount++;
            var updateTimeMs = 400.0f;
            if (clock.ElapsedMilliseconds >= updateTimeMs)
            {
                //MeasuredFps = (float)frameCount / (clock.ElapsedMilliseconds / 1000.0f);
                MeasuredFps = (float)frameCount / (clock.ElapsedMilliseconds / 1000.0f);
                frameCount = 0;
                clock.Restart();
            }
        }
        public void AimAtPos(Vector2 location, Single smoothSpeed = 2)
        {
            Single ScreenCenterX = overlayWindowForm.Width / 2;
            Single ScreenCenterY = overlayWindowForm.Height / 2;
            Single TargetX = 0;
            Single TargetY = 0;
            if (location.X > ScreenCenterX)
            {
                TargetX = -(ScreenCenterX - location.X);
                TargetX /= smoothSpeed;
            }
            else if (location.X < ScreenCenterX)
            {
                TargetX = location.X - ScreenCenterX;
                TargetX /= smoothSpeed;
            }
            if (location.Y > ScreenCenterY)
            {
                TargetY = -(ScreenCenterY - location.Y);
                TargetY /= smoothSpeed;
            }
            else if (location.Y < ScreenCenterY)
            {
                TargetY = location.Y - ScreenCenterY;
                TargetY /= smoothSpeed;
            }
            if (TargetX > 10) TargetX = 10;
            if (TargetY > 10) TargetY = 10;
            mouse_event(0x0001, (int)TargetX, (int)TargetY, 0, 0);
        }
        [DllImport("user32")] static extern Int32 GetWindowLong(IntPtr hWnd, Int32 nIndex);
        [DllImport("user32")] static extern bool GetWindowRect(IntPtr hwnd, out Rect rectangle);
        [DllImport("user32")] static extern bool GetClientRect(IntPtr hwnd, out Rect rectangle);
        [DllImport("user32")] static extern int SetWindowLong(IntPtr hWnd, Int32 nIndex, Int32 dwNewLong);
        [DllImport("user32")] static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32")] static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, UInt32 uFlags);
        [DllImport("user32")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32")] static extern short GetKeyState(Int32 keyCode);
        [DllImport("user32")] static extern void mouse_event(UInt32 dwFlags, Int32 dx, Int32 dy, UInt32 dwData, Int32 dwExtraInfo);
        struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }
    }
}