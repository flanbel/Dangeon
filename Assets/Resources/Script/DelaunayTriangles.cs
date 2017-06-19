using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//三角形。
[System.Serializable]
public class Triangle
{
    public Point p1, p2, p3;    // 頂点座標

    public Triangle(Point v1, Point v2, Point v3)
    {
        p1 = v1;
        p2 = v2;
        p3 = v3;
    }

    static public bool operator ==(Triangle t1, Triangle t2)
    {
        return (t1.p1 == t2.p1 && t1.p2 == t2.p2 && t1.p3 == t2.p3 ||
               t1.p1 == t2.p2 && t1.p2 == t2.p3 && t1.p3 == t2.p1 ||
               t1.p1 == t2.p3 && t1.p2 == t2.p1 && t1.p3 == t2.p2 ||

               t1.p1 == t2.p3 && t1.p2 == t2.p2 && t1.p3 == t2.p1 ||
               t1.p1 == t2.p2 && t1.p2 == t2.p1 && t1.p3 == t2.p3 ||
               t1.p1 == t2.p1 && t1.p2 == t2.p3 && t1.p3 == t2.p2);
    }

    static public bool operator !=(Triangle t1, Triangle t2)
    {
        return false;
    }

    // ======================================  
    // 他の三角形と共有点を持つか  
    // ======================================   
    public bool hasCommonPoints(Triangle t)
    {
        return (p1 == t.p1 || p1 == t.p2 || p1 == t.p3 ||
               p2 == t.p1 || p2 == t.p2 || p2 == t.p3 ||
               p3 == t.p1 || p3 == t.p2 || p3 == t.p3);
    }
};
//円
public struct Circle
{
    public Vector3 center;  // 中心座標
    public float radius;  // 半径
};

//ドロネー三角形分割をするクラス。
public class DelaunayTriangles : MonoBehaviour {
    //三角形リスト
    [SerializeField]
    private List<Triangle> _TriangleList = new List<Triangle>();

    private SpanningTree _SpanningTree;

    private void Start()
    {
        _SpanningTree = GetComponent<SpanningTree>();
    }   

    //全ての点を内包する三角形を生成
    Triangle DecisionIncludingTriangle(List<Point> points)
    {
        // ======================================  
        // 外部三角形を作る(結構大きいけど問題なし。)
        // ======================================  
        float maxX, maxZ; maxX = maxZ = float.MinValue;
        float minX, minZ; minX = minZ = float.MaxValue;
        //最小、最大を求める。
        foreach (Point it in points)
        {
            minX = Mathf.Min(minX, it.pos.x);
            minZ = Mathf.Min(minZ, it.pos.z);
            maxX = Mathf.Max(maxX, it.pos.x);
            maxZ = Mathf.Max(maxZ, it.pos.z);
        }

        // すべての点を包含する矩形の外接円
        Vector3 center = new Vector3(0, 0, 0);
        center.x = (maxX + minX) * 0.5f;          // 中心x座標
        center.z = (maxZ + minZ) * 0.5f;          // 中心z座標

        float dX = maxX - center.x;
        float dZ = maxZ - center.z;
        float radius = Mathf.Sqrt(dX * dX + dZ * dZ) + 1.0f;  // 半径

        Vector3 p1 = new Vector3();    
        p1.x = (center.x - Mathf.Sqrt(3.0f) * radius);
        p1.z = (center.z - radius);

        Vector3 p2 = new Vector3();    
        p2.x = (center.x + Mathf.Sqrt(3.0f) * radius);
        p2.z = (center.z - radius);

        Vector3 p3 = new Vector3(); 
        p3.x = center.x;
        p3.z = (center.z + 2.0f * radius);

        //ポイント生成。(デバッグ用)
        Vector3[] pos = { center, p1, p2, p3 };
        for(int i = 0; i < 4; i++)
        {
            //わかりやすいように丸を出すよー。
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.name = "BigTriangle" + i.ToString();
            point.transform.SetParent(GameObject.Find("Points").transform);
            point.transform.position = pos[i];
            point.transform.localScale = Vector3.one * 5;

            point.GetComponent<MeshRenderer>().material = Resources.Load("Material/BigPoint") as Material;
        }

        return new Triangle(new Point(p1, -1, Vector3.zero), new Point(p2, -1, Vector3.zero), new Point(p3, -1, Vector3.zero));
    }

    //再分割？
    private void manageDuplicativeTriangles
        (List<Triangle> newTriangleList, List<Triangle> duplicativeTriangleList, Triangle t)
    {
        bool existsInNewTriangleList = false;
        foreach (Triangle iter in newTriangleList)
        {
            if (iter == t)
            {
                existsInNewTriangleList = true;
                bool existsInDuplicativeTriangleList = false;

                foreach (Triangle iter2 in duplicativeTriangleList)
                {
                    if (iter2 == t)
                    {
                        existsInDuplicativeTriangleList = true;
                        break;
                    }
                }
                if (!existsInDuplicativeTriangleList)
                {
                    duplicativeTriangleList.Add(t);
                }
                break;
            }
        }
        if (!existsInNewTriangleList) newTriangleList.Add(t);
    }

