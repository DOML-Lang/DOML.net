#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Reflection;
using DOML.Logger;

namespace DOML.IR
{
    /// <summary>
    /// This controls the runtime.
    /// </summary>
    /// <remarks> 
    /// A super simple stack based VM.  
    /// Mainly due to the fact that simplicity means a lot of commands come down to a few safety checks and a single call.
    /// And we don't need the complexity.  Simpler code is easier on the JITTER and we should get more optimisations.
    /// </remarks>
    public class InterpreterRuntime
    {
        private object[] objectRegisters;
        private object[] stack;
        private int stackPtr = -1; // This is where we are 'up to' and the stack is, where as stack.length is the actual length of the vm stack

        public int CurrentStackSize => stackPtr + 1;

        public int MaxStackSize => stack.Length;

        public int RegisterSize => objectRegisters.Length;

        #region Safe_Stack_Implementation

        /// <summary>
        /// Reserves space in the registers.
        /// </summary>
        /// <param name="space"> The space (in terms of amount of objects) to reserve.  1 indexed. </param>
        /// <returns> True if registers was resized. </returns>
        /// <remarks> More of a check to see if we need to resize registers. </remarks>
        public bool ReserveRegister(int space)
        {
            if (objectRegisters == null || space > RegisterSize)
            {
                // Resize array
                objectRegisters = new object[space];
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get an object at an index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <param name="obj"> The resulting object. </param>
        /// <returns> if the index < length. </returns>
        public bool GetObject(int index, out object obj)
        {
            if (index < RegisterSize)
            {
                obj = objectRegisters[index];
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        /// <summary>
        /// Set an object at an index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <param name="obj"> The object to set. </param>
        /// <returns> if the index < length. </returns>
        public bool SetObject(object obj, int index)
        {
            if (index < RegisterSize)
            {
                objectRegisters[index] = obj;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool UnsetObject(int index)
        {
            if (index < RegisterSize)
            {
                objectRegisters[index] = null;
                return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Push a value onto the stack.
        /// </summary>
        /// <param name="value"> The value to push onto the stack. </param>
        /// <returns> True if value could be pushed onto stack. </returns>
        /// <remarks> Really should bound primatives. </remarks>
        public bool Push(object value, bool reserveIfNoSpace)
        {
            // >= because stack pointer is 0 indexed
            if (CurrentStackSize > MaxStackSize)
            {
                if (reserveIfNoSpace)
                {
                    int oldLength = MaxStackSize;
                    object[] temp = stack;
                    stack = new object[CurrentStackSize]; // Increase by one to fill spot

                    Log.Warning("Attempted to push a value onto a stack that was too small, reserving the new space and moving over values");

                    for (int i = 0; i < oldLength; i++)
                    {
                        stack[i] = temp[i];
                    }
                }
                else
                {
                    Log.Error("No space on stack");
                    return false;
                }
            }

            // We will have space, if for some reason the reserve function failed then that's a runtime exception
            // And it won't reach here.
            stack[++stackPtr] = value;
            return true;
        }

        /// <summary>
        /// Pops a stack value.
        /// </summary>
        /// <param name="result"> The result to set if peeking completed. </param>
        /// <returns> Returns false if it failed in finding a value, i.e. nothing to pop, or object was an invalid type. </returns>
        public bool Pop(out object result)
        {
            // Not entirely sure if we should build off 'peek' but eh?
            if (Peek(out result))
            {
                // Set the index to null, to make sure that if something tried to pop it off again
                // It would error out (IF they tried), and move pointer down.
                stack[stackPtr--] = null;
                return true;
            }
            else
            {
                // @TODO: Add error log if we remove the peek code.
                return false;
            }
        }

        /// <summary>
        /// Pops a stack value.
        /// </summary>
        /// <typeparam name="T"> The type expecting from the stack. </typeparam>
        /// <param name="result"> The result to set if peeking completed. </param>
        /// <returns> Returns false if it failed in finding a value, i.e. nothing to pop, or object was an invalid type. </returns>
        public bool Pop<T>(out T result)
        {
            // Not entirely sure if we should build off 'peek' but eh?
            if (Peek<T>(out result))
            {
                // Set the index to null, to make sure that if something tried to pop it off again
                // It would error out (IF they tried), and move pointer down.
                stack[stackPtr--] = null;
                return true;
            }
            else
            {
                // @TODO: Add error log if we remove the peek code.
                return false;
            }
        }

        /// <summary>
        /// Pops a stack value with no return value.
        /// </summary>
        /// <typeparam name="T"> The type expecting from the stack. </typeparam>
        /// <param name="result"> The result to set if peeking completed. </param>
        /// <returns> Returns false if it failed in finding a value, i.e. nothing to pop, or object was an invalid type. </returns>
        public bool PopWithNoReturn()
        {
            if (CurrentStackSize > 0 && CurrentStackSize <= MaxStackSize && stack[stackPtr] != null)
            {
                // Set the index to null, to make sure that if something tried to pop it off again
                stack[stackPtr--] = null;
                return true;
            }
            else
            {
                Log.Error("PopWithNoReturn failed", false);
                return false;
            }
        }

        public bool TopIsOfType<T>(out bool result)
        {
            if (CurrentStackSize > 0 && CurrentStackSize <= MaxStackSize && stack[stackPtr] != null)
            {
                result = stack[stackPtr] is T;
                return true;
            }
            else
            {
                result = false;
                return false;
            }
        }

        public bool Peek(out object result)
        {
            if (CurrentStackSize > 0 && CurrentStackSize <= MaxStackSize && stack[stackPtr] != null)
            {
                result = stack[stackPtr];
                return true;
            }
            else
            {
                Log.Error("Nothing to pop off stack, or wanting to pop off object as an invalid type.", false);
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Peeks a stack value.
        /// </summary>
        /// <typeparam name="T"> The type expecting from the stack. </typeparam>
        /// <param name="result"> The result to set if peeking completed. </param>
        /// <returns> Returns false if it failed in finding a value, i.e. nothing to peek, or object was an invalid type. </returns>
        public bool Peek<T>(out T result)
        {
            if (CurrentStackSize > 0 && CurrentStackSize <= MaxStackSize && stack[stackPtr] != null)
            {
                if (stack[stackPtr] is T)
                {
                    result = (T)stack[stackPtr];
                    return true;
                }
                else if (stack[stackPtr] is IConvertible)
                {
                    result = (T)Convert.ChangeType(stack[stackPtr], typeof(T));
                    return true;
                }
                else if (typeof(T).GetTypeInfo().IsAssignableFrom(stack[stackPtr].GetType().GetTypeInfo()))
                {
                    // Inefficient find a faster way (that hopefully doesn't use reflection
                    result = (T)stack[stackPtr];
                    return true;
                }
                else
                {
                    Log.Error("Type doesn't match top value, and not convertible to it.");
                    result = default(T);
                    return false;
                }
            }
            else
            {
                Log.Error("Nothing to pop off stack, or wanting to pop off object as an invalid type.", false);
                result = default(T);
                return false;
            }
        }

        /// <summary>
        /// Reserves space in the stack.
        /// </summary>
        /// <param name="space"> The space (in terms of amount of objects) to reserve.  1 indexed. </param>
        /// <returns> True if array was resized. </returns>
        /// <remarks> More of a check to see if we need to resize array. </remarks>
        public bool ReserveSpace(int space)
        {
            if (stack == null || space > MaxStackSize)
            {
                // Resize array
                stack = new object[space];
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ClearSpace()
        {
            if (stack != null)
            {
                // Currently same as unsafe??
                // I don't really understand how this could be unsafe??
                stack = new object[MaxStackSize];
            }

            stackPtr = -1;
        }

        public void ClearRegisters()
        {
            objectRegisters = new object[RegisterSize];
        }
        #endregion

        #region Unsafe_Stack_Implementation
        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Mainly here for the interpreter to run if it can verify that the bytecode will perform all checks.
        /// Get an object at an index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <returns> The object. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public object Unsafe_GetObject(int index)
        {
            return objectRegisters[index];
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Mainly here for the interpreter to run if it can verify that the bytecode will perform all checks.
        /// Set an object at an index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <param name="obj"> The object value to set. </param>
        /// <returns> if the index < length. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_SetObject(object obj, int index)
        {
            objectRegisters[index] = obj;
        }

        public void Unsafe_UnsetObject(int index)
        {
            objectRegisters[index] = null;
        }

        public bool Unsafe_TopIsOfType<T>()
        {
            return stack[stackPtr] is T;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Mainly here for the interpreter to run if it can verify that the bytecode will perform all checks.
        /// Will push a value onto the stack.
        /// </summary>
        /// <param name="value"> The value to push. </param>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_Push(object value)
        {
            stack[++stackPtr] = value;
        }

        /// <summary>
        /// Pops top object unsafely.
        /// </summary>
        /// <returns> The popped object. </returns>
        /// <remarks> Compared to <see cref="Unsafe_Pop{T}"/> it won't perform any casts. </remarks>
        public object Unsafe_Pop()
        {
            object temp = Unsafe_Peek();
            stack[stackPtr--] = null;
            return temp;
        }

        /// <summary>
        /// Pops the top object without returning.
        /// </summary>
        public void Unsafe_Pop_NoReturn()
        {
            stack[stackPtr--] = null;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Mainly here for the interpreter to run if it can verify that the bytecode will perform all checks.
        /// Will pop a value off the stack.
        /// </summary>
        /// <typeparam name="T"> The type to pop. </typeparam>
        /// <returns> The value in type T. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public T Unsafe_Pop<T>()
        {
            // Again as with the safe code this should be avoided??
            // It'll become inlined anyway??
            T temp = Unsafe_Peek<T>();
            stack[stackPtr--] = null;
            return temp;
        }

        public object Unsafe_Peek()
        {
            return stack[stackPtr];
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Mainly here for the interpreter to run if it can verify that the bytecode will perform all checks.
        /// Will peek a value off the stack.
        /// </summary>
        /// <typeparam name="T"> The type to peek. </typeparam>
        /// <returns> The value in type T. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public T Unsafe_Peek<T>()
        {
            return (T)stack[stackPtr];
        }

        public void Unsafe_ClearSpace()
        {
            stack = new object[stack.Length];
            stackPtr = -1;
        }

        public void Unsafe_ClearRegisters()
        {
            objectRegisters = new object[objectRegisters.Length];
        }
        #endregion
    }
}
