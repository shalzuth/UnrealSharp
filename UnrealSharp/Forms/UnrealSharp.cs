using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace UnrealSharp
{
    public partial class UnrealSharp : Form
    {
        String staticGameName => "autogenerate";
        //String staticGameName => "FSD-Win64-Shipping";
        Process process;
        Overlay esp;
        public List<UnrealEngine.UEObject> actors { get; set; } = new List<UnrealEngine.UEObject>();
        public UnrealSharp()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            if (staticGameName == "autogenerate") AddProcesses();
            else inspectProcess_Click(null, null);
        }
        void AddProcesses()
        {
            var window = Memory.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "UnrealWindow", null);
            if (window != IntPtr.Zero)
            {
                Memory.GetWindowThreadProcessId(window, out Int32 unrealProcId);
                var proc = Process.GetProcessById(unrealProcId);
                actorList.Items.Add(proc.Id + " : " + proc.ProcessName + " : " + proc.MainWindowTitle + " UE Autodetect");
                actorList.Items.Add("");
            }
            foreach (var proc in Process.GetProcesses())
                actorList.Items.Add(proc.Id + " : " + proc.ProcessName + " : " + proc.MainWindowTitle);
        }
        Object sync = new Object();
        void inspectProcess_Click(object sender, EventArgs e)
        {
            if (process == null)
            {
                GetProcess();
                new UnrealEngine(new Memory(process)).UpdateAddresses();
                esp = new Overlay(process);
                new Thread(() =>
                {
                    while (true)
                    {
                        lock (sync)
                        {
                            esp.Begin();
                            if (EngineLoop() > 0) { UnrealEngine.UEObject.ClearCache(); }
                            esp.End();
                        }
                    }
                })
                { IsBackground = true }.Start();
            }
            else
            {
                DumpScene();
            }
        }
        private void DumpScene()
        {
            actorList.Items.Clear();
            var World = new UnrealEngine.UEObject(UnrealEngine.Memory.ReadProcessMemory<UInt64>(UnrealEngine.GWorldPtr));
            var Levels = World["Levels"];
            for (var levelIndex = 0u; levelIndex < Levels.Num; levelIndex++)
            {
                var Level = Levels[levelIndex];
                actorList.Items.Add(Level.Address + " : " + Level.GetFullPath());
                var Actors = new UnrealEngine.UEObject(Level.Address + 0xA8); // todo fix hardcoded 0xA8 offset...
                for (var i = 0u; i < Actors.Num; i++)
                {
                    var Actor = Actors[i];
                    if (Actor.Address == 0) continue;
                    if (Actor.IsA("Class /Script/Engine.Actor"))
                        actorList.Items.Add(Actor.Address + " : " + Actor.GetFullPath());
                }
            }
        }
        private void dump_Click(object sender, EventArgs e)
        {
            UnrealEngine.Instance.DumpSdk();
        }
        private void DisplayActorInfo(UInt64 actorAddr)
        {
            actorInfo.Items.Clear();
            var actor = new UnrealEngine.UEObject(actorAddr);
            actorInfo.Items.Add(actor.Address + " : " + actor.GetFullPath() + " : " + actor.ClassName);
            var tempEntity = actor.ClassAddr;
            while (true)
            {
                var classNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(tempEntity + UnrealEngine.UEObject.nameOffset);
                var name = UnrealEngine.UEObject.GetName(classNameIndex);

                actorInfo.Items.Add(name);
                var field = tempEntity + UnrealEngine.UEObject.childPropertiesOffset - UnrealEngine.UEObject.fieldNextOffset;
                while ((field = UnrealEngine.Memory.ReadProcessMemory<UInt64>(field + UnrealEngine.UEObject.fieldNextOffset)) > 0)
                {
                    var fName = UnrealEngine.UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + UnrealEngine.UEObject.fieldNameOffset));
                    var fType = actor.GetFieldType(field);
                    var fValue = "(" + field.ToString() + ")";
                    var offset = (UInt32)actor.GetFieldOffset(field);
                    if (fType == "BoolProperty")
                    {
                        fType = "Boolean";
                        fValue = actor[fName].Flag.ToString();
                    }
                    else if (fType == "FloatProperty")
                    {
                        fType = "Single";
                        fValue = BitConverter.ToSingle(BitConverter.GetBytes(actor[fName].Value)).ToString();
                    }
                    else if (fType == "IntProperty")
                    {
                        fType = "Int32";
                        fValue = actor[fName].Value.ToString();
                    }
                    else if (fType == "ObjectProperty" || fType == "StructProperty")
                    {
                        var obj = new UnrealEngine.UEObject(UnrealEngine.Memory.ReadProcessMemory<UInt64>(actorAddr + offset)) { FieldOffset = offset };
                        fType = obj.GetShortName();
                    }
                    actorInfo.Items.Add("  " + fType + " " + fName + " = " + fValue);
                }
                tempEntity = UnrealEngine.Memory.ReadProcessMemory<UInt64>(tempEntity + UnrealEngine.UEObject.structSuperOffset);
                if (tempEntity == 0) break;
            }
        }
        private void actorList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (process == null) return;
            var actorAddr = UInt64.Parse(actorList.SelectedItem.ToString().Split(':')[0].Replace(" ", ""));
            DisplayActorInfo(actorAddr);
        }

        private void actorInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var actorAddr = UInt64.Parse(actorInfo.Items[0].ToString().Split(':')[0].Replace(" ", ""));
            var fName = actorInfo.SelectedItem.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var a = new UnrealEngine.UEObject(actorAddr)[fName];
            DisplayActorInfo(a.Address);
        }

        void GetProcess()
        {
            while (true)
            {
                if (staticGameName != "autogenerate") process = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Contains(staticGameName) && p.MainWindowHandle != IntPtr.Zero);
                else process = Process.GetProcessById(Int32.Parse(actorList.SelectedItem.ToString().Split(':')[0].Replace(" ", "")));
                if (process != null) break;
                Thread.Sleep(500);
            }
            inspectProcess.Text = "Dump Actor List";
        }
        Int32 EngineLoop()
        {
            if (UnrealEngine.GWorldPtr == 0) return 1;
            var sb = new StringBuilder();
            sb.AppendLine("Shalzuth's Helper Tool");
            sb.AppendLine("FPS : " + esp.MeasuredFps.ToString("0.00"));
            sb.AppendLine("ESP(F1) : " + Hotkeys.ToggledKey(Keys.F1));
            sb.AppendLine("Radar(F2) : " + Hotkeys.ToggledKey(Keys.F2));
            sb.AppendLine("HoldAim(F3) : " + Hotkeys.IsPressed(Keys.F3));
            esp.GraphicsDevice.DrawText(sb.ToString(), esp.tf, new RawRectangleF(20, 20, 20 + 400, 500), esp.br);
            var World = new UnrealEngine.UEObject(UnrealEngine.Memory.ReadProcessMemory<UInt64>(UnrealEngine.GWorldPtr)); if (World == null || !World.IsA("Class /Script/Engine.World")) return 1;
            var PersistentLevel = World["PersistentLevel"];
            var Levels = World["Levels"];
            var OwningGameInstance = World["OwningGameInstance"]; if (OwningGameInstance == null || !OwningGameInstance.IsA("Class /Script/Engine.GameInstance")) return 1;
            var LocalPlayers = OwningGameInstance["LocalPlayers"]; if (LocalPlayers == null) return 1;
            var PlayerController = LocalPlayers[0]["PlayerController"]; if (PlayerController == null) return 1;
            var Player = PlayerController["Player"];
            var AcknowledgedPawn = PlayerController["AcknowledgedPawn"];
            if (AcknowledgedPawn == null || !AcknowledgedPawn.IsA("Class /Script/Engine.Character")) return 1;

            var PlayerCameraManager = PlayerController["PlayerCameraManager"];
            var CameraCache = PlayerCameraManager["CameraCachePrivate"];
            var CameraPOV = CameraCache["POV"];
            var CameraLocation = UnrealEngine.Memory.ReadProcessMemory<Vector3>(CameraPOV["Location"].Address);
            var CameraRotation = UnrealEngine.Memory.ReadProcessMemory<Vector3>(CameraPOV["Rotation"].Address);
            var CameraFOV = UnrealEngine.Memory.ReadProcessMemory<Single>(CameraPOV["FOV"].Address);
            var PlayerRoot = AcknowledgedPawn["RootComponent"];
            var PlayerRelativeLocation = PlayerRoot["RelativeLocation"];
            var PlayerLocation = UnrealEngine.Memory.ReadProcessMemory<Vector3>(PlayerRelativeLocation.Address);
            if (Hotkeys.ToggledKey(Keys.F2)) esp.DrawArrow(PlayerLocation, CameraRotation, PlayerLocation, CameraRotation);
            var bestAngle = Single.MaxValue;
            var target = Vector2.Zero;
            for (var levelIndex = 0u; levelIndex < Levels.Num; levelIndex++)
            {
                var Level = Levels[levelIndex];
                var Actors = new UnrealEngine.UEObject(Level.Address + 0xA8); // todo fix hardcoded 0xA8 offset...
                var y = 0;
                for (var i = 0u; i < Actors.Num; i++)
                {
                    var Actor = Actors[i];
                    if (Actor.Address == 0) continue;
                    if (Actor.Address == Player.Address) continue;
                    if (!Actor.IsA("Class /Script/Engine.Actor")) continue;
                    if (Actor["bActorIsBeingDestroyed"].Value == 1) continue;
                    var RootComponent = Actor["RootComponent"];
                    if (RootComponent == null || RootComponent.Address == 0 || !RootComponent.ClassName.Contains("Component")) continue;
                    var RelativeLocation = RootComponent["RelativeLocation"];
                    var Location = UnrealEngine.Memory.ReadProcessMemory<Vector3>(RelativeLocation.Address);
                    var RelativeRotation = RootComponent["RelativeRotation"];
                    var Rotation = UnrealEngine.Memory.ReadProcessMemory<Vector3>(RelativeRotation.Address);

                    var loc = esp.WorldToScreen(Location, CameraLocation, CameraRotation, CameraFOV);
                    if (loc.X > 0 && loc.Y > 0 && loc.X < esp.Width && loc.Y < esp.Height)
                        if (Hotkeys.ToggledKey(Keys.F1)) esp.DrawBox(Location, Rotation, CameraLocation, CameraRotation, CameraFOV, Color.Red);
                    if (Hotkeys.ToggledKey(Keys.F2)) esp.DrawArrow(Location, Rotation, CameraLocation, CameraRotation);

                    if (Hotkeys.IsPressed(Keys.F3))
                    {
                        var turnVector = CameraLocation.CalcRotation(Location, CameraRotation, 0.0f);
                        var turnWeight = (Single)(CameraRotation - turnVector).Length();
                        if (turnWeight < bestAngle)
                        {
                            bestAngle = turnWeight;
                            target = esp.WorldToScreen(Location, CameraLocation, CameraRotation, CameraFOV);
                        }
                    }
                }
                if (Hotkeys.IsPressed(Keys.F3)) esp.AimAtPos(target);
            }
            return 0;
        }
    }
}
