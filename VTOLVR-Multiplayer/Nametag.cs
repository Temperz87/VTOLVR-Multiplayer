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
    public void SetText(string name, Transform parent, Transform head)
    {
        this.head = head;
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.SetText(name);
        transform.SetParent(parent.transform);
        transform.localPosition = Vector3.up * 10f;
    }
    public void Update()
    {
        if (head != null)
            transform.LookAt(head);
    }
}