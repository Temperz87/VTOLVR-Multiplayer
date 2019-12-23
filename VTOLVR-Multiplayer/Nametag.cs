using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

class Nametag
{
    public string name { get; set; }
    TextMeshPro textMesh;
    GameObject thisObject;
    GameObject parent;

    public Nametag(string name, GameObject parent)
    {
        this.name = name;
        this.parent = parent;
        thisObject = new GameObject("nametag");
        textMesh = thisObject.AddComponent<TextMeshPro>();
        textMesh.SetText(name);
        thisObject.transform.SetParent(parent.transform);

        thisObject.transform.position = parent.transform.position;
        thisObject.transform.Translate(Vector3.up * 10f);
    }

    public void SetName(string name)
    {
        this.name = name;
        textMesh.SetText(name);
    }
}
