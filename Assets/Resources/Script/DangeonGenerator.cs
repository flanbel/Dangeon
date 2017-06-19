using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ルームを示すポイント
[System.Serializable]
public class Point
{
    //座標
    public Vector3 pos;
    //各ポイントを識別するための一意なID
    [SerializeField]
    private int _ID;
    public int Id { get { return _ID; } }
    //ポイントの所属するグループ
    public int group;
    //部屋のサイズ
    private Vector3 _RoomSize;
    public Vector3 roomSize { get { return _RoomSize; } }


    public Point(Vector3 p, int id, Vector3 size)
    {
        pos = p;
        _ID = id;
        group = _ID;
        _RoomSize = size;
    }

    static public bool operator ==(Point p1, Point p2)
    {
        return (p1.pos == p2.pos);
    }

    static public bool operator !=(Point p1, Point p2)
    {
        return (p1.pos != p2.pos);
    }
}

public class DangeonGenerator : MonoBehaviour {

    //床のオブジェクト
    [SerializeField]
    private GameObject _FloorObject;

    //壁のオブジェクト
    [SerializeField]
    private GameObject _WallObject;

    //生成範囲となる楕円の縦横の長さ
    [SerializeField]
    private Vector2 _CircleSize = new Vector2(100, 100);

    //生成する最大数
    [SerializeField]
    private int _MaxGenerateNum = 50;

    //部屋の大きさの最小最大。
    [SerializeField]
    private Vector2 _RoomSizeMinMax = new Vector2(5, 20);

    //部屋と判定するしきい値
    [SerializeField]
    private int _Threshold = 10;

    //生成間隔
    [SerializeField]
    private float _GeneratorInterval = 0.2f;

    //頂点list
    [SerializeField]
    private List<Point> _PointList = new List<Point>();

    //デバッグ時のポイントのサイズ。
    [SerializeField]
    private int _DebugPointSize = 10;

    //部屋追加。
    public void AddRoom(Vector3 pos, Vector3 size)
    {
        //ポイント生成。
        CreatePoint(pos, "Point" + _PointList.Count.ToString());
        //listに追加
        _PointList.Add(new Point(pos, _PointList.Count, size));
    }

    //ポイント生成。
    private GameObject CreatePoint(Vector3 pos, string name = "point")
    {
        //わかりやすいように丸を出すよー。
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(GameObject.Find("Points").transform);
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * _DebugPointSize;

        return sphere;
    }

    //最小全域木作成後に追加するルートの比率。
    public float _AddRoot = 0.2f;

    //ドロネー三角形分割するやつ。
    [SerializeField]
    private DelaunayTriangles _DelaunayTriangles;

    //全域木作るやつ。
    [SerializeField]
    private SpanningTree _SpanningTree;

    //ダンジョン
    [SerializeField]
    private Dangeon _Dangeon;

