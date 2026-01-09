using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JALib.Tools;
using UnityEngine;
using UnityModManagerNet;

namespace JALib.Core;

static class JALogger {
    private const string NextInfo = " | ";
    private const string LogTypeInfo = "INFO";
    private const string LogTypeWarn = "WARN";
    private const string LogTypeCritical = "CRITICAL";
    private const string LogTypeError = "ERROR";
    private const string LogTypeException = "Exception";
    private static bool _initialized;
    private static List<string> _buffer;
    private static List<string> _history;
    private static List<LogCache> _logCache;
    private static int _historyCapacity;
    private static CancellationTokenSource cancellationToken;
    private static bool _removingCache;

    static JALogger() {
        try {
            _buffer = (List<string>) typeof(UnityModManager.Logger).GetValue("buffer");
            _history = (List<string>) typeof(UnityModManager.Logger).GetValue("history");
            _historyCapacity = (int) typeof(UnityModManager.Logger).GetValue("historyCapacity");
            _logCache = new List<LogCache>(_historyCapacity);
            _initialized = true;
        } catch (Exception e) {
            _initialized = false;
            _buffer = _history = null;
            _logCache = null;
            UnityModManager.Logger.LogException("JALogger initialization failed", e, "[JALib] [Exception] ");
        }
        try {
            Harmony harmony = new("JALib.JALogger");
            harmony.Patch(((Delegate) AddStackFrameInfo).Method, transpiler: new HarmonyMethod(((Delegate) AddStackFrameInfoTranspiler).Method));
            harmony.Patch(((Delegate) GetCurrentTask).Method, transpiler: new HarmonyMethod(((Delegate) GetCurrentTaskTranspiler).Method));
        } catch (Exception e) {
            LogExceptionInternal("JALogger transpiler patching failed", e);
        }
        try {
            Task.Run(CacheAutoRemover);
        } catch (Exception e) {
            LogExceptionInternal("JALogger cache remover initialization failed", e);
        }
    }

    private static void CacheAutoRemover() {
        if(_removingCache) return;
        try {
            _removingCache = true;
            if(cancellationToken != null) {
                cancellationToken.Cancel();
                cancellationToken.Dispose();
                cancellationToken = null;
            }
            List<(LogCache, int)> removeIndices = [];
            for(int i = 0; i < _logCache.Count; i++) {
                LogCache cache = _logCache[i];
                if(!_history.Contains(cache.FullString)) 
                    removeIndices.Add((cache, cache.RepeatCount));
            }
            int count = 0;
            lock(_logCache) 
                foreach((LogCache cache, int repeatCount) in removeIndices)
                    if(repeatCount == cache.RepeatCount) {
                        _logCache.Remove(cache);
                        count++;
                    }
            if(count > 0) LogInternal($"JALogger cache removed {count} entries, current cache size: {_logCache.Count}");
            cancellationToken = new CancellationTokenSource();
            Task.Delay(1000, cancellationToken.Token).OnCompleted(CacheAutoRemover);
        } catch (Exception e) {
            LogExceptionInternal("JALogger cache remover failed", e);
        } finally {
            _removingCache = false;
        }
    }

    public static void Log(JAMod mod, string message, int stackTraceSkip) {
        if(_initialized) Write(mod, LogTypeInfo, message, false, stackTraceSkip);
        else UnityModManager.Logger.Log(MakeFullString(mod.Name, LogTypeInfo, message, stackTraceSkip + 2), "");
    }

    public static void NativeLog(JAMod mod, string message, int stackTraceSkip) {
        if(_initialized) Write(mod, LogTypeInfo, message, true, stackTraceSkip);
        else Console.WriteLine(MakeFullString(mod.Name, LogTypeInfo, message, stackTraceSkip + 2));
    }

    public static void Warn(JAMod mod, string message, int stackTraceSkip) {
        if(_initialized) Write(mod, LogTypeWarn, message, false, stackTraceSkip);
        else UnityModManager.Logger.Log(MakeFullString(mod.Name, LogTypeWarn, message, stackTraceSkip + 2), "");
    }

    public static void Critical(JAMod mod, string message, int stackTraceSkip) {
        if(_initialized) Write(mod, LogTypeCritical, message, false, stackTraceSkip);
        else UnityModManager.Logger.Log(MakeFullString(mod.Name, LogTypeCritical, message, stackTraceSkip + 2), "");
    }

    public static void Error(JAMod mod, string message, int stackTraceSkip) {
        if(_initialized) Write(mod, LogTypeError, message, false, stackTraceSkip);
        else UnityModManager.Logger.Log(MakeFullString(mod.Name, LogTypeError, message, stackTraceSkip + 2), "");
    }

