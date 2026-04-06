using System.Collections.Generic;
using UnityEngine;

public class SceneProps : MonoBehaviour
{
    private static SceneProps instance;

    private List<Rigidbody> _props = new();

    private void Awake()
    {
        instance = this;
        LoadProps();
    }

    private void LoadProps()
    {
        _props.AddRange(GetComponentsInChildren<Rigidbody>());
    }

    public static Rigidbody GetProp(int index)
    {
        return instance._props[index];
    }

    public static int GetPropIndex(Rigidbody prop)
    {
        return instance._props.IndexOf(prop);
    }
}
