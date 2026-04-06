using System.Collections.Generic;
using UnityEngine;

public class PropProvider : MonoBehaviour
{
    private static PropProvider instance;

    [SerializeField] private List<GameObject> props = new();

    private void Awake()
    {
        instance = this;
    }

    public static int GetRandomPropIndex(int currentIndex)
    {
        if (currentIndex == -1) return Random.Range(0, instance.props.Count);
        if (currentIndex == 0)
        {
            return Random.Range(1, instance.props.Count);
        } else if (currentIndex == instance.props.Count - 1)
        {
            return Random.Range(0, instance.props.Count - 1);
        } else
        {
            var randomIndex = Random.Range(0, instance.props.Count);
            if (randomIndex == currentIndex) randomIndex++;
            return randomIndex;
        }
    }

    public static GameObject GetPropById(int id)
    {
        return instance.props[id];
    }
}