    public static void LogException(JAMod mod, string key, Exception ex, int stackTraceSkip) {
        string message = (key == null ? "" : key + ": ") + ex.GetType().Name + " - " + ex.Message;
        if(_initialized) Write(mod, LogTypeException, message, false, stackTraceSkip);
        else UnityModManager.Logger.Log(MakeFullString(mod.Name, LogTypeException, message, stackTraceSkip + 2), "");
        Console.WriteLine(ex);
    }

    private static void Write(JAMod mod, string logType, string message, bool onlyNative, int stackTraceSkip) {
        if(message == null) return;
        DateTime now = DateTime.Now;
        string fullMessage = MakeFullString(mod.Name, logType, message, stackTraceSkip + 3, now);
        Console.WriteLine(fullMessage);
        if(onlyNative) return;
        lock(_buffer) {
            _buffer.Add(fullMessage);
            LogCache current = null;
            lock(_logCache) {
                foreach(LogCache cache in _logCache) {
                    if(!cache.IsSame(mod, message)) continue;
                    current = cache;
                    cache.RepeatCount++;
                    _history.Remove(cache.FullString);
                    break;
                }
                if(current == null) _logCache.Add(current = new LogCache(mod, message));
            }
            string halfMessage = MakeHalfString(mod.Name, logType, message, now, current.RepeatCount);
            current.FullString = halfMessage;
            _history.Add(halfMessage);
            if(!_removingCache && _logCache.Count > _historyCapacity) Task.Run(CacheAutoRemover);
            if(_history.Count < _historyCapacity * 2) return;
            _history.RemoveRange(0, _historyCapacity);
        }
    }

    public static void LogInternal(string message) {
        if(_initialized) WriteInternal(JALib.ModId, LogTypeInfo, message);
        else UnityModManager.Logger.Log(MakeFullString(JALib.ModId, LogTypeInfo, message, 2), "");
    }

    public static void LogExceptionInternal(string key, Exception ex) {
        string message = (key == null ? "" : key + ": ") + ex.GetType().Name + " - " + ex.Message;
        if(_initialized) WriteInternal(JALib.ModId, LogTypeException, message);
        else UnityModManager.Logger.Log(MakeFullString(JALib.ModId, LogTypeException, message, 2), "");
        Console.WriteLine(ex);
    }

    public static void WriteInternal(string modName, string logType, string message) {
        try {
            if(message == null) return;
            DateTime now = DateTime.Now;
            string fullMessage = MakeFullString(modName, logType, message, 3, now);
            Console.WriteLine(fullMessage);
            lock(_buffer) {
                _buffer.Add(fullMessage);
                _history.Add(MakeHalfString(modName, logType, message, now, 0));
            }
        } catch (Exception e) {
            Console.WriteLine(e);
            UnityModManager.Logger.Log(MakeFullString(modName, logType, message, 3), "");
        }
    }

