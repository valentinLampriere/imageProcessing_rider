using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Material[] cubesMat; // 0 blue 1 red
    [SerializeField] private Manager manager;
    [SerializeField] private ColorDetection cd;

    private List<Vector2> points;

    //void Awake()
    //{
    //    points = new List<Vector2>();
    //}

    // Start is called before the first frame update
    void Start()
    {
        List<Vector2> points = new List<Vector2>();
        float x = 0f;
        float y = 280f;

        for (int i = 0; i < 75; i++)
        {
            x += Random.Range(10.0f, 20.0f);
            y += Random.Range(-10.0f, 10.0f);
            points.Add(new Vector2(x, y));
        }

        //points.Add(new Vector2(2, 2));
        //points.Add(new Vector2(10, 5));

        CreateLine(points);

        //CreateRectangleCube(new Vector2(0, 3), new Vector2(2, 0), new Vector2(5, 5), new Vector2(7, 2), Color.red);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateLine(List<Vector2> points)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateLineCube(points[i], points[i + 1], cubePrefab);
        }
    }

    public void CreateRectangleCube(Vector2 _p1, Vector2 _p2, Vector2 _p3, Vector2 _p4, Color color)
    {
        Vector2 pl = (_p1 + _p2) / 2f;
        Vector2 pr = (_p3 + _p4) / 2f;

        GameObject rectangle = CreateLineCube(pl, pr, obstaclePrefab);
        float scaleY = Vector2.Distance(_p1, _p2);
        Vector3 currentScale = rectangle.transform.localScale;
        rectangle.transform.localScale = new Vector3(currentScale.x, scaleY, currentScale.z);

        ChangeRectangleColor(rectangle, color);
    }

    public void ChangeRectangleColor(GameObject rectangle, Color color)
    {
        MeshRenderer mesh = rectangle.GetComponent<MeshRenderer>();

        if (color == Color.blue)
        {
            mesh.material = cubesMat[0];
        }
        else if (color == Color.red)
        {
            mesh.material = cubesMat[1];
        }

        Color c = mesh.material.color;
        rectangle.GetComponent<Obstacle>().CurrentColor = c;
        mesh.material.color = new Color(c.r, c.g, c.b, 0.3f);
    }

    public GameObject CreateLineCube(Vector2 _p1, Vector2 _p2, GameObject objectType)
    {
        Vector2 pos = (_p1 + _p2) / 2f;
        //pos += new Vector2(0.5f, -0.5f);
        float scale = Vector2.Distance(_p1, _p2);
        float rotate = Mathf.Rad2Deg * Mathf.Atan2(_p2.y - _p1.y, _p2.x - _p1.x);

        GameObject cube = Instantiate(objectType, pos, Quaternion.Euler(0, 0, rotate));
        cube.transform.localScale = new Vector3(scale + (Mathf.Abs(rotate) * 0.001f), 1f, 10f);

        return cube;
    }

    public void SetTerrain(List<Vector2> _points)
    {
        points = _points;

        CreateLine(points);
    }

    public void CreateRectangles()
    {
        List<List<PointF>> blueRectangles = cd.GetBlueRectangles();
        Debug.Log(blueRectangles.Count);

        foreach (List<PointF> listPointF in blueRectangles)
        {
            Vector2 p1 = new Vector2(listPointF[0].X, listPointF[0].Y);
            Vector2 p2 = new Vector2(listPointF[1].X, listPointF[1].Y);
            Vector2 p3 = new Vector2(listPointF[2].X, listPointF[2].Y);
            Vector2 p4 = new Vector2(listPointF[3].X, listPointF[3].Y);
            CreateRectangleCube(p1, p4, p2, p3, UnityEngine.Color.blue);
        }

        List<List<PointF>> redRectangles = cd.GetRedRectangles();
        Debug.Log(blueRectangles.Count);

        foreach (List<PointF> listPointF in redRectangles)
        {
            Vector2 p1 = new Vector2(listPointF[0].X, listPointF[0].Y);
            Vector2 p2 = new Vector2(listPointF[1].X, listPointF[1].Y);
            Vector2 p3 = new Vector2(listPointF[2].X, listPointF[2].Y);
            Vector2 p4 = new Vector2(listPointF[3].X, listPointF[3].Y);
            CreateRectangleCube(p1, p4, p2, p3, UnityEngine.Color.red);
        }
    }
}
