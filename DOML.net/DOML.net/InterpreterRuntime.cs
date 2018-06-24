#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOML.Logger;

namespace DOML.IR {
    /// <summary>
    /// This controls the runtime.
    /// </summary>
    /// <remarks>
    /// A simple stack based VM with object registers.
    /// == WHY ==
    ///     Mainly due to the fact that simplicity means a lot of commands come down to a few safety checks and a single call.
    ///     And we don't need the complexity.  Simpler code is easier on the JITTER and we should get more optimisations.
    /// == DETAILS ==
    /// Contignent static array based data structures for both the stack and object register.
    /// The stack is implemented as a static array backing with a pointer ontop.  
    ///     - Through the API there is no way to index it like an array only pop and push.
    /// The object registers are a static array with indexing support.
    /// </remarks>
    public class InterpreterRuntime {
        /// <summary>
        /// The object registers array, contains all the objects in an indexable array.
        /// </summary>
        /// <remarks>
        /// Don't use <c>objectRegisters.Length</c> and rather use <see cref="RegisterSize"/> for clarity.
        /// </remarks>
        private object[] objectRegisters = new object[0];

        /// <summary>
        /// The stack which is implemented as static array with a ptr representing a push/poppable realm.
        /// Avoid direct indexing the stack (no API access should be provided to index) with the exception of indexing with the value of <see cref="stackPtr"/>.
        /// </summary>
        /// <remarks>
        /// Don't use <c>stack.Length</c> and rather use <see cref="MaxStackSize"/>.
        /// </remarks>
        private object[] stack = new object[0];

        /// <summary>
        /// The current stack pointer, representing what element is the top of the stack.
        /// A value of <code>-1</code> represents that the stack has no elements and a value equal to <see cref="MaxStackSize"/><code> - 1</code> means the stack is full.
        /// </summary>
        /// <remarks> 
        /// When getting the value of stackPtr to represent how many elements exist in the stack use <see cref="CurrentStackSize"/> instead (though note it is equal to <see cref="stackPtr"/><code> + 1</code>.
        /// </remarks>
        private int stackPtr = -1;

        /// <summary>
        /// The current stack size, represents the current highest index + 1.
        /// Does not represent the maximum size holdable by the stack (which is represented by <see cref="MaxStackSize"/>).
        /// </summary>
        /// <remarks> 
        /// Equal to <see cref="stackPtr"/><code> + 1</code>. 
        /// </remarks>
        public int CurrentStackSize => stackPtr + 1;

        /// <summary>
        /// Represents the maximum size the stack can grow to.
        /// Does not represent the current size which is represented by <see cref="CurrentStackSize"/>.
        /// </summary>
        /// <remarks>
        /// Equal to <c>stack.Length</c>.
        /// </remarks>
        public int MaxStackSize => stack.Length;

        /// <summary>
        /// Represents the current amount of registers.
        /// </summary>
        /// <remarks> 
        /// Equal to <c>objectRegisters.Length</c>.
        /// </remarks>
        public int RegisterSize => objectRegisters.Length;

        /// <summary>
        /// How much vector space left.
        /// </summary>
        private int collectionCount;

        /// <summary>
        /// Clears the stack array.
        /// A.k.a. sets all values to null.
        /// </summary>
        /// <returns> True if could clear the stack. </returns>
        public bool ClearSpace() {
            // We never set stack to null so no need for check
            if (CurrentStackSize > 0) {
                Array.Clear(stack, 0, CurrentStackSize);
                stackPtr = -1; // Reset pointer
                return true;
            } else {
                Log.Info("Couldn't clear stack cause stack was empty.");
                return false;
            }
        }

        /// <summary>
        /// Clears all registers.
        /// A.k.a. sets all values to null.
        /// </summary>
        /// <returns> True if could clear all registers. </returns>
        public bool ClearRegisters() {
            if (RegisterSize > 0) {
                Array.Clear(objectRegisters, 0, RegisterSize);
                return true;
            } else {
                Log.Info("Couldn't clear registers cause the register array was empty.");
                return false;
            }
        }

