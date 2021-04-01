using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace UnrealSharp
{
    public class UnrealEngine
    {
        public static UnrealEngine Instance;
        public static UInt64 GNames;
        public static UInt64 GObjects;
        public static UInt64 GWorldPtr;
        public static UInt64 GEngine;
        public static Memory Memory;
        public UnrealEngine(Memory mem) { Memory = mem; Instance = this; }
        public void UpdateAddresses()
        {
            {
                var GNamesPattern = (UInt64)Memory.FindPattern("48 8D 0D ? ? ? ? E8 ? ? ? ? C6 05 ? ? ? ? 01 0F 10 03 4C 8D 44 24 20 48 8B C8");
                //GNamesPattern = (UInt64)Memory.FindPattern("74 09 48 8D 15 ? ? ? ? EB 16");
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
                var GWorldPattern = (UInt64)Memory.FindPattern("48 8B 1D ? ? ? ? 48 85 DB 74 3B 41 B0 01");
                var GObjectsPattern = (UInt64)Memory.FindPattern("C1 F9 10 48 63 C9 48 8D 14 40 48 8B 05");
                //DumpGNames();

                var offset = UnrealEngine.Memory.ReadProcessMemory<UInt32>(GWorldPattern + 3);
                GWorldPtr = GWorldPattern + offset + 7;
                UpdateUEObject();

                offset = Memory.ReadProcessMemory<UInt32>(GObjectsPattern + 13);
                GObjects = GObjectsPattern + offset + 17 - Memory.BaseAddress;
            }
            {
                var GEnginePattern = (UInt64)Memory.FindPattern("48 8B 0D ?? ?? ?? ?? 48 85 C9 74 1E 48 8B 01 FF 90");
                var offset = Memory.ReadProcessMemory<UInt32>(GEnginePattern + 3);
                GEngine = Memory.ReadProcessMemory<UInt64>(GEnginePattern + offset + 7);
            }
            if (false)
            {
                var engine = new UEObject(GEngine);
                var staticCtor = (UInt64)Memory.FindPattern("4C 89 44 24 18 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4");
                var console = new UEObject(Memory.Execute(staticCtor, engine["ConsoleClass"].Value, engine["GameViewport"].Address, 0, 0, 0, 0, 0, 0, 0));
                engine["GameViewport"]["ViewportConsole"] = console;
            }
            //DumpSdk();
        }
        public void UpdateUEObject()
        {
            var world = Memory.ReadProcessMemory<UInt64>(GWorldPtr);
            {
                var classPtr = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var foundClassAndName = false;
                for(var c = 0u; c < 0x50 && !foundClassAndName; c+= 0x8)
                {
                    classPtr = Memory.ReadProcessMemory<UInt64>(world + c);
                    if (classPtr == 0x0) continue;
                    for (var n = 0u; n < 0x50 && !foundClassAndName; n+= 0x8)
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
                        UEObject.childrenOffset = UEObject.childPropertiesOffset;
                        foundFuncs = true;
                    }
                }
                if (!foundFuncs) throw new Exception("bad childs offset");
            }
            {
                var foundNextField = false;
                var classPtr  = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
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
                var foundNextField = false;
                var classPtr  = Memory.ReadProcessMemory<UInt64>(world + UEObject.classOffset);
                var fieldPtr = Memory.ReadProcessMemory<UInt64>(classPtr + UEObject.childPropertiesOffset);
                for (var c = 0u; c < 0x80 && !foundNextField; c += 0x8)
                {
                    var childClassPtr = Memory.ReadProcessMemory<UInt64>(fieldPtr + c);
                    if (childClassPtr == 0x0) continue;
                    var classNameOffset = UEObject.NewFName ? 0 : UEObject.fieldNameOffset;
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
                for (var i = 0u; i < 0x200 && !foundFuncFlags; i+=8)
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
        public void DumpSdk()
        {
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
            foreach(var package in packages)
            {
                var sb = new StringBuilder();
                var packageObj = new UEObject(package.Key);
                var packageName = packageObj.GetName();
                packageName = packageName.Substring(packageName.LastIndexOf("/") + 1);
                var dumpedClasses = new List<String>();
                foreach(var objAddr in package.Value)
                {
                    var obj = new UEObject(objAddr);
                    if (dumpedClasses.Contains(obj.ClassName)) continue;
                    dumpedClasses.Add(obj.ClassName);
                    sb.AppendLine(obj.ClassName);
                }
                System.IO.File.WriteAllText(Memory.Process.ProcessName + @"\" + packageName + ".cs", sb.ToString());
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
                var offset = Memory.ReadProcessMemory<Int32>(fieldAddr + fieldOffset);
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
            UInt64 _classAddr = UInt64.MaxValue;
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
            public String ClassHiereachy()
            {
                var hierarchy = "";
                var tempEntityClassAddr = ClassAddr;
                while (true)
                {
                    var tempEntity = new UEObject(tempEntityClassAddr);
                    hierarchy += " | " + tempEntity.GetFullPath();
                    tempEntityClassAddr = Memory.ReadProcessMemory<UInt64>(tempEntityClassAddr + structSuperOffset);
                    if (tempEntityClassAddr == 0) break;
                }
                return hierarchy;
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
                    tempEntityClassAddr = Memory.ReadProcessMemory<UInt64>(tempEntityClassAddr + structSuperOffset);
                    if (tempEntityClassAddr == 0) break;
                }
                ClassIsSubClass[key] = false;
                return false;
            }
            public Boolean IsA(String className)
            {
                return IsA(ClassAddr, className);
            }
            public static Boolean NewFName = true;
            public static String GetName(Int32 key)
            {
                if (!NewFName) return GetNameOld(key);
                var namePtr = Memory.ReadProcessMemory<UInt64>(GNames + (UInt32)((key >> 16) + 2) * 8);
                if (namePtr == 0) return "badIndex";
                var nameEntry = Memory.ReadProcessMemory<UInt16>(namePtr + (((UInt16)key) * 2u));
                var nameLength = (Int32)(nameEntry >> 6);
                if (nameLength <= 0) return "badIndex";

                Memory.maxStringLength = nameLength;
                string result = Memory.ReadProcessMemory<String>(namePtr + ((UInt16)key) * 2u + 2u);
                Memory.maxStringLength = 0x100;
                return result;
            }
            public String GetName()
            {
                return GetName(Memory.ReadProcessMemory<Int32>(Address + nameOffset));
            }
            public static String GetNameOld(Int32 i)
            {
                var fNamePtr = Memory.ReadProcessMemory<ulong>(GNames + ((UInt64)i / 0x4000) * 8);
                var fName2 = Memory.ReadProcessMemory<ulong>(fNamePtr + (8 * ((UInt64)i % 0x4000)));
                var fName3 = Memory.ReadProcessMemory<String>(fName2 + 0xc);
                return fName3;
            }
            public String GetShortName()
            {
                var classNameIndex = Memory.ReadProcessMemory<Int32>(ClassAddr + nameOffset);
                return GetName(classNameIndex);
            }
            public String GetFullPath()
            {
                if (AddrToName.ContainsKey(Address)) return AddrToName[Address];
                var classPtr = Memory.ReadProcessMemory<UInt64>(Address + classOffset);
                var classNameIndex = Memory.ReadProcessMemory<Int32>(classPtr + nameOffset);
                var name = GetName(classNameIndex);
                UInt64 outerEntityAddr = Address;
                var parentName = "";
                while (true)
                {
                    var tempOuterEntityAddr = Memory.ReadProcessMemory<UInt64>(outerEntityAddr + objectOuterOffset);
                    //var tempOuterEntityAddr = Memory.ReadProcessMemory<UInt64>(outerEntityAddr + structSuperOffset);
                    if (tempOuterEntityAddr == outerEntityAddr || tempOuterEntityAddr == 0) break;
                    outerEntityAddr = tempOuterEntityAddr;
                    var outerNameIndex = Memory.ReadProcessMemory<Int32>(outerEntityAddr + nameOffset);
                    var tempName = GetName(outerNameIndex);
                    if (tempName == "") break;
                    if (tempName == "None") break;
                    parentName = tempName + "." + parentName;
                }
                name += " " + parentName;
                var nameIndex = Memory.ReadProcessMemory<Int32>(Address + nameOffset);
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
                    tempEntityClassAddr = Memory.ReadProcessMemory<UInt64>(tempEntityClassAddr + structSuperOffset);
                    if (tempEntityClassAddr == 0) break;
                }
                return sb.ToString();
            }
            public String GetFieldType(UInt64 fieldAddr)
            {
                if (FieldAddrToType.ContainsKey(fieldAddr)) return FieldAddrToType[fieldAddr];
                var fieldType = Memory.ReadProcessMemory<UInt64>(fieldAddr + fieldClassOffset);
                var name = GetName(Memory.ReadProcessMemory<Int32>(fieldType + (NewFName ? 0 : fieldNameOffset)));
                FieldAddrToType[fieldAddr] = name;
                return name;
            }
            UInt64 GetFieldAddr(UInt64 origClassAddr, UInt64 classAddr, String fieldName)
            {
                if (ClassFieldToAddr.ContainsKey(origClassAddr) && ClassFieldToAddr[origClassAddr].ContainsKey(fieldName)) return ClassFieldToAddr[origClassAddr][fieldName];
                var field = classAddr + childPropertiesOffset - fieldNextOffset;
                while ((field = Memory.ReadProcessMemory<UInt64>(field + fieldNextOffset)) > 0)
                {
                    var fName = GetName(Memory.ReadProcessMemory<Int32>(field + fieldNameOffset));
                    if (fName == fieldName)
                    {
                        if (!ClassFieldToAddr.ContainsKey(origClassAddr))
                            ClassFieldToAddr[origClassAddr] = new ConcurrentDictionary<String, UInt64>();
                        ClassFieldToAddr[origClassAddr][fieldName] = field;
                        return field;
                    }
                }
                var parentClass = Memory.ReadProcessMemory<UInt64>(classAddr + structSuperOffset);
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
                var field = classAddr + childrenOffset - funcNextOffset;
                while ((field = Memory.ReadProcessMemory<UInt64>(field + funcNextOffset)) > 0)
                {
                    var fName = GetName(Memory.ReadProcessMemory<Int32>(field + nameOffset));
                    if (fName == fieldName)
                    {
                        if (!ClassFieldToAddr.ContainsKey(origClassAddr))
                            ClassFieldToAddr[origClassAddr] = new ConcurrentDictionary<String, UInt64>();
                        ClassFieldToAddr[origClassAddr][fieldName] = field;
                        return field;
                    }
                }
                var parentClass = Memory.ReadProcessMemory<UInt64>(classAddr + structSuperOffset);
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
                        return null;
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
                    if (_num > 0x1000) _num = 0x1000;
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
                UnrealEngine.Memory.WriteProcessMemory((UInt64)funcAddr + funcFlagsOffset, BitConverter.GetBytes(nativeFlag));
                var val = UnrealEngine.Memory.ExecuteUEFunc<T>((IntPtr)VTableFunc, (IntPtr)Address, (IntPtr)funcAddr, args);
                UnrealEngine.Memory.WriteProcessMemory((UInt64)funcAddr + funcFlagsOffset, BitConverter.GetBytes(initFlags));
                return val;
            }
        }
    }
}