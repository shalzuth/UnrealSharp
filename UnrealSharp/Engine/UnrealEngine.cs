using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnrealSharp
{
    public class UnrealEngine
    {
        public static UnrealEngine Instance;
        static UInt64 GNamesPattern;
        public static UInt64 GNames;
        static UInt64 GObjectsPattern;
        public static UInt64 GObjects;
        static UInt64 GWorldPtrPattern;
        public static UInt64 GWorldPtr;
        static UInt64 GEnginePattern;
        public static UInt64 GEngine;
        public static UInt64 GStaticCtor;
        public static UInt64 ActorListOffset;
        public static Memory Memory;
        public UnrealEngine(Memory mem) { Memory = mem; Instance = this; }
        public void LoadAddesses(String data)
        {
            var dataBytes = Convert.FromBase64String(data);
            var i = 0;

            GNamesPattern = BitConverter.ToUInt64(dataBytes, i++ * 8);
            var newFName = true;// (GNamesPattern & 0x8000000000000000) == 0x8000000000000000; if (newFName) GNamesPattern -= 0x8000000000000000;
            var offset = Memory.ReadProcessMemory<UInt32>(GNamesPattern + 3);
            GNames = newFName ? GNamesPattern + offset + 7 : Memory.ReadProcessMemory<UInt64>(GNamesPattern + offset + 7);

            GWorldPtrPattern = BitConverter.ToUInt64(dataBytes, i++ * 8);
            offset = Memory.ReadProcessMemory<UInt32>(GWorldPtrPattern + 3);
            GWorldPtr = GWorldPtrPattern + offset + 7;

            GObjectsPattern = BitConverter.ToUInt64(dataBytes, i++ * 8);
            offset = Memory.ReadProcessMemory<UInt32>(GObjectsPattern + 13);
            GObjects = GObjectsPattern + offset + 17 - Memory.BaseAddress;

            GEnginePattern = BitConverter.ToUInt64(dataBytes, i++ * 8);
            offset = Memory.ReadProcessMemory<UInt32>(GEnginePattern + 3);
            GEngine = Memory.ReadProcessMemory<UInt64>(GEnginePattern + offset + 7);

            GStaticCtor = BitConverter.ToUInt64(dataBytes, i++ * 8);
            var j = 0;
            UEObject.objectOuterOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.classOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.nameOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.structSuperOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.childPropertiesOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.childrenOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.fieldNameOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.fieldTypeNameOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.fieldClassOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.fieldNextOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.funcNextOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.fieldOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.propertySize = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.vTableFuncNum = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.funcFlagsOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.enumArrayOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
            UEObject.enumCountOffset = BitConverter.ToUInt32(dataBytes, i * 8 + j++ * 4);
        }
        public String SaveAddresses()
        {
            var bytes = new List<Byte>();
            bytes.AddRange(BitConverter.GetBytes(GNamesPattern));
            bytes.AddRange(BitConverter.GetBytes(GWorldPtrPattern));
            bytes.AddRange(BitConverter.GetBytes(GObjectsPattern));
            bytes.AddRange(BitConverter.GetBytes(GEnginePattern));
            bytes.AddRange(BitConverter.GetBytes(GStaticCtor));
            bytes.AddRange(BitConverter.GetBytes(UEObject.objectOuterOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.classOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.nameOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.structSuperOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.childPropertiesOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.childrenOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.fieldNameOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.fieldTypeNameOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.fieldClassOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.fieldNextOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.funcNextOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.fieldOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.propertySize));
            bytes.AddRange(BitConverter.GetBytes(UEObject.vTableFuncNum));
            bytes.AddRange(BitConverter.GetBytes(UEObject.funcFlagsOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.enumArrayOffset));
            bytes.AddRange(BitConverter.GetBytes(UEObject.enumCountOffset));
            return Convert.ToBase64String(bytes.ToArray());
        }
        public void UpdateAddresses()
        {
            {
                // GNamesPattern = (UInt64)Memory.FindPattern("48 8D 0D ? ? ? ? E8 ? ? ? ? C6 05 ? ? ? ? 01 0F 10 03 4C 8D 44 24 20 48 8B C8");
                //GNamesPattern = (UInt64)Memory.FindPattern("74 09 48 8D 15 ? ? ? ? EB 16");
                GNamesPattern = (UInt64)Memory.FindPattern("48 8D 35 ? ? ? ? EB 16");
                if (GNamesPattern == 0)
                {
                    UEObject.NewFName = false;
                    GNamesPattern = (UInt64)Memory.FindPattern("48 8B 05 ? ? ? ? 48 85 C0 75 5F");
                    var offset = Memory.ReadProcessMemory<UInt32>(GNamesPattern + 3);
                    GNames = Memory.ReadProcessMemory<UInt64>(GNamesPattern + offset + 7);
                    if (GNamesPattern == 0) throw new Exception("need new GNames pattern");
                    if (UEObject.GetName(1) != "ByteProperty") throw new Exception("bad GNames");
                }
                else
                {
                    var offset = Memory.ReadProcessMemory<UInt32>(GNamesPattern + 3);
                    GNames = GNamesPattern + offset + 7;
                    if (UEObject.GetName(3) != "ByteProperty") throw new Exception("bad GNames");
                }
            }
            {
                GWorldPtrPattern = (UInt64)Memory.FindPattern("48 8B 1D ? ? ? ? 48 85 DB 74 3B 41 B0 01");
                GObjectsPattern = (UInt64)Memory.FindPattern("C1 F9 10 48 63 C9 48 8D 14 40 48 8B 05");
                //DumpGNames();

                var offset = UnrealEngine.Memory.ReadProcessMemory<UInt32>(GWorldPtrPattern + 3);
                GWorldPtr = GWorldPtrPattern + offset + 7;
                UpdateUEObject();

                offset = Memory.ReadProcessMemory<UInt32>(GObjectsPattern + 13);
                GObjects = GObjectsPattern + offset + 17 - Memory.BaseAddress;
            }
            {
                GEnginePattern = (UInt64)Memory.FindPattern("48 8B 0D ?? ?? ?? ?? 48 85 C9 74 1E 48 8B 01 FF 90");
                var offset = Memory.ReadProcessMemory<UInt32>(GEnginePattern + 3);
                GEngine = Memory.ReadProcessMemory<UInt64>(GEnginePattern + offset + 7);
            }
            {
                var engine = new UEObject(GEngine);
                GStaticCtor = (UInt64)Memory.FindPattern("4C 89 44 24 18 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4");
            }
            {
                var world = Memory.ReadProcessMemory<UInt64>(GWorldPtr);
                var World = new UEObject(world);
                var Level = World["PersistentLevel"];
                var owningWorldOffset = (UInt64)Level.GetFieldOffset(Level.GetFieldAddr("OwningWorld"));
                // https://github.com/EpicGames/UnrealTournament/blob/3bf4b43c329ce041b4e33c9deb2ca66d78518b29/Engine/Source/Runtime/Engine/Classes/Engine/Level.h#L366
                // Actors, StreamedLevelOwningWorld, Owning World
                ActorListOffset = owningWorldOffset - 0x10;
            }
            //DumpSdk();
        }
        public void EnableConsole()
        {
            var engine = new UEObject(GEngine);
            var console = new UEObject(Memory.Execute(GStaticCtor, engine["ConsoleClass"].Value, engine["GameViewport"].Address, 0, 0, 0, 0, 0, 0, 0));
            engine["GameViewport"]["ViewportConsole"] = console;
        }
        public void UpdateUEObject()
        {
            var world = Memory.ReadProcessMemory<UInt64>(GWorldPtr);
            {
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var foundClassAndName = false;
                for (var c = 0u; c < 0x50 && !foundClassAndName; c += 0x8)
                {
                    classPtr = Memory.ReadProcessMemory<UInt64>(world + c);
                    if (classPtr == 0x0) continue;
                    for (var n = 0u; n < 0x50 && !foundClassAndName; n += 0x8)
                    {
                        var classNameIndex = Memory.ReadProcessMemory<Int32>(classPtr + n);
                        var name = UEObject.GetName(classNameIndex);
                        if (name == "World")
                        {
                            UEObject.classOffset = c;
                            UEObject.nameOffset = n;
                            foundClassAndName = true;
                        }
                    }
                }
                if (!foundClassAndName) throw new Exception("bad World or offsets?");
            }
            {
                var foundOuter = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                for (var o = 0u; o < 0x50; o += 0x8)
                {
                    var outerObj = Memory.ReadProcessMemory<UInt64>(classPtr + o);
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(outerObj + UEObject.nameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "/Script/Engine")
                    {
                        UEObject.objectOuterOffset = o;
                        foundOuter = true;
                        break;
                    }
                }
                if (!foundOuter) throw new Exception("bad outer addr");
            }
            {
                var foundSuper = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                for (var o = 0u; o < 0x50; o += 0x8)
                {
                    var superObj = Memory.ReadProcessMemory<UInt64>(classPtr + o);
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(superObj + UEObject.nameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "Object")
                    {
                        UEObject.structSuperOffset = o;
                        foundSuper = true;
                        break;
                    }
                }
                if (!foundSuper) throw new Exception("bad super addr");
            }
            {
                var foundChildsAndFieldName = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                for (var c = 0u; c < 0x80 && !foundChildsAndFieldName; c += 0x8)
                {
                    var childPtr = Memory.ReadProcessMemory<UInt64>(classPtr + c);
                    if (childPtr == 0x0) continue;
                    for (var n = 0u; n < 0x80 && !foundChildsAndFieldName; n += 0x8)
                    {
                        var classNameIndex = Memory.ReadProcessMemory<Int32>(childPtr + n);
                        var name = UEObject.GetName(classNameIndex);
                        if (name == "PersistentLevel")
                        {
                            UEObject.childPropertiesOffset = c;
                            UEObject.fieldNameOffset = n;
                            foundChildsAndFieldName = true;
                        }
                    }
                }
                if (!foundChildsAndFieldName) throw new Exception("bad childs offset");
            }
            {
                var foundNextField = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var fieldPtr = Memory.ReadProcessMemory<UInt64>(classPtr + UEObject.childPropertiesOffset);
                for (var c = 0u; c < 0x80 && !foundNextField; c += 0x8)
                {
                    var childClassPtr = Memory.ReadProcessMemory<UInt64>(fieldPtr + c);
                    if (childClassPtr == 0x0) continue;
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(childClassPtr + UEObject.fieldNameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "NetDriver")
                    {
                        UEObject.fieldNextOffset = c;
                        foundNextField = true;
                    }
                }
                if (!foundNextField) throw new Exception("bad next field offset");
            }
            {
                var foundNextField = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var fieldPtr = Memory.ReadProcessMemory<UInt64>(classPtr + UEObject.childPropertiesOffset);
                for (var c = 0u; c < 0x180 && !foundNextField; c += 0x8)
                {
                    var childClassPtr = Memory.ReadProcessMemory<UInt64>(fieldPtr + c);
                    if (childClassPtr == 0x0) continue;
                    //var classNameOffset = UEObject.NewFName ? 0 : UEObject.fieldNameOffset;
                    var classNameOffset = UEObject.fieldNameOffset;
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(childClassPtr + classNameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "ObjectProperty")
                    {
                        UEObject.fieldClassOffset = c;
                        foundNextField = true;
                    }
                }
                if (!foundNextField) throw new Exception("bad field class offset");
            }
            {
                var foundFuncs = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                for (var c = 0u; c < 0x80 && !foundFuncs; c += 0x8)
                {
                    var childPtr = Memory.ReadProcessMemory<UInt64>(classPtr + c);
                    if (childPtr == 0x0) continue;
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(childPtr + UEObject.nameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "K2_GetWorldSettings")
                    {
                        UEObject.childrenOffset = c;
                        foundFuncs = true;
                    }
                }
                if (!foundFuncs)
                {
                    var testObj = new UEObject(world);
                    var isField = testObj["K2_GetWorldSettings"];
                    if (isField != null)
                    {
                        UEObject.childrenOffset = UEObject.funcNextOffset = UEObject.childPropertiesOffset;
                        foundFuncs = true;
                    }
                }
                if (!foundFuncs) throw new Exception("bad childs offset");
            }
            if (UEObject.childrenOffset != UEObject.childPropertiesOffset)
            {
                var foundNextField = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var fieldPtr = Memory.ReadProcessMemory<UInt64>(classPtr + UEObject.childrenOffset);
                for (var c = 0u; c < 0x80 && !foundNextField; c += 0x8)
                {
                    var childClassPtr = Memory.ReadProcessMemory<UInt64>(fieldPtr + c);
                    if (childClassPtr == 0x0) continue;
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(childClassPtr + UEObject.nameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "HandleTimelineScrubbed")
                    {
                        UEObject.funcNextOffset = c;
                        foundNextField = true;
                    }
                }
                if (!foundNextField) throw new Exception("bad next offset");
            }
            {
                var foundFieldOffset = false;
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var fieldPtr = Memory.ReadProcessMemory<UInt64>(classPtr + UEObject.childPropertiesOffset);
                for (var c = 0x0u; c < 0x80 && !foundFieldOffset; c += 0x4)
                {
                    var fieldOffset = Memory.ReadProcessMemory<UInt64>(fieldPtr + c);
                    var nextFieldPtr = Memory.ReadProcessMemory<UInt64>(fieldPtr + UEObject.fieldNextOffset);
                    var fieldOffsetPlus8 = Memory.ReadProcessMemory<UInt64>(nextFieldPtr + c);
                    if ((fieldOffset + 8) == fieldOffsetPlus8)
                    {
                        UEObject.fieldOffset = c;
                        foundFieldOffset = true;
                    }
                }
                if (!foundFieldOffset) throw new Exception("bad field offset");
            }
            {
                var World = new UEObject(world);
                var field = World.GetFieldAddr("StreamingLevelsToConsider");
                var foundPropertySize = false;
                for (var c = 0x60u; c < 0x100 && !foundPropertySize; c += 0x8)
                {
                    var classAddr = Memory.ReadProcessMemory<UInt64>(field + c);
                    var classNameIndex = Memory.ReadProcessMemory<Int32>(classAddr + UEObject.nameOffset);
                    var name = UEObject.GetName(classNameIndex);
                    if (name == "StreamingLevelsToConsider")
                    {
                        UEObject.propertySize = c;
                        foundPropertySize = true;
                    }
                }
                if (!foundPropertySize) throw new Exception("bad property size offset");
            }
            {
                var vTable = UnrealEngine.Memory.ReadProcessMemory<UInt64>(world);
                var foundProcessEventOffset = false;
                for (var i = 50u; i < 0x100 && !foundProcessEventOffset; i++)
                {
                    var s = UnrealEngine.Memory.ReadProcessMemory<IntPtr>(vTable + i * 8);
                    var sig = (UInt64)UnrealEngine.Memory.FindPattern("40 55 56 57 41 54 41 55 41 56 41 57", s, 0X20);
                    if (sig != 0)
                    {
                        UEObject.vTableFuncNum = i;
                        foundProcessEventOffset = true;
                    }
                }
                if (!foundProcessEventOffset) throw new Exception("bad process event offset");
            }
            {
                var testObj = new UEObject(world);
                var funcAddr = testObj.GetFuncAddr(testObj.ClassAddr, testObj.ClassAddr, "K2_GetWorldSettings");
                var foundFuncFlags = false;
                for (var i = 0u; i < 0x200 && !foundFuncFlags; i += 8)
                {
                    var flags = UnrealEngine.Memory.ReadProcessMemory<UInt64>(funcAddr + i);
                    if (flags == 0x0008000104020401)
                    {
                        UEObject.funcFlagsOffset = i;
                        foundFuncFlags = true;
                    }
                }
                if (!foundFuncFlags) throw new Exception("bad func flags offset");
            }
        }
        public void DumpGNames()
        {
            var testObj = new UEObject(0);
            var sb = new StringBuilder();
            var i = 0;
            while (true)
            {
                var name = UEObject.GetName(i);
                if (name == "badIndex") break;
                sb.AppendLine("[" + i + " / " + (i).ToString("X") + "] " + name);
                i += name.Length / 2 + name.Length % 2 + 1;
            }
            System.IO.Directory.CreateDirectory(Memory.Process.ProcessName);
            System.IO.File.WriteAllText(Memory.Process.ProcessName + @"\GNamesDump.txt", sb.ToString());
        }
        public String GetTypeFromFieldAddr(String fName, String fType, UInt64 fAddr, out String gettersetter)
        {
            gettersetter = "";
            if (fType == "BoolProperty")
            {
                fType = "bool";
                gettersetter = "{ get { return this[nameof(" + fName + ")].Flag; } set { this[nameof(" + fName + ")].Flag = value; } }";
            }
            else if (fType == "ByteProperty" || fType == "Int8Property")
            {
                fType = "byte";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "Int16Property")
            {
                fType = "short";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "UInt16Property")
            {
                fType = "ushort";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "IntProperty")
            {
                fType = "int";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "UInt32Property")
            {
                fType = "uint";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "Int64Property")
            {
                fType = "long";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "UInt64Property")
            {
                fType = "ulong";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "FloatProperty")
            {
                fType = "float";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "DoubleProperty")
            {
                fType = "double";
                gettersetter = "{ get { return this[nameof(" + fName + ")].GetValue<" + fType + ">(); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
            }
            else if (fType == "StrProperty")
            {
                fType = "unk";
            }
            else if (fType == "TextProperty")
            {
                fType = "unk";
            }
            else if (fType == "ObjectProperty")
            {
                var structFieldIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(UnrealEngine.Memory.ReadProcessMemory<UInt64>(fAddr + UEObject.propertySize) + UEObject.nameOffset);
                fType = UEObject.GetName(structFieldIndex);
                gettersetter = "{ get { return this[nameof(" + fName + ")].As<" + fType + ">(); } set { this[\"" + fName + "\"] = value; } }";
            }
            else if (fType == "StructProperty")
            {
                var structFieldIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(UnrealEngine.Memory.ReadProcessMemory<UInt64>(fAddr + UEObject.propertySize) + UEObject.nameOffset);
                fType = UEObject.GetName(structFieldIndex);
                //gettersetter = "{ get { return UnrealEngine.Memory.ReadProcessMemory<" + fType + ">(this[nameof(" + fName + ")].Address); } set { this[nameof(" + fName + ")].SetValue<" + fType + ">(value); } }";
                gettersetter = "{ get { return this[nameof(" + fName + ")].As<" + fType + ">(); } set { this[\"" + fName + "\"] = value; } }";
            }
            else if (fType == "EnumProperty")
            {
                var structFieldIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(UnrealEngine.Memory.ReadProcessMemory<UInt64>(fAddr + UEObject.propertySize + 8) + UEObject.nameOffset);
                fType = UEObject.GetName(structFieldIndex);
                gettersetter = "{ get { return (" + fType + ")this[nameof(" + fName + ")].GetValue<int>(); } set { this[nameof(" + fName + ")].SetValue<int>((int)value); } }";
            }
            else if (fType == "NameProperty")
            {
                fType = "unk";
            }
            else if (fType == "ArrayProperty")
            {
                var inner = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fAddr + UEObject.propertySize);
                var innerClass = UnrealEngine.Memory.ReadProcessMemory<UInt64>(inner + UEObject.fieldClassOffset);
                var structFieldIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(innerClass + UEObject.nameOffset);
                fType = UEObject.GetName(structFieldIndex);
                var innerType = GetTypeFromFieldAddr(fName, fType, inner, out gettersetter);
                gettersetter = "{ get { return new Array<" + innerType + ">(this[nameof(" + fName + ")].Address); } }";// set { this[\"" + fName + "\"] = value; } }";
                fType = "Array<" + innerType + ">";
            }
            else if (fType == "SoftObjectProperty")
            {
                fType = "unk";
            }
            else if (fType == "SoftClassProperty")
            {
                fType = "unk";
            }
            else if (fType == "WeakObjectProperty")
            {
                fType = "unk";
            }
            else if (fType == "LazyObjectProperty")
            {
                fType = "unk";
            }
            else if (fType == "DelegateProperty")
            {
                fType = "unk";
            }
            else if (fType == "MulticastSparseDelegateProperty")
            {
                fType = "unk";
            }
            else if (fType == "MulticastInlineDelegateProperty")
            {
                fType = "unk";
            }
            else if (fType == "ClassProperty")
            {
                fType = "unk";
            }
            else if (fType == "MapProperty")
            {
                fType = "unk";
            }
            else if (fType == "SetProperty")
            {
                fType = "unk";
            }
            else if (fType == "FieldPathProperty")
            {
                fType = "unk";
            }
            else if (fType == "InterfaceProperty")
            {
                fType = "unk";
            }
            if (fType == "unk")
            {
                fType = "Object";
                gettersetter = "{ get { return this[nameof(" + fName + ")]; } set { this[nameof(" + fName + ")] = value; } }";
            }
            return fType;
        }
        public class Package
        {
            public String FullName;
            public String Name => FullName.Substring(FullName.LastIndexOf("/") + 1);
            public List<SDKClass> Classes = new List<SDKClass>();
            public List<Package> Dependencies = new List<Package>();
            public class SDKClass
            {
                public String SdkType;
                public String Namespace;
                public String Name;
                public String Parent;
                public List<SDKFields> Fields = new List<SDKFields>();
                public List<SDKFunctions> Functions = new List<SDKFunctions>();
                public class SDKFields
                {
                    public String Type;
                    public String Name;
                    public String GetterSetter;
                    public Int32 EnumVal;
                }
                public class SDKFunctions
                {
                    public String ReturnType;
                    public String Name;
                    public List<SDKFields> Params = new List<SDKFields>();
                }

            }
        }
        public void DumpSdk(String location = "")
        {
            if (location == "") location = Memory.Process.ProcessName;
            var addresses = new StringBuilder();
            addresses.AppendLine("namespace SDK.Addresses");
            addresses.AppendLine("{");
            addresses.AppendLine("    public static class Hardcoded");
            addresses.AppendLine("    {");
            addresses.AppendLine("        public static string Payload = \"" + SaveAddresses() + "\";");
            addresses.AppendLine("    }");
            addresses.AppendLine("}");
            System.IO.Directory.CreateDirectory(location);
            System.IO.File.WriteAllText(location + @"\Addresses.cs", addresses.ToString());
            var entityList = Memory.ReadProcessMemory<UInt64>(Memory.BaseAddress + GObjects);
            var count = Memory.ReadProcessMemory<UInt32>(Memory.BaseAddress + GObjects + 0x14);
            entityList = Memory.ReadProcessMemory<UInt64>(entityList);
            var packages = new Dictionary<UInt64, List<UInt64>>();
            for (var i = 0u; i < count; i++)
            {
                // var entityAddr = Memory.ReadProcessMemory<UInt64>((entityList + 8 * (i / 0x10400)) + 24 * (i % 0x10400));
                var entityAddr = Memory.ReadProcessMemory<UInt64>((entityList + 8 * (i >> 16)) + 24 * (i % 0x10000));
                if (entityAddr == 0) continue;
                var outer = entityAddr;
                while (true)
                {
                    var tempOuter = Memory.ReadProcessMemory<UInt64>(outer + UEObject.objectOuterOffset);
                    if (tempOuter == 0) break;
                    outer = tempOuter;
                }
                if (!packages.ContainsKey(outer)) packages.Add(outer, new List<UInt64>());
                packages[outer].Add(entityAddr);
            }
            var ii = 0;
            var dumpedPackages = new List<Package>();
            foreach (var package in packages)
            {
                var packageObj = new UEObject(package.Key);
                var fullPackageName = packageObj.GetName();
                var dumpedClasses = new List<String>();
                var sdkPackage = new Package { FullName = fullPackageName };
                foreach (var objAddr in package.Value)
                {
                    var obj = new UEObject(objAddr);
                    if (dumpedClasses.Contains(obj.ClassName)) continue;
                    dumpedClasses.Add(obj.ClassName);
                    if (obj.ClassName.StartsWith("Package")) continue;
                    var typeName = obj.ClassName.StartsWith("Class") ? "class" : obj.ClassName.StartsWith("ScriptStruct") ? "class" : obj.ClassName.StartsWith("Enum") ? "enum" : "unk";
                    //if (obj.ClassName.StartsWith("BlueprintGenerated")) typeName = "class";
                    var className = obj.GetName();
                    if (typeName == "unk") continue;
                    if (className == "Object") continue;
                    var parentClass = UnrealEngine.Memory.ReadProcessMemory<UInt64>(obj.Address + UEObject.structSuperOffset);
                    var sdkClass = new Package.SDKClass { Name = className, Namespace = fullPackageName, SdkType = typeName };
                    if (typeName == "enum") sdkClass.Parent = "int";
                    else if (parentClass != 0)
                    {
                        var parentNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(parentClass + UEObject.nameOffset);
                        var parentName = UEObject.GetName(parentNameIndex);
                        sdkClass.Parent = parentName;
                    }
                    else sdkClass.Parent = "Object";
                    //else throw new Exception("unparented obj not supported");

                    if (typeName == "enum")
                    {
                        var enumArray = UnrealEngine.Memory.ReadProcessMemory<UInt64>(objAddr + 0x40);
                        var enumCount = UnrealEngine.Memory.ReadProcessMemory<UInt32>(objAddr + 0x48);
                        for (var i = 0u; i < enumCount; i++)
                        {
                            var enumNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(enumArray + i * 0x10);
                            var enumName = UEObject.GetName(enumNameIndex);
                            enumName = enumName.Substring(enumName.LastIndexOf(":") + 1);
                            var enumNameRepeatedIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(enumArray + i * 0x10 + 4);
                            if (enumNameRepeatedIndex > 0)
                                enumName += "_" + enumNameRepeatedIndex;
                            var enumVal = UnrealEngine.Memory.ReadProcessMemory<Int32>(enumArray + i * 0x10 + 0x8);
                            sdkClass.Fields.Add(new Package.SDKClass.SDKFields { Name = enumName, EnumVal = enumVal });
                        }
                    }
                    else if (typeName == "unk")
                    {
                        continue;
                    }
                    else
                    {
                        var field = obj.Address + UEObject.childPropertiesOffset - UEObject.fieldNextOffset;
                        while ((field = UnrealEngine.Memory.ReadProcessMemory<UInt64>(field + UEObject.fieldNextOffset)) > 0)
                        {
                            var fName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + UEObject.fieldNameOffset));
                            var fType = obj.GetFieldType(field);
                            var fValue = "(" + field.ToString() + ")";
                            var offset = (UInt32)obj.GetFieldOffset(field);
                            var gettersetter = "{ get { return new {0}(this[\"{1}\"].Address); } set { this[\"{1}\"] = value; } }";
                            fType = GetTypeFromFieldAddr(fName, fType, field, out gettersetter);
                            //if (typeName == "struct") gettersetter = ";";
                            if (fName == className) fName += "_value";
                            if (fType == "Function")
                            {
                                var func = new Package.SDKClass.SDKFunctions { Name = fName };
                                var fField = field + UEObject.childPropertiesOffset - UEObject.fieldNextOffset;
                                while ((fField = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fField + UEObject.fieldNextOffset)) > 0)
                                {
                                    var pName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(fField + UEObject.fieldNameOffset));
                                    var pType = obj.GetFieldType(fField);
                                    pType = GetTypeFromFieldAddr("", pType, fField, out _);
                                    func.Params.Add(new Package.SDKClass.SDKFields { Name = pName, Type = pType });
                                }
                                sdkClass.Functions.Add(func);
                            }
                            else sdkClass.Fields.Add(new Package.SDKClass.SDKFields { Type = fType, Name = fName, GetterSetter = gettersetter });
                        }
                        if (UEObject.funcNextOffset != UEObject.childrenOffset)
                        {
                            field = obj.Address + UEObject.childrenOffset - UEObject.funcNextOffset;
                            while ((field = UnrealEngine.Memory.ReadProcessMemory<UInt64>(field + UEObject.funcNextOffset)) > 0)
                            {
                                var fName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + UEObject.nameOffset));
                                if (fName == className) fName += "_value";
                                var func = new Package.SDKClass.SDKFunctions { Name = fName };
                                var fField = field + UEObject.childPropertiesOffset - UEObject.fieldNextOffset;
                                while ((fField = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fField + UEObject.fieldNextOffset)) > 0)
                                {
                                    var pName = UEObject.GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(fField + UEObject.fieldNameOffset));
                                    var pType = obj.GetFieldType(fField);
                                    pType = GetTypeFromFieldAddr("", pType, fField, out _);
                                    func.Params.Add(new Package.SDKClass.SDKFields { Name = pName, Type = pType });
                                }
                                sdkClass.Functions.Add(func);
                            }
                        }
                    }
                    sdkPackage.Classes.Add(sdkClass);
                }
                dumpedPackages.Add(sdkPackage);
            }
            foreach (var p in dumpedPackages)
            {
                p.Dependencies = new List<Package>();
                foreach (var c in p.Classes)
                {
                    {
                        var fromPackage = dumpedPackages.Find(tp => tp.Classes.Count(tc => tc.Name == c.Parent) > 0);
                        if (fromPackage != null && fromPackage != p && !p.Dependencies.Contains(fromPackage)) p.Dependencies.Add(fromPackage);
                    }
                    foreach (var f in c.Fields)
                    {
                        var fromPackage = dumpedPackages.Find(tp => tp.Classes.Count(tc => tc.Name == f.Type?.Replace("Array<", "").Replace(">", "")) > 0);
                        if (fromPackage != null && fromPackage != p && !p.Dependencies.Contains(fromPackage)) p.Dependencies.Add(fromPackage);
                    }
                    foreach (var f in c.Functions)
                    {
                        foreach (var param in f.Params)
                        {
                            var fromPackage = dumpedPackages.Find(tp => tp.Classes.Count(tc => tc.Name == param.Type?.Replace("Array<", "").Replace(">", "")) > 0);
                            if (fromPackage != null && fromPackage != p && !p.Dependencies.Contains(fromPackage)) p.Dependencies.Add(fromPackage);
                        }
                    }
                }
            }
            foreach (var p in dumpedPackages)
            {
                var sb = new StringBuilder();
                sb.AppendLine("using UnrealSharp;");
                sb.AppendLine("using Object = UnrealSharp.UEObject;");
                foreach(var d in p.Dependencies) sb.AppendLine("using SDK" + d.FullName.Replace("/", ".") + "SDK;");
                sb.AppendLine("namespace SDK" + p.FullName.Replace("/", ".") + "SDK");
                sb.AppendLine("{");
                var printedClasses = 0;
                foreach(var c in p.Classes)
                {
                    if (c.Fields.Count > 0) printedClasses++;
                   // sb.AppendLine("    [Namespace(\"" + c.Namespace + "\")]");
                    sb.AppendLine("    public " + c.SdkType + " " + c.Name + ((c.Parent == null) ? "" : ( " : " + c.Parent)));
                    sb.AppendLine("    {");
                    if (c.SdkType != "enum")
                        sb.AppendLine("        public " + c.Name + "(ulong addr) : base(addr) { }");
                    foreach (var f in c.Fields)
                    {
                        if (f.Name == "RelatedPlayerState") continue; // todo fix
                        if (c.SdkType == "enum")
                            sb.AppendLine("        " + f.Name + " = " + f.EnumVal + ",");
                        else
                            sb.AppendLine("        public " + f.Type + " " + f.Name + " " + f.GetterSetter);
                    }
                    foreach (var f in c.Functions)
                    {
                        if (f.Name == "ClientReceiveLocalizedMessage") continue; // todo fix
                        var returnType = f.Params.FirstOrDefault(pa => pa.Name == "ReturnValue")?.Type ?? "void";
                        var parameters = String.Join(", ", f.Params.FindAll(pa => pa.Name != "ReturnValue").Select(pa => pa.Type + " " + pa.Name));
                        var args = f.Params.FindAll(pa => pa.Name != "ReturnValue").Select(pa => pa.Name).ToList();
                        args.Insert(0, "nameof(" + f.Name + ")");
                        var argList = String.Join(", ", args);
                        var returnTypeTemplate = returnType == "void" ? "" : ("<" + returnType + ">");
                        sb.AppendLine("        public " + returnType + " " + f.Name + "(" + parameters + ") { " + (returnType == "void" ? "" : "return ") + "Invoke" + returnTypeTemplate + "(" + argList + "); }");
                    }
                    sb.AppendLine("    }");
                }
                sb.AppendLine("}");
                if (printedClasses == 0 && !dumpedPackages.Any(pack => pack.Dependencies.Contains(p)))
                    continue;
                System.IO.File.WriteAllText(location + @"\" + p.Name + ".cs", sb.ToString());
            }
        }
    }
    public class Array<T> : UEObject
    {
        public Array(UInt64 addr) : base(addr) { }
        public UInt32 Num
        {
            get
            {
                if (_num != UInt32.MaxValue) return _num;
                _num = UnrealEngine.Memory.ReadProcessMemory<UInt32>(Address + 8);
                if (_num > 0x20000) _num = 0x20000;
                return _num;
            }
        }
        public Byte[] ArrayCache
        {
            get
            {
                if (_arrayCache.Length != 0) return _arrayCache;
                _arrayCache = UnrealEngine.Memory.ReadProcessMemory(Value, (Int32)Num * 8);
                return _arrayCache;
            }
        }
        public T this[UInt32 index] { get { return (T)Activator.CreateInstance(typeof(T), BitConverter.ToUInt64(ArrayCache, (Int32)index * 8)); } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class NamespaceAttribute : Attribute
    {
        public string name;
        public NamespaceAttribute(string name)
        {
            this.name = name;
        }
    }
    public class UEObject
    {
        public static UInt32 objectOuterOffset = 0x20;
        public static UInt32 classOffset = 0x10;
        public static UInt32 nameOffset = 0x18;
        public static UInt32 structSuperOffset = 0x40;
        public static UInt32 childPropertiesOffset = 0x50;
        public static UInt32 childrenOffset = 0x48;
        public static UInt32 fieldNameOffset = 0x28;
        public static UInt32 fieldTypeNameOffset = 0;
        public static UInt32 fieldClassOffset = 0x8;
        public static UInt32 fieldNextOffset = 0x20;
        public static UInt32 funcNextOffset = 0x20;
        public static UInt32 fieldOffset = 0x4C;
        public static UInt32 propertySize = 0x78;
        public static UInt32 vTableFuncNum = 66;
        public static UInt32 funcFlagsOffset = 0xB0;
        public static UInt32 enumArrayOffset = 0x40;
        public static UInt32 enumCountOffset = 0x48;

        static ConcurrentDictionary<UInt64, String> AddrToName = new ConcurrentDictionary<UInt64, String>();
        static ConcurrentDictionary<UInt64, UInt64> AddrToClass = new ConcurrentDictionary<UInt64, UInt64>();
        static ConcurrentDictionary<String, Boolean> ClassIsSubClass = new ConcurrentDictionary<String, Boolean>();
        static ConcurrentDictionary<String, UInt64> ClassToAddr = new ConcurrentDictionary<String, UInt64>();
        static ConcurrentDictionary<UInt64, ConcurrentDictionary<String, UInt64>> ClassFieldToAddr = new ConcurrentDictionary<UInt64, ConcurrentDictionary<String, UInt64>>();
        static ConcurrentDictionary<UInt64, Int32> FieldAddrToOffset = new ConcurrentDictionary<UInt64, Int32>();
        static ConcurrentDictionary<UInt64, String> FieldAddrToType = new ConcurrentDictionary<UInt64, String>();
        public static void ClearCache()
        {
            AddrToName.Clear();
            AddrToClass.Clear();
            ClassIsSubClass.Clear();
            ClassToAddr.Clear();
            ClassFieldToAddr.Clear();
            FieldAddrToOffset.Clear();
            FieldAddrToType.Clear();
        }
        public Int32 GetFieldOffset(UInt64 fieldAddr)
        {
            if (FieldAddrToOffset.ContainsKey(fieldAddr)) return FieldAddrToOffset[fieldAddr];
            var offset = UnrealEngine.Memory.ReadProcessMemory<Int32>(fieldAddr + fieldOffset);
            FieldAddrToOffset[fieldAddr] = offset;
            return offset;
        }
        String _className;
        public String ClassName
        {
            get
            {
                if (_className != null) return _className;
                _className = GetFullPath();// GetFullName(ClassAddr);
                return _className;
            }
        }
        public UInt64 _classAddr = UInt64.MaxValue;
        public UInt64 ClassAddr
        {
            get
            {
                if (_classAddr != UInt64.MaxValue) return _classAddr;
                if (AddrToClass.ContainsKey(Address))
                {
                    _classAddr = AddrToClass[Address];
                    return _classAddr;
                }
                _classAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address + classOffset);
                AddrToClass[Address] = _classAddr;
                return _classAddr;
            }
        }
        public UEObject(UInt64 address)
        {
            Address = address;
        }
        public Boolean IsA(UInt64 entityClassAddr, String targetClassName)
        {
            var key = entityClassAddr + ":" + targetClassName;
            if (ClassIsSubClass.ContainsKey(key)) return ClassIsSubClass[key];
            var tempEntityClassAddr = entityClassAddr;
            while (true)
            {
                var tempEntity = new UEObject(tempEntityClassAddr);
                var className = tempEntity.GetFullPath();
                if (className == targetClassName)
                {
                    ClassIsSubClass[key] = true;
                    return true;
                }
                tempEntityClassAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(tempEntityClassAddr + structSuperOffset);
                if (tempEntityClassAddr == 0) break;
            }
            ClassIsSubClass[key] = false;
            return false;
        }
        public Boolean IsA(String className)
        {
            return IsA(ClassAddr, className);
        }
        public Boolean IsA<T>(out T converted) where T : UEObject
        {
            var n = typeof(T).Namespace;
            n = n.Substring(3, n.Length - 6).Replace(".", "/");
            n = "Class " + n + "." + typeof(T).Name;
            converted = As<T>();
            return IsA(ClassAddr, n);
        }
        public Boolean IsA<T>() where T : UEObject
        {
            if (Address == 0) return false;
            return IsA<T>(out _);
        }
        public static Boolean NewFName = true;
        public static String GetName(Int32 key)
        {
            if (!NewFName) return GetNameOld(key);
            var namePtr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(UnrealEngine.GNames + (UInt32)((key >> 16) + 2) * 8);
            if (namePtr == 0) return "badIndex";
            var nameEntry = UnrealEngine.Memory.ReadProcessMemory<UInt16>(namePtr + (((UInt16)key) * 2u));
            var nameLength = (Int32)(nameEntry >> 6);
            if (nameLength <= 0) return "badIndex";

            UnrealEngine.Memory.maxStringLength = nameLength;
            string result = UnrealEngine.Memory.ReadProcessMemory<String>(namePtr + ((UInt16)key) * 2u + 2u);
            UnrealEngine.Memory.maxStringLength = 0x100;
            return result;
        }
        public String GetName()
        {
            return GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(Address + nameOffset));
        }
        public static String GetNameOld(Int32 i)
        {
            var fNamePtr = UnrealEngine.Memory.ReadProcessMemory<ulong>(UnrealEngine.GNames + ((UInt64)i / 0x4000) * 8);
            var fName2 = UnrealEngine.Memory.ReadProcessMemory<ulong>(fNamePtr + (8 * ((UInt64)i % 0x4000)));
            var fName3 = UnrealEngine.Memory.ReadProcessMemory<String>(fName2 + 0x10);
            return fName3;
        }
        public String GetShortName()
        {
            var classNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(ClassAddr + nameOffset);
            return GetName(classNameIndex);
        }
        public String GetFullPath()
        {
            if (AddrToName.ContainsKey(Address)) return AddrToName[Address];
            var classPtr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address + classOffset);
            var classNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(classPtr + nameOffset);
            var name = GetName(classNameIndex);
            UInt64 outerEntityAddr = Address;
            var parentName = "";
            while (true)
            {
                var tempOuterEntityAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(outerEntityAddr + objectOuterOffset);
                //var tempOuterEntityAddr = Memory.ReadProcessMemory<UInt64>(outerEntityAddr + structSuperOffset);
                if (tempOuterEntityAddr == outerEntityAddr || tempOuterEntityAddr == 0) break;
                outerEntityAddr = tempOuterEntityAddr;
                var outerNameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(outerEntityAddr + nameOffset);
                var tempName = GetName(outerNameIndex);
                if (tempName == "") break;
                if (tempName == "None") break;
                parentName = tempName + "." + parentName;
            }
            name += " " + parentName;
            var nameIndex = UnrealEngine.Memory.ReadProcessMemory<Int32>(Address + nameOffset);
            name += GetName(nameIndex);
            AddrToName[Address] = name;
            return name;
        }
        public String GetHierachy()
        {
            var sb = new StringBuilder();
            var tempEntityClassAddr = ClassAddr;
            while (true)
            {
                var tempEntity = new UEObject(tempEntityClassAddr);
                var className = tempEntity.GetFullPath();
                sb.AppendLine(className);
                tempEntityClassAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(tempEntityClassAddr + structSuperOffset);
                if (tempEntityClassAddr == 0) break;
            }
            return sb.ToString();
        }
        public String GetFieldType(UInt64 fieldAddr)
        {
            if (FieldAddrToType.ContainsKey(fieldAddr)) return FieldAddrToType[fieldAddr];
            var fieldType = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fieldAddr + fieldClassOffset);
            //var name = GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(fieldType + (NewFName ? 0 : fieldNameOffset)));
            var name = GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(fieldType + fieldNameOffset));
            FieldAddrToType[fieldAddr] = name;
            return name;
        }
        UInt64 GetFieldAddr(UInt64 origClassAddr, UInt64 classAddr, String fieldName)
        {
            if (ClassFieldToAddr.ContainsKey(origClassAddr) && ClassFieldToAddr[origClassAddr].ContainsKey(fieldName)) return ClassFieldToAddr[origClassAddr][fieldName];
            var field = classAddr + childPropertiesOffset - fieldNextOffset;
            while ((field = UnrealEngine.Memory.ReadProcessMemory<UInt64>(field + fieldNextOffset)) > 0)
            {
                var fName = GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + fieldNameOffset));
                if (fName == fieldName)
                {
                    if (!ClassFieldToAddr.ContainsKey(origClassAddr))
                        ClassFieldToAddr[origClassAddr] = new ConcurrentDictionary<String, UInt64>();
                    ClassFieldToAddr[origClassAddr][fieldName] = field;
                    return field;
                }
            }
            var parentClass = UnrealEngine.Memory.ReadProcessMemory<UInt64>(classAddr + structSuperOffset);
            //if (parentClass == classAddr) throw new Exception("parent is me");
            if (parentClass == 0)
            {
                if (!ClassFieldToAddr.ContainsKey(origClassAddr))
                    ClassFieldToAddr[origClassAddr] = new ConcurrentDictionary<String, UInt64>();
                ClassFieldToAddr[origClassAddr][fieldName] = 0;
                return 0;
            }
            return GetFieldAddr(origClassAddr, parentClass, fieldName);
        }
        public UInt64 GetFieldAddr(String fieldName)
        {
            return GetFieldAddr(ClassAddr, ClassAddr, fieldName);
        }
        public UInt64 GetFuncAddr(UInt64 origClassAddr, UInt64 classAddr, String fieldName)
        {
            if (!NewFName) return GetFieldAddr(origClassAddr, classAddr, fieldName);
            if (ClassFieldToAddr.ContainsKey(origClassAddr) && ClassFieldToAddr[origClassAddr].ContainsKey(fieldName)) return ClassFieldToAddr[origClassAddr][fieldName];
            if (UEObject.funcNextOffset == UEObject.childrenOffset) return GetFieldAddr(origClassAddr, classAddr, fieldName);
            var field = classAddr + childrenOffset - funcNextOffset;
            while ((field = UnrealEngine.Memory.ReadProcessMemory<UInt64>(field + funcNextOffset)) > 0)
            {
                var fName = GetName(UnrealEngine.Memory.ReadProcessMemory<Int32>(field + nameOffset));
                if (fName == fieldName)
                {
                    if (!ClassFieldToAddr.ContainsKey(origClassAddr))
                        ClassFieldToAddr[origClassAddr] = new ConcurrentDictionary<String, UInt64>();
                    ClassFieldToAddr[origClassAddr][fieldName] = field;
                    return field;
                }
            }
            var parentClass = UnrealEngine.Memory.ReadProcessMemory<UInt64>(classAddr + structSuperOffset);
            if (parentClass == classAddr) throw new Exception("parent is me");
            if (parentClass == 0) throw new Exception("bad field");
            return GetFuncAddr(origClassAddr, parentClass, fieldName);
        }
        public UInt32 FieldOffset;
        public Byte[] Data;
        public UInt64 _value = 0xdeadbeef0badf00d;
        public UInt64 Value
        {
            get
            {
                if (_value != 0xdeadbeef0badf00d) return _value;
                _value = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address);
                return _value;
            }
            set
            {
                _value = 0xdeadbeef0badf00d;
                UnrealEngine.Memory.WriteProcessMemory(Address, value);
            }
        }

        public T GetValue<T>()
        {
            return UnrealEngine.Memory.ReadProcessMemory<T>(Address);
        }
        public void SetValue<T>(T value)
        {
            UnrealEngine.Memory.WriteProcessMemory<T>(Address, value);
        }
        UInt64 boolMask = 0;
        public Boolean Flag
        {
            get
            {
                var val = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address);
                return ((val & boolMask) == boolMask);
            }
            set
            {
                var val = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address);
                if (value) val |= boolMask;
                else val &= ~boolMask;
                UnrealEngine.Memory.WriteProcessMemory(Address, val);
                //UnrealEngine.Memory.WriteProcessMemory(Address, value);
            }

        }
        public UInt64 Address;
        public UEObject this[String key]
        {
            get
            {
                var fieldAddr = GetFieldAddr(key);
                if (fieldAddr == 0) return null;
                var fieldType = GetFieldType(fieldAddr);
                var offset = (UInt32)GetFieldOffset(fieldAddr);
                UEObject obj;
                if (fieldType == "ObjectProperty" || fieldType == "ScriptStruct")
                    obj = new UEObject(UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address + offset)) { FieldOffset = offset };
                else if (fieldType == "ArrayProperty")
                {
                    obj = new UEObject(Address + offset);
                    obj._classAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fieldAddr + propertySize);
                }
                else if (fieldType.Contains("Bool"))
                {
                    obj = new UEObject(Address + offset);
                    obj._classAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fieldAddr + classOffset);
                    obj.boolMask = UnrealEngine.Memory.ReadProcessMemory<Byte>(fieldAddr + propertySize);
                }
                else if (fieldType.Contains("Function"))
                {
                    obj = new UEObject(fieldAddr);
                    //obj.BaseObjAddr = Address;
                }
                else if (fieldType.Contains("StructProperty"))
                {
                    obj = new UEObject(Address + offset);
                    obj._classAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fieldAddr + propertySize);
                }
                else if (fieldType.Contains("FloatProperty"))
                {
                    obj = new UEObject(Address + offset);
                    obj._classAddr = 0;
                }
                else
                {
                    obj = new UEObject(Address + offset);
                    obj._classAddr = UnrealEngine.Memory.ReadProcessMemory<UInt64>(fieldAddr + propertySize);
                }
                if (obj.Address == 0)
                {
                    obj = new UEObject(0);
                    //var classInfo = Engine.Instance.DumpClass(ClassAddr);
                    //throw new Exception("bad addr");
                }
                return obj;
            }
            set
            {
                var fieldAddr = GetFieldAddr(key);
                var offset = (UInt32)GetFieldOffset(fieldAddr);
                UnrealEngine.Memory.WriteProcessMemory(Address + offset, value.Address);
            }
        }
        public UInt32 _num = UInt32.MaxValue;
        public UInt32 Num
        {

            get
            {
                if (_num != UInt32.MaxValue) return _num;
                _num = UnrealEngine.Memory.ReadProcessMemory<UInt32>(Address + 8);
                if (_num > 0x10000) _num = 0x10000;
                return _num;
            }
        }
        public Byte[] _arrayCache = new Byte[0];
        public Byte[] ArrayCache
        {
            get
            {
                if (_arrayCache.Length != 0) return _arrayCache;
                _arrayCache = UnrealEngine.Memory.ReadProcessMemory(Value, (Int32)Num * 8);
                return _arrayCache;
            }
        }
        public UEObject this[UInt32 index] { get { return new UEObject(BitConverter.ToUInt64(ArrayCache, (Int32)index * 8)); } }
        public UInt64 _vTableFunc = 0xdeadbeef0badf00d;
        public UInt64 VTableFunc
        {
            get
            {
                if (_vTableFunc != 0xdeadbeef0badf00d) return _vTableFunc;
                _vTableFunc = UnrealEngine.Memory.ReadProcessMemory<UInt64>(Address) + vTableFuncNum * 8;
                _vTableFunc = UnrealEngine.Memory.ReadProcessMemory<UInt64>(_vTableFunc);
                return _vTableFunc;
            }
        }
        public T Invoke<T>(String funcName, params Object[] args)
        {
            var funcAddr = GetFuncAddr(ClassAddr, ClassAddr, funcName);
            var initFlags = UnrealEngine.Memory.ReadProcessMemory<UInt64>((UInt64)funcAddr + funcFlagsOffset);
            var nativeFlag = initFlags;
            nativeFlag |= 0x400;
            if (nativeFlag != initFlags) UnrealEngine.Memory.WriteProcessMemory((UInt64)funcAddr + funcFlagsOffset, BitConverter.GetBytes(nativeFlag));
            var val = UnrealEngine.Memory.ExecuteUEFunc<T>((IntPtr)VTableFunc, (IntPtr)Address, (IntPtr)funcAddr, args);
            if (nativeFlag != initFlags) UnrealEngine.Memory.WriteProcessMemory((UInt64)funcAddr + funcFlagsOffset, BitConverter.GetBytes(initFlags));
            return val;
        }
        public void Invoke(String funcName, params Object[] args)
        {
            Invoke<UInt64>(funcName, args);
        }
        public T As<T>() where T : UEObject
        {
            var obj = (T)Activator.CreateInstance(typeof(T), Address);
            obj._classAddr = _classAddr;
            return obj;
        }
    }
}