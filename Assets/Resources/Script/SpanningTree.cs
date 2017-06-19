using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//辺
[System.Serializable]
public class Line
{
    //頂点
    public Point p1, p2;
    //コスト
    [SerializeField]
    private int _Cost;
    public int cost { get { return _Cost; } }
    //type(Debug)
    public enum LineType
    {
        Normal,
        Mini,
        Add
    }
    public LineType _Type;


    public Line(Point v1, Point v2, LineType type = Line.LineType.Normal)
    {
        p1 = v1;
        p2 = v2;
        //コスト計算
        Vector3 vec = v1.pos - v2.pos;
        _Cost = (int)vec.magnitude;

        _Type = type;
    }

    static public bool operator ==(Line t1, Line t2)
    {
        return (t1.p1 == t2.p1 && t1.p2 == t2.p2 ||
                t1.p1 == t2.p2 && t1.p2 == t2.p1);
    }

    static public bool operator !=(Line t1, Line t2)
    {
        return false;
    }

    public void DebugDrawLine()
    {
        Color color = Color.grey;
        switch (_Type)
        {
            case LineType.Mini:
                color = Color.red;
                break;
            case LineType.Add:
                color = Color.yellow;
                break;
        }

        Debug.DrawLine(p1.pos, p2.pos, color);
    }
};

//通路
[System.Serializable]
public class PassWay
{
    //つなぐポジション
    public Vector3 p1, p2;
    //つないでいる部屋のID
    public int ID1, ID2;
    public PassWay(Vector3 v1, int id1, Vector3 v2, int id2)
    {
        p1 = v1;
        p2 = v2;
        ID1 = id1;
        ID2 = id2;
    }

    public void DrawDebugLine()
    {
        Debug.DrawLine(p1, p2, Color.blue);
    }
}

//全域木
public class SpanningTree : MonoBehaviour {

    //辺のリスト
    [SerializeField]
    private List<Line> _LineList;
    //最小経路+追加文の経路
    [SerializeField]
    List<Line> _MiniLine = new List<Line>();
    //部屋と部屋をつなぐ通路
    [SerializeField]
    List<PassWay> _Pathway = new List<PassWay>();
    

    private void Update()
    {
        foreach (Line line in _LineList)
        {
            line.DebugDrawLine();
        }

        foreach (PassWay way in _Pathway)
        {
            way.DrawDebugLine();
        }
    }

    //重複チェック追加
    void AddList(Line line)
    {
        foreach (Line l in _LineList)
        {
            //重複チェック
            if (line == l)
            {
                return;
            }
        }
        _LineList.Add(line);
    }

    //てきとう。
    bool AddMiniList(Line line)
    {
        foreach (Line l in _MiniLine)
        {
            //重複チェック
            if (line == l)
            {
                return false;
            }
        }
        _MiniLine.Add(line);
        return true;
    }

    //三角形リストから辺のリストを生成。
    public void CreateLine(List<Triangle> traiangles)
    {
        //辺を追加。
        foreach(Triangle t in traiangles)
        {
            AddList(new Line(t.p1, t.p2));
            AddList(new Line(t.p2, t.p3));
            AddList(new Line(t.p3, t.p1));
        }
        //コストが小さい順にソート
        _LineList.Sort((a, b) => a.cost - b.cost);
    }

    //最小全域木作成
    public void CreateMinimumTree(List<Point> pointlist)
    {        
        foreach(Line line in _LineList)
        {
            //グループが異なるモノを1つにまとめていく
            if (line.p1.group != line.p2.group)
            {
                //追加
                line._Type = Line.LineType.Mini;
                _MiniLine.Add(line);
                //グループをまとめる。
                int group = line.p2.group;
                foreach(Point p in pointlist)
                {
                    p.group = (p.group == group) ? line.p1.group : p.group;             
                }
            }
        }
    }