    //三角形分割
    private void TriangleSplit(List<Triangle> triangleList, List<Point> points)
    {
        foreach (Point vIter in points)
        {
            Point p = vIter;

            // --------------------------------------  
            // 追加候補の三角形を保持する一時リスト  
            // --------------------------------------  
            List<Triangle> newTriangleList = new List<Triangle>();          // 新規分割された三角形                
            List<Triangle> duplicativeTriangleList = new List<Triangle>();  // 重複リスト


            // --------------------------------------  
            // 現在の三角形セットから要素を一つずつ取り出して、  
            // 与えられた点が各々の三角形の外接円の中に含まれるかどうか判定  
            // --------------------------------------  
            for (int idx = 0; idx < triangleList.Count;)
            {
                // 三角形セットから三角形を取りだして…
                Triangle t = triangleList[idx];

                // その外接円を求める。  
                Circle c;
                {
                    // 三角形の各頂点座標を (x1, z1), (x2, z2), (x3, z3) とし、  
                    // その外接円の中心座標を (x, z) とすると、  
                    //     (x - x1)  (x - x1) + (z - z1)  (z - z1)  
                    //   = (x - x2)  (x - x2) + (z - z2)  (z - z2)  
                    //   = (x - x3)  (x - x3) + (z - z3)  (z - z3)  
                    // より、以下の式が成り立つ  
                    //  
                    // x = { (z3 - z1)  (x2  x2 - x1  x1 + z2  z2 - z1  z1)  
                    //     + (z1 - z2)  (x3  x3 - x1  x1 + z3  z3 - z1  z1)} / c  
                    //  
                    // z = { (x1 - x3)  (x2  x2 - x1  x1 + z2  z2 - z1  z1)  
                    //     + (x2 - x1)  (x3  x3 - x1  x1 + z3  z3 - z1  z1)} / c  
                    //  
                    // ただし、  
                    //   c = 2  {(x2 - x1)  (z3 - z1) - (z2 - z1)  (x3 - x1)} 

                    float x1 = t.p1.pos.x; float z1 = t.p1.pos.z;
                    float x2 = t.p2.pos.x; float z2 = t.p2.pos.z;
                    float x3 = t.p3.pos.x; float z3 = t.p3.pos.z;

                    float m = 2.0f * ((x2 - x1) * (z3 - z1) - (z2 - z1) * (x3 - x1));
                    float x = ((z3 - z1) * (x2 * x2 - x1 * x1 + z2 * z2 - z1 * z1)
                              + (z1 - z2) * (x3 * x3 - x1 * x1 + z3 * z3 - z1 * z1)) / m;
                    float z = ((x1 - x3) * (x2 * x2 - x1 * x1 + z2 * z2 - z1 * z1)
                              + (x2 - x1) * (x3 * x3 - x1 * x1 + z3 * z3 - z1 * z1)) / m;

                    c.center.x = x;
                    c.center.z = z;

                    // 外接円の半径 r は、半径から三角形の任意の頂点までの距離に等しい 
                    float DX = t.p1.pos.x - x;
                    float DZ = t.p1.pos.z - z;
                    float radius = Mathf.Sqrt(DX * DX + DZ * DZ);

                    c.radius = radius;
                }

                float dx = c.center.x - p.pos.x;
                float dz = c.center.z - p.pos.z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                // ======================================  
                // 一時リストを使って重複判定  
                // ======================================  
                if (dist < c.radius)
                {
                    // 再分割

                    Triangle t1 = new Triangle(p, t.p1, t.p2);
                    t1.p1 = p; t1.p2 = t.p1; t1.p3 = t.p2;
                    manageDuplicativeTriangles(newTriangleList, duplicativeTriangleList, t1);

                    Triangle t2 = new Triangle(p, t.p2, t.p3);
                    t2.p1 = p; t2.p2 = t.p2; t2.p3 = t.p3;
                    manageDuplicativeTriangles(newTriangleList, duplicativeTriangleList, t2);

                    Triangle t3 = new Triangle(p, t.p3, t.p1);
                    t3.p1 = p; t3.p2 = t.p3; t3.p3 = t.p1;
                    manageDuplicativeTriangles(newTriangleList, duplicativeTriangleList, t3);

                    triangleList.Remove(triangleList[idx]);
                }
                else
                    ++idx;
            }

            // --------------------------------------  
            // 一時リストのうち、重複のないものを三角形リストに追加   
            // --------------------------------------  
            foreach (Triangle iter in newTriangleList)
            {
                bool exists = false;
                foreach (Triangle iter2 in duplicativeTriangleList)
                {
                    if (iter == iter2)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists) triangleList.Add(iter);
            }
        }
    }

    //ドロネー三角形分割
    public List<Triangle> DelaunaySplit(List<Point> points)
    {
        //初期化。
        _TriangleList.Clear();
        //全ての点を内包する三角形。
        Triangle hugeTriangle = DecisionIncludingTriangle(points);
        //listに追加。
        _TriangleList.Add(hugeTriangle);

        //三角形分割
        TriangleSplit(_TriangleList,points);

        //外部三角形の頂点を削除
        for (int idx = 0; idx < _TriangleList.Count;)
        {
            if (hugeTriangle.hasCommonPoints(_TriangleList[idx]))
                _TriangleList.Remove(_TriangleList[idx]);
            else
                ++idx;
        }

        return _TriangleList;
    }
};