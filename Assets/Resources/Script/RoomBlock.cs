using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBlock : MonoBehaviour {

    private Rigidbody _Rigid = null;
    private MeshRenderer _Renderer = null;

    private bool _Passed = false;
    public bool passed { set { _Passed = value; } }

    // Use this for initialization
    void Start () {
        _Rigid = GetComponent<Rigidbody>();
        _Renderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        //部屋かどうか？
        if (_Passed)
            _Renderer.material = Resources.Load("Material/Passed") as Material;
        else
        {
            //寝ているかどうか？
            if (_Rigid.IsSleeping())
                _Renderer.material = Resources.Load("Material/Static") as Material;
            else
                _Renderer.material = Resources.Load("Material/Dynamic") as Material;
        }

    }
}
