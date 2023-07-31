using System;
using System.Diagnostics;
using UnrealSharp;

namespace UEInspector
{
    public partial class Inspector : Form
    {
        Process process;
        public Inspector()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            AddProcesses();
        }
        void AddProcesses()
        {
            var window = Memory.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "UnrealWindow", null);
            if (window != IntPtr.Zero)
            {
                Memory.GetWindowThreadProcessId(window, out Int32 unrealProcId);
                process = Process.GetProcessById(unrealProcId);
                new UnrealEngine(new Memory(process)).UpdateAddresses();
            }
        }
        void DisplayActorInfo(nint actorAddr)
        {
            actorInfo.Items.Clear();
            actorInfo.Text = "";
            var actor = new UEObject(actorAddr);
            actorInfo.Items.Add(actor.Address + " : " + actor.GetFullPath() + " : " + actor.ClassName);
            var tempEntity = actor.ClassAddr;
            while (true)
            {
                var classNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(tempEntity + UEObject.nameOffset);
                var name = UEObject.GetName(classNameIndex);

                actorInfo.Items.Add(name);
                var field = tempEntity + UEObject.childPropertiesOffset - UEObject.fieldNextOffset;
                while ((field = UnrealEngine.Memory.ReadProcessMemory<nint>(field + UEObject.fieldNextOffset)) > 0)
                {
                    var fName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + UEObject.fieldNameOffset));
                    var fType = actor.GetFieldType(field);
                    var fValue = "(" + field.ToString() + ")";
                    var offset = actor.GetFieldOffset(field);
                    if (fType == "BoolProperty")
                    {
                        fType = "Boolean";
                        fValue = actor[fName].Flag.ToString();
                    }
                    else if (fType == "FloatProperty")
                    {
                        fType = "Single";
                        fValue = BitConverter.ToSingle(BitConverter.GetBytes(actor[fName].Value), 0).ToString();
                    }
                    else if (fType == "IntProperty")
                    {
                        fType = "Int32";
                        fValue = actor[fName].Value.ToString();
                    }
                    else if (fType == "ObjectProperty" || fType == "StructProperty")
                    {
                        var structFieldIndex = UnrealEngine.Memory.ReadProcessMemory<int>(UnrealEngine.Memory.ReadProcessMemory<nint>(field + UEObject.propertySize) + UEObject.nameOffset);
                        fType = UEObject.GetName(structFieldIndex);
                    }
                    actorInfo.Items.Add("  " + fType + " " + fName + " = " + fValue);
                }

                field = tempEntity + UEObject.childrenOffset - UEObject.funcNextOffset;
                while ((field = UnrealEngine.Memory.ReadProcessMemory<nint>(field + UEObject.funcNextOffset)) > 0)
                {
                    var fName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + UEObject.nameOffset));
                    actorInfo.Items.Add("  func " + fName);
                }
                tempEntity = UnrealEngine.Memory.ReadProcessMemory<nint>(tempEntity + UEObject.structSuperOffset);
                if (tempEntity == 0) break;
            }
        }

        private void dump_Click(object sender, EventArgs e)
        {
            actorList.Items.Clear();
            var World = new UEObject(UnrealEngine.Memory.ReadProcessMemory<nint>(UnrealEngine.GWorldPtr));
            var Levels = World["Levels"];
            for (var levelIndex = 0; levelIndex < Levels.Num; levelIndex++)
            {
                var Level = Levels[levelIndex];
                actorList.Items.Add(Level.Address + " : " + Level.GetFullPath());
                var Actors = new UEObject(Level.Address + 0xA8); // todo fix hardcoded 0xA8 offset...
                for (var i = 0; i < Actors.Num; i++)
                {
                    var Actor = Actors[i];
                    if (Actor.Address == 0) continue;
                    if (Actor.IsA("Class /Script/Engine.Actor"))
                        actorList.Items.Add(Actor.Address + " : " + Actor.GetFullPath());
                }
            }

        }
        private void actorList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (process == null) return;
                var actorAddr = nint.Parse(actorList.SelectedItem.ToString().Split(':')[0].Replace(" ", ""));
                DisplayActorInfo(actorAddr);
            }
            catch { }
        }

        private void actorInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var actorAddr = nint.Parse(actorInfo.Items[0].ToString().Split(':')[0].Replace(" ", ""));
                var fName = actorInfo.SelectedItem.ToString().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
                var a = new UEObject(actorAddr)[fName];
                DisplayActorInfo(a.Address);
            }
            catch { }
        }

        private void dumpSDK_Click(object sender, EventArgs e)
        {
            UnrealEngine.Instance.DumpSdk();
        }
    }
}