    // [JALib INFO 15:22:35] Listening on port: 60962 [3]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string MakeHalfString(string modName, string logType, string message, DateTime now, int repeatCount) {
        StringBuilder sb = new();
        bool isColored = true;
        switch(logType) {
            case LogTypeWarn:
                sb.Append("<color=yellow>");
                break;
            case LogTypeCritical:
                sb.Append("<color=orange>");
                break;
            case LogTypeError:
                sb.Append("<color=red>");
                break;
            case LogTypeException:
                sb.Append("<color=magenta>");
                break;
            default:
                isColored = false;
                break;
        }
        sb.Append('[').Append(modName).Append(' ').Append(logType).Append(' ').Append(now.ToString("HH:mm:ss")).Append("] ").Append(message);
        if(isColored) sb.Append("</color>");
        if(repeatCount > 1) sb.Append(" <color=gray>[").Append(repeatCount).Append("]</color>");
        return sb.ToString();
    }
    
    // [JALib INFO 15:22:35.807 #1] Listening on port: 60962
    //   ⚡ Thread Pool Worker(6) | Task #1
    //   📍 JALib.API.ApplicatorAPI.Connect() 
    //   📁 V:\JALib\JALib\API\ApplicatorAPI.cs:22
    //
    private static string MakeFullString(string mod, string logType, string message, int stackTraceSkip, DateTime? now = null) {
        now ??= DateTime.Now;
        StringBuilder sb = new(((mod.Length + logType.Length + message.Length) / 16 + 5) * 16);
        sb.Append('[').Append(mod).Append(' ')
            .Append(logType).Append(' ')
            .Append(now.Value.ToString("HH:mm:ss.fff")).Append(" #")
            .Append(Time.frameCount).Append("] ").Append(message);
        int flag = JALib.Instance?.Setting?.loggerLogDetail ?? 7;
        if((flag & 1) == 1) {
            sb.Append("\n  ⚡ ")
                .Append(Thread.CurrentThread.Name ?? "Native Thread").Append('(').Append(Thread.CurrentThread.ManagedThreadId).Append(')');
            if(Thread.CurrentThread.Name == "Thread Pool Worker") {
                Task currentTask = GetCurrentTask();
                sb.Append(NextInfo);
                if(currentTask != null) sb.Append("Task #").Append(currentTask.Id);
                else sb.Append("Unknown Task");
            }
        }
        StackFrame frame = null;
        if((flag & 2) == 2) {
            frame = new StackFrame(stackTraceSkip, true);
            sb.Append("\n  📍 ");
            AddStackFrameInfo(sb, frame);
        }
        if((flag & 4) == 4) {
            frame ??= new StackFrame(stackTraceSkip, true);
            if((object) frame.GetMethod() != null) {
                sb.Append("\n  📁 ");
                string str1;
                try {
                    str1 = frame.GetFileName();
                } catch (SecurityException) {
                    str1 = null;
                }
                if(str1 == null || str1[0] == '<')
                    str1 = $"<{frame.GetMethod().Module.ModuleVersionId:N}>";
                sb.Append(str1).Append(':').Append(frame.GetFileLineNumber());
            }
        }
        if(flag != 0) sb.Append('\n');
        return sb.ToString();
    }

    private static void AddStackFrameInfo(StringBuilder sb, StackFrame frame) {
        MethodBase method = frame.GetMethod();
        if((object) method == null) {
            string internalMethodName = frame.StackFrameDummy<string>(0);
            if(internalMethodName != null) sb.Append(internalMethodName);
            else
                sb.Append('<').AppendJoin("", "0x", frame.StackFrameDummy<long>(1).ToString("x5"), " + 0x", frame.GetNativeOffset().ToString("x5"))
                    .Append("> <unknown method>");
        } else {
            AddType(sb, method.DeclaringType, true);
            sb.Append('.').Append(method.Name);
            if(method.IsGenericMethod) {
                sb.Append('[');
                foreach(Type genericType in method.GetGenericArguments()) {
                    AddType(sb, genericType, false);
                    sb.Append(", ");
                }
                sb.Length -= 2;
                sb.Append(']');
            }
            ParameterInfo[] parameters = method.GetParameters();
            sb.Append('(');
            foreach(ParameterInfo parameter in parameters) {
                AddType(sb, parameter.ParameterType, false);
                if(!parameter.Name.IsNullOrEmpty()) {
                    sb.Append(" ");
                    sb.Append(parameter.Name);
                }
                sb.Append(", ");
            }
            if(parameters.Length > 0) sb.Length -= 2;
            sb.Append(')');
        }
    }

#pragma warning disable CS8509
    private static T StackFrameDummy<T>(this StackFrame frame, int i) {
        return i switch {
            0 => frame.Invoke<T>("GetInternalMethodName"),
            1 => frame.Invoke<T>("GetMethodAddress"),
        };
    }
#pragma warning restore CS8509

    public static IEnumerable<CodeInstruction> AddStackFrameInfoTranspiler(IEnumerable<CodeInstruction> instructions) {
        List<CodeInstruction> codes = new(instructions);
        for(int i = 0; i < codes.Count; i++) {
            CodeInstruction code = codes[i];
            if(code.operand is MethodInfo { Name: "StackFrameDummy" }) {
                CodeInstruction old = codes[i - 1];
                if(old.opcode == OpCodes.Ldc_I4_0)
                    codes[i - 1] = new CodeInstruction(OpCodes.Call, typeof(StackFrame).GetMethod("GetInternalMethodName", BindingFlags.NonPublic | BindingFlags.Instance));
                else if(old.opcode == OpCodes.Ldc_I4_1)
                    codes[i - 1] = new CodeInstruction(OpCodes.Call, typeof(StackFrame).GetMethod("GetMethodAddress", BindingFlags.NonPublic | BindingFlags.Instance));
                codes.RemoveAt(i);
            }
        }
        return codes;
    }

    private static Task GetCurrentTask() => typeof(Task).GetField("t_currentTask", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue<Task>();

    public static IEnumerable<CodeInstruction> GetCurrentTaskTranspiler(IEnumerable<CodeInstruction> instructions) {
        return [
            new CodeInstruction(OpCodes.Ldsfld, typeof(Task).Field("t_currentTask")),
            new CodeInstruction(OpCodes.Ret)
        ];
    }

    private static void AddType(StringBuilder sb, Type type, bool fullName) {
        if(type.IsNested) {
            AddType(sb, type.DeclaringType!, fullName);
            fullName = false;
            sb.Append("+");
        }
        sb.Append(fullName ? type.FullName : type.Name);
        if(type.IsGenericType) {
            sb.Append("[");
            foreach(Type genericType in type.GetGenericArguments()) {
                AddType(sb, genericType, false);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append("]");
        }
    }

    private class LogCache(JAMod mod, string message) {
        public JAMod Mod = mod;
        public string Message = message;
        public string FullString;
        public int RepeatCount = 1;

        public bool IsSame(JAMod mod, string message) {
            return Mod == mod && Message == message;
        }
    }
}