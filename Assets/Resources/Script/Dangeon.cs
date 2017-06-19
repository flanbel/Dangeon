using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dangeon : MonoBehaviour
{
    //ダンジョンの二次元配列
    [SerializeField]
    private int[,] _DangeonArray;
    public int[,] dangeonArray { get { return _DangeonArray; } }

    [SerializeField]
    private Vector2 _ArraySize;

    //配列作成。
    public void CreateArray()
    {
        GameObject Rooms = GameObject.Find("Rooms");
        Transform[] rooms = Rooms.GetComponentsInChildren<Transform>();
        float minX, maxX, minZ, maxZ;
        minX = minZ = float.MaxValue;
        maxX = maxZ = float.MinValue;
        foreach (Transform room in rooms)
        {
            minX = Mathf.Min(minX, room.position.x - room.lossyScale.x);
            minZ = Mathf.Min(minZ, room.position.z - room.lossyScale.z);

            maxX = Mathf.Max(maxX, room.position.x + room.lossyScale.x);
            maxZ = Mathf.Max(maxZ, room.position.z + room.lossyScale.z);
        }
        int sizeX = Mathf.RoundToInt(maxX - minX);
        int sizeY = Mathf.RoundToInt(maxZ - minZ);

        _DangeonArray = new int[sizeX, sizeY];
        _ArraySize = new Vector2(sizeX, sizeY);
    }

    //ダンジョン配置
    public void DungeonArrangement(List<PassWay> passway)
    {

    }
}
