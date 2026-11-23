using System.Collections.Generic;
using UnityEngine;

/// <summary>Satu modal aktif. Semua panelPush/Pop ke sini.</summary>
public static class UiModalState
{
    private static readonly Stack<object> Stack = new Stack<object>();

    public static bool IsBlocking => Stack.Count > 0;
    public static int Count => Stack.Count;
    public static object Top => Stack.Count > 0 ? Stack.Peek() : null;

    public static void Push(object owner)
    {
        if (owner == null)
        {
            return;
        }

        Stack.Push(owner);
    }

    public static void Pop(object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (Stack.Count > 0 && ReferenceEquals(Stack.Peek(), owner))
        {
            Stack.Pop();
            return;
        }

        // Jika owner bukan di atas (mis. panel ditutup berurutan), buang dari tumpukan.
        if (Stack.Contains(owner))
        {
            Stack<object> temp = new Stack<object>();
            while (Stack.Count > 0)
            {
                object item = Stack.Pop();
                if (!ReferenceEquals(item, owner))
                {
                    temp.Push(item);
                }
            }

            while (temp.Count > 0)
            {
                Stack.Push(temp.Pop());
            }
        }
    }

    public static void Clear()
    {
        Stack.Clear();
    }
}
