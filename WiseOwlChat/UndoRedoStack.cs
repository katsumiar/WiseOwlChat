using System;
using System.Collections.Generic;

namespace WiseOwlChat
{
    public class UndoRedoStack<T>
    {
        private Stack<T?> undoStack = new();
        private Stack<T?> redoStack = new();
        private bool isUndoing = false;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        private T? beforeObj;

        public void Push(T current)
        {
            if (!isUndoing)
            {
                if (beforeObj != null)
                {
                    undoStack.Push(beforeObj);
                }
                beforeObj = current;
            }
        }

        public void TryUndo(T current, Action<T> setAction)
        {
            if (CanUndo)
            {
                isUndoing = true;
                redoStack.Push(current);
                var obj = undoStack.Pop();
                if (obj != null)
                {
                    setAction(obj);
                }
                isUndoing = false;
            }
        }

        public void TryRedo(T current, Action<T> setAction)
        {
            if (CanRedo)
            {
                isUndoing = true;
                undoStack.Push(current);
                var obj = redoStack.Pop();
                if (obj != null )
                {
                    setAction(obj);
                }
                isUndoing = false;
            }
        }
    }
}
