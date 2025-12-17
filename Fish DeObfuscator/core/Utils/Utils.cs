using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Fish_DeObfuscator.core.Utils
{
    public static class Utils
    {
        private static readonly Random random = new Random();
        private static readonly HashSet<string> usedNames = new HashSet<string>();

        public static string GenerateRandomName(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            string name;
            int attempts = 0;
            do
            {
                char[] stringChars = new char[length];
                for (int i = 0; i < length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }
                name = new string(stringChars);
                attempts++;

                if (attempts > 1000)
                {
                    name = name + random.Next(1000, 9999).ToString();
                    break;
                }
            } while (usedNames.Contains(name));

            usedNames.Add(name);
            return name;
        }

        public static string RandomString(int length)
        {
            return GenerateRandomName(length);
        }

        public static bool IsUnitySpecialType(TypeDef type)
        {
            if (type == null)
                return false;

            try
            {
                return type.BaseType?.FullName == "UnityEngine.MonoBehaviour"
                    || type.BaseType?.FullName == "UnityEngine.ScriptableObject"
                    || type.FullName.StartsWith("UnityEngine.")
                    || type.FullName.StartsWith("UnityEditor.")
                    || type.FullName.StartsWith("System.")
                    || type.FullName.StartsWith("Microsoft.");
            }
            catch
            {
                return true;
            }
        }

        public static bool IsEntryPoint(MethodDef method, ModuleDefMD module)
        {
            if (method == null || module == null)
                return false;

            try
            {
                if (module.EntryPoint != null && method == module.EntryPoint)
                {
                    Console.WriteLine($"Protecting entry point method: {method.DeclaringType.FullName}.{method.Name}");
                    return true;
                }

                if (method.Name == "Main" && method.IsStatic)
                {
                    bool isMainSignature = false;

                    if (method.Parameters.Count == 0)
                    {
                        isMainSignature = (method.ReturnType.FullName == "System.Void" || method.ReturnType.FullName == "System.Int32");
                    }
                    else if (method.Parameters.Count == 1)
                    {
                        var param = method.Parameters[0];
                        if (param.Type.FullName == "System.String[]")
                        {
                            isMainSignature = (method.ReturnType.FullName == "System.Void" || method.ReturnType.FullName == "System.Int32");
                        }
                    }

                    if (isMainSignature)
                    {
                        Console.WriteLine($"Protecting Main method: {method.DeclaringType.FullName}.{method.Name}");
                        return true;
                    }
                }

                if (method.HasCustomAttributes)
                {
                    foreach (var attr in method.CustomAttributes)
                    {
                        var attrTypeName = attr.AttributeType.FullName;
                        if (attrTypeName.Contains("DllExport") || attrTypeName.Contains("UnmanagedExport") || attrTypeName.Contains("Export"))
                        {
                            Console.WriteLine($"Protecting exported method: {method.DeclaringType.FullName}.{method.Name}");
                            return true;
                        }
                    }
                }

                if (method.IsPublic && method.DeclaringType.IsPublic)
                {
                    if (method.DeclaringType.HasCustomAttributes)
                    {
                        foreach (var attr in method.DeclaringType.CustomAttributes)
                        {
                            var attrTypeName = attr.AttributeType.FullName;
                            if (attrTypeName.Contains("ComVisible") || attrTypeName.Contains("Guid") || attrTypeName.Contains("ClassInterface"))
                            {
                                Console.WriteLine($"Protecting COM visible method: {method.DeclaringType.FullName}.{method.Name}");
                                return true;
                            }
                        }
                    }

                    if (method.HasCustomAttributes)
                    {
                        foreach (var attr in method.CustomAttributes)
                        {
                            var attrTypeName = attr.AttributeType.FullName;
                            if (attrTypeName.Contains("ComVisible") || attrTypeName.Contains("DispId"))
                            {
                                Console.WriteLine($"Protecting COM visible method: {method.DeclaringType.FullName}.{method.Name}");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking entry point for method {method?.Name}: {ex.Message}");
                return true;
            }
        }

        public static bool IsEntryPointType(TypeDef type, ModuleDefMD module)
        {
            if (type == null || module == null)
                return false;

            try
            {
                if (type.HasMethods)
                {
                    foreach (var method in type.Methods)
                    {
                        if (IsEntryPoint(method, module))
                        {
                            return true;
                        }
                    }
                }

                if (type.HasCustomAttributes)
                {
                    foreach (var attr in type.CustomAttributes)
                    {
                        var attrTypeName = attr.AttributeType.FullName;
                        if (attrTypeName.Contains("ComVisible") || attrTypeName.Contains("Guid") || attrTypeName.Contains("ClassInterface") || attrTypeName.Contains("DllExport"))
                        {
                            Console.WriteLine($"Protecting entry point type: {type.FullName}");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking entry point type {type?.Name}: {ex.Message}");
                return true;
            }
        }

        public static bool IsUnitySpecialMethod(MethodDef method)
        {
            if (method == null)
                return false;

            try
            {
                var unityMethods = Fish_DeObfuscator.Program.UnitySpecialMethods;
                if (unityMethods != null)
                {
                    return Array.IndexOf(unityMethods, method.Name) != -1;
                }
            }
            catch { }

            string[] defaultUnityMethods =
            {
                "Awake",
                "Start",
                "Update",
                "LateUpdate",
                "FixedUpdate",
                "OnEnable",
                "OnDisable",
                "OnDestroy",
                "OnApplicationPause",
                "OnApplicationFocus",
                "OnApplicationQuit",
                "OnTriggerEnter",
                "OnTriggerExit",
                "OnTriggerStay",
                "OnCollisionEnter",
                "OnCollisionExit",
                "OnCollisionStay",
                "OnMouseDown",
                "OnMouseUp",
                "OnMouseEnter",
                "OnMouseExit",
                "OnMouseOver",
                "OnGUI",
                "OnDrawGizmos",
                "OnDrawGizmosSelected",
            };

            return Array.IndexOf(defaultUnityMethods, method.Name) != -1;
        }

        public static void AddUsedName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                usedNames.Add(name);
            }
        }

        public static bool IsIntegerConstant(Instruction instruction)
        {
            if (instruction == null)
                return false;

            return instruction.OpCode == OpCodes.Ldc_I4
                || instruction.OpCode == OpCodes.Ldc_I4_0
                || instruction.OpCode == OpCodes.Ldc_I4_1
                || instruction.OpCode == OpCodes.Ldc_I4_2
                || instruction.OpCode == OpCodes.Ldc_I4_3
                || instruction.OpCode == OpCodes.Ldc_I4_4
                || instruction.OpCode == OpCodes.Ldc_I4_5
                || instruction.OpCode == OpCodes.Ldc_I4_6
                || instruction.OpCode == OpCodes.Ldc_I4_7
                || instruction.OpCode == OpCodes.Ldc_I4_8
                || instruction.OpCode == OpCodes.Ldc_I4_M1
                || instruction.OpCode == OpCodes.Ldc_I4_S;
        }

        public static int GetConstantValue(Instruction instruction)
        {
            if (instruction == null)
                return 0;

            try
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Ldc_I4_M1:
                        return -1;
                    case Code.Ldc_I4_0:
                        return 0;
                    case Code.Ldc_I4_1:
                        return 1;
                    case Code.Ldc_I4_2:
                        return 2;
                    case Code.Ldc_I4_3:
                        return 3;
                    case Code.Ldc_I4_4:
                        return 4;
                    case Code.Ldc_I4_5:
                        return 5;
                    case Code.Ldc_I4_6:
                        return 6;
                    case Code.Ldc_I4_7:
                        return 7;
                    case Code.Ldc_I4_8:
                        return 8;
                    case Code.Ldc_I4:
                    case Code.Ldc_I4_S:
                        if (instruction.Operand != null)
                        {
                            return Convert.ToInt32(instruction.Operand);
                        }
                        return 0;
                    default:
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsSafeToObfuscate(MethodDef method)
        {
            if (method == null)
                return false;

            try
            {
                if (method.IsSpecialName || method.IsConstructor || !method.HasBody)
                    return false;

                if (method.IsRuntimeSpecialName || method.IsAbstract || method.IsVirtual)
                    return false;

                if (IsUnitySpecialMethod(method))
                    return false;

                if (HasComplexControlFlow(method))
                    return false;

                if (method.HasCustomAttributes)
                {
                    foreach (var attr in method.CustomAttributes)
                    {
                        var attrName = attr.AttributeType.Name;
                        if (attrName.Contains("Serialize") || attrName.Contains("DllImport") || attrName.Contains("MonoPInvoke") || attrName.Contains("Unity"))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSafeToObfuscate(MethodDef method, ModuleDefMD module)
        {
            if (!IsSafeToObfuscate(method))
                return false;

            if (IsEntryPoint(method, module))
                return false;

            return true;
        }

        public static bool HasComplexControlFlow(MethodDef method)
        {
            if (method?.Body?.Instructions == null)
                return true;

            try
            {
                int branchCount = 0;
                int switchCount = 0;
                int exceptionHandlerCount = method.Body.ExceptionHandlers?.Count ?? 0;

                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.FlowControl == FlowControl.Branch || instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                    {
                        branchCount++;
                    }

                    if (instruction.OpCode == OpCodes.Switch)
                    {
                        switchCount++;
                    }
                }

                return branchCount > 8 || switchCount > 0 || exceptionHandlerCount > 0;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsInControlFlowContext(List<Instruction> instructions, int index)
        {
            if (instructions == null || index < 0 || index >= instructions.Count)
                return false;

            try
            {
                for (int offset = 1; offset <= 3 && index + offset < instructions.Count; offset++)
                {
                    var nextInstruction = instructions[index + offset];

                    if (nextInstruction.OpCode == OpCodes.Nop || nextInstruction.OpCode == OpCodes.Pop)
                        continue;

                    if (
                        nextInstruction.OpCode.FlowControl == FlowControl.Branch
                        || nextInstruction.OpCode.FlowControl == FlowControl.Cond_Branch
                        || nextInstruction.OpCode == OpCodes.Switch
                        || nextInstruction.OpCode == OpCodes.Br
                        || nextInstruction.OpCode == OpCodes.Br_S
                        || nextInstruction.OpCode == OpCodes.Brfalse
                        || nextInstruction.OpCode == OpCodes.Brfalse_S
                        || nextInstruction.OpCode == OpCodes.Brtrue
                        || nextInstruction.OpCode == OpCodes.Brtrue_S
                    )
                    {
                        return true;
                    }

                    if (nextInstruction.OpCode == OpCodes.Ceq || nextInstruction.OpCode == OpCodes.Cgt || nextInstruction.OpCode == OpCodes.Clt || nextInstruction.OpCode == OpCodes.Cgt_Un || nextInstruction.OpCode == OpCodes.Clt_Un)
                    {
                        return true;
                    }

                    break;
                }

                return false;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsConstantSafeToReplace(int value, List<Instruction> instructions, int index)
        {
            if (instructions == null || index < 0)
                return false;

            try
            {
                if (value == 0 || value == 1 || value == -1)
                    return false;

                if (Math.Abs(value) > 10000)
                    return false;

                if (IsInControlFlowContext(instructions, index))
                    return false;

                if (index + 1 < instructions.Count)
                {
                    var nextInstruction = instructions[index + 1];
                    if (
                        nextInstruction.OpCode == OpCodes.Ldelem_I1
                        || nextInstruction.OpCode == OpCodes.Ldelem_I2
                        || nextInstruction.OpCode == OpCodes.Ldelem_I4
                        || nextInstruction.OpCode == OpCodes.Ldelem_I8
                        || nextInstruction.OpCode == OpCodes.Ldelem_Ref
                        || nextInstruction.OpCode == OpCodes.Stelem_I1
                        || nextInstruction.OpCode == OpCodes.Stelem_I2
                        || nextInstruction.OpCode == OpCodes.Stelem_I4
                        || nextInstruction.OpCode == OpCodes.Stelem_I8
                        || nextInstruction.OpCode == OpCodes.Stelem_Ref
                    )
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int ProcessMethodConstantsSafely(MethodDef method, ModuleDefMD module, Func<Instruction, int, bool> replaceFunction)
        {
            if (!IsSafeToObfuscate(method, module) || replaceFunction == null)
                return 0;

            try
            {
                var instructions = method.Body.Instructions.ToList();
                int processedCount = 0;

                for (int i = 0; i < instructions.Count; i++)
                {
                    var instruction = instructions[i];

                    if (IsIntegerConstant(instruction))
                    {
                        int value = GetConstantValue(instruction);

                        if (IsConstantSafeToReplace(value, instructions, i))
                        {
                            try
                            {
                                if (replaceFunction(instruction, value))
                                {
                                    processedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error replacing constant {value} in method {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing method constants for {method.Name}: {ex.Message}");
                return 0;
            }
        }

        public static void SafeReplaceInstruction(List<Instruction> instructions, Instruction oldInstruction, List<Instruction> newInstructions)
        {
            if (oldInstruction == null || newInstructions == null || newInstructions.Count == 0)
                return;

            try
            {
                int index = instructions.IndexOf(oldInstruction);
                if (index >= 0)
                {
                    instructions.RemoveAt(index);
                    for (int i = 0; i < newInstructions.Count; i++)
                    {
                        instructions.Insert(index + i, newInstructions[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error replacing instruction: {ex.Message}");
            }
        }

        public static Random GetRandom()
        {
            return random;
        }

        public static void ClearUsedNames()
        {
            usedNames.Clear();
        }

        public static bool IsNameUsed(string name)
        {
            return usedNames.Contains(name);
        }

        public static bool IsLoadLocal(Instruction instr)
        {
            if (instr == null)
                return false;
            return instr.OpCode == OpCodes.Ldloc || instr.OpCode == OpCodes.Ldloc_0 || instr.OpCode == OpCodes.Ldloc_1 || instr.OpCode == OpCodes.Ldloc_2 || instr.OpCode == OpCodes.Ldloc_3 || instr.OpCode == OpCodes.Ldloc_S;
        }

        public static bool IsStoreLocal(Instruction instr)
        {
            if (instr == null)
                return false;
            return instr.OpCode == OpCodes.Stloc || instr.OpCode == OpCodes.Stloc_0 || instr.OpCode == OpCodes.Stloc_1 || instr.OpCode == OpCodes.Stloc_2 || instr.OpCode == OpCodes.Stloc_3 || instr.OpCode == OpCodes.Stloc_S;
        }

        public static Local GetLocal(Instruction instr, IList<Local> locals)
        {
            if (instr == null)
                return null;
            if (instr.Operand is Local l)
                return l;
            if (instr.OpCode == OpCodes.Ldloc_0 || instr.OpCode == OpCodes.Stloc_0)
                return locals.Count > 0 ? locals[0] : null;
            if (instr.OpCode == OpCodes.Ldloc_1 || instr.OpCode == OpCodes.Stloc_1)
                return locals.Count > 1 ? locals[1] : null;
            if (instr.OpCode == OpCodes.Ldloc_2 || instr.OpCode == OpCodes.Stloc_2)
                return locals.Count > 2 ? locals[2] : null;
            if (instr.OpCode == OpCodes.Ldloc_3 || instr.OpCode == OpCodes.Stloc_3)
                return locals.Count > 3 ? locals[3] : null;
            return null;
        }

        public static int EmulateAndResolveLocal(MethodDef method, byte[] blob, Local blobPtr, Local targetLocal, Instruction targetInstr)
        {
            var instrs = method.Body.Instructions;
            var locals = new int[method.Body.Variables.Count];

            int offsetShiftLocalIdx = -1;
            foreach (var instr in instrs)
            {
                if (instr.OpCode == OpCodes.Ldind_I4)
                {
                    int idx = instrs.IndexOf(instr);
                    if (idx >= 2 && instrs[idx - 1].OpCode == OpCodes.Add && IsLoadLocal(instrs[idx - 2]))
                    {
                        var l = GetLocal(instrs[idx - 2], method.Body.Variables);
                        if (l != null)
                        {
                            offsetShiftLocalIdx = l.Index;
                            break;
                        }
                    }
                }
            }

            if (offsetShiftLocalIdx != -1 && offsetShiftLocalIdx < locals.Length)
                locals[offsetShiftLocalIdx] = 4;

            for (int i = 0; i < instrs.Count; i++)
            {
                if (instrs[i] == targetInstr)
                    break;

                if (IsStoreLocal(instrs[i]))
                {
                    int idx = instrs.IndexOf(instrs[i]);
                    if (idx > 0 && IsIntegerConstant(instrs[idx - 1]))
                    {
                        var l = GetLocal(instrs[i], method.Body.Variables);
                        if (l != null && l.Index < locals.Length)
                            locals[l.Index] = GetConstantValue(instrs[idx - 1]);
                    }
                }
            }

            int maxSteps = 20000;
            int ip = 0;
            var stack = new Stack<int>();

            while (ip < instrs.Count && maxSteps-- > 0)
            {
                var instr = instrs[ip];
                if (instr == targetInstr)
                {
                    if (targetLocal.Index < locals.Length)
                        return locals[targetLocal.Index];
                    return -1;
                }

                try
                {
                    if (IsIntegerConstant(instr))
                    {
                        stack.Push(GetConstantValue(instr));
                    }
                    else if (IsLoadLocal(instr))
                    {
                        var l = GetLocal(instr, method.Body.Variables);
                        stack.Push(l != null && l.Index < locals.Length ? locals[l.Index] : 0);
                    }
                    else if (IsStoreLocal(instr))
                    {
                        var l = GetLocal(instr, method.Body.Variables);
                        if (stack.Count > 0 && l != null && l.Index < locals.Length)
                            locals[l.Index] = stack.Pop();
                    }
                    else if (instr.OpCode == OpCodes.Add)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(a + b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Sub)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(a - b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Mul)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(a * b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Div)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(b == 0 ? 0 : a / b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Ldind_I4)
                    {
                        if (stack.Count >= 1)
                        {
                            int addr = stack.Pop();
                            if (blob != null && addr >= 0 && addr + 4 <= blob.Length)
                            {
                                stack.Push(BitConverter.ToInt32(blob, addr));
                            }
                            else
                            {
                                stack.Push(0);
                            }
                        }
                    }
                    else if (instr.OpCode == OpCodes.Pop)
                    {
                        if (stack.Count > 0)
                            stack.Pop();
                    }
                    else if (instr.OpCode == OpCodes.Dup)
                    {
                        if (stack.Count > 0)
                            stack.Push(stack.Peek());
                    }
                    else if (instr.OpCode == OpCodes.Br || instr.OpCode == OpCodes.Br_S)
                    {
                        var tgt = instr.Operand as Instruction;
                        int tgtIdx = instrs.IndexOf(tgt);
                        if (tgtIdx != -1)
                        {
                            ip = tgtIdx;
                            continue;
                        }
                    }
                    else if (instr.OpCode == OpCodes.Bne_Un || instr.OpCode == OpCodes.Bne_Un_S)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            var tgt = instr.Operand as Instruction;
                            if (a != b)
                            {
                                int tgtIdx = instrs.IndexOf(tgt);
                                if (tgtIdx != -1)
                                {
                                    ip = tgtIdx;
                                    continue;
                                }
                            }
                        }
                    }
                    else if (instr.OpCode == OpCodes.Beq || instr.OpCode == OpCodes.Beq_S)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            var tgt = instr.Operand as Instruction;
                            if (a == b)
                            {
                                int tgtIdx = instrs.IndexOf(tgt);
                                if (tgtIdx != -1)
                                {
                                    ip = tgtIdx;
                                    continue;
                                }
                            }
                        }
                    }
                    else if (instr.OpCode == OpCodes.Ldsflda)
                    {
                        stack.Push(0);
                    }
                }
                catch { }

                ip++;
            }

            return -1;
        }

        public static int EmulateStackTop(MethodDef method, Instruction targetInstr)
        {
            var instrs = method.Body.Instructions;
            var locals = new int[method.Body.Variables.Count];

            int maxSteps = 20000;
            int ip = 0;
            var stack = new Stack<int>();

            while (ip < instrs.Count && maxSteps-- > 0)
            {
                var instr = instrs[ip];
                if (instr == targetInstr)
                {
                    return stack.Count > 0 ? stack.Peek() : -1;
                }

                try
                {
                    if (IsIntegerConstant(instr))
                    {
                        stack.Push(GetConstantValue(instr));
                    }
                    else if (IsLoadLocal(instr))
                    {
                        var l = GetLocal(instr, method.Body.Variables);
                        stack.Push(l != null && l.Index < locals.Length ? locals[l.Index] : 0);
                    }
                    else if (IsStoreLocal(instr))
                    {
                        var l = GetLocal(instr, method.Body.Variables);
                        if (stack.Count > 0 && l != null && l.Index < locals.Length)
                            locals[l.Index] = stack.Pop();
                    }
                    else if (instr.OpCode == OpCodes.Add)
                    {
                        if (stack.Count >= 2)
                            stack.Push(stack.Pop() + stack.Pop());
                    }
                    else if (instr.OpCode == OpCodes.Sub)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(a - b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Mul)
                    {
                        if (stack.Count >= 2)
                            stack.Push(stack.Pop() * stack.Pop());
                    }
                    else if (instr.OpCode == OpCodes.Div)
                    {
                        if (stack.Count >= 2)
                        {
                            int b = stack.Pop();
                            int a = stack.Pop();
                            stack.Push(b == 0 ? 0 : a / b);
                        }
                    }
                    else if (instr.OpCode == OpCodes.Pop)
                    {
                        if (stack.Count > 0)
                            stack.Pop();
                    }
                    else if (instr.OpCode == OpCodes.Dup)
                    {
                        if (stack.Count > 0)
                            stack.Push(stack.Peek());
                    }
                    else if (instr.OpCode == OpCodes.Br || instr.OpCode == OpCodes.Br_S)
                    {
                        var tgt = instr.Operand as Instruction;
                        int tgtIdx = instrs.IndexOf(tgt);
                        if (tgtIdx != -1)
                        {
                            ip = tgtIdx;
                            continue;
                        }
                    }
                }
                catch { }

                ip++;
            }

            return -1;
        }

        public static Instruction FindInstruction(IList<Instruction> instrs, OpCode opCode)
        {
            foreach (var instr in instrs)
                if (instr.OpCode == opCode)
                    return instr;
            return null;
        }
    }
}
