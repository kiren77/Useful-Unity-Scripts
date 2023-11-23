using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class ListGameObjects : MonoBehaviour
{
    public bool listComponents = false;
    public bool useSpecificHierarchy = false;
    public Transform specificHierarchy;

    StringBuilder sb = new StringBuilder();

    void Start()
    {
        List<GameObject> rootObjects = new List<GameObject>();
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootObjects);

        // iterate root objects and print their names
        foreach (GameObject obj in rootObjects)
        {
            sb.AppendLine("Root Object: " + obj.name);
            if (useSpecificHierarchy)
            {
                PrintSpecificHierarchy(obj.transform, specificHierarchy, "-");
            }
            else
            {
                PrintChildren(obj.transform, "-");
            }
        }

        Debug.Log(sb.ToString());
    }

    void PrintChildren(Transform parent, string indentation)
    {
        foreach (Transform child in parent)
        {
            sb.AppendLine(indentation + child.gameObject.name);
            if (listComponents)
            {
                foreach (Component component in child.gameObject.GetComponents<Component>())
                {
                    if (component is Behaviour behaviour && !behaviour.enabled)
                    {
                        sb.AppendLine(indentation + "- [DA] " + component.GetType().Name);
                    }
                }
            }
            PrintChildren(child, indentation + "-");
        }
    }

    void PrintSpecificHierarchy(Transform parent, Transform specific, string indentation)
{
    if (parent == specific)
    {
        int depth = CalculateDepth(specific);
        PrintHierarchyUp(parent, "", depth); // Adjusted to pass initial indentation and depth
        PrintChildren(parent, indentation + "-");
    }
    else
    {
        foreach (Transform child in parent)
        {
            PrintSpecificHierarchy(child, specific, indentation + "-");
        }
    }
}

void PrintHierarchyUp(Transform child, string indentation, int depth)
{
    if (child.parent != null)
    {
        PrintHierarchyUp(child.parent, indentation, depth - 1);
    }
    sb.AppendLine(new string('-', depth) + child.gameObject.name); // Adjust indentation based on depth
}

int CalculateDepth(Transform transform)
{
    int depth = 0;
    while (transform.parent != null)
    {
        depth++;
        transform = transform.parent;
    }
    return depth;
}

}