    //最小全域木にルートを追加。
    public void AddRootforMiniLine(List<Point> pointlist,float percent)
    {
        int diff = _LineList.Count - _MiniLine.Count;
        //追加するルートの本数計算
        int addnum = Mathf.Min(diff, Mathf.RoundToInt(_MiniLine.Count * percent));
        int count = 0;
        while (count < addnum)
        {
            int idx1 = Random.Range(0, pointlist.Count);
            int idx2 = Random.Range(0, pointlist.Count);
            //てきとうなライン作成。
            Line line = new Line(pointlist[idx1], pointlist[idx2]);

            foreach (Line l in _LineList)
            {
                if (line == l)
                {
                    if (AddMiniList(l))
                    {
                        count++;
                        l._Type = Line.LineType.Add;
                    }
                    break;
                }
            }
        }

    }

    //通路生成。
    public List<PassWay> CreatePathway()
    {
        //通路の親
        GameObject ways;
        if ((ways = GameObject.Find("Ways")) == null)
            ways = new GameObject("Ways");//ないなら生成。
        foreach(Line line in _MiniLine)
        {
            //p1->p2
            Vector3 vec = (line.p2.pos - line.p1.pos) / 2;
            //辺の中心座標
            Vector3 center = line.p1.pos + vec;

            Vector3 pos1 = line.p1.pos, pos2 = line.p2.pos;
            Vector3 s1 = line.p1.roomSize/2, s2 = line.p2.roomSize/2;
            Rect R1, R2;
            //左　下　右　上の辺
            R1 = new Rect(pos1.x - s1.x, pos1.z - s1.z, pos1.x + s1.x, pos1.z + s1.z);
            R2 = new Rect(pos2.x - s2.x, pos2.z - s2.z, pos2.x + s2.x, pos2.z + s2.z);

            Vector2 flg = Vector2.zero;
            //X軸判定
            if(R1.y < center.z && center.z < R1.height &&
               R2.y < center.z && center.z < R2.height)
            {
                flg.x = 1;
            }
            //Y軸判定
            if (R1.x < center.x && center.x < R1.width &&
               R2.x < center.x && center.x < R2.width)
            {
                flg.y = 1;
            }

            if(flg.x == 0 && flg.y == 0)
            {
                //L時の通路を制作
                //比較
                Point height, low;

                if (pos1.z > pos2.z)
                {
                    height = line.p1;
                    low = line.p2;
                }
                else
                {
                    height = line.p2;
                    low = line.p1;
                }

                //横に出す。
                Vector3 p1 = new Vector3(height.pos.x, 0, height.pos.z);
                Vector3 p2 = new Vector3(low.pos.x, 0, height.pos.z);
                _Pathway.Add(new PassWay(p1, height.Id, p2, low.Id));
                //縦に出す。
                Vector3 p3 = new Vector3(low.pos.x, 0, height.pos.z);
                Vector3 p4 = new Vector3(low.pos.x, 0, low.pos.z);
                _Pathway.Add(new PassWay(p3, height.Id, p4, low.Id));
            }
            else if(flg.x == 1 && flg.y == 0)
            {
                //横に伸ばした通路
                Vector3 p1, p2;
                p1 = new Vector3(pos1.x, 0, center.z);
                p2 = new Vector3(pos2.x, 0, center.z);
                _Pathway.Add(new PassWay(p1, line.p1.Id, p2, line.p2.Id));
            }
            else if(flg.x == 0 && flg.y == 1)
            {
                //縦に伸ばす通路
                Vector3 p1, p2;
                p1 = new Vector3(center.x, 0, pos1.z);
                p2 = new Vector3(center.x, 0, pos2.z);
                _Pathway.Add(new PassWay(p1, line.p1.Id, p2, line.p2.Id));
            }
            //debug
            GameObject c = new GameObject();
            c.transform.position = center;
            c.name = line.p1.Id.ToString() + "To" + line.p2.Id.ToString();
            c.transform.SetParent(ways.transform);

        }
        return _Pathway;
    }
}