        /* No safe functions will ever throw!! */
        #region Safe_Implementation
        /// <summary>
        /// Reserves space in the registers.
        /// </summary>
        /// <param name="space"> The space (in terms of amount of objects) to reserve.  1 indexed. </param>
        /// <returns> True if registers was resized else false.  </returns>
        /// <remarks> 
        /// More of a check to see if we need to resize registers,
        /// and a way to make all calls go through the API and not provide direct interaction with <see cref="objectRegisters"/>.
        /// </remarks>
        public bool ReserveRegister(int space) {
            if (space > RegisterSize) {
                // Resize array
                objectRegisters = new object[space];
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Get an object at an index.
        /// Indexes the object register array with the index provided.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <param name="obj"> The object at that index. </param>
        /// <returns> If there exists an object at that index. </returns>
        public bool GetObject(int index, out object obj) {
            if (index < RegisterSize && objectRegisters[index] != null) {
                obj = objectRegisters[index];
                return true;
            } else {
                obj = null;
                return false;
            }
        }

        /// <summary>
        /// Set an object at an index.
        /// Will override any object at that index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <param name="obj"> The object to set. </param>
        /// <returns> If the passesd object isn't null and there is space. </returns>
        public bool SetObject(object obj, int index) {
            if (index < RegisterSize && obj != null) {
                objectRegisters[index] = obj;
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Removes the object at the index provided.
        /// Note: Will still return false even if the object
        ///       at the index is null.
        /// </summary>
        /// <param name="index"> The index to remove the object. </param>
        /// <returns> If <paramref name="index"/> <see cref="RegisterSize"/>. </returns>
        public bool RemoveObject(int index) {
            if (index < RegisterSize) {
                objectRegisters[index] = null;
                return false;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Pushes a value onto the stack.
        /// </summary>
        /// <typeparam name="T"> The type of the object, used to make arrays more efficient. </typeparam>
        /// <param name="value"> The value to push onto the stack. </param>
        /// <param name="resizeIfNoSpace"> If there is no space then resize the stack and copy values over. </param>
        /// <returns> True if value could be pushed onto stack. </returns>
        public bool Push<T>(T value, bool resizeIfNoSpace) {
            if (collectionCount > 0) {
                if (TopIsOfType<T[]>(out bool result)) {
                    if (result == false) {
                        PushVector<T>(collectionCount);
                    }
                } else {
                    // This means that TopIsOfType FAILS
                    // not that it isn't IList, it fails if there is no object to check.
                    return false;
                }

                if (typeof(T) == typeof(object)) {
                    if (Peek(out IList vec)) {
                        Type generic = vec.GetType().GetGenericTypeDefinition().GenericTypeArguments[0];
                        if (generic == typeof(object) || value.GetType() == generic) {
                            vec[vec.Count - collectionCount] = value;
                            collectionCount--;
                        }
                    }
                } else {
                    // No need for type check since peek will automatically do that for us
                    if (Peek(out T[] vec)) {
                        vec[vec.Length - collectionCount] = value;
                        collectionCount--;
                        return true;
                    }
                }
                return false;
            }

            /* No need to check for > since it only ever increases by one and you can't directly change the index
             * Without pushing/popping therefore if somehow it does occur then that's a runtime error that we want.
             */
            if (CurrentStackSize == MaxStackSize) {
                // Could argue that we should always resize on safe calls.
                if (resizeIfNoSpace) {
                    object[] temp = stack;
                    stack = new object[CurrentStackSize + 1]; // Increase by one to fill spot

                    Log.Warning("Attempted to push a value onto a stack that was too small, reserving the new space and moving over values");
                    temp.CopyTo(stack, 0);
                } else {
                    Log.Error("No space on stack");
                    return false;
                }
            }

            // It will only reach here if there is enough space
            stack[++stackPtr] = value;
            return true;
        }

        /// <summary>
        /// Pops a value from the top of the stack.
        /// </summary>
        /// <param name="result"> The value from the top of the array. </param>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool Pop(out object result) {
            // Not entirely sure if we should build off 'peek' but eh?
            if (Peek(out result)) {
                // Set the index to null, to make sure that if something tried to pop it off again
                // It would error out, and move pointer down.
                stack[stackPtr--] = null;
                return true;
            } else {
                // @CONDITIONAL_TODO: Add error log if we remove the peek code.
                return false;
            }
        }

        /// <summary>
        /// Pops a stack value.
        /// </summary>
        /// <typeparam name="T"> The type expecting from the stack. </typeparam>
        /// <param name="result"> The result to set if peeking completed. </param>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool Pop<T>(out T result) {
            // Not entirely sure if we should build off 'peek' but eh?
            if (Peek<T>(out result)) {
                // Set the index to null, to make sure that if something tried to pop it off again
                // It would error out, and move pointer down.
                stack[stackPtr--] = null;
                return true;
            } else {
                // @CONDITIONAL_TODO: Add error log if we remove the peek code.
                return false;
            }
        }

        /// <summary>
        /// Pops a value off the stack but doesn't return it.
        /// </summary>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool Pop() {
            // We aren't checking if CurrentStackSize <= MaxStackSize for the same reason as in Push
            // It shouldn't occur, the API doesn't allow it and thus if it does its an error we want to highlight.
            if (CurrentStackSize > 0 && stack[stackPtr] != null) {
                // Set the index to null, to make sure that if something tried to pop it off again
                // It would error out, and move pointer down.                
                stack[stackPtr--] = null;
                return true;
            } else {
                Log.Error("Nothing to pop off the stack, or wanting to pop off object that has as an invalid type.");
                return false;
            }
        }

        /// <summary>
        /// Checks if the top is of the type required.
        /// </summary>
        /// <typeparam name="T"> The type to check the object to. </typeparam>
        /// <param name="result"> The result to if the object is of the type as the one provided. </param>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool TopIsOfType<T>(out bool result) {
            // We aren't checking if CurrentStackSize <= MaxStackSize for the same reason as in Push
            // It shouldn't occur, the API doesn't allow it and thus if it does its an error we want to highlight.
            if (CurrentStackSize > 0 && stack[stackPtr] != null) {
                result = stack[stackPtr] is T;
                return true;
            } else {
                result = false;
                return false;
            }
        }

        /// <summary>
        /// Peeks the top stack value.
        /// </summary>
        /// <param name="result"> The resulting top value. </param>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool Peek(out object result) {
            // We aren't checking if CurrentStackSize <= MaxStackSize for the same reason as in Push
            // It shouldn't occur, the API doesn't allow it and thus if it does its an error we want to highlight.
            if (CurrentStackSize > 0 && stack[stackPtr] != null) {
                result = stack[stackPtr];
                return true;
            } else {
                Log.Error("Nothing to peek off the stack.");
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Peeks the top stack value.
        /// </summary>
        /// <typeparam name="T"> The type expected that the top value will be. </typeparam>
        /// <param name="result"> The resulting top value. </param>
        /// <returns> Returns false if there is no value at the location provided (either cause index invalid or cause value == null). </returns>
        public bool Peek<T>(out T result) {
            // We aren't checking if CurrentStackSize <= MaxStackSize for the same reason as in Push
            // It shouldn't occur, the API doesn't allow it and thus if it does its an error we want to highlight.
            if (CurrentStackSize > 0 && stack[stackPtr] != null) {
                // We aren't using TopIsOfType<T>() since its less efficient (it doesn't check the below conditions just this one)
                if (stack[stackPtr] is T) {
                    // Easy conversion this will be the most common variant.
                    // This will occur for all straight forward conversions like well int to int
                    // But will also occur for childed classes being converted to their parents.
                    result = (T)stack[stackPtr];
                    return true;
                } else if (stack[stackPtr] is IConvertible) {
                    // Slightly more efficient then below
                    // All primatives will follow this
                    result = (T)Convert.ChangeType(stack[stackPtr], typeof(T));
                    return true;
                } else {
                    Log.Error("Top value isn't convertible to type.");
                    result = default(T);
                    return false;
                }
            } else {
                Log.Error("Nothing to peek off the stack.");
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
        public bool ReserveSpace(int space) {
            if (space > MaxStackSize) {
                // Resize array
                stack = new object[space];
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Gets the system ready to make a vector.
        /// </summary>
        /// <param name="count"> The size of the vector. </param>
        /// <returns> If the vector can be created (count > 1). </returns>
        public bool MakeVector(int count) {
            if (count > 1) {
                collectionCount = count;
                // This will stop the system from doubling up the lists
                Unsafe_Push<object>(false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pushes a vector onto the stack.
        /// </summary>
        /// <typeparam name="T"> The type of the vector. </typeparam>
        /// <param name="count"> The size of the vector. </param>
        /// <returns> True if success. </returns>
        private bool PushVector<T>(int count) {
            if (count > 1) {
                T[] array = new T[count];

                // Note: Not sure we want to always resize?
                if (!Push(array, true)) {
                    return false;
                }
                return true;
            } else {
                return false;
            }
        }

        #endregion

        #region Unsafe_Stack_Implementation
        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Get an object at an index.
        /// </summary>
        /// <param name="index"> The index of the object. </param>
        /// <returns> The object. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public object Unsafe_GetObject(int index) {
            return objectRegisters[index];
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Set an object at an index.
        /// </summary>
        /// <param name="index"> The index to set. </param>
        /// <param name="obj"> The object value to set. </param>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_SetObject(object obj, int index) {
            objectRegisters[index] = obj;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Removes the object at the index.
        /// </summary>
        /// <param name="index"> The index of the object to remove. </param>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_RemoveObject(int index) {
            objectRegisters[index] = null;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Returns if the top is of the type provided.
        /// </summary>
        /// <typeparam name="T"> The type to compare the top value against. </typeparam>
        /// <returns> True if the top is of the type provided. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public bool Unsafe_TopIsOfType<T>() {
            return stack[stackPtr] is T;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Will push a value onto the stack.
        /// </summary>
        /// <typeparam name="T"> The type, used to make arrays more efficient. </typeparam>
        /// <param name="value"> The value to push. </param>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_Push<T>(T value) {
            if (collectionCount > 0) {
                T[] array = Unsafe_Peek<T[]>();
                array[array.Length - collectionCount] = value;
                collectionCount--;
                return;
            }

            stack[++stackPtr] = value;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Pops top object.
        /// </summary>
        /// <returns> The top object. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public object Unsafe_Pop() {
            object temp = Unsafe_Peek();
            stack[stackPtr--] = null;
            return temp;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Pops top object without returning.
        /// </summary>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public void Unsafe_Pop_NoReturn() {
            stack[stackPtr--] = null;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Will pop a value off the stack.
        /// Will check if it is T else it'll just do a convert.
        /// </summary>
        /// <typeparam name="T"> The type to cast the top object too. </typeparam>
        /// <returns> The value in type T. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public T Unsafe_Pop<T>() {
            T temp = Unsafe_Peek<T>();
            stack[stackPtr--] = null;
            return temp;
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Will peek the top value of the stack.
        /// </summary>
        /// <returns> The value in type T. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public object Unsafe_Peek() {
            return stack[stackPtr];
        }

        /// <summary>
        /// Unsafe implementation, doesn't perform any checks.
        /// Will peek the top value of the stack.
        /// Will check if it is T else it'll just do a convert.
        /// </summary>
        /// <typeparam name="T"> The type to cast the top object too. </typeparam>
        /// <returns> The value in type T. </returns>
        /// <remarks> Will throw a bunch of errors if you do something badly wrong. </remarks>
        public T Unsafe_Peek<T>() {
            return stack[stackPtr] is T ? Unsafe_Peek<T>() : (T)Convert.ChangeType(Unsafe_Peek(), typeof(T));
        }

        #endregion
    }
}
