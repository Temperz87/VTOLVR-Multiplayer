using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct V3
{
    public float x, y, z;
    public V3(UnityEngine.Vector3 vector3)
    {
        this.x = vector3.x;
        this.y = vector3.y;
        this.z = vector3.z;
    }

    public Vector3 GetV3()
    {
        return new Vector3(x, y, z);
    }
}