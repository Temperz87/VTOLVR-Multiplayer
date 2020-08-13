using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    private TextMeshPro textMesh;
    public static Transform head;

    /// <summary>
    /// Sets the text
    /// </summary>
    /// <param name="name">The name to display</param>
    /// <param name="parent">The GameObject to attach the name to</param>
    /// <param name="head">The GameObject to rotate towards, typically the head of the player</param>
    public void SetText(string name, Transform parent, Transform head)
    {
        Nametag.head = head;
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.enableWordWrapping = false;
        textMesh.SetText(name);
        transform.SetParent(parent);
        transform.localPosition = new Vector3(0, 10, 0);
    }
    public void Update()
    {
        if (head != null)
            transform.LookAt(2 * transform.position - head.position);
        else
            head = VRHead.instance.transform;
        if (transform.parent != null)
            transform.position = transform.parent.position + Vector3.up * 10;
    }
}