    // Use this for initialization
    void Start () {
        DungeonGeneration();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    //ダンジョンの生成。
    public void DungeonGeneration()
    {
        //コルーチン再生。
        StartCoroutine("Generator");
    }

    /// <summary>
    /// 楕円に納まるポジションを計算
    /// </summary>
    Vector3 CalcRandomPointInCircle()
    {
        float t = 2.0f * Mathf.PI * Random.value;
        //0.0f～2.0f;
        float u = Random.value + Random.value;
        //1.0f以下に丸める。
        float r = u > 1 ? 2 - u : u;
        return new Vector3((_CircleSize.x * r * Mathf.Cos(t) / 2), 0, (_CircleSize.y * r * Mathf.Sin(t) / 2));
    }

    /// <summary>
    /// 床を生成する。
    /// </summary>
    void CreateFloor()
    {
        //インスタンス生成。
        GameObject floor = Instantiate(_FloorObject);
        floor.transform.SetParent(transform);
        //サイズ決定。
        float x = _RoomSizeMinMax.x, z = _RoomSizeMinMax.y + 1;
        Vector3 sca = new Vector3(Random.Range(x, z), Random.Range(x, z), Random.Range(x, z));

        //整数に丸める。
        sca.x = Mathf.RoundToInt(sca.x);
        sca.y = Mathf.RoundToInt(sca.x);
        sca.z = Mathf.RoundToInt(sca.z);

        floor.transform.localScale = sca;
        //ポジション決定。
        floor.transform.position = CalcRandomPointInCircle();
    }

    /// <summary>
    /// 部屋の決定。
    /// </summary>
    void DecisionRoom()
    {
        //部屋の親となる空のオブジェクト
        GameObject Rooms = new GameObject("Rooms");
        //一時的に部屋のトランスフォームを保持するリスト
        List<Transform> TmpRoomList = new List<Transform>();
        //子供たちのトランスフォームを取得
        Transform[] childs = gameObject.GetComponentsInChildren<Transform>();
        //loop
        int Ccount = gameObject.transform.childCount;
        for (int idx = 1; idx < Ccount; idx++)
        {
            //平べったく
            Vector3 sca = childs[idx].lossyScale;
            sca.y = 1.0f;
            childs[idx].localScale = sca;

            float size = Mathf.Min(sca.x, sca.z);
            RoomBlock room = null;
            if (room = childs[idx].gameObject.GetComponent<RoomBlock>())
            {
                //しきい値より大きいかどうか？
                if (room.passed = (size >= _Threshold))
                {
                    Vector3 pos = childs[idx].position;
                    pos.x = Mathf.RoundToInt(pos.x);
                    pos.y = Mathf.RoundToInt(pos.y);
                    pos.z = Mathf.RoundToInt(pos.z);
                    //部屋の中心ポジションをリストに追加。
                    AddRoom(pos, sca);
                    TmpRoomList.Add(childs[idx]);
                }
            }
        }
        foreach(Transform room in TmpRoomList)
        {
            room.SetParent(Rooms.transform);
        }
        TmpRoomList.Clear();
    }

    /// <summary>
    /// ダンジョン生成するコルーチン
    /// </summary>
    /// <returns></returns>
    IEnumerator Generator()
    {
        //床生成ループ
        while (transform.childCount < _MaxGenerateNum)
        {
            //待つ
            yield return new WaitForSeconds(_GeneratorInterval);
            //床生成
            CreateFloor();
        }

        //子供たちのリジッドボディ取得
        Rigidbody[] rigids = gameObject.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rigid in rigids)
        {
            //リジッドボディのキネマティックをfalseに設定する。
            //物理エンジンで部屋をばらけさせる。
            rigid.isKinematic = false;
        }

        //物理挙動が落ち着くまで少し待つ(適当)
        float counter = 0.0f,n = 1.0f;
        while (true)
        {
            //n秒毎にチェック
            yield return new WaitForSeconds(n);
            //時間加算
            counter += n;

            bool bre = true;
            foreach (Rigidbody rigid in rigids)
            {
                //起きていたらダメ
                if (!rigid.IsSleeping())
                    bre = false;
            }

            //全員が寝ているか時間経過でループを抜ける。
            if (bre || counter > 5.0f)
                break;
        }

        foreach (Rigidbody rigid in rigids)
        {
            //リジッドボディのキネマティックをtrueに設定する。            
            rigid.isKinematic = true;
        }
        //部屋の決定
        DecisionRoom();

        //ドロネー三角形分割
        List<Triangle> Triangles = _DelaunayTriangles.DelaunaySplit(_PointList);
        //三角形リストから全域木生成。
        _SpanningTree.CreateLine(Triangles);
        //全域木から最小全域木を作成。
        _SpanningTree.CreateMinimumTree(_PointList);
        //最小全域木に多少の経路を追加。
        _SpanningTree.AddRootforMiniLine(_PointList, _AddRoot);
        //経路から通路を計算。
        List<PassWay> passway = _SpanningTree.CreatePathway();
        //配列を作成。
        _Dangeon.CreateArray();
        //ダンジョンを配置
        _Dangeon.DungeonArrangement(passway);
    }
}
