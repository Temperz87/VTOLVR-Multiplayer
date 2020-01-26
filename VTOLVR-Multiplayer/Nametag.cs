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
    public Transform head;

    /// <summary>
    /// Sets the text
    /// </summary>
    /// <param name="name">The name to display</param>
    /// <param name="parent">The GameObject to attach the name to</param>
    /// <param name="head">The GameObject to rotate towards, typically the head of the player</param>
    public void SetText(string name, Transform parent, Transform head)
    {
        this.head = head;
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
            transform.LookAt(head);
    